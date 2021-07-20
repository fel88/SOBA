using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Soba
{
    public partial class Capturer : Form
    {
        public Capturer()
        {
            InitializeComponent();
        }
        public void UpdateList()
        {
            listView1.Items.Clear();
            var wnds = User32.FindWindows(delegate (IntPtr wnd, IntPtr param)
            {
                return User32.GetWindowText(wnd).Contains(textBox1.Text);
                return true;
            });
            User32.EnumWindows(delegate (IntPtr wnd, IntPtr param)
            {
                var txt = User32.GetWindowText(wnd);

                if (!string.IsNullOrEmpty(txt) && txt.ToUpper().Contains(textBox1.Text.ToUpper()))
                {
                    listView1.Items.Add(new ListViewItem(new string[] { wnd.ToString(), txt }) { Tag = wnd });
                }
                return true;
            }, IntPtr.Zero);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            UpdateList();
        }
        IntPtr hwn;
        Bitmap bmpScreenshot;
        Graphics gfxScreenshot;
        RECT rect;
        int cntr = 0;
        Mat lastCaptured;

        void captureHwnd(IntPtr wnd)
        {
            if (pictureBox1.Image != null)
            {
                pictureBox1.Image.Dispose();
            }

            var temp = User32.CaptureImage(wnd);
            lastCaptured = BitmapConverter.ToMat(temp);
            pictureBox1.Image = temp;

            hwn = wnd;
            //    label1.Text = "Handle: " + wnd.ToString();

            User32.GetWindowRect(hwn, out rect);

            if (bmpScreenshot != null)
            {
                bmpScreenshot.Dispose();
            }
            if (gfxScreenshot != null)
            {
                gfxScreenshot.Dispose();
            }
            bmpScreenshot = new Bitmap((rect.Width / 2 + 1) * 2, (rect.Height / 2 + 1) * 2, PixelFormat.Format24bppRgb);
            gfxScreenshot = Graphics.FromImage(bmpScreenshot);
        }

        IntPtr lastHwnd;
        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count <= 0) return;
            IntPtr wnd = (IntPtr)listView1.SelectedItems[0].Tag;
            lastHwnd = wnd;
            captureHwnd(wnd);
        }

        List<Mat> saved = new List<Mat>();
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            capture();
        }
        
        private void capture()
        {
            saved.Add(lastCaptured.Clone());
            listView2.Items.Add(new ListViewItem("frame #"+listView2.Items.Count) { Tag = saved.Last() });
        }

        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count == 0) return;
            var bmp = listView2.SelectedItems[0].Tag as Mat;
            pictureBox1.Image = BitmapConverter.ToBitmap(bmp) as Bitmap;
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            if (sfd.ShowDialog() != DialogResult.OK) return;
            var fi = new FileInfo(sfd.FileName);
            var di = new DirectoryInfo(fi.DirectoryName);
            int index = 0;
            foreach (var item in saved)
            {
                string path = Path.Combine(di.FullName, $"{index}_" + fi.Name);
                index++;
                item.SaveImage(path);
            }
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            saved.Clear();
            listView2.Items.Clear();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count == 0) return;
            var bmp = listView2.SelectedItems[0].Tag as Mat;
            saved.Remove(bmp);
            listView2.Items.Clear();
            foreach (var item in saved)
            {
                listView2.Items.Add(new ListViewItem("frame #"+listView2.Items.Count) { Tag = item });

            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            timer1.Enabled = checkBox1.Checked;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            captureHwnd(lastHwnd);
            capture();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            try
            {
                timer1.Interval = int.Parse(textBox2.Text);
                textBox2.BackColor = Color.White;
            }
            catch (Exception ex)
            {
                textBox2.BackColor = Color.Red;
            }
        }
    }
}
