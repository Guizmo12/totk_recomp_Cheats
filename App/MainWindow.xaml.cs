using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Data;
using Microsoft.Win32;

namespace HyruleCompanion;
public partial class MainWindow:Window {
 readonly Settings settings=Settings.Load();
 readonly List<Item> catalog;
 readonly List<Cheat> cheats=[];
 readonly DispatcherTimer searchTimer=new(){Interval=TimeSpan.FromMilliseconds(160)};
 readonly DispatcherTimer gameTimer=new(){Interval=TimeSpan.FromMilliseconds(30)};
 readonly Dictionary<string,Button> categoryButtons=[];
 NativeMemory? memory;CheatEngine? engine;Hotkeys? hotkeys;
 FrameBridge? bridge;bool spawning,advancedCompatible;HashSet<string> worldActors=[];
 PlayerPose? undoTravel;
 bool jumpBoosted;
 string renderedActive="";
 string category="All items",group="All types",pageName="items";
 bool favoritesOnly,initialized,closing;
 int page;const int PageSize=36;
 List<Item> filtered=[];Item? selected;
 public MainWindow(){
  InitializeComponent();Chrome.ReduceMotion=settings.ReduceMotion;ReduceMotionCheck.IsChecked=settings.ReduceMotion;catalog=Catalog.Load();foreach(var item in catalog)item.Favorite=settings.Favorites.Contains(item.Id);
  AddCheat("health","Infinite health","Keep your current hearts full.","Player","\uE95E",1);
  AddCheat("stamina","Infinite stamina","Keep climbing, swimming and gliding.","Player","\uE945",2);
  AddCheat("battery","Infinite battery","Keep your Zonai creations powered.","Player","\uE83F",3);
  AddCheat("durability","Equipment durability","Refill the equipped weapon, bow and shield.","Inventory","\uE90F",4);
  AddCheat("arrows","999 arrows","Maintain a full stack of arrows.","Inventory","\uE72A",5);
  AddCheat("rupees","999,999 rupees","Maintain the maximum rupee balance.","Inventory","\uE8D4",6);
  AddCheat("speed","Movement speed","Use the multiplier below; switching off restores your original speed.","World","\uEADA",7);
  AddCheat("fall","No fall damage","Needs a game-code extension for this recomp.","Player","\uE81D",0,false);
  AddCheat("jump","High jump","Boost each jump. Hold Space or Xbox Y; adjust the height below.","World","\uE74A",8);
  cheats.First(c=>c.Id=="jump").Value=2;
  ShowCheats(cheats);HotkeyList.ItemsSource=cheats.Where(c=>c.Supported).ToList();
  foreach(var c in new[]{"All items","Weapons","Bows","Shields","Armor","Materials","Food","Zonai devices","Arrows","Key items"}){
   var b=new Button{Content=c,Tag=c,Style=(Style)FindResource("CategoryChip")};b.Click+=CategoryClick;CategoryChips.Children.Add(b);categoryButtons[c]=b;
  }
  GroupFilter.Items.Add("All types");GroupFilter.SelectedIndex=0;
  GamePathBox.Text=settings.GamePath;TopmostCheck.IsChecked=settings.AlwaysOnTop;Topmost=settings.AlwaysOnTop;ReturnToGameCheck.IsChecked=settings.ReturnToGame;SpawnShortcutBox.Text=settings.SpawnHotkey;
  CatalogCaption.Text=$"{catalog.Count:N0} entries, including armor upgrades. Find your next favorite.";
  PlaceList.ItemsSource=settings.Places;
  searchTimer.Tick+=(_,_)=>{searchTimer.Stop();page=0;RefreshItems();};
  gameTimer.Tick+=GameTick;
  PreviewKeyDown+=WindowKey;
  initialized=true;Navigate("items");RefreshItems();
  var first=catalog.FirstOrDefault(x=>x.Id==settings.Recent.FirstOrDefault())??catalog.FirstOrDefault(x=>x.Id=="Weapon_Sword_070")??catalog.First();SelectItem(first);
 }
 void AddCheat(string id,string name,string description,string cat,string icon,int n,bool supported=true){cheats.Add(new(){Id=id,Name=name,Description=description,Category=cat,Icon=icon,Supported=supported,Hotkey=settings.Hotkeys.GetValueOrDefault(id,supported?$"Ctrl+Alt+{n}":"—"),Note=supported?"Connect to the game":"Not supported by this build"});}
 void WindowLoaded(object sender,RoutedEventArgs e){
  var hwnd=new WindowInteropHelper(this).Handle;hotkeys=new(hwnd,HotkeyPressed);HwndSource.FromHwnd(hwnd).AddHook(hotkeys.Hook);RegisterShortcuts();
  if(App.Arguments.Contains("--live-check")){Connect();Dispatcher.BeginInvoke(async()=>{await Task.Delay(500);await RunLiveCheck();});}
  else if(App.Arguments.Contains("--connect"))Connect();
  if(App.Arguments.Contains("--visual-review"))Dispatcher.BeginInvoke(async()=>{await Task.Delay(400);await RunVisualReview();});
  if(App.Arguments.Contains("--ui-check")&&!App.Arguments.Contains("--visual-review"))Dispatcher.BeginInvoke(async()=>{await Task.Delay(400);RunUiCheck();});
 }
 void RegisterShortcuts(){if(hotkeys==null)return;var failed=new List<string>();for(var i=0;i<cheats.Count;i++)if(cheats[i].Supported&&!hotkeys.Set(i+1,cheats[i].Hotkey))failed.Add(cheats[i].Name);if(!hotkeys.Set(90,settings.SpawnHotkey))failed.Add("Spawn selected item");if(!hotkeys.Set(99,"Ctrl+Alt+F12"))failed.Add("Disable all");if(failed.Count>0)ShowNotice("Some shortcuts are already in use: "+string.Join(", ",failed)+". Choose alternatives in Settings.");}
 void HotkeyPressed(int id){if(id==99){DisableAll();return;}if(memory==null||!memory.Alive)return;Hotkeys.GetWindowThreadProcessId(Hotkeys.GetForegroundWindow(),out var pid);if(pid!=memory.Pid)return;if(id==90){SpawnClick(this,new());return;}if(id>=1&&id<=cheats.Count)Toggle(cheats[id-1]);}
 void NavigateClick(object sender,RoutedEventArgs e)=>Navigate((string)((Button)sender).Tag);
 void Navigate(string name){pageName=name;ItemsPage.Visibility=name=="items"?Visibility.Visible:Visibility.Collapsed;CheatsPage.Visibility=name=="cheats"?Visibility.Visible:Visibility.Collapsed;TravelPage.Visibility=name=="travel"?Visibility.Visible:Visibility.Collapsed;SettingsPage.Visibility=name=="settings"?Visibility.Visible:Visibility.Collapsed;foreach(var (b,id) in new[]{(NavItems,"items"),(NavCheats,"cheats"),(NavTravel,"travel"),(NavSettings,"settings")}){b.Background=id==name?(Brush)FindResource("ActionBrush"):Brushes.Transparent;b.BorderBrush=id==name?(Brush)FindResource("CyanBrush"):Brushes.Transparent;b.BorderThickness=new(1);b.Foreground=(Brush)FindResource(id==name?"TextBrush":"MutedBrush");Chrome.SetSelected(b,id==name);}Chrome.Reveal(name=="items"?ItemsPage:name=="cheats"?CheatsPage:name=="travel"?TravelPage:SettingsPage);}
 void CategoryClick(object sender,RoutedEventArgs e){category=(string)((Button)sender).Tag;group="All types";page=0;GroupFilter.Items.Clear();GroupFilter.Items.Add("All types");foreach(var g in catalog.Where(x=>category=="All items"||x.Category==category).Select(x=>x.Group).Where(x=>x!="").Distinct().Order())GroupFilter.Items.Add(g);GroupFilter.SelectedIndex=0;RefreshItems();}
 void SearchChanged(object sender,TextChangedEventArgs e){if(SearchHint!=null)SearchHint.Visibility=string.IsNullOrEmpty(SearchBox.Text)?Visibility.Visible:Visibility.Collapsed;if(!initialized)return;searchTimer.Stop();searchTimer.Start();}
 void FilterChanged(object sender,SelectionChangedEventArgs e){if(!initialized)return;group=GroupFilter.SelectedItem?.ToString()??"All types";page=0;RefreshItems();}
 void FavoritesClick(object sender,RoutedEventArgs e){favoritesOnly=!favoritesOnly;page=0;RefreshItems();}
 void RefreshItems(){
  if(!initialized)return;
  var order=(SortBox.SelectedItem as ComboBoxItem)?.Content?.ToString()??"Name A–Z";
  filtered=Catalog.Filter(catalog,SearchBox.Text,category,group,favoritesOnly,order,settings.Recent).ToList();
  var pages=Math.Max(1,(filtered.Count+PageSize-1)/PageSize);page=Math.Clamp(page,0,pages-1);ItemList.ItemsSource=filtered.Skip(page*PageSize).Take(PageSize).ToList();
  EmptyResults.Visibility=filtered.Count==0?Visibility.Visible:Visibility.Collapsed;ResultCount.Text=$"{filtered.Count:N0} results";
  PageLabel.Text=$"Page {page+1} of {pages}";PreviousButton.IsEnabled=page>0;NextButton.IsEnabled=page<pages-1;
  FavoritesButton.Content=favoritesOnly?"★ Favorites":"☆ Favorites";FavoritesButton.Foreground=(Brush)FindResource(favoritesOnly?"GoldBrush":"TextBrush");Chrome.SetSelected(FavoritesButton,favoritesOnly);
  foreach(var (c,b) in categoryButtons){Chrome.SetSelected(b,c==category);}
 }
 void ItemSelected(object sender,SelectionChangedEventArgs e){if(ItemList.SelectedItem is Item item)SelectItem(item);}
 void SelectItem(Item item){selected=item;DetailName.Text=item.Name;DetailCategory.Text=item.Category.ToUpperInvariant();DetailId.Text=SpawnCatalog.WorldActor(item.Id);DetailImage.Source=Artwork.Load(item.ImagePath,256);DetailFavorite.Content=item.Star;SpecialItemNote.Text=item.UnitsPerPickup>1?$"Each pickup contains {item.UnitsPerPickup} arrows. Quantity counts bundles.":item.Special?"Special item: world spawning may differ from obtaining it normally.":"";settings.Recent.Remove(item.Id);settings.Recent.Insert(0,item.Id);if(settings.Recent.Count>60)settings.Recent.RemoveAt(60);UpdateSpawnControl();SaveSettings();}
 void UpdateSpawnControl(){SpawnButton.IsEnabled=memory!=null&&advancedCompatible&&!spawning&&selected!=null&&worldActors.Contains(SpawnCatalog.WorldActor(selected.Id));SpawnStatus.Text=memory==null?"Connect to your game to check item spawning.":!advancedCompatible?"Item spawning is not compatible with this recomp build.":selected!=null&&!worldActors.Contains(SpawnCatalog.WorldActor(selected.Id))?"This entry has no world actor in your game. It cannot be dropped in front of Link.":spawning?"Creating your item…":"Experimental • spawns in front of Link. A save backup is created before first use.";SpawnButton.ToolTip=SpawnStatus.Text;}
 void FavoriteClick(object sender,RoutedEventArgs e){var item=(Item)((Button)sender).Tag;Favorite(item);e.Handled=true;}
 void DetailFavoriteClick(object sender,RoutedEventArgs e){if(selected!=null)Favorite(selected);}
 void Favorite(Item item){item.Favorite=!item.Favorite;if(item.Favorite)settings.Favorites.Add(item.Id);else settings.Favorites.Remove(item.Id);if(selected==item)DetailFavorite.Content=item.Star;SaveSettings();if(favoritesOnly)RefreshItems();}
 void ClearFiltersClick(object sender,RoutedEventArgs e){SearchBox.Clear();category="All items";group="All types";favoritesOnly=false;GroupFilter.SelectedIndex=0;page=0;RefreshItems();}
 void PreviousClick(object sender,RoutedEventArgs e){page--;RefreshItems();}
 void NextClick(object sender,RoutedEventArgs e){page++;RefreshItems();}
 void CopyIdClick(object sender,RoutedEventArgs e){if(selected==null)return;try{Clipboard.SetText(SpawnCatalog.WorldActor(selected.Id));FooterStatus.Text="Copied "+SpawnCatalog.WorldActor(selected.Id);}catch{ShowNotice("The clipboard is busy. Try copying again.");}}
 void ConnectClick(object sender,RoutedEventArgs e){if(memory!=null)Disconnect();else Connect();}
 void Connect(){
  try{memory=new(settings.GamePath);engine=new(memory);advancedCompatible=FrameBridge.IsCompatible(settings.GamePath);LoadWorldActors();ConnectionStatus.Text="Connected • 1.2.1";StatusDot.Fill=Brush("#73E3B7");ConnectButton.Content="Disconnect";foreach(var c in cheats){c.Available=c.Supported;c.Note=c.Supported?"Ready • off":"Not supported by this build";}NoticeBorder.Visibility=Visibility.Collapsed;FooterStatus.Text="Game connected • every cheat is off";UpdateSpawnControl();gameTimer.Start();WriteReport("connection-report.json");}
  catch(Exception e){memory?.Dispose();memory=null;engine=null;ShowNotice(e.Message);ConnectionStatus.Text="Connection unavailable";StatusDot.Fill=Brush("#C9AE70");}
 }
 void Disconnect(){gameTimer.Stop();DisableAll();try{bridge?.Dispose();bridge=null;}catch(Exception e){ShowNotice("The game extension could not disconnect cleanly: "+e.Message);}memory?.Dispose();memory=null;engine=null;advancedCompatible=false;ConnectionStatus.Text="Not connected";StatusDot.Fill=Brush("#C9AE70");ConnectButton.Content="Connect to game";foreach(var c in cheats){c.Available=false;if(c.Supported)c.Note="Connect to the game";}UpdateSpawnControl();FooterStatus.Text="Disconnected • cheats are off";}
 void CheatClick(object sender,RoutedEventArgs e){Toggle((Cheat)((CheckBox)sender).Tag);e.Handled=true;}
 void Toggle(Cheat c){
  if(c.Active){try{engine?.Stop(c.Id);c.Active=false;c.Note="Off";}catch(Exception e){c.Active=false;c.Note="Off • restoration failed";ShowNotice(e.Message);}UpdateActive();return;}
  if(engine==null||memory==null||!memory.Alive||!c.Supported){ShowNotice("Connect to a supported game before enabling this control.");return;}
  try{if(c.Id=="jump"){engine.Plan(c.Id,c.Value);jumpBoosted=false;}else engine.Apply(c.Id,c.Id=="speed"?SpeedSlider.Value:c.Value);c.Active=true;c.Note=c.Id=="jump"?"Experimental • boost once per jump press":"Live • updating while enabled";NoticeBorder.Visibility=Visibility.Collapsed;}
  catch(Exception e){c.Active=false;c.Note=e.Message;ShowNotice(c.Name+": "+e.Message);}UpdateActive();
 }
 void GameTick(object? sender,EventArgs e){
  if(memory==null)return;if(!memory.Alive){Disconnect();ShowNotice("The game closed. Every cheat has been switched off.");return;}
  foreach(var c in cheats.Where(x=>x.Active).ToArray())try{if(c.Id=="jump"){if(!JumpInput.Pressed(memory.Pid)){jumpBoosted=false;continue;}if(jumpBoosted)continue;var v=engine!.JumpVelocity();if(v<=0.3f||v>25)continue;engine.Apply(c.Id,c.Value);jumpBoosted=true;}else engine!.Apply(c.Id,c.Id=="speed"?SpeedSlider.Value:c.Value);}catch(Exception ex){try{engine!.Stop(c.Id);}catch{}c.Active=false;c.Note="Stopped: "+ex.Message;ShowNotice(c.Name+" stopped because its game data is unavailable.");}UpdateActive();
 }
 void ShowCheats(IEnumerable<Cheat> source){var view=CollectionViewSource.GetDefaultView(source.ToList());view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(Cheat.Category)));CheatList.ItemsSource=view;}
 void UpdateActive(){var active=cheats.Where(c=>c.Active).ToList();var n=active.Count;ActiveCount.Text=$"{n} active";if(memory!=null)FooterStatus.Text=$"Connected • {n} cheat{(n==1?"":"s")} active";var ids=string.Join("|",active.Select(c=>c.Id));if(ActiveOnly.IsChecked==true&&renderedActive!=ids){ShowCheats(active);renderedActive=ids;}}
 void ActiveOnlyClick(object sender,RoutedEventArgs e){renderedActive=string.Join("|",cheats.Where(c=>c.Active).Select(c=>c.Id));ShowCheats(ActiveOnly.IsChecked==true?cheats.Where(c=>c.Active):cheats);}
 void DisableAllClick(object sender,RoutedEventArgs e)=>DisableAll();
 void DisableAll(){var errors=new List<string>();foreach(var c in cheats.Where(c=>c.Active)){try{if(memory?.Alive==true)engine?.Stop(c.Id);}catch(Exception e){errors.Add(e.Message);}c.Active=false;c.Note="Off";}UpdateActive();if(errors.Count>0)ShowNotice("Cheats stopped, but a value could not be restored: "+errors[0]);}
 void PresetClick(object sender,RoutedEventArgs e){if(engine==null){ShowNotice("Connect to the game before enabling a preset.");return;}var set=cheats.Where(c=>c.Id is "health" or "stamina" or "battery").ToArray();try{foreach(var c in set)engine.Plan(c.Id);foreach(var c in set)if(!c.Active)Toggle(c);}catch(Exception ex){ShowNotice("The exploration preset is not ready: "+ex.Message);}}
 void SpeedChanged(object sender,RoutedPropertyChangedEventArgs<double> e){if(SpeedLabel!=null)SpeedLabel.Text=e.NewValue.ToString("0.00",CultureInfo.InvariantCulture)+"×";}
 void JumpChanged(object sender,RoutedPropertyChangedEventArgs<double> e){if(JumpLabel!=null)JumpLabel.Text=e.NewValue.ToString("0.0",CultureInfo.InvariantCulture)+"×";var c=cheats.FirstOrDefault(c=>c.Id=="jump");if(c!=null)c.Value=e.NewValue;}
 void BrowseClick(object sender,RoutedEventArgs e){var dialog=new OpenFileDialog{Title="Choose your TOTK recomp",Filter="TOTK executable (totk.exe)|totk.exe",CheckFileExists=true};if(dialog.ShowDialog(this)!=true)return;Disconnect();settings.GamePath=dialog.FileName;GamePathBox.Text=settings.GamePath;SaveSettings();}
 void TopmostClick(object sender,RoutedEventArgs e){Topmost=settings.AlwaysOnTop=TopmostCheck.IsChecked==true;SaveSettings();}
 void CaptureHotkey(object sender,KeyEventArgs e){
  e.Handled=true;if(hotkeys==null)return;var c=(Cheat)((TextBox)sender).Tag;var key=e.Key==Key.System?e.SystemKey:e.Key;
  if(key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)return;
  var text=key==Key.Delete?"":Hotkeys.Format(key,Keyboard.Modifiers);
  if(text!=""&&(!Hotkeys.TryParse(text,out _,out _)||Keyboard.Modifiers.HasFlag(ModifierKeys.Windows))){ShowNotice("Choose a key with Ctrl, Alt or Shift. Delete clears a shortcut.");return;}
  if(text=="Ctrl+Alt+F12"||(text!=""&&text==settings.SpawnHotkey)||cheats.Any(x=>x!=c&&x.Hotkey==text&&text!="")){ShowNotice("That shortcut is already assigned. Choose another combination.");return;}
  var index=cheats.IndexOf(c)+1;var old=c.Hotkey;if(!hotkeys.Set(index,text)){hotkeys.Set(index,old);ShowNotice("Another application uses that shortcut. Choose another combination.");return;}c.Hotkey=text;settings.Hotkeys[c.Id]=text;SaveSettings();NoticeBorder.Visibility=Visibility.Collapsed;
 }
 void ResetHotkeysClick(object sender,RoutedEventArgs e){settings.Hotkeys.Clear();for(var i=0;i<7;i++)cheats[i].Hotkey=$"Ctrl+Alt+{i+1}";cheats.First(c=>c.Id=="jump").Hotkey="Ctrl+Alt+8";settings.SpawnHotkey="Ctrl+Alt+S";SpawnShortcutBox.Text=settings.SpawnHotkey;hotkeys?.Dispose();RegisterShortcuts();SaveSettings();}
 void CaptureSpawnHotkey(object sender,KeyEventArgs e){e.Handled=true;if(hotkeys==null)return;var key=e.Key==Key.System?e.SystemKey:e.Key;if(key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift)return;var text=key==Key.Delete?"":Hotkeys.Format(key,Keyboard.Modifiers);if(text!=""&&(!Hotkeys.TryParse(text,out _,out _)||Keyboard.Modifiers.HasFlag(ModifierKeys.Windows)||text=="Ctrl+Alt+F12"||cheats.Any(c=>c.Hotkey==text))){ShowNotice("Choose an unused shortcut with Ctrl, Alt or Shift. Delete removes it.");return;}if(!hotkeys.Set(90,text)){hotkeys.Set(90,settings.SpawnHotkey);ShowNotice("Another application is using that shortcut.");return;}settings.SpawnHotkey=text;SpawnShortcutBox.Text=text;SaveSettings();}
 void ReturnToGameClick(object sender,RoutedEventArgs e){settings.ReturnToGame=ReturnToGameCheck.IsChecked==true;SaveSettings();}
 void SavePlaceClick(object sender,RoutedEventArgs e){if(memory==null){ShowNotice("Connect to the game before saving a place.");return;}try{var pose=PlayerLocator.Read(memory);var name=PlaceNameBox.Text.Trim();if(name.Length<1||name.Length>60){ShowNotice("Name this place using 1–60 characters.");return;}settings.Places.Add(new(name,pose.X,pose.Y,pose.Z,pose.Rotation));PlaceList.ItemsSource=settings.Places.ToList();PlaceNameBox.Clear();SaveSettings();ShowNotice("Saved your current position as "+name+".");}catch(Exception ex){ShowNotice(ex.Message);}}
 void RemovePlaceClick(object sender,RoutedEventArgs e){if(PlaceList.SelectedItem is not SavedPlace place)return;settings.Places.Remove(place);PlaceList.ItemsSource=settings.Places.ToList();SaveSettings();}
 async void TravelClick(object sender,RoutedEventArgs e){if(PlaceList.SelectedItem is not SavedPlace place){ShowNotice("Choose a saved place first.");return;}await TravelTo(place.Pose,false);}
 async void UndoTravelClick(object sender,RoutedEventArgs e){if(undoTravel!=null)await TravelTo(undoTravel,true);}
 async Task TravelTo(PlayerPose destination,bool undo){
  if(memory==null||!advancedCompatible){ShowNotice("Connect to your original TOTK 1.2.1 recomp to use travel.");return;}if(spawning){ShowNotice("Wait for the current game action to finish.");return;}
  var game=memory;spawning=true;UpdateSpawnControl();TravelButton.IsEnabled=false;UndoTravelButton.IsEnabled=false;
  try{var before=PlayerLocator.Read(game);var distance=Math.Sqrt(Math.Pow(before.X-destination.X,2)+Math.Pow(before.Y-destination.Y,2)+Math.Pow(before.Z-destination.Z,2));if(distance>2000)throw new InvalidOperationException("This preview supports nearby travel within 2,000 meters. Move closer to that place first.");BackupSaves();bridge??=new FrameBridge(game);if(settings.ReturnToGame)game.FocusGame();bridge.QueueTeleport(destination);var start=DateTime.UtcNow;while(bridge.State!=3){await Task.Delay(40);if(memory!=game||!game.Alive)throw new InvalidOperationException("The game disconnected.");if((DateTime.UtcNow-start).TotalSeconds>60)throw new InvalidOperationException("Return to the game to finish traveling.");}await Task.Delay(120);var after=PlayerLocator.Read(game);if(Math.Abs(after.X-destination.X)>0.5||Math.Abs(after.Z-destination.Z)>0.5||Math.Abs(after.Y-destination.Y)>4)throw new InvalidOperationException("The game did not move Link to the requested position.");undoTravel=before;ShowNotice(undo?"Returned to your previous position.":"Arrived at your saved place. You can undo this teleport.");File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"travel-report.json"),JsonSerializer.Serialize(new{time=DateTimeOffset.Now,before,destination,after},new JsonSerializerOptions{WriteIndented=true}));}
  catch(Exception ex){ShowNotice("Travel: "+ex.Message);}finally{spawning=false;TravelButton.IsEnabled=true;UndoTravelButton.IsEnabled=undoTravel!=null;UpdateSpawnControl();}
 }
 void ReportClick(object sender,RoutedEventArgs e){try{WriteReport("connection-report.json");ShowNotice("Connection report saved next to HyruleCompanion.exe. It stays on this computer.");}catch(Exception ex){ShowNotice(ex.Message);}}
 object PoseReport(){try{return memory!=null?PlayerLocator.Read(memory):"Not connected";}catch(Exception e){return e.Message;}}
 void WriteReport(string filename)=>File.WriteAllText(Path.Combine(AppContext.BaseDirectory,filename),JsonSerializer.Serialize(new{time=DateTimeOffset.Now,game=settings.GamePath,connected=memory!=null,pid=memory?.Pid,build=NativeMemory.BuildId,values=engine?.Snapshot(),player=PoseReport(),spawningCompatible=advancedCompatible,worldActorCount=worldActors.Count,frameCallback=bridge==null?null:new{state=bridge.State,frames=bridge.Frames},active=cheats.Where(c=>c.Active).Select(c=>c.Id).ToArray()},new JsonSerializerOptions{WriteIndented=true}));
 void LoadWorldActors(){worldActors.Clear();try{using var doc=JsonDocument.Parse(File.ReadAllText(Path.Combine(Path.GetDirectoryName(settings.GamePath)!,"romfs","index.json")));foreach(var f in doc.RootElement.GetProperty("files").EnumerateArray()){var p=f.GetProperty("path").GetString()??"";if(p.StartsWith("Pack/Actor/",StringComparison.Ordinal)&&p.EndsWith(".pack.zs",StringComparison.Ordinal))worldActors.Add(p[11..^8]);}}catch{ShowNotice("The game’s actor catalog could not be read. Item spawning is unavailable.");}}
 void BackupSaves(){var source=Path.Combine(Path.GetDirectoryName(settings.GamePath)!,"profile","save");var destination=Path.Combine(AppContext.BaseDirectory,"backups","before-first-spawn");var done=Path.Combine(destination,"backup-complete.txt");if(File.Exists(done))return;if(!Directory.Exists(source))throw new InvalidOperationException("Your save folder could not be found, so spawning was not enabled.");foreach(var file in Directory.EnumerateFiles(source,"*",SearchOption.AllDirectories)){var relative=Path.GetRelativePath(source,file);var target=Path.Combine(destination,relative);Directory.CreateDirectory(Path.GetDirectoryName(target)!);File.Copy(file,target,true);}File.WriteAllText(done,"Save files copied before the first native frame extension was attached.\nSource: "+source+"\nTime: "+DateTimeOffset.Now.ToString("O"));}
 async void SpawnClick(object sender,RoutedEventArgs e){
  if(memory==null||selected==null||!advancedCompatible||spawning)return;
  if(!int.TryParse(QuantityBox.Text,out var quantity)||quantity<1||quantity>25||!double.TryParse(DistanceBox.Text,NumberStyles.Float,CultureInfo.InvariantCulture,out var distance)||!double.IsFinite(distance)||distance<1||distance>10){ShowNotice("Choose a quantity from 1 to 25 and a distance from 1 to 10 meters. Use a decimal point, for example 2.5.");return;}
  if(!worldActors.Contains(SpawnCatalog.WorldActor(selected.Id))){ShowNotice("The selected item has no world actor in this game.");return;}
  var game=memory;var item=selected;var worldActor=SpawnCatalog.WorldActor(item.Id);spawning=true;UpdateSpawnControl();var spawned=new List<object>();string? error=null;
  try{
   BackupSaves();bridge??=new FrameBridge(game);var first=bridge.Frames;if(settings.ReturnToGame)game.FocusGame();SpawnStatus.Text="Return to the game to finish spawning…";var start=DateTime.UtcNow;while(bridge.Frames==first){await Task.Delay(40);if(memory!=game||!game.Alive)throw new InvalidOperationException("The game disconnected.");if((DateTime.UtcNow-start).TotalSeconds>60)throw new InvalidOperationException("The game is not presenting frames. Resume gameplay and try again.");}
   for(var n=0;n<quantity;n++){
    if(memory!=game||!game.Alive)throw new InvalidOperationException("The game disconnected.");bridge.QueueSpawn(item.Id,n,distance);start=DateTime.UtcNow;
    while(bridge.State!=3){await Task.Delay(30);if(memory!=game||!game.Alive)throw new InvalidOperationException("The game disconnected.");if((DateTime.UtcNow-start).TotalSeconds>5)throw new InvalidOperationException("The game has not completed the spawn request. No further items were queued.");}
    if(!bridge.Result||bridge.PreActor==0)throw new InvalidOperationException($"The game declined this item (result {bridge.ResultCode}).");var pre=bridge.PreActor;ulong actor=0;
    while(actor==0){await Task.Delay(40);if(memory!=game||!game.Alive)throw new InvalidOperationException("The game disconnected.");actor=BitConverter.ToUInt64(game.Read(pre+0x20,8));if((DateTime.UtcNow-start).TotalSeconds>15)throw new InvalidOperationException("The game accepted the request, but the item has not finished loading.");}
    var namePointer=BitConverter.ToUInt64(game.Read(actor+0x218,8));var name=System.Text.Encoding.UTF8.GetString(game.Read(namePointer,96)).Split('\0')[0];if(name!=worldActor)throw new InvalidOperationException("The spawned actor could not be verified.");var pos=game.Read(actor+0x2B4,12);spawned.Add(new{actor=actor.ToString("X"),name,x=BitConverter.ToSingle(pos),y=BitConverter.ToSingle(pos,4),z=BitConverter.ToSingle(pos,8)});SpawnStatus.Text=$"Created {n+1} of {quantity} • {item.Name}";
   }
   var label=item.UnitsPerPickup>1?$"{quantity} bundle{(quantity==1?"":"s")} ({quantity*item.UnitsPerPickup} arrows)":$"{quantity} × {item.Name}";
   FooterStatus.Text=$"Spawned {label} in front of Link.";ShowNotice($"Spawned {label}. Your original save backup is in the app’s backups folder.");
  }catch(Exception ex){error=ex.Message;ShowNotice("Item spawning: "+ex.Message);}
  finally{File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"spawn-report.json"),JsonSerializer.Serialize(new{time=DateTimeOffset.Now,item=item.Id,worldActor,requested=quantity,unitsPerPickup=item.UnitsPerPickup,totalUnits=quantity*item.UnitsPerPickup,error,verifiedActors=spawned,player=PoseReport()},new JsonSerializerOptions{WriteIndented=true}));spawning=false;UpdateSpawnControl();}
 }
 void ShowNotice(string message){Notice.Text=message;var success=message.StartsWith("Spawned ")||message.StartsWith("Saved ")||message.StartsWith("Arrived ")||message.StartsWith("Returned ")||message.StartsWith("Connection report saved");Notice.Foreground=NoticeBorder.BorderBrush=(Brush)FindResource(success?"SuccessBrush":"WarningBrush");NoticeBorder.Visibility=Visibility.Visible;}
 void SaveSettings(){try{settings.Save();}catch(Exception e){FooterStatus.Text="Preferences could not be saved: "+e.Message;}}
 void WindowKey(object sender,KeyEventArgs e){if(e.Key==Key.F&&Keyboard.Modifiers==ModifierKeys.Control){Navigate("items");SearchBox.Focus();SearchBox.SelectAll();e.Handled=true;}if(e.Key==Key.Escape&&Keyboard.FocusedElement==SearchBox){SearchBox.Clear();e.Handled=true;}}
 void TitleDrag(object sender,MouseButtonEventArgs e){if(e.ClickCount==2)WindowState=WindowState==WindowState.Maximized?WindowState.Normal:WindowState.Maximized;else if(e.LeftButton==MouseButtonState.Pressed)DragMove();}
 void MinimizeClick(object sender,RoutedEventArgs e)=>WindowState=WindowState.Minimized;
 void MaximizeClick(object sender,RoutedEventArgs e)=>WindowState=WindowState==WindowState.Maximized?WindowState.Normal:WindowState.Maximized;
 void CloseClick(object sender,RoutedEventArgs e)=>Close();
 void WindowClosing(object? sender,CancelEventArgs e){if(closing)return;closing=true;Disconnect();hotkeys?.Dispose();searchTimer.Stop();SaveSettings();}
 static SolidColorBrush Brush(string color)=>(SolidColorBrush)new BrushConverter().ConvertFromString(color)!;
 async Task RunLiveCheck(){
  var checks=new Dictionary<string,object>();
  try{if(engine==null||memory==null)throw new InvalidOperationException(Notice.Text);checks["readOnlyPreflight"]=engine.Snapshot();var a=NativeMemory.Main+0x3877F34;var before=memory.Read(a,4);try{engine.Apply("speed",1.25);checks["speedWriteReadback"]=BitConverter.ToSingle(memory.Read(a,4))==1.25f;}finally{engine.Stop("speed");}checks["speedRestoredExactly"]=memory.Read(a,4).SequenceEqual(before);checks["activeAfterCheck"]=cheats.Count(x=>x.Active);if(advancedCompatible){var pose=PlayerLocator.Read(memory);var up=pose with{X=pose.X+0.75f,Y=pose.Y+0.25f};try{await TravelTo(up,false);var moved=PlayerLocator.Read(memory);checks["teleportMovedPlayerHorizontally"]=moved.X>pose.X+0.35f;checks["teleportReadback"]=moved;}finally{await TravelTo(pose,true);}var restored=PlayerLocator.Read(memory);checks["teleportReturnedPlayer"]=Math.Abs(restored.X-pose.X)<0.5f&&Math.Abs(restored.Y-pose.Y)<0.5f&&Math.Abs(restored.Z-pose.Z)<0.5f;}}
  catch(Exception e){checks["error"]=e.ToString();}
  File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"live-check.json"),JsonSerializer.Serialize(checks,new JsonSerializerOptions{WriteIndented=true}));
 }
 void RunUiCheck(){
  var checks=new Dictionary<string,object>();
  try{var previous=selected;checks["catalogCount"]=catalog.Count;SearchBox.Text="hylian shield";searchTimer.Stop();RefreshItems();checks["searchFindsHylianShield"]=filtered.Any(x=>x.Id=="Weapon_Shield_030");SearchBox.Clear();RefreshItems();var item=catalog.First(x=>x.Id=="Weapon_Sword_070");var prior=item.Favorite;Favorite(item);checks["favoriteToggle"]=item.Favorite!=prior;Favorite(item);SelectItem(item);checks["spawnAvailabilityMatchesGame"]=SpawnButton.IsEnabled==(memory!=null&&advancedCompatible&&worldActors.Contains(SpawnCatalog.WorldActor(item.Id)));checks["allCheatsInitiallyOff"]=cheats.All(c=>!c.Active);Navigate("cheats");checks["cheatsPageVisible"]=CheatsPage.Visibility==Visibility.Visible;Navigate("settings");checks["settingsPageVisible"]=SettingsPage.Visibility==Visibility.Visible;Navigate("items");if(previous!=null)SelectItem(previous);checks["layoutWidth"]=ActualWidth;}
  catch(Exception e){checks["error"]=e.ToString();}
  try{var previous=selected;var five=catalog.Single(x=>x.Id=="Obj_ArrowBundle_A_01");var ten=catalog.Single(x=>x.Id=="Obj_ArrowBundle_A_02");checks["fiveArrowBundleInCatalog"]=five.UnitsPerPickup==5;checks["tenArrowBundleInCatalog"]=ten.UnitsPerPickup==10;SelectItem(five);checks["bundleQuantityExplained"]=SpecialItemNote.Text.Contains("5 arrows")&&SpecialItemNote.Text.Contains("bundles");if(App.Arguments.Contains("--arrow-preview")){CategoryClick(categoryButtons["Arrows"],new());SelectItem(five);}else if(previous!=null)SelectItem(previous);}catch(Exception ex){checks["bundleError"]=ex.ToString();}
  File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"ui-check.json"),JsonSerializer.Serialize(checks,new JsonSerializerOptions{WriteIndented=true}));
  Dispatcher.BeginInvoke(async()=>{await Task.Delay(200);UpdateLayout();var bitmap=new RenderTargetBitmap((int)ActualWidth,(int)ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(this);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using var file=File.Create(Path.Combine(AppContext.BaseDirectory,"preview.png"));png.Save(file);});
 }
}
