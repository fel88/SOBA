using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace annotator1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            workBmp = new Bitmap(2000, 1500);
            gr = Graphics.FromImage(workBmp);
            ResizeEnd += Form1_ResizeEnd;
            tags.Add(new annotator1.Tag() { Name = "tag1" });            
            setTagToolStripMenuItem.DropDownItems.Clear();
            foreach (var item in tags)
            {
                var c = new ToolStripMenuItem(item.Name) { Tag = item };
                setTagToolStripMenuItem.DropDownItems.Add(c);
                c.Click += (s, e) =>
                {
                    selected.Tag = ((s as ToolStripMenuItem).Tag as Tag);
                    updateTagsList();
                };
            }


            pictureBox1.MouseWheel += PictureBox1_MouseWheel;
            updateTags();
        }

        void updateTags()
        {
            listView3.Items.Clear();
            foreach (var item in tags)
            {
                listView3.Items.Add(new ListViewItem(item.Name) { Tag = item });
            }
        }
        public float ZoomFactor = 1.2f;

        private void PictureBox1_MouseWheel(object sender, MouseEventArgs e)
        {
            //zoom *= Math.Sign(e.Delta) * 1.3f;
            //zoom += Math.Sign(e.Delta) * 0.31f;

            var pos = pictureBox1.PointToClient(Cursor.Position);
            if (!pictureBox1.ClientRectangle.IntersectsWith(new Rectangle(pos.X, pos.Y, 1, 1)))
            {
                return;
            }

            float zold = zoom;

            if (e.Delta > 0) { zoom *= ZoomFactor; } else { zoom /= ZoomFactor; }

            if (zoom < 0.08) { zoom = 0.08f; }
            if (zoom > 100) { zoom = 100f; }

            sx = -(pos.X / zold - sx - pos.X / zoom);
            sy = (pos.Y / zold + sy - pos.Y / zoom);
        }

        private void Form1_ResizeEnd(object sender, EventArgs e)
        {
            redraw();
        }

        Bitmap workBmp;

        void loadDir(string dpath)
        {
            Text = dpath;
            var d = new DirectoryInfo(dpath);
            listView2.Items.Clear();
            listView2.Items.Add(new ListViewItem(new string[] { ".." }) { Tag = d.Parent });
            foreach (var item in d.GetDirectories())
            {
                listView2.Items.Add(new ListViewItem(new string[] { item.Name }) { ForeColor = Color.Blue, Tag = item });

            }
            foreach (var item in d.GetFiles())
            {

                listView2.Items.Add(new ListViewItem(new string[] { item.Name }) { Tag = item });
                if (Items.Any(z => z.Path.ToLower() == item.FullName.ToLower()))
                {
                    var fr = Items.First(z => z.Path.ToLower() == item.FullName.ToLower());
                    if (fr.Infos.Count > 0)
                        listView2.Items[listView2.Items.Count - 1].BackColor = Color.LightGreen;
                    else
                        listView2.Items[listView2.Items.Count - 1].BackColor = Color.LightBlue;
                }
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != DialogResult.OK) return;
            var f = new FileInfo(ofd.FileName);
            loadDir(f.DirectoryName);

        }
        public float sx, sy;


        public PointF Transform(PointF p1)
        {
            return new PointF((p1.X + sx) * zoom, -(p1.Y + sy) * zoom);
        }


        Bitmap crnt;
        Graphics gr;
        float zoom = 1;
        void redraw()
        {
            gr.Clear(Color.White);
          
            UpdateDrag();
            //gr.DrawImage(crnt, new Rectangle(0, 0, pictureBox1.Width, pictureBox1.Height), new Rectangle(0, 0, crnt.Width, crnt.Height), GraphicsUnit.Pixel);
            if (crnt != null)
            {
                var tt = Transform(new PointF(0, 0));
                gr.DrawImage(crnt, tt.X, tt.Y, crnt.Width * zoom, crnt.Height * zoom);
            }
            if (currentItem != null)
                foreach (var item in currentItem.Infos)
                {
                    var pen = new Pen(Color.Blue, 2);
                    var t1 = Transform(item.Rect.Location);
                    if (selected == item)
                    {
                        var pen2 = new Pen(Color.Red, 2);
                        gr.DrawRectangle(pen2, t1.X, t1.Y, item.Rect.Width * zoom, item.Rect.Height * zoom);
                    }
                    else

                        gr.DrawRectangle(pen, t1.X, t1.Y, item.Rect.Width * zoom, item.Rect.Height * zoom);
                    
                    var fnt = new Font("Consolas", 14);
                    var ms = gr.MeasureString(item.Tag.Name, fnt);
                    gr.FillRectangle(Brushes.White, t1.X, t1.Y - 20, ms.Width, ms.Height);
                    gr.DrawString(item.Tag.Name, fnt, Brushes.Black, t1.X, t1.Y - 20);
                }
            if (drag)
            {
                var pos = BackTransform(pictureBox1.PointToClient(Cursor.Position));
                var t1 = Transform(startp);
                gr.DrawRectangle(Pens.Green, new Rectangle((int)t1.X, (int)t1.Y, (int)(zoom * Math.Abs(pos.X - startp.X)), (int)(zoom * Math.Abs(pos.Y - startp.Y))));
            }
            gr.DrawLine(Pens.Red, Transform(new PointF(0, 0)), Transform(new PointF(100, 0)));
            gr.DrawLine(Pens.Blue, Transform(new PointF(0, 0)), Transform(new PointF(0, 100)));
            var back = BackTransform(pictureBox1.PointToClient(Cursor.Position));
            
            gr.FillRectangle(new SolidBrush(Color.FromArgb(200,Color.White)), 0, 0, 100, 30);
            gr.DrawString($"{Math.Round(back.X,1)}, {Math.Round(back.Y,1)}", SystemFonts.DefaultFont, Brushes.Red, 0, 0);
            gr.DrawString(info, SystemFonts.DefaultFont, Brushes.Black, 0, 15);
            pictureBox1.Image = workBmp;

        }
        public PointF BackTransform(PointF p1)
        {
            var posx = (p1.X / zoom - sx);
            var posy = (-p1.Y / zoom - sy);
            return new PointF(posx, posy);
        }
        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count == 0) return;
            if (listView2.SelectedItems[0].Tag is DirectoryInfo di)
            {
                //loadDir(di.FullName);
                return;
            }
            var item = listView2.SelectedItems[0].Tag as FileInfo;
            if (!(item.Extension.ToLower().EndsWith("jpg") || item.Extension.ToLower().EndsWith("bmp") || item.Extension.ToLower().EndsWith("png")))
            {
                crnt = null;
                currentItem = null;
                updateTagsList();
                return;
            }
            crnt = Bitmap.FromFile(item.FullName) as Bitmap;
            var fr = Items.FirstOrDefault(z => z.Path.ToLower() == item.FullName.ToLower());
            if (fr == null)
            {
                Items.Add(new DataSetItem() { Path = item.FullName });
                fr = Items.Last();
            }
            currentItem = fr;
            if (fr.Infos.Count > 0)
            {
                listView2.SelectedItems[0].BackColor = Color.LightGreen;
            }
            else
                listView2.SelectedItems[0].BackColor = Color.LightBlue;
            updateTagsList();


            redraw();
            info = "count: " + Items.Count(z => z.Infos.Count > 0);
        }
        string info = "";
        List<Tag> tags = new List<Tag>();
        //List<RectInfo> infos = new List<RectInfo>();
        public void UpdateDrag()
        {
            if (isDrag)
            {
                var p = pictureBox1.PointToClient(Cursor.Position);

                sx = origsx + ((p.X - startx) / zoom);
                sy = origsy + (-(p.Y - starty) / zoom);
            }
        }
        Point startp;
        bool drag = false;
        public float startx, starty;
        public float origsx, origsy;
        public bool isDrag = false;
        DataSetItem currentItem = null;
        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currentItem != null)
            {
                currentItem.Infos.Clear();
            }

            updateTagsList();
        }

        private void updateTagsList()
        {

            listView1.Items.Clear();
            if (currentItem == null) return;
            foreach (var item in currentItem.Infos)
            {
                listView1.Items.Add(new ListViewItem(new string[] { item.Tag.Name }) { Tag = item });
            }
        }

        public class DataSetItem
        {
            public string Path;
            public List<RectInfo> Infos = new List<RectInfo>();
        }
        public List<DataSetItem> Items = new List<DataSetItem>();
        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
        public static string CreateMD5(byte[] inputBytes)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {

                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            if (sfd.ShowDialog() != DialogResult.OK) return;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\"?>");
            sb.AppendLine("<root>");
            foreach (var item in Items)
            {
                if (item.Infos.Count == 0) continue;
                var h = CreateMD5(File.ReadAllBytes(item.Path));
                sb.AppendLine($"<item hash=\"{h}\">");
                sb.AppendLine("<path>");
                sb.AppendLine("<![CDATA[" + item.Path + "]]>");
                sb.AppendLine("</path>");
                foreach (var info in item.Infos)
                {
                    sb.AppendLine($"<info x=\"{info.Rect.X}\" y=\"{info.Rect.Y}\" w=\"{info.Rect.Width}\" h=\"{info.Rect.Height}\" name=\"{info.Tag.Name}\"/>");
                }
                sb.AppendLine("</item>");
            }
            sb.AppendLine("</root>");
            File.WriteAllText(sfd.FileName, sb.ToString());
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != DialogResult.OK) return;
            var doc = XDocument.Load(ofd.FileName);
            Items.Clear();

            foreach (var item in doc.Descendants("item"))
            {
                var hash = item.Attribute("hash").Value;
                var path = item.Element("path").Value;
                Items.Add(new DataSetItem() { Path = path });
                foreach (var info in item.Elements("info"))
                {
                    var xx = int.Parse(info.Attribute("x").Value);
                    var yy = int.Parse(info.Attribute("y").Value);
                    var ww = int.Parse(info.Attribute("w").Value);
                    var hh = int.Parse(info.Attribute("h").Value);

                    Items.Last().Infos.Add(new RectInfo() { Tag = tags.First(), Rect = new Rectangle(xx, yy, ww, hh) });

                }
            }
            info = "count: " + Items.Count;
            Text = "items in dataset: " + Items.Count;

        }

        RectInfo selected = null;
        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;
            selected = listView1.SelectedItems[0].Tag as RectInfo;
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;
            var t = listView1.SelectedItems[0].Tag as RectInfo;
            currentItem.Infos.Remove(t);
            updateTagsList();
            selected = null;
        }

        private void setTagToolStripMenuItem_EnabledChanged(object sender, EventArgs e)
        {

        }

        private void setTagToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {

        }

        private void setTagToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void listView2_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listView2.SelectedItems.Count == 0) return;
            if (listView2.SelectedItems[0].Tag is DirectoryInfo di)
            {
                loadDir(di.FullName);
                return;
            }
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            Directory.CreateDirectory("annotations");
            Directory.CreateDirectory("images");
            StringBuilder sb2 = new StringBuilder();

            List<string> dirs = new List<string>();
            Dictionary<string, string> alias = new Dictionary<string, string>();
            foreach (var item in Items)
            {
                var fi = new FileInfo(item.Path);
                var nm = fi.Name;
                if (nm.Contains(" ")) continue;
                var folder = new DirectoryInfo(fi.DirectoryName).Name;

                if (!dirs.Contains(fi.DirectoryName.ToLower()))
                {
                    dirs.Add(fi.DirectoryName.ToLower());
                    int suffix = 0;
                    string p = folder;
                    if (alias.ContainsValue(p))
                    {
                        while (true)
                        {
                            p = $"{folder}{suffix}";
                            if (!alias.ContainsValue(p)) break;
                            suffix++;
                        }
                    }
                    alias.Add(fi.DirectoryName.ToLower(), p);
                }

                folder = alias[fi.DirectoryName.ToLower()];
                Directory.CreateDirectory(Path.Combine("images", folder));


                sb2.AppendLine($"{folder}/{nm} {folder}_{Path.GetFileNameWithoutExtension(nm)}.xml");

                var mat = OpenCvSharp.Cv2.ImRead(item.Path);
                var koef = 1024f / mat.Width;
                mat = mat.Resize(new OpenCvSharp.Size(1024, koef * mat.Height));
                var ipath = Path.Combine("images", folder, nm);
                if (!File.Exists(ipath))
                {
                    mat.SaveImage(ipath);
                }
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("<annotation>");
                sb.AppendLine($"<folder>{folder}</folder>");
                sb.AppendLine($"<filename>{nm}</filename>");
                sb.AppendLine($"<source><database>Unknown</database></source>");
                sb.AppendLine($"<size>");
                sb.AppendLine($"<width>{1024}</width>");
                sb.AppendLine($"<height>{(int)mat.Height}</height>");
                sb.AppendLine($"<depth>{3}</depth>");
                sb.AppendLine($"</size>");
                sb.AppendLine($"<segmented>{0}</segmented>");
                foreach (var info in item.Infos)
                {
                    sb.AppendLine("<object>");
                    sb.AppendLine("<name>face</name>");
                    sb.AppendLine("<pose>Unspecified</pose>");
                    sb.AppendLine("<truncated>0</truncated>");
                    sb.AppendLine("<difficult>0</difficult>");
                    sb.AppendLine("<bndbox>");
                    sb.AppendLine($"<xmin>{(int)(info.Rect.X * koef)}</xmin>");
                    sb.AppendLine($"<ymin>{-(int)(info.Rect.Y * koef)}</ymin>");
                    sb.AppendLine($"<xmax>{(int)(info.Rect.Right * koef)}</xmax>");
                    sb.AppendLine($"<ymax>{(int)((-info.Rect.Y + info.Rect.Height) * koef)}</ymax>");
                    sb.AppendLine("</bndbox>");
                    sb.AppendLine("</object>");
                }
                sb.AppendLine("</annotation>");
                var xpath = Path.Combine("annotations", folder + "_" + Path.GetFileNameWithoutExtension(nm) + ".xml");
                if (!File.Exists(xpath))
                {
                    File.WriteAllText(xpath, sb.ToString());
                }
            }
            File.WriteAllText("img_list.txt", sb2.ToString());
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            Capturer capt = new Capturer();
            capt.Show();
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            Items.Clear();
            info = "count: " + Items.Count;
            Text = "items in dataset: " + Items.Count;

        }

        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            var pos = pictureBox1.PointToClient(Cursor.Position);

            if (e.Button == MouseButtons.Right)
            {
                isDrag = true;

                startx = pos.X;
                starty = pos.Y;
                origsx = sx;
                origsy = sy;
            }
            else if (e.Button == MouseButtons.Left)
            {
                var temp = BackTransform(pos);
                startp = new Point((int)temp.X, (int)temp.Y);
                drag = true;
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            isDrag = false;

            if (drag)
            {
                var temp = BackTransform(pictureBox1.PointToClient(Cursor.Position));
                var endp = new Point((int)temp.X, (int)temp.Y);
                if (currentItem != null)
                {

                    var w = Math.Abs(endp.X - startp.X);
                    var h = Math.Abs(endp.Y - startp.Y);
                    if (w > 10 && h > 10)
                    {
                        currentItem.Infos.Add(new RectInfo()
                        {

                            Rect = new Rectangle(startp.X, startp.Y, w, h),
                            Tag = tags[0]
                        });
                    }
                }
                updateTagsList();
                drag = false;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {

            {
                redraw();
            }
        }
    }

    public class RectInfo
    {
        public Rectangle Rect;
        public Tag Tag;
    }

    public class Tag
    {
        public string Name;
    }
}

