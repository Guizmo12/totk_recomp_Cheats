using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace HyruleCompanion;

public interface IGameMemory { byte[] Read(ulong address,int count); void Write(ulong address,byte[] bytes); }

public sealed class NativeMemory : IGameMemory, IDisposable {
 public const ulong Main=0x7000000000;
 public const string BuildId="9B4E43650501A4D4";
 public int Pid {get;}
 internal SafeProcessHandle Handle=>handle;
 internal ulong ImageBase=>(ulong)process.MainModule!.BaseAddress;
 public bool FocusGame()=>SetForegroundWindow(process.MainWindowHandle);
 public string GamePath{get;}
 public RecompBuild Build{get;}
 readonly SafeProcessHandle handle;
 readonly Process process;
 public bool Alive {get{try{return !process.HasExited;}catch{return false;}}}
 public NativeMemory(string exe) {
  exe=Path.GetFullPath(exe);
  GamePath=exe;
  var candidates=Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exe)).Where(p=>{try{return string.Equals(p.MainModule?.FileName,exe,StringComparison.OrdinalIgnoreCase);}catch{return false;}}).ToArray();
  if(candidates.Length!=1)throw new InvalidOperationException(candidates.Length==0?"Start TOTK and load your save, then connect.":"More than one matching game is running. Close the extra copy before connecting.");
  var nso=Path.Combine(Path.GetDirectoryName(exe)!,"exefs","main");
  using(var stream=File.OpenRead(nso)) {var head=new byte[0x48];stream.ReadExactly(head);if(System.Text.Encoding.ASCII.GetString(head,0,4)!="NSO0"||Convert.ToHexString(head.AsSpan(0x40,8))!=BuildId)throw new InvalidOperationException("This trainer currently supports TOTK 1.2.1 only. No changes were made.");}
  Build=RecompBuild.Detect(exe)??throw new InvalidOperationException("This executable has no verified compatibility profile. Supported recomp builds: 0.1.1 and 0.3.11 with TOTK 1.2.1. No changes were made.");
  process=candidates[0];Pid=process.Id;
  handle=OpenProcess(0x438,false,Pid);
  if(handle.IsInvalid)throw new InvalidOperationException("Windows could not open the game. Run both apps with the same permissions.");
  try{var speed=BitConverter.ToSingle(Read(Main+0x3877F34,4));if(!float.IsFinite(speed)||speed<0.1||speed>10)throw new InvalidOperationException("The game memory layout does not match this version.");}
  catch {handle.Dispose();process.Dispose();throw;}
 }
 public byte[] Read(ulong address,int count) {
  if(address<0x10000||address>0x7FFFFFFFFF||count<1||count>4096)throw new InvalidOperationException("Game data is not ready. Load a playable save first.");
  var result=new byte[count];if(!ReadProcessMemory(handle,(nuint)address,result,(nuint)count,out var n)||n!=(nuint)count)throw new InvalidOperationException("Game data is temporarily unavailable. Try again after loading finishes.");return result;
 }
 internal byte[] ReadNative(ulong address,int count){var module=process.MainModule!;var start=(ulong)module.BaseAddress;if(count<1||count>64||address<start||address+(ulong)count>start+(ulong)module.ModuleMemorySize)throw new InvalidOperationException("The native function is outside the game executable.");var bytes=new byte[count];if(!ReadProcessMemory(handle,(nuint)address,bytes,(nuint)count,out var n)||n!=(nuint)count)throw new InvalidOperationException("The native game function could not be inspected.");return bytes;}
 public void Write(ulong address,byte[] bytes) {
  // This application writes only four-byte fields, never lifted ARM or native x64 code.
  if(bytes.Length!=4||address<0x10000||address>0x7FFFFFFFFF||(address>=Main&&address<Main+0x2B1A000))throw new InvalidOperationException("A write outside the supported game data was refused.");
  var old=Read(address,4);
  if(old.SequenceEqual(bytes))return;
  if(!VirtualProtectEx(handle,(nuint)address,4,0x04,out var previous))throw new InvalidOperationException("Windows refused access to this game value.");
  try{if(!WriteProcessMemory(handle,(nuint)address,bytes,4,out var n)||n!=4)throw new InvalidOperationException("The game value could not be changed.");}
  finally {if(!VirtualProtectEx(handle,(nuint)address,4,previous,out _))throw new InvalidOperationException("The game's memory protection could not be restored. Disconnect and restart the game.");}
 }
 public void Dispose(){handle.Dispose();process.Dispose();}
 [DllImport("kernel32.dll",SetLastError=true)] static extern SafeProcessHandle OpenProcess(uint rights,bool inherit,int pid);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool ReadProcessMemory(SafeProcessHandle h,nuint a,byte[] b,nuint n,out nuint read);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool WriteProcessMemory(SafeProcessHandle h,nuint a,byte[] b,nuint n,out nuint written);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool VirtualProtectEx(SafeProcessHandle h,nuint a,nuint n,uint protection,out uint old);
 [DllImport("user32.dll")] static extern bool SetForegroundWindow(nint window);
}

public sealed class CheatEngine(IGameMemory memory) {
 readonly Dictionary<ulong,byte[]> originals=[];
 ulong Pointer(ulong a){var v=BitConverter.ToUInt64(memory.Read(a,8));if(v<0x10000||v>0x7FFFFFFFFF)throw new InvalidOperationException("This value is not available in the current game scene.");return v;}
 public ulong Chain(ulong root,params ulong[] offsets){var a=Pointer(NativeMemory.Main+root);foreach(var o in offsets)a=Pointer(a+o);return a;}
 int Int(ulong a)=>BitConverter.ToInt32(memory.Read(a,4));
 float Float(ulong a)=>BitConverter.ToSingle(memory.Read(a,4));
 static void Require(bool condition){if(!condition)throw new InvalidOperationException("The game value is outside its expected range. No change was applied.");}
 public List<(ulong Address,byte[] Value)> Plan(string id,double value=1) {
  var ops=new List<(ulong,byte[])>();
  void I(ulong a,int v,int min,int max){Require(Int(a)>=min&&Int(a)<=max&&v>=min&&v<=max);ops.Add((a,BitConverter.GetBytes(v)));}
  void F(ulong a,float v,float min,float max){var current=Float(a);Require(float.IsFinite(current)&&current>=min&&current<=max&&float.IsFinite(v)&&v>=min&&v<=max);ops.Add((a,BitConverter.GetBytes(v)));}
  switch(id){
   case "health": {var actor=Chain(0x46AF308,0x480,0x228,0x2F8);var a=Pointer(actor+0x1830)+8;var b=Pointer(actor+0x1838)+8;var max=Int(b);Require(max>=4&&max<=160&&max%4==0);I(a,max,0,160);break;}
   case "stamina": {var max=Float(Chain(0x4721B98,0x198)+0x5BC);Require(max>=100&&max<=3000);F(Chain(0x46AF308,0x480,0x228,0x3A8)+0x13FC,max,0,10000);break;}
   case "battery": {var max=Float(Chain(0x4721B98,0x198)+0x594);Require(max>=100&&max<=100000);F(Chain(0x4728538,0x1E8,0x58,0xF8)+0x5C,max,0,100000);break;}
   case "arrows": I(Chain(0x4721B98,0x138,0xBE8),999,0,999);break;
   case "rupees": I(Chain(0x4721B98,0xD8)+0x28EA4,999999,0,999999);break;
   case "speed": F(NativeMemory.Main+0x3877F34,(float)value,0.25f,3f);break;
   case "jump": {Require(value>=1&&value<=5);var a=Chain(0x471DCB0,0xC8,0x178,0x30)+0xFC;var velocity=Float(a);Require(float.IsFinite(velocity)&&velocity>=-200&&velocity<=200);F(a,Math.Clamp(velocity*(float)Math.Sqrt(value),-200,200),-200,200);break;}
   case "durability": {
    var held=Chain(0x46AF308,0x480,0x228,0x230);
    foreach(var slot in new ulong[]{0x28,0x70,0x88}){
     try {var record=Pointer(held+slot);record=Pointer(record+0x40);record=Pointer(record+0x228);record=Pointer(record+0x2F8);var a=Pointer(record+0x1830)+8;var b=Pointer(record+0x1838)+8;var max=Int(b);if(max>=1&&max<=10000)I(a,max,0,10000);}catch(InvalidOperationException){/* Empty equipment slots have no record. */}
    }if(ops.Count==0)throw new InvalidOperationException("Equip a weapon, bow or shield before enabling durability.");break;
   }
   default: throw new InvalidOperationException("This control needs a compatible recomp extension. It is not available in this build.");
  }return ops;
 }
 public void Apply(string id,double value=1){foreach(var op in Plan(id,value)){if(id=="speed"&&!originals.ContainsKey(op.Address))originals[op.Address]=memory.Read(op.Address,4);memory.Write(op.Address,op.Value);}}
 public void Stop(string id){if(id!="speed")return;foreach(var (a,b) in originals.ToArray()){memory.Write(a,b);originals.Remove(a);}}
 public float JumpVelocity()=>Float(Chain(0x471DCB0,0xC8,0x178,0x30)+0xFC);
 public Dictionary<string,object> Snapshot(){var result=new Dictionary<string,object>();foreach(var id in new[]{"health","stamina","battery","arrows","rupees","speed","durability","jump"})try{result[id]=Plan(id).Select(x=>new{address=x.Address.ToString("X"),current=Convert.ToHexString(memory.Read(x.Address,4)),target=Convert.ToHexString(x.Value)}).ToArray();}catch(Exception e){result[id]=e.Message;}return result;}
}
