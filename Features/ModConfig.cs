using BepInEx.Configuration;

namespace OverTheTopRealism.Features
{
    public class ModConfig
    {
        // ─── General ────────────────────────────────────────────────────────
        public ConfigEntry<bool> ModEnabled { get; }

        // ─── Cardiovascular ──────────────────────────────────────────────────
        public ConfigEntry<bool> CardioStrainEnabled { get; }
        public ConfigEntry<float> StrokeRiskThreshold { get; }
        public ConfigEntry<float> StrokeBuildRate { get; }

        // ─── Trauma / Fractures ──────────────────────────────────────────────
        public ConfigEntry<bool> LegFractureStrainEnabled { get; }
        public ConfigEntry<float> FracturePainSpike { get; }
        public ConfigEntry<float> FractureShockSpike { get; }

        // ─── Hemodynamics / Clotting ─────────────────────────────────────────
        public ConfigEntry<bool> ClottingHemodynamicsEnabled { get; }
        public ConfigEntry<float> BloodPressureBleedMult { get; }

        // ─── Fatigue ─────────────────────────────────────────────────────────
        public ConfigEntry<bool> FatigueNapsEnabled { get; }
        public ConfigEntry<float> MicroNapThreshold { get; }

        // ─── Pain Shock / Fainting ───────────────────────────────────────────
        public ConfigEntry<bool> PainShockFaintingEnabled { get; }
        public ConfigEntry<float> FaintPainThreshold { get; }

        // ─── Infection / Sepsis ──────────────────────────────────────────────
        public ConfigEntry<bool> SepsisProgressionEnabled { get; }
        public ConfigEntry<float> SepsisBuildRate { get; }

        // ─── Concussions & Head Trauma (New) ─────────────────────────────────
        public ConfigEntry<bool> ConcussionsEnabled { get; }
        public ConfigEntry<float> ConcussionSensitivity { get; }

        // ─── Electrolyte Cramps (New) ────────────────────────────────────────
        public ConfigEntry<bool> ElectrolyteCrampsEnabled { get; }
        public ConfigEntry<float> CrampThreshold { get; }

        // ─── Hypothermia & Shivering (New) ───────────────────────────────────
        public ConfigEntry<bool> HypothermiaEnabled { get; }
        public ConfigEntry<float> ShiverThreshold { get; }

        // ─── Suffocation Panic (New) ─────────────────────────────────────────
        public ConfigEntry<bool> SuffocationPanicEnabled { get; }
        public ConfigEntry<float> OxygenPanicThreshold { get; }

        // ─── Splint Failures & Compound Fractures (New) ──────────────────────
        public ConfigEntry<bool> CompoundFracturesEnabled { get; }
        public ConfigEntry<float> SplintBreakChance { get; }

        // ─── Drug Realism (New) ──────────────────────────────────────────────
        public ConfigEntry<bool> DrugRealismEnabled { get; }
        public ConfigEntry<bool> DrugWithdrawalEnabled { get; }
        public ConfigEntry<bool> DrugOverdoseEnabled { get; }

        public ModConfig(ConfigFile cfg)
        {
            // ── General ──
            ModEnabled = cfg.Bind(
                "0. General", "ModEnabled", true,
                "Master switch to enable or disable the Over-the-top Realism mod entirely.");

            // ── Cardiovascular ──
            CardioStrainEnabled = cfg.Bind(
                "1. Cardiovascular Strain", "CardioStrainEnabled", true,
                "If enabled, high heart rates (>150 bpm) and blood pressure trigger progressive stroke risk, potentially leading to cardiac arrest.");

            StrokeRiskThreshold = cfg.Bind(
                "1. Cardiovascular Strain", "StrokeRiskThreshold", 150f,
                "The heart rate (bpm) above which stroke risk starts accumulating.");

            StrokeBuildRate = cfg.Bind(
                "1. Cardiovascular Strain", "StrokeBuildRate", 0.05f,
                "Rate at which stroke damage builds up per second when heart rate is critically high.");

            // ── Trauma ──
            LegFractureStrainEnabled = cfg.Bind(
                "2. Trauma & Fractures", "LegFractureStrainEnabled", true,
                "If enabled, walking or landing with broken leg bones causes traumatic sharp pain spikes, shock, and further muscle damage.");

            FracturePainSpike = cfg.Bind(
                "2. Trauma & Fractures", "FracturePainSpike", 1.8f,
                "Pain spike value added to limbs when stepping on broken legs.");

            FractureShockSpike = cfg.Bind(
                "2. Trauma & Fractures", "FractureShockSpike", 2.2f,
                "Physical shock added per step when leg bones are fractured.");

            // ── Hemodynamics ──
            ClottingHemodynamicsEnabled = cfg.Bind(
                "3. Hemodynamics & Clotting", "ClottingHemodynamicsEnabled", true,
                "If enabled, high blood pressure pushes blood out of wounds faster. Active movement also prevents wound clotting.");

            BloodPressureBleedMult = cfg.Bind(
                "3. Hemodynamics & Clotting", "BloodPressureBleedMult", 1.25f,
                "Multiplier for wound bleeding rate at high blood pressures.");

            // ── Fatigue ──
            FatigueNapsEnabled = cfg.Bind(
                "4. Fatigue & Exhaustion", "FatigueNapsEnabled", true,
                "If enabled, moving when completely exhausted (<15% energy) has a chance to trigger involuntary micro-naps (blackouts).");

            MicroNapThreshold = cfg.Bind(
                "4. Fatigue & Exhaustion", "MicroNapThreshold", 15f,
                "Energy level percentage below which micro-naps can occur.");

            // ── Pain Shock ──
            PainShockFaintingEnabled = cfg.Bind(
                "5. Pain & Fainting", "PainShockFaintingEnabled", true,
                "If enabled, extremely high pain (>80%) will trigger a traumatic pain-shock response and cause sudden fainting.");

            FaintPainThreshold = cfg.Bind(
                "5. Pain & Fainting", "FaintPainThreshold", 80f,
                "Average pain percentage above which the player will pass out from pain shock.");

            // ── Sepsis ──
            SepsisProgressionEnabled = cfg.Bind(
                "6. Sepsis & Contamination", "SepsisProgressionEnabled", true,
                "If enabled, open wounds left untreated will progressively fester and trigger blood infection, causing systemic septic shock.");

            SepsisBuildRate = cfg.Bind(
                "6. Sepsis & Contamination", "SepsisBuildRate", 0.015f,
                "Multiplier for septic shock build-up over time due to infected wounds.");

            // ── Concussions (New) ──
            ConcussionsEnabled = cfg.Bind(
                "7. Concussions & Head Trauma", "ConcussionsEnabled", true,
                "If enabled, blunt hits or severe pain to the head can cause concussions, blurring vision and introducing temporary blackouts.");

            ConcussionSensitivity = cfg.Bind(
                "7. Concussions & Head Trauma", "ConcussionSensitivity", 1.0f,
                "Multiplier adjusting how easily concussions build up from head injuries.");

            // ── Electrolyte Cramps (New) ──
            ElectrolyteCrampsEnabled = cfg.Bind(
                "8. Electrolyte Cramps", "ElectrolyteCrampsEnabled", true,
                "If enabled, extreme dehydration causes skeletal cramps, temporarily disabling legs or forcing you to drop items.");

            CrampThreshold = cfg.Bind(
                "8. Electrolyte Cramps", "CrampThreshold", 15f,
                "Thirst percentage below which muscle and hand cramps start occurring.");

            // ── Hypothermia (New) ──
            HypothermiaEnabled = cfg.Bind(
                "9. Hypothermia & Shivering", "HypothermiaEnabled", true,
                "If enabled, low core temperatures cause severe shivering (doubling stamina drain) and numbness (increasing aiming/throwing sway).");

            ShiverThreshold = cfg.Bind(
                "9. Hypothermia & Shivering", "ShiverThreshold", 35.0f,
                "Core body temperature in Celsius below which shivering and stamina drain penalties begin.");

            // ── Suffocation Panic (New) ──
            SuffocationPanicEnabled = cfg.Bind(
                "10. Suffocation Panic", "SuffocationPanicEnabled", true,
                "If enabled, suffocation or drowning causes adrenaline surges, heart rate spikes, visual disorientation, and rapid blackouts.");

            OxygenPanicThreshold = cfg.Bind(
                "10. Suffocation Panic", "OxygenPanicThreshold", 70f,
                "Blood oxygen percentage below which respiratory suffocation panic is triggered.");

            // ── Splints & Compound Fractures (New) ──
            CompoundFracturesEnabled = cfg.Bind(
                "11. Splints & Compound Fractures", "CompoundFracturesEnabled", true,
                "If enabled, heavy falls can snap limb splints. Walking on unsplinted fractures can convert them into compound fractures.");

            SplintBreakChance = cfg.Bind(
                "11. Splints & Compound Fractures", "SplintBreakChance", 0.40f,
                "Probability (0.0 to 1.0) that a heavy fall breaks a limb's splint.");

            // ── Drug Realism (New) ──
            DrugRealismEnabled = cfg.Bind(
                "12. Drug Realism", "DrugRealismEnabled", true,
                "If enabled, painkillers and sleeping pills have realistic side effects, interactive respiratory depression, and severe withdrawal tremors.");

            DrugWithdrawalEnabled = cfg.Bind(
                "12. Drug Realism", "DrugWithdrawalEnabled", true,
                "If enabled, severe opiate withdrawal causes cold sweat fever/chills and uncontrollable muscle tremors.");

            DrugOverdoseEnabled = cfg.Bind(
                "12. Drug Realism", "DrugOverdoseEnabled", true,
                "If enabled, painkiller overdoses can depress respiration leading to asphyxiation, or cause sudden cardiac arrest.");
        }
    }
}
