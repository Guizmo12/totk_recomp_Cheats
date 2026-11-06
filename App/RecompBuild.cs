using System.IO;
using System.Security.Cryptography;
namespace HyruleCompanion;

// Native addresses belong to the compiled Windows executable, not the game update.
// Keep independently verified profiles; never reuse one release's addresses on another.
public sealed record RecompBuild(string Release,string ExeHash,ulong HookRva,ulong SpawnRva,ulong MatrixRva,string HookSignature,string SpawnSignature,string MatrixSignature) {
 public const string GameVersion="1.2.1";
 public static IReadOnlyList<RecompBuild> Supported{get;}=new[]{
  new RecompBuild("0.1.1+701bd62","650ac3c2653a54d80fee0b8f69d58bc1f985ac2cfa81ea22b8ee8bafee69f052",0x44C55F0,0x3FB0AA0,0x2F39DE0,
   "4157415641554154565755534883ec284889ce488b05e69c9d098338000f85c8",
   "4157415641554154565755534883ec284889ce488b0536e8ee098338000f85b8",
   "41565657534883ec284889ce488b05fd54f60a8338000f8582020000488b8ea0"),
  new RecompBuild("0.3.11+0ae47a5","4aec5cd94e9deb7ffd9b3cbe714a8f56a01ea28af845bfef531c0ab1ba8196fe",0x4866D10,0x4334B20,0x3210EE0,
   "4157415641554154565755534883ec284889ce488b05563ba8098338000f85d5",
   "4157415641554154565755534883ec284889ce488b05465dfb098338000f855e",
   "41565657534883ec284889ce488b058d990d0b8338000f8537020000488b8ea0")
 };
 public static RecompBuild? FromHash(string hash)=>Supported.FirstOrDefault(p=>p.ExeHash.Equals(hash,StringComparison.OrdinalIgnoreCase));
 public static RecompBuild? Detect(string path){using var stream=File.OpenRead(path);return FromHash(Convert.ToHexString(SHA256.HashData(stream)));}
 public void ValidateFunctions(Func<ulong,int,byte[]> read,ulong imageBase){
  foreach(var (rva,signature) in new[]{(HookRva,HookSignature),(SpawnRva,SpawnSignature),(MatrixRva,MatrixSignature)}){
   var bytes=Convert.FromHexString(signature);
   if(!read(imageBase+rva,bytes.Length).SequenceEqual(bytes))throw new InvalidOperationException("The game's native functions do not match recomp "+Release+". Restart an unmodified supported build before connecting.");
  }
 }
}
