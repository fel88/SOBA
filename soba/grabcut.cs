using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Soba
{
    public partial class grabcut : Form
    {
        public grabcut()
        {
            InitializeComponent();
            ctx.Init(pictureBox1);
            ctx.Bmp = new Bitmap(3000, 3000);
            ctx.gr = Graphics.FromImage(ctx.Bmp);
            Load += Form1_Load;
            pictureBox1.MouseDown += PictureBox1_MouseDown;
            pictureBox1.MouseUp += PictureBox1_MouseUp;
        }
        Mat _tmp;
        Mat _bgd = new Mat();
        Mat _fgd = new Mat();
        private void PictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            ldrag = false;
            if (_mask != null)
            {
                _tmp = _mask & (byte)GrabCutClasses.FGD;
                var c = Cv2.CountNonZero(_tmp);

                if (c > 0)
                {
                    _mask.CopyTo(_gcut);
                    Cv2.GrabCut(_src, _gcut, new Rect(), _bgd, _fgd, 1, GrabCutModes.InitWithMask);
                    show();
                }
            }
        }

        bool ldrag;
        private void PictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            switch (tool)
            {
                case SegTool.GrabCut:
                    {
                        if (e.Button == MouseButtons.Left && currentBmp != null)
                        {
                            ldrag = true;
                            var pt = pictureBox1.PointToClient(Cursor.Position);
                            var _pt = ctx.BackTransform(pt);
                            lstart = new OpenCvSharp.Point(_pt.X, currentBmp.Height - _pt.Y);
                        }
                    }
                    break;
                case SegTool.Brush:
                    {
                        if (e.Button == MouseButtons.Left && currentBmp != null)
                        {
                            /* ldrag = true;
                             var pt = pictureBox1.PointToClient(Cursor.Position);
                             var _pt = ctx.BackTransform(pt);
                             var tt = new OpenCvSharp.Point(_pt.X, currentBmp.Height - _pt.Y);*/

                        }
                    }
                    break;
                case SegTool.Flood:
                    {
                        if (e.Button == MouseButtons.Left && currentBmp != null)
                        {
                            var w = _src.Width;
                            var h = _src.Height;
                            var pt = pictureBox1.PointToClient(Cursor.Position);
                            var _pt = ctx.BackTransform(pt);
                            var seed = new OpenCvSharp.Point(_pt.X, currentBmp.Height - _pt.Y);

                            //h,w,chn = im.shape
                            //seed = (w / 2, h / 2)
                            ///OpenCvSharp.Point seed = new OpenCvSharp.Point(w / 2, h / 2);
                            var flood = _src.Clone();
                            Mat mask = Mat.Zeros(new OpenCvSharp.Size(_src.Width + 2, _src.Height + 2), MatType.CV_8UC1);
                            int floodflags = 4;
                            floodflags |= (int)OpenCvSharp.FloodFillFlags.MaskOnly;
                            floodflags |= (int)(255 << 8);
                            //Cv2.FloodFill(_src, mask, seed, new Scalar(255, 0, 0));
                            var rect = new Rect();
                            Cv2.FloodFill(flood, mask, seed, new Scalar(0, 0, 255), out rect, new Scalar(floodParam, floodParam, floodParam, floodParam), new Scalar(floodParam, floodParam, floodParam, floodParam), FloodFillFlags.Link4);
                            //Cv2.FloodFill(_src, seed, new Scalar(0, 0, 255),);
                            Cv2.Circle(flood, seed, 2, new Scalar(0, 255, 0), Cv2.FILLED, LineTypes.AntiAlias);
                            Rect crop = new Rect(1, 1, mask.Width - 2, mask.Height - 2);
                            mask = new Mat(mask, crop);
                            HierarchyIndex[] hi;
                            Cv2.FindContours(mask, out co, out hi, RetrievalModes.List, ContourApproximationModes.ApproxNone);
                            for (int i = 0; i < co.Length; i++)
                            {
                                double tol = 0.5;
                                co[i] = simplifyDouglasPeucker(co[i].Select(z => new Point2f(z.X, z.Y)).ToArray(), tol).Select(z => new OpenCvSharp.Point(z.X, z.Y)).ToArray();
                            }
                            mask *= 255;
                            //num,im,mask,rect = cv2.floodFill(im, mask, seed, (255, 0, 0), (10,) * 3, (10,) * 3, floodflags)
                            pictureBox2.Image = BitmapConverter.ToBitmap(mask);
                        }
                    }
                    break;
            }

        }

        OpenCvSharp.Point lstart;

        private void Form1_Load(object sender, EventArgs e)
        {
            mf = new MessageFilter();
            Application.AddMessageFilter(mf);
        }

        MessageFilter mf = null;        

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != DialogResult.OK) return;

            currentBmp = Bitmap.FromFile(ofd.FileName) as Bitmap;
            toolStripStatusLabel1.Text = currentBmp.Width + "x" + currentBmp.Height;
            _src = BitmapConverter.ToMat(currentBmp);
            if (_src.Channels() == 4)
            {
                var spl = _src.Split();
                OpenCvSharp.Cv2.Merge(new Mat[] { spl[0], spl[1], spl[2] }, _src);
                currentBmp = _src.ToBitmap();

            }
            _src2 = new Mat();
            _src.CopyTo(_src2);
            //  roi = new Rect(0, 0, _src.Cols, _src.Rows);
            _mask = Mat.Ones(_src.Size(), MatType.CV_8UC1) * (byte)GrabCutClasses.PR_BGD;
            _bin = Mat.Zeros(_src.Size(), MatType.CV_8UC1);
        }
        Mat _gcut = new Mat();
        Mat _mask;
        GrabCutClasses _mode = GrabCutClasses.FGD;
        OpenCvSharp.Point[][] co;

        Mat getBinMask()
        {

            Mat binmask = new Mat(_gcut.Size(), MatType.CV_8U);
            binmask = _gcut & (byte)GrabCutClasses.FGD;
            binmask = binmask * 255;


            Mat tmp = new Mat();
            binmask.CopyTo(tmp);


            OpenCvSharp.HierarchyIndex[] hi;

            binmask *= 0;
            Cv2.FindContours(tmp, out co, out hi, RetrievalModes.CComp, ContourApproximationModes.ApproxNone);
            //simplify
            for (int i = 0; i < co.Length; i++)
            {
                double tol = 0.5;
                co[i] = simplifyDouglasPeucker(co[i].Select(z => new Point2f(z.X, z.Y)).ToArray(), tol).Select(z => new OpenCvSharp.Point(z.X, z.Y)).ToArray();
            }

            for (int i = 0; i < co.Length; i++)
            {
                if (hi[i].Parent !=-1) continue;
                if (Cv2.ContourArea(co[i]) < 50) continue;
                Cv2.DrawContours(binmask, co, i, new Scalar(255, 255, 255), Cv2.FILLED, LineTypes.AntiAlias);
            }
            for (int i = 0; i < co.Length; i++)
            {
                if (hi[i].Parent ==-1) continue;
                if (Cv2.ContourArea(co[i]) < 50) continue;
                Cv2.DrawContours(binmask, co, i, new Scalar(0, 0, 0), Cv2.FILLED, LineTypes.AntiAlias);
            }

            binmask.CopyTo(_bin);

            return binmask;
        }
        Mat _src;
        Mat _src2;
        Mat _bin;
        Mat getFG()
        {

            Mat fg = Mat.Zeros(_src.Size(), _src.Type());
            Mat mask = getBinMask();
            _src.CopyTo(fg, mask);
            return fg;
        }

        DrawingContext ctx = new DrawingContext();
        Bitmap currentBmp;
        private void timer1_Tick(object sender, EventArgs e)
        {
            var pt = pictureBox1.PointToClient(Cursor.Position);
            var _pt2 = ctx.BackTransform(pt);
            if (ldrag && tool == SegTool.GrabCut)
            {

                var _pt = new OpenCvSharp.Point(_pt2.X, currentBmp.Height - _pt2.Y);


                Cv2.Line(_mask, lstart, _pt, new Scalar((byte)_mode), 1);

                if (_mode == GrabCutClasses.FGD) Cv2.Line(_src2, lstart, _pt, new Scalar(255, 0, 0), 1);
                else if (_mode == GrabCutClasses.BGD) Cv2.Line(_src2, lstart, _pt, new Scalar(0, 255, 0), 1);

                lstart = _pt;
                currentBmp = BitmapConverter.ToBitmap(_src2);
                //cout << _pt << endl;

            }
            if (ldrag && tool == SegTool.Brush)
            {

                //polyBool subtract
                // OR contour-> mask then fill circle -> back to contours

            }
            ctx.UpdateDrag();
            ctx.gr.Clear(Color.White);
            ctx.gr.DrawLine(Pens.Red, ctx.Transform(new PointF()), ctx.Transform(new PointF(100, 0)));
            ctx.gr.DrawLine(Pens.Blue, ctx.Transform(new PointF()), ctx.Transform(new PointF(0, 100)));

            var p0 = ctx.Transform(new PointF());
            if (currentBmp != null)
            {
                ctx.gr.DrawImage(currentBmp, new RectangleF(p0.X, p0.Y - currentBmp.Height * ctx.zoom, currentBmp.Width * ctx.zoom, currentBmp.Height * ctx.zoom), new RectangleF(0, 0, currentBmp.Width, currentBmp.Height), GraphicsUnit.Pixel);
                var _pt = new OpenCvSharp.Point(_pt2.X, _pt2.Y);
                var p = ctx.Transform(_pt.X, _pt.Y);

            }
            if (co != null && co.Length > 0)
            {
                foreach (var itemc in co)
                {


                    List<PointF> fff = new List<PointF>();

                    foreach (var item in itemc)
                    {
                        var p = ctx.Transform(item.X, currentBmp.Height - item.Y);
                        fff.Add(new PointF(p.X, p.Y));
                    }

                    if (fff.Count > 3)
                        ctx.gr.DrawPolygon(new Pen(Color.LightGreen, 2), fff.ToArray());

                    for (int i = 0; i < fff.Count; i++)
                    {
                        //if (i % 30 == 0) continue;
                        PointF cc = fff[i];
                        var tp = (cc);
                        int w = 6;
                        //ctx.gr.FillRectangle(Brushes.LightPink, tp.X, tp.Y, 10, 10);
                        ctx.gr.FillEllipse(Brushes.LightGreen, tp.X - w / 2, tp.Y - w / 2, w, w);
                    }
                }
            }
            if (currentBmp != null)
            {

                var _pt = new OpenCvSharp.Point(_pt2.X, _pt2.Y);
                var p = ctx.Transform(_pt.X, _pt.Y);
                if (tool == SegTool.Brush)
                {
                    var bs = brushSize * ctx.zoom;
                    ctx.gr.FillEllipse(Brushes.Red, p.X - bs / 2, p.Y - bs / 2, bs, bs);
                }
            }
            pictureBox1.Image = ctx.Bmp;

        }

        
        public static double getSqDist(Point2f p1, Point2f p2)
        {

            var dx = p1.X - p2.X;
            var dy = p1.Y - p2.Y;

            return dx * dx + dy * dy;
        }

        // square distance from a point to a segment
        public static double getSqSegDist(Point2f p, Point2f p1, Point2f p2)
        {

            var x = p1.X;
            var y = p1.Y;
            var dx = p2.X - x;
            var dy = p2.Y - y;

            if (dx != 0 || dy != 0)
            {

                var t = ((p.X - x) * dx + (p.Y - y) * dy) / (dx * dx + dy * dy);

                if (t > 1)
                {
                    x = p2.X;
                    y = p2.Y;

                }
                else if (t > 0)
                {
                    x += dx * t;
                    y += dy * t;
                }
            }

            dx = p.X - x;
            dy = p.Y - y;

            return dx * dx + dy * dy;
        }
        public static void simplifyDPStep(Point2f[] points, int first, int last, double? sqTolerance, List<Point2f> simplified)
        {
            var maxSqDist = sqTolerance;
            var index = -1;
            var marked = false;
            for (var i = first + 1; i < last; i++)
            {
                var sqDist = getSqSegDist(points[i], points[first], points[last]);

                if (sqDist > maxSqDist)
                {
                    index = i;
                    maxSqDist = sqDist;
                }
            }


            if (maxSqDist > sqTolerance || marked)
            {
                if (index - first > 1) simplifyDPStep(points, first, index, sqTolerance, simplified);
                simplified.Add(points[index]);
                if (last - index > 1) simplifyDPStep(points, index, last, sqTolerance, simplified);
            }
        }

        // simplification using Ramer-Douglas-Peucker algorithm
        public static Point2f[] simplifyDouglasPeucker(Point2f[] points, double? sqTolerance)
        {
            var last = points.Length - 1;

            var simplified = new List<Point2f>();
            simplified.Add(points[0]);
            simplifyDPStep(points, 0, last, sqTolerance, simplified);
            simplified.Add(points[last]);

            return simplified.ToArray();
        }
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            _mode = GrabCutClasses.FGD;
            tool = SegTool.GrabCut;
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            _mode = GrabCutClasses.BGD;

        }
        //  Mat _dsp;
        //   Rect roi;
        void show()
        {
            Scalar fg_color = new Scalar(255, 0, 0);
            Scalar bg_color = new Scalar(0, 255, 0);
            Mat scribbled_src = _src.Clone();
            const float alpha = 0.7f;
            for (int y = 0; y < _gcut.Rows; y++)
            {
                for (int x = 0; x < _gcut.Cols; x++)
                {
                    if (_gcut.At<byte>(y, x) == (byte)GrabCutClasses.FGD)
                    {
                        Cv2.Circle(scribbled_src, new OpenCvSharp.Point(x, y), 2, fg_color, -1);
                    }
                    else if (_gcut.At<byte>(y, x) == (byte)GrabCutClasses.BGD)
                    {
                        Cv2.Circle(scribbled_src, new OpenCvSharp.Point(x, y), 2, bg_color, -1);
                    }
                    else if (_gcut.At<byte>(y, x) == (byte)GrabCutClasses.PR_BGD)
                    {
                        Vec3b pix = scribbled_src.At<Vec3b>(y, x);
                        pix[0] = (byte)(pix[0] * alpha + bg_color[0] * (1 - alpha));
                        pix[1] = (byte)(pix[1] * alpha + bg_color[1] * (1 - alpha));
                        pix[2] = (byte)(pix[2] * alpha + bg_color[2] * (1 - alpha));
                        scribbled_src.Set<Vec3b>(y, x, pix);

                    }
                    else if (_gcut.At<byte>(y, x) == (byte)GrabCutClasses.PR_FGD)
                    {
                        Vec3b pix = scribbled_src.At<Vec3b>(y, x);
                        pix[0] = (byte)(pix[0] * alpha + fg_color[0] * (1 - alpha));
                        pix[1] = (byte)(pix[1] * alpha + fg_color[1] * (1 - alpha));
                        pix[2] = (byte)(pix[2] * alpha + fg_color[2] * (1 - alpha));
                        scribbled_src.Set<Vec3b>(y, x, pix);
                    }
                }
            }



            pictureBox2.Image = scribbled_src.ToBitmap();

            Mat fg = getFG();


            pictureBox3.Image = fg.ToBitmap();


            Mat msk = getBinMask();
            Cv2.CvtColor(msk, msk, ColorConversionCodes.GRAY2BGR);


            pictureBox4.Image = msk.ToBitmap();


        }
        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            //_src.CopyTo(new Mat(_dsp,roi));         // 
            _src.CopyTo(_src2);
            currentBmp = BitmapConverter.ToBitmap(_src2);

            _mask = Mat.Ones(_src.Size(), MatType.CV_8UC1) * (byte)GrabCutClasses.PR_BGD;
            _gcut = Mat.Ones(_src.Size(), MatType.CV_8UC1) * (byte)GrabCutClasses.PR_BGD;

            show();
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            if (tableLayoutPanel1.ColumnStyles[1].Width == 0)
            {
                tableLayoutPanel1.ColumnStyles[1].SizeType = SizeType.Percent;
                tableLayoutPanel1.ColumnStyles[1].Width = 50;
            }
            else
            {
                tableLayoutPanel1.ColumnStyles[1].SizeType = SizeType.Absolute;
                tableLayoutPanel1.ColumnStyles[1].Width = 0;
            }

        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {


            _src = BitmapConverter.ToMat(currentBmp);
            _src = _src.Resize(new OpenCvSharp.Size(_src.Width / 2, _src.Height / 2));
            currentBmp = _src.ToBitmap();


            _src2 = new Mat();
            _src.CopyTo(_src2);
            //  roi = new Rect(0, 0, _src.Cols, _src.Rows);
            _mask = Mat.Ones(_src.Size(), MatType.CV_8UC1) * (byte)GrabCutClasses.PR_BGD;
            _bin = Mat.Zeros(_src.Size(), MatType.CV_8UC1);

            toolStripStatusLabel1.Text = currentBmp.Width + "x" + currentBmp.Height;
        }
        SegTool tool = SegTool.GrabCut;
        float brushSize = 100;
        public enum SegTool
        {
            None, GrabCut, Flood, Brush
        }
        private void toolStripButton7_Click(object sender, EventArgs e)
        {
            tool = SegTool.Flood;            
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            pictureBox2.Image.Save("temp.jpg");
            Process.Start("temp.jpg");
        }
        int floodParam = 5;
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            floodParam = trackBar1.Value;
            brushSize = trackBar1.Value * 5;

        }

        private void toolStripButton8_Click(object sender, EventArgs e)
        {
            tool = SegTool.Brush;
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            pictureBox3.Image.Save("temp.jpg");
            Process.Start("temp.jpg");
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {
            pictureBox4.Image.Save("temp.jpg");
            Process.Start("temp.jpg");
        }
                
    }
}
