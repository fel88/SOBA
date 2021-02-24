using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace annotator1
{
    public partial class WiderExportDialog : Form
    {
        public WiderExportDialog()
        {
            InitializeComponent();
        }

        public void Init(DataSetItem[] items, Tag[] tags)
        {
            this.items = items;
            this.tags = tags;
            foreach (var item in tags)
            {
                aliases.Add(item.Name, "face");
            }
            updateAliasList();
        }
        Tag[] tags;
        DataSetItem[] items;
        Dictionary<string, string> aliases = new Dictionary<string, string>();
        public void Export()
        {
            Directory.CreateDirectory("annotations");
            Directory.CreateDirectory("images");
            StringBuilder sb2 = new StringBuilder();

            List<string> dirs = new List<string>();
            Dictionary<string, string> alias = new Dictionary<string, string>();
            foreach (var item in items)
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
                float koef = 1;
                if (checkBox1.Checked)
                {
                    koef = (float)(normWidth) / mat.Width;
                    mat = mat.Resize(new OpenCvSharp.Size(normWidth, koef * mat.Height));
                }

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
                sb.AppendLine($"<width>{mat.Width}</width>");
                sb.AppendLine($"<height>{mat.Height}</height>");
                sb.AppendLine($"<depth>{3}</depth>");
                sb.AppendLine($"</size>");
                sb.AppendLine($"<segmented>{0}</segmented>");
                foreach (var info in item.Infos)
                {
                    sb.AppendLine("<object>");
                    string cls = "face";
                    if (info.Tag != null)
                    {
                        if (aliases.ContainsKey(info.Tag.Name))
                        {
                            cls = aliases[info.Tag.Name];
                        }
                    }
                    sb.AppendLine($"<name>{cls}</name>");
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
            var fl = new FileInfo("img_list.txt");
            MessageBox.Show($"Exported to: {fl.FullName}", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            if (sfd.ShowDialog() != DialogResult.OK) return;            
            //Export();
        }

        int normWidth = 1024;
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                normWidth = int.Parse(textBox1.Text);
            }
            catch (Exception ex)
            {

            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            foreach (var item in tags)
            {
                aliases[item.Name] = item.Name;
            }
            updateAliasList();
        }

        void updateAliasList()
        {
            listView1.Items.Clear();
            foreach (var item in tags)
            {                
                listView1.Items.Add(new ListViewItem(new string[] { item.Name, aliases[item.Name] }) { });
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            foreach (var item in tags)
            {
                aliases[item.Name] = textBox2.Text;
            }
            updateAliasList();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;
            var txt = listView1.SelectedItems[0].SubItems[0].Text;
            aliases[txt] = textBox3.Text;
            updateAliasList();
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            Export();
        }
    }
}
