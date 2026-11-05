using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace HyruleCompanion;

public static class Chrome {
 public static readonly DependencyProperty SelectedProperty=DependencyProperty.RegisterAttached("Selected",typeof(bool),typeof(Chrome),new FrameworkPropertyMetadata(false));
 public static bool GetSelected(DependencyObject o)=>(bool)o.GetValue(SelectedProperty);
 public static void SetSelected(DependencyObject o,bool v)=>o.SetValue(SelectedProperty,v);
 public static readonly DependencyProperty IconProperty=DependencyProperty.RegisterAttached("Icon",typeof(string),typeof(Chrome),new FrameworkPropertyMetadata(""));
 public static string GetIcon(DependencyObject o)=>(string)o.GetValue(IconProperty);
 public static void SetIcon(DependencyObject o,string v)=>o.SetValue(IconProperty,v);
 public static readonly DependencyProperty HoverLiftProperty=DependencyProperty.RegisterAttached("HoverLift",typeof(bool),typeof(Chrome),new PropertyMetadata(false,HoverChanged));
 public static bool GetHoverLift(DependencyObject o)=>(bool)o.GetValue(HoverLiftProperty);
 public static void SetHoverLift(DependencyObject o,bool v)=>o.SetValue(HoverLiftProperty,v);
 public static bool ReduceMotion{get;set;}
 public static bool MotionEnabled=>!ReduceMotion&&SystemParameters.ClientAreaAnimation;
 static void HoverChanged(DependencyObject o,DependencyPropertyChangedEventArgs e){if(o is not FrameworkElement f)return;if((bool)e.NewValue){f.MouseEnter+=Enter;f.MouseLeave+=Leave;}else{f.MouseEnter-=Enter;f.MouseLeave-=Leave;}}
 static void Enter(object sender,MouseEventArgs e)=>Lift((FrameworkElement)sender,-2);
 static void Leave(object sender,MouseEventArgs e)=>Lift((FrameworkElement)sender,0);
 static void Lift(FrameworkElement f,double y){if(f.RenderTransform is not TranslateTransform t){t=new();f.RenderTransform=t;}if(!MotionEnabled){t.BeginAnimation(TranslateTransform.YProperty,null);t.Y=0;return;}t.BeginAnimation(TranslateTransform.YProperty,new DoubleAnimation(y,TimeSpan.FromMilliseconds(140)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut}});}
 public static void Reveal(UIElement page){page.BeginAnimation(UIElement.OpacityProperty,null);if(MotionEnabled)page.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(.6,1,TimeSpan.FromMilliseconds(160)));else page.Opacity=1;}
}

// A retained vector frame: no bitmap borders and no per-frame redraw loop.
public sealed class OrnateFrame:Border {
 public static readonly DependencyProperty CornerSizeProperty=DependencyProperty.Register(nameof(CornerSize),typeof(double),typeof(OrnateFrame),new FrameworkPropertyMetadata(12d,FrameworkPropertyMetadataOptions.AffectsRender));
 public double CornerSize{get=>(double)GetValue(CornerSizeProperty);set=>SetValue(CornerSizeProperty,value);}
 public static readonly DependencyProperty OrnamentProperty=DependencyProperty.Register(nameof(Ornament),typeof(bool),typeof(OrnateFrame),new FrameworkPropertyMetadata(true,FrameworkPropertyMetadataOptions.AffectsRender));
 public bool Ornament{get=>(bool)GetValue(OrnamentProperty);set=>SetValue(OrnamentProperty,value);}
 static readonly Geometry Corner=Geometry.Parse("M3,67 V18 L18,3 H67 M10,48 V22 L22,10 H48 M3,18 H18 V3 M10,25 H25 V10 M25,24 L31,31 L25,38 L19,31 Z");
 static OrnateFrame(){Corner.Freeze();}
 static StreamGeometry Outline(Rect r,double cut){var g=new StreamGeometry();using(var c=g.Open()){c.BeginFigure(new(r.Left+cut,r.Top),true,true);c.PolyLineTo([new(r.Right-cut,r.Top),new(r.Right,r.Top+cut),new(r.Right,r.Bottom-cut),new(r.Right-cut,r.Bottom),new(r.Left+cut,r.Bottom),new(r.Left,r.Bottom-cut),new(r.Left,r.Top+cut)],true,false);}g.Freeze();return g;}
 protected override void OnRender(DrawingContext dc){var w=ActualWidth;var h=ActualHeight;if(w<3||h<3)return;var cut=Math.Min(CornerSize,Math.Min(w,h)/3);var stroke=BorderBrush??Brushes.Transparent;var thickness=BorderThickness.Left;var outer=Outline(new(.6,.6,w-1.2,h-1.2),cut);dc.DrawGeometry(Background,thickness>0?new Pen(stroke,thickness):null,outer);if(!Ornament||thickness<=0)return;dc.PushOpacity(.28);dc.DrawGeometry(null,new Pen(stroke,.7),Outline(new(3.5,3.5,Math.Max(0,w-7),Math.Max(0,h-7)),Math.Max(3,cut-2)));dc.Pop();var scale=Math.Min(.36,Math.Max(.21,cut/48));var s=70*scale;for(var i=0;i<4;i++){var matrix=i switch{0=>new Matrix(scale,0,0,scale,0,0),1=>new Matrix(-scale,0,0,scale,w,0),2=>new Matrix(scale,0,0,-scale,0,h),_=>new Matrix(-scale,0,0,-scale,w,h)};dc.PushTransform(new MatrixTransform(matrix));dc.DrawGeometry(null,new Pen(stroke,2.1),Corner);dc.Pop();}if(w>160){dc.PushOpacity(.62);var mid=w/2;dc.DrawGeometry(null,new Pen(stroke,.8),Geometry.Parse(FormattableString.Invariant($"M{mid-14},{h-1} L{mid-7},{h-5} L{mid},{h-1} L{mid+7},{h-5} L{mid+14},{h-1}")));dc.Pop();}}
}

public sealed class SheikahSeal:FrameworkElement {
 static readonly DrawingGroup Drawing=Build();
 static DrawingGroup Build(){var g=new DrawingGroup();var b=new SolidColorBrush(Color.FromRgb(72,214,249));b.Freeze();using(var d=g.Open()){var p=new Pen(b,1.1);d.DrawEllipse(null,p,new(100,100),87,87);d.DrawEllipse(null,p,new(100,100),73,73);d.DrawEllipse(null,new Pen(b,.7){DashStyle=DashStyles.Dash},new(100,100),67,67);for(var i=0;i<32;i++){var a=i*Math.PI/16;double r=i%4==0?94:89;d.DrawLine(p,new(100+82*Math.Cos(a),100+82*Math.Sin(a)),new(100+r*Math.Cos(a),100+r*Math.Sin(a)));}d.PushTransform(new TranslateTransform(40,57));d.DrawGeometry(null,new Pen(b,2.5){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round},Geometry.Parse("M8,43 Q58,-21 112,43 Q60,91 8,43 M60,25 V5 M44,23 L31,9 M73,23 L88,9 M60,59 V83 M43,63 L35,78 M77,63 L85,78"));d.DrawEllipse(null,new Pen(b,2.5),new(60,43),16,16);d.DrawEllipse(b,null,new(60,43),5,5);d.Pop();}g.Freeze();return g;}
 protected override void OnRender(DrawingContext dc){if(ActualWidth<=0||ActualHeight<=0)return;dc.PushTransform(new ScaleTransform(ActualWidth/200,ActualHeight/200));dc.DrawDrawing(Drawing);dc.Pop();}
}

public static class Artwork {
 static readonly Dictionary<(string,int),BitmapSource> Cache=[];static readonly Queue<(string,int)> Order=[];
 public static BitmapSource Load(string path,int width){var key=(path,width);if(Cache.TryGetValue(key,out var cached))return cached;var image=new BitmapImage();image.BeginInit();image.UriSource=new Uri(path,UriKind.Absolute);image.CacheOption=BitmapCacheOption.OnLoad;image.DecodePixelWidth=width;image.EndInit();image.Freeze();Cache[key]=image;Order.Enqueue(key);while(Order.Count>256)Cache.Remove(Order.Dequeue());return image;}
}
public sealed class ThumbnailConverter:IValueConverter {public object Convert(object value,Type t,object parameter,CultureInfo culture)=>Artwork.Load((string)value,160);public object ConvertBack(object v,Type t,object p,CultureInfo c)=>throw new NotSupportedException();}
public sealed class NonEmptyVisibilityConverter:IValueConverter {public object Convert(object value,Type t,object parameter,CultureInfo culture)=>string.IsNullOrWhiteSpace(value?.ToString())?Visibility.Collapsed:Visibility.Visible;public object ConvertBack(object v,Type t,object p,CultureInfo c)=>throw new NotSupportedException();}
public sealed class CheatArtworkConverter:IValueConverter {public object Convert(object value,Type t,object p,CultureInfo c)=>Application.Current.FindResource("Cheat."+value);public object ConvertBack(object v,Type t,object p,CultureInfo c)=>throw new NotSupportedException();}
public sealed class CheatNoteBrushConverter:IValueConverter {public object Convert(object value,Type t,object p,CultureInfo c){var s=value?.ToString()??"";return Application.Current.FindResource(s.StartsWith("Ready")||s.StartsWith("Live")?"SuccessBrush":s.StartsWith("Not supported")||s.StartsWith("Experimental")||s.StartsWith("Stopped")?"WarningBrush":"MutedBrush");}public object ConvertBack(object v,Type t,object p,CultureInfo c)=>throw new NotSupportedException();}
