using System;
using System.Collections.Generic;

namespace Soba
{
    public class DataSetItem
    {
        public DataSetItem(Dataset p)
        {
            Parent = p;
        }
        public Dataset Parent { get; private set; }
        public string Path;
        public List<RectInfo> Infos = new List<RectInfo>();
        public void AddRectInfo(RectInfo r)
        {
            r.Parent = this;
            Infos.Add(r);
        }

        public virtual void Refresh()
        {
            
        }
    }
}

