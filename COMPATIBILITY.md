# Recomp compatibility — Hyrule Companion 0.6.3

Hyrule Companion supports the Windows executable profiles below. All five profiles use **TOTK game update 1.2.1**, build ID `9B4E43650501A4D4`. Recomp releases and Nintendo game updates are separate version numbers.

Choose the game executable in Settings and connect. The app identifies its SHA-256 automatically, selects the matching native addresses, and shows the recomp version in the connection badge. Favorites, shortcuts, saved places, map filters and other preferences continue to use the same settings file.

## Verified Windows executables

| Recomp | Executable SHA-256 |
| --- | --- |
| 0.1.1+701bd62 | `650ac3c2653a54d80fee0b8f69d58bc1f985ac2cfa81ea22b8ee8bafee69f052` |
| 0.3.11+0ae47a5 (installed v3 build) | `4aec5cd94e9deb7ffd9b3cbe714a8f56a01ea28af845bfef531c0ab1ba8196fe` |
| 0.4.0+7a37fc3c (tested v3 build) | `313be5bc2c0aad4fbf9688047ef43e91782b8e62c014924923e2b955584904a9` |
| 0.4.1+6151ac4f (tested v3 build) | `161cdd4c029a50d636cb46f7f295762bc52ed7c2dfd66b9990ff68e0a2aa8132` |
| 0.5.0+afae80c9 (tested v3 build) | `3dc3c334ac1143c27bcc3debc5fb78090ae4c5f69a61542957c04c34ebe89250` |

The installer compiles the executable locally. Other compiler/CPU variants or rebuilt executables can have different hashes and need their own verified profile. Unknown builds are refused; the app never falls back to another release’s native addresses. Game update 1.4.3 is outside these profiles.

Each profile also validates the in-memory entry bytes of all three native functions before attaching: the frame callback, item creation and transform setter. A modified or mismatched function is refused before the extension writes memory. The guest CPU layout remains 1,024 bytes with SP at 256, PC at 264 and vector registers at 512; the callback preserves the complete state. Profile lookup, native-signature validation and game pointer checks are separate checks.

## Verification on recomp 0.4.0

All 37 automated checks passed. In live gameplay on recomp 0.4.0 with TOTK 1.2.1, the app read all eight supported cheat values. Movement speed changed and restored exactly. A short teleport moved Link and returned him to his starting position. One apple spawned with a Take prompt. The app reconnected after closing, verified all three restored native function signatures, and left every cheat off. The four-tab interface checks passed. The older profiles passed regression checks and their gameplay verification is historical.

[Official v0.4.0 release](https://github.com/valentderah/totk-pc-recomp/releases/tag/v0.4.0) and [runtime CPU layout](https://github.com/valentderah/totk-pc-recomp/blob/v0.4.0/runtime/cpu.h).

## Earlier verification on recomp 0.3.11

- Release build and 36 logic/native-callback checks passed.
- Original executable hash, native function addresses and callback behavior remain covered by regression checks. Its earlier gameplay validation remains historical; the old executable was replaced during the recomp update.
- On the running v0.3.11 / game 1.2.1 installation: all eight cheat pointer checks returned valid values; a movement-speed write read back correctly and restored its exact original bytes.
- A short teleport moved Link by 0.75 meters, then returned him to the starting position.
- One apple spawned and its loaded actor was verified. Fire Temple heat converted it to a Baked Apple, with an in-game Take prompt.
- Closing and reconnecting validated the original native function bytes again, confirming the callback was restored. Every cheat remained off after checking.
- Native interface checks cover all four tabs and the existing map, search, favorites, saved places and layout checks. A fresh save backup was made before the live compatibility checks.

Only the short trip above was retested on v0.3.11. Earlier long-distance, arrow and item tests remain historical. No fall damage remains unavailable; high jump remains experimental.

Source reference: [official v0.3.11 release](https://github.com/valentderah/totk-pc-recomp/releases/tag/v0.3.11) and [runtime CPU layout](https://github.com/valentderah/totk-pc-recomp/blob/v0.3.11/runtime/cpu.h). Native addresses were checked against the installed executable’s function symbols. No game executable, ROM, saves or personal reports are included in the download.

## Recomp 0.4.1 verification

Hyrule Companion 0.5.2 connected to the running Windows v3 build with TOTK 1.2.1 and validated all three native function signatures. It read 15,062 actor catalog entries with every cheat off. All 37 automated checks passed. Gameplay writes, item spawning and teleportation were last exercised live on recomp 0.4.0.

## Recomp 0.5.0 verification

Version 0.6.3 connected to the running Windows v3 build with TOTK 1.2.1. All three native signatures, eight cheat readbacks, the item catalog and the four-tab visual review passed. The recomp log confirmed that the existing Free Autobuild mod loaded. Installing and removing the mod passed using a test copy of the new build and matching assets. The live check used read-only access. All 38 automated checks passed.
