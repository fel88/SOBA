using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Soba
{
    public class WIDERDataSetItem : DataSetItem
    {
        public WIDERDataSetItem(Dataset p) : base(p) { }

        public string AnnotationXmlPath;
        public override void Refresh()
        {
            if (Parent.ReadOnly) return;
            File.Copy(AnnotationXmlPath, AnnotationXmlPath + ".bak", true);
            var doc = XDocument.Load(AnnotationXmlPath);
            var root = doc.Element("annotation");
            var objs = root.Elements("object").ToArray();
            foreach (var item in objs)
            {
                item.Remove();
            }
            
            var koef = 1;
            foreach (var info in Infos)
            {
                StringBuilder sb = new StringBuilder();                
                sb.AppendLine("<object>");
                sb.AppendLine($"<name>{info.Tag.Name}</name>");
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
                var elem = XElement.Parse(sb.ToString());
                root.Add(elem);
            }            
            
            doc.Save(AnnotationXmlPath);
            base.Refresh();
        }
    }
}

