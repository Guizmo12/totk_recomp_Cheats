using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace HyruleCompanion;

// A version-pinned, session-only frame callback. The executable on disk is never patched.
// The callback saves all 1024 bytes of guest CPU state, performs one queued game call,
// restores that state, then runs the displaced native prologue and original function.
public sealed class FrameBridge:IDisposable {
 public const string SupportedExeHash="650ac3c2653a54d80fee0b8f69d58bc1f985ac2cfa81ea22b8ee8bafee69f052";
 static readonly byte[] Prologue=Convert.FromHexString("4157415641554154565755534883EC28");
 const ulong HookRva=0x44C55F0,SpawnRva=0x3FB0AA0,MatrixRva=0x2F39DE0;
 readonly NativeMemory memory;
 public ulong Mailbox{get;}
 readonly ulong hook,trampoline;
 bool attached;
 public int State=>BitConverter.ToInt32(memory.Read(Mailbox,4));
 public uint Frames=>BitConverter.ToUInt32(memory.Read(Mailbox+12,4));
 public bool Result=>BitConverter.ToUInt64(memory.Read(Mailbox+8,8))%0x100000000!=0;
 public uint ResultCode=>BitConverter.ToUInt32(memory.Read(Mailbox+0x500,4));
 public ulong PreActor=>BitConverter.ToUInt64(memory.Read(Mailbox+0x508,8));
 public static bool IsCompatible(string path){using var f=File.OpenRead(path);return Convert.ToHexString(SHA256.HashData(f)).Equals(SupportedExeHash,StringComparison.OrdinalIgnoreCase);}
 public FrameBridge(NativeMemory game){
  memory=game;if(!IsCompatible(game.GamePath))throw new InvalidOperationException("The item-spawning extension is currently verified only for your original 0.1.1 build.");
  hook=game.ImageBase+HookRva;if(!game.ReadNative(hook,16).SequenceEqual(Prologue))throw new InvalidOperationException("The game’s frame callback is already modified. Restart the game before connecting the item spawner.");
  PlayerLocator.Read(game);
  for(ulong address=0x6000000000;address<0x6100000000;address+=0x10000){var p=VirtualAllocEx(game.Handle,(nint)address,0x10000,0x3000,0x04);if(p!=0){Mailbox=(ulong)p;break;}}
  if(Mailbox==0)throw new InvalidOperationException("A compatible game-address buffer could not be allocated.");
  trampoline=Mailbox+0x5000;
  try{
   RawWrite(trampoline,Prologue.Concat(AbsoluteJump(hook+16)).ToArray());
   RawWrite(Mailbox+0x4000,BuildCallback(Mailbox,trampoline));
   Protect(Mailbox+0x4000,0x2000,0x20);
   using(var pause=new PausedThreads(game.Pid)){
    if(!game.ReadNative(hook,16).SequenceEqual(Prologue))throw new InvalidOperationException("The game changed while attaching the item spawner.");
    pause.Relocate(hook,16,trampoline);
    try{Patch(hook,AbsoluteJump(Mailbox+0x4000).Concat(new byte[]{0x90,0x90}).ToArray());}
    catch{Patch(hook,Prologue);throw;}
    attached=true;
   }
  }catch{throw;}
 }
 public void QueueSpawn(string itemId,int quantityIndex,double distance){
  var actor=SpawnCatalog.WorldActor(itemId);
  if(State is 1 or 2)throw new InvalidOperationException("The previous item is still being created.");
  if(actor.Length<1||actor.Length>96||actor.Any(c=>!(char.IsAsciiLetterOrDigit(c)||c=='_')))throw new InvalidOperationException("The actor name is invalid.");
  if(distance<1||distance>10||quantityIndex<0||quantityIndex>=25)throw new InvalidOperationException("Choose 1–25 items and a spawn distance from 1 to 10 meters.");
  var pose=PlayerLocator.Read(memory);var rotation=pose.Rotation;
  var forwardX=rotation[2];var forwardZ=rotation[8];var norm=Math.Sqrt(forwardX*forwardX+forwardZ*forwardZ);if(norm<0.1)throw new InvalidOperationException("Link’s facing direction is not ready.");forwardX/=(float)norm;forwardZ/=(float)norm;
  var lateral=quantityIndex==0?0:(quantityIndex%5-2)*0.3f;var extra=(quantityIndex/5)*0.4f;
  var x=pose.X+forwardX*(float)(distance+extra)+forwardZ*lateral;var z=pose.Z+forwardZ*(float)(distance+extra)-forwardX*lateral;
  RawWrite(Mailbox+0x100,Encoding.ASCII.GetBytes(actor+'\0'));Q(Mailbox+0x80,Mailbox+0x100);
  var arg=new byte[0x98];F(arg,0,x);F(arg,4,pose.Y+1f);F(arg,8,z);F(arg,12,1);F(arg,28,1);F(arg,44,1);F(arg,48,1);F(arg,52,1);F(arg,56,1);
  BitConverter.GetBytes(Mailbox+0x190).CopyTo(arg,0x40);BitConverter.GetBytes(Mailbox+0x2000).CopyTo(arg,0x58);arg[0x90]=7;RawWrite(Mailbox+0x200,arg);
  var blackboard=new byte[0x1918];BitConverter.GetBytes(32).CopyTo(blackboard,0);BitConverter.GetBytes(Mailbox+0x2018).CopyTo(blackboard,8);RawWrite(Mailbox+0x2000,blackboard);
  var mgr=BitConverter.ToUInt64(memory.Read(NativeMemory.Main+0x4722920,8));if(mgr<0x10000||mgr>0x7FFFFFFFFF)throw new InvalidOperationException("The actor manager is not ready.");
  var regs=new ulong[]{mgr,Mailbox+0x80,Mailbox+0x200,0,1,0,0,0};SetArguments(regs,NativeMemory.Main+0xAB92CC,memory.ImageBase+SpawnRva);
 }
 public void QueueTeleport(PlayerPose destination){
  if(State is 1 or 2)throw new InvalidOperationException("The previous game action is still running.");var current=PlayerLocator.Read(memory);
  if(new[]{destination.X,destination.Y,destination.Z}.Any(x=>!float.IsFinite(x)||Math.Abs(x)>100000)||destination.Rotation.Length!=9)throw new InvalidOperationException("The saved position is invalid.");
  var matrix=new byte[48];for(var row=0;row<3;row++){for(var col=0;col<3;col++)F(matrix,row*16+col*4,destination.Rotation[row*3+col]);F(matrix,row*16+12,row==0?destination.X:row==1?destination.Y:destination.Z);}RawWrite(Mailbox+0x900,matrix);SetArguments([current.Actor,Mailbox+0x900,0,0,0,0,0,0],NativeMemory.Main+0x6BA86C,memory.ImageBase+MatrixRva);
 }
 void SetArguments(ulong[] regs,ulong guest,ulong native){RawWrite(Mailbox+0x800,regs.SelectMany(BitConverter.GetBytes).ToArray());RawWrite(Mailbox+0x500,new byte[16]);Q(Mailbox+0x20,native);Q(Mailbox+0x28,guest);RawWrite(Mailbox,new byte[]{1,0,0,0});}
 static void F(byte[] data,int offset,float value)=>BitConverter.GetBytes(value).CopyTo(data,offset);
 void Q(ulong a,ulong v)=>RawWrite(a,BitConverter.GetBytes(v));
 void RawWrite(ulong a,byte[] bytes){if(!WriteProcessMemory(memory.Handle,(nuint)a,bytes,(nuint)bytes.Length,out var n)||n!=(nuint)bytes.Length)throw new InvalidOperationException("The game extension buffer could not be written.");}
 uint Protect(ulong a,nuint size,uint value){if(!VirtualProtectEx(memory.Handle,(nuint)a,size,value,out var old))throw new InvalidOperationException("The game extension’s memory protection could not be set.");return old;}
 void Patch(ulong a,byte[] bytes){var old=Protect(a,(nuint)bytes.Length,0x40);try{RawWrite(a,bytes);FlushInstructionCache(memory.Handle,(nuint)a,(nuint)bytes.Length);}finally{Protect(a,(nuint)bytes.Length,old);}}
 public void Dispose(){if(!attached||!memory.Alive)return;using(var pause=new PausedThreads(memory.Pid)){if(State==1)RawWrite(Mailbox,new byte[4]);Patch(hook,Prologue);}attached=false;
  // A callback already executing may still return through this allocation. Keeping 64 KiB
  // reserved until process exit avoids freeing instructions beneath a running game thread.
 }
 static byte[] AbsoluteJump(ulong target)=>new byte[]{0xFF,0x25,0,0,0,0}.Concat(BitConverter.GetBytes(target)).ToArray();
 public static byte[] BuildCallback(ulong data,ulong original){
  var b=new List<byte>();void H(string hex)=>b.AddRange(Convert.FromHexString(hex));void Q(ulong n)=>b.AddRange(BitConverter.GetBytes(n));
  H("53565741544883EC284889CB49BC");Q(data); // preserve native nonvolatile registers, keep the incoming Cpu pointer in rbx
  H("F041FF44240CB801000000BA02000000F0410FB114240F85");var skip=b.Count;H("00000000");
  H("0FAE5C24204889DE498DBC2400100000B980000000F348A5"); // preserve MXCSR and the complete guest Cpu
  H("498DB424000800004889DFB908000000F348A5"); // x0-x7 from the queued call
  H("488B8300010000482D001000004889830001000048C70000000000498D94240005000048895008498D94240805000048895010"); // reserve guest stack space and pass the final three ABI arguments
  H("48B800F00F2070000000488983F0000000498B442428488983080100004889D9498B442420FFD0");
  H("488B034189442408498DB424001000004889DFB980000000F348A50FAE54242041C7042403000000");
  var finish=b.Count;var delta=finish-(skip+4);var d=BitConverter.GetBytes(delta);for(var i=0;i<4;i++)b[skip+i]=d[i];
  H("4889D94883C428415C5F5E5B");b.AddRange(AbsoluteJump(original));return b.ToArray();
 }
 [DllImport("kernel32.dll",SetLastError=true)] static extern nint VirtualAllocEx(SafeProcessHandle h,nint a,nuint size,uint type,uint protection);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool VirtualFreeEx(SafeProcessHandle h,nint a,nuint size,uint type);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool VirtualProtectEx(SafeProcessHandle h,nuint a,nuint size,uint protection,out uint old);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool WriteProcessMemory(SafeProcessHandle h,nuint a,byte[] b,nuint size,out nuint n);
 [DllImport("kernel32.dll")] static extern bool FlushInstructionCache(SafeProcessHandle h,nuint a,nuint size);
}

sealed class PausedThreads:IDisposable {
 readonly List<nint> threads=[];
 public PausedThreads(int pid){try{using var process=Process.GetProcessById(pid);foreach(ProcessThread t in process.Threads){var h=OpenThread(0x1A,false,(uint)t.Id);if(h==0)throw new InvalidOperationException("A game thread could not be paused for the extension.");if(SuspendThread(h)==uint.MaxValue){CloseHandle(h);throw new InvalidOperationException("The game could not be paused for the extension.");}threads.Add(h);}}catch{Dispose();throw;}}
 public void Relocate(ulong start,ulong length,ulong destination){var raw=Marshal.AllocHGlobal(1250);var context=(nint)((raw.ToInt64()+15)&~15L);try{foreach(var h in threads){for(var i=0;i<1232;i++)Marshal.WriteByte(context,i,0);Marshal.WriteInt32(context,48,0x100001);if(!GetThreadContext(h,context))throw new InvalidOperationException("A paused game thread could not be inspected.");var rip=(ulong)Marshal.ReadInt64(context,248);if(rip>=start&&rip<start+length){Marshal.WriteInt64(context,248,(long)(destination+rip-start));if(!SetThreadContext(h,context))throw new InvalidOperationException("A paused game thread could not be relocated.");}}}finally{Marshal.FreeHGlobal(raw);}}
 public void Dispose(){foreach(var h in threads){ResumeThread(h);CloseHandle(h);}threads.Clear();}
 [DllImport("kernel32.dll",SetLastError=true)] static extern nint OpenThread(uint access,bool inherit,uint id);
 [DllImport("kernel32.dll",SetLastError=true)] static extern uint SuspendThread(nint h);
 [DllImport("kernel32.dll",SetLastError=true)] static extern uint ResumeThread(nint h);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool GetThreadContext(nint h,nint context);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool SetThreadContext(nint h,nint context);
 [DllImport("kernel32.dll")] static extern bool CloseHandle(nint h);
}
