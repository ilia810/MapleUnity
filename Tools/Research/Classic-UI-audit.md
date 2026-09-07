# Classic UI comparison — all nine references

Implementation follow-up: [compact windows, independent focus and current controls](Classic-UI-implementation.md). The comparisons below describe the **pre-implementation baseline**.

Reviewed 2026-09-06. This is a visual and implementation audit, not a completed UI replacement. It expands the earlier bottom-HUD pass to the whole screen. [Illustrated comparison](../ArtReferences/MapleClassic/review.html).

Evidence: all nine unchanged user screenshots in `Tools/ArtReferences/MapleClassic`, the latest `classic-reference-final-scenes-*` Unity captures, the final corrected HUD capture `classic-reference-final-glyphs-classic-hud-wide.png`, current Unity UI code, and read-only inspection of `C:/HeavenClient/MapleStory-Client/nx/UI.nx`. Older scene captures remain representative of the windows; their quickslot-number rendering predates the final glyph correction.

## Scale and positions

The reference screenshots are 2048×1152. Their 800px-style HUD appears about 1200px wide, consistent with approximately 1.5× video scaling. Dividing screenshot measurements by 1.5 gives useful working dimensions, **not confirmed original resolution or exact asset specifications**. Measurements below are approximate outer bounds; compression and borders introduce several pixels of uncertainty.

| Element | Reference outer bounds, x/y/width/height at 2048×1152 | Approximate working size | Current Unity layout |
| --- | --- | --- | --- |
| Bottom HUD, all images | 425 / 1050 / 1200 / 102 | 800×68 | Centered 800×71; overall structure is now close |
| Quickslots, all images | 1402 / 935 / 221 / 115 | 147×77 | 151×80, docked above HUD's right edge; similar placement |
| Skill book, image 2 | 1392 / 164 / 291 / 554 | 194×369 | 175×289 book inside a centered 449×392 group with blue companion |
| Inventory, images 5–6 | 988 / 334 / 310 / 430 | 207×287 | 175×289 bag inside a centered 449×338 group with blue companion |
| Shop, images 4 and 7 | 679 / 294 / 693 / 565 | 462×377 | 463×339 shop inside a centered 737×380 group with blue companion |
| Dialogue, images 3 and 8 | 632 / 389 / 786 / 316 | 524×211 | No general NPC dialogue window |
| Character stats, image 9 | 886 / 363 / 275 / 419 | 183×279 | Separate blue 266×380 progression window |
| Attached stat details, image 9 | 1160 / 450 / 278 / 332 | 185×221 | Separate blue 266×280 combat window, initially top-left |
| Quest helper, image 3 | 1608 / 25 / 333 / 332 | 222×221 in this state | Missing; height varies with objectives in the references |

Placement findings:

- The minimap starts at the upper-left edge. Its width and height vary with location and collapsed/expanded state; it is not one fixed-size rectangular placeholder. Image 4 shows just its collapsed title strip.
- Buffs are tight to the upper-right in images 1–2. Quest tracking is near the upper-right in images 3–9. The screenshots do not show enough simultaneous states to establish the exact spacing between these two systems.
- The skill book sits at roughly 68% across and 14% down the screen. The inventory sits near 48% across and 29% down. The latter coordinates repeat in images 5–6. These are useful reference placements, but may be player-dragged positions rather than game defaults.
- Dialogue and shop are horizontally centered. Dialogue is wide and shallow, slightly above the screen's vertical center. Shop occupies a taller centered pair of panes. Our attached controls make the shop group much wider and shift the shop artwork left of center.
- The character-stat detail pane attaches to the main pane's right edge and shares its bottom edge. It is shorter and starts lower; it is not a separately positioned full-height panel.
- Tooltips appear close to the hovered item and overlay windows/world. Equipment comparison panels sit beside one another and share their top edge. They need viewport-aware placement near screen edges.
- Chat history grows upward from the HUD's left portion. Quickslots occupy a separate area above its right portion. Our current message line does not reproduce this screen composition.
- Current `ClassicWindow` uses centered roots and a generic screen clamp. It reserves only the bottom strip, not the taller quickslot area. Combat stats initially occupy the reference minimap corner. Drag positions last in memory; they are not saved across sessions. Several ordinary windows automatically close one another. Window-specific initial positions, focus/overlap rules and saved positions need deliberate treatment; a screenshot alone cannot prove all interaction rules.

## Image 1 — HUD and combat

[Reference](../ArtReferences/MapleClassic/01-hud-combat.png) · [Current HUD](../../Logs/classic-reference-final-glyphs-classic-hud-wide.png) · [Current critical hit](../../Logs/classic-reference-final-scenes-melee-critical.png)

- The centered two-tier strip and 4×2 quickslots now resemble the reference. Border thickness, button proportions, lettering, padding and gauge-caption spacing still need a close matching pass. Our button reads `SHOP`; the reference reads `CASH SHOP`.
- The reference has a channel/input area, an envelope button and small chat controls before the toolbar. Our top strip is a plain local system-message line and omits that arrangement. The reference also shows gray error-message and white input states.
- Quickslots show action/item/skill icons with keyboard labels such as Shift, Insert, Home, Page Up, Ctrl, Delete, End and Page Down. Our bindings are 1–8 and slots contain skills only. Their lower digits currently indicate skill levels; reference quantities and cooldown counters have different meanings. This needs binding/item-slot work as well as visual placement.
- Top-right buff icons and their time/count overlays are absent. Active effects currently appear as text inside Combat Stats.
- Player/monster labels and dark nameplate backings are absent. The reference includes a monster level/name plate and player emblem/name plate.
- Damage uses stylized outlined gradient number artwork and a burst. Unity currently renders ordinary font text, including a literal `CRIT` prefix. Existing health bars also use simple generated rectangles instead of the reference's thin framed presentation.
- EXP/system notifications appear near the lower-right of the gameplay area. Our floating notices occupy a high central position and also feed the single bottom line. Message stacking, position, fade and style differ.

## Image 2 — skill inventory

[Reference](../ArtReferences/MapleClassic/02-skill-book.png) · [Current](../../Logs/classic-reference-final-scenes-classic-skills.png)

- Reference: one compact, taller book. Current: shorter left book plus a large blue permanent description, casting, SP, quickslot and practice panel.
- Six visible skill rows and a narrow vertical scrollbar versus four rows and page buttons in the companion.
- The reference shows beginner/I/II/III tabs for this character, with II selected. Unity always creates beginner/I/II/III/IV tabs. The visible screenshot does not establish tab availability rules for every job.
- The reference's book emblem and job-basics header differ from our generic `SKILL BOOK` tile and uppercase job heading.
- Reference rows use pale blue name/level fields, white separators, framed icons, a plain numeric learned level on the lower line and small arrow controls at the right. Ours use smaller gray rows, `Lv. n/max` text and a single large SP action elsewhere.
- The SP field stays in the book footer. Descriptions and quickslot assignment should no longer force a second full-size window to remain open. Preserve real prerequisites, learned levels, casting and assignment when changing the interaction.
- Match the taller silhouette and reference position; enlarging the existing 175×289 bitmap would also enlarge icons and lettering and would not create two additional rows.

## Image 3 — right-portrait NPC dialogue and quest tracking

[Reference](../ArtReferences/MapleClassic/03-npc-dialogue-right.png) · [Current world](../../Logs/classic-reference-final-glyphs-classic-hud-wide.png)

- The wide centered dialogue, thick inset gray frame, warm neutral body, white text sheet and blue-gray footer are missing.
- Betty's portrait is on the right with a dark blue nameplate below. Text is on the left, with deliberate line spacing and colored names/locations.
- Green `END CHAT` sits at the lower-left; orange `YES` and `NO` sit at the lower-right. This is a distinct layout from the existing blue local menu.
- The upper-right quest helper has a translucent body, a small title/control strip, bold quest headings, objective counts and per-quest remove controls. No tracked-quest view or full local quest/dialogue state currently backs this.
- The minimap includes a region icon, region/location names and colored markers. It is missing in Unity.
- Chat history is visible above the channel/input bar, with translucent dark and colored message backgrounds. Unity lacks the history panel.
- Player/NPC names, emblems and status markers contribute to the crowded town scene. Unity's NPC sprites exist, but the corresponding nameplates/dialogue/quest markers do not.

## Image 4 — shop and collapsed minimap

[Reference](../ArtReferences/MapleClassic/04-shop.png) · [Current](../../Logs/classic-reference-final-scenes-local-shop.png)

- Two centered panes with no permanent third blue panel. Current local save/help/quantity controls widen the group to 737px and displace the shop artwork.
- Shopkeeper portrait on the left and the actual dressed player portrait on the right. Current right portrait area is empty except for a `Your inventory` label and the bitmap's shadow.
- Authored green `LEAVE STORE`, orange `BUY ITEM` and orange `SELL ITEM` controls sit in the portrait areas. Current wide gray `Buy`/`Sell` controls occupy the tabs row instead.
- The shop has an `All` tab; the player side has Equip/Use/Set-up/Etc tabs. These category tabs are missing in the current shop.
- Six visible rows per side, independent slim scrollbars, icon/name/coin/price alignment, and an orange selected row. Current lists show five rows with shared external paging and text-only prices.
- Current clicking a row trades immediately. The reference visibly separates selection from Buy/Sell controls; the revised UI should support selection and an explicit purchase/sale action. Screenshots do not establish double-click or quantity-dialog behavior.
- The NPC's world speech bubble and name/title are missing. The collapsed minimap title strip at the top-left is also missing.
- The reference's indoor map is surrounded by black. The current shop capture exposes blue/cyan outside the room. A separate framing/clear-color check is needed; these captures are different maps, so their room dimensions cannot be compared directly.

## Image 5 — item inventory

[Reference](../ArtReferences/MapleClassic/05-inventory.png) · [Current](../../Logs/classic-reference-final-scenes-classic-inventory.png)

- Five columns by six visible rows, versus our four columns by six rows. The target bag is wider but approximately the same height.
- Six tabs including `Deco`, versus the current five categories. Current bag capacity is explicitly 24 per category; the target's 30 visible cells and extra tab need a compatible capacity/category decision. Merely painting six extra usable-looking cells would be misleading.
- Reference cells are warm gray/beige with white dividers, consistent icon insets and outlined stack counts. Existing icon assets and count glyphs are reusable; the grid geometry must change.
- Title controls include expand/organize controls beside Close. Current only exposes Close. The meso footer needs the reference's wider field and spacing.
- Remove the permanent blue item-details/practice companion from the ordinary inventory presentation. Details belong in a hover tooltip; offline practice actions need a compact separate home.
- The bag is positioned to the right of center in both images 5 and 6. Our group is centered as a whole, leaving the bag itself left of center.

## Image 6 — consumable tooltip

[Reference](../ArtReferences/MapleClassic/06-item-tooltip.png) · [Current selected details](../../Logs/classic-reference-final-scenes-weapon-inventory.png)

- A floating translucent navy tooltip overlaps the inventory/world, instead of occupying a permanent blue side panel.
- It has a thin pale border, a bold white title with a small colored marker, a larger item icon on a light square, and a separate wrapped description column.
- Its width and height follow the content; its position follows the hovered item. The reference tooltip is below and to the right of the hovered slot, with the inventory still visible behind it.
- The current selection-based text description does not implement hover entry/exit, delayed appearance, viewport clamping or dismissal when a window closes or an item moves. These behaviors need explicit implementation and checking.
- Image 6 also shows the narrow vertical minimap representation and lower-right pet/system feedback. Different minimap aspect ratios must preserve the map image rather than stretch it.

## Image 7 — equipment comparison and shop tooltips

[Reference](../ArtReferences/MapleClassic/07-equipment-comparison.png) · [Current equipment details](../../Logs/classic-reference-final-scenes-weapon-inventory.png)

- Two navy tooltips appear side by side: hovered shop equipment and equipped equipment. Unity displays only one unformatted details block in the companion.
- Each tooltip needs its own title, enlarged icon, requirements, class eligibility labels, divider and stat rows. Unmet requirements use red; eligible/ineligible class labels have distinct colors.
- The reference separates type, attributes, defenses and remaining enhancements. Our description compresses several requirements/stats onto plain lines and exposes internal-style stat names such as `WeaponAttack`.
- Comparison needs to select the correct equipped slot and remain stable for duplicate item copies. Existing exact-slot inventory operations must be preserved.
- Remaining enhancements and rolled/scrolled item instances are not modeled by the current simple inventory stack. The visual should show only supported truthful data until those properties are implemented.
- Shop and tooltip layers must coexist without the hover view consuming the click intended for the shop. Close or refresh must clear stale comparisons.

## Image 8 — left-portrait dialogue and world bubbles

[Reference](../ArtReferences/MapleClassic/08-npc-dialogue-left.png) · [Current world](../../Logs/classic-reference-final-glyphs-classic-hud-wide.png)

- This is the complementary dialogue layout: Cherry and her nameplate on the left, white text area on the right. Supporting only the right-portrait image would miss this requirement.
- Both dialogue variants share the centered frame, footer and button alignment. Portrait placement, text padding and nameplate size must remain consistent when sides switch.
- NPCs have pale rounded speech bubbles with a border/tail and colored text, plus yellow names and service titles below. None of this is currently presented by the general NPC renderer.
- The station clock and boarding request are location-specific content and logic, not universal HUD elements. The image alone does not define schedules or transport rules.
- This image reinforces the larger chat-history state, upper-right objective panel and minimap region header.

## Image 9 — character stat window and attached details

[Reference](../ArtReferences/MapleClassic/09-character-stats.png) · [Current progression](../../Logs/classic-reference-final-scenes-character-progression.png) · [Current combat stats](../../Logs/classic-reference-final-scenes-active-buffs.png)

- A compact light main window with pink identity/vital labels, green STR/DEX/INT/LUK labels, aligned values, small increase controls and an AP footer. Our blue progression panel has large gray AP/job buttons and explanatory text instead.
- The reference groups name, job, level, HP, MP, EXP and fame above attributes; AP and `DETAIL` sit at the bottom. Current information is split across the HUD, progression and combat panels.
- The attached detail pane uses blue row labels for attack, weapon defense, magic, magic defense, accuracy, evasion, critical rate, critical damage, speed and jump. The current combat panel is an upper-left text list with buff timers, not this grid.
- Existing source stat artwork is useful but is itself a different layout: its main bitmap is 175×347, includes a Guild row, and puts AP between vitals and attributes. Its detail art includes `HANDS` and different critical labels. It cannot simply be displayed unchanged to match this screenshot.
- First-job advancement/practice controls are local rewrite conveniences, not part of the pictured stat window. Preserve their function in an appropriate separate menu while rebuilding the character-stat view.
- The lower-right party roster is visible in this image. It remains outside the user's current offline scope; do not invent party members or online status to fill the space.

## Shared appearance and behavior gaps

| Area | Difference / required treatment |
| --- | --- |
| Normal window palette | Light neutral content, pale blue list fields and restrained blue-gray footers; saturated blue Notice3 companions dominate the current windows |
| Typography | Match role-specific sizes, weights, baselines and wrapping. Current 11–12px Arial text and stretched generic controls produce different density; the screenshots do not prove an exact font family |
| Frames and scaling | Preserve native bevels, corners, icon sizes and pixel alignment while recomposing dimensions. Do not stretch an entire baked window to add rows/columns |
| Tabs and controls | Pink selected tabs with red baseline, authored close/arrow/scroll controls, orange primary actions and green exit/detail actions; current generic gray buttons substitute for many of them |
| Cursor | Reference glove pointer, including hover/selection poses; no runtime custom-cursor binding found in the Unity scripts |
| World labels | Player/NPC/monster names, NPC service titles, dark backings and some markers/emblems are absent; online-specific identity decoration needs its own scope |
| Combat overlays | Font-based damage, simple HP rectangles and high-centered notices differ from the reference's number art, borders and lower-right message stack |
| Focus and safe bounds | Ordinary windows, HUD, quickslots, hover overlays and modal dialogue need separate placement/layering rules; resizing should keep visible controls reachable |

The screenshots show different maps, outfits, enemies, levels, party states and camera positions from our test captures. Those content differences do not establish renderer defects. Exact map coverage, object overlap, animation, NPC grounding and effects timing require a matched scene or gameplay capture. The visible interior surround and missing labels are concrete differences; broad claims that every background/sprite is wrong would be unsupported.

The video exit-fullscreen banner, user chat text and `ONLINE TEST` watermark are not game UI requirements to reproduce. The screenshots do not show the open Menu/Shortcut/World Map screens or a separate equipment-slot window; their exact layouts remain unestablished by this set.

## Asset findings and implementation order

The original C++ source remains useful for behavior, but its UI contains mixed-version paths and previous trial changes. Confirm paths and dimensions against the actual NX archive before porting coordinates.

| Source artwork verified in UI.nx | Use / limitation |
| --- | --- |
| `UIWindow.img/Item/backgrnd`, 175×289 | Four-column source bag; requires reassembly for the five-column target |
| `UIWindow.img/Skill/backgrnd`, 175×289; `BtSpUp` | Four-row source book and small SP controls; target is taller |
| `UIWindow.img/Shop/backgrnd`, 463×339; `BtBuy`, `BtSell`, `BtExit`, tabs, `select` | Most shop pieces exist; add the sixth row without enlarging the original five |
| `UIWindow.img/Stat/backgrnd`, 175×347; details 177×203 and 177×221 | Reuse palette/frames/labels selectively; section ordering differs |
| `UIWindow.img/UtilDlgEx/t`, `c`, `s`, `bar` | 529px-wide composable dialogue frame and 121×19 nameplate; close to the target family |
| `UIWindow.img/MiniMap/{MaxMap,MinMap,Min}` | Original minimap frame pieces exist; map preview/markers still need runtime wiring |
| `UIWindow.img/QuestAlarm` | 223px-wide composable quest helper artwork exists; quest tracking behavior is missing |
| `UIWindow.img/ToolTip/Equip` | Requirement/class/property glyphs exist. The C++ tooltip's `UIToolTip.img` path is absent in this NX archive; do not copy it blindly |
| `Basic.img/Cursor`, `Basic.img/VScr*`, `Basic.img/ItemNo` | Cursor states, scroll controls and count glyphs can be reused |

Recommended sequence for the next implementation passes:

1. Shared light window pieces, font/control metrics, window positioning/focus and hover-tooltip layer. Move local practice/help controls out of the permanent inventory/skill/shop companions while retaining their functions.
2. Inventory and equipment details/comparison, including the five-column presentation, organized title controls and truthful capacity/category handling.
3. Six-row skill book, small per-row SP actions, scrolling and compact quickslot assignment.
4. Character stats with an attached detail pane, reusing earned AP and real combat values.
5. Centered six-row shop, actual player portrait, category tabs, selection/actions and shared tooltips.
6. Minimap, top-right buff icons, local message history, nameplates, cursor and reference-style damage feedback.
7. General NPC dialogue and tracked quests with real offline state. Online chat, parties and cash-shop service remain excluded.

Validation for these passes should capture the same UI states at 1366×768, then check smaller viewports. Review proportions, corner sharpness, grid alignment, text clipping, tooltip edges, dragging/focus and overlap with quickslots. Preserve the existing inventory transactions, AP/SP/prerequisite behavior, skill assignment, shop range checks and save/load coverage. This audit made no runtime changes and did not rerun the deferred tests.
