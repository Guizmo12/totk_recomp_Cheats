using System.Text;

namespace HyruleCompanion;
public record PlayerPose(ulong Actor,float X,float Y,float Z,float[] Rotation);
public static class PlayerLocator {
 static ulong P(IGameMemory m,ulong a){var p=BitConverter.ToUInt64(m.Read(a,8));if(p<0x10000||p>0x7FFFFFFFFF)throw new InvalidOperationException("Link is not available in this scene.");return p;}
 public static PlayerPose Read(IGameMemory m){
  // Publicly documented TOTK 1.2.1 scene/resident-actor ABI; no Switch plugin is loaded.
  var module=P(m,NativeMemory.Main+0x4728538);var scene=P(m,module+0x1E8);var components=P(m,scene+0x58);var residents=P(m,components+13*8);
  var count=BitConverter.ToInt32(m.Read(residents+0x20,4));if(count<1||count>128)throw new InvalidOperationException("The playable scene has not loaded.");var list=P(m,residents+0x28);
  for(var i=0;i<count;i++)try{var link=P(m,list+(ulong)i*0x70+8);var actor=P(m,link+0x40);var name=P(m,actor+0x218);var text=Encoding.UTF8.GetString(m.Read(name,32)).Split('\0')[0];if(text!="Player")continue;var data=m.Read(actor+0x2B4,48);var v=Enumerable.Range(0,12).Select(n=>BitConverter.ToSingle(data,n*4)).ToArray();if(v.Any(x=>!float.IsFinite(x))||v.Take(3).Any(x=>Math.Abs(x)>100000)||v.Skip(3).Any(x=>Math.Abs(x)>1.01))throw new InvalidOperationException("The player transform does not match this version.");return new(actor,v[0],v[1],v[2],v[3..]);}catch(InvalidOperationException){ }
  throw new InvalidOperationException("Link could not be found. Load your save and try again.");
 }
}
