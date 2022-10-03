using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Soba
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            workBmp = new Bitmap(2000, 1500);
            gr = Graphics.FromImage(workBmp);
            ResizeEnd += Form1_ResizeEnd;
            tags.Add(new Tag() { Name = "tag1" });

            pictureBox1.MouseWheel += PictureBox1_MouseWheel;
            updateTagsList();
            Load += Form1_Load;


            //hack
            toolStripButton2.BackgroundImageLayout = ImageLayout.None;
            toolStripButton2.BackgroundImage = new Bitmap(1, 1);
            toolStripButton2.BackColor = Color.LightGreen;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            mf = new MessageFilter();
            Application.AddMessageFilter(mf);
        }
        MessageFilter mf = null;

        void updateTagsList()
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
                    var tag = "(null)";
                    if (item.Tag != null)
                        tag = item.Tag.Name;
                    var ms = gr.MeasureString(tag, fnt);

                    gr.FillRectangle(Brushes.White, t1.X, t1.Y - 20, ms.Width, ms.Height);
                    gr.DrawString(tag, fnt, Brushes.Black, t1.X, t1.Y - 20);
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

            gr.FillRectangle(new SolidBrush(Color.FromArgb(200, Color.White)), 0, 0, 100, 30);
            gr.DrawString($"{Math.Round(back.X, 1)}, {Math.Round(back.Y, 1)}", SystemFonts.DefaultFont, Brushes.Red, 0, 0);
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
                updateInfosList();
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
            updateInfosList();


            redraw();
            if (autoFit)
            {
                fitAll();
            }
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

            updateInfosList();
        }

        private void updateInfosList()
        {

            listView1.Items.Clear();
            if (currentItem == null) return;
            foreach (var item in currentItem.Infos)
            {
                if (item.Tag == null)
                {
                    var t = new ListViewItem(new string[] { "(null)" }) { Tag = item };
                    t.BackColor = Color.Red;
                    t.ForeColor = Color.White;
                    listView1.Items.Add(t);
                }
                else
                {
                    listView1.Items.Add(new ListViewItem(new string[] { item.Tag.Name }) { Tag = item });
                }

            }
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

        private void toolStripButton2_Click(object sender, EventArgs e)
        {

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
            updateInfosList();
            selected = null;
        }

        private void setTagToolStripMenuItem_EnabledChanged(object sender, EventArgs e)
        {

        }

        private void setTagToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            setTagToolStripMenuItem.DropDownItems.Clear();
            foreach (var item in tags)
            {
                var c = new ToolStripMenuItem(item.Name) { Tag = item };
                setTagToolStripMenuItem.DropDownItems.Add(c);
                c.Click += (s, ee) =>
                {
                    selected.Tag = ((s as ToolStripMenuItem).Tag as Tag);
                    updateInfosList();
                };
            }
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



        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            Capturer capt = new Capturer();
            capt.Show();
        }


        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TagEditDialog ted = new TagEditDialog();
            ted.Set("tag1");
            if (ted.ShowDialog() != DialogResult.OK) return;

            if (tags.Any(z => z.Name == ted.Value))
            {
                MessageBox.Show("duplicate tag.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            tags.Add(new Tag() { Name = ted.Value });
            updateTagsList();
        }

        Tag AddOrGetTag(string name)
        {
            var f = tags.FirstOrDefault(z => z.Name == name);
            if (f == null)
            {
                f = new Soba.Tag() { Name = name };
                tags.Add(f);
            }
            return f;
        }

        private void deleteToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (listView3.SelectedItems.Count == 0) return;
            var tag = (listView3.SelectedItems[0].Tag as Tag);
            tags.Remove(tag);
            foreach (var item in Items)
            {
                foreach (var iitem in item.Infos)
                {
                    if (iitem.Tag == tag)
                    {
                        iitem.Tag = null;
                    }
                }
            }
            updateTagsList();
        }

        private void renameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView3.SelectedItems.Count == 0) return;
            var tag = (listView3.SelectedItems[0].Tag as Tag);

            TagEditDialog ted = new TagEditDialog();
            ted.Set(tag.Name);
            ted.ShowDialog();

            if (tags.Any(z => z.Name == ted.Value))
            {
                MessageBox.Show("duplicate tag.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            tag.Name = ted.Value;
            updateTagsList();
        }


        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != DialogResult.OK) return;
            var f = new FileInfo(ofd.FileName);
            loadDir(f.DirectoryName);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "xml|*.xml";
            if (sfd.ShowDialog() != DialogResult.OK) return;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\"?>");
            sb.AppendLine("<root>");
            sb.AppendLine("<tags>");
            foreach (var item in tags)
            {
                if (string.IsNullOrEmpty(item.Name)) throw new Exception("tag empty string");
                sb.AppendLine($"<tag name=\"{item.Name}\"/>");
            }
            sb.AppendLine("</tags>");
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
                    string tagn = info.Tag == null ? string.Empty : info.Tag.Name;
                    sb.AppendLine($"<info x=\"{info.Rect.X}\" y=\"{info.Rect.Y}\" w=\"{info.Rect.Width}\" h=\"{info.Rect.Height}\" name=\"{tagn}\"/>");
                }
                sb.AppendLine("</item>");
            }
            sb.AppendLine("</root>");
            File.WriteAllText(sfd.FileName, sb.ToString());
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "xml|*.xml";
            if (ofd.ShowDialog() != DialogResult.OK) return;
            if (!ofd.FileName.EndsWith("xml"))
            {
                MessageBox.Show("only xml supported.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                var doc = XDocument.Load(ofd.FileName);
                Items.Clear();
                tags.Clear();
                foreach (var item in doc.Descendants("tag"))
                {
                    var tag = item.Attribute("name").Value;
                    tags.Add(new Tag() { Name = tag });
                }
                updateTagsList();

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

                        var tag = info.Attribute("name").Value;
                        Items.Last().Infos.Add(new RectInfo() { Tag = tags.FirstOrDefault(z => z.Name == tag), Rect = new Rectangle(xx, yy, ww, hh) });

                    }
                }
                info = "count: " + Items.Count;
                Text = "items in dataset: " + Items.Count;
                loadDir(new FileInfo(ofd.FileName).DirectoryName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void wIDEToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WiderExportDialog wed = new WiderExportDialog();
            wed.Init(Items.ToArray(), tags.ToArray());
            wed.ShowDialog();

        }

        private void clearToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Items.Clear();
            info = "count: " + Items.Count;
            Text = "items in dataset: " + Items.Count;
        }

        public void FitToPoints(PictureBox pb, PointF[] points)
        {
            var maxx = points.Max(z => z.X);
            var minx = points.Min(z => z.X);
            var maxy = points.Max(z => z.Y);
            var miny = points.Min(z => z.Y);

            var w = pb.Width;
            var h = pb.Height;

            var dx = maxx - minx;
            var kx = w / dx;
            var dy = maxy - miny;
            var ky = h / dy;

            var oz = zoom;
            var sz1 = new Size((int)(dx * kx), (int)(dy * kx));
            var sz2 = new Size((int)(dx * ky), (int)(dy * ky));
            zoom = kx;
            if (sz1.Width > w || sz1.Height > h) zoom = ky;

            var x = dx / 2 + minx;
            var y = dy / 2 + miny;


            sx = (w / 2f) / zoom - x;
            sy = -((h / 2f) / zoom + y);

            var test = Transform(new PointF(x, y));

        }

        void fitAll()
        {
            if (crnt != null)
                FitToPoints(pictureBox1, new PointF[] { new PointF(0, 0), new PointF(crnt.Width, 0), new PointF(0, -crnt.Height) });
        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            fitAll();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            Fetcher ff = new Fetcher();
            ff.Show();
        }


        bool autoFit = true;

        private void toolStripButton2_CheckedChanged(object sender, EventArgs e)
        {
            autoFit = toolStripButton2.Checked;
            if (autoFit)
            {
                fitAll();
                toolStripButton2.BackColor = Color.LightGreen;
            }
            else
            {
                toolStripButton2.BackColor = Color.Transparent;
            }
        }

        private void openToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count == 0) return;
            var tag = listView2.SelectedItems[0].Tag;
            if (tag is FileInfo fin)
            {
                Process.Start(fin.FullName);
            }
            if (tag is DirectoryInfo din)
            {
                Process.Start(din.FullName);
            }
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            grabcut g = new grabcut();
            g.Show();
        }

        private void wIDERToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "WIDER inage list (*.txt)|*.txt";
            if (ofd.ShowDialog() != DialogResult.OK) return;
            Items.Clear();
            tags.Clear();
            var fin = new FileInfo(ofd.FileName);
            var anp = Path.Combine(fin.Directory.FullName, "annotations");
            var imp = Path.Combine(fin.Directory.FullName, "images");
            foreach (var line in File.ReadLines(ofd.FileName))
            {
                var aa = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
                var ap = Path.Combine(anp, aa[1]);
                var doc1 = XDocument.Load(ap);
                var dsi = new DataSetItem() { Path = Path.Combine(imp, aa[0]).Replace("/","\\") };
                Items.Add(dsi);
                foreach (var item in doc1.Descendants("object"))
                {
                    var tag = AddOrGetTag(item.Element("name").Value);
                    var bb = item.Element("bndbox");

                    var rect = new Rectangle();
                    rect.X = int.Parse(bb.Element("xmin").Value);
                    rect.Y = int.Parse(bb.Element("ymin").Value);
                    rect.Width = int.Parse(bb.Element("xmax").Value) - rect.X;
                    rect.Height = int.Parse(bb.Element("ymax").Value) - rect.Y;
                    
                    rect.Y *= -1;
                    dsi.Infos.Add(new RectInfo() { Tag = tag, Rect = rect });
                }
            }
            updateTagsList();
            updateInfosList();
            updateItemsList();
        }
        void updateItemsList()
        {
            listView2.Items.Clear();
            foreach (var item in Items)
            {
                listView2.Items.Add(new ListViewItem(new string[] { Path.GetFileName(item.Path) }) { Tag = new FileInfo(item.Path) });
            }
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
                            Tag = tags.Count > 0 ? tags[0] : null
                        });
                    }
                }
                updateInfosList();
                drag = false;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            redraw();
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

    public class DataSetItem
    {
        public string Path;
        public List<RectInfo> Infos = new List<RectInfo>();
    }
}

