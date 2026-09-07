# Shop and item tooltip visual pass — 2026-09-06

## Reference and source

The user's shop and item-hover references (images 4, 6 and 7 in `Tools/ArtReferences/MapleClassic`) guide this pass. Behavioral source is the inspected working tree at `C:/HeavenClient/MapleStory-Client`, especially `IO/UITypes/UIShop.cpp` and `IO/Components/EquipTooltip.cpp` / `ItemTooltip.cpp`. The C++ UI has mixed-version coordinates and `UIToolTip.img` dependencies that are absent in the supplied UI.nx, so those dimensions and missing textures are not copied blindly.

## Original shop composition

- `UIWindow.img/Shop/backgrnd` is 463×339. The Unity shop remains 463×378 with six rows, using its native top 126 pixels, six copies of the original 39-pixel list row, and its last 18 footer pixels. This preserves the actual rounded silver frames, portrait wells, background dither, tab surround and row separators.
- Selected rows use `UIWindow.img/Shop/select`, 162×35. Each price uses `Shop/meso`, 12×12, with text starting after the coin. Pack quantities and bag-stack counts remain explicit. Long names end with an ellipsis; hovering exposes the full title.
- Both list columns now have original `Basic.img/VScr` up/down arrows and a thumb between them. Arrows disable at the corresponding boundary. Mouse-wheel input works over rows and blank list areas. Selection, exact-stack sale validation and existing ×1/×10 transactions remain the same.

## Standing character and NPC portraits

The C++ shop calls `charlook.draw(..., false, STAND1, DEFAULT)`. Its `CharLook::draw(Point, bool, Stance, Expression)` overload calls `CharEquips::adjust_stance`, selecting the equipped weapon's standing pose. The former Unity UI copied whatever pose was currently in the world, including walking. `MapleCharacterRenderer.ComposeStandingPortrait` now assembles a standing/default-expression look from the actor's real appearance and equipped item IDs. The composition stays under an inactive object; ordinary UI images present its layers. It never subscribes to player view updates, changes movement/animation clocks, or adds a visible world actor. Equipment/appearance changes refresh the portrait; animation phase alone does not rebuild it. All visible parts are fitted together inside the portrait well.

The shopkeeper uses the existing `ClassicNpcPortrait` origin-aware presenter with a shop-sized baseline and bounds. The actual nearby NPC selects its animation rather than relying solely on a hard-coded first frame. This does not expand the current Luna-only offline shop catalog.

## Tooltip layout and state

Equipment cards keep their 236-pixel width and align comparison cards along their top edge. The title has a cyan bullet, larger white text and enough height for wrapping. Requirements occupy a dedicated block next to the icon. Six supported job families use measured text widths to fit one compact row above a divider; item stats and remaining template enhancement slots follow below. Gender restrictions and one-of-a-kind/untradeable metadata are shown when applicable. No rolled enhancements, fame requirement, or other unmodeled property is invented.

The navy background is slightly more opaque for legibility. Normal item cards retain their 286-pixel format, render escaped NX line breaks as actual newlines, and clear any equipment comparison. Cards remain noninteractive, fit within the viewport at each corner, and update requirements/equipment comparison while the pointer is stationary. The title/icon/content cache distinguishes different icons and character job/gender state. Hiding or destroying the owning window clears its tooltip, including owners represented by a GameObject.

## Scope and validation

Focused scene coverage exercises original price/selection art during real buying, arrow and wheel scrolling, a separately standing equipped portrait while the real actor walks, equipment refresh without changing its phase, two-card corner placement at 1366×768 and 640×480, live requirements and closing/clearing stale tooltips. Validation passed: 40/40 scene regression cases (`Logs/shop-tooltip-scenes.xml`), then 5/5 affected cases after the final source-description correction (`Logs/shop-tooltip-text-final.xml`). Windows player compilation passed with zero C# errors (`Logs/shop-tooltip-player.log`). All 384 C# files match the isolated snapshot and have metadata. Final shop/tooltip captures use `Logs/shop-tooltip-text-final-*`; the review contains 212 valid local file links. The original Unity editor and unsaved scene were preserved.

This is presentation and interaction work. More shops, scripted services, a complete keyboard editor, additional item actions, quest chains and class-specific actions remain separate rewrite work. Online play remains excluded.
