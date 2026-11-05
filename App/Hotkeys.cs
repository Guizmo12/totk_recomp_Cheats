using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace HyruleCompanion;
public sealed class Hotkeys(nint hwnd,Action<int> pressed):IDisposable {
 readonly HashSet<int> registered=[];
 public bool Set(int id,string text){Remove(id);if(string.IsNullOrEmpty(text))return true;if(!TryParse(text,out var mod,out var key))return false;if(!RegisterHotKey(hwnd,id,mod|0x4000,(uint)KeyInterop.VirtualKeyFromKey(key)))return false;registered.Add(id);return true;}
 public static bool TryParse(string text,out uint mod,out Key key){mod=0;key=Key.None;var parts=text.Split('+',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);if(parts.Length<2)return false;foreach(var p in parts[..^1]){if(p=="Ctrl")mod|=2;else if(p=="Alt")mod|=1;else if(p=="Shift")mod|=4;else return false;}try{var k=parts[^1];if(k=="Backspace")k="Back";if(k.Length==1&&char.IsDigit(k[0]))k="D"+k;key=(Key)new KeyConverter().ConvertFromString(k)!;return key!=Key.None&&mod!=0;}catch{return false;}}
 public static string Format(Key key,ModifierKeys mods){var p=new List<string>();if(mods.HasFlag(ModifierKeys.Control))p.Add("Ctrl");if(mods.HasFlag(ModifierKeys.Alt))p.Add("Alt");if(mods.HasFlag(ModifierKeys.Shift))p.Add("Shift");var k=key.ToString();if(k.Length==2&&k[0]=='D'&&char.IsDigit(k[1]))k=k[1..];p.Add(k);return string.Join("+",p);}
 public nint Hook(nint w,int msg,nint p,nint l,ref bool handled){if(msg==0x0312){pressed((int)p);handled=true;}return 0;}
 public void Remove(int id){if(registered.Remove(id))UnregisterHotKey(hwnd,id);}
 public void Dispose(){foreach(var id in registered.ToArray())Remove(id);}
 [DllImport("user32.dll",SetLastError=true)] static extern bool RegisterHotKey(nint w,int id,uint modifiers,uint key);
 [DllImport("user32.dll")] static extern bool UnregisterHotKey(nint w,int id);
 [DllImport("user32.dll")] public static extern nint GetForegroundWindow();
 [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(nint w,out uint pid);
}
