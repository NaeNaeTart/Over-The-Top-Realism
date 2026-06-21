using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace OverTheTopRealism.Features
{
    [HarmonyPatch]
    public static class RealismSystems
    {
        // ─── Persistence Vitals ──────────────────────────────────────────────
        private static float _concussionTimer = 0f;
        private static float _crampCooldown = 0f;
        private static float _microNapCooldown = 0f;
        private static HashSet<string> _compoundFractures = new HashSet<string>();

        // ─── Helper to identify if Body is the local player ──────────────────
        private static bool IsLocalPlayer(Body body)
        {
            if (body == null) return false;
            // PlayerCamera.main.body holds the local player's Body instance.
            // If multiplayer is active, this check guarantees we only patch the client's own body.
            if (PlayerCamera.main != null)
            {
                return body == PlayerCamera.main.body;
            }
            return true; // Fallback to true if camera is not initialized yet
        }

        // ─── Cardiovascular Postfix ──────────────────────────────────────────
        [HarmonyPatch(typeof(Body), "HandleCirculation")]
        [HarmonyPostfix]
        public static void HandleCirculation_Postfix(Body __instance, Painkillers painkillers)
        {
            if (!Plugin.Cfg.ModEnabled.Value || !Plugin.Cfg.CardioStrainEnabled.Value)
                return;

            if (!IsLocalPlayer(__instance))
                return;

            float hr = __instance.heartRate;
            float strokeThreshold = Plugin.Cfg.StrokeRiskThreshold.Value;

            if (hr > strokeThreshold && __instance.alive)
            {
                // Accumulate stroke risk based on how high the heart rate is above threshold
                float hrDelta = hr - strokeThreshold;
                float buildRate = Plugin.Cfg.StrokeBuildRate.Value;
                
                // Increase strokeAmount
                float strokeDelta = hrDelta * buildRate * Time.deltaTime;
                __instance.strokeAmount = Mathf.Clamp(__instance.strokeAmount + strokeDelta, 0f, 100f);

                // If stroke risk is critically high, introduce arrhythmia or trigger V-Fib (cardiac arrest)
                if (__instance.strokeAmount > 60f && UnityEngine.Random.value < (Plugin.Cfg.StrokeFibrillationChance.Value * Time.deltaTime))
                {
                    Plugin.Logger.LogWarning($"[Realism] Critically high stroke level ({__instance.strokeAmount:F1}%) caused sudden cardiovascular fibrillation!");
                    __instance.TryStartFibrillation(forced: true);
                }
            }
        }

        // ─── Trauma, Fatigue, Pain Shock, and Sepsis Postfix ──────────────────
        [HarmonyPatch(typeof(Body), "HandleBody")]
        [HarmonyPostfix]
        public static void HandleBody_Postfix(Body __instance, Painkillers pnk)
        {
            if (!Plugin.Cfg.ModEnabled.Value || !__instance.alive)
                return;

            if (!IsLocalPlayer(__instance))
                return;

            // Cleanup healed fractures from compound list
            if (__instance.limbs != null)
            {
                foreach (var limb in __instance.limbs)
                {
                    if (limb != null && !limb.broken)
                    {
                        _compoundFractures.Remove(limb.fullName);
                    }
                }
            }

            // ── 1. Traumatic Fracture Strain (broken leg penalties) ──
            if (Plugin.Cfg.LegFractureStrainEnabled.Value && __instance.standing && Mathf.Abs(__instance.moveDir.x) > 0.1f)
            {
                bool hasBrokenLeg = false;
                if (__instance.limbs != null)
                {
                    foreach (var limb in __instance.limbs)
                    {
                        if (limb != null && limb.isLegLimb && limb.broken && !limb.dismembered)
                        {
                            hasBrokenLeg = true;

                            // Apply sharp localized pain and physical shock spikes
                            float painSpike = Plugin.Cfg.FracturePainSpike.Value * Time.deltaTime * 6f;
                            float shockSpike = Plugin.Cfg.FractureShockSpike.Value * Time.deltaTime * 4f;

                            float maxFracturePain = limb.splinted ? 40f : 85f;
                            if (limb.pain < maxFracturePain)
                            {
                                limb.pain = Mathf.Clamp(limb.pain + painSpike, 0f, maxFracturePain);
                            }
                            __instance.shock = Mathf.Clamp(__instance.shock + shockSpike, 0f, 100f);

                            // Walking on broken legs degrades muscle health (tissue tearing)
                            limb.muscleHealth = Mathf.Clamp(limb.muscleHealth - (0.4f * Time.deltaTime), 0f, 100f);

                            // Walking on broken legs causes internal bleeding to build up in the limb
                            limb.bleedAmount = Mathf.Clamp(limb.bleedAmount + (0.12f * Time.deltaTime), 0f, 100f - limb.skinHealth);

                            // Walking on an UNsplinted fractured leg has a chance to trigger a Compound Fracture!
                            if (Plugin.Cfg.CompoundFracturesEnabled.Value && !limb.splinted)
                            {
                                // Configurable chance per second of walking to worsen simple fracture into compound fracture
                                if (UnityEngine.Random.value < (Plugin.Cfg.CompoundFractureChance.Value * Time.deltaTime))
                                {
                                    Plugin.Logger.LogWarning($"[Realism] Unsplinted bone in {limb.fullName} tore through skin! COMPOUND FRACTURE!");
                                    limb.bleedAmount = Mathf.Max(limb.bleedAmount, 50f); // Severe immediate hemorrhaging
                                    limb.muscleHealth = Mathf.Max(0f, limb.muscleHealth - 40f);
                                    limb.pain = 100f; // Utter agony
                                    __instance.shock = Mathf.Clamp(__instance.shock + 30f, 0f, 100f);
                                    __instance.Scream();
                                    _compoundFractures.Add(limb.fullName);
                                    Sound.Play("gore", limb.transform.position);
                                }
                            }
                        }
                    }
                }

                // Chance to scream and stumble from sudden agony
                if (hasBrokenLeg && UnityEngine.Random.value < (0.12f * Time.deltaTime))
                {
                    Plugin.Logger.LogInfo("[Realism] Severe fracture pain caused player to scream and stagger!");
                    __instance.Scream();
                    __instance.shock = Mathf.Clamp(__instance.shock + 12f, 0f, 100f);
                }
            }

            // ── 2. Pain Shock & Sudden Fainting (Passing out from unbearable pain) ──
            if (Plugin.Cfg.PainShockFaintingEnabled.Value && __instance.averagePain > Plugin.Cfg.FaintPainThreshold.Value && __instance.conscious)
            {
                float excessPain = __instance.averagePain - Plugin.Cfg.FaintPainThreshold.Value;
                float faintChance = excessPain * 0.005f * Time.deltaTime; // Higher pain above threshold = faster fainting

                if (UnityEngine.Random.value < faintChance)
                {
                    Plugin.Logger.LogWarning($"[Realism] Player fainted from extreme traumatic pain shock ({__instance.averagePain:F1}% average pain)!");
                    __instance.consciousness = 0f;
                    __instance.shock = Mathf.Clamp(__instance.shock + 20f, 0f, 100f);
                    __instance.Ragdoll();
                }
            }

            // ── 3. Sleep Deprivation / Fatigue Micro-Naps ──
            _microNapCooldown -= Time.deltaTime;
            if (Plugin.Cfg.FatigueNapsEnabled.Value && __instance.energy < Plugin.Cfg.MicroNapThreshold.Value && __instance.conscious && _microNapCooldown <= 0f)
            {
                float movementIntensity = Mathf.Abs(__instance.moveDir.x) + Mathf.Abs(__instance.moveDir.y);
                if (movementIntensity > 0.1f)
                {
                    float fatigueExcess = Plugin.Cfg.MicroNapThreshold.Value - __instance.energy;
                    float microNapChance = fatigueExcess * 0.004f * Time.deltaTime; // High chance of collapsing as energy hits 0

                    if (UnityEngine.Random.value < microNapChance)
                    {
                        Plugin.Logger.LogWarning($"[Realism] Fatigue blackout! Player collapsed into a micro-nap (Energy: {__instance.energy:F1}%).");
                        __instance.consciousness = 0f;
                        __instance.Ragdoll();
                        _microNapCooldown = 45f; // 45 seconds of grace period after waking up before another micro-nap can trigger
                    }
                }
            }

            // ── 4. Sepsis and Wound Contamination Progression ──
            if (Plugin.Cfg.SepsisProgressionEnabled.Value && __instance.limbs != null)
            {
                float sepsisDelta = 0f;
                foreach (var limb in __instance.limbs)
                {
                    if (limb != null && limb.infected && !limb.dismembered)
                    {
                        // Contaminated infected wounds build septic shock over time
                        sepsisDelta += limb.infectionAmount * Plugin.Cfg.SepsisBuildRate.Value * Time.deltaTime;

                        // Localized tissue necrosis (infection degrades muscle and skin health)
                        limb.muscleHealth = Mathf.Clamp(limb.muscleHealth - (0.04f * Time.deltaTime), 0f, 100f);
                        limb.skinHealth = Mathf.Clamp(limb.skinHealth - (0.04f * Time.deltaTime), 0f, 100f);
                    }
                }

                if (sepsisDelta > 0f)
                {
                    __instance.septicShock = Mathf.Clamp(__instance.septicShock + sepsisDelta, 0f, 100f);

                    // High systemic sepsis causes high body fever (overheating core temperature)
                    if (__instance.septicShock > 25f)
                    {
                        __instance.temperature = Mathf.MoveTowards(__instance.temperature, 40.5f, 0.008f * Time.deltaTime * (__instance.septicShock / 10f));
                    }
                }
            }

            // ── 5. Concussions & Head Trauma ──
            if (Plugin.Cfg.ConcussionsEnabled.Value && __instance.limbs != null && __instance.limbs.Length > 0)
            {
                var head = __instance.limbs[0];
                if (head != null && !head.dismembered)
                {
                    if (head.pain > 40f)
                    {
                        // Concussion accumulates from high head pain
                        _concussionTimer = Mathf.Clamp(_concussionTimer + (head.pain * 0.05f * Time.deltaTime * Plugin.Cfg.ConcussionSensitivity.Value), 0f, 100f);
                    }
                    else
                    {
                        // Concussions slowly fade away
                        _concussionTimer = Mathf.Max(_concussionTimer - (0.15f * Time.deltaTime), 0f);
                    }

                    if (_concussionTimer > 5f)
                    {
                        // Physical camera/head jittering simulating concussive vertigo
                        if (head.rb != null && UnityEngine.Random.value < 0.25f)
                        {
                            head.rb.AddForce(UnityEngine.Random.insideUnitCircle * 45f);
                        }

                        // Momentary directional control vertigo/scramble
                        if (UnityEngine.Random.value < (0.02f * Time.deltaTime * (_concussionTimer / 10f)))
                        {
                            __instance.moveDir *= -1f; // VERTIGO: momentary reverse controls
                        }
                    }

                    // Severe concussion blackouts on jumping
                    if (_concussionTimer > 15f && __instance.standing && __instance.moveDir.y > 0.1f && __instance.conscious)
                    {
                        if (UnityEngine.Random.value < 0.35f)
                        {
                            Plugin.Logger.LogWarning($"[Realism] Jump-induced vertigo caused concussion blackout! Concussion Level: {_concussionTimer:F1}%");
                            __instance.consciousness = 0f;
                            __instance.Ragdoll();
                        }
                    }
                }
            }

            // ── 6. Electrolyte Cramps ──
            if (Plugin.Cfg.ElectrolyteCrampsEnabled.Value && __instance.thirst < Plugin.Cfg.CrampThreshold.Value && __instance.conscious)
            {
                _crampCooldown -= Time.deltaTime;
                if (_crampCooldown <= 0f)
                {
                    _crampCooldown = UnityEngine.Random.Range(30f, 65f); // Cramps strike every 30-65s of extreme dehydration

                    // Trigger leg cramp or hand cramp
                    bool isLegCramp = UnityEngine.Random.value > 0.4f;

                    if (isLegCramp && __instance.limbs != null && __instance.limbs.Length > 3)
                    {
                        // Select leg (Limb 2 or 3)
                        var leg = __instance.limbs[UnityEngine.Random.Range(2, 4)];
                        if (leg != null && !leg.dismembered)
                        {
                            Plugin.Logger.LogWarning($"[Realism] Severe dehydration leg cramp on {leg.fullName}!");
                            leg.pain = Mathf.Clamp(leg.pain + 40f, 0f, 100f);
                            __instance.shock = Mathf.Clamp(__instance.shock + 10f, 0f, 100f);
                            __instance.Scream();
                            // Temporarily trip / Ragdoll player
                            __instance.Ragdoll();
                        }
                    }
                    else
                    {
                        // Select arm hand slot and force drop item
                        int slotToDrop = UnityEngine.Random.Range(0, 2);
                        Plugin.Logger.LogWarning($"[Realism] Extreme dehydration hand cramp caused item drop in hand slot {slotToDrop}!");
                        __instance.DropItem(slotToDrop);
                    }
                }
            }

            // ── 7. Hypothermia & Shivering ──
            if (Plugin.Cfg.HypothermiaEnabled.Value && __instance.temperature < Plugin.Cfg.ShiverThreshold.Value)
            {
                // Core Hypothermia < 35°C
                // Double stamina exhaustion rate
                __instance.stamina = Mathf.Clamp(__instance.stamina - (2.2f * Time.deltaTime), 0f, Mathf.Max(70f, __instance.bloodOxygen));

                // Character shivers physically
                if (__instance.limbs != null)
                {
                    foreach (var limb in __instance.limbs)
                    {
                        if (limb != null && limb.rb != null && !limb.dismembered)
                        {
                            limb.rb.AddForce(UnityEngine.Random.insideUnitCircle * 18f);
                        }
                    }
                }

                // Hypothermic Numbness < 32°C (causes extreme hand/aim sway)
                if (__instance.temperature < 32f && __instance.limbs != null)
                {
                    foreach (var limb in __instance.limbs)
                    {
                        if (limb != null && limb.isArm && limb.rb != null && !limb.dismembered)
                        {
                            limb.rb.AddTorque(UnityEngine.Random.Range(-55f, 55f));
                        }
                    }
                }

                // Frostbite cell necrosis < 30°C
                if (__instance.temperature < 30f && __instance.limbs != null)
                {
                    foreach (var limb in __instance.limbs)
                    {
                        if (limb != null && !limb.isVital && !limb.dismembered)
                        {
                            limb.skinHealth = Mathf.Clamp(limb.skinHealth - (0.08f * Time.deltaTime), 0f, 100f);
                            limb.muscleHealth = Mathf.Clamp(limb.muscleHealth - (0.08f * Time.deltaTime), 0f, 100f);
                        }
                    }
                }
            }

            // ── 8. Respiratory Suffocation Panic ──
            if (Plugin.Cfg.SuffocationPanicEnabled.Value && __instance.bloodOxygen < Plugin.Cfg.OxygenPanicThreshold.Value)
            {
                // Panic adrenaline spikes
                __instance.adrenaline = Mathf.Clamp(__instance.adrenaline + (22f * Time.deltaTime), 0f, 100f);
                __instance.heartRate = Mathf.Lerp(__instance.heartRate, 195f, 4.5f * Time.deltaTime);

                // Hyperventilating bodily vibration
                if (__instance.limbs != null && __instance.limbs.Length > 0 && __instance.limbs[0] != null && __instance.limbs[0].rb != null)
                {
                    __instance.limbs[0].rb.AddForce(UnityEngine.Random.insideUnitCircle * 35f);
                }

                // Severe asphyxiation blackout
                if (__instance.bloodOxygen < 40f && __instance.conscious)
                {
                    __instance.consciousness = Mathf.MoveTowards(__instance.consciousness, 0f, 22f * Time.deltaTime);
                    if (__instance.consciousness <= 5f)
                    {
                        Plugin.Logger.LogWarning("[Realism] Asphyxiation blackout! Player lost consciousness from lack of oxygen.");
                        __instance.Ragdoll();
                    }
                }
            }

            // ── 9. Drug Realism (Painkillers, Sleeping Pills, and Stimulants) ──
            if (Plugin.Cfg.DrugRealismEnabled.Value)
            {
                Painkillers activePnk = pnk ?? __instance.GetComponent<Painkillers>();
                var pills = __instance.GetComponent<SleepingPills>();

                // Opiate Respiratory Depression & Overdose
                if (activePnk != null)
                {
                    if (Plugin.Cfg.DrugOverdoseEnabled.Value && activePnk.opiateAmount > 60f)
                    {
                        float depressionFactor = Mathf.Clamp01((activePnk.opiateAmount - 60f) / 140f); // 0 at 60, 1 at 200
                        float targetRate = Mathf.Lerp(__instance.respiratoryRate, 10f, depressionFactor);
                        __instance.respiratoryRate = Mathf.MoveTowards(__instance.respiratoryRate, targetRate, 25f * Time.deltaTime);

                        // If they are in a severe overdose (>150f opiateAmount)
                        if (activePnk.opiateAmount > 150f && activePnk.antagonistAmount <= 0f)
                        {
                            __instance.consciousness = Mathf.MoveTowards(__instance.consciousness, 0f, 15f * Time.deltaTime);
                            if (__instance.conscious && __instance.consciousness <= 5f)
                            {
                                Plugin.Logger.LogWarning($"[Realism] Player blacked out from severe Painkiller (Opiate) overdose! (Opiate: {activePnk.opiateAmount:F1})");
                                __instance.Ragdoll();
                            }
                            __instance.bloodOxygen = Mathf.Clamp(__instance.bloodOxygen - (10f * depressionFactor * Time.deltaTime), 0f, 100f);
                        }
                    }

                    // Opiate Withdrawal Tremors & Dysregulation
                    if (Plugin.Cfg.DrugWithdrawalEnabled.Value && activePnk.actualOpiateReception < -10f)
                    {
                        float severity = Mathf.Clamp01(Mathf.Abs(activePnk.actualOpiateReception + 10f) / 40f); // 0 at -10, 1 at -50
                        if (__instance.limbs != null)
                        {
                            foreach (var limb in __instance.limbs)
                            {
                                if (limb != null && limb.rb != null && !limb.dismembered)
                                {
                                    limb.rb.AddForce(UnityEngine.Random.insideUnitCircle * (12f * severity));
                                    if (limb.isArm)
                                    {
                                        limb.rb.AddTorque(UnityEngine.Random.Range(-35f, 35f) * severity);
                                    }
                                }
                            }
                        }
                        float targetTemp = 36.6f - (2.5f * severity);
                        __instance.temperature = Mathf.MoveTowards(__instance.temperature, targetTemp, 0.015f * Time.deltaTime * severity);
                    }
                }

                // Interactive Opioid + Sedative CNS Depression
                if (activePnk != null && pills != null && Plugin.Cfg.DrugOverdoseEnabled.Value)
                {
                    if (activePnk.opiateAmount > 20f && pills.amount > 30f)
                    {
                        float synergisticDepression = (activePnk.opiateAmount / 50f) * (pills.amount / 300f);
                        synergisticDepression = Mathf.Clamp(synergisticDepression, 0.1f, 3.5f);

                        __instance.bloodOxygen = Mathf.Clamp(__instance.bloodOxygen - (12f * synergisticDepression * Time.deltaTime), 0f, 100f);
                        __instance.consciousness = Mathf.MoveTowards(__instance.consciousness, 0f, 10f * synergisticDepression * Time.deltaTime);
                        if (__instance.conscious && __instance.consciousness <= 5f)
                        {
                            Plugin.Logger.LogWarning($"[Realism] Fatal Drug Interaction! Opioids and Sedatives mixed. Player blacked out. (Opiate: {activePnk.opiateAmount:F0}, Pill Amount: {pills.amount:F0})");
                            __instance.Ragdoll();
                        }
                    }
                }

                // Stimulant Toxicity & Crash
                if (Plugin.Cfg.DrugOverdoseEnabled.Value && (__instance.caffeinated > 150f || __instance.curAdrenaline > 75f))
                {
                    float stimulantLvl = Mathf.Max(__instance.caffeinated / 2f, __instance.curAdrenaline);
                    if (__instance.heartRate > 150f && UnityEngine.Random.value < (0.012f * Time.deltaTime * (stimulantLvl / 50f)))
                    {
                        Plugin.Logger.LogWarning($"[Realism] Extreme stimulant abuse caused violent cardiac arrhythmia!");
                        __instance.heartRate = Mathf.Lerp(__instance.heartRate, 210f, 12f * Time.deltaTime);
                        __instance.stamina = Mathf.Max(0f, __instance.stamina - 25f);
                        __instance.shock = Mathf.Clamp(__instance.shock + 15f, 0f, 100f);

                        if (stimulantLvl > 90f && UnityEngine.Random.value < Plugin.Cfg.StimulantFibrillationChance.Value)
                        {
                            Plugin.Logger.LogError($"[Realism] Arrhythmia deteriorated into Ventricular Fibrillation (cardiac arrest)!");
                            __instance.TryStartFibrillation(forced: true);
                        }
                    }
                }

                if (__instance.caffeinated > 0f && __instance.caffeinated < 35f)
                {
                    float crashFactor = (35f - __instance.caffeinated) / 35f;
                    __instance.energy = Mathf.Clamp(__instance.energy - (0.6f * crashFactor * Time.deltaTime), 0f, 100f);
                }
            }
        }

        // ─── Hemodynamics & Clotting Postfix ─────────────────────────────────
        [HarmonyPatch(typeof(Limb), "Update")]
        [HarmonyPostfix]
        public static void Limb_Update_Postfix(Limb __instance)
        {
            if (!Plugin.Cfg.ModEnabled.Value || !Plugin.Cfg.ClottingHemodynamicsEnabled.Value)
                return;

            var body = __instance.body;
            if (body == null || !body.alive)
                return;

            if (!IsLocalPlayer(body))
                return;

            // 1. High Blood Pressure Hemodynamics (High B.P. pumps blood out faster)
            if (body.bloodPressure > 120f && __instance.bleedAmount > 0f)
            {
                float excessPressure = body.bloodPressure - 120f;
                // High pressure forces out extra blood volume based on the pressure delta
                float extraBleed = __instance.totalBleedAmount * (excessPressure / 120f) * (Plugin.Cfg.BloodPressureBleedMult.Value - 1f) * Time.deltaTime;
                body.bloodVolume = Mathf.Max(body.bloodVolume - extraBleed, -100f);
            }

            // 2. Rest-Dependent Clotting (Active movement or thrashing prevents wounds from closing)
            float movement = Mathf.Abs(body.moveDir.x) + Mathf.Abs(body.moveDir.y);
            bool isMoving = movement > 0.1f;
            bool isFlopOrRagdoll = !body.standing; // If they are flopping/ragdolled, wound edges are thrashing around

            if ((isMoving || isFlopOrRagdoll) && __instance.bleedAmount > 0f)
            {
                // In vanilla Limb.Update, bleedAmount was clotted (reduced) by:
                // Time.deltaTime * body.bleedClottingSpeed * (hasShrapnel ? 0f : 1f) * WorldGeneration.GetRunSettingFloat("healingrate")
                float vanillaClotting = Time.deltaTime * body.bleedClottingSpeed * (__instance.hasShrapnel ? 0f : 1f) * WorldGeneration.GetRunSettingFloat("healingrate");

                // Partially or fully negate clotting during physical strain to simulate wound opening
                float clottingnegation = 0f;
                if (isFlopOrRagdoll)
                {
                    clottingnegation = 1.0f; // Flopping completely ruins clotting
                }
                else if (isMoving)
                {
                    clottingnegation = movement > 1.0f ? 0.70f : 0.25f; // Sprinting/running vs standard walking input
                }

                __instance.bleedAmount = Mathf.Clamp(__instance.bleedAmount + (vanillaClotting * clottingnegation), 0f, 100f - __instance.skinHealth);
            }
        }

        // ─── Impact Trauma, Splint Failures & Compound Fractures Postfix ─────
        [HarmonyPatch(typeof(Limb), "ImpactDamage")]
        [HarmonyPostfix]
        public static void Limb_ImpactDamage_Postfix(Limb __instance, float force, Vector2 dir)
        {
            if (!Plugin.Cfg.ModEnabled.Value || !Plugin.Cfg.CompoundFracturesEnabled.Value)
                return;

            var body = __instance.body;
            if (body == null || !body.alive)
                return;

            if (!IsLocalPlayer(body))
                return;

            // Heavy trauma impact threshold (fall damage / blunt injury)
            if (force > 8.0f)
            {
                if (__instance.broken)
                {
                    if (__instance.splinted)
                    {
                        // Heavy fall has a high chance to break/snap the splint!
                        if (UnityEngine.Random.value < Plugin.Cfg.SplintBreakChance.Value)
                        {
                            Plugin.Logger.LogWarning($"[Realism] Heavy impact force ({force:F1}f) snapped the splint on {__instance.fullName}!");
                            __instance.splinted = false;

                            var splintComp = __instance.GetComponent<SplintLimb>();
                            if (splintComp != null)
                            {
                                UnityEngine.Object.Destroy(splintComp);
                            }

                            // Snap sound
                            Sound.Play("dislocation", __instance.transform.position);
                        }
                    }
                    else
                    {
                        // Falling on an already broken bone without a splint turns it into a severe COMPOUND FRACTURE!
                        Plugin.Logger.LogWarning($"[Realism] Brutal fall force ({force:F1}f) caused bone in {__instance.fullName} to shatter and break skin! COMPOUND FRACTURE!");
                        __instance.bleedAmount = Mathf.Max(__instance.bleedAmount, 60f); // Massive hemorrhaging
                        __instance.muscleHealth = Mathf.Max(0f, __instance.muscleHealth - 45f); // Tearing muscle tissue
                        __instance.pain = 100f; // Absolute agony
                        body.shock = Mathf.Clamp(body.shock + 25f, 0f, 100f);
                        body.Scream();
                        _compoundFractures.Add(__instance.fullName);
                        Sound.Play("gore", __instance.transform.position);
                    }
                }
            }
        }

        // ─── Native Moodles Overlay Patch ──────────────────────────────────
        [HarmonyPatch(typeof(MoodleManager), "AddAllMoodles")]
        [HarmonyPostfix]
        public static void AddAllMoodles_Postfix(MoodleManager __instance)
        {
            if (!Plugin.Cfg.ModEnabled.Value)
                return;

            Body? body = PlayerCamera.main != null ? PlayerCamera.main.body : null;
            if (body == null || !body.alive)
                return;

            // 1. Stroke Risk
            if (Plugin.Cfg.CardioStrainEnabled.Value && body.strokeAmount > 0f)
            {
                int intensity = body.strokeAmount > 60f ? 3 : (body.strokeAmount > 30f ? 2 : 1);
                bool critical = body.strokeAmount > 60f;
                __instance.AddMoodle(intensity, "stroke", "Cardiovascular Stroke Risk", 
                    $"High heart rate has induced a stroke risk of {body.strokeAmount:F0}%. Rest immediately to lower strain!", critical);
            }

            // 2. Fractures & Compound Fractures
            if (body.limbs != null)
            {
                bool hasUnsplintedFracture = false;
                bool hasSplintedFracture = false;
                bool hasCompoundFracture = false;

                foreach (var limb in body.limbs)
                {
                    if (limb != null && limb.broken && !limb.dismembered)
                    {
                        if (_compoundFractures.Contains(limb.fullName))
                        {
                            hasCompoundFracture = true;
                        }
                        else if (limb.splinted)
                        {
                            hasSplintedFracture = true;
                        }
                        else
                        {
                            hasUnsplintedFracture = true;
                        }
                    }
                }

                if (hasCompoundFracture)
                {
                    __instance.AddMoodle(3, "amputation", "Compound Leg Fracture", 
                        "A fractured bone has ruptured your muscle and skin! Walking is causing catastrophic tissue damage, bleeding, and intense agony.", true);
                }
                else if (hasUnsplintedFracture)
                {
                    __instance.AddMoodle(3, "trauma", "Unsplinted Leg Fracture", 
                        "A weight-bearing leg bone is fractured and completely unsupported! Heavy movement causes immense pain, shock, and bleeding.", true);
                }
                else if (hasSplintedFracture)
                {
                    __instance.AddMoodle(0, "trauma", "Splinted Leg Fracture", 
                        "Your leg fracture is stabilized with a splint. Avoid high falls or jumping to prevent the splint from snapping.");
                }
            }

            // 3. Pain Shock
            if (Plugin.Cfg.PainShockFaintingEnabled.Value && body.averagePain > Plugin.Cfg.FaintPainThreshold.Value)
            {
                __instance.AddMoodle(3, "horrified", "Extreme Pain Shock", 
                    $"Average body pain is at a critical {body.averagePain:F0}%. High risk of sudden traumatic fainting and collapse!", true);
            }

            // 4. Fatigue / Micro-Naps
            if (Plugin.Cfg.FatigueNapsEnabled.Value && body.energy < Plugin.Cfg.MicroNapThreshold.Value)
            {
                int intensity = body.energy < 10f ? 3 : (body.energy < 25f ? 2 : 1);
                bool critical = body.energy < 10f;
                __instance.AddMoodle(intensity, "badsleep", "Extreme Fatigue", 
                    $"Energy levels are depleted to {body.energy:F0}%. Severe risk of sudden micro-nap blackouts!", critical);
            }

            // 5. Sepsis
            if (Plugin.Cfg.SepsisProgressionEnabled.Value && body.septicShock > 0f)
            {
                int intensity = body.septicShock > 50f ? 3 : (body.septicShock > 25f ? 2 : 1);
                bool critical = body.septicShock > 50f;
                __instance.AddMoodle(intensity, "dirty", "Systemic Septic Shock", 
                    $"Septic shock levels are at {body.septicShock:F0}% from wound contamination, inducing high systemic fever!", critical);
            }

            // 6. Concussions
            if (Plugin.Cfg.ConcussionsEnabled.Value && _concussionTimer > 0f)
            {
                int intensity = _concussionTimer > 15f ? 3 : (_concussionTimer > 5f ? 2 : 1);
                bool critical = _concussionTimer > 15f;
                __instance.AddMoodle(intensity, "braindamage", "Concussive Vertigo", 
                    $"Severe head trauma has dazed you ({_concussionTimer:F0}% concussion). Jumping or sprinting may cause sudden blackouts!", critical);
            }

            // 7. Electrolyte Cramps
            if (Plugin.Cfg.ElectrolyteCrampsEnabled.Value && body.thirst < Plugin.Cfg.CrampThreshold.Value)
            {
                __instance.AddMoodle(3, "underweight", "Dehydration Cramps", 
                    $"Thirst is at a critical {body.thirst:F0}%. Electrolyte collapse may cause agonizing cramps and force-drop items!", true);
            }

            // 8. Hypothermia & Shivering
            if (Plugin.Cfg.HypothermiaEnabled.Value && body.temperature < Plugin.Cfg.ShiverThreshold.Value)
            {
                int intensity = body.temperature < 30f ? 3 : (body.temperature < 32f ? 2 : 1);
                bool critical = body.temperature < 30f;
                __instance.AddMoodle(intensity, "wet", "Severe Hypothermia", 
                    $"Core temperature is {body.temperature:F1}°C. Shivering jitters, double stamina drain, and frostbite tissue damage active!", critical);
            }

            // 9. Respiratory Suffocation Panic
            if (Plugin.Cfg.SuffocationPanicEnabled.Value && body.bloodOxygen < Plugin.Cfg.OxygenPanicThreshold.Value)
            {
                int intensity = body.bloodOxygen < 40f ? 3 : (body.bloodOxygen < 60f ? 2 : 1);
                bool critical = body.bloodOxygen < 40f;
                __instance.AddMoodle(intensity, "oxygen", "Suffocation Panic", 
                    $"Oxygen levels are critically low ({body.bloodOxygen:F0}%). High adrenaline panic and asphyxiation blackouts imminent!", critical);
            }

            // 10. Drug Realism (Painkillers, Sleeping Pills, and Stimulants)
            if (Plugin.Cfg.DrugRealismEnabled.Value)
            {
                var pnk = body.GetComponent<Painkillers>();
                var pills = body.GetComponent<SleepingPills>();

                if (pnk != null)
                {
                    if (pnk.opiateAmount > 60f && Plugin.Cfg.DrugOverdoseEnabled.Value)
                    {
                        int intensity = pnk.opiateAmount > 150f ? 3 : (pnk.opiateAmount > 100f ? 2 : 1);
                        bool critical = pnk.opiateAmount > 120f;
                        __instance.AddMoodle(intensity, "brainhealth", "Painkiller Toxicity", 
                            $"Severe opiate overdose ({pnk.opiateAmount:F0} mg). Experiencing dangerous central nervous system and respiratory depression!", critical);
                    }
                    else if (pnk.actualOpiateReception < -10f && Plugin.Cfg.DrugWithdrawalEnabled.Value)
                    {
                        int intensity = pnk.actualOpiateReception < -30f ? 3 : (pnk.actualOpiateReception < -20f ? 2 : 1);
                        __instance.AddMoodle(intensity, "cough", "Opiate Withdrawal Chills", 
                            $"Agonizing opioid withdrawal ({pnk.actualOpiateReception:F0}%). Experiencing muscle tremors, cold sweats, and temperature dysregulation!", true);
                    }
                }

                // Mixed Drug Overdose
                if (pnk != null && pills != null && Plugin.Cfg.DrugOverdoseEnabled.Value)
                {
                    if (pnk.opiateAmount > 20f && pills.amount > 30f)
                    {
                        __instance.AddMoodle(3, "stroke", "Lethal Drug Interaction", 
                            "Mixed opioid and sedative painkillers in bloodstream! Severe synergistic CNS depression is causing rapid respiratory failure!", true);
                    }
                }

                // Stimulant Overdose & Crash
                if (Plugin.Cfg.DrugOverdoseEnabled.Value && (body.caffeinated > 150f || body.curAdrenaline > 75f))
                {
                    __instance.AddMoodle(3, "stroke", "Stimulant Over-Toxicity", 
                        "Dangerous level of stimulant consumption! High risk of sudden ventricular fibrillation or severe cardiac arrhythmia!", true);
                }
                else if (body.caffeinated > 0f && body.caffeinated < 35f)
                {
                    __instance.AddMoodle(1, "badsleep", "Stimulant Crash", 
                        $"Stimulants are wearing off (Caffeine: {body.caffeinated:F0}%). Suffering from severe energy drainage and sluggishness.");
                }
            }
        }
    }
}
