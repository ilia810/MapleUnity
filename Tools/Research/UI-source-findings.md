# Original UI assets: implementation map

Read-only research for the Unity offline UI restoration. Source: inspected working tree at `C:/HeavenClient/MapleStory-Client`; images and metadata: its actual `nx/UI.nx`. Do not infer release/version from the v83/v87/v92 comments. The available file contains `StatusBar.img`, `UIWindow.img`, and `Basic.img`, with no `UIWindow2.img`.

The C++ UI code has partly adapted classic asset paths but retains incompatible modern coordinates, overwritten button positions, missing fallbacks, and explicit trial offsets. Use its sound/input/data behavior where coherent, and use the actual image boundaries for layout. These are distinct from the previously verified source gameplay algorithms.

## Shared drawing rules

- At scale 1, source `Texture::draw(P)` has image top-left `P - origin` and native bitmap dimensions. Hit bounds follow that same rule. Origins are not layout positions unless deliberately negative.
- Source button sprites live under `normal/0`, `mouseOver/0`, `pressed/0`, `disabled/0`. Most of these classic UI origins are `(0,0)`; some small centered buttons and selected tab labels differ.
- `TwoSpriteButton` uses the selected image for mouse-over or pressed, otherwise unselected. Preserve stable hit regions when state images have different widths/origins.
- `GraphicsGL::draw` interprets horizontal `Range(a,b)` as pixels cropped **from left and right**, respectively, not a rectangle from a to b. Both destination and source bounds move inward by those amounts. Vertical works the same way.
- C++ `Icon` shifts the item texture origin by `(0,32)` and scales all inventory/equipment icons to 0.85. This is an active local modification, not an intrinsic asset requirement. Unity icons already normalized to top-left should not receive another 32px shift; native 32px icons fit the actual cells.
- `Basic.img/ItemNo/0..9` supplies item quantity glyphs. `Basic.img/LevelNo/0..9` supplies the original level glyphs. `StatusBar.img/number` supplies HUD digits, `Lbracket`, `Rbracket`, `slash`, `percent`, `dot`.

## HUD

All paths below are under `StatusBar.img`.

| Asset | Native size | Source final draw anchor relative to HUD |
| --- | --- | --- |
| `base/backgrnd` | 800 x 71 | (0,0) |
| `base/backgrnd2` | 570 x 71 | (1,0) |
| `base/chat` | 566 x 5 | (0,0) |
| `gauge/bar` | 340 x 31 | (217,38) |
| `gauge/graduation` | 340 x 31 | (217,37) |
| `base/box` | 42 x 19 | (572,10) |
| `base/quickSlot` | 151 x 80 | Derived from Shortcut bounds, see below |

`base/backgrnd` has the silver/blue strip; `backgrnd2` has the black player/status backing and baked LV label. `gauge/bar` already contains HP/MP/EXP captions and fully colored fills, plus black edges. `graduation` adds white borders and graduations. Player name, job, level value, and all current/maximum figures are separate draws.

Exact colored rectangles inside the **bar bitmap**, in top-left coordinates with width/height:

| Gauge | Interior rectangle | Full horizontal span including borders |
| --- | --- | --- |
| HP | (2,15,105,14) | [0,109) |
| MP | (110,15,105,14) | [108,217) |
| EXP | (223,15,115,14) | [221,340) |

For a restoration, retain the labels/edges, mask only the missing part of those interiors, then draw graduation on top. This keeps the authored MP/EXP gradients. `gauge/gray` is a solid #CCCCCC column, 1x16. `gauge/tempExp` is a 1x16 yellow-to-orange/red gradient; it is not the lime fill already in `bar`.

Active source HP/MP behavior uses `hpFlash/0` and `mpFlash/0`, 109x18, to cover the **missing** fraction on the right. It calls `Gauge::update(1 - current/max)`. Source `V87_FILL_RIGHT` crops left `109 - floor(109 * missing)` and right 0. The source draws HP at (217,51), MP at (328,51), but its MP offset is 3px to the right of the authored bar. Both flash animations contain 5 frames of 130ms, though the regular gauge uses frame0.

Source EXP is unsuitable to copy literally: its `V87_FILL` passes `(0,length)` as a crop, which removes `length` from the right; the tiling path removes each complete 1px column, drawing zero width. It also starts at a test 25% and uses screen-width-minus20 despite this classic bitmap having a 115px EXP interior. These are source UI bugs, not intentional gameplay semantics.

Current source text anchors: HP `(236,41)`, MP `(348,42)`, EXP `(464,41)`, actual level digit `(43,47)`, job `(86,31)`, player name `(86,44)`. Brackets/numbers have small +1px offsets in `UIStatusBar::draw`.

Constructor overwrite: final lower buttons are `BtShop`, `BtNPT`, `BtMenu`, `BtShort` at x=572,628,684,740 and y=36; each is54x34. The earlier `VWIDTH-350` calculation is overwritten. There are also duplicate hotkey buttons: the strip starting at (572,10) is what the normal main-button draw loop uses; another later `(235,3)` block is under separate enum identities. Do not draw both sets. Offline mapping should avoid adding online actions just because matching artwork exists.

Quickslot is4 columns x2 rows at35px pitch. Source places its image six pixels right of the lower Shortcut button, bottom-aligned plus3px, i.e. top-left `(800,-7)` for its final54x34 Shortcut at `(740,36)`. That deliberately runs outside an800px HUD panel and needs screen-space placement/clamping. Labels `key/0..7` mean Shift,Ins,Home,PageUp,Ctrl,Del,End,PageDown. Labels at image+(10+35c,9+35r), raw item/skill anchors at image+(7+35c,39+35r), the latter accounting for original item icon origins. Keep the Unity existing hotbar keys truthful if bindings remain1/2/3.

Source HUD root y is `height - 75 + 9` for widths<=1024, despite the image being71px high; this clips5px. Restoration should anchor the actual71px image flush with the bottom instead of copying that trial offset.

## Inventory

- `UIWindow.img/Item/backgrnd` is175x289. Baked: ITEM INVENTORY title, empty tab band,24 cells, meso icon/field and the word mesos. Draw the numeric balance inside the field, not another coin/title/field.
- Grid:4 columns x6 rows,32px cells,36px horizontal/35px vertical pitch. C++ starts icon anchors `(11,51)` and scales icons85%; native Unity32px icons can align to the baked cells near `(7,50)`.
- `Item/Tab/enabled/0..4` and `disabled/0..4` contain **label glyphs only**, not complete buttons. Visually verified order: Equip,Use,Set-up,Etc,Cash. Source accidentally binds label2 to ETC and3 to SETUP; fix the mapping.
- Source label anchors `(9,26)`, `(35,26)`, `(52,26)`, `(82,26)`, `(99,26)` are tightly clustered and were calculated from label widths, not full tab geometry. Compose original generic tab backgrounds across the band and center labels in their actual targets.
- `Item/FullBackgrnd` is603x289 and exists. `Item/BtFull`, `BtGather`, `BtSmall`, `BtSort` are12x12 with origin(6,6); `BtCoin`14x14 origin0. These are currently skipped by source version branches despite existing assets.
- `Item/disabled`32x32 is available for locked cells; `activeIcon`36x36 origin(2,2) is available for selection.
- No `Item/BtClose`. Source fallback `Basic.img/BtClose3` is also absent. Use existing `Basic.img/BtClose`12x12, e.g. top-right `(157,5)`.
- Source meso text is right-aligned at x15, causing it to extend out of the field. Place balance within the visible authored field (approximately x25..136 at y268..281).

## Equipment

`UIWindow.img/Equip/backgrnd`175x291 has its title, mannequin, slot labels and all cell backgrounds baked. No tabs needed. `pet` is itself a177x181 bitmap; source erroneously loads `pet/0` and gets no image. Main restoration need not expose unimplemented pet actions.

Use32x32 cells on a33px grid with top-left `(5 + 33*c, 35 + 33*r)`:

| Slot | c,r | Slot | c,r |
| --- | --- | --- | --- |
| Hat | 1,0 | Medal | 0,1 |
| Face | 1,1 | Ring1 / Ring2 | 3,1 / 4,1 |
| Eye | 2,2 | Ear / Shoulder | 3,2 / 4,2 |
| Cape | 0,3 | Top | 1,3 |
| Pendant | 2,3 | Weapon / Shield | 3,3 / 4,3 |
| Gloves | 0,4 | Bottom / Belt | 1,4 / 2,4 |
| Ring3 / Ring4 | 3,4 / 4,4 | Shoes | 2,5 |
| Tamed mob | 0,6 | Saddle / Mob equip | 1,6 / 2,6 |
| Pet MP | 4,5 | Pet HP | 4,6 |

The source uses y34 and draws scaled icons at an extra(4,4), and mistakenly places Saddle in row7 outside the baked grid. Do not copy that row. No `Equip/BtClose` exists; use shared close. `Equip/BtDetail`54x18 is an authored pet-equipment action, not an empty generic button.

## Skills

`UIWindow.img/Skill/backgrnd`175x289 has SKILL INVENTORY title, blank tab band, Skill Book badge, book-name dark panel, bottom Skill Point label and numeric field baked. `skill0` and `skill1` are141x35 authored row surfaces; `skillBlank` does not exist. `line`141x1 is available.

Source narrow layout is4 visible rows at `(10,93+40*r)`, with icon metadata anchor +`(2,2)`, name +`(38,-5)`, level +`(38,13)`. `BtSpUp`12x12 sits around `(135,113+40*r)`. Use text-fit/clipping: source caps names to97px. Rows4+ need scrolling or paging; source only enables scrollbar above12 skills, which is wrong for its4-row display.

`Skill/Tab/enabled/0..4` and disabled are tiny star/Roman-numeral labels, requiring generic tab surfaces and explicit placement. Source creates them at(0,0), on the title. Enabled tab0 origin(3,3) differs from disabled tab0 origin0. No `BtClose`; use shared close.

## Shop

`UIWindow.img/Shop/backgrnd`463x339 contains two portrait/header panes, blank tab strips, and **five** baked item rows per side. Top portrait panes end near84px. Tabs occupy approximately92..117. List rows start126 and repeat39px (126,165,204,243,282), ending321px. Source modern9-row start124/pitch42, x488 scrollbar, mesos x493, and default-position buttons do not fit this texture.

`Shop/select`162x35 exists; `select2` does not. `BtBuy`, `BtSell`, `BtExit` are80x18, `BtRecharge`30x16. All have origin0, so put them explicitly in the header panes or the authored companion controls. Tabs are separate text sprites, not baked into the background and not positioned by origin. Do not overwrite the native row separators or add6 rows without intentionally changing the list area.

`TabSell` label0..4 use the same Equip,Use,Setup,Etc,Cash ordering (extra labels5,6 also exist). `TabBuy` labels0..3 are a separate family. Display item icons at native size within left35px of each row, names/prices in the adjacent162px row area, with count glyphs when applicable. NPC portrait on left and current character on right are separate from the baked shadows. The upper right meso icon and field are baked already.

## Generic authored frames and controls

- `Basic.img/Notice3`: `t`266x21 blank blue title, `c`266x20 body strip, `s`266x55 bottom. Good for vertical variable-height companions with custom content. All origins0. Preserve corner/edge pixels; tile or stretch only body height.
- `Basic.img/Notice2`: t258x40,c258x20,s258x47. `Notice4`: t266x21,c266x20,s266x78, also s_line266x84. `Notice` fixed264x132; `YesNo` fixed264x132.
- `UIWindow.img/UtilDlgEx`: t529x28,c529x20,s529x58, plus original BtNext/BtPrev/BtOK/BtYes/BtNo/BtClose and namebar121x19. Original NPC dialogue family. Top/body/bottom source drawing is sequential; body is variable height.
- For **custom text** controls, compose `Basic.img/Tab2/left0`, `fill0`, `right0` (4x19,1x19,4x19) and state1 counterparts. These are blank authored skins, so no baked text removal is needed. Family `Tab` is24px high; `Tab3`21px; `Tab8`17px. Center live text over the resulting width. Disabled/selected/hover state selection should stay visually distinct.
- Standard baked-word buttons: `Basic.img/BtOK`65x24, BtCancel/BtYes/BtNo and numbered variants. Use only when their wording matches the action.
- Close X: `Basic.img/BtClose`12x12 or `BtClose2`14x14. `UIWindow.img/BtUIClose` and BtUIClose2 are32x15 **worded** close buttons.
- Original scrollbar: `Basic.img/VScr/{enabled,disabled}`. All main sprites12x12: enabled `base`, `prev0/1`, `next0/1`, `thumb0/1`; disabled `base`, `prev`, `next`. Not MapleButton normal/pressed hierarchy.

## Inspection artifacts

`Tools/Research/ui_nx_inspect.py` reads actual PKG4 nodes, metadata and bitmap bytes without Unity. Tested against UI.nx using Anaconda Python's installed Pillow and lz4.block. CLI exports selected subtrees as hierarchy-preserving PNGs plus metadata.json; does not modify NX or runtime files. `UI-actual-metadata.json` is a bounded sample of important roots and glyph metadata. Temporary inspection images are under `Temp/UIResearchProbe`; main agent owns final contact sheets, UI implementation and Unity validation.
