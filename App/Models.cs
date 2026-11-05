using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace HyruleCompanion;
public abstract class Bindable:INotifyPropertyChanged {public event PropertyChangedEventHandler? PropertyChanged;protected void Changed([CallerMemberName]string? p=null)=>PropertyChanged?.Invoke(this,new(p));}
public sealed class Item:Bindable {
 public string Id{get;set;}=""; public string Name{get;set;}="";public string Category{get;set;}="";public string Group{get;set;}="";public string Image{get;set;}="";public bool Special{get;set;}
 public bool SpawnSupported=>Category!="Armor"&&Category!="Food"&&Id!="Energy_Material_01";
 public string ImagePath=>Path.Combine(AppContext.BaseDirectory,Image);
 bool isCurrent;public bool IsCurrent{get=>isCurrent;set{isCurrent=value;Changed();}}
 bool favorite;public bool Favorite{get=>favorite;set{favorite=value;Changed();Changed(nameof(Star));}}
 public string Star=>Favorite?"★":"☆";
 public string Subtitle=>string.IsNullOrEmpty(Group)?Category:Group;
 public int UnitsPerPickup=>SpawnCatalog.UnitsInPickup(Id);
 public string UnitsBadge=>UnitsPerPickup>1?$"×{UnitsPerPickup}":"";
 public string UnitsSearchTerms=>UnitsPerPickup>1?$"{UnitsPerPickup}x x{UnitsPerPickup}":"";
}
public sealed class Cheat:Bindable {
 public string Id{get;init;}="";public string Name{get;init;}="";public string Description{get;init;}="";public string Category{get;init;}="";public string Icon{get;init;}="";
 public bool Supported{get;init;}=true;
 bool active;public bool Active{get=>active;set{active=value;Changed();Changed(nameof(State));}}
 bool available;public bool Available{get=>available;set{available=value;Changed();}}
 string note="Connect to the game";public string Note{get=>note;set{note=value;Changed();}}
 string hotkey="";public string Hotkey{get=>hotkey;set{hotkey=value;Changed();}}
 public string State=>Active?"ACTIVE":Supported?"OFF":"UNAVAILABLE";
 public double Value{get;set;}=1;
}
public sealed class Settings {
 public string GamePath{get;set;}=@"G:\Jeux\Recomp\totk.exe";
 public HashSet<string> Favorites{get;set;}=[];
 public List<string> Recent{get;set;}=[];
 public Dictionary<string,string> Hotkeys{get;set;}=[];
 public bool AlwaysOnTop{get;set;}
 public bool ReduceMotion{get;set;}
 public bool ReturnToGame{get;set;}=true;
 public string SpawnHotkey{get;set;}="Ctrl+Alt+S";
 public List<SavedPlace> Places{get;set;}=[];
 public HashSet<string>? MapCategories{get;set;}
 public static string FilePath=>Path.Combine(AppContext.BaseDirectory,"user-settings.json");
 public static Settings Load(){try{return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath))??new();}catch{return new();}}
 public void Save(){var temp=FilePath+".tmp";File.WriteAllText(temp,JsonSerializer.Serialize(this,new JsonSerializerOptions{WriteIndented=true}));File.Move(temp,FilePath,true);}
}
public record SavedPlace(string Name,float X,float Y,float Z,float[] Rotation){public string Coordinates=>$"X {X:0.0}   Height {Y:0.0}   Z {Z:0.0}";public PlayerPose Pose=>new(0,X,Y,Z,Rotation);}
public static class Catalog {
 public static List<Item> Load()=>JsonSerializer.Deserialize<List<Item>>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"assets","catalog.json")))??[];
 public static IEnumerable<Item> Filter(IEnumerable<Item> source,string query,string category,string group,bool favorites,string order,IReadOnlyList<string> recent){
  var terms=query.Trim().Split(' ',StringSplitOptions.RemoveEmptyEntries);
  var found=source.Where(x=>x.SpawnSupported&&(category=="All items"||x.Category==category)&&(group=="All types"||x.Group==group)&&(!favorites||x.Favorite)&&terms.All(t=>(x.Name+" "+x.Id+" "+x.Group+" "+x.Category+" "+x.UnitsSearchTerms).Contains(t,StringComparison.OrdinalIgnoreCase)));
  return order switch {"Favorites first"=>found.OrderByDescending(x=>x.Favorite).ThenBy(x=>x.Name),"Recently selected"=>found.OrderBy(x=>recent.IndexOf(x.Id)<0?int.MaxValue:recent.IndexOf(x.Id)).ThenBy(x=>x.Name),_=>found.OrderBy(x=>x.Name)};
 }
 static int IndexOf<T>(this IReadOnlyList<T> list,T value){for(var i=0;i<list.Count;i++)if(EqualityComparer<T>.Default.Equals(list[i],value))return i;return -1;}
}
