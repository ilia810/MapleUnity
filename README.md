# MapleUnity

An in-progress Unity/C# port of the HeavenClient C++ MapleStory client. HeavenClient is the source implementation for gameplay, asset interpretation, and rendering behavior.

For drawing and asset editing on the MacBook while the Windows PC handles code,
see [the MacBook asset workflow](MACBOOK_ASSETS.md), including the reference images,
editable asset locations and branch handoff steps.

The current milestone includes local Henesys exploration and a hunting map with animated ground monsters, basic attacks, player contact damage, hit/death feedback, EXP/leveling, revival, real recovery items, melee weapon stances, source weapon trails, delayed impacts, attack movement rules, source melee stats, timed stat potions, a real skill book, weapon mastery, Sword Booster, Power Strike, Slash Blast, Energy Bolt, Magic Claw, bows/crossbows/claws/guns with ammunition and first-job ranged skills, travelling projectiles, basic equipment, and offline respawns. Character and ground-monster movement are checked against HeavenClient C++ traces. Walking, jumping, falling, platform drops, and ladder/rope climbing run on the source 8 ms clock. Player and NPC art interleaves with scenery by source foothold layer. Overlapping footholds retain source contact/layer rules; falling past the map returns the player to a real spawn foothold. Swimming and facial animation are implemented. Local loot, a meso wallet, Luna’s shop, manual character saving, AP allocation, first-job advancement and earned first-job skills now form a playable offline loop. Advanced movement and full equipment/job progression remain unfinished. Online play is deferred. See [PROJECT_STATUS.md](PROJECT_STATUS.md) for the source mapping, validation results, and remaining work.

## Keyboard settings

**Short Cut** opens the original NX keyboard in its own window. Every key can be reassigned: drag a shortcut onto another key to move it, or onto a bound key to swap. Select a key and click an action below to assign it; drag learned skills or supported consumables from their windows too. Right-click clears one key, CLEAR ALL clears the whole keyboard, and DEFAULT restores the controls. Movement, Attack/Jump, expressions, window commands and NPC/shop actions use the edited assignments.

Number keys 1–8 initially follow the eight HUD keys until individually reassigned. Changes apply immediately and survive map travel. Use **Menu → Save progress** to preserve the complete keyboard for another session; versions 1–4 migrate when loaded. The key names elsewhere in this README describe the default layout. [Implementation and migration details](Tools/Research/Classic-full-keyboard-source.md).

## Run the local scene

1. Open this project with **Unity 2023.2.20f1** (the version in `ProjectSettings/ProjectVersion.txt`).
2. Make the matching `Character.nx`, `Map.nx`, `String.nx`, `Npc.nx`, `Mob.nx`, `Item.nx`, and `Skill.nx` files available. The loader checks:
   - An explicitly configured loader path.
   - The `MAPLE_NX_PATH` environment variable.
   - `Assets/StreamingAssets/NX`.
   - The original development location, `C:\HeavenClient\MapleStory-Client\nx`, when it exists.
3. If setting `MAPLE_NX_PATH`, restart Unity/Hub so the editor inherits it.
4. Choose **MapleUnity > Play Henesys**, or open **`Assets/henesys.unity`**, let Unity finish importing, and press Play.
5. Select the **Game** tab. For native pixel size, use a **1024 x 768** Game-view resolution and **Scale 1x**. Maximize the Game view if needed; a small preview scaled to fit will reduce detail.

To generate another map, use **MapleUnity > Generate Map Scene**, select its ID, and click **Generate Scene**. Save the scene if desired, then press Play. The generated GameManager stores the selected starting map. Existing generated scenes with missing tile/object/background sprites are repaired from NX when Play starts. Do not judge runtime rendering from the zoomed Scene view.

To try combat, stop Play and choose **MapleUnity > Play Hunting Ground**. This generates map **100010000** with its **49 authored monsters** and enters Play. You can also generate that map ID through the map generator. Walk up to a monster, face it, and press **Z or Left Ctrl**. Basic attacks hit the closest target in front; health bars appear on damage, monsters react and play their death animation, and ordinary local spawns return after seven seconds. Henesys town itself has no monster spawns.

Touching an attacking monster now reduces HP and knocks the player away, followed by a two-second hit grace period. HP and EXP bars reflect the character state. Basic-attack kills award the source monster EXP (Blue Snail: 4); level 1 requires 15 EXP, and excess carries into the next level. Leveling restores current HP/MP. After defeat, click **Revive at a safe spawn** to return to the map’s authored return town, or a safe spawn in the current map when no return town is available. Local revival restores HP/MP and preserves EXP.

Press **P**, or open **C → Character progression**, to spend earned AP on STR, DEX, INT or LUK and choose your first job. A fresh character earns **5 AP per level**. Magician is available at **level 8**; Swordsman, Bowman, Thief and Pirate at **level 10**. Choosing a job grants its starter weapon into the bag, **100 ammunition** for ranged jobs, and **1 SP**. Equip the weapon through **I**; its normal attribute requirements still apply. The first-job choice is once per character and requires space for the complete starter bundle.

Each naturally earned level after advancement grants **3 SP**. Open **K**, select the first-job tier and a supported skill, then click **+1 (1 SP)** to learn or improve it. Required skills must be learned first, and level caps apply. Assign learned active skills to quickslots **1–8**. Unsupported skills remain visible with an explanation and preserve your SP. Points are saved, and SP remains associated with its job family when switching practice kits. Direct level jumps from a practice kit grant no AP/SP.

These **5 AP / 1 initial SP / 3 later SP** rewards and the menu-based first-job choice are explicit offline policies: HeavenClient receives progression decisions from a server. The original NX data supplies skill levels, prerequisites and equipment requirements. Natural maximum HP/MP growth, HP/MP AP spending, beginner skills and later advancements remain unfinished. Existing version-1 saves retain their character and learned skills, start with zero unspent AP/SP, and earn points at their next natural level; previous levels are not credited retroactively.

Press **I** (or click **Inventory [I]**) to open the inventory. Click **Practice supplies** once per character (the claim is saved) to receive real Red/Orange/Blue Potions, Elixirs, a White Undershirt, Blue Jean Shorts, Red Rubber Boots, a Sword, a Wooden Baseball Bat, a Basic Polearm, and a Stolen Fence shield (requires level 5), Warrior Potions, and Speed Potions. This is an optional practice kit, not a monster drop table. In **Bag**, select an item and click **Use** or **Equip**. In **Equipped**, select an item and click **Remove**. The panel pages larger bags and scales to smaller Game views.

Orange Potion restores **150 HP**; Blue Potion restores **100 MP**; Elixir restores **50% of maximum HP and MP**. Recovery only consumes an item when it restores something. Equipment moves between bag and worn slots, applies its NX stats, validates requirements, and updates the character immediately, including separate shirt chest/arm pieces. Ordinary clothing, hats, gloves, capes, shields, one- and two-handed swords/axes/maces, daggers, wands/staves, spears, and polearms are available. Bows, crossbows, claws and guns are also enabled. Knuckles remain unsupported.

For ranged combat, open **Inventory → Ranged practice**, choose **Bow**, **Crossbow**, **Claw** or **Gun**, then click **Equip** on the selected weapon. Each kit grants its weapon, **100 matching ammunition**, **10 Blue Potions**, and level-20 skills. Supplies are granted once per kit per character (claims persist in saves). It sets the matching job and raises your level only as needed (10–12), preserving earned EXP and attributes. Return to the selector to switch jobs between kits; it does not refill claimed supplies. Starting Play without loading a save starts a fresh character and fresh kits.

Face a monster in the Hunting Ground and press **Left Ctrl or Z**. Standing ranged attacks select the closest target within the source 400-pixel reach, then launch an arrow, star or bullet after the weapon's windup. Damage arrives with the projectile. Ammunition is selected automatically from the first compatible bag slot and adds its source attack bonus; the combat stats window (C) shows the remaining count. Local play spends one round per accepted shot, even with no target, and blocks attacks when empty. The last round retains its damage bonus. This consumption policy supplies the server's role for offline practice; connected inventory counts remain server-owned. Down + Attack retains the source close-range prone action and reduced damage. Additional ranged skills and ammunition recharging remain pending.

Ranged practice also fills the hotbar. **Bow/crossbow: 1 = Arrow Blow, 2 = Double Shot. Claw: 1 = Lucky Seven. Gun: 1 = Double Shot.** Press **K** to inspect the learned skills, their costs and requirements. Arrow Blow costs **14 MP + 1 arrow** for one 260% hit. Archer Double Shot costs **16 MP + 2 arrows** for two 130% hits. Lucky Seven costs **16 MP + 2 stars** for two 150% hits. Gun Double Shot costs **7 MP + 2 bullets** for two 110% hits with staggered launches. Stand up before casting; an underfilled active ammo stack cannot borrow rounds from another stack. Empty casts still pay the listed costs. Use Blue Potions from your bag to restore MP.

These four skills extend HeavenClient's incomplete attack registry using its generic skill rules and the original NX art. Lucky Seven uses those generic damage bounds; the source has no separate retail Lucky Seven formula. Gun Double Shot also preserves the source percentage-range calculation: at level 20 its 380% factor extends the 400-pixel base reach to **1,520 pixels**. Arrow Blow and gun Double Shot use their skill projectile art; archer Double Shot and Lucky Seven use the equipped ammunition art. Crossbows use Double Shot's two-handed hit effect.

Try the **Wooden Baseball Bat** and **Basic Polearm** in the practice kit. The bat uses a two-handed standing pose and a one-handed walking pose; the polearm uses two-handed standing and walking poses. Attacks choose the source swing/stab pool, use NX frame delays and weapon speed, and finish before a held attack can repeat. With a weapon equipped, Down + Attack uses the prone stab. Equipment changes wait for the swing to finish. Equipping a two-handed weapon returns the shield to the bag; equipping a shield returns a conflicting weapon.

Equipped melee attacks now draw their NX afterimages and use each stance’s authored reach against the monster’s current animation bounds. The closest target is chosen when the swing starts; damage, hit reaction, death and EXP arrive after the authored windup. A moving target stays selected, while death, travel or target removal cancels a pending impact. Compare a Sword and Basic Polearm in the Hunting Ground to see their different reach and trails.

During a swing, steering, facing and stance stay locked while momentum, gravity, landings and contact knockback continue. Hold a direction to resume walking as the swing ends. Ordinary jumps pressed during the swing are discarded: release and press Jump again afterward. A prone stab retains the source crouching drop-through action. Holding Up waits until the swing finishes before entering a nearby ladder or portal. Try walking right, attacking, then holding Left and Jump to check the lock and return to movement.

Equipped melee damage now uses HeavenClient’s job/weapon stat multipliers, equipment attack, derived accuracy, level differences and monster defense. A hit can miss or critically strike (the source default is 5%, with 1.5× damage); floating numbers distinguish all three outcomes at the scheduled impact. Prone attacks and ordinary wand/staff strikes use the source reduced damage range. The local character starts with no extra flat weapon attack or accuracy bonus; the existing 15-point starting attributes remain local practice settings. Unarmed practice combat and explicit diagnostic damage overrides retain their previous behavior.

Use a **Warrior Potion** from the practice supplies for **+5 weapon attack for 180 seconds**, or a **Speed Potion** for **+8 speed for 180 seconds**. The readout beside the movement diagnostics shows melee damage range, accuracy, critical chance and active buff timers. Reusing a potion refreshes its timer; a later buff to the same stat replaces the earlier value. Gear swaps and expiry never add those bonuses to base stats. Buffs follow simulation time, persist through map travel, and keep counting down while defeated in local play. Loading saved progress also restores kit claims; restarting Play without loading starts fresh.

Press **K** (or click **Skills**) and choose **Try Fighter skills** for an optional local practice setup. It changes the job to Fighter and grants level-20 Sword Mastery, Sword Booster, Power Strike and Slash Blast, while keeping your current level and EXP. Equip the Sword from the inventory practice supplies to see mastery raise the melee range from **2–12 to 8–12**, with accuracy **19 → 39**. Unequipping it removes the mastery bonus.

The practice setup also fills the hotbar: **1 = Power Strike**, **2 = Slash Blast**, **3 = Sword Booster**. You can click those slots or cast a selected skill from the Skills window. Power Strike costs 12 MP and attacks one target at 260% damage. Slash Blast costs 16 HP and 14 MP, attacks up to six targets at 130%, and extends forward reach to 150%. Both use the equipped weapon's stance, trail and delayed impact, plus real skill art. Sword Booster costs 10 HP and 10 MP for 200 seconds of faster attacks. A cast cannot spend the last HP, interrupt another action, or execute an unsupported spell. Potions restore spent resources. Skill sounds, later job advancements, beginner skills and additional passive handlers remain future work.

The Skills window also offers **Try Magician skills**. This optional practice setup grants level-20 Energy Bolt and Magic Claw, a Wooden Wand and 10 Blue Potions. It raises characters below level 8 to level 8 so the wand meets its actual equipment requirement, preserves earned EXP and attributes, and changes the job to Magician. Equip the Wooden Wand from Inventory, then use **1 = Energy Bolt** (14 MP) or **2 = Magic Claw** (20 MP). The bolt damages its selected target on arrival; Claw deals two independently rolled hits. Casting without a target still launches a bolt. Projectiles and active skill effects clear safely across map travel while the learned book and equipped wand remain.

**Magic damage limitation:** the inspected HeavenClient code does not apply character MATK or skill `mad` to damage. This port preserves its WATK/INT/LUK-derived bounds and magic-defense rule; it does not claim the retail magic formula. The HUD shows the separate melee and magic ranges while a wand/staff is equipped.

The source client's contact-defense formula remains an approximation. Job-specific stat growth, rolled/scrolled gear, stack splitting, full hat masks, additional skills/spells/passives, and comprehensive loot tables remain pending. Local progress survives map travel and can be saved manually. Positive authored spawn delays are respected; negative delays do not respawn locally. Online spawning remains the server's responsibility.

Henesys is also the enabled build scene. Missing NX files produce warnings and mock fallbacks; those fallbacks do not provide the verified visual scene. The NX files are not included in Git.

| Action | Control |
|---|---|
| Move | Left/right arrows or A/D |
| Jump | Space or left Alt; release before jumping again |
| Basic attack | Left Ctrl or Z |
| Ladder / nearby portal | Up arrow or W |
| Crouch / climb down | Down arrow or S |
| Drop through a platform | Hold Down to crouch, then press Jump |
| Jump off a ladder / rope | Hold Left or Right and Jump |
| Character progression / AP / first job | P, or C → Character progression |
| Learn / improve / assign skills | K; select a skill and use +1 (1 SP) |
| Inventory | I, or click Inventory [I] |
| Use / equip / remove | Select an inventory item, then click its action |
| Item pickup | Automatic when close enough |
| Revive after defeat | Click **Revive at a safe spawn** |

Drop-through requires another foothold less than 600 pixels below. Jump alone keeps the player attached to a ladder; releasing its end or jumping away starts the source one-second re-grab cooldown. Flash jump is not bound in the main game input. The first skills and Luna’s local shop are playable; general NPC dialogue, quests, other shops and server play remain incomplete. Inventory actions and the optional practice kit currently run in local play.

Animated map backgrounds now use HeavenClient's 8 ms world clock, including frame delays, fades, scaling and scrolling. To see them, open **MapleUnity → Generate Map Scene**, choose **Kerning City (103000000)** for fading lights or **Aqua Road (230000000)** for moving fish, generate the scene and press Play. Henesys's clouds also follow this clock. Existing generated scenes reload their animation frames on startup. Unity Pause freezes the backgrounds; map travel resets their phase. Aqua Road now uses the original underwater movement. **Jump from the ground with Alt/Space, then use arrows or WASD to swim in all four directions.** Releasing the keys lets you drift; landing restores normal walking. Jump does not propel you while already swimming, matching HeavenClient. The two original swim poses, including equipped clothing, follow simulation time. Standing, walking, crouching, jumping and climbing now share the source body clock: walking follows actual horizontal speed, climbing follows vertical speed, and a stopped climb retains its current pose. Equipment and clothes sample the same frame. The face now blinks and animates expressions with its original frame delays and brow alignment. In local play, **F1–F7** select hit, smile, troubled, cry, angry, bewildered and stunned expressions. Expressions return to normal automatically; the source allows another change after five seconds of advancing simulation (holding still on a ladder freezes this cooldown). The optional physics overlay now uses F9. Pausing simulation freezes every ordinary body pose; changing clothes or weapons that use the same pose preserves its phase.

## Original HUD and windows

Play the existing **henesys** scene; regeneration is unnecessary. The runtime now loads **UI.nx** from the same NX directory as the other assets and builds the original HUD and window art automatically.

- **I**: inventory, with Equip/Use/Setup/Etc/Cash tabs and real stack slots. Drag an item within its category to move it, swap with another item, or combine matching stacks. Overflow stays in the source slot. Select an item, then use the action in the adjacent details window; duplicate stacks are handled separately.
- **E**: equipped items on the original equipment diagram.
- **K**: skills. Use page arrows or tier tabs, spend earned SP with **+1 (1 SP)**, inspect/cast a skill, or assign a learned active skill to quickslots **1–8**.
- **P**: character progression, base-attribute AP allocation and the five first-job choices.
- **C**: combat stats, ammunition and active buff timers. WASD retains movement controls.
- **N** near Luna: trade. The HUD **MENU** opens local help and manual Save/Load.
- Drag window title bars to move them; use **X** or **Escape** to close them.

The HUD follows the supplied Classic references: a centered 800×71 strip with attached quickslots, job/name fields, values above shaded gauges, and three large lower buttons. It remains at native size at 800px and wider; smaller views scale the whole strip together. The upper arrow hides/shows quickslots without changing assignments or 1–8 casting. The red shoe opens local trade; N and MENU offer alternate routes. Quickslot icons use native 32px canvases and original level-number glyphs. The Cash Shop button is unavailable. Minimap, chat, quests and other unimplemented windows are still future UI work.

## Local loot, shop, and saving

Monsters now drop **5 × monster level** mesos (minimum 5, maximum 1,000), plus a 20% chance of a Red Potion. Snails, blue snails, red snails and orange mushrooms also drop their matching material. Walk over loot to collect it. A full bag leaves item drops on the ground; mesos use a separate wallet. Uncollected drops expire after three minutes or on map travel.

Visit **Henesys Market → General Store** (map **100000102**), stand near **Luna**, and press **N** or open **Local play → Trade with Luna**. Buy potions, ammunition packs, a Sword or Red Rubber Boots; sell eligible bag items from the Sell tab. Quantity x10 buys ten displayed packs or sells ten items from the clicked stack. The sell list follows bag order and shows the available count for each stack. Worn equipment cannot be sold. Ammunition purchasing is supported; recharge and ammunition resale remain pending.

Each bag category has **24 slots**. Items split/fill stacks using NX `slotMax`; each equipment copy takes one slot. Drag within a category to move, swap, or combine stacks. Moves require an alive character between attacks; dropping outside the bag cancels. Pickups or other bag changes cancel a held drag. Use/equip and shop sales act on the selected stack, and swapped equipment returns to that bag slot. Ammunition priority follows slot order. Saved progress preserves the new positions. Full-bag failures do not discard gear or consume practice-kit claims. Stack splitting, dragging between bag and equipment, and rolled equipment instances remain unfinished.

Use **Local play → Save progress** before stopping Play. Next time, choose **Load saved progress**. Saving is manual; opening or generating a map never loads an older character automatically. Saves retain level/EXP, base stats, HP/MP, job, mesos, bag slots/counts, equipped gear, learned skills, hotbar and kit claims. Loading replaces this session at the saved map's safe entrance. Temporary buffs/actions and map drops/monsters restart; debug damage overrides are not saved.

The single save remains `offline/character-v1.json` under Unity's `Application.persistentDataPath`. New writes use internal format version 2 and retain AP, SP by job family, the first-job choice and previously rewarded levels. Version-1 saves load through the migration described above. The preceding save remains in `.bak`. Writes use a temporary file and atomic replacement; invalid or incompatible saves are rejected before changing the active character or collision terrain.

Loot quantities, drop chances, shop stock and purchase prices are **explicit offline rules** in `OfflineEconomy.cs`. HeavenClient normally receives loot results and shop stock from its server. NX supplies item names, effects, icons, stack sizes, resale prices and `notSale` restrictions. Online play remains deferred.

## Validate changes

Use the Unity Test Runner, or run the Windows helper from the project root:

```powershell
pwsh -File Tools/Run-UnityValidation.ps1 -Mode Smoke
pwsh -File Tools/Run-UnityValidation.ps1 -Mode PlayerCompile
```

- **Smoke** opens real Henesys, checks eight background layers, character sprites, all 35 NPC foothold positions, and bush/ground ordering. It walks, jumps, climbs and exits a real ladder, verifies ladder/rope metadata, drops onto a lower foothold, and checks listener cleanup. It also checks the editor generator helper, recovers deliberately lost map art, verifies native camera zoom and the selected starting map, and travels from Henesys through the market and a narrow interior, returns to generated Henesys, tests a same-map teleport and fall recovery, and checks camera bounds, retained zoom, topology, NPC art, and cleanup. It also measures rendered pixel occlusion while dropping between foothold layers, checks character parts remain grouped, and verifies NPC/player ordering after travel and while climbing. The hunting-map check also verifies all 49 monster sprites, foothold layers, movement frames, attack/hit/death poses, health feedback, respawn counts, and cleanup while leaving during a death animation. It also checks contact damage, hit flashing, the live EXP bar, level-up feedback, defeat, the revival button’s raycast/click path, return-map travel, and progress retention. The inventory check clicks through the practice kit, equipment swaps, potion use and map travel, and verifies complete clothing pieces in standing, walking, attack and jump poses. The weapon check also clicks between bat, sword/shield and polearm, checks independent stand/walk overrides and arm/sleeve/weapon order, verifies that attacks follow simulation time, and exercises prone attacks and travel cancellation. The afterimage check renders rightward sword windup/impact/fade and a leftward polearm kill, verifies delayed health/EXP feedback and simulation-only effect playback, and checks effect/impact cancellation across map travel. The stat/buff check clicks real Warrior/Speed Potions, checks live stat changes, refresh/expiry and travel, and captures delayed MISS and CRIT feedback using controlled random rolls. The attack movement check verifies momentum braking, locked facing/stance, discarded jumps, prone stabs and the return to held movement using real equipped art. The skill checks use the real Skills window and hotbar, verify mastery and Booster costs/timing across travel, and render delayed Power Strike and six-target Slash Blast with the original skill art. Magician checks equip a real wand through inventory, capture projectile flight/arrival, verify MP costs, two visible Claw damage lines and travel cleanup. Ranged skill checks cast all four skills from the hotbar, verify separate gun launches, original projectile/use/hit artwork, crossbow hit effects, two damage labels, resource costs and cancellation across travel. Ranged checks select and equip all four practice kits through UI clicks, render their real weapons and ammunition, verify last-round damage, empty-ammo blocking, left-facing gun recoil and travel cleanup, and check the selector at 640 x 480. They also check slot geometry and the Skills window at 640 x 480. Swimming checks use real Aqua Road terrain and both equipped poses, freeze movement/animation with simulation, attack while afloat and return to dry-map walking. Background checks verify cloud pause/resume, Kerning light fades, Aqua Road fish frames/flips, same-map resets and travel cleanup. The progression check earns a combat level-up, spends AP, chooses Magician, learns prerequisite-linked skills, upgrades a quickslot, casts through the HUD, and restores points and skills from disk in a fresh scene. It saves scene renders, HUD captures, and player/NPC/monster/movement/equipment closeups. It requires NX data and graphics.
- The local-loop smoke also earns monster loot, trades through Luna’s real shop location, saves gear/skills/hotbar/claims, and loads the character into a fresh scene. It checks actual UI click routing and renders the shop and save menu at two window sizes.
- **PlayerCompile** compiles Windows player scripts and checks that editor/test assemblies do not enter the runtime set. It does not create or launch a packaged game.
- **EditMode** accepts a test filter. Existing failing tests are deferred at the user's request; use the focused command below for this rewrite. Running without a filter still executes the entire older suite.

Each command copies `Assets`, `Packages`, and `ProjectSettings` into a unique directory under `Temp`, so an open editor on the working project is unaffected. Results, logs, and screenshots go into `Logs`. Snapshots are retained; fresh imports take longer than an ordinary rerun. Pass `-UnityPath` if Unity is installed elsewhere.

For the current rewrite checks (753 checks, including the compiled C++ references):

```powershell
pwsh -File Tools/Run-UnityValidation.ps1 -Mode EditMode -TestFilter 'CharacterProgressionIntegrationTests;InventorySlotIntegrationTests;LocalLoopIntegrationTests;FaceAnimationTests;FaceAnimationIntegrationTests;FaceAnimationAssetTests;StanceAnimationTests;StanceAnimationIntegrationTests;StanceAnimationAssetTests;SwimmingTraceTests;SwimmingIntegrationTests;SwimmingAssetTests;BackgroundAnimationTests;BackgroundAssetTests;BackgroundRecoveryTests;BackgroundAnimationRenderingTests;RangedSkillIntegrationTests;AmmunitionRewriteTests;RangedCombatIntegrationTests;MagicCombatRewriteTests;MagicSkillIntegrationTests;MeleeSkillIntegrationTests;PassiveSkillRewriteTests;PassiveSkillIntegrationTests;SkillSourceAssetTests;CombatStatRewriteTests;CombatBuffAssetTests;AttackMovementTraceTests;TraversalInputTests;WorldRecoveryTests.PortalUsesPixelCoordinates_AndRequiresAnotherPressToReturn;AfterimageCombatTests;AfterimageAssetTests;WeaponRewriteTests;WeaponAssetRewriteTests;ItemInventoryRewriteTests;ItemAssetRewriteTests;PlayerCombatProgressionTests;MonsterAssetTests;MonsterRewriteTests;MonsterGroundTraceTests;NormalMovementTraceTests;TraversalTraceTests;TerrainTraceTests;CharacterRenderingRegressionTests;RealCharacterLandmarkTests;SourceSimulationClockTests;MapRenderOrderTests'
```

## Code layout and source conventions

| Directory | Responsibility |
|---|---|
| `Assets/Scripts/GameLogic` | Unity-independent world state, input intent, physics, combat, inventory |
| `Assets/Scripts/GameData` | NX readers, source coordinate conversion, asset providers and caches |
| `Assets/Scripts/GameView` | Unity input, character layers, camera, and world views |
| `Assets/Scripts/SceneGeneration` | Source map extraction and scene components |
| `Assets/Scripts/Core` | Runtime composition between map generation and the view |
| `Assets/Scripts/Tests` | Unit, integration, rendering, and PlayMode tests |

Map footholds and portals retain Maple pixels with Y pointing down. Runtime actor positions use Unity world units with Y pointing up: 100 pixels = 1 unit. The Unity player root is at the character's center; its visual root is half the player's height below it, at the feet.

The simulation advances at HeavenClient's 8 ms tick (125 ticks/second), independently of rendering. NormalMovement keeps source feet positions/velocities in double precision and converts only at the public Player/view boundary. Jump, crouch, climb, and portal calls queue intent for a tick. Ladder entry uses rounded feet coordinates and happens after movement. Underwater maps import the source `info/swim` flag. Swimming uses source directional force, drag and gravity, while walking and jumping from underwater footholds keep their ground rules. Advanced movement skills still use legacy approximations.

The movement fixtures contain 9,680 C++ ticks across 48 scenarios, including overlaps, authored-layer retention, equal-height ties, crossing slopes, negative coordinates, walls, ceilings, and map limits. Regenerate them with g++ on PATH and the matching source checkout:

```powershell
python Tools/Generate-HeavenMovementTrace.py --source C:/HeavenClient/MapleStory-Client
```

The generator compiles selected source methods with synthetic input/terrain; it does not launch the full C++ client. See PROJECT_STATUS.md for its coverage and intentional differences.

The available Character.nx lacks HeavenClient's default shirt (1042399); the character currently renders with default pants and a bare torso. See the [latest scene](Logs/playtest-reviewed-scenes.png), [player closeup](Logs/playtest-reviewed-scenes-player.png), and [NPC closeup](Logs/playtest-reviewed-scenes-npc.png), and [ladder pose](Logs/playtest-reviewed-scenes-ladder.png).

The saved scene contains embedded map art. Runtime MonoBehaviours must each live in a file matching their class name, with committed `.meta` files, so scene references remain stable after reload. Rendering diagnostics can be enabled with `MAPLE_RENDERING_DEBUG`.
