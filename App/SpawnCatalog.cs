namespace HyruleCompanion;

public static class SpawnCatalog {
 // Inventory identifiers and world-object names are not always interchangeable.
 // NormalArrow is the flying projectile; this object is the collectible arrow.
 public static string WorldActor(string itemId)=>itemId switch {
  "NormalArrow"=>"Obj_ArrowNormal_A_01",
  _=>itemId
 };
 public static int UnitsInPickup(string itemId)=>itemId switch {
  "Obj_ArrowBundle_A_01"=>5,
  "Obj_ArrowBundle_A_02"=>10,
  _=>1
 };
}
