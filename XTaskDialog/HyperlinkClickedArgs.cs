using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XTaskDialog
{
    public class HyperlinkClickedArgs : EventArgs
    {
        public HyperlinkClickedArgs(string link)
        {
            this.Link = link;
        }

        public string Link { get; }
    }
}
