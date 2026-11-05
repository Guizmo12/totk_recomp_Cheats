using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
namespace HyruleCompanion;
public partial class MainWindow {
 readonly DispatcherTimer mapTimer=new(){Interval=TimeSpan.FromMilliseconds(650)};
 List<MapLocation> landmarks=[];bool updatingDestination;bool previousHud;
 bool HudCoordinates=>CoordinateSpace.SelectedIndex==1;
 readonly List<MapCategory> mapCategories=[];
 readonly HashSet<string> enabledMapCategories=[];
 readonly Dictionary<string,CheckBox> mapCategoryControls=[];
 readonly Dictionary<string,Expander> mapGroupControls=[];
 bool changingMapFilters;bool compendiumMapFilters;
 readonly DispatcherTimer mapSearchTimer=new(){Interval=TimeSpan.FromMilliseconds(180)};
 T LoadMapResource<T>(string name){using var stream=Application.GetResourceStream(new Uri("/HyruleCompanion;component/Resources/Maps/"+name,UriKind.Relative)).Stream;return JsonSerializer.Deserialize<T>(stream,new JsonSerializerOptions{NumberHandling=System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals})!;}
 void InitializeTravelMap(){
  landmarks=LoadMapResource<List<MapLocation>>("landmarks.json");
  mapCategories.AddRange(LoadMapResource<List<MapCategory>>("categories.json").Select(c=>c with{Group=MapFilters.MainGroups.FirstOrDefault(g=>g.Value.Contains(c.Kind)).Key??(c.Kind=="Saved"?"Your places":c.Kind.StartsWith("Resource: ",StringComparison.Ordinal)?c.Group:"Additional map data")}));
  if(settings.MapFilterLayoutVersion<1){settings.MapCategories=null;settings.MapFilterLayoutVersion=1;}
  enabledMapCategories.UnionWith(settings.MapCategories??mapCategories.Where(c=>MapFilters.IsPrimary(c.Kind)||c.Kind=="Saved").Select(c=>c.Kind).ToHashSet());
  settings.MapCategories=enabledMapCategories.ToHashSet();SaveSettings();
  BuildMapFilters();UpdateMapFilterMode();TravelMap.CategoryGroups=mapCategories.ToDictionary(c=>c.Kind,c=>c.Group);
  TravelMap.DestinationPicked+=(place,doubleClick)=>PickMapDestination(place,doubleClick||MapClickTeleport.IsChecked==true);
  TravelMap.PointerMoved+=(x,z)=>MapPointerLabel.Text=$"X {x:0}   Z {z:0}";
  mapSearchTimer.Tick+=(_,_)=>{mapSearchTimer.Stop();RefreshTravelMap();};
  mapTimer.Tick+=(_,_)=>{UpdateTravelReadiness();if(TravelPage.IsVisible)UpdateMapPlayer();};mapTimer.Start();SetMapLayer("Surface");
 }
 void BuildMapFilters(){
  foreach(var group in mapCategories.GroupBy(c=>c.Group).OrderBy(g=>Array.IndexOf(new[]{"Travel Gates","Quests","Collectibles","Locations","Services","Other","Bosses","Your places"},g.Key) is var i&&i>=0?i:100).ThenBy(g=>g.Key)){
   var body=new StackPanel();var actions=new WrapPanel();
   foreach(var show in new[]{true,false}){var button=new Button{Content=show?"Show group":"Hide group",FontSize=10,Padding=new Thickness(7,4,7,4),Margin=new Thickness(0,0,5,7)};button.Click+=(_,_)=>{foreach(var cat in group){if(show)enabledMapCategories.Add(cat.Kind);else enabledMapCategories.Remove(cat.Kind);}ApplyMapFilters();};actions.Children.Add(button);}body.Children.Add(actions);
   foreach(var cat in group.OrderBy(c=>MapFilters.MainGroups.TryGetValue(group.Key,out var order)?Array.IndexOf(order,c.Kind):0).ThenBy(c=>MapFilters.IsPrimary(c.Kind)?"":c.DisplayName)){var box=new CheckBox{Tag=cat.Kind,IsChecked=enabledMapCategories.Contains(cat.Kind),FontSize=11,Foreground=System.Windows.Media.Brushes.White,Margin=new Thickness(1,5,1,5),ToolTip=cat.Kind+" · "+cat.Count.ToString("N0")+" across all layers"};box.Click+=(_,_)=>{if(changingMapFilters)return;if(box.IsChecked==true)enabledMapCategories.Add(cat.Kind);else enabledMapCategories.Remove(cat.Kind);ApplyMapFilters();};mapCategoryControls[cat.Kind]=box;body.Children.Add(box);}
   var expander=new Expander{Header=group.Key,Content=body,Foreground=(System.Windows.Media.Brush)FindResource("GoldBrush"),FontSize=12,Margin=new Thickness(0,0,0,3),IsExpanded=false};mapGroupControls[group.Key]=expander;MapFilterGroups.Children.Add(expander);
  }
 }
 void ApplyMapFilters(bool persist=true){changingMapFilters=true;try{foreach(var (kind,box) in mapCategoryControls)box.IsChecked=enabledMapCategories.Contains(kind);}finally{changingMapFilters=false;}if(persist){settings.MapCategories=enabledMapCategories.ToHashSet();SaveSettings();}RefreshTravelMap();}
 void ToggleMapFilters(object sender,RoutedEventArgs e){MapFilterDrawer.Visibility=MapFilterDrawer.IsVisible?Visibility.Collapsed:Visibility.Visible;Chrome.SetSelected(MapFiltersButton,MapFilterDrawer.Visibility==Visibility.Visible);}
 bool InCurrentMapFilterSection(MapCategory c)=>compendiumMapFilters?!MapFilters.IsPrimary(c.Kind)&&c.Kind!="Saved":MapFilters.IsPrimary(c.Kind)||c.Kind=="Saved";
 void MapPresetClick(object sender,RoutedEventArgs e){var show=(string)((Button)sender).Tag=="all";if(!show&&!compendiumMapFilters)enabledMapCategories.Clear();else foreach(var c in mapCategories.Where(InCurrentMapFilterSection)){if(show)enabledMapCategories.Add(c.Kind);else enabledMapCategories.Remove(c.Kind);}ApplyMapFilters();}
 void MapModeClick(object sender,RoutedEventArgs e){compendiumMapFilters=(string)((Button)sender).Tag=="compendium";MapCategorySearch.Clear();UpdateMapFilterMode();}
 void UpdateMapFilterMode(){MapFilterHeading.Text=compendiumMapFilters?"Hyrule Compendium":"Filter markers";Chrome.SetSelected(MapMarkerMode,!compendiumMapFilters);Chrome.SetSelected(MapCompendiumMode,compendiumMapFilters);MapCategorySearchChanged(this,new TextChangedEventArgs(TextBox.TextChangedEvent,UndoAction.None));RefreshTravelMap();}
 void MapCategorySearchChanged(object sender,TextChangedEventArgs e){if(!initialized)return;var query=MapCategorySearch.Text.Trim();foreach(var c in mapCategories)mapCategoryControls[c.Kind].Visibility=InCurrentMapFilterSection(c)&&(c.DisplayName+" "+c.Group).Contains(query,StringComparison.OrdinalIgnoreCase)?Visibility.Visible:Visibility.Collapsed;foreach(var (name,group) in mapGroupControls){group.Visibility=mapCategories.Any(c=>c.Group==name&&mapCategoryControls[c.Kind].Visibility==Visibility.Visible)?Visibility.Visible:Visibility.Collapsed;group.IsExpanded=query.Length>0;}}
 void RefreshTravelMap(){
  if(!initialized||TravelMap==null)return;
  var saved=settings.Places.Select(p=>new MapLocation(p.Name,"Saved",p.Y < -100?"Depths":p.Y>750?"Sky":"Surface",p.X,p.Y,p.Z)).ToList();
  var all=MapFilters.Apply(landmarks.Concat(saved),enabledMapCategories,MapSearch.Text).ToList();
  TravelMap.Locations=all;TravelMap.InvalidateVisual();
  var visible=all.Where(p=>p.Layer==TravelMap.Layer).OrderBy(p=>p.Name).ToList();LandmarkList.ItemsSource=visible;
  var counts=landmarks.Concat(saved).Where(p=>p.Layer==TravelMap.Layer).GroupBy(p=>p.Kind).ToDictionary(g=>g.Key,g=>g.Count());
  foreach(var c in mapCategories){var box=mapCategoryControls[c.Kind];box.Content=$"{c.DisplayName}  ·  {counts.GetValueOrDefault(c.Kind):N0}";box.Opacity=counts.GetValueOrDefault(c.Kind)>0?1:.5;}
  MapFiltersButton.Content=$"Filters · {enabledMapCategories.Count(MapFilters.IsPrimary)}/49";
  MapFilterSummary.Text=compendiumMapFilters?"Optional resources and additional map data. Enable only the types you need.":"49 categories · choose what to explore. Zoom in to reveal finer markers.";
  MapVisibleCount.Text=$"{visible.Count:N0} matches · zoom for detail";
 }
 void SetMapLayer(string layer){TravelMap.SetLayer(layer);foreach(var b in new[]{LayerSurface,LayerSky,LayerDepths})Chrome.SetSelected(b,Equals(b.Tag,layer));RefreshTravelMap();}
 void MapLayerClick(object sender,RoutedEventArgs e)=>SetMapLayer((string)((Button)sender).Tag);
 void MapFilterChanged(object sender,TextChangedEventArgs e){if(initialized){mapSearchTimer.Stop();mapSearchTimer.Start();}}
 void MapZoomIn(object sender,RoutedEventArgs e)=>TravelMap.ZoomBy(1.4);
 void MapZoomOut(object sender,RoutedEventArgs e)=>TravelMap.ZoomBy(1/1.4);
 void MapFit(object sender,RoutedEventArgs e)=>TravelMap.SetView(0,0,1);
 void MapLocatePlayer(object sender,RoutedEventArgs e){UpdateMapPlayer();if(TravelMap.Player is {} p){SetMapLayer(p.Y < -100?"Depths":p.Y>750?"Sky":"Surface");TravelMap.SetView(p.X,p.Z,4);}else ShowNotice("Connect to the game and load your save to locate Link.");}
 void UpdateMapPlayer(){if(memory==null||!memory.Alive){TravelMap.Player=null;PlayerCoordinatesLabel.Text="Connect to locate Link";TravelMap.InvalidateVisual();return;}try{var p=PlayerLocator.Read(memory);TravelMap.Player=p;PlayerCoordinatesLabel.Text=$"Link · X {p.X:0}  Y {p.Y:0}  Z {p.Z:0}";TravelMap.InvalidateVisual();}catch{TravelMap.Player=null;PlayerCoordinatesLabel.Text="Link unavailable in this scene";}}
 void PickMapDestination(MapLocation place,bool teleport){updatingDestination=true;try{DestinationName.Text=place.Name;TravelX.Text=place.X.ToString("0.###",CultureInfo.InvariantCulture);TravelZ.Text=(HudCoordinates?-place.Z:place.Z).ToString("0.###",CultureInfo.InvariantCulture);TravelY.Text=float.IsFinite(place.Y)?(HudCoordinates?place.Y-105:place.Y).ToString("0.###",CultureInfo.InvariantCulture):"";TravelMap.Selected=place;TravelMap.InvalidateVisual();}finally{updatingDestination=false;}UpdateTravelReadiness();if(teleport)CoordinateTravelClick(this,new());}
 void LandmarkSelected(object sender,SelectionChangedEventArgs e){if(LandmarkList.SelectedItem is not MapLocation p)return;SetMapLayer(p.Layer);PickMapDestination(p,false);TravelMap.SetView(p.X,p.Z,Math.Max(TravelMap.Zoom,3));}
 void SavedMapPlaceSelected(object sender,SelectionChangedEventArgs e){if(PlaceList.SelectedItem is SavedPlace p){var layer=p.Y < -100?"Depths":p.Y>750?"Sky":"Surface";SetMapLayer(layer);PickMapDestination(new(p.Name,"Saved",layer,p.X,p.Y,p.Z),false);TravelMap.SetView(p.X,p.Z,3);}}
 bool ReadTravelCoordinates(out float x,out float y,out float z){if(!TravelCoordinates.TryParse(TravelX.Text,TravelY.Text,TravelZ.Text,out x,out y,out z))return false;if(HudCoordinates){var world=TravelCoordinates.FromHud(x,z,y);x=world.X;y=world.Y;z=world.Z;}return float.IsFinite(y)&&Math.Abs(y)<=100000;}
 void CoordinateSpaceChanged(object sender,SelectionChangedEventArgs e){if(!initialized||TravelY==null)return;var valid=TravelCoordinates.TryParse(TravelX.Text,TravelY.Text,TravelZ.Text,out var x,out var y,out var z);if(valid&&previousHud){var world=TravelCoordinates.FromHud(x,z,y);x=world.X;y=world.Y;z=world.Z;}previousHud=HudCoordinates;MapAltitudeHelp.Text=HudCoordinates?"A map click sets X and Y. Enter Z (HUD altitude) before teleporting. No terrain sampling. Long-distance travel is experimental.":"A map click sets X and Z. Set altitude before teleporting: the map does not measure terrain height. Long-distance travel is experimental.";CoordinateExplanation.Text=HudCoordinates?"HUD order: X east • Y north • Z altitude":"World coordinates • X east / Z south / Y height";HorizontalCoordinateLabel.Text=HudCoordinates?"Y (north / south)":"Z (south / north)";AltitudeCoordinateLabel.Text=HudCoordinates?"Z — altitude (HUD)":"Y — altitude (meters)";if(valid)PickMapDestination(new("Custom coordinates","Point",TravelMap.Layer,x,y,z),false);else if(TravelCoordinates.TryParse(TravelX.Text,"0",TravelZ.Text,out _,out _,out var horizontal)){updatingDestination=true;try{TravelZ.Text=(-horizontal).ToString("0.###",CultureInfo.InvariantCulture);}finally{updatingDestination=false;}}UpdateTravelReadiness();}
 void TravelCoordinateChanged(object sender,TextChangedEventArgs e){if(!initialized||updatingDestination||TravelY==null)return;if(ReadTravelCoordinates(out var x,out var y,out var z)){TravelMap.Selected=new("Custom coordinates","Point",TravelMap.Layer,x,y,z);DestinationName.Text="Custom coordinates";TravelMap.InvalidateVisual();}UpdateTravelReadiness();}
 void UpdateTravelReadiness(){if(!initialized||CoordinateTravelButton==null)return;var valid=ReadTravelCoordinates(out _,out _,out _);CoordinateTravelButton.IsEnabled=valid&&memory?.Alive==true&&advancedCompatible&&!spawning;TravelCoordinateStatus.Text=!valid?"Enter X, Z and altitude as finite numbers (±100,000).":spawning?"Travel in progress…":memory==null?"Coordinates ready. Connect to the game to teleport.":!advancedCompatible?"This game build does not support travel.":"Ready • uses the entered altitude, not sampled terrain.";}
 async void CoordinateTravelClick(object sender,RoutedEventArgs e){if(!ReadTravelCoordinates(out var x,out var y,out var z)){ShowNotice("Enter valid X, Z and altitude before teleporting.");return;}if(memory==null||!advancedCompatible){ShowNotice("Connect to your supported game build before teleporting.");return;}try{var current=PlayerLocator.Read(memory);await TravelTo(current with{X=x,Y=y,Z=z},false);}catch(Exception ex){ShowNotice("Travel: "+ex.Message);}}
 void SaveDestinationClick(object sender,RoutedEventArgs e){if(!ReadTravelCoordinates(out var x,out var y,out var z)){ShowNotice("Enter all three destination coordinates first.");return;}var name=PlaceNameBox.Text.Trim();if(name.Length is <1 or >60){ShowNotice("Name this destination using 1–60 characters.");return;}settings.Places.Add(new(name,x,y,z,[1,0,0,0,1,0,0,0,1]));PlaceList.ItemsSource=settings.Places.ToList();PlaceNameBox.Clear();RefreshTravelMap();SaveSettings();ShowNotice("Saved destination "+name+".");}
 async Task RunTravelUiChecks(Dictionary<string,object> checks){
  Navigate("travel");await LayoutReady();checks["mapLandmarks"]=landmarks.Count==69299;checks["allMarkerCategories"]=mapCategories.Count==354;checks["referenceMainCategories"]=mapCategories.Count(c=>MapFilters.IsPrimary(c.Kind))==49;checks["referenceCategoryGroups"]=MapFilters.MainGroups.Count==7;checks["mapLayers"]=landmarks.Select(p=>p.Layer).Distinct().Count()==3;
  var oldFilters=enabledMapCategories.ToHashSet();enabledMapCategories.UnionWith(mapCategories.Select(c=>c.Kind));ApplyMapFilters(false);
  var known=landmarks.First(p=>p.Kind=="Tower");PickMapDestination(known,false);checks["landmarkAltitude"]=ReadTravelCoordinates(out var x,out var y,out var z)&&Math.Abs(y-known.Y)<.01&&Math.Abs(x-known.X)<.01&&Math.Abs(z-known.Z)<.01;
  var point=TravelMap.Project(-452,264);var back=TravelMap.Unproject(point);checks["mapCoordinateRoundTrip"]=Math.Abs(back.X+452)<.001&&Math.Abs(back.Z-264)<.001;
  TravelMap.ZoomBy(2,new System.Windows.Point(120,160));var zoomBack=TravelMap.Unproject(TravelMap.Project(321,-876));checks["mapZoomCoordinateRoundTrip"]=Math.Abs(zoomBack.X-321)<.001&&Math.Abs(zoomBack.Z+876)<.001;
  TravelY.Text="NaN";checks["mapRejectsInvalidAltitude"]=!ReadTravelCoordinates(out _,out _,out _)&&!CoordinateTravelButton.IsEnabled;
  TravelX.Text="50000.5";TravelY.Text="1200";TravelZ.Text="-45000";checks["arbitraryCoordinatesAccepted"]=ReadTravelCoordinates(out _,out _,out _);
  MapSearch.Text="Lookout Landing";mapSearchTimer.Stop();RefreshTravelMap();CoordinateSpace.SelectedIndex=1;checks["hudConversionPreservesDestination"]=ReadTravelCoordinates(out var hx,out var hy,out var hz)&&Math.Abs(hx-50000.5)<.01&&Math.Abs(hy-1200)<.01&&Math.Abs(hz+45000)<.01;CoordinateSpace.SelectedIndex=0;checks["landmarkSearch"]=LandmarkList.Items.Cast<MapLocation>().Any(p=>p.Name.Contains("Lookout Landing"));MapSearch.Clear();mapSearchTimer.Stop();
  enabledMapCategories.Clear();enabledMapCategories.UnionWith(new[]{"Korok Seed","Well","Hinox"});ApplyMapFilters(false);checks["mapMultipleCategories"]=TravelMap.Locations.Count>900&&TravelMap.Locations.All(p=>enabledMapCategories.Contains(p.Kind));
  enabledMapCategories.Clear();ApplyMapFilters(false);checks["mapHideAll"]=TravelMap.Locations.Count==0&&LandmarkList.Items.Count==0;
  enabledMapCategories.UnionWith(mapCategories.Select(c=>c.Kind));ApplyMapFilters(false);
  PickMapDestination(new("Unmeasured","Point","Surface",100,float.NaN,200),false);checks["mapClearsStaleAltitude"]=TravelY.Text==""&&!ReadTravelCoordinates(out _,out _,out _);CoordinateSpace.SelectedIndex=1;checks["mapFormatSwitchWithoutAltitude"]=TravelY.Text==""&&TravelZ.Text=="-200";CoordinateSpace.SelectedIndex=0;checks["mapFormatSwitchBackWithoutAltitude"]=TravelY.Text==""&&TravelZ.Text=="200";
  checks["mapLayerFilters"]=LandmarkList.Items.Cast<MapLocation>().All(p=>p.Layer==TravelMap.Layer);
  MapFilterDrawer.Visibility=Visibility.Visible;MapCategorySearch.Text="Korok";await LayoutReady();checks["mapFilterSearch"]=mapCategoryControls["Korok Seed"].IsVisible&&!mapCategoryControls["Tower"].IsVisible;SaveVisual("travel-filters-search");MapCategorySearch.Clear();checks["compendiumHiddenFromMainFilters"]=mapCategoryControls["Resource: Apple"].Visibility==Visibility.Collapsed;MapModeClick(new Button{Tag="compendium"},new());MapCategorySearch.Text="Apple";await LayoutReady();checks["separateCompendiumSearch"]=mapCategoryControls["Resource: Apple"].IsVisible&&!mapCategoryControls["Tower"].IsVisible;SaveVisual("travel-compendium");MapModeClick(new Button{Tag="markers"},new());MapFilterDrawer.Visibility=Visibility.Collapsed;
  var clock=System.Diagnostics.Stopwatch.StartNew();TravelMap.InvalidateVisual();await LayoutReady();clock.Stop();checks["mapFilterLayoutMilliseconds"]=clock.ElapsedMilliseconds;checks["mapMarkerBuildMilliseconds"]=TravelMap.LastMarkerBuildMilliseconds;checks["mapClusteringBoundsRenderWork"]=TravelMap.RenderedMarkerCount<1500;
  foreach(var layer in new[]{"Surface","Sky","Depths"}){SetMapLayer(layer);TravelMap.SetView(0,0,1.6);await LayoutReady();SaveVisual("travel-map-"+layer.ToLowerInvariant());}
  SetMapLayer("Surface");PickMapDestination(known,false);TravelMap.SetView(0,0,1.6);enabledMapCategories.RemoveWhere(k=>!MapFilters.IsPrimary(k)&&k!="Saved");ApplyMapFilters(false);MapFilterDrawer.Visibility=Visibility.Visible;await LayoutReady();SaveVisual("travel-filters");checks["unclutteredOverview"]=TravelMap.RenderedMarkerCount<100;MapFilterDrawer.Visibility=Visibility.Collapsed;enabledMapCategories.Clear();enabledMapCategories.UnionWith(oldFilters);ApplyMapFilters(false);
 }
 async Task RunTravelLiveCheck(){
  var checks=new Dictionary<string,object>();PlayerPose? before=null;
  try{if(memory==null||!advancedCompatible)throw new InvalidOperationException("Connect to the supported running game first.");before=PlayerLocator.Read(memory);
   var target=landmarks.Where(p=>p.Kind=="Tower").Select(p=>(Place:p,Distance:Math.Sqrt(Math.Pow(p.X-before.X,2)+Math.Pow(p.Z-before.Z,2)))).Where(p=>p.Distance>2200).OrderBy(p=>p.Distance).First();
   checks["distanceMeters"]=target.Distance;checks["destination"]=target.Place;FooterStatus.Text="Checking long-distance teleport, then returning Link…";
   await TravelTo(before with{X=target.Place.X,Y=target.Place.Y+3,Z=target.Place.Z},false);var after=PlayerLocator.Read(memory);checks["longDistanceReadback"]=Math.Abs(after.X-target.Place.X)<1&&Math.Abs(after.Z-target.Place.Z)<1;checks["after"]=after;checks["notice"]=Notice.Text;
  }catch(Exception ex){checks["error"]=ex.Message;}
  finally{if(before!=null&&memory?.Alive==true){await TravelTo(before,true);try{var restored=PlayerLocator.Read(memory);checks["returnedToOriginalPosition"]=Math.Abs(restored.X-before.X)<1&&Math.Abs(restored.Y-before.Y)<5&&Math.Abs(restored.Z-before.Z)<1;}catch(Exception ex){checks["returnError"]=ex.Message;}}System.IO.File.WriteAllText(System.IO.Path.Combine(AppContext.BaseDirectory,"map-travel-check.json"),JsonSerializer.Serialize(checks,new JsonSerializerOptions{WriteIndented=true}));}
 }
}
