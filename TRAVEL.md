# Interactive Travel — version 0.4.0

The native offline map covers Surface, Sky and Depths. It now includes **69,299 points in 354 selectable categories**, drawn from Zelda Dungeon’s published main markers, wiki categories, supplemental markers and compendium locations (snapshot: November 5, 2026). The count includes 51 main categories, 302 compendium types, and the saved-places filter; categories without source points are not fabricated. Repeated locations represent separate physical objects or entrances. 900 Korok puzzle locations can award 1,000 seeds.

## Filters

Open **Filters**. Combine any checkboxes; use the filter search to find names such as Korok, quest, well, apple or Lynel. Expand a group to show or hide its categories. Counts refer to the selected map layer. Choices save automatically with existing preferences, favorites, hotkeys and saved places.

- **Explore:** all main points of interest; resource/compendium markers off. This is the initial view.
- **All:** every supported main and compendium category.
- **None:** clear the selection, then choose individual categories.
- **Travel:** travel gates and saved destinations.

Groups include travel gates, quests, collectibles, locations, services, bosses, activities, saved places, and compendium materials, equipment, monsters and creatures. The destination search further narrows the enabled categories; the destination list follows the current layer. Filters can be closed to recover map space.

Drag to pan, use the wheel or +/- to zoom, and Fit to reset the view. Numbered clusters split as you zoom; clicking a cluster zooms in instead of teleporting. At maximum zoom, exact overlaps can be chosen through the destination list. Native vector markers use distinct colors and symbols. Marker drawings are cached between viewport/filter changes, including while Link’s position updates.

## Teleporting

Select an individual marker or destination-list entry to populate its position. Double-click an individual marker to teleport, or enable single-click teleport. A cluster never triggers travel. Published altitude is supplied when available; unmeasured points clear the previous altitude and require manual entry. Original travel-gate landing coordinates retain their previously tested clearance. Other points use their recorded object position; a map point is not a guaranteed safe landing platform.

Engine coordinates are X east, Z south and Y height. HUD coordinates are east, north and altitude: engine X = HUD X, engine Z = -HUD Y, engine Y = HUD Z + 105. The selector preserves the destination. Finite coordinates between -100,000 and +100,000 are accepted. Map coverage is X ±6,000 and Z ±5,000. Changing layers does not measure terrain or automatically choose an altitude.

Save current or custom positions as named places. Undo last teleport returns to the pre-trip position. Travel works in the current overworld; it does not unlock gates or enter separate shrine interiors. Arbitrary coordinates and long-distance streaming remain experimental.

## Verification and sources

Version 0.4.0: Release build; 30 logic/native-callback checks; category composition, search/layer intersection, empty selection and settings round trips; actual WPF filtering, altitude validation and clustering checks; all four screens at four window sizes plus 125% offscreen rasterization. Native mouse clicks also verified Hide all and a resource checkbox. No new gameplay writes were needed for this map/filter update. The real ~2.8 km trip and Undo were tested in version 0.3.0; that evidence is historical, not a claim of testing every new point.

Map data: [Zelda Dungeon TOTK interactive map](https://www.zeldadungeon.net/tears-of-the-kingdom-interactive-map/) and its published wiki map categories. Only factual point names, categories and coordinates are retained; no guide prose, website code, account state or remote runtime is embedded. Basemap artwork and original game text © Nintendo. Basemaps from [vguzman812/totk-coordinates-map](https://github.com/vguzman812/totk-coordinates-map), with its MIT notice in MAP-RESOURCE-LICENSE.txt. Original travel-gate positions were cross-checked against the published zeldamods object map. The offline snapshot counts and source digests are in App/Resources/Maps/manifest.json in the source repository.
