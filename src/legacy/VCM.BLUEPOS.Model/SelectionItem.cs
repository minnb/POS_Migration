using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model
{
    public class SelectionItem
    {
        public string Value { get; set; }
        public string Name { get; set; }

        public SelectionItem()
        {
        }
        public SelectionItem(string value, string name)
        {
            Value = value;
            Name = name;
        }
    }
}
