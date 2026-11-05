using System.Globalization;
namespace HyruleCompanion;
public record MapLocation(string Name,string Kind,string Layer,float X,float Y,float Z,int MinZoom=0);
public record MapCategory(string Kind,string Group,int Count){public string DisplayName=>Kind switch{"Tower"=>"Skyview Tower","Shrine"=>"Shrine of Light",_=>Kind.Replace("Resource: ","")};}
public static class MapFilters {
 public static readonly IReadOnlyDictionary<string,string[]> MainGroups=new Dictionary<string,string[]>{
  ["Travel Gates"]=["Tower","Shrine","Lightroot","Tech Lab","Dungeon","Other Travel Gate"],
  ["Quests"]=["Main Quest","Side Adventure","Shrine Quest","Side Quest","Quest Objective","Dragon's Tear","Stone Tablet"],
  ["Collectibles"]=["Korok Seed","Treasure Chest","Sage's Will","Old Map","Armor","Unique Weapon","Yiga Schematic","Large Zonaite Deposit"],
  ["Locations"]=["Village","Chasm","Cave","Well"],
  ["Services"]=["Stable","Inn","General Store","Armor Shop","Bargainer Statue","Koltin Balloon","Other Shops","Great Fairy","Device Dispenser","Goddess Statue","Cooking Pot"],
  ["Other"]=["Hudson Sign","Minigame","Cherry Tree","Ascend Point"],
  ["Bosses"]=["Flux Construct","Hinox","Stone Talus","Frox","Gleeok","Lynel","Molduga","Gloom Spawn","Dungeon Boss"]
 };
 public static bool IsPrimary(string kind)=>MainGroups.Values.Any(kinds=>kinds.Contains(kind));
 public static bool VisibleAtScale(MapLocation pin,double pixelsPerMeter)=>pin.MinZoom<=0||pixelsPerMeter+1e-8>=.046875*Math.Pow(2,pin.MinZoom);
 public static IEnumerable<MapLocation> Apply(IEnumerable<MapLocation> points,IReadOnlySet<string> enabled,string query,string? layer=null){
  var terms=query.Trim().Split(' ',StringSplitOptions.RemoveEmptyEntries);
  return points.Where(p=>enabled.Contains(p.Kind)&&(layer==null||p.Layer==layer)&&terms.All(t=>(p.Name+" "+p.Kind+" "+p.Layer).Contains(t,StringComparison.OrdinalIgnoreCase)));
 }
}
public static class TravelCoordinates {
 public static (float X,float Y,float Z) FromHud(float east,float north,float height)=>(east,height+105,-north);
 public static (float East,float North,float Height) ToHud(float x,float y,float z)=>(x,-z,y-105);
 public const double MapWidth=12000,MapHeight=10000;
 public static (double X,double Z) FromNormalized(double u,double v)=>(u*MapWidth-6000,v*MapHeight-5000);
 public static (double U,double V) ToNormalized(double x,double z)=>((x+6000)/MapWidth,(z+5000)/MapHeight);
 public static bool TryParse(string x,string height,string z,out float east,out float altitude,out float south){east=altitude=south=0;return Number(x,out east)&&Number(height,out altitude)&&Number(z,out south);}
 static bool Number(string s,out float value)=>(float.TryParse(s,NumberStyles.Float,CultureInfo.InvariantCulture,out value)||float.TryParse(s,NumberStyles.Float,CultureInfo.CurrentCulture,out value))&&float.IsFinite(value)&&Math.Abs(value)<=100000;
}
