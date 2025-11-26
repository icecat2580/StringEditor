using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StringDiagram.Models
{
    public class FreeWeldInfo
    {
        /// <summary>
        /// 连续管索引
        /// </summary>
        public int CTindex { get; set; }
        /// <summary>
        /// 分段索引
        /// </summary>
        public int SectionIndex { get; set; }
        /// <summary>
        /// 连续管上端往下指定深度处
        /// </summary>
        public double Position { get; set; }
    }
}
