# Over-the-Top Realism Mod

An unofficial, high-fidelity realism expansion for **Casualties Unknown (Demo)**. 

---

> ### ⚠️ Unofficial Fan-Made Disclaimer
> This is an **unofficial, non-commercial, fan-made modification** created purely for educational, experimental, and personal entertainment purposes. 
> * **No Affiliation:** This project is not affiliated with, endorsed by, or in any way associated with the game's developer, **Orsoniks**, or any of the game's official publishers.
> * **Respecting Intellectual Property:** This mod does not distribute or bundle any original game binaries, graphics, textures, audio, or other copyrighted game assets. It functions strictly as an independent code patch injected at runtime via BepInEx.
> * **Developer First:** We deeply respect Orsoniks' work, creativity, and ownership. If you enjoy the demo, please support the official release of *Casualties Unknown*!

---

## 🩸 Overview
This mod transforms the medical and survival systems of *Casualties Unknown* into a brutal, physical, and highly realistic simulation. It expands the base game's simple stats (like cold, thirst, and pain) into tangible, real-time physical consequences. Every system is fully customizable or can be toggled off entirely in the configuration.

---

## ⚙️ Core Realism Features

### 🫀 1. Cardiovascular Stroke Risk
* **The Mechanic:** High heart rate (>150 BPM) from sprinting, stress, or adrenaline builds up cardiovascular stroke risk in real-time. 
* **The Threat:** At critically high stroke risk (>60%), the player has a chance of experiencing sudden ventricular fibrillation (cardiac arrest).
* **Moodle:** `Cardiovascular Stroke Risk` (uses the `"stroke"` sprite) warns you when your heart is under dangerous strain.

### 🦴 2. Traumatic Leg Fracture Strain
* **The Mechanic:** Walking or jumping with a broken leg bone causes intense localized pain spikes, shock, and further degrades limb muscle health.
* **The Threat:** Continuous movement on an unsupported fracture causes internal hemorrhaging within that limb.

### 💀 3. Compound Fractures
* **The Mechanic:** Walking on an *unsplinted* fractured leg has a dynamic, real-time chance of causing a **Compound Fracture**—simulating the bone tearing through muscle and skin.
* **The Threat:** Spawns sever immediate bleeding, sets pain to maximum, drops muscle health, and causes a gut-wrenching audio snap.
* **Moodle:** `Compound Leg Fracture` (uses the `"amputation"` sprite) signals a severe, bleeding bone emergency.

### 🪵 4. Splint Fragility from Impact
* **The Mechanic:** Splints are no longer permanent, invincible braces.
* **The Threat:** Taking a heavy fall or impact on a splinted leg has a high chance to snap the splint, breaking the brace and returning the bone to an unsupported or compound state.

### 🧠 5. Acute Pain Shock & Fainting
* **The Mechanic:** While vanilla slowly exhausts you over 20 seconds of pain, this mod adds *acute neurogenic shock*.
* **The Threat:** Taking massive, sudden trauma that spikes pain above 75% can cause the player to immediately faint, lose consciousness, and collapse into a ragdoll on the spot.
* **Moodle:** `Extreme Pain Shock` (uses the `"horrified"` sprite) signals when pain levels are high enough to cause sudden blackout.

### 💤 6. Sleep Deprivation Micro-Naps
* **The Mechanic:** In the base game, low energy slurs your movement but never forces you to sleep.
* **The Threat:** Attempting to walk, jump, or run when completely exhausted (Energy < 30%) carries an escalating chance of experiencing sudden, uncontrollable **micro-nap blackouts**, forcing you to collapse into sleep.
* **Moodle:** `Extreme Fatigue` (uses the `"badsleep"` sprite) warns when involuntary collapse is imminent.

### 🦠 7. Sepsis & Wound Necrosis
* **The Mechanic:** Festering infected wounds are now progressively destructive.
* **The Threat:** Localized infections dynamically consume and rot away both muscle health and skin health in that limb. Systemic sepsis builds progressively over time, triggering a high core-body fever.
* **Moodle:** `Systemic Septic Shock` (uses the `"dirty"` sprite) tracks systemic bloodstream toxicity.

### 💫 8. Concussions & Head Trauma Vertigo
* **The Mechanic:** Heavy head impacts or severe head pain accumulate a custom concussion level.
* **The Threat:** Concussions cause physical head/camera jitter, momentary reversed movement controls (vertigo), and severe jump-induced vertigo blackouts.
* **Moodle:** `Concussive Vertigo` (uses the `"braindamage"` sprite) tracks head trauma.

### ⚡ 9. Electrolyte Cramps & Item Drops
* **The Mechanic:** Severe dehydration (Thirst < 25%) triggers skeletal and muscular cramps.
* **The Threat:** Leg cramps trigger pain spikes and instantly trip/ragdoll you. Hand cramps trigger pain in the arms and force-drop whatever weapon or item you are holding in your active slot.
* **Moodle:** `Dehydration Cramps` (uses the `"underweight"` sprite) warns you before spasms strike.

### ❄️ 10. Hypothermia & Frostbite Necrosis
* **The Mechanic:** Core cold (< 35°C) doubles stamina drainage and introduces physical shivering forces to all limbs.
* **The Threat:** Extreme cold (< 32°C) introduces severe arm/aim sway. Freezing cold (< 30°C) triggers frostbite cell necrosis, decaying non-vital extremity muscle and skin health.
* **Moodle:** `Severe Hypothermia` (uses the `"wet"` sprite) tracks shivering and frostbite decay.

### 🫁 11. Suffocation Panic
* **The Mechanic:** Low blood oxygen (< 65%) triggers adrenaline surges, heart rate spikes, and visible hyperventilating bodily vibration.
* **The Threat:** Oxygen below 40% drains consciousness rapidly, leading to asphyxiation blackouts.
* **Moodle:** `Suffocation Panic` (uses the `"oxygen"` sprite) signals oxygen distress.

### 🩸 12. Hemodynamics & Rest-Dependent Clotting
* **The Mechanic:** High blood pressure (>120) physically pumps blood out of wounds faster.
* **The Threat:** Active movement, running, jumping, or flopping on the ground negates 75% to 100% of the body's natural wound clotting. Staying still or resting is now mandatory for wounds to close naturally.

---

## 💊 13. Drug Realism & Mixed Interactions

This system prevents players from using drugs as consequence-free, magical health cures. 

* **Opioid Respiratory Depression:** High painkiller levels (`opiateAmount > 60 mg`) depress the central nervous system, actively throttling your respiratory rate. Severe overdoses (`opiateAmount > 150 mg`) cause sudden blackouts and direct blood oxygen loss (asphyxiation) unless treated with Naloxone (which vanilla models via `antagonistAmount`).
  * *Moodle:* `Painkiller Toxicity` (uses the `"brainhealth"` sprite) tracks dosage safety.
* **Lethal Drug Interaction:** Mixing painkillers (`opiateAmount > 20 mg`) and sleeping pills (`amount > 30`) triggers **synergistic CNS depression**, multiplying respiratory failure by **3x** and causing a rapid, life-threatening blackout and suffocation.
  * *Moodle:* `Lethal Drug Interaction` (uses the `"stroke"` sprite) warns of mixed-pill arrest.
* **Opioid Withdrawal Tremors:** Going through withdrawal (`actualOpiateReception < -10%`) triggers severe shivering (cold sweats) and heavy arm aim shake (torque jolts), alongside temperature instability.
  * *Moodle:* `Opiate Withdrawal Chills` (uses the `"cough"` sprite) tracks detoxification.
* **Stimulant Hyper-Toxicity & Crash:** Abusing caffeine or adrenaline surges while under physical strain causes violent cardiac arrhythmia and stroke risk spikes. Once stimulants wear off, you suffer a severe energy crash.
  * *Moodle:* `Stimulant Over-Toxicity` (uses the `"stroke"` sprite) / `Stimulant Crash` (uses the `"badsleep"` sprite).

---

## 🔌 Installation & Customization

### Installation
1. Ensure you have **BepInEx** installed for *Casualties Unknown Demo*.
2. Copy `OverTheTopRealism.dll` into the `BepInEx/plugins/` directory.
3. Start the game once to generate the configuration file.

### Customization
Every feature can be customized or fully disabled in the generated config file:
📁 `BepInEx/config/OverTheTopRealism.cfg`

---

## 📄 License
Licensed under the [MIT License](LICENSE).
