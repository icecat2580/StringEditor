namespace StringDiagram.Models
{
    /// <summary>
    /// 降额区间（从滚筒端起算的轴向位置，单位与管柱长度一致，通常为 m）。
    /// </summary>
    public sealed class DeratingZoneItem
    {
        public double StartPos { get; set; }
        public double EndPos { get; set; }
        /// <summary>降额强度 0～1，用于控制填充不透明度。</summary>
        public double ZoneValue { get; set; }
    }
}
