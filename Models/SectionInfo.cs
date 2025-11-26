using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StringDiagram.Models
{
    public class SectionInfo
    {
        public double Length;                          // 段长
        public double OuterDiameterOfReelEnd;          // 滚筒端外径
        public double InnerDiameterOfReelEnd;          // 滚筒端内径
        public double OuterDiameterOfFreeEnd;          // 自由端外径
        public double InnerDiameterOfFreeEnd;          // 自由端内径
        public List<FreeWeldInfo> freeWeldInfos= new List<FreeWeldInfo>();
        /// <summary>
        /// 该分段内部的自由焊缝位置（从本段上端向下的距离，单位：米）
        /// </summary>
        public List<double> FreeWeldPositions { get; } = new List<double>();

        public SectionInfo(double length, double outerDiameterOfReelEnd, double innerDiameterOfReelEnd, double outerDiameterOfFreeEnd, double innerDiameterOfFreeEnd)
        {
            Length = length;
            OuterDiameterOfReelEnd = outerDiameterOfReelEnd;
            InnerDiameterOfReelEnd = innerDiameterOfReelEnd;
            OuterDiameterOfFreeEnd = outerDiameterOfFreeEnd;
            InnerDiameterOfFreeEnd = innerDiameterOfFreeEnd;
        }
        public SectionInfo()
        {
            
        }
    }
}
