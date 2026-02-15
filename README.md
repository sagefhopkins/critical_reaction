# Critical Reaction

![Critical Reaction Logo](logo.png)

## Introduction

**Critical Reaction** is a 1–4 player, top-down pixel-art co-op game where you work as lab technicians racing the clock to complete semi-realistic chemical workflows. Instead of fighting enemies, players juggle stations, timers, and shared resources to produce enough of a target compound (like benzoic acid) before the delivery deadline.

Think Overcooked, but instead of cooking you're running a chemistry lab.

---

## MVP (Minimum Viable Product)

For this capstone, the **MVP** is defined as:

- [x] Being able to **start the game**.
- [x] Being able to **load a level/map and spawn player characters**.
- [x] Being able to **receive an order**.
- [x] Being able to **fill an order** and have the game recognize completion.

If all four of these are working end-to-end, the core loop of Critical Reaction is considered minimally complete.

---
## Beta Tasks

For this capstone, the **Beta Taskks** are defined as :

### Week One - Core Systems and Data Models

- [x] Implement waste/invalid batch handling
- [x] Implement order definition model (RequiredProductId, RequiredQuantity, TimeLimit)
- [x] Build container item model (contents, volume, temperature, step stage)
- [x] Implement recipe step state machine (Empty -> FinalizedProduct/Invalid)
- [x] Implement multi-timer architecture (global order timer + per step timers)
- [x] Implement time-sensitive reaction window evaluation
- [x] Implement measurement accuracy tolerance system
- [x] Implement temperature control model (heat/cool rate + target range)

### Week Two - Station Implementations

- [x] Implement Measurement Bench Station (Raw -> measured reagent)
- [x] Implement Liquid Measurement Station (Raw -> measured volume)
- [x] Implement Hot Plate Station (temperature-based step progression)
- [x] Implement Cooling Bath Station (cooling + stablization)
- [x] Implement Wash/Sink Station (wash mixture to reduce impurity)
- [x] Implement Distillation Rig Station (solvent removal -> concentrated product)
- [x] Implement Titration Bench Station (validate strength/purity)
- [x] Implement Storage Rack (Shared team inventory for intermediates)
- [x] Implement Input Shelves (Unlimited reagents for Beta)
- [x] Implement Waste Bin (discard invalid batches; scoring impact hook)
- [x] Implement Spill Zone Hazard (slow + cleanup; scoring impact hook)

### Week Three - Player Experience, UI and Level Design

- [ ] Implement contextual button prompts for Interactions
- [ ] Implement player indicators (outline + name tag per technician)
- [ ] Implement station/container readout panel (temp, step timer, fill level, ready state)
- [x] Implement quick alert strip
- [x] Implement Campaign Map screen
- [x] Implement Options Menu (Audio, Display)
- [ ] Build Level 1 Layout (stations + short looping pathing)
- [ ] Implement Level 1 Parameters (4:00 timer, 250mL target)
- [ ] Implement brine production interactions (measure + mix + saturation indicator)
- [ ] Create reusable "level template" pipeline for future campaign levels

### Week Four - Campaign Progression and Multiplayer

- [ ] Implement campaign linear unlock rules
- [ ] Implement medal/grade scoring (Stars / Bronze/Silver/Gold)
- [ ] Implement save triggers (post-level + return to campaign map)
- [ ] Implement saved data model (progress, best score/time, tutorial flags, settings)
- [ ] Enforce "no mid-level save" ----------
- [ ] Implement SQLite persistence layer (profiles + campaign progress)
- [ ] Implement simultaneous station use rules + locking
- [ ] Implement co-op role-emergence balancing instrumentation
- [ ] Bugfix : Blank screen when backing out of lobby to main menu and reopening Coop menu
---

## Project Status

- **Status:** Beta implementations
- **MVP Progress:** 4 of 4 milestones complete
- **Beta Progress:** 10 of 36 milestones complete

Core systems work: movement, item carrying, storage racks, and the Scale workstation. Networking uses Unity Netcode with server-authoritative sync. Order/delivery system is next.

---

## What's Working

### Player Systems
- WASD/controller movement with networked animations
- Carry one item at a time
- Context-based interaction prompts
- Rebindable controls in Options

### Workstations
- **Scale** - Tare, unit switching, physics-based particles in beakers. Drag scoops around and shake to dump. This one took forever to get right.
- **Storage Rack** - 9 slots, networked, pick up and drop off items

### Networking & Co-op
- Local co-op for 1–4 players
- Server-authoritative sync via Unity Netcode
- Lobby with join/ready flow

### UI & Menus
- Main menu with Options and Credits
- Workstation/storage interaction menus
- Control rebinding

## Planned Features

- **Order/Delivery System** - Receive and complete production orders
- **Campaign Progression** - 6-level Benzoic Acid campaign
- **Additional Stations** - Distillation, cooling bath, wash/separation
- **Timer System** - Global and per-step timers
- **Scoring/Ranking** - Bronze/Silver/Gold completion ranks
- **Tutorial System** - Guided introduction to mechanics

Stretch goals:
- Difficulty modes
- Cosmetic unlocks (lab coats, goggles, décor)

---

## Technologies

- **Engine:** [Unity](https://unity.com/) (2D)
- **Language:** C#
- **Networking:** Unity Netcode for GameObjects
- **Target Platforms:**
  - Windows, Linux, Mac PC (Primary)
  - Console support is possible in future
- **Version Control:** Git
- **Project Management:** Jira


---

## Project Structure

Current Unity project layout:

```text
CriticalReaction/
  Assets/
    Art/                 # Sprites and environment assets
    Prefabs/
      Player.prefab      # Player character with all components
      Items/             # LabItem scriptable objects
      Recipes/           # Recipe scriptable objects
      ChemicalParticles/ # Particle data and prefabs
    Scenes/
      Main Menu.unity    # Title screen
      CoopGame.unity     # Main gameplay scene
    Scripts/
      Gameplay/
        Player/          # PlayerController, PlayerCarry, PlayerInteractor
        Items/           # LabItem, Recipe definitions
        Workstations/
          Workstation/   # Base workstation framework
          Scale/         # Measurement station with particle physics
          Burner/        # Heating station
          StorageRack/   # Item storage system
        Interactions/    # InteractionMenus coordinator
        Coop/            # CoopGameManager
      UX/
        Options/         # InputSettings, RebindEditor
        CoopMenu/        # CoopFlow, CoopConnectMenu, LobbyManager
        MainMenu/        # Title screen logic
        Campaign/        # Campaign progression
        Net/             # Network relay and bootstrap
  ProjectSettings/
  Packages/
  README
```

---

## Installation (End User)

Once builds are available:

1. **Download the build** from the releases page.

2. **Extract** the zip wherever you want.

3. **Run** `CriticalReaction.exe`.

4. **Basic usage**
   - From the main menu, choose **Start Campaign**.
   - On the player setup screen, have each player join (keyboard or controller).
   - Play through the Benzoic Acid campaign levels in order.

5. **Controls (rebindable via Options menu)**
   - Keyboard: `WASD` to move, `E` to interact with workstations/storage, `Esc` to pause.
   - Controller support available.

No Unity install needed to play.

---

## Development Setup

### Prerequisites

- **Unity** (2022.x LTS via Unity Hub)
- **Git**
- **IDE** - Visual Studio, Rider, or VS Code

### Getting the Project

```bash
# Clone the repository
git clone https://github.com/sagefhopkins/critical_reaction.git
cd critical-reaction
```

### Opening in Unity

1. Open **Unity Hub**.
2. Click **Add project** and select the `critical-reaction` folder.
3. Open the project with the correct Unity version.

### First Build / Play

1. In Unity, open the main menu scene, for example:
   `Assets/Scenes/Menus/MainMenu.unity`
2. Press **Play** in the Unity editor:
   - Verify that:
     - The game loads.
     - The main menu appears.
     - You can enter the player setup, campaign map, and load at least one level.
3. To create a standalone build:
   - Go to **File → Build Settings…**
   - Add the relevant scenes (Main Menu, Player Setup, Campaign Map, at least one Level).
   - Choose **PC, Mac & Linux Standalone** → **Build**.
   - Select an output folder and wait for the build to complete.

---

## Roadmap

### Phase 1 – MVP / Core Loop

- [x] Main menu → player setup flow.
- [x] Implement player spawn & local co-op join/ready logic.
- [x] Implement player movement and interaction system.
- [x] Implement item carry system.
- [x] Implement input rebinding.
- [x] Implement basic stations:
  - [x] Scale (Measurement Bench with particle physics)
  - [ ] Burner (Heating/Mixing station)
  - [x] Storage Rack
  - [x] Delivery Zone
- [x] End-to-end order flow:
  - [x] Receive order
  - [x] Produce product
  - [x] Deliver required quantity
  - [x] Trigger win/lose states

### Phase 2 – Benzoic Acid Campaign

- [ ] Implement all 6 campaign levels (see outline below).
- [ ] Add time limits and quantity requirements per level.
- [ ] Add basic scoring / rank system (Bronze/Silver/Gold).
- [ ] Add tutorial hints and level briefings.

### Phase 3 – Additional Stations

- [ ] Distillation rig
- [ ] Cooling bath
- [ ] Wash/separation station
- [ ] Sink

### Phase 4 – Polish & Extras

- [ ] Improve pixel art (characters, lab equipment, backgrounds).
- [ ] Add sound effects and simple music loops.
- [ ] Add simple cosmetic unlocks (lab coats, goggles, décor).

### Phase 5 – Stretch Goals

- [ ] Difficulty settings (casual / normal / strict).
- [ ] Extended campaigns with other products.
- [ ] Online co-op support.

---

## Benzoic Acid Campaign Outline

Campaign 1 walks the player through a simplified, game-friendly workflow for producing benzoic acid via oxidation of toluene with potassium permanganate. Levels are consolidated so that 1–4 players always have enough parallel work to stay busy.

**Level 1 – Prep & Standard Solutions**
   - **Goal:** Prepare all reagents and a sodium hydroxide standard solution for later steps.
   - **Stations:** Scale, Graduated Cylinder, Input Shelves, Storage Rack, Delivery Chute
   - **Chemistry:** Weigh NaOH pellets on the scale to a target mass. Measure water to a target volume in the graduated cylinder. Dissolve NaOH in water to produce a standard solution (known concentration). Measure out toluene (liquid starting material) and potassium permanganate (solid oxidizing agent). Stage everything on storage racks.
   - **Co-op split:** Players divide across scale, graduated cylinder, and storage rack shuttling.
   - **Focus:** Accurate measurement, station familiarity, and team coordination basics.

**Level 2 – Reaction Assembly & Heat**
   - **Goal:** Assemble the reaction mixture and run the oxidation at a controlled temperature.
   - **Stations:** Hot Plate, Cooling Bath, Input Shelves, Storage Rack, Delivery Chute
   - **Chemistry:** Combine toluene + potassium permanganate + solvent into the reaction flask in the correct order (wrong sequence reduces yield or causes failure). Heat on the hot plate to a target temperature band (~100 C reflux). Hold for a required duration — too hot causes boil-over, too cold stalls the reaction. Add concentrated sulfuric acid at the correct moment during the hold. Use cooling bath if temperature spikes above the safe range.
   - **Co-op split:** Some players assemble new batches while others monitor temperature and manage the timed acid addition.
   - **Focus:** Correct reagent sequencing, temperature control, and reaction timing.

**Level 3 – Distill & Separate**
   - **Goal:** Distill the reaction product and purify via liquid–liquid separation.
   - **Stations:** Distillation Rig, Wash/Separation Station, Storage Rack, Waste Bin, Delivery Chute
   - **Chemistry:** Load completed reaction mixture into the distillation rig. Vacuum distill under reduced pressure — collect the fraction in the target temperature range (wrong cut = fail). Take the distillate to the separatory funnel. Add concentrated NaOH solution to convert benzoic acid into sodium benzoate (moves into the water layer). Shake and vent the funnel (pressure builds, venting is a timed action). Let layers settle — organic on top, aqueous on bottom. Drain and keep the aqueous layer (sodium benzoate), discard organic layer to waste. Repeat wash cycle 3 times per batch.
   - **Co-op split:** One pair runs distillation, another runs the repetitive wash/separate cycles.
   - **Focus:** Correct fraction collection, layer identification, safe venting, and pipelining.

**Level 4 – Acidify & Filter**
   - **Goal:** Convert sodium benzoate back to benzoic acid and isolate the solid product.
   - **Stations:** Titration Bench, Vacuum Filtration, Wash/Sink, Storage Rack, Delivery Chute
   - **Chemistry:** Take aqueous sodium benzoate solution. Slowly add HCl at the titration bench while monitoring pH. When pH drops below 2, benzoic acid precipitates as a white solid ("crashing out"). Adding too fast overshoots and degrades purity; too slow wastes time. Set up vacuum filtration. Pour the suspension through the filter to collect solid benzoic acid. Wash the collected solid with cold water to remove leftover salts and acid.
   - **Co-op split:** Some players acidify batches at titration, others run filtration and washing.
   - **Focus:** Controlled acid addition, pH endpoint timing, and clean filtration technique.

**Level 5 – Purify & Dry**
   - **Goal:** Recrystallize for purity and dry to final product spec.
   - **Stations:** Hot Plate, Cooling Bath, Vacuum Filtration, Drying Oven, Storage Rack, Delivery Chute
   - **Chemistry:** Dissolve crude benzoic acid crystals in a hot water/ethanol mixture on the hot plate (must fully dissolve). Cool slowly at room temperature to grow large, pure crystals (rushing gives small impure crystals). Chill in ice/cooling bath to complete crystallization. Filter again to collect purified crystals. Transfer to the drying oven at low temperature. Dry until stable mass (overheating degrades the product).
   - **Co-op split:** Players manage multiple batches at different stages — dissolving, cooling, filtering, drying.
   - **Focus:** Controlled cooling for crystal growth, avoiding overheating, and hitting yield target.

**Level 6 – Critical Run: Full Batch**
   - **Goal:** Run a streamlined full process under one master timer and deliver the required amount of benzoic acid.
   - **Stations:** All — Scale, Graduated Cylinder, Hot Plate, Cooling Bath, Distillation Rig, Wash/Separation, Titration Bench, Vacuum Filtration, Wash/Sink, Drying Oven, Input Shelves, Storage Rack, Waste Bin, Delivery Chute
   - **Chemistry:** All steps — measure, react, distill, separate, acidify, filter, recrystallize, dry, deliver.
   - **Co-op split:** All stations active. Stagger batches through the pipeline to maximize throughput.
   - **Focus:** Station-to-station coordination, parallel tasking, timing dependencies, and meeting the final delivery quantity.

---

## Known Issues

- Art and animations are placeholder
- Only CoopGame scene works right now, no campaign levels yet
- No order/delivery or timer system yet
- Scale is the only fully functional workstation

---

## Support

Open an issue if something's broken or you have ideas.

---

## Contributors

- **Sage Hopkins** – Lead Developer
- **Tia Moss** – Programming & Systems
- **Noah Hopkins** - Chemist
