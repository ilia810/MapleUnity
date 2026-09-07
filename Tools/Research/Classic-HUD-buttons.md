# Classic HUD buttons — original artwork with GIMP effects, 2026-09-07

The current art preserves the original icons, caption pixels, placement and
colors. The three normal PNGs are restored byte for byte from commit
`afe2633853c884a38c338d0561d29b5d1d250ac4`. The geometric redraw, replacement font
and palette changes from the first GIMP pass have been removed after review.
The original screenshot-derived softness is retained.

GIMP 3.2.4 was used through its Python batch API to edit interaction effects:
a soft narrow hover rim, an inset pressed bevel with the existing one-HUD-pixel
right/down depression, and a readable grayscale disabled state. The original
caption and icon pixels are reused, not retyped or redrawn. Hover no longer
applies the older diffuse white halo over the content.

Three layered XCF files, four state groups per button, effect layers and a live
disabled-state desaturation filter are retained under
[`ArtSource/HudButtons/`](../../ArtSource/HudButtons/README.md). The captions are
original raster art, not editable replacement-font text. This was a GIMP batch
edit, not manual painting or AI image generation.

- [Original art / current interaction states](../ArtReferences/MenuButtons/gimp-cleanup-comparison.png)
- [Actual 74 × 34 display size](../ArtReferences/MenuButtons/gimp-cleanup-actual-size.png)
- [Static HUD-context mockup](../ArtReferences/MenuButtons/gimp-cleanup-context.png)

All twelve PNGs remain 148 × 68, 8-bit RGBA with the exact original alpha masks.
Normal-state PNG bytes and source/export visible pixels were checked. Runtime
paths and `.meta` GUIDs are unchanged. No Unity runtime or C# changes were made.
The Windows PC still needs to review this revision in-game; the tests below
belong to the historical baseline. Do not run the old PowerShell builder over
these exports, as it would overwrite the revised effects.

## Historical screenshot-crop baseline — 2026-09-07

The Cash Shop, Menu and Short Cut controls now use complete 74 × 34 faces matching
the user's nine screenshots in `Tools/ArtReferences/MapleClassic`. This supersedes
the older narrow-sprite column extension in `Classic-HUD-reference.md`.

`Tools/Build-ClassicHudButtons.ps1` builds the 12 imported PNGs under
`Assets/Resources/UI/ClassicHudButtons`. Normal artwork comes from the cleanest
supplied frame, `09-character-stats.png` (3840 × 2160), at these pixel rectangles:

| Button | X | Y | Width | Height |
| --- | ---: | ---: | ---: | ---: |
| Cash Shop | 2409 | 2062 | 206 | 96 |
| Menu | 2618 | 2062 | 206 | 96 |
| Short Cut | 2826 | 2062 | 207 | 96 |

Each crop is stored at 148 × 68 with transparent stepped corners and rendered at
74 × 34. Icons and lettering remain together with their authored proportions.
Uncompressed, mip-free textures keep the small captions readable. The nine
reference screenshots are unchanged.

The screenshots show the resting state. The additional states follow the local
UI.nx button effects: a bright icon/caption halo on hover, a reversed bevel and
one-pixel depression on press, and muted grayscale when disabled. These states
are reconstructed, not claimed to have been captured from the reference video.
Keyboard focus shares the hover artwork. Standard Unity Button activation and
hit routing retain the existing three actions; `ClassicHudButton` distinguishes
mouse selection from keyboard focus so a clicked button restores on pointer exit.

Browser workflow: JS Paint opened successfully. Loading the supplied local PNG
through its Open command failed because the browser extension rejected local
file access. No Paint-edited asset was saved; the reproducible local builder was
used for the final art.

Validation: all five focused scene tests passed. `ClassicHudButtonSceneTests` exercises pointer enter/down/up/exit,
release outside, focus, right clicks, disabled activation, hiding while pressed,
keyboard submit and the three actual actions. It captures normal, hover, pressed,
disabled, 640 × 480, 800 × 600 and 1366 × 768 layouts. Existing classic UI and
quickslot scene tests run alongside it. Player script compilation also passed
with zero C# errors. Artifacts: `Logs/menu-button-redesign`; `comparison.png`
shows the previous buttons and all four updated states rendered in Unity.
