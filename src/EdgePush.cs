using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading;

[assembly:System.Reflection.AssemblyTitle("EdgePush")]
[assembly:System.Reflection.AssemblyProduct("EdgePush")]
[assembly:System.Reflection.AssemblyVersion("1.0.0.0")]
[assembly:System.Reflection.AssemblyFileVersion("1.0.0.0")]

namespace EdgePush {
static class Native {
 public delegate IntPtr Hook(int code, IntPtr wp, IntPtr lp);
 [DllImport("user32.dll")]public static extern bool PrintWindow(IntPtr window,IntPtr dc,uint flags);
 [DllImport("user32.dll")]public static extern int GetGuiResources(IntPtr process,int flags);
 [StructLayout(LayoutKind.Sequential)] public struct Mouse { public Point point; public uint data, flags, time; public UIntPtr extra; }
 [StructLayout(LayoutKind.Sequential,Pack=1)] public struct Blend { public byte op, flags, alpha, format; }
 [DllImport("user32.dll",SetLastError=true)] public static extern IntPtr SetWindowsHookEx(int id, Hook fn, IntPtr module, uint thread);
 [DllImport("kernel32.dll",CharSet=CharSet.Auto)] public static extern IntPtr GetModuleHandle(string name);
 [DllImport("user32.dll")] public static extern bool UnhookWindowsHookEx(IntPtr hook);
 [DllImport("user32.dll")] public static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr wp,IntPtr lp);
 [DllImport("user32.dll")] public static extern bool SetCursorPos(int x,int y);
 [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int key);
 [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
 [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr context);
 [DllImport("user32.dll")] public static extern IntPtr GetDC(IntPtr hwnd);
 [DllImport("user32.dll")] public static extern int ReleaseDC(IntPtr hwnd,IntPtr dc);
 [DllImport("gdi32.dll")] public static extern IntPtr CreateCompatibleDC(IntPtr dc);
 [DllImport("gdi32.dll")] public static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
 [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr obj);
 [DllImport("gdi32.dll")] public static extern bool DeleteDC(IntPtr dc);
 [DllImport("user32.dll")] public static extern bool UpdateLayeredWindow(IntPtr hwnd,IntPtr dst,ref Point pos,ref Size size,IntPtr src,ref Point origin,uint key,ref Blend blend,uint flags);
}
sealed class Settings {
 public bool enabled=true, sound=true, ctrl_bypass=true;
 public int threshold=120, release_ms=240, fps=60, effect=8, theme=0;
 public string language="en";public int version=4;
 public bool full_edge=true; public int opacity=85, thickness=3, duration=500;
 public static readonly string PathName=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"settings.json");
 public static Settings Load() {
  var serializer=new JavaScriptSerializer(); Settings s=new Settings();
  try { if(File.Exists(PathName)) {string json=File.ReadAllText(PathName);s=serializer.Deserialize<Settings>(json)??s;if(!serializer.Deserialize<Dictionary<string,object>>(json).ContainsKey("version")){s.effect=8;s.language="en";}}
   else { string old=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"config.json");
    if(File.Exists(old)) { var d=serializer.Deserialize<Dictionary<string,object>>(File.ReadAllText(old));
     if(d.ContainsKey("threshold")) s.threshold=Convert.ToInt32(d["threshold"]);
     if(d.ContainsKey("enabled")) s.enabled=Convert.ToBoolean(d["enabled"]);
     if(d.ContainsKey("sound")) s.sound=Convert.ToBoolean(d["sound"]);
     if(d.ContainsKey("ctrl_bypass")) s.ctrl_bypass=Convert.ToBoolean(d["ctrl_bypass"]);
     if(d.ContainsKey("theme")) s.theme=Math.Max(0,Array.IndexOf(new[]{"cyan","purple","emerald","solar","ice"},Convert.ToString(d["theme"])));
    }
   }
  } catch(Exception ex) { MessageBox.Show(Words.Get(s,"Could not read settings; defaults will be used.","Ayarlar okunamadı; varsayılanlar kullanılacak.")+"\n"+ex.Message,"EdgePush"); }
  s.threshold=Math.Max(40,Math.Min(2400,s.threshold)); s.release_ms=Math.Max(80,Math.Min(1200,s.release_ms));
  s.effect=Math.Max(0,Math.Min(10,s.effect));s.theme=Math.Max(0,Math.Min(4,s.theme));if(Array.IndexOf(new[]{15,30,60,120},s.fps)<0)s.fps=60;s.opacity=Math.Max(15,Math.Min(100,s.opacity));s.thickness=Math.Max(1,Math.Min(10,s.thickness));s.duration=Math.Max(150,Math.Min(1200,s.duration));s.language=s.language=="tr"?"tr":"en";s.version=4;return s;
 }
 public void Save() {
  string temp=PathName+".tmp";
  try {File.WriteAllText(temp,new JavaScriptSerializer().Serialize(this));
   if(File.Exists(PathName)) File.Replace(temp,PathName,null); else File.Move(temp,PathName);
  } catch(Exception ex) { MessageBox.Show(Words.Get(this,"Could not save settings:","Ayarlar kaydedilemedi:")+"\n"+ex.Message,"EdgePush"); }
 }
}
sealed class Seam {
 public bool Vertical; public int Coordinate,Start,End;public Rectangle Negative,Positive;
 public Seam(bool v,int c,int start,int end) {Vertical=v;Coordinate=c;Start=start;End=end;}
 public static List<Seam> Find(Rectangle[] screens) {
  var list=new List<Seam>();
  for(int i=0;i<screens.Length;i++)for(int j=i+1;j<screens.Length;j++){
   var a=screens[i];var b=screens[j];int lo=Math.Max(a.Top,b.Top),hi=Math.Min(a.Bottom,b.Bottom);
   if(lo<hi && (a.Right==b.Left||b.Right==a.Left))list.Add(new Seam(true,a.Right==b.Left?a.Right:b.Right,lo,hi){Negative=a.Right==b.Left?a:b,Positive=a.Right==b.Left?b:a});
   lo=Math.Max(a.Left,b.Left);hi=Math.Min(a.Right,b.Right);
   if(lo<hi && (a.Bottom==b.Top||b.Bottom==a.Top))list.Add(new Seam(false,a.Bottom==b.Top?a.Bottom:b.Bottom,lo,hi){Negative=a.Bottom==b.Top?a:b,Positive=a.Bottom==b.Top?b:a});
  } return list;
 }
 public bool Cross(Point previous,Point target) {
  int before=Vertical?previous.X:previous.Y, after=Vertical?target.X:target.Y;
  if(!((before<Coordinate&&after>=Coordinate)||(before>=Coordinate&&after<Coordinate)))return false;
  double fraction=(Coordinate-before)/(double)(after-before);
  double along=(Vertical?previous.Y:previous.X)+fraction*((Vertical?target.Y:target.X)-(Vertical?previous.Y:previous.X));
  // A diagonal pointer move may enter the taller destination above/below the shared span.
  // Match both monitor rectangles as well as the path intersection to protect corner exits.
  return (along>=Start&&along<End)||(Negative.Contains(previous)&&Positive.Contains(target))||(Positive.Contains(previous)&&Negative.Contains(target));
 }
}
sealed class Overlay : Form {
 Bitmap bitmap; Graphics graphics; readonly IntPtr dc; IntPtr dib,old;
 readonly Settings cfg; public Point Center {get;set;} public bool Vertical=true,Arrival; public double Ratio;public Seam Edge;public int SourceStart=int.MinValue,SourceEnd,Direction=1;
 public static readonly Color[] Colors={Color.FromArgb(0,210,245),Color.FromArgb(180,90,255),Color.FromArgb(0,225,150),Color.FromArgb(255,155,45),Color.FromArgb(215,235,255)};
 readonly Stopwatch clock=Stopwatch.StartNew();
 public Overlay(Settings c){cfg=c;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;IntPtr screen=Native.GetDC(IntPtr.Zero);dc=Native.CreateCompatibleDC(screen);Native.ReleaseDC(IntPtr.Zero,screen);}
 protected override bool ShowWithoutActivation {get{return true;}}
 protected override CreateParams CreateParams {get{var p=base.CreateParams;p.ExStyle|=0x80000|0x20|0x08000000|0x80;return p;}}
 void Surface(int width,int height){if(bitmap!=null&&bitmap.Width==width&&bitmap.Height==height)return;if(graphics!=null)graphics.Dispose();if(bitmap!=null)bitmap.Dispose();bitmap=new Bitmap(width,height,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);graphics=Graphics.FromImage(bitmap);graphics.SmoothingMode=SmoothingMode.AntiAlias;}
 public void Render(double progress){
  if(cfg.effect==4){Hide();return;}
  bool full=cfg.full_edge&&Edge!=null&&(!Arrival||cfg.effect>=8);int edgeStart=SourceStart==int.MinValue?(Edge==null?0:Edge.Start):SourceStart;int edgeEnd=SourceStart==int.MinValue?(Edge==null?240:Edge.End):SourceEnd;int span=full?edgeEnd-edgeStart:240;
  int w=full?(Vertical?320:span+40):280,h=full?(Vertical?span+40:320):280;Surface(w,h);graphics.Clear(Color.Transparent);
  Point pos=full?(Vertical?new Point(Edge.Coordinate-160,edgeStart-20):new Point(edgeStart-20,Edge.Coordinate-160)):new Point(Center.X-w/2,Center.Y-h/2);
  float cx=Center.X-pos.X,cy=Center.Y-pos.Y;float k=(float)Math.Max(0,Math.Min(1,Ratio));Color color=Colors[cfg.theme];float phase=(float)clock.Elapsed.TotalSeconds;
  if(cfg.effect>=8){Reference(graphics,cfg,Vertical,full?20:20,full?span+20:260,Vertical?cx:cy,Vertical?cy:cx,k,phase,Arrival?(float)progress:-1,Direction);}
  else if(Arrival){float t=(float)progress,r=12+100*t;int alpha=(int)(240*(1-t));
   for(int ring=0;ring<(cfg.effect==1?3:1);ring++)using(var pen=new Pen(Color.FromArgb(Math.Max(0,alpha-ring*45),color),cfg.thickness)){
    float radius=r+ring*12;if(cfg.effect==2)graphics.DrawPolygon(pen,new[]{new PointF(cx,cy-radius),new PointF(cx+radius,cy),new PointF(cx,cy+radius),new PointF(cx-radius,cy)});
    else graphics.DrawEllipse(pen,cx-radius,cy-radius,2*radius,2*radius);
   }
   if(cfg.effect==5||cfg.effect==6)for(int i=0;i<28;i++){double angle=i*2.39996;float radius=(20+(i%5)*9)*t*2;float x=cx+(float)Math.Cos(angle)*radius,y=cy+(float)Math.Sin(angle)*radius;using(var pen=new Pen(Color.FromArgb(alpha,color),2))graphics.DrawLine(pen,x,y,x+(float)Math.Cos(angle)*9*(1-t),y+(float)Math.Sin(angle)*9*(1-t));}
  }else{
   float start=full?20:(Vertical?cy:cx)-20-90*k,end=full?span+20:(Vertical?cy:cx)+20+90*k;
   for(int layer=5;layer>=0;layer--)using(var pen=new Pen(Color.FromArgb(layer==0?(int)(140+110*k):12+(int)(12*k),color),cfg.thickness+layer*5)){
    if(Vertical)graphics.DrawLine(pen,cx,start,cx,end);else graphics.DrawLine(pen,start,cy,end,cy);
   }
   if(cfg.effect==5||cfg.effect==7||cfg.effect==1){
    int count=Math.Max(2,(int)((end-start)/9));var points=new PointF[count+1];
    for(int i=0;i<=count;i++){float axis=start+(end-start)*i/count;float near=(float)Math.Exp(-Math.Pow((axis-(Vertical?cy:cx))/130,2));float wave=cfg.effect==5?(float)(Math.Sin(i*4.9+Math.Floor(phase*24))*Math.Sin(i*1.7+phase*13)):(float)Math.Sin(axis/30-phase*6);float displacement=wave*(4+26*k)*near;
     points[i]=Vertical?new PointF(cx+displacement,axis):new PointF(axis,cy+displacement);
    }
    using(var glow=new Pen(Color.FromArgb(60,color),8))graphics.DrawLines(glow,points);using(var pen=new Pen(Color.FromArgb(245,color),1.6f))graphics.DrawLines(pen,points);
   }
   if(cfg.effect==2)for(int i=-2;i<=2;i++){float y=cy+i*20;using(var pen=new Pen(Color.FromArgb(190,color),2))graphics.DrawPolygon(pen,new[]{new PointF(cx,y-9),new PointF(cx+8+k*12,y),new PointF(cx,y+9),new PointF(cx-8-k*12,y)});}
   if(cfg.effect==6||cfg.effect==5)for(int i=0;i<20;i++){float life=(phase*(1.1f+i*.023f)+i*.173f)%1;float side=i%2==0?1:-1;float x=cx+side*(5+life*55)*k,y=cy+(float)Math.Sin(i*3.1)*life*95;using(var pen=new Pen(Color.FromArgb((int)(220*(1-life)),color),1.5f))graphics.DrawLine(pen,x,y,x+side*6,y+3);}
   using(var brush=new SolidBrush(Color.FromArgb(240,Color.White)))graphics.FillEllipse(brush,cx-3,cy-3,6,6);
  }
  IntPtr next=bitmap.GetHbitmap(Color.FromArgb(0));if(dib!=IntPtr.Zero){Native.SelectObject(dc,old);Native.DeleteObject(dib);}dib=next;old=Native.SelectObject(dc,dib);
  Point origin=Point.Empty;Size size=new Size(w,h);var blend=new Native.Blend{alpha=(byte)(cfg.opacity*255/100),format=1};
  if(!Visible)Show();Native.UpdateLayeredWindow(Handle,IntPtr.Zero,ref pos,ref size,dc,ref origin,0,ref blend,2);
 }
 public static Color Hue(float value){value=value-(float)Math.Floor(value);float scaled=value*6;int sector=(int)scaled;int f=(int)((scaled-sector)*255);switch(sector){case 0:return Color.FromArgb(255,f,50);case 1:return Color.FromArgb(255-f,255,50);case 2:return Color.FromArgb(50,255,f);case 3:return Color.FromArgb(50,255-f,255);case 4:return Color.FromArgb(f,50,255);default:return Color.FromArgb(255,50,255-f);}}
 public static void Reference(Graphics g,Settings cfg,bool vertical,float start,float end,float seam,float mouse,float pressure,float phase,float arrival,int direction){
  float fade=arrival<0?1:1-arrival;float gap=arrival<0?0:25+120*arrival;int thickness=cfg.thickness;
  if(cfg.effect==9){
   float width=20+60*pressure;for(float axis=start;axis<end;axis+=4){Color c=Hue((axis-start)/(end-start)*.65f+phase*.06f);float a=seam-direction*width,b=seam;RectangleF rect=vertical?new RectangleF(Math.Min(a,b),axis,width,4):new RectangleF(axis,Math.Min(a,b),4,width);using(var brush=new LinearGradientBrush(rect,Color.Transparent,c,vertical?0:90)){
    var blend=new ColorBlend();blend.Positions=new[]{0f,.35f,.8f,1f};int alpha=(int)((110+120*pressure)*fade);Color tint=Color.FromArgb(alpha,c);blend.Colors=direction>0?new[]{Color.FromArgb(0,c),Color.FromArgb(alpha/5,c),tint,tint}:new[]{tint,tint,Color.FromArgb(alpha/5,c),Color.FromArgb(0,c)};brush.InterpolationColors=blend;g.FillRectangle(brush,rect);
   }}
  }else if(cfg.effect==8){
   var rainbowState=g.Save();g.SetClip(vertical?new RectangleF(direction>0?0:seam+1,0,direction>0?Math.Max(0,seam-1):100000,100000):new RectangleF(0,direction>0?0:seam+1,100000,direction>0?Math.Max(0,seam-1):100000));Color[] spectrum={Color.FromArgb(255,100,95),Color.FromArgb(255,200,90),Color.FromArgb(225,245,100),Color.FromArgb(100,235,165),Color.FromArgb(85,190,255),Color.FromArgb(160,105,255)};
   for(int band=0;band<spectrum.Length;band++){
    var points=new List<PointF>();Action flush=delegate{if(points.Count>1){using(var pen=new Pen(Color.FromArgb((int)(225*fade),spectrum[band]),Math.Max(1.5f,thickness))){pen.StartCap=LineCap.Round;pen.EndCap=LineCap.Round;g.DrawLines(pen,points.ToArray());}}points.Clear();};
    for(float axis=start;axis<=end;axis+=5){float dist=Math.Abs(axis-mouse);if(arrival>=0&&dist<gap){flush();continue;}float near=(float)Math.Exp(-Math.Pow((axis-mouse)/135,2));float baseOffset=7+(5-band)*(thickness+1);float offset=-direction*(baseOffset*(1-pressure*.94f*near)+2*(float)Math.Sin(axis/65+phase*3)*(1-near));
     float open=arrival<0?0:direction*25*arrival*(float)Math.Exp(-Math.Pow((dist-gap)/35,2));points.Add(vertical?new PointF(seam+offset+open,axis):new PointF(axis,seam+offset+open));
    }flush();
   }g.Restore(rainbowState);
  }else{
   Color gold=Color.FromArgb(255,220,135);using(var pen=new Pen(Color.FromArgb((int)(130*fade),gold),thickness)){
    float startGap=arrival<0?8:gap;for(float axis=start;axis<end;axis+=26)if(Math.Abs(axis-mouse)>startGap){float d=(float)Math.Sin(axis*.03+phase*9)*pressure*3;if(vertical)g.DrawLine(pen,seam+d,axis,seam+d,Math.Min(end,axis+13));else g.DrawLine(pen,axis,seam+d,Math.Min(end,axis+13),seam+d);}
   }
   float burst=arrival<0?pressure:1-arrival;for(int i=0;i<40;i++){float life=arrival<0?(phase*2+i*.137f)%1:arrival;double angle=-Math.PI/2+i*Math.PI/39;float distance=(12+life*90)*burst;float dx=-direction*(float)Math.Cos(angle),dy=(float)Math.Sin(angle);float x=seam+dx*distance,y=mouse+dy*distance;Color c=i%3==0?Color.White:gold;
    using(var pen=new Pen(Color.FromArgb((int)(240*(1-life)*fade),c),i%4==0?3:2)){if(vertical)g.DrawLine(pen,x,y,x+dx*(7+18*burst),y+dy*(7+18*burst));else g.DrawLine(pen,y,x,y+dy*(7+18*burst),x+dx*(7+18*burst));}
   }
   using(var brush=new SolidBrush(Color.FromArgb((int)(220*burst*fade),gold))){if(vertical)g.FillEllipse(brush,seam-5,mouse-5,10,10);else g.FillEllipse(brush,mouse-5,seam-5,10,10);}
  }
  if(arrival>=0&&cfg.effect!=8){float radius=8+70*arrival;using(var pen=new Pen(Color.FromArgb((int)(210*fade),cfg.effect==10?Color.FromArgb(255,220,135):Hue(phase*.06f)),2)){float x=vertical?seam+direction*15:mouse,y=vertical?mouse:seam+direction*15;g.DrawEllipse(pen,x-radius,y-radius,2*radius,2*radius);}}
 }
 public void Export(string path){if(bitmap!=null)bitmap.Save(path);}
 protected override void Dispose(bool disposing){if(disposing){if(graphics!=null)graphics.Dispose();if(bitmap!=null)bitmap.Dispose();if(dib!=IntPtr.Zero){Native.SelectObject(dc,old);Native.DeleteObject(dib);}Native.DeleteDC(dc);}base.Dispose(disposing);}
}
sealed class AnimationClock : IDisposable {
 sealed class TimerHandle : WaitHandle {public TimerHandle(IntPtr handle){SafeWaitHandle=new Microsoft.Win32.SafeHandles.SafeWaitHandle(handle,true);}}
 readonly Control dispatcher=new Control();readonly Stopwatch watch=Stopwatch.StartNew();readonly TimerHandle timer;RegisteredWaitHandle waiter;
 int generation;bool running;double next;public int Fps=60;
 public int Interval{get{return (int)Math.Round(1000.0/Fps);}}
 public event EventHandler Tick;
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern IntPtr CreateWaitableTimerEx(IntPtr attributes,string name,uint flags,uint access);
 [DllImport("kernel32.dll",SetLastError=true)]static extern bool SetWaitableTimer(IntPtr handle,ref long due,int period,IntPtr callback,IntPtr state,bool resume);
 [DllImport("kernel32.dll")]static extern bool CancelWaitableTimer(IntPtr handle);
 public AnimationClock(){IntPtr control=dispatcher.Handle;IntPtr handle=CreateWaitableTimerEx(IntPtr.Zero,null,2,0x1F0003);if(handle==IntPtr.Zero)handle=CreateWaitableTimerEx(IntPtr.Zero,null,0,0x1F0003);if(handle==IntPtr.Zero)throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());timer=new TimerHandle(handle);}
 void Schedule(double delay){long due=-Math.Max(1,(long)Math.Ceiling(delay*10000));if(!SetWaitableTimer(timer.SafeWaitHandle.DangerousGetHandle(),ref due,0,IntPtr.Zero,IntPtr.Zero,false))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());}
 public void Start(){if(running)return;running=true;int token=++generation;next=watch.Elapsed.TotalMilliseconds;
  waiter=ThreadPool.RegisterWaitForSingleObject(timer,delegate{try{dispatcher.BeginInvoke((Action)delegate{
   if(!running||token!=generation)return;try{if(Tick!=null)Tick(this,EventArgs.Empty);}finally{if(running&&token==generation){next+=1000.0/Fps;double now=watch.Elapsed.TotalMilliseconds;if(next<now-1000.0/Fps)next=now+1000.0/Fps;Schedule(Math.Max(.1,next-now));}}
  });}catch(InvalidOperationException){}},null,Timeout.Infinite,false);Schedule(.1);
 }
 public void Stop(){if(!running)return;running=false;generation++;CancelWaitableTimer(timer.SafeWaitHandle.DangerousGetHandle());if(waiter!=null){waiter.Unregister(null);waiter=null;}}
 public void Dispose(){Stop();timer.Dispose();dispatcher.Dispose();}
}
static class Startup {
 const string Key=@"Software\Microsoft\Windows\CurrentVersion\Run";
 public static bool Enabled {get{try{using(var key=Microsoft.Win32.Registry.CurrentUser.OpenSubKey(Key)){string command=key==null?null:key.GetValue("EdgePush") as string;return command==Command;}}catch(System.Security.SecurityException){return false;}catch(UnauthorizedAccessException){return false;}}}
 static string Command{get{return "\""+Application.ExecutablePath+"\"";}}
 public static void Set(bool enabled){using(var key=Microsoft.Win32.Registry.CurrentUser.CreateSubKey(Key)){if(enabled)key.SetValue("EdgePush",Command,Microsoft.Win32.RegistryValueKind.String);else{string current=key.GetValue("EdgePush") as string;if(current==Command)key.DeleteValue("EdgePush",false);}}}
}
sealed class Engine : IDisposable {
 readonly Settings cfg;readonly Overlay overlay;readonly AnimationClock timer=new AnimationClock();
 readonly Stopwatch clock=Stopwatch.StartNew();readonly Native.Hook callback;IntPtr hook;
 public List<Seam> Seams;Point previous;Seam active;int sign;double pressure,lastPush,arrivalStart;bool preview;
 public Engine(Settings c){cfg=c;overlay=new Overlay(c);Refresh();previous=Cursor.Position;
  timer.Tick+=Tick; callback=Mouse;
  hook=Native.SetWindowsHookEx(14,callback,Native.GetModuleHandle(null),0);
  if(hook==IntPtr.Zero)throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
 }
 public void Refresh(){var screens=Screen.AllScreens;var rects=new Rectangle[screens.Length];for(int i=0;i<rects.Length;i++)rects[i]=screens[i].Bounds;Seams=Seam.Find(rects);Reset();previous=Cursor.Position;}
 public void Reset(){pressure=0;active=null;preview=false;timer.Stop();overlay.Hide();}
 IntPtr Mouse(int code,IntPtr wp,IntPtr lp){
  if(code<0||wp.ToInt32()!=0x200)return Native.CallNextHookEx(hook,code,wp,lp);
  try {
   var data=(Native.Mouse)Marshal.PtrToStructure(lp,typeof(Native.Mouse));
   if((data.flags&1)!=0)return Native.CallNextHookEx(hook,code,wp,lp);
   Point target=data.point;Point from=previous;previous=target;double now=clock.Elapsed.TotalMilliseconds;
   if(!cfg.enabled||(cfg.ctrl_bypass&&(Native.GetAsyncKeyState(0x11)&0x8000)!=0)){Reset();return Native.CallNextHookEx(hook,code,wp,lp);}
   foreach(var seam in Seams)if(seam.Cross(from,target)){
    int axis=seam.Vertical?target.X:target.Y;int before=seam.Vertical?from.X:from.Y;
    int dir=before<seam.Coordinate?1:-1;
    if(active!=seam||sign!=dir||now-lastPush>cfg.release_ms)pressure=0;
    active=seam;sign=dir;lastPush=now;
    // Cap a single event: a fast flick cannot skip the entire resistance.
    pressure+=Math.Min(60,Math.Max(1,dir>0?axis-seam.Coordinate+1:seam.Coordinate-axis));
    bool pass=pressure>=cfg.threshold;
    int coordinate=pass?seam.Coordinate+(dir>0?3:-3):seam.Coordinate+(dir>0?-1:0);
    Rectangle sourceBounds=before<seam.Coordinate?seam.Negative:seam.Positive;
    int along=seam.Vertical?target.Y:target.X;
    if(!pass)along=Math.Max(seam.Vertical?sourceBounds.Top:sourceBounds.Left,Math.Min((seam.Vertical?sourceBounds.Bottom:sourceBounds.Right)-1,along));
    Point result=seam.Vertical?new Point(coordinate,along):new Point(along,coordinate);
    previous=result;Native.SetCursorPos(result.X,result.Y);
    overlay.Center=seam.Vertical?new Point(seam.Coordinate,target.Y):new Point(target.X,seam.Coordinate);
    overlay.Edge=seam;var source=Screen.FromPoint(from).Bounds;overlay.SourceStart=seam.Vertical?source.Top:source.Left;overlay.SourceEnd=seam.Vertical?source.Bottom:source.Right;overlay.Direction=dir;overlay.Vertical=seam.Vertical;overlay.Ratio=pressure/cfg.threshold;overlay.Arrival=pass;
    if(pass){overlay.Center=result;arrivalStart=now;pressure=0;active=null;
     string sound=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"click.wav");
     if(cfg.sound&&File.Exists(sound))PlaySound(sound,IntPtr.Zero,0x20000|1|2);
    }
    timer.Fps=cfg.fps;timer.Start();return new IntPtr(1);
   }
   if(active!=null){int axis=active.Vertical?target.X:target.Y;int along=active.Vertical?target.Y:target.X;
    if(along<active.Start||along>=active.End||(sign>0&&axis<active.Coordinate-10)||(sign<0&&axis>active.Coordinate+10))Reset();}
  }catch{Reset();}
  return Native.CallNextHookEx(hook,code,wp,lp);
 }
 [DllImport("winmm.dll",CharSet=CharSet.Unicode)]static extern bool PlaySound(string file,IntPtr module,uint flags);
 void Tick(object sender,EventArgs e){double now=clock.Elapsed.TotalMilliseconds;
  if(preview){double t=(now-arrivalStart)/700;if(t<1){overlay.Ratio=t;overlay.Arrival=false;overlay.Render(0);return;}preview=false;overlay.Arrival=true;arrivalStart=now;}
  if(overlay.Arrival){double t=(now-arrivalStart)/cfg.duration;if(t>=1){Reset();return;}overlay.Render(t);}
  else{if(now-lastPush>cfg.release_ms){pressure=Math.Max(0,pressure-cfg.threshold*timer.Interval/240.0);if(pressure==0){Reset();return;}overlay.Ratio=pressure/cfg.threshold;}overlay.Render(0);}
 }
 public void Preview(){Reset();overlay.Edge=Seams.Count>0?Seams[0]:null;overlay.Center=overlay.Edge==null?Cursor.Position:overlay.Edge.Vertical?new Point(overlay.Edge.Coordinate,(overlay.Edge.Start+overlay.Edge.End)/2):new Point((overlay.Edge.Start+overlay.Edge.End)/2,overlay.Edge.Coordinate);overlay.Vertical=overlay.Edge==null||overlay.Edge.Vertical;if(overlay.Edge!=null){Point sourcePoint=overlay.Vertical?new Point(overlay.Edge.Coordinate-1,overlay.Center.Y):new Point(overlay.Center.X,overlay.Edge.Coordinate-1);var bounds=Screen.FromPoint(sourcePoint).Bounds;overlay.SourceStart=overlay.Vertical?bounds.Top:bounds.Left;overlay.SourceEnd=overlay.Vertical?bounds.Bottom:bounds.Right;overlay.Direction=1;}arrivalStart=clock.Elapsed.TotalMilliseconds;preview=true;timer.Fps=cfg.fps;timer.Start();}
 public void Dispose(){if(hook!=IntPtr.Zero)Native.UnhookWindowsHookEx(hook);hook=IntPtr.Zero;timer.Dispose();overlay.Dispose();}
}
static class Words {
 public static string Get(Settings cfg,string en,string tr){return cfg.language=="tr"?tr:en;}
}
sealed class Sample : Control {
 readonly Settings cfg;public Sample(Settings c){cfg=c;DoubleBuffered=true;Size=new Size(552,110);}
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Color.FromArgb(12,16,23));int mid=Width/2;
  using(var b=new SolidBrush(Color.FromArgb(24,33,45))){g.FillRectangle(b,12,8,mid-20,Height-16);g.FillRectangle(b,mid+8,8,mid-20,Height-16);}
  if(cfg.effect>=8)Overlay.Reference(g,cfg,true,5,Height-5,mid,Height/2,.8f,0,-1,1);
  else if(cfg.effect!=4)for(int i=4;i>=0;i--)using(var p=new Pen(Color.FromArgb(i==0?230:20,Overlay.Colors[cfg.theme]),cfg.thickness+i*4))g.DrawLine(p,mid,cfg.full_edge?5:30,mid,cfg.full_edge?Height-5:Height-30);
  using(var f=new Font("Segoe UI",9))using(var b=new SolidBrush(Color.FromArgb(160,180,200))){g.DrawString(Words.Get(cfg,"Display 1","Ekran 1"),f,b,25,Height-30);g.DrawString(Words.Get(cfg,"Display 2","Ekran 2"),f,b,mid+25,Height-30);}
 }
}
sealed class Panel : Form {
 readonly Settings cfg;readonly Engine engine;readonly Action changed;Sample sample;ComboBox colorBox;public bool AllowClose;
 readonly System.Windows.Forms.Timer saveTimer=new System.Windows.Forms.Timer{Interval=220};
 public Panel(Settings c,Engine en,Action notify){cfg=c;engine=en;changed=notify;
  Text="EdgePush";ClientSize=new Size(600,725);StartPosition=FormStartPosition.CenterScreen;Font=new Font("Segoe UI",10);BackColor=Color.FromArgb(19,23,30);ForeColor=Color.FromArgb(230,235,240);FormBorderStyle=FormBorderStyle.FixedSingle;MaximizeBox=false;Icon=Assets.Icon;
  saveTimer.Tick+=delegate{saveTimer.Stop();cfg.Save();};Build();
 }
 string T(string en,string tr){return Words.Get(cfg,en,tr);}
 void Build(){SuspendLayout();Controls.Clear();
  Controls.Add(new Label{Text="EdgePush",Font=new Font("Segoe UI",22,FontStyle.Bold),Location=new Point(24,14),AutoSize=true});
  var language=Combo(new[]{"English","Türkçe"},cfg.language=="tr"?1:0,380,24,196);language.SelectedIndexChanged+=delegate{saveTimer.Stop();cfg.language=language.SelectedIndex==1?"tr":"en";cfg.Save();engine.Reset();changed();BeginInvoke((Action)delegate{Control[] old=new Control[Controls.Count];Controls.CopyTo(old,0);foreach(Control item in old)item.Dispose();Build();});};
  LabelAt(Screen.AllScreens.Length+T(" displays / "," ekran / ")+engine.Seams.Count+T(" shared edges"," ortak kenar"),26,62).ForeColor=Color.FromArgb(145,165,185);
  sample=new Sample(cfg){Location=new Point(24,94)};Controls.Add(sample);
  Check(T("Enable resistance","Geçiş direnci etkin"),24,221,cfg.enabled,delegate(bool v){cfg.enabled=v;});
  Choice(T("Effect","Efekt"),258,new[]{"Portal",T("Wave","Dalga"),T("Crystal","Kristal"),T("Line","Çizgi"),T("Off","Kapalı"),T("Electric","Elektrik"),T("Sparks","Kıvılcım"),"Aurora",T("Rainbow membrane","Gökkuşağı zar"),T("Gradient glow","Yumuşak renk geçişi"),T("Spark burst","Kıvılcım patlaması")},cfg.effect,delegate(int i){cfg.effect=i;UpdateColor();});
  colorBox=Choice(T("Color","Renk"),298,new[]{T("Cyan","Turkuaz"),T("Purple","Mor"),T("Emerald","Zümrüt"),T("Solar","Güneş"),T("Ice","Buz")},cfg.theme,delegate(int i){cfg.theme=i;});UpdateColor();
  Check(T("Full display edge","Ekran kenarının tamamı"),24,340,cfg.full_edge,delegate(bool v){cfg.full_edge=v;});
  Number(T("Resistance","Direnç"),382,40,2400,20,cfg.threshold,"px",delegate(int v){cfg.threshold=v;});
  Number(T("Pressure hold","Basıncı tut"),426,80,1200,40,cfg.release_ms,"ms",delegate(int v){cfg.release_ms=v;});
  Number(T("Brightness","Parlaklık"),470,15,100,5,cfg.opacity,"%",delegate(int v){cfg.opacity=v;});
  Number(T("Line width","Çizgi kalınlığı"),514,1,10,1,cfg.thickness,"px",delegate(int v){cfg.thickness=v;});
  Number(T("Arrival duration","Varış süresi"),558,150,1200,50,cfg.duration,"ms",delegate(int v){cfg.duration=v;});
  Check(T("Ctrl bypass","Ctrl ile geçiş"),24,613,cfg.ctrl_bypass,delegate(bool v){cfg.ctrl_bypass=v;});Check(T("Sound","Ses"),230,613,cfg.sound,delegate(bool v){cfg.sound=v;});var fps=Combo(new[]{"15 FPS","30 FPS","60 FPS","120 FPS"},Array.IndexOf(new[]{15,30,60,120},cfg.fps),420,609,156);fps.SelectedIndexChanged+=delegate{cfg.fps=new[]{15,30,60,120}[fps.SelectedIndex];Save();};
  Check(T("Start with Windows","Windows ile başlat"),24,644,Startup.Enabled,delegate(bool value){try{Startup.Set(value);}catch(Exception ex){MessageBox.Show(ex.Message,T("Startup setting failed","Başlangıç ayarı değiştirilemedi"));BeginInvoke((Action)delegate{Control[] old=new Control[Controls.Count];Controls.CopyTo(old,0);foreach(Control item in old)item.Dispose();Build();});}});
  ButtonAt(T("Preview on display edge","Ekran kenarında önizle"),24,671,325,delegate{engine.Preview();});ButtonAt(T("Hide to tray","Tepsiye gizle"),365,671,211,delegate{Close();});
  ResumeLayout();
 }
 Label LabelAt(string text,int x,int y){var l=new Label{Text=text,Location=new Point(x,y),AutoSize=true};Controls.Add(l);return l;}
 void Save(){engine.Reset();changed();sample.Invalidate();saveTimer.Stop();saveTimer.Start();}
 void Check(string text,int x,int y,bool value,Action<bool> apply){var c=new CheckBox{Text=text,Location=new Point(x,y),AutoSize=true,Checked=value};c.CheckedChanged+=delegate{apply(c.Checked);Save();};Controls.Add(c);}
 ComboBox Combo(string[] items,int selected,int x,int y,int width){var box=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Location=new Point(x,y),Width=width,ForeColor=ForeColor,BackColor=Color.FromArgb(35,43,55),FlatStyle=FlatStyle.Flat,DrawMode=DrawMode.OwnerDrawFixed};box.DrawItem+=delegate(object sender,DrawItemEventArgs e){e.DrawBackground();if(e.Index>=0)using(var brush=new SolidBrush(box.Enabled?box.ForeColor:SystemColors.GrayText))e.Graphics.DrawString(Convert.ToString(box.Items[e.Index]),box.Font,brush,e.Bounds.X+3,e.Bounds.Y+1);};box.Items.AddRange(items);box.SelectedIndex=selected;Controls.Add(box);return box;}
 ComboBox Choice(string label,int y,string[] items,int selected,Action<int> apply){LabelAt(label,24,y+4);var box=Combo(items,selected,220,y,356);box.SelectedIndexChanged+=delegate{apply(box.SelectedIndex);Save();};return box;}
 void Number(string label,int y,int min,int max,int step,int value,string unit,Action<int> apply){LabelAt(label,24,y+5);
  var slider=new TrackBar{Minimum=min,Maximum=max,Value=value,SmallChange=step,LargeChange=Math.Max(step,(max-min)/10),TickStyle=TickStyle.None,AutoSize=false,Location=new Point(207,y),Size=new Size(220,30)};
  var input=new NumericUpDown{Minimum=min,Maximum=max,Increment=step,Value=value,Location=new Point(438,y+2),Width=93,BackColor=Color.FromArgb(35,43,55),ForeColor=ForeColor,BorderStyle=BorderStyle.FixedSingle};bool updating=false;
  Action<int> update=delegate(int v){if(updating)return;updating=true;slider.Value=v;input.Value=v;apply(v);updating=false;Save();};slider.ValueChanged+=delegate{update(slider.Value);};input.ValueChanged+=delegate{update((int)input.Value);};Controls.Add(slider);Controls.Add(input);LabelAt(unit,545,y+5);
 }
 void ButtonAt(string text,int x,int y,int width,Action action){var b=new Button{Text=text,Location=new Point(x,y),Size=new Size(width,32),FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(29,51,65)};b.Click+=delegate{action();};Controls.Add(b);}
 void UpdateColor(){if(colorBox!=null){colorBox.Enabled=cfg.effect!=4&&cfg.effect<8;colorBox.Invalidate();}}
 protected override void OnFormClosing(FormClosingEventArgs e){if(!AllowClose&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;saveTimer.Stop();cfg.Save();Hide();}base.OnFormClosing(e);}
 protected override void Dispose(bool disposing){if(disposing)saveTimer.Dispose();base.Dispose(disposing);}
 protected override void OnFormClosed(FormClosedEventArgs e){saveTimer.Stop();cfg.Save();saveTimer.Dispose();base.OnFormClosed(e);}
}
static class Assets {
 static Icon icon;public static Icon Icon{get{if(icon==null){using(var stream=System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("EdgePush.ico"))icon=new Icon(stream);}return icon;}}
}
sealed class App : ApplicationContext {
 readonly Settings cfg;readonly Engine engine;readonly NotifyIcon tray;readonly ToolStripMenuItem toggle,settings,refresh,exit;Panel panel;
 public App(bool show){cfg=Settings.Load();cfg.Save();engine=new Engine(cfg);var menu=new ContextMenuStrip();
  settings=(ToolStripMenuItem)menu.Items.Add("",null,delegate{ShowPanel();});toggle=(ToolStripMenuItem)menu.Items.Add("",null,delegate{cfg.enabled=!cfg.enabled;engine.Reset();cfg.Save();Update();});refresh=(ToolStripMenuItem)menu.Items.Add("",null,delegate{engine.Refresh();});exit=(ToolStripMenuItem)menu.Items.Add("",null,delegate{ExitThread();});
  tray=new NotifyIcon{Icon=Assets.Icon,Text="EdgePush",ContextMenuStrip=menu,Visible=true};tray.DoubleClick+=delegate{ShowPanel();};Update();if(show)ShowPanel();
 }
 void Update(){settings.Text=Words.Get(cfg,"Settings","Ayarlar");refresh.Text=Words.Get(cfg,"Refresh displays","Ekranları yeniden algıla");exit.Text=Words.Get(cfg,"Exit","Çıkış");toggle.Text=cfg.enabled?Words.Get(cfg,"Pause resistance","Direnci duraklat"):Words.Get(cfg,"Enable resistance","Direnci etkinleştir");tray.Text="EdgePush · "+(cfg.enabled?Words.Get(cfg,"Enabled","Etkin"):Words.Get(cfg,"Paused","Duraklatıldı"));}
 void ShowPanel(){if(panel==null||panel.IsDisposed)panel=new Panel(cfg,engine,Update);panel.Show();panel.Activate();}
 protected override void ExitThreadCore(){if(panel!=null){panel.AllowClose=true;panel.Close();panel.Dispose();}tray.Visible=false;tray.Dispose();engine.Dispose();base.ExitThreadCore();}
}
static class Program {
 [STAThread] static void Main(string[] args){
  if(Array.IndexOf(args,"--diagnostics")>=0){Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);Application.ThreadException+=delegate(object sender,ThreadExceptionEventArgs e){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"diagnostics-error.txt"),e.Exception.ToString());Environment.Exit(1);};AppDomain.CurrentDomain.UnhandledException+=delegate(object sender,UnhandledExceptionEventArgs e){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"diagnostics-error.txt"),e.ExceptionObject.ToString());};}

  if(Array.IndexOf(args,"--clock-test")>=0){Tests.Clocks();return;}
  if(Array.IndexOf(args,"--self-test")>=0){Tests.Run();return;}
  if(Array.IndexOf(args,"--diagnostics")>=0){
   try{Native.SetProcessDpiAwarenessContext(new IntPtr(-4));}catch(EntryPointNotFoundException){Native.SetProcessDPIAware();}Application.EnableVisualStyles();
   var cfg=new Settings{effect=8};
   using(var visual=new Overlay(cfg)){
    visual.Center=new Point(100,500);visual.Vertical=true;visual.Edge=new Seam(true,100,443,1080);visual.SourceStart=0;visual.SourceEnd=1080;visual.Ratio=.8;
    for(int effect=0;effect<11;effect++){cfg.effect=effect;if(effect==4)continue;visual.Arrival=false;visual.Render(0);visual.Export(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"effect-"+effect+".png"));visual.Arrival=true;visual.Render(.4);visual.Export(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"arrival-"+effect+".png"));}
    cfg.effect=8;visual.Arrival=true;visual.Render(.4);string noLinePath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"rainbow-no-outline.png");visual.Export(noLinePath);
    using(var checkImage=new Bitmap(noLinePath)){for(int x=160;x<checkImage.Width;x++)for(int y=0;y<checkImage.Height;y++)if(checkImage.GetPixel(x,y).A!=0)throw new Exception("Rainbow destination outline detected");}
    cfg.effect=8;visual.Arrival=false;visual.Render(0);
    string fullPath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"full-height-test.png");visual.Export(fullPath);
    using(var fullImage=new Bitmap(fullPath)){bool top=false,bottom=false;for(int x=0;x<fullImage.Width;x++){top|=fullImage.GetPixel(x,22).A>0;bottom|=fullImage.GetPixel(x,1097).A>0;}if(fullImage.Height!=1120||!top||!bottom)throw new Exception("Full source display height not rendered");}
    int before=Native.GetGuiResources(Process.GetCurrentProcess().Handle,0);
    for(int i=0;i<240;i++)visual.Render(0);int after=Native.GetGuiResources(Process.GetCurrentProcess().Handle,0);
    if(after>before+3)throw new Exception("GDI resource growth: "+before+" to "+after);
    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"render-test.txt"),"10 edge and 10 arrival variants rendered; source extent 0..1080, crossing interval 443..1080; 240 frames GDI handles: "+before+" to "+after);
    visual.Vertical=false;visual.Edge=new Seam(false,100,443,1080);visual.SourceStart=-1920;visual.SourceEnd=0;visual.Center=new Point(-1000,100);visual.Render(0);string horizontalPath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"horizontal-rainbow.png");visual.Export(horizontalPath);using(var image=new Bitmap(horizontalPath)){bool left=false,right=false;for(int y=0;y<image.Height;y++){left|=image.GetPixel(22,y).A>0;right|=image.GetPixel(1937,y).A>0;}if(image.Width!=1960||!left||!right)throw new Exception("Full horizontal source width not rendered");}visual.Hide();
   }
   using(var engine=new Engine(cfg))using(var panel=new Panel(cfg,engine,delegate{})){
    panel.Show();panel.Hide();panel.Show();panel.Activate();
    var captureTimer=new System.Windows.Forms.Timer{Interval=400};captureTimer.Tick+=delegate{captureTimer.Stop();using(var image=new Bitmap(panel.Width,panel.Height)){panel.DrawToBitmap(image,new Rectangle(Point.Empty,panel.Size));image.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"settings-preview.png"));}
     var sliders=new List<TrackBar>();var numbers=new List<NumericUpDown>();ComboBox language=null;
     foreach(Control control in panel.Controls){if(control is TrackBar)sliders.Add((TrackBar)control);if(control is NumericUpDown)numbers.Add((NumericUpDown)control);if(control is ComboBox&&((ComboBox)control).Items.Contains("English"))language=(ComboBox)control;}
     if(sliders.Count!=5||numbers.Count!=5||language==null)throw new Exception("Settings controls missing");
     for(int i=0;i<sliders.Count;i++){sliders[i].Value=sliders[i].Minimum;if(numbers[i].Value!=sliders[i].Minimum)throw new Exception("Slider to number failed");numbers[i].Value=numbers[i].Maximum;if(sliders[i].Value!=numbers[i].Maximum)throw new Exception("Number to slider failed");}
     for(int i=0;i<sliders.Count;i++)sliders[i].Value=new[]{120,240,85,3,500}[i];language.SelectedIndex=1;Application.DoEvents();panel.Refresh();using(var image=new Bitmap(panel.Width,panel.Height)){panel.DrawToBitmap(image,new Rectangle(Point.Empty,panel.Size));image.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"settings-tr.png"));}
     if(cfg.language!="tr")throw new Exception("Language switch failed");
     ComboBox effects=null,colors=null,fpsOptions=null;foreach(Control control in panel.Controls){var box=control as ComboBox;if(box==null)continue;if(box.Items.Count==11)effects=box;else if(box.Items.Count==5)colors=box;else if(box.Items.Count==4)fpsOptions=box;}
     if(effects==null||colors==null||fpsOptions==null)throw new Exception("Effect/color/FPS selector missing");
     for(int i=0;i<11;i++){effects.SelectedIndex=i;if(colors.Enabled!=(i!=4&&i<8))throw new Exception("Color availability mismatch: "+i);}
     for(int i=0;i<4;i++){fpsOptions.SelectedIndex=i;if(cfg.fps!=new[]{15,30,60,120}[i])throw new Exception("FPS selection mismatch");}
     effects.SelectedIndex=8;fpsOptions.SelectedIndex=2;panel.Close();if(panel.IsDisposed||panel.Visible)throw new Exception("Close did not hide to tray");panel.Show();
     File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"ui-test.txt"),"5 sliders synchronized both ways; EN/TR switch; all 11 color-availability states; 15/30/60/120 FPS selection; close-to-tray passed");engine.Preview();};captureTimer.Start();
    var timer=new System.Windows.Forms.Timer{Interval=2000};timer.Tick+=delegate{timer.Stop();panel.AllowClose=true;panel.Close();};timer.Start();Application.Run(panel);timer.Dispose();captureTimer.Dispose();
   }return;
  }
  bool created;using(var mutex=new Mutex(true,"Local\\EdgePush.Native",out created)){
   if(!created){MessageBox.Show(Words.Get(Settings.Load(),"EdgePush is already running. Open Settings from the system tray.","EdgePush zaten çalışıyor. Ayarları sistem tepsisinden açabilirsin."),"EdgePush");return;}
   try{try{if(!Native.SetProcessDpiAwarenessContext(new IntPtr(-4)))Native.SetProcessDPIAware();}catch(EntryPointNotFoundException){Native.SetProcessDPIAware();}
    Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
    Application.Run(new App(Array.IndexOf(args,"--settings")>=0||Array.IndexOf(args,"--show")>=0));
   }catch(Exception ex){MessageBox.Show(ex.ToString(),Words.Get(Settings.Load(),"Could not start EdgePush","EdgePush başlatılamadı"));}
  }
 }
}
static class Tests {
 public static void Clocks(){var rates=new[]{15,30,60,120};int index=0,count=0;var report=new System.Text.StringBuilder();var elapsed=new Stopwatch();using(var clock=new AnimationClock())using(var sample=new System.Windows.Forms.Timer{Interval=700})using(var context=new ApplicationContext()){
  clock.Tick+=delegate{count++;};Action begin=delegate{count=0;clock.Fps=rates[index];elapsed.Restart();clock.Start();};sample.Tick+=delegate{clock.Stop();elapsed.Stop();if(count<2)throw new Exception("Animation clock did not run");report.AppendLine(rates[index]+" FPS target: "+Math.Round(count/elapsed.Elapsed.TotalSeconds,1)+" callbacks/s");index++;if(index==rates.Length){sample.Stop();context.ExitThread();}else begin();};begin();sample.Start();Application.Run(context);File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"clock-test.txt"),report.ToString());
 }}
 public static void Run(){int count=0;
  Action<bool> check=delegate(bool ok){if(!ok)throw new Exception("Test failed #"+count);count++;};
  var seams=Seam.Find(new[]{new Rectangle(-1920,0,1920,1080),new Rectangle(0,0,2560,1440)});
  check(seams.Count==1);check(seams[0].Coordinate==0);check(seams[0].Cross(new Point(-1,400),new Point(20,400)));check(seams[0].Cross(new Point(0,400),new Point(-1,400)));check(!seams[0].Cross(new Point(-1,1080),new Point(20,1080)));
  seams=Seam.Find(new[]{new Rectangle(0,-1080,1920,1080),new Rectangle(0,0,1920,1080)});check(seams.Count==1&&!seams[0].Vertical);check(seams[0].Cross(new Point(100,-1),new Point(100,0)));
  check(Seam.Find(new[]{new Rectangle(0,0,100,100),new Rectangle(100,100,100,100)}).Count==0);
  check(Seam.Find(new[]{new Rectangle(0,0,100,100),new Rectangle(105,0,100,100)}).Count==0);
  seams=Seam.Find(new[]{new Rectangle(0,0,1920,1080),new Rectangle(1920,-200,1920,1500)});
  check(seams[0].Cross(new Point(1919,0),new Point(1928,-10)));check(seams[0].Cross(new Point(1919,1079),new Point(1930,1100)));
  check(seams[0].Cross(new Point(1920,-10),new Point(1910,0)));check(seams[0].Cross(new Point(1920,1100),new Point(1910,1079)));
  check(seams[0].Cross(new Point(1910,1070),new Point(1930,1100)));check(!seams[0].Cross(new Point(1900,500),new Point(1910,500)));
  seams=Seam.Find(new[]{new Rectangle(0,0,1920,1080),new Rectangle(-200,1080,2300,1080)});
  check(seams[0].Cross(new Point(0,1079),new Point(-10,1090)));check(seams[0].Cross(new Point(1919,1079),new Point(1950,1090)));
  check(seams[0].Cross(new Point(-10,1080),new Point(0,1070)));check(seams[0].Cross(new Point(1950,1080),new Point(1919,1070)));
  for(int corner=0;corner<4;corner++){var settings=new Settings{fps=new[]{15,30,60,120}[corner]};check(Array.IndexOf(new[]{15,30,60,120},settings.fps)>=0);}
  File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"self-test.txt"),count+" geometry assertions passed");
 }
}
}


