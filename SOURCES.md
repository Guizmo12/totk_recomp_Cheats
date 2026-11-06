# Sources and credits

- Game architecture and recomp: [valentderah/totk-pc-recomp](https://github.com/valentderah/totk-pc-recomp). Guest module base and CPU calling convention were checked against its runtime. This trainer is independent of that project.
- TOTK 1.2.1 actor creation, transform ABI and engine offsets: [aquacluck/totk-lotuskit](https://github.com/aquacluck/totk-lotuskit), inspected at `59dcfcc`. Its GPL Switch plugin is not copied into or loaded by this application. The trainer has its own Windows frame callback and communication code.
- Item names and icons: [Marc Robledo's TOTK save editor](https://github.com/marcrobledo/savegame-editors/tree/master/zelda-totk). Name contributors include Echocolat, Exincracci, HylianLZ and Karlos007. The repository's MIT license is included as CATALOG-LICENSE.txt. Original game artwork belongs to Nintendo.
- Cheat pointer research: Eiffel2018 and contributors, as collected in [HamletDuFromage/switch-cheats-db](https://github.com/HamletDuFromage/switch-cheats-db) for build ID `9B4E43650501A4D4`.
- Movement/jump research cross-checked against [Arch9SK7/MyNXCheats](https://github.com/Arch9SK7/MyNXCheats).
- Visual direction inspired by the user's Wand screenshot, with original WPF styling and geometric artwork. No Wand code or branding assets are included.

The original 0.1.1 executable hash gate was:

`650ac3c2653a54d80fee0b8f69d58bc1f985ac2cfa81ea22b8ee8bafee69f052`

Unknown recomp executables are refused; version 0.5.0 adds the separate verified 0.3.11 profile described in COMPATIBILITY.md.


## Interface 0.2
Direction artistique et décor : HYRULE_COMPANION_CODEX_PACK fourni par l’utilisateur. Symboles SVG adaptés en dessins WPF, illustrations du bandeau embarquées. Polices système Georgia et Segoe UI. Les images d’objets réelles conservent leur attribution et leur licence existantes.


## Interactive map 0.3.0
Basemap artwork © Nintendo, sourced from vguzman812/totk-coordinates-map (MIT notice in MAP-RESOURCE-LICENSE.txt). Published landmark coordinates and game text: objmap-totk.zeldamods.org game_files. Native map renderer and UI written for this application; Zelda Dungeon supplied the visual reference. See TRAVEL.md.


## Expanded map 0.4.0
Factual point names, coordinates and category coverage: Zelda Dungeon, https://www.zeldadungeon.net/tears-of-the-kingdom-interactive-map/ (published Surface/Sky/Depths markers.json, objects.json and wiki map categories/supplemental markers; snapshot 2026-11-05). Dataset normalized for this offline application; source digests and per-category counts are included in Resources/Maps/manifest.json. Original game names/data © Nintendo. No website source code, guide descriptions, login state or website icons are copied. Previously verified travel-gate coordinates retain their original zeldamods-derived landing positions. Earlier visual-reference-only attribution describes version 0.3.0.


## Recomp 0.3.11 compatibility
Official release: https://github.com/valentderah/totk-pc-recomp/releases/tag/v0.3.11. Guest CPU layout and native calling convention checked against the release’s runtime/cpu.h and the user’s installed setup. Native function addresses and entry bytes were inspected in the installed executable’s symbols; independent profiles retain the original 0.1.1 mapping. Profile definitions are in RecompBuild.cs. No runtime source code is copied into the trainer.
