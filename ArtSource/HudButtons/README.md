# Classic HUD buttons — preserve the original art, clean up the effects

Artwork branch: `art/hud-buttons-gimp`

Original-art commit: `afe2633853c884a38c338d0561d29b5d1d250ac4`

Edited with GIMP 3.2.4 on macOS, 2026-09-07.

## Artwork fidelity

The original icons, raster captions, placement and normal-state colors are
preserved. The three normal PNGs are restored **byte for byte** from the original
commit above. Captions were not retyped or resized. The gold coins and notebook
retain their soft original shapes, and the Short Cut windows retain their
original colors. This supersedes the first redraw, whose geometric icons,
larger replacement type and altered palette did not match the requested style.

The original art is the reference-derived crop documented in
`Tools/Research/Classic-HUD-buttons.md`. This revision changes interaction effects
through GIMP's native layer, selection, fill and filter APIs. It is a GIMP batch
edit, not manual painting or AI image generation. The source screenshot's
existing softness remains; no new sharp detail is invented.

## Editable GIMP sources

`BtShop.xcf`, `BtMenu.xcf`, and `BtShort.xcf` are 148 × 68 layered documents.
Each has four named state groups, with only NORMAL visible when opened:

- **NORMAL:** one preserved original-art layer.
- **HOVER / FOCUS:** original art, an 18% soft inner-rim light and an 8% upper
  reflection. The caption and icon are not washed out by a diffuse white halo.
- **PRESSED:** fixed original frame; an interior copy shifted two texture pixels
  right/down; 8% face shade; 25% inset top-left shade; 18% lower-right reflection.
- **DISABLED:** original art with a live luminance-desaturation filter and a 28%
  neutral-gray veil to reduce contrast without obscuring the caption.

Toggle exactly one group on before exporting. Effect layers and their opacity
can be adjusted independently. Icons and captions remain original raster art;
they are not font-dependent text layers. The unmodified source remains available
in NORMAL. Re-exporting NORMAL in GIMP preserves all visible pixels, although
GIMP may clear RGB values under fully transparent corners; runtime NORMAL uses
the original PNG bytes to avoid even that invisible change.

## Runtime handoff and checks

Existing paths: `Assets/Resources/UI/ClassicHudButtons/`. Nine interaction PNGs
are revised; the three normal PNGs match the original commit. All images are
148 × 68, 8-bit RGBA, displayed at 74 × 34. Every state's alpha silhouette exactly
matches the original. The pressed interior shifts by one HUD pixel, while the
frame remains stationary. Existing `.meta` files and GUIDs are unchanged. No
new Unity paths, metadata, import settings or C# changes are needed.

Proofs under `Tools/ArtReferences/MenuButtons/`:

- `gimp-cleanup-comparison.png`: original normal art and all current states at 3×.
- `gimp-cleanup-actual-size.png`: all states at 74 × 34; inspect at 100% zoom.
- `gimp-cleanup-context.png`: static GIMP composite over the supplied screenshot,
  **not a Unity runtime capture**.

Checks cover original normal PNG bytes, visible source/export pixel agreement,
PNG format and dimensions, unchanged alpha masks across all states, distinct
interaction images, GIMP source groups and live disabled filter, and unchanged
Unity metadata. Actual-size and enlarged proofs were visually inspected.

The Windows PC should review the artwork and run the existing
`ClassicHudButtonSceneTests`, including mouse/keyboard focus, press/release,
disabled states and the small HUD. This revision has not been run in Unity on
this Mac.

## Rebuilding with GIMP

From the repository root, run the Python file inside GIMP:

```sh
/Applications/GIMP.app/Contents/MacOS/gimp-console \
  --new-instance --no-interface --no-data --no-fonts \
  --batch-interpreter=python-fu-eval \
  --batch="exec(open('ArtSource/HudButtons/build_gimp.py').read())" --quit
```

It reads the original normal PNGs from Git and writes all states and XCFs into
`ArtSource/HudButtons/generated/` for review. It does not overwrite the saved
sources or runtime exports. Later manual XCF edits cannot be retained by this
rebuild; open the saved XCFs when refining the existing layers.

To update the proof images from runtime exports:

```sh
/Applications/GIMP.app/Contents/MacOS/gimp-console \
  --new-instance --no-interface --no-data \
  --batch-interpreter=python-fu-eval \
  --batch="exec(open('ArtSource/HudButtons/preview_gimp.py').read())" --quit
```

Only the proof's annotation text uses Arial Bold; the game captions are original
raster pixels. Do not run `Tools/Build-ClassicHudButtons.ps1` over this revision:
it would restore the older interaction effects.
