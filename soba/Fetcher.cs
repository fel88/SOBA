using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Soba
{
    public partial class Fetcher : Form
    {
        public Fetcher()
        {
            InitializeComponent();
        }
        OpenCvSharp.VideoCapture cap;
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != DialogResult.OK) return;
            cap = new OpenCvSharp.VideoCapture(ofd.FileName);
            Text = $"Source: {ofd.FileName}";
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (cap == null || pause) return;
            if (forwTo)
            {

                var ofps = cap.Get(5);//5 fps

                forwTo = false;
                var frm = cap.Get(7);//7 frame count
                var secs = (frm / ofps) * 1000;

                cap.Set(0, forwPosPercetange * secs);//posmsec 0
            }

            if (oneFrameStep && oneFrameStepDir == -1)
            {
                var pf = cap.Get(1);//1 posframes
                cap.Set(1, Math.Max(0, pf - 2));
            }
            if (oneFrameStep) { pause = true; oneFrameStep = false; }


            Mat mat = new Mat();
            cap.Read(mat);
            pictureBox1.Image = BitmapConverter.ToBitmap(mat);
        }
        bool pause = false;
        private void pictureBox1_Click(object sender, EventArgs e)
        {
            pause = !pause;
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            listView1.Items.Add(new ListViewItem(new string[] { "image" }) { Tag = pictureBox1.Image });
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            Directory.CreateDirectory("images");
            var dir = Directory.CreateDirectory(Path.Combine("images", DateTime.Now.ToString().Replace(":", " -")));
            int cntr = 0;
            foreach (var item in listView1.Items)
            {
                var bmp = (item as ListViewItem).Tag as Bitmap;
                bmp.Save(Path.Combine(dir.FullName, $"{cntr++}.jpg"));
            }
            MessageBox.Show($"{listView1.Items.Count} were saved to {dir.FullName}", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        bool forwTo = false;
        double forwPosPercetange = 0;
        private void pictureBox2_Click(object sender, EventArgs e)
        {
            Bitmap bmp = new Bitmap(pictureBox2.Width, pictureBox2.Height);
            var gr = Graphics.FromImage(bmp);
            gr.Clear(Color.White);


            var pc = pictureBox2.PointToClient(Cursor.Position);
            var ff = pc.X / (float)pictureBox2.Width;
            forwTo = true;
            forwPosPercetange = ff;

            gr.FillRectangle(Brushes.LightBlue, 0, 0, (int)(forwPosPercetange * bmp.Width), bmp.Height);
            pictureBox2.Image = bmp;
        }
        bool oneFrameStep = false;
        int oneFrameStepDir = 1;
        private void button2_Click(object sender, EventArgs e)
        {
            pause = false;
            oneFrameStep = true;
            oneFrameStepDir = 1;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            pause = false;
            oneFrameStep = true;
            oneFrameStepDir = -1;
        }
    }
}
