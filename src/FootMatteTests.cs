using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace NaiwaDesktop
{
    internal static class FootMatteTests
    {
        private static void Check(bool value,string name,List<string> log)
        { if(!value)throw new Exception("FAIL: "+name);log.Add("PASS: "+name); }

        private static Bitmap Decode(ZipArchiveEntry entry)
        {
            using(Stream stream=entry.Open())using(Image image=Image.FromStream(stream))
            {
                Bitmap bitmap=new Bitmap(image.Width,image.Height,PixelFormat.Format32bppPArgb);
                using(Graphics graphics=Graphics.FromImage(bitmap))
                {graphics.CompositingMode=CompositingMode.SourceCopy;graphics.DrawImageUnscaled(image,0,0);}
                return bitmap;
            }
        }
        private static byte[] Pixels(Bitmap bitmap)
        {
            BitmapData data=bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppPArgb);
            try{byte[] pixels=new byte[data.Stride*bitmap.Height];Marshal.Copy(data.Scan0,pixels,0,pixels.Length);return pixels;}
            finally{bitmap.UnlockBits(data);}
        }
        internal static void Run(Assets assets,string folder,List<string> log)
        {
            bool localized=true,solidPreserved=true,premultiplied=true;
            int frames=0,idleChanges=0,standingChanges=0,airChanges=0,lyingChanges=0;
            Bitmap idleBefore=null,idleAfter=null,laughBefore=null,laughAfter=null;
            try
            {
                using(ZipArchive zip=ZipFile.OpenRead(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"素材.nwp")))
                foreach(ZipArchiveEntry entry in zip.Entries)
                {
                    bool laugh=entry.FullName.StartsWith("laugh/",StringComparison.Ordinal);
                    if(!entry.FullName.EndsWith(".png",StringComparison.OrdinalIgnoreCase)||
                        !(laugh||entry.FullName.StartsWith("look/",StringComparison.Ordinal)||entry.FullName.StartsWith("blink/",StringComparison.Ordinal)))continue;
                    int index=laugh?Int32.Parse(Path.GetFileNameWithoutExtension(entry.FullName)):-1;
                    using(Bitmap bitmap=Decode(entry))
                    {
                        byte[] before=Pixels(bitmap);
                        if(entry.FullName=="look/00.png")idleBefore=(Bitmap)bitmap.Clone();
                        if(laugh&&index==30)laughBefore=(Bitmap)bitmap.Clone();
                        FootMatte.Apply(bitmap,index);byte[] after=Pixels(bitmap);
                        if(entry.FullName=="look/00.png")idleAfter=(Bitmap)bitmap.Clone();
                        if(laugh&&index==30)laughAfter=(Bitmap)bitmap.Clone();
                        Rectangle[] regions=FootMatte.Regions(index);int changes=0;
                        for(int y=0;y<bitmap.Height;y++)for(int x=0;x<bitmap.Width;x++)
                        {
                            int at=(y*bitmap.Width+x)*4;
                            bool different=false;
                            for(int channel=0;channel<4;channel++)if(before[at+channel]!=after[at+channel])different=true;
                            if(!different)continue;
                            changes++;
                            if(!regions[0].Contains(x,y)&&!regions[1].Contains(x,y))localized=false;
                            if(before[at+3]>200)solidPreserved=false;
                            if(after[at+3]<before[at+3])premultiplied=false;
                            for(int channel=0;channel<3;channel++)if(after[at+channel]>after[at+3])premultiplied=false;
                        }
                        if(!laugh)idleChanges+=changes;
                        else if(index<355)standingChanges+=changes;
                        else if(index<374)airChanges+=changes;
                        else lyingChanges+=changes;
                        frames++;
                    }
                }
                Check(frames==assets.Count+assets.EyeCount+2*assets.BlinkCount,"foot matte scans every original idle, blink and laugh frame",log);
                Check(localized&&solidPreserved,"foot repair preserves every non-foot pixel and opaque foot detail",log);
                Check(premultiplied,"foot repair retains valid premultiplied colors and never erodes the source alpha",log);
                Check(idleChanges>0&&standingChanges>0&&airChanges>0&&lyingChanges>0,"foot repair covers idle, standing laugh, take-off and lying poses",log);
                SaveComparison(idleBefore,idleAfter,laughBefore,laughAfter,Path.Combine(folder,"foot-repair-comparison.png"));
                log.Add("INFO: foot matte changed pixels: idle/blink="+idleChanges+", standing="+standingChanges+", airborne="+airChanges+", lying="+lyingChanges);
            }
            finally
            {
                if(idleBefore!=null)idleBefore.Dispose();if(idleAfter!=null)idleAfter.Dispose();
                if(laughBefore!=null)laughBefore.Dispose();if(laughAfter!=null)laughAfter.Dispose();
            }
        }
        private static void SaveComparison(Bitmap idleBefore,Bitmap idleAfter,Bitmap laughBefore,Bitmap laughAfter,string path)
        {
            if(idleBefore==null||idleAfter==null||laughBefore==null||laughAfter==null)throw new InvalidDataException("Missing foot comparison samples.");
            const int columnWidth=470,rowHeight=220;
            using(Bitmap canvas=new Bitmap(columnWidth*2,rowHeight*4,PixelFormat.Format32bppArgb))
            using(Graphics graphics=Graphics.FromImage(canvas))
            using(Font font=new Font("Microsoft YaHei UI",10,FontStyle.Regular))
            {
                graphics.InterpolationMode=InterpolationMode.NearestNeighbor;
                graphics.PixelOffsetMode=PixelOffsetMode.Half;
                for(int row=0;row<4;row++)for(int column=0;column<2;column++)
                {
                    bool dark=row%2==1;Rectangle cell=new Rectangle(column*columnWidth,row*rowHeight,columnWidth,rowHeight);
                    using(Brush background=new SolidBrush(dark?Color.FromArgb(28,35,31):Color.FromArgb(246,246,236)))graphics.FillRectangle(background,cell);
                    using(Brush ink=new SolidBrush(dark?Color.FromArgb(235,237,222):Color.FromArgb(39,65,45)))
                        graphics.DrawString((row<2?"待机":"大笑")+" · "+(column==0?"修复前":"修复后"),font,ink,cell.Left+12,cell.Top+7);
                    Bitmap source=row<2?(column==0?idleBefore:idleAfter):(column==0?laughBefore:laughAfter);
                    graphics.DrawImage(source,new Rectangle(cell.Left+10,cell.Top+30,450,189),new Rectangle(75,300,150,63),GraphicsUnit.Pixel);
                }
                canvas.Save(path,ImageFormat.Png);
            }
        }
    }
}
