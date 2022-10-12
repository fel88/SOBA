using System;
using System.Collections.Generic;
using System.Linq;

namespace Soba
{
    public class Dataset
    {
        public string Name;
        public List<Tag> Tags = new List<Tag>();
        public List<DataSetItem> Items = new List<DataSetItem>();

        public bool ReadOnly { get; set; }

        public Tag AddOrGetTag(string name)
        {
            var f = Tags.FirstOrDefault(z => z.Name == name);
            if (f == null)
            {
                f = new Soba.Tag() { Name = name };
                Tags.Add(f);
            }
            return f;
        }
        public void AddItem(DataSetItem dsi)
        {
            Items.Add(dsi);
        }
    }
}

