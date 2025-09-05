using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace AccesosLauncher
{
    public class AppItem
    {
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";
        public string RelativeDirectory { get; set; } = "";
        public ImageSource? Icon { get; set; }
    }
}
