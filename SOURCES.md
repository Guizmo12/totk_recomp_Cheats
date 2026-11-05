# Sources and credits

- Game architecture and recomp: [valentderah/totk-pc-recomp](https://github.com/valentderah/totk-pc-recomp). Guest module base and CPU calling convention were checked against its runtime. This trainer is independent of that project.
- TOTK 1.2.1 actor creation, transform ABI and engine offsets: [aquacluck/totk-lotuskit](https://github.com/aquacluck/totk-lotuskit), inspected at `59dcfcc`. Its GPL Switch plugin is not copied into or loaded by this application. The trainer has its own Windows frame callback and communication code.
- Item names and icons: [Marc Robledo's TOTK save editor](https://github.com/marcrobledo/savegame-editors/tree/master/zelda-totk). Name contributors include Echocolat, Exincracci, HylianLZ and Karlos007. The repository's MIT license is included as CATALOG-LICENSE.txt. Original game artwork belongs to Nintendo.
- Cheat pointer research: Eiffel2018 and contributors, as collected in [HamletDuFromage/switch-cheats-db](https://github.com/HamletDuFromage/switch-cheats-db) for build ID `9B4E43650501A4D4`.
- Movement/jump research cross-checked against [Arch9SK7/MyNXCheats](https://github.com/Arch9SK7/MyNXCheats).
- Visual direction inspired by the user's Wand screenshot, with original WPF styling and geometric artwork. No Wand code or branding assets are included.

The executable hash gate for native spawning and travel is:

`650ac3c2653a54d80fee0b8f69d58bc1f985ac2cfa81ea22b8ee8bafee69f052`

Other recomp builds are deliberately not treated as compatible with the native callback.


## Interface 0.2
Direction artistique et décor : HYRULE_COMPANION_CODEX_PACK fourni par l’utilisateur. Symboles SVG adaptés en dessins WPF, illustrations du bandeau embarquées. Polices système Georgia et Segoe UI. Les images d’objets réelles conservent leur attribution et leur licence existantes.


## Interactive map 0.3.0
Basemap artwork © Nintendo, sourced from vguzman812/totk-coordinates-map (MIT notice in MAP-RESOURCE-LICENSE.txt). Published landmark coordinates and game text: objmap-totk.zeldamods.org game_files. Native map renderer and UI written for this application; Zelda Dungeon supplied the visual reference. See TRAVEL.md.
