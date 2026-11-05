using System.Globalization;
namespace HyruleCompanion;
public record MapLocation(string Name,string Kind,string Layer,float X,float Y,float Z);
public static class TravelCoordinates {
 public static (float X,float Y,float Z) FromHud(float east,float north,float height)=>(east,height+105,-north);
 public static (float East,float North,float Height) ToHud(float x,float y,float z)=>(x,-z,y-105);
 public const double MapWidth=12000,MapHeight=10000;
 public static (double X,double Z) FromNormalized(double u,double v)=>(u*MapWidth-6000,v*MapHeight-5000);
 public static (double U,double V) ToNormalized(double x,double z)=>((x+6000)/MapWidth,(z+5000)/MapHeight);
 public static bool TryParse(string x,string height,string z,out float east,out float altitude,out float south){east=altitude=south=0;return Number(x,out east)&&Number(height,out altitude)&&Number(z,out south);}
 static bool Number(string s,out float value)=>(float.TryParse(s,NumberStyles.Float,CultureInfo.InvariantCulture,out value)||float.TryParse(s,NumberStyles.Float,CultureInfo.CurrentCulture,out value))&&float.IsFinite(value)&&Math.Abs(value)<=100000;
}
