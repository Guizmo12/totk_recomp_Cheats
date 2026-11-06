# Verification

Verified on the user's running TOTK 1.2.1 recomp, release 0.1.1+701bd62.

- The WPF app builds with zero warnings and zero errors.
- A second launch exited and focused the existing app; exactly one trainer process remained.
- Twenty logic/native-callback checks passed. Fake-memory checks are not claims of live gameplay testing.
- The real app's search, favorites, navigation, startup with every cheat off and spawn availability checks passed.
- A real movement-speed write read back correctly and restored the original bytes exactly.
- The health shortcut refilled Link's displayed hearts. The emergency shortcut returned the app to zero active cheats.
- A real apple appeared in front of Link with an in-game pickup prompt. A subsequent two-apple batch, triggered by the spawn shortcut, produced two independently verified loaded actors.
- A short horizontal teleport moved Link, and the return operation brought him back within the verification tolerance.
- A save backup was completed before the first native callback was installed.

Limits: no fall damage is unavailable. High jump is experimental; its pointer chain and scaling logic are checked, but controller and jump behavior have not been fully exercised in gameplay. Stamina, battery, arrows, rupees and equipped durability pointers were validated against the running game; every gameplay interaction involving them has not been tested. Actor types other than the tested apple remain experimental. Inventory-only entries without world actors cannot be spawned physically.

## Arrow pickup fix — v0.1.1

The single-arrow entry previously spawned the flying projectile NormalArrow. It now resolves to the collectible Obj_ArrowNormal_A_01. The corrected actor was created in the running game, and the user confirmed it worked. Regression checks cover that alias and unchanged ordinary item IDs. The 5- and 10-arrow bundles were added separately; their quantities were checked against the game’s ArrowBundle_x5/ArrowBundle_x10 PouchContentParam records (BundleActorNum 5/10). Bundle pickups have not yet been tested live.


## Map categories — v0.4.0
Release build and 30 logic/native-callback checks passed. New checks cover multi-category selection, search/category/layer intersection, empty-selection persistence, and preservation of existing preferences. Native WPF review covers 69,299 points, 354 filters, category search, clearing stale altitude, cluster-bounded rendering, and all four tabs at four sizes and 125% offscreen rasterization. Actual mouse-driven Hide all and the Apple category produced the expected zero/1,086 Surface points. Final checks.json reports measured marker-build time separately from overall layout time; cached drawings are reused for live player updates. Gameplay teleport/spawn reports above are historical and were not rerun for this UI/data update.


## Simplified reference categories — v0.4.1
32 logic/native-callback checks passed, including the exact seven-group, 49-category taxonomy and reference zoom thresholds. Native WPF checks cover hidden compendium entries in the normal panel, separate compendium search, and fewer than 100 drawn overview markers with all main categories enabled. All four screens were reviewed at the existing window sizes. No gameplay memory writes were required.


## Dual recomp profiles — v0.5.0
36 logic/native-callback checks passed. Both profiles validate all three native entry signatures; altered signatures and unknown hashes are refused. On the running v0.3.11 / game 1.2.1 build, all cheat pointer checks passed; speed changed and restored exactly, a short teleport returned Link to his starting position, and one verified apple displayed a collectible pickup prompt (baked by Fire Temple heat). Clean disconnect/reconnect revalidated the restored native entry bytes. Original-build gameplay evidence remains historical; its hash, native addresses and callback regression checks are retained. See COMPATIBILITY.md.
