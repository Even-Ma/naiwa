using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace NaiwaDesktop
{
    internal static class FootMatte
    {
        private sealed class Key
        {
            internal readonly int Frame;
            internal readonly Rectangle Left, Right;
            internal Key(int frame, Rectangle left, Rectangle right) { Frame = frame; Left = left; Right = right; }
        }
        private static Rectangle R(int x, int y, int width, int height) { return new Rectangle(x, y, width, height); }
        // Traced against the original 288 x 368 clip. The fast take-off and
        // landing need more keys than the standing section. The brown triangle
        // to the right in airborne frames is the tail and is deliberately out.
        private static readonly Key[] Keys = new Key[] {
            new Key(0,R(80,310,74,31),R(142,322,58,35)),
            new Key(8,R(80,310,74,31),R(142,322,58,35)),
            new Key(15,R(86,310,64,29),R(170,326,48,32)),
            new Key(340,R(86,310,64,29),R(170,326,48,32)),
            new Key(350,R(86,310,64,29),R(170,326,48,32)),
            new Key(355,R(85,312,61,27),R(161,330,47,27)),
            new Key(356,R(85,312,60,27),R(153,330,45,27)),
            new Key(357,R(85,312,60,27),R(145,329,46,28)),
            new Key(358,R(86,310,36,28),R(124,305,32,36)),
            new Key(359,R(76,306,28,29),R(101,286,31,38)),
            new Key(360,R(59,243,22,35),R(72,283,35,32)),
            new Key(361,R(59,243,22,35),R(72,283,35,32)),
            new Key(362,R(52,235,26,31),R(56,279,36,29)),
            new Key(363,R(50,236,26,26),R(49,278,37,31)),
            new Key(364,R(29,231,30,34),R(22,278,43,27)),
            new Key(365,R(35,232,30,29),R(13,276,49,27)),
            new Key(366,R(36,212,34,31),R(17,274,39,28)),
            new Key(367,R(56,196,35,35),R(14,271,38,33)),
            new Key(368,R(63,188,29,38),R(19,268,34,34)),
            new Key(370,R(63,190,29,37),R(20,268,34,35)),
            new Key(371,R(61,196,29,34),R(20,271,35,35)),
            new Key(372,R(61,196,29,34),R(20,271,35,35)),
            new Key(373,R(53,221,31,33),R(24,298,37,31)),
            new Key(374,R(50,238,33,30),R(28,309,33,25)),
            new Key(375,R(51,245,32,29),R(30,309,34,25)),
            new Key(378,R(51,245,32,29),R(30,309,34,25)),
            new Key(379,R(48,232,32,29),R(35,312,33,26)),
            new Key(380,R(50,222,29,29),R(35,312,33,26)),
            new Key(421,R(50,222,29,29),R(35,312,33,26))
        };
        private static Rectangle Mix(Rectangle a, Rectangle b, double t)
        {
            int left=(int)Math.Round(a.Left+(b.Left-a.Left)*t), top=(int)Math.Round(a.Top+(b.Top-a.Top)*t);
            int right=(int)Math.Round(a.Right+(b.Right-a.Right)*t), bottom=(int)Math.Round(a.Bottom+(b.Bottom-a.Bottom)*t);
            return Rectangle.FromLTRB(left,top,right,bottom);
        }
        internal static Rectangle[] Regions(int laughIndex)
        {
            if(laughIndex<0)return new Rectangle[]{Keys[0].Left,Keys[0].Right};
            for(int i=1;i<Keys.Length;i++)if(laughIndex<=Keys[i].Frame)
            {
                Key a=Keys[i-1],b=Keys[i];double t=(laughIndex-a.Frame)/(double)(b.Frame-a.Frame);
                return new Rectangle[]{Mix(a.Left,b.Left,t),Mix(a.Right,b.Right,t)};
            }
            return new Rectangle[]{Keys[Keys.Length-1].Left,Keys[Keys.Length-1].Right};
        }
        private static int Straight(byte value,int alpha) { return alpha==0?0:Math.Min(255,(value*255+alpha/2)/alpha); }
        private static bool Donor(byte[] source,int at)
        {
            int alpha=source[at+3];if(alpha<220)return false;
            int r=Straight(source[at+2],alpha),g=Straight(source[at+1],alpha),b=Straight(source[at],alpha);
            return r>=g-3&&g>b+5;
        }
        private static int FindDonor(byte[] source,int x,int y,int width,int height,int stride)
        {
            int best=-1,bestDistance=99;
            for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++)
            {
                int distance=dx*dx+dy*dy,nx=x+dx,ny=y+dy;
                if(distance>4||distance>=bestDistance||nx<0||ny<0||nx>=width||ny>=height)continue;
                int at=ny*stride+nx*4;if(Donor(source,at)){best=at;bestDistance=distance;}
            }
            return best;
        }
        internal static void Apply(Bitmap bitmap,int laughIndex=-1)
        {
            if(bitmap==null)throw new ArgumentNullException("bitmap");
            if(bitmap.PixelFormat!=PixelFormat.Format32bppPArgb)throw new ArgumentException("Foot matte expects a premultiplied 32-bit bitmap.","bitmap");
            // Different art must not accidentally inherit these source-specific
            // masks. The original asset decoder already validates dimensions.
            if(bitmap.Width!=288||bitmap.Height!=368)return;
            Rectangle[] regions=Regions(laughIndex);
            BitmapData data=bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),ImageLockMode.ReadWrite,PixelFormat.Format32bppPArgb);
            try
            {
                int stride=data.Stride;if(stride<=0)throw new InvalidDataException("Unsupported bitmap stride.");
                byte[] source=new byte[stride*bitmap.Height];Marshal.Copy(data.Scan0,source,0,source.Length);
                byte[] output=(byte[])source.Clone();
                Rectangle bounds=new Rectangle(2,2,bitmap.Width-4,bitmap.Height-4);
                for(int part=0;part<regions.Length;part++)
                {
                    Rectangle region=Rectangle.Intersect(regions[part],bounds);if(region.IsEmpty)continue;
                    int dw=region.Width+2,dh=region.Height+2;
                    byte[] dilated=new byte[dw*dh];
                    for(int yy=0;yy<dh;yy++)for(int xx=0;xx<dw;xx++)
                    {
                        int x=region.Left+xx-1,y=region.Top+yy-1,max=0;
                        for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)max=Math.Max(max,source[(y+dy)*stride+(x+dx)*4+3]);
                        dilated[yy*dw+xx]=(byte)max;
                    }
                    int[] bottom=new int[region.Width];
                    for(int xx=0;xx<region.Width;xx++)
                    {
                        bottom[xx]=-1;
                        for(int y=region.Top;y<region.Bottom;y++)if(source[y*stride+(region.Left+xx)*4+3]>=200)bottom[xx]=y;
                    }
                    for(int y=region.Top;y<region.Bottom;y++)for(int x=region.Left;x<region.Right;x++)
                    {
                        // In the small idle overlap, the first foot owns its
                        // sole; the second mask must not bridge its toe gaps.
                        if(part>0&&regions[0].Contains(x,y))continue;
                        int at=y*stride+x*4,alpha=source[at+3];if(alpha>200)continue;
                        int closed=255,xx=x-region.Left+1,yy=y-region.Top+1;
                        for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)closed=Math.Min(closed,dilated[(yy+dy)*dw+xx+dx]);
                        bool repair=closed>alpha&&y<bottom[x-region.Left]-1;
                        int r=Straight(source[at+2],alpha),g=Straight(source[at+1],alpha),b=Straight(source[at],alpha);
                        int low=Math.Min(r,Math.Min(g,b)),high=Math.Max(r,Math.Max(g,b));
                        bool green=alpha>0&&g>r+6&&g>b+12;
                        bool white=alpha>0&&low>145&&high-low<45;
                        bool edge=false;
                        if(green||white)for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)if(source[(y+dy)*stride+(x+dx)*4+3]<=20)edge=true;
                        bool clean=(green||white)&&edge;
                        if(!repair&&!clean)continue;
                        int donor=FindDonor(source,x,y,bitmap.Width,bitmap.Height,stride);if(donor<0)continue;
                        int next=repair?closed:alpha;output[at+3]=(byte)next;
                        for(int channel=0;channel<3;channel++)
                        {
                            int color=Straight(source[donor+channel],source[donor+3]);
                            int value=clean?(color*next+127)/255:source[at+channel]+(color*(next-alpha)+127)/255;
                            output[at+channel]=(byte)Math.Min(next,Math.Max(0,value));
                        }
                    }
                }
                Marshal.Copy(output,0,data.Scan0,output.Length);
            }
            finally{bitmap.UnlockBits(data);}
        }
    }
}
