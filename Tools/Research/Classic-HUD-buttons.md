# Reference menu buttons — 2026-09-07

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
