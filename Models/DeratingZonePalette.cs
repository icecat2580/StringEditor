using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace StringDiagram.Models
{
    /// <summary>
    /// 降额段颜色：默认按插入顺序从调色板循环取色（红→橙→黄→绿→青→蓝→紫）。
    /// RGB 来自 <see cref="ColorsByIndex"/>；透明度由每条记录的 <see cref="DeratingZoneItem.ZoneValue"/> 决定。
    /// </summary>
    public static class DeratingZonePalette
    {
        private static readonly Color[] DefaultRgb = new[]
        {
            Color.FromRgb(0xE5, 0x39, 0x35), // 红
            Color.FromRgb(0xFB, 0x8C, 0x00), // 橙
            Color.FromRgb(0xFB, 0xC0, 0x02), // 黄
            Color.FromRgb(0x43, 0xA0, 0x47), // 绿
            Color.FromRgb(0x00, 0xAC, 0xC1), // 青
            Color.FromRgb(0x1E, 0x88, 0xE5), // 蓝
            Color.FromRgb(0x8E, 0x24, 0xAA), // 紫
        };

        /// <summary>顺序与默认彩虹一致，键为 0..Count-1。</summary>
        public static readonly Dictionary<int, Color> ColorsByIndex = BuildDictionary();

        private static Dictionary<int, Color> BuildDictionary()
        {
            var d = new Dictionary<int, Color>();
            for (int i = 0; i < DefaultRgb.Length; i++)
                d[i] = DefaultRgb[i];
            return d;
        }

        /// <summary>按插入顺序下标取 RGB（循环）。</summary>
        public static Color GetRgb(int insertionIndex)
        {
            if (DefaultRgb.Length == 0)
                return Colors.Red;
            int i = insertionIndex % DefaultRgb.Length;
            if (i < 0)
                i += DefaultRgb.Length;
            return DefaultRgb[i];
        }

        /// <summary>
        /// 组合 RGB（来自调色板）与降额值（0～1）得到带 Alpha 的颜色。
        /// </summary>
        public static Color GetZoneColor(int insertionIndex, double zoneValue)
        {
            double z = Math.Max(0, Math.Min(1, zoneValue));
            byte a = (byte)Math.Round(255.0 * z);
            if (a < 1 && z > 0)
                a = 1;
            Color rgb = GetRgb(insertionIndex);
            return Color.FromArgb(a, rgb.R, rgb.G, rgb.B);
        }
    }
}
