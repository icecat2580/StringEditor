using StringDiagram.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace StringDiagram.Models
{
        public class CtTopImageInfo
    {
        public int CTIndex { get; set; }
        public Image Image { get; set; }
        public ImageSource   ImageSource { get; set; }
        public TopIconKind TopIconKind { get; set; }=TopIconKind.Default;
    }
}
