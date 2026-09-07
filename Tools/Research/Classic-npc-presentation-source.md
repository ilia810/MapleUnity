# NPC speech, dialogue orientation and event feedback — 6 September 2026

Reference source: the working tree at `C:/HeavenClient/MapleStory-Client`, original installed NX files and the user's nine Classic screenshots. This milestone adds presentation to the existing offline loop; it does not enable network play, add quests or change reward rules.

## Ambient NPC speech

`Gameplay/MapleMap/Npc.cpp` resolves linked NPC art and collects each state node's `speak` references from `String.nx/Npc.img/{originalNpcId}`. Its draw path does not actually draw these lines. Unity's new `NxNpcSpeech` retains the original NPC string identity while resolving linked art, reads explicit speech references from `info`, `say`, `stand` and other states, removes duplicates, and rejects missing/cyclic links. It does not turn arbitrary `d0` dialogue greetings into ambient speech. Examples checked against real data include Bruce's `say/speak`, Luna's `info/speak`, and Sam's linked art with Sam's own lines.

`UI.nx/ChatBalloon.img/npc` contains the original six-pixel corners, 12-pixel horizontal tiles, 14-pixel vertical tiles, center and 13×13 tail. The `clr` value is ARGB `FF800000`, the dark red text seen in the references. `IO/Components/MapleFrame.cpp` and `ChatBalloon.cpp` explain frame assembly and the four-second line duration. Unity repeats the native edge/center art without editing an NX bitmap. Normal text is 80 pixels wide; longer lines get 140 pixels. Heights round to full 14-pixel strips.

`ClassicNpcBubbles` adds a local scheduling policy because the C++ client does not render NPC speech: each NPC has a deterministic offset in a 12-second cycle, a line remains visible for four seconds, and subsequent cycles choose subsequent authored lines. Up to three nonoverlapping on-screen bubbles are shown, with nearby NPCs taking priority. Bubbles stay above actual rendered sprite bounds, clear quest markers, keep their tails aimed at the actor, and stay behind windows. Lines that cannot fit above an NPC in the viewport wait for a later opportunity. Bubbles never intercept pointer input. The active conversation pauses that NPC's ambient bubble. Old-map actors and bubbles are removed during travel.

## Both dialogue orientations

The reference screenshots show NPC portraits on both sides. `IO/UITypes/UINpcTalk.cpp` in the inspected checkout implements a left-side portrait or hides it according to a speaker byte; it does not implement the reference's right-side layout. Current offline Quest.nx Say pages contain no speaker-side flags.

Unity uses the original `UIWindow.img/UtilDlgEx` top, repeated center and bottom pieces for both orientations. Only the empty frame is mirrored; the portrait, name bar, text and buttons are placed independently and stay readable. For a newly opened offline conversation, an NPC standing to the player's right uses the right portrait; an NPC to the left or at the same X uses the left portrait. This approach-side rule is a Unity presentation choice, not an inferred server packet or fabricated quest field. The chosen side remains stable throughout the conversation.

Both layouts keep the existing independent window position/focus, paged dialogue, acceptance/reward flow and proximity guards. NPC body text is 13 pixels. Dialogue and journal scrollbars now hide when their text fits, and appear for overflowing pages.

## Level-up effect and notifications

`Character/CharEffect.cpp` and `Char.cpp::init` point to `Effect.nx/BasicEff.img/LevelUp`. The installed effect has 21 numbered frames with authored origins and the original 100-ms default delay. `ClassicPlayerEffects` plays it once around the player's feet on the real `LeveledUp` event. It follows the interpolated actor position, remains upright, respects the current foothold drawing layer, stops after its authored duration and clears during map changes. Restoring a save does not emit or replay the effect. Multiple levels awarded together restart a single effect instead of stacking copies. This milestone does not add level-up audio.

`ClassicNotifications` replaces the high-centered transient text with the lower-right placement shown in the references. It shows real EXP events, level messages and successful item/meso pickups in a nonblocking stack above the HUD/quickslots. EXP/level messages are gold; pickups are white, with a dark shadow. The stack is limited to six entries and uses a local six-second lifetime with a one-second fade. Newest messages appear at the bottom. Messages still reach the existing HUD history; EXP awarded with a level-up is no longer discarded by the old level-notice priority flag. Map changes clear transient messages.

## Verification and remaining scope

The focused scene tests exercise both portrait sides and quest acceptance, speech text and pointer passthrough, small viewport bounds, real EXP and pickups, native effect frames, bounded message lifetime, map cleanup and save restoration. Source data tests cover state/info speech references, linked NPC identity and missing/unreferenced text. Result files and final captures are recorded in `PROJECT_STATUS.md` and the updated Classic visual review.

Remaining visual work includes HUD/quickslot typography and fine spacing, broader character/NPC animation coverage, item/action binding visuals and additional source effects such as job advancement and quest completion. Full scripted NPC services and additional quest chains remain separate rewrite work. The original NX archives and reference screenshots are unchanged.
