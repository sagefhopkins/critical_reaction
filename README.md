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
- [ ] Being able to **receive an order**.
- [ ] Being able to **fill an order** and have the game recognize completion.

If all four of these are working end-to-end, the core loop of Critical Reaction is considered minimally complete.

---

## Project Status

- **Status:** Early Prototype / Pre-Alpha
- **MVP Progress:** 2 of 4 milestones complete

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
- **Campaign Progression** - 8-level Benzoic Acid campaign
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
  - [ ] Delivery Zone
- [ ] End-to-end order flow:
  - [ ] Receive order
  - [ ] Produce product
  - [ ] Deliver required quantity
  - [ ] Trigger win/lose states

### Phase 2 – Benzoic Acid Campaign

- [ ] Implement all 8 campaign levels (see outline below).
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

Campaign 1 walks the player through a simplified, game-friendly workflow for producing benzoic acid. Each level’s win condition is:  
**Deliver enough of the required product before the timer expires.**

1. **Level 1 – Brine Basics**  
   - **Goal:** Produce a required volume of saturated brine.  
   - **Focus:** Solid measurement, simple solution mixing, recognizing “saturation.”

2. **Level 2 – Standard Solution Setup**  
   - **Goal:** Prepare a standard solution used later for neutralization/titration.  
   - **Focus:** Liquid measurement, basic “strength check” at a titration bench.

3. **Level 3 – Reaction Mix Assembly**  
   - **Goal:** Prepare the reaction mixture that will lead to benzoic acid.  
   - **Focus:** Combining organic starting reagent, solvent, and oxidizer in the right order and ratio.

4. **Level 4 – Heat and Hold**  
   - **Goal:** Run the reaction under controlled temperature.  
   - **Focus:** Using a hot plate and cooling bath to keep the mixture in a safe temperature band for a set time.

5. **Level 5 – Wash and Separate**  
   - **Goal:** Wash the reaction mixture and collect the cleaned phase.  
   - **Focus:** Using the brine from Level 1, managing a simple separation step (top/bottom layer), and collecting enough “good” layer.

6. **Level 6 – Boil-Off Station**  
   - **Goal:** Concentrate the product by removing solvent.  
   - **Focus:** Operating a distillation rig, stopping at the right time to reach a target volume without burning the product.

7. **Level 7 – Crash the Crystals**  
   - **Goal:** Precipitate benzoic acid crystals.  
   - **Focus:** Acidification and staged cooling (room temperature → ice bath) to grow a required mass of crystals.

8. **Level 8 – Critical Run: Full Batch**
   - **Goal:** Run a streamlined version of the whole process under one big timer and deliver the final amount of benzoic acid.
   - **Focus:** Coordination across all stations, managing multiple batches, and timing everything so the team can meet the final order.

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
