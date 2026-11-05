# Interactive Travel — version 0.4.1

The normal map filter panel matches Zelda Dungeon’s **49 main categories in seven groups**:

- **Travel Gates:** Skyview Tower, Shrine of Light, Lightroot, Tech Lab, Dungeon, Other Travel Gate.
- **Quests:** Main Quest, Side Adventure, Shrine Quest, Side Quest, Quest Objective, Dragon’s Tear, Stone Tablet.
- **Collectibles:** Korok Seed, Treasure Chest, Sage’s Will, Old Map, Armor, Unique Weapon, Yiga Schematic, Large Zonaite Deposit.
- **Locations:** Village, Chasm, Cave, Well.
- **Services:** Stable, Inn, General Store, Armor Shop, Bargainer Statue, Koltin Balloon, Other Shops, Great Fairy, Device Dispenser, Goddess Statue, Cooking Pot.
- **Other:** Hudson Sign, Minigame, Cherry Tree, Ascend Point.
- **Bosses:** Flux Construct, Hinox, Stone Talus, Frox, Gleeok, Lynel, Molduga, Gloom Spawn, Dungeon Boss.

Your saved places remain in a separate personal group. **All** enables the main categories. **None** clears the map filters. Use the category checkboxes together, search within the panel, or show/hide an entire group. Counts refer to the selected layer. Choices save automatically. The main filter button counts the 49 reference categories, not individual resource types.

## A clearer map

The overview shows major destinations. Finer markers appear as you zoom in, using the reference dataset’s published minimum zoom levels. The scale conversion uses Zelda Dungeon’s published 564-pixel tiles over a 12,032-coordinate world. Major markers remain visible at Fit. Drag to pan; wheel or +/- zoom; Fit resets the view; Locate centers on Link. Numbered clusters split when clicked; a cluster never triggers teleporting. The higher maximum zoom allows chests and other fine detail to be reached.

**Compendium** is a separate, optional section for individual materials, equipment, monsters and creatures. It starts off and never appears as hundreds of entries in the main category list. Additional place/supplemental data are also kept in that optional section. The offline database retains 69,299 point records, so this simplification does not discard data. Personal places remain available.

## Teleporting

Click an individual marker or use the destination search/list to populate its coordinates. Double-click an individual marker to teleport, or enable single-click travel. Published altitude is supplied when available; unmeasured points clear the previous altitude and require manual entry. Changing coordinate format also works before altitude is entered. Original travel-gate coordinates retain their previously tested clearance. Other points use their recorded object position; they are not guaranteed landing platforms.

Engine coordinates: X east, Z south, Y height. HUD coordinates: east, north, altitude. Conversion: engine X = HUD X, engine Z = -HUD Y, engine Y = HUD Z + 105. Finite coordinates between ±100,000 are accepted; map coverage is X ±6,000 and Z ±5,000. A layer change does not measure terrain height. Save current or custom destinations as named places; Undo returns to the pre-trip position. Travel does not unlock gates or enter separate shrine interiors. Arbitrary-coordinate travel remains experimental.

## Verification and attribution

Version 0.4.1: Release build; 32 logic/native-callback checks, including the exact seven/49-category taxonomy and zoom thresholds; native WPF checks for separate compendium search, uncluttered overview, filter composition, altitude validation, coordinate formats and clustering; all four tabs at four sizes and 125% offscreen rasterization. No gameplay writes are needed for these checks. A real ~2.8 km trip and Undo were verified in version 0.3.0; that evidence is historical.

Data: [Zelda Dungeon TOTK map](https://www.zeldadungeon.net/tears-of-the-kingdom-interactive-map/), published main markers, wiki categories, supplemental markers and compendium locations; snapshot November 5, 2026. Only factual point names, coordinates, categories and minimum zoom levels are retained. No website code, guide descriptions, login state or remote runtime is embedded. Game artwork/data © Nintendo. Basemaps from [vguzman812/totk-coordinates-map](https://github.com/vguzman812/totk-coordinates-map); its MIT notice is in MAP-RESOURCE-LICENSE.txt. Original travel-gate positions were cross-checked against zeldamods. Snapshot counts and source digests are in Resources/Maps/manifest.json in the source tree.
