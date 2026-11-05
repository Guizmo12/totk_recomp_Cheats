using HyruleCompanion;
using System.Windows.Input;
using System.Text.Json;
using System.Runtime.InteropServices;
using NativeMemory=HyruleCompanion.NativeMemory;

internal class Program {
 [STAThread] static int Main(){var passed=0;void Test(string name,Action a){try{a();Console.WriteLine("PASS "+name);passed++;}catch(Exception e){Console.WriteLine("FAIL "+name+": "+e.Message);Environment.Exit(1);}}void Assert(bool x){if(!x)throw new Exception("Assertion failed");}
 Test("emergency shortcut parses",()=>{Assert(Hotkeys.TryParse("Ctrl+Alt+Backspace",out var m,out var k));Assert(m==3&&k==Key.Back);});
 Test("default shortcut parses",()=>{Assert(Hotkeys.TryParse("Ctrl+Alt+1",out var m,out var k));Assert(m==3&&k==Key.D1);});
 Test("unmodified shortcut refused",()=>Assert(!Hotkeys.TryParse("F1",out _,out _)));
 var data=new FakeMemory();var engine=new CheatEngine(data);var a=NativeMemory.Main+0x3877F34;data.F(a,1);
 Test("read-only preflight performs no writes",()=>{var p=engine.Plan("speed",1.5);Assert(p.Count==1&&data.Writes==0);});
 Test("speed restores the exact original bytes",()=>{engine.Apply("speed",1.5);Assert(BitConverter.ToSingle(data.Read(a,4))==1.5f);engine.Stop("speed");Assert(BitConverter.ToSingle(data.Read(a,4))==1f);});
 Test("invalid speed cannot write",()=>{var n=data.Writes;try{engine.Apply("speed",float.NaN);}catch(InvalidOperationException){}Assert(data.Writes==n);});
 Test("unavailable controls cannot write",()=>{var n=data.Writes;try{engine.Apply("fall");}catch(InvalidOperationException){}Assert(data.Writes==n);});
 Test("invalid pointer cannot write",()=>{data.P(NativeMemory.Main+0x46AF308,0);var n=data.Writes;try{engine.Apply("health");}catch(InvalidOperationException){}Assert(data.Writes==n);});
 Test("health follows pointer chain and uses actual maximum",()=>{ulong p=0x100000;data.P(NativeMemory.Main+0x46AF308,p);foreach(var off in new ulong[]{0x480,0x228,0x2F8}){data.P(p+off,p+0x10000);p+=0x10000;}data.P(p+0x1830,0x300000);data.P(p+0x1838,0x400000);data.I(0x300008,4);data.I(0x400008,12);var plan=engine.Plan("health");Assert(plan.Count==1&&plan[0].Address==0x300008&&BitConverter.ToInt32(plan[0].Value)==12);engine.Apply("health");Assert(BitConverter.ToInt32(data.Read(0x300008,4))==12);});
 Test("bad max health blocks all writes",()=>{data.I(0x400008,9999);var n=data.Writes;try{engine.Apply("health");}catch(InvalidOperationException){}Assert(data.Writes==n);});
 Test("jump boost scales upward velocity by the square root of height",()=>{ulong p=0x500000;data.P(NativeMemory.Main+0x471DCB0,p);foreach(var off in new ulong[]{0xC8,0x178,0x30}){data.P(p+off,p+0x10000);p+=0x10000;}data.F(p+0xFC,6);var plan=engine.Plan("jump",4);Assert(plan.Count==1&&Math.Abs(BitConverter.ToSingle(plan[0].Value)-12)<0.001f);});
 var items=new List<Item>{new(){Id="Weapon_Shield_030",Name="Hylian Shield",Category="Shields",Favorite=true},new(){Id="Item_Fruit_A",Name="Apple",Category="Materials"},new(){Id="Weapon_Sword_070",Name="Master Sword",Category="Weapons"}};
 Test("search matches multiple words regardless of case",()=>Assert(Catalog.Filter(items,"HYLIAN shield","All items","All types",false,"",[]).Single().Id=="Weapon_Shield_030"));
 Test("actor ID search works",()=>Assert(Catalog.Filter(items,"Sword_070","All items","All types",false,"",[]).Count()==1));
 Test("category and favorites compose",()=>Assert(!Catalog.Filter(items,"","Materials","All types",true,"",[]).Any()));
 Test("recent ordering prioritizes selected items",()=>Assert(Catalog.Filter(items,"","All items","All types",false,"Recently selected",["Weapon_Sword_070"]).First().Id=="Weapon_Sword_070"));
 Test("native frame callback preserves the CPU and invokes one queued call",NativeCallbackCheck.Run);
 Test("arrow inventory entry resolves to the collectible pickup instead of the projectile",()=>Assert(SpawnCatalog.WorldActor("NormalArrow")=="Obj_ArrowNormal_A_01"));
 Test("ordinary items keep their existing world actor",()=>Assert(SpawnCatalog.WorldActor("Item_Fruit_A")=="Item_Fruit_A"&&SpawnCatalog.WorldActor("Weapon_Sword_001")=="Weapon_Sword_001"));
 Test("arrow bundles retain their own world actor and pickup count",()=>Assert(SpawnCatalog.WorldActor("Obj_ArrowBundle_A_01")=="Obj_ArrowBundle_A_01"&&SpawnCatalog.UnitsInPickup("Obj_ArrowBundle_A_01")==5&&SpawnCatalog.UnitsInPickup("Obj_ArrowBundle_A_02")==10&&SpawnCatalog.UnitsInPickup("NormalArrow")==1));
 Test("five-arrow bundle can be found using 5x",()=>{var bundle=new Item{Id="Obj_ArrowBundle_A_01",Name="Arrows ×5",Category="Arrows",Group="Bundles"};Assert(Catalog.Filter([bundle],"5x arrows","All items","All types",false,"",[]).Single()==bundle&&bundle.UnitsBadge=="×5");});
 Console.WriteLine($"{passed} checks passed. Fake-memory checks validate logic, not live gameplay.");return 0;
 }
}
static class NativeCallbackCheck {
 [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void Fn(nint c);
 [DllImport("kernel32.dll")] static extern nint VirtualAlloc(nint a,nuint size,uint type,uint protection);
 [DllImport("kernel32.dll")] static extern bool VirtualFree(nint a,nuint size,uint type);
 public static void Run(){
  var mail=Marshal.AllocHGlobal(65536);var cpu=Marshal.AllocHGlobal(1024);var stack=Marshal.AllocHGlobal(65536);nint code=0;var problem="";var calls=0;var originals=0;
  try{
   Marshal.Copy(new byte[65536],0,mail,65536);var initial=Enumerable.Repeat((byte)0xA5,1024).ToArray();var sp=(stack.ToInt64()+64000)&~15L;BitConverter.GetBytes(sp).CopyTo(initial,256);Marshal.Copy(initial,0,cpu,1024);
   Fn target=c=>{try{calls++;if(Marshal.ReadInt64(c)!=101||Marshal.ReadInt64(c,56)!=108||Marshal.ReadInt64(c,264)!=0x7000AB92CC)problem="Guest arguments differ";var s=(nint)Marshal.ReadInt64(c,256);if(s.ToInt64()!=sp-4096||Marshal.ReadInt64(s)!=0||Marshal.ReadInt64(s,8)!=mail.ToInt64()+0x500||Marshal.ReadInt64(s,16)!=mail.ToInt64()+0x508)problem="Stack arguments differ";Marshal.Copy(new byte[1024],0,c,1024);Marshal.WriteInt64(c,1);}catch(Exception e){problem=e.Message;}};
   Fn original=c=>{originals++;var bytes=new byte[1024];Marshal.Copy(c,bytes,0,1024);if(!bytes.SequenceEqual(initial))problem="Guest CPU was not restored exactly";};
   Marshal.WriteInt64(mail,0x20,Marshal.GetFunctionPointerForDelegate(target).ToInt64());Marshal.WriteInt64(mail,0x28,0x7000AB92CC);for(var i=0;i<8;i++)Marshal.WriteInt64(mail,0x800+i*8,101+i);
   var bytes=FrameBridge.BuildCallback((ulong)mail,(ulong)Marshal.GetFunctionPointerForDelegate(original));code=VirtualAlloc(0,4096,0x3000,0x40);if(code==0)throw new Exception("Local test allocation failed");Marshal.Copy(bytes,0,code,bytes.Length);var hook=Marshal.GetDelegateForFunctionPointer<Fn>(code);
   hook(cpu);if(calls!=0||originals!=1)throw new Exception("Idle callback did not pass through");Marshal.WriteInt32(mail,1);hook(cpu);if(calls!=1||originals!=2||Marshal.ReadInt32(mail)!=3||Marshal.ReadInt32(mail,8)!=1||Marshal.ReadInt32(mail,12)!=2)throw new Exception("Queued callback counters differ");hook(cpu);if(calls!=1||originals!=3)throw new Exception("Completed command ran twice");if(problem!="")throw new Exception(problem);GC.KeepAlive(target);GC.KeepAlive(original);
  }finally{if(code!=0)VirtualFree(code,0,0x8000);Marshal.FreeHGlobal(stack);Marshal.FreeHGlobal(cpu);Marshal.FreeHGlobal(mail);}
 }
}
sealed class FakeMemory:IGameMemory {readonly Dictionary<ulong,byte> bytes=[];public int Writes;public byte[] Read(ulong a,int n)=>Enumerable.Range(0,n).Select(i=>bytes.GetValueOrDefault(a+(ulong)i)).ToArray();public void Write(ulong a,byte[] b){Writes++;Put(a,b);}void Put(ulong a,byte[] b){for(var i=0;i<b.Length;i++)bytes[a+(ulong)i]=b[i];}public void F(ulong a,float v)=>Put(a,BitConverter.GetBytes(v));public void I(ulong a,int v)=>Put(a,BitConverter.GetBytes(v));public void P(ulong a,ulong v)=>Put(a,BitConverter.GetBytes(v));}
