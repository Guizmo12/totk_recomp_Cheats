using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
namespace HyruleCompanion;
public partial class MainWindow {
 void ReduceMotionClick(object sender,RoutedEventArgs e){Chrome.ReduceMotion=settings.ReduceMotion=ReduceMotionCheck.IsChecked==true;SaveSettings();}
 void WindowSizeChanged(object sender,SizeChangedEventArgs e){if(SidebarColumn==null)return;SidebarColumn.Width=new(ActualWidth<1280?202:224);DetailsColumn.Width=new(ActualWidth<1280?270:294);HeroRow.Height=new(ActualHeight<780?112:ActualHeight<820?126:146);SidebarHeaderRow.Height=new(ActualHeight<820?150:194);foreach(var b in new[]{NavItems,NavCheats,NavTravel,NavSettings})b.Padding=ActualHeight<820?new Thickness(14,11,14,11):new Thickness(18,14,18,14);ResizeCards();}
 void ItemRowMouseWheel(object sender,System.Windows.Input.MouseWheelEventArgs e){var scroll=Descendants<ScrollViewer>(ItemList).FirstOrDefault();if(scroll==null)return;scroll.ScrollToVerticalOffset(scroll.VerticalOffset-e.Delta);e.Handled=true;}
 void RenderItemRows(){ItemList.ItemsSource=filtered.Chunk(itemColumns).Select(row=>row.ToList()).ToList();if(filtered.Count>0)ItemList.ScrollIntoView(ItemList.Items[0]);}
 void ItemViewportChanged(object sender,SizeChangedEventArgs e)=>ResizeCards();
 void ResizeCards(){if(ItemList==null||ItemList.ActualWidth<100)return;var available=ItemList.ActualWidth-42;var columns=available>=680?4:available>=470?3:2;var width=Math.Max(130,Math.Floor(available/columns)-11);if(!Equals(Resources["ItemCardWidth"],width))Resources["ItemCardWidth"]=width;if(itemColumns!=columns){itemColumns=columns;if(initialized)RenderItemRows();}}
 static IEnumerable<T> Descendants<T>(DependencyObject root) where T:DependencyObject {for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);if(child is T t)yield return t;foreach(var nested in Descendants<T>(child))yield return nested;}}
 async Task LayoutReady(){UpdateLayout();await Dispatcher.InvokeAsync(()=>{},DispatcherPriority.Render);await Task.Delay(100);UpdateLayout();}
 void SaveVisual(string name,double scale=1){var visual=(Visual)Content;var bitmap=new RenderTargetBitmap((int)(ActualWidth*scale),(int)(ActualHeight*scale),96*scale,96*scale,PixelFormats.Pbgra32);bitmap.Render(visual);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var stream=File.Create(Path.Combine(AppContext.BaseDirectory,"visual-review",name+".png"));encoder.Save(stream);}
 async Task RunVisualReview(){
  var dir=Path.Combine(AppContext.BaseDirectory,"visual-review");Directory.CreateDirectory(dir);var checks=new Dictionary<string,object>();var oldRecent=settings.Recent.ToList();var oldFavorites=settings.Favorites.ToHashSet();var oldSelected=selected;var oldWidth=Width;var oldHeight=Height;var motion=Chrome.ReduceMotion;Chrome.ReduceMotion=true;
  try{
   checks["catalogCount"]=catalog.Count;checks["allCheatsInitiallyOff"]=cheats.All(x=>!x.Active);checks["connectionTruth"]=memory==null?ConnectionStatus.Text!="Connected • 1.2.1":memory.Alive;
   SearchBox.Text="hylian shield";searchTimer.Stop();RefreshItems();checks["search"]=filtered.Any(x=>x.Id=="Weapon_Shield_030");SearchBox.Clear();searchTimer.Stop();RefreshItems();
   var favorite=catalog.First();var before=favorite.Favorite;Favorite(favorite);checks["favoritesToggle"]=favorite.Favorite!=before;Favorite(favorite);
   CategoryClick(categoryButtons["Arrows"],new());checks["arrowsFilter"]=filtered.Count>0&&filtered.All(x=>x.Category=="Arrows");checks["fiveArrowBundle"]=filtered.Any(x=>x.UnitsPerPickup==5);ClearFiltersClick(this,new());searchTimer.Stop();
   await LayoutReady();var itemScroll=Descendants<ScrollViewer>(ItemList).First();var firstRow=Descendants<ListBox>(ItemList).First();firstRow.RaiseEvent(new System.Windows.Input.MouseWheelEventArgs(System.Windows.Input.Mouse.PrimaryDevice,Environment.TickCount,-120){RoutedEvent=UIElement.PreviewMouseWheelEvent});await LayoutReady();checks["wheelOverCardsScrollsResults"]=itemScroll.VerticalOffset>0;itemScroll.ScrollToEnd();await LayoutReady();checks["continuousScrollReachesLastItem"]=ItemList.Items.Cast<List<Item>>().SelectMany(x=>x).Count()==filtered.Count&&itemScroll.VerticalOffset>0;checks["rowsVirtualized"]=Descendants<ListBoxItem>(ItemList).Count()<filtered.Count;var lastRow=Descendants<ListBox>(ItemList).Last();lastRow.SelectedIndex=0;checks["selectionAfterScrolling"]=ReferenceEquals(selected,lastRow.SelectedItem);SaveVisual("items-scroll-bottom");itemScroll.ScrollToTop();
   checks["unsupportedItemsExcluded"]=catalog.All(x=>x.SpawnSupported);
   SelectItem(catalog.First(x=>x.Id=="Weapon_Shield_030"));checks["selectedActor"]=DetailId.Text==SpawnCatalog.WorldActor(selected!.Id);checks["spawnTruth"]=SpawnButton.IsEnabled==(memory!=null&&advancedCompatible&&worldActors.Contains(SpawnCatalog.WorldActor(selected.Id)));
   checks["allCategories"]=categoryButtons.Count==8;checks["hotkeyRows"]=HotkeyList.Items.Count==cheats.Count(x=>x.Supported);checks["gamePathPreserved"]=GamePathBox.Text==settings.GamePath;checks["placesPreserved"]=PlaceList.Items.Count==settings.Places.Count;
   foreach(var size in new[]{(1440d,920d),(1600d,900d),(1920d,1080d),(1120d,720d)}){
    Width=size.Item1;Height=size.Item2;
    foreach(var name in new[]{"items","cheats","travel","settings"}){Navigate(name);await LayoutReady();SaveVisual($"{name}-{size.Item1:0}x{size.Item2:0}");if(name=="items"&&size.Item1==1120){var details=DetailScroll;details.ScrollToEnd();await LayoutReady();SaveVisual("items-small-details-bottom");checks["smallSpawnReachable"]=SpawnButton.IsVisible&&details.VerticalOffset>0;details.ScrollToTop();}
     checks[$"{name}-{size.Item1:0}-layout"]=Descendants<ScrollViewer>(this).Where(v=>v.IsVisible).All(v=>v.ScrollableWidth<1);}
   }
   Width=1280;Height=720;foreach(var name in new[]{"items","cheats","travel","settings"}){Navigate(name);await LayoutReady();SaveVisual(name+"-125percent",1.25);}checks["dpiReview"]="125% offscreen rasterization of native WPF tree; Windows DPI setting unchanged";
   Width=1440;Height=920;Navigate("items");SearchBox.Text="no such hyrule item 947";searchTimer.Stop();RefreshItems();await LayoutReady();checks["emptyState"]=EmptyResults.IsVisible;SaveVisual("items-empty");ClearFiltersClick(this,new());searchTimer.Stop();
   foreach(var name in new[]{"cheats","settings"}){Navigate(name);await LayoutReady();var scroller=Descendants<ScrollViewer>(name=="cheats"?CheatsPage:SettingsPage).First();scroller.ScrollToEnd();await LayoutReady();SaveVisual(name+"-bottom");checks[name+"-scrollReachesBottom"]=scroller.VerticalOffset>0&&scroller.ScrollableHeight-scroller.VerticalOffset<1;scroller.ScrollToTop();}
   Navigate("cheats");ActiveOnly.IsChecked=true;ActiveOnlyClick(this,new());checks["activeOnly"]=CheatList.Items.Count==0;ActiveOnly.IsChecked=false;ActiveOnlyClick(this,new());
   ShowNotice("Connect to a supported game before enabling this control.");await LayoutReady();SaveVisual("cheats-validation");NoticeBorder.Visibility=Visibility.Collapsed;
  }catch(Exception ex){checks["error"]=ex.ToString();}
  finally{settings.Recent=oldRecent;settings.Favorites=oldFavorites;foreach(var item in catalog)item.Favorite=oldFavorites.Contains(item.Id);if(oldSelected!=null){SelectItem(oldSelected);settings.Recent=oldRecent;}SaveSettings();Width=oldWidth;Height=oldHeight;Navigate("items");Chrome.ReduceMotion=motion;await LayoutReady();SaveVisual("final");File.WriteAllText(Path.Combine(dir,"checks.json"),JsonSerializer.Serialize(checks,new JsonSerializerOptions{WriteIndented=true}));}
 }
}
