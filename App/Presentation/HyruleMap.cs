using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace HyruleCompanion;
// Native, retained map rendering: bitmap basemap, vector pins and no browser/runtime dependency.
public sealed class HyruleMap:FrameworkElement {
 readonly Dictionary<string,BitmapSource> maps=[];
 readonly Typeface font=new("Segoe UI");
 static readonly Brush Cyan=new SolidColorBrush(Color.FromRgb(72,214,249)),Gold=new SolidColorBrush(Color.FromRgb(233,196,119));
 public IReadOnlyList<MapLocation> Locations{get;set;}=[];
 public MapLocation? Selected{get;set;}
 public PlayerPose? Player{get;set;}
 public string Layer{get;private set;}="Surface";
 public double Zoom{get;private set;}=1.6;
 public double CenterX{get;private set;}
 public double CenterZ{get;private set;}
 public event Action<MapLocation,bool>? DestinationPicked;
 public event Action<double,double>? PointerMoved;
 Point dragStart,previous;bool dragging,moved;int clickCount;
 double Scale=>Math.Max(.00001,Math.Min(ActualWidth/12000,ActualHeight/10000)*Zoom);
 public HyruleMap(){Focusable=true;ClipToBounds=true;Cursor=Cursors.Cross;MouseDown+=Down;MouseMove+=Move;MouseUp+=Up;MouseWheel+=Wheel;KeyDown+=MapKey;LostMouseCapture+=(_,_)=>dragging=false;}
 public Point Project(double x,double z)=>new((x-CenterX)*Scale+ActualWidth/2,(z-CenterZ)*Scale+ActualHeight/2);
 public (double X,double Z) Unproject(Point p)=>((p.X-ActualWidth/2)/Scale+CenterX,(p.Y-ActualHeight/2)/Scale+CenterZ);
 public void SetLayer(string layer){if(layer is not ("Surface" or "Sky" or "Depths"))return;Layer=layer;InvalidateVisual();}
 public void SetView(double x,double z,double? zoom=null){CenterX=Math.Clamp(x,-6000,6000);CenterZ=Math.Clamp(z,-5000,5000);if(zoom.HasValue)Zoom=Math.Clamp(zoom.Value,1,24);InvalidateVisual();}
 public void ZoomBy(double factor,Point? anchor=null){var p=anchor??new Point(ActualWidth/2,ActualHeight/2);var before=Unproject(p);Zoom=Math.Clamp(Zoom*factor,1,24);var after=Unproject(p);CenterX=Math.Clamp(CenterX+before.X-after.X,-6000,6000);CenterZ=Math.Clamp(CenterZ+before.Z-after.Z,-5000,5000);InvalidateVisual();}
 void Down(object sender,MouseButtonEventArgs e){if(e.ChangedButton!=MouseButton.Left)return;Focus();dragStart=previous=e.GetPosition(this);dragging=true;moved=false;clickCount=e.ClickCount;CaptureMouse();e.Handled=true;}
 void Move(object sender,MouseEventArgs e){var p=e.GetPosition(this);var world=Unproject(p);PointerMoved?.Invoke(world.X,world.Z);if(dragging&&e.LeftButton==MouseButtonState.Pressed){if((p-dragStart).Length>5)moved=true;if(moved){CenterX=Math.Clamp(CenterX-(p.X-previous.X)/Scale,-6000,6000);CenterZ=Math.Clamp(CenterZ-(p.Y-previous.Y)/Scale,-5000,5000);InvalidateVisual();}previous=p;}else{var hit=Hit(p);ToolTip=hit==null?"Click to select • double-click to teleport • drag to pan • wheel to zoom":$"{hit.Name} • {hit.Kind}\nX {hit.X:0.0}  Y {hit.Y:0.0}  Z {hit.Z:0.0}";}}
 void Up(object sender,MouseButtonEventArgs e){if(!dragging||e.ChangedButton!=MouseButton.Left)return;var p=e.GetPosition(this);var pick=!moved;dragging=false;ReleaseMouseCapture();if(pick){var w=Unproject(p);if(w.X < -6000||w.X>6000||w.Z < -5000||w.Z>5000)return;var hit=Hit(p)??new MapLocation("Map destination","Point",Layer,(float)w.X,float.NaN,(float)w.Z);Selected=hit;InvalidateVisual();DestinationPicked?.Invoke(hit,clickCount>=2);}e.Handled=true;}
 MapLocation? Hit(Point p)=>Locations.Where(x=>x.Layer==Layer).Select(x=>(Pin:x,Distance:(Project(x.X,x.Z)-p).Length)).Where(x=>x.Distance<=12).OrderBy(x=>x.Distance).Select(x=>x.Pin).FirstOrDefault();
 void MapKey(object sender,KeyEventArgs e){var step=400/Zoom;switch(e.Key){case Key.Left:SetView(CenterX-step,CenterZ);break;case Key.Right:SetView(CenterX+step,CenterZ);break;case Key.Up:SetView(CenterX,CenterZ-step);break;case Key.Down:SetView(CenterX,CenterZ+step);break;case Key.Add:case Key.OemPlus:ZoomBy(1.3);break;case Key.Subtract:case Key.OemMinus:ZoomBy(1/1.3);break;case Key.Enter:if(Selected!=null)DestinationPicked?.Invoke(Selected,true);break;default:return;}e.Handled=true;}
 void Wheel(object sender,MouseWheelEventArgs e){ZoomBy(e.Delta>0?1.3:1/1.3,e.GetPosition(this));e.Handled=true;}
 BitmapSource Basemap(){if(maps.TryGetValue(Layer,out var cached))return cached;var b=new BitmapImage();b.BeginInit();b.UriSource=new Uri($"pack://application:,,,/HyruleCompanion;component/Resources/Maps/{Layer.ToLowerInvariant()}.jpg");b.DecodePixelWidth=4096;b.CacheOption=BitmapCacheOption.OnLoad;b.EndInit();b.Freeze();maps[Layer]=b;return b;}
 void Label(DrawingContext d,string text,Point p,Brush color,double size=11){var f=new FormattedText(text,CultureInfo.InvariantCulture,FlowDirection.LeftToRight,font,size,color,VisualTreeHelper.GetDpi(this).PixelsPerDip);d.DrawText(f,p);}
 protected override void OnRender(DrawingContext d){base.OnRender(d);d.DrawRectangle(new SolidColorBrush(Color.FromRgb(11,24,25)),null,new Rect(RenderSize));if(ActualWidth<1||ActualHeight<1)return;d.PushClip(new RectangleGeometry(new Rect(RenderSize)));var tl=Project(-6000,-5000);var br=Project(6000,5000);d.DrawImage(Basemap(),new Rect(tl,br));d.DrawRectangle(new SolidColorBrush(Color.FromArgb(22,3,15,20)),null,new Rect(RenderSize));
  if(Zoom>=2.5){var grid=new Pen(new SolidColorBrush(Color.FromArgb(35,230,210,150)),.6);for(var x=-6000;x<=6000;x+=1000)d.DrawLine(grid,Project(x,-5000),Project(x,5000));for(var z=-5000;z<=5000;z+=1000)d.DrawLine(grid,Project(-6000,z),Project(6000,z));}
  foreach(var pin in Locations.Where(x=>x.Layer==Layer)){var pt=Project(pin.X,pin.Z);if(pt.X < -20||pt.X>ActualWidth+20||pt.Y < -20||pt.Y>ActualHeight+20)continue;var color=pin.Kind=="Saved"?Gold:Cyan;var radius=pin.Kind=="Tower"?6:4;d.DrawEllipse(new SolidColorBrush(Color.FromArgb(210,3,23,30)),new Pen(color,1.4),pt,radius,radius);if(pin.Kind=="Tower")d.DrawLine(new Pen(color,1.5),new(pt.X,pt.Y-10),new(pt.X,pt.Y+10));if(Zoom>=5||pin==Selected)Label(d,pin.Name,new(pt.X+10,pt.Y-9),Brushes.White,11);}
  if(Player!=null){var p=Project(Player.X,Player.Z);d.DrawEllipse(Brushes.Black,new Pen(Brushes.White,2),p,7,7);d.DrawEllipse(Brushes.White,null,p,3,3);}
  if(Selected is {} target&&target.Layer==Layer){var p=Project(target.X,target.Z);d.DrawEllipse(null,new Pen(Gold,2),p,12,12);d.DrawLine(new Pen(Gold,1),new(p.X-20,p.Y),new(p.X+20,p.Y));d.DrawLine(new Pen(Gold,1),new(p.X,p.Y-20),new(p.X,p.Y+20));}
  d.Pop();Label(d,"N",new(ActualWidth-30,12),Gold,15);Label(d,$"{Layer.ToUpperInvariant()}  •  {Zoom:0.0}×",new(13,12),Brushes.White,11);var meters=1000*Scale;while(meters>ActualWidth/3)meters/=2;d.DrawLine(new Pen(Gold,2),new(15,ActualHeight-24),new(15+meters,ActualHeight-24));Label(d,$"{meters/Scale:0} m",new(15,ActualHeight-20),Gold,10);
 }
}
