using System.IO;
using System.Windows;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HyruleCompanion;
public partial class App:Application {
 public static string[] Arguments=[];
 Mutex? instance;bool ownsInstance;
 protected override void OnStartup(StartupEventArgs e){base.OnStartup(e);instance=new Mutex(true,"Local\\HyruleCompanion",out ownsInstance);if(!ownsInstance){foreach(var p in Process.GetProcessesByName("HyruleCompanion"))using(p){if(p.Id==Environment.ProcessId||p.MainWindowHandle==0)continue;ShowWindow(p.MainWindowHandle,9);SetForegroundWindow(p.MainWindowHandle);break;}Shutdown();return;}Arguments=e.Args;DispatcherUnhandledException+=(_,args)=>{File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"app-error.txt"),args.Exception.ToString());MessageBox.Show("Hyrule Companion encountered an error. Details were saved to app-error.txt next to the app.","Hyrule Companion");args.Handled=true;Shutdown(1);};try{new MainWindow().Show();}catch(Exception ex){File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"app-error.txt"),ex.ToString());MessageBox.Show("The app could not start. Keep the assets folder next to HyruleCompanion.exe. Details are in app-error.txt.","Hyrule Companion");Shutdown(1);}}
 protected override void OnExit(ExitEventArgs e){if(ownsInstance)instance?.ReleaseMutex();instance?.Dispose();base.OnExit(e);}
 [DllImport("user32.dll")] static extern bool ShowWindow(nint w,int command);
 [DllImport("user32.dll")] static extern bool SetForegroundWindow(nint w);
}
