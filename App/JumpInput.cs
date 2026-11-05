using System.Runtime.InteropServices;

namespace HyruleCompanion;
public static class JumpInput {
 public static bool Pressed(int gamePid){Hotkeys.GetWindowThreadProcessId(Hotkeys.GetForegroundWindow(),out var pid);if(pid!=gamePid)return false;if((GetAsyncKeyState(0x20)&0x8000)!=0)return true;try{for(uint i=0;i<4;i++)if(XInputGetState(i,out var s)==0&&(s.Buttons&0x8000)!=0)return true;}catch(DllNotFoundException){}return false;}
 [StructLayout(LayoutKind.Sequential)] struct State{public uint Packet;public ushort Buttons;public byte LT,RT;public short LX,LY,RX,RY;}
 [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
 [DllImport("xinput1_4.dll")] static extern uint XInputGetState(uint index,out State state);
}
