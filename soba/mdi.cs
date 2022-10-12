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
using System.Xml.Linq;

namespace Soba
{
    public partial class mdi : Form
    {
        public mdi()
        {
            InitializeComponent();
        }

        private void wIDERToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "WIDER inage list (*.txt)|*.txt";
            if (ofd.ShowDialog() != DialogResult.OK) return;
            WIDERDataset dataset = new WIDERDataset() { ReadOnly = true };
            dataset.Name = ofd.FileName;
            var fin = new FileInfo(ofd.FileName);
            var anp = Path.Combine(fin.Directory.FullName, "annotations");
            var imp = Path.Combine(fin.Directory.FullName, "images");
            foreach (var line in File.ReadLines(ofd.FileName))
            {
                var aa = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
                var ap = Path.Combine(anp, aa[1]);
                var doc1 = XDocument.Load(ap);
                var dsi = new WIDERDataSetItem(dataset)
                {
                    Path = Path.Combine(imp, aa[0]).Replace("/", "\\"),
                    AnnotationXmlPath = ap
                };
                dataset.AddItem(dsi);
                foreach (var item in doc1.Descendants("object"))
                {
                    var tag = dataset.AddOrGetTag(item.Element("name").Value);
                    var bb = item.Element("bndbox");

                    var rect = new Rectangle();
                    rect.X = int.Parse(bb.Element("xmin").Value);
                    rect.Y = int.Parse(bb.Element("ymin").Value);
                    rect.Width = int.Parse(bb.Element("xmax").Value) - rect.X;
                    rect.Height = int.Parse(bb.Element("ymax").Value) - rect.Y;

                    rect.Y *= -1;
                    dsi.AddRectInfo(new RectInfo() { Tag = tag, Rect = rect });
                }
            }

            Form1 f = new Form1();
            f.Init(dataset);
            f.MdiParent = this;
            f.Show();
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form1 f = new Form1();
            f.Init(new Dataset() { Name = "new dataset1" });
            f.MdiParent = this;
            f.Show();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox1 b = new AboutBox1();
            b.ShowDialog();
        }
    }
}
