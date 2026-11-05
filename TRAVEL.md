# Interactive Travel — version 0.3.0

Travel now contains a native, offline interactive map for Surface, Sky and Depths. Pan by dragging; zoom with the wheel or +/-; Fit shows the full world. The target crosshair and Link's horizontal position are drawn as native vector markers. Locate centers the map on Link.

## Teleporting

Click a map location to populate the destination; double-click to teleport. Enable **Teleport on single map click** to teleport on a single click instead. For arbitrary points, enter an altitude first. Select a landmark from the map or searchable destination list to use its published position with a 2-meter vertical clearance. Landmark coordinates are location markers, not guaranteed safe landing platforms.

There are 287 built-in destinations: 152 shrines, 15 Skyview Towers and 120 lightroots. Filter by type or search by name. Custom coordinate destinations and current positions can be saved as named places. **Undo last teleport** returns to the position before the last successful teleport.

The artificial 2,000-meter limit is removed. Finite engine coordinates between -100,000 and +100,000 are accepted; map coverage is X -6,000…6,000 and Z -5,000…5,000. This is coordinate teleportation in the current overworld; it does not unlock the game's travel gates or enter separate shrine/dungeon interiors.

## Coordinate formats

- **Engine coordinates:** X is east/west, Z is south/north, and Y is vertical altitude. North is negative Z.
- **In-game HUD coordinates:** the three displayed values are east, north and altitude. The conversion is engine X = HUD X, engine Z = -HUD Y, engine Y = HUD Z + 105. Changing the selector preserves the chosen destination.

A 2D map does not measure ground height. A custom point keeps the altitude you entered. The Surface/Sky/Depths buttons change the map image, not the altitude. Incorrect altitude can place Link in the air, underground or inside geometry. Long-distance streaming and arbitrary destinations remain experimental; Undo and save backups remain available.

## Verification

- Release build succeeded; 26 logic/native-callback checks passed.
- Map calibration, zoom coordinate round trips, layer availability, landmark search, invalid-altitude refusal, arbitrary-coordinate parsing and HUD conversion passed.
- Four-screen visual/layout checks passed at 1440×920, 1600×900, 1920×1080, 1120×720 and native WPF rasterization at 125%.
- Actual UI-driven long-distance teleport (~2.8 km) reached the requested coordinates; the destination area loaded; Undo returned Link to the pre-trip position. The initial background startup check had not moved Link. Travel now waits for the game to receive foreground focus before issuing its request.
- This is evidence for the tested trip, not a claim that every destination or height is safe. No coordinate request is reported successful without checking the player's position.

## Map resources

The basemaps and game text are Nintendo game artwork/data. The basemap image files and their calibration were obtained from [vguzman812/totk-coordinates-map](https://github.com/vguzman812/totk-coordinates-map); the upstream MIT notice is included as MAP-RESOURCE-LICENSE.txt. Landmark positions/names are from the published [zeldamods object map game files](https://objmap-totk.zeldamods.org/game_files/map_summary/MainField/static.json), with shrine names from its StaticMsg/Dungeon data and other names from StaticMsg/LocationMarker. The map renderer and controls are original native WPF code. No Zelda Dungeon website code, service requests or user account is embedded. Zelda Dungeon was used as the visual/interaction reference.
