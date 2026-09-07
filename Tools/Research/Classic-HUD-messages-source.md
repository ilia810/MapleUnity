# HUD message strip and history — 2026-09-07

This pass addresses the remaining message-area differences in the user's Classic references: the All label at the left of the white strip, compact controls at its right, a disabled envelope near the toolbar, translucent history above the HUD, and the gray notice state. It builds on the complete keyboard remapping and preserves independent windows.

## Reference and NX mapping

The reference HUD spans roughly 1200 pixels in the 2048×1152 video captures, corresponding to the current 800-pixel native HUD at approximately 1.5× scale. The message area is about 566 native pixels wide. Images 3–9 show the All/Friend tag and white field, while image 1 shows the gray notice state. The retained user images and NX archives are unchanged.

| Element | Local source and treatment |
| --- | --- |
| All tag | `StatusBar.img/base/chatTarget`, native 81×20, at (3,6) |
| White strip | Existing `StatusBar.img/base/backgrnd`; message starts at (90,5) |
| Collapse/expand | `UIWindow.img/SoftKeyboard/Bt/0/BtMin` / `BtMax`, native 12×12, at (535,10) |
| Strip arrows | `Basic.img/VScr4` enabled/disabled previous/next art, 15×13, at (551,4)/(551,17) |
| History | 566-pixel translucent panel at the HUD left, with original 566×5 `StatusBar.img/base/chat` as its resize edge |
| History scrollbar | Shared VScr4 tiled track, native arrows and fixed 15×25 thumb |
| Disabled envelope | First 20×19 button well from `StatusBar.img/base/box`; a 14×11 pixel-grid UI glyph matches the reference envelope silhouette |
| Notice state | Gray field with pink single-line notice text; restores All/white presentation for the next ordinary message |

The actual `StatusBar.img/BtWhisper` is a 12×19 selector arrow and `base/iconMemo` is a colored memo symbol, rather than the newer gray envelope shown in the references. Neither is mislabeled as an exact match. The envelope uses simple runtime UI geometry inside the native button well; no new bitmap or edited NX asset is introduced. Its button stays disabled in local play.

Read-only exports and dimensions are in `Logs/hud-message-assets` and `Logs/hud-message-controls`. The reference C++ paths are `C:/HeavenClient/MapleStory-Client/IO/UITypes/UIStatusBar.cpp` and `UIChatBar.cpp`. The working tree contains mixed-version fallback chat layouts; it confirms channel/message grouping, the VScr4 arrows/track, resize behavior and separate input/history areas. This pass follows the supplied Classic reference geometry rather than its fallback dimensions.

## Behavior and layout

`ClassicMessageHistory` owns the view; `StatusBar.PostMessage` keeps its public API and delegates to it. EXP and level messages retain yellow text, action notices use pink, and ordinary messages/pickups use white. Actual pickup events now also enter history. Explicit line breaks and wrapped lines remain readable; source text is escaped before our rich-text colors are applied.

The history retains the latest 50 records. Its real ScrollRect clips content to the viewport, supports wheel/arrow/thumb scrolling and starts at the newest message. New messages follow the bottom only when the reader is already there. While reading earlier records, the current offset is retained; pruning the oldest record adjusts that offset by the removed record's wrapped height. Travel retains the history.

Drag the native top edge to resize. Its glove affordance follows the existing grab states. Height stays within the current display, and all strip controls remain inside the HUD at both wide and small sizes. Hiding the history retains its messages and scroll position; its native toggle switches to the expand artwork. The white strip abbreviates overlong messages with an ellipsis and exposes the full current message on hover. Full text remains in history. Floating notifications also abbreviate to their bounded two-line area rather than silently clipping mid-message, and refit if the display width changes.

The All label describes the existing local message history. It is not a simulated online channel picker or input field. Network chat, whispers and friend/party communication stay outside the user's current offline scope. Collapse/resize state lasts for the current session; this pass does not change character saves or write new UI preferences.

## Validation

The initial compatibility run passed 2/2 existing UI scene checks (`Logs/hud-messages-existing.xml`). The focused run passed 8/8 UI/history/keyboard/cursor cases (`Logs/hud-messages-focused.xml`), including real pointer raycasts, native controls, wrapped/colored text, scroll position retention, the 50-record limit, drag resizing, thumb input, collapse/expand, literal markup, actual EXP/pickup events and independent windows. Captures at 1366×768 and 640×480 were inspected. The broader regression passed 49/49 scene cases (`Logs/hud-messages-scenes.xml`). The final envelope render correction passed 8/8 affected history/UI/keyboard/quickslot cases (`Logs/hud-messages-final.xml`), including an actual rendered-mesh check. Final wide and small captures show the envelope and bounded notices correctly. Windows player compilation passed with zero C# errors (`Logs/hud-messages-player-verified.log`). All 396 C# sources have metadata and match the isolated validation snapshot. The original Unity editor and scene remain untouched.

Remaining visual work includes comparison with matched map/outfit/UI states, refinements tied to unported classes/NPC scripts, and additional art tied to gameplay not yet ported. The [quest helper AUTO control](Classic-quest-auto-source.md) is implemented in the following pass. Online play and the pre-existing unrelated failing suite remain deferred.
