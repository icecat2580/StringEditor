using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace StringDiagram.Models
{
    public class SectionRegion
    {
        public int CTIndex { get; set; }
        public int SectionIndex { get; set; }
        public Rect Bounds { get; set; }                  // 在 Root 坐标系中
        public System.Windows.Shapes.Polygon LeftWall { get; set; }
        public System.Windows.Shapes.Polygon RightWall { get; set; }
        public System.Windows.Shapes.Shape InsideShape { get; set; }

        public Brush InsideOriginalBrush { get; set; }

    }
}
