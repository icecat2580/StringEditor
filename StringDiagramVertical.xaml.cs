using StringDiagram.Enums;
using StringDiagram.Extensions;
using StringDiagram.Interfaces;
using StringDiagram.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace StringDiagram
{
    /// <summary>
    /// StringDiagramVertical.xaml 的交互逻辑
    /// </summary>
    internal sealed partial class StringDiagramVertical : UserControl, IStringEditor
    {

        private readonly List<List<SectionInfo>> CTs = new List<List<SectionInfo>>()
        {
        };
        private readonly List<SectionRegion> _sectionRegions = new List<SectionRegion>();
        private readonly List<CtTopImageInfo> _ctTopImages = new List<CtTopImageInfo>();
        private readonly HashSet<int> _zoneModeCTIndices = new HashSet<int>();
        private readonly List<DeratingZoneItem> _deratingZones = new List<DeratingZoneItem>();
        private sealed class ZoneCandidate
        {
            public int Index;
            public double Start;
            public double End;
            public double Value;
        }

        private sealed class ResolvedZoneSpan
        {
            public int ZoneIndex;
            public double ZoneValue;
            public double Start;
            public double End;
        }
        private sealed class ZoneVisualTag
        {
            public int ZoneIndex;
            public bool IsOverlay;
        }
        private sealed class CtMetric
        {
            public int CTIndex;
            public double StartMeter;
            public double LengthMeter;
        }
        public event Action<int, int> OnSelectedSectionhandler;
        public event Action<int, int> OnSelectedZonehandler;
        // 纵向缩放
        public double MeterToPixel { get; set; } = 0.5 * 10000;

        //当前选中的CT索引
        private int _selectedCTIndex = -1;
        //当前选中的分段索引
        private int _selectedSectionIndex = -1;
        // 当前选中的降额索引（-1 表示无）
        private int _selectedZoneIndex = -1;
        private static readonly Brush SelectedZoneBrush = new SolidColorBrush(Colors.Purple);

        private const double TopIconWidth = 24;
        private const double TopIconHeight = 24;

        // 选中分段的左侧引导线与深度文字
        private System.Windows.Shapes.Line _selectedTopGuideLine;
        private System.Windows.Shapes.Line _selectedBottomGuideLine;
        private TextBlock _selectedTopDepthText;
        private TextBlock _selectedBottomDepthText;
        private readonly List<System.Windows.Shapes.Line> _selectedFreeWeldMarks = new List<System.Windows.Shapes.Line>();
        private readonly List<Shape> _selectedZoneOverlays = new List<Shape>();


        // 模板
        private readonly DrawingGroup _reelTemplate;
        private readonly DrawingGroup _connectorTemplate;

        // 实际使用的 ImageSource（每次换色后重新生成）
        private ImageSource _reelIconSource;
        private ImageSource _connectorIconSource;
        public StringDiagramVertical(List<List<SectionInfo>> CTs) : this()
        {
            this.CTs = CTs;
        }
        public StringDiagramVertical()
        {
            InitializeComponent();
            Root.MouseLeftButtonDown += Root_MouseLeftButtonDown;
            Zone.MouseLeftButtonDown += Zone_MouseLeftButtonDown;
            _reelTemplate = ((DrawingGroup)FindResource("Drawing.滚筒")).CloneCurrentValue();
            _connectorTemplate = ((DrawingGroup)FindResource("Drawing.连接器")).CloneCurrentValue();
            UpdateReelIcon(ReelBrush ?? Brushes.Black);
            UpdateConnectorIcon(ConnectorBrush ?? Brushes.Black);

        }



        #region 依赖属性

        #region MinWallFracTotal


        public double MinWallFracTotal
        {
            get { return (double)GetValue(MinWallFracTotalProperty); }
            set { SetValue(MinWallFracTotalProperty, value); }
        }

        public static readonly DependencyProperty MinWallFracTotalProperty =
            DependencyProperty.Register(
                "MinWallFracTotal", 
                typeof(double), 
                typeof(StringDiagramVertical),
                new PropertyMetadata(0.5));


        #endregion

        #region MaxWallFracTotal


        public double MaxWallFracTotal
        {
            get { return (double)GetValue(MaxWallFracTotalProperty); }
            set { SetValue(MaxWallFracTotalProperty, value); }
        }

        public static readonly DependencyProperty MaxWallFracTotalProperty =
            DependencyProperty.Register(
                "MaxWallFracTotal",
                typeof(double), 
                typeof(StringDiagramVertical), 
                new PropertyMetadata(0.7));


        #endregion

        #region SectionSelectedBrush

        public static readonly DependencyProperty SectionSelectedBrushProperty =
            DependencyProperty.Register(
                nameof(SectionSelectedBrush),
                typeof(Brush),
                typeof(StringDiagramVertical),
                new PropertyMetadata(new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0xFF, 0xE0))));

        public Brush SectionSelectedBrush
        {
            get => (Brush)GetValue(SectionSelectedBrushProperty);
            set => SetValue(SectionSelectedBrushProperty, value);
        }

        #endregion

        #region SectionBrush

        public static readonly DependencyProperty SectionBrushProperty =
            DependencyProperty.Register(
                nameof(SectionBrush),
                typeof(Brush),
                typeof(StringDiagramVertical),
                new PropertyMetadata(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0F0E3"))));

        public Brush SectionBrush
        {
            get => (Brush)GetValue(SectionBrushProperty);
            set => SetValue(SectionBrushProperty, value);
        }

        #endregion

        #region ReelBrush

        public static readonly DependencyProperty ReelBrushProperty =
            DependencyProperty.Register(
                nameof(ReelBrush),
                typeof(Brush),
                typeof(StringDiagramVertical),
                new PropertyMetadata(Brushes.Green, OnReelBrushChanged));

        private static void OnReelBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (StringDiagramVertical)d;
            if (c != null)
            {
                var brush = e.NewValue as Brush;
                c.UpdateReelIcon(brush);
                c.RedrawSections();
            }
        }


        public Brush ReelBrush
        {
            get => (Brush)GetValue(ReelBrushProperty);
            set => SetValue(ReelBrushProperty, value);
        }

        #endregion

        #region ConnectorBrush


        public Brush ConnectorBrush
        {
            get { return (Brush)GetValue(ConnectorBrushProperty); }
            set { SetValue(ConnectorBrushProperty, value); }
        }

        public static readonly DependencyProperty ConnectorBrushProperty =
            DependencyProperty.Register("" +
                "ConnectorBrush",
                typeof(Brush),
                typeof(StringDiagramVertical),
                new PropertyMetadata(Brushes.Red, OnConnectorBrushChanged));
        private static void OnConnectorBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (StringDiagramVertical)d;
            if (c != null)
            {
                var brush = e.NewValue as Brush;
                c.UpdateConnectorIcon(brush);
                c.RedrawSections();
            }
        }

        #endregion

        #region WeldBrush

        public static readonly DependencyProperty WeldBrushProperty =
            DependencyProperty.Register(
                nameof(WeldBrush),
                typeof(Brush),
                typeof(StringDiagramVertical),
                new PropertyMetadata(Brushes.Black));

        public Brush WeldBrush
        {
            get => (Brush)GetValue(WeldBrushProperty);
            set => SetValue(WeldBrushProperty, value);
        }

        #endregion

        #region FreeWeldBrush


        public Brush FreeWeldBrush
        {
            get { return (Brush)GetValue(FreeWeldBrushProperty); }
            set { SetValue(FreeWeldBrushProperty, value); }
        }

        public static readonly DependencyProperty FreeWeldBrushProperty =
            DependencyProperty.Register(
                "FreeWeldBrush",
                typeof(Brush), 
                typeof(StringDiagramVertical),
                new PropertyMetadata(Brushes.Black));


        #endregion

        #region InsideBrush

        public static readonly DependencyProperty InsideBrushProperty =
            DependencyProperty.Register(
                nameof(InsideBrush),
                typeof(Brush),
                typeof(StringDiagramVertical),
                new PropertyMetadata(Brushes.Transparent));

        public Brush InsideBrush
        {
            get => (Brush)GetValue(InsideBrushProperty);
            set => SetValue(InsideBrushProperty, value);
        }

        #endregion

        #region ContainerBrush

        public static readonly DependencyProperty ContainerBrushProperty =
            DependencyProperty.Register(
                nameof(ContainerBrush),
                typeof(Brush),
                typeof(StringDiagramVertical),
                new PropertyMetadata(Brushes.Transparent));

        public Brush ContainerBrush
        {
            get => (Brush)GetValue(ContainerBrushProperty);
            set => SetValue(ContainerBrushProperty, value);
        }

        #endregion

        #region LineBrush

        public static readonly DependencyProperty LineBrushProperty =
            DependencyProperty.Register(
                nameof(LineBrush),
                typeof(Brush),
                typeof(StringDiagramVertical),
                new PropertyMetadata(Brushes.Black));

        public Brush LineBrush
        {
            get => (Brush)GetValue(LineBrushProperty);
            set => SetValue(LineBrushProperty, value);
        }

        #endregion

        #region LineWeight

        public static readonly DependencyProperty LineWeightProperty =
            DependencyProperty.Register(
                nameof(LineWeight),
                typeof(double),
                typeof(StringDiagramVertical),
                new PropertyMetadata(1D));

        public double LineWeight
        {
            get => (double)GetValue(LineWeightProperty);
            set => SetValue(LineWeightProperty, value);
        }

        #endregion

        #region FontBrush

        public static readonly DependencyProperty FontBrushProperty =
            DependencyProperty.Register(
                nameof(FontBrush),
                typeof(Brush),
                typeof(StringDiagramVertical),
                new PropertyMetadata(Brushes.Black));

        public Brush FontBrush
        {
            get => (Brush)GetValue(FontBrushProperty);
            set => SetValue(FontBrushProperty, value);
        }

        #endregion

        #region ConTainerHeight
        public double ConTainerHeight
        {
            get { return (double)GetValue(ConTainerHeightProperty); }
            set { SetValue(ConTainerHeightProperty, value); }
        }

        public static readonly DependencyProperty ConTainerHeightProperty =
            DependencyProperty.Register
                ("ConTainerHeight",
                typeof(double),
                typeof(StringDiagramVertical),
                new PropertyMetadata(600D, OnHeightChanged));

        private static void OnHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (StringDiagramVertical)d;
            if (c != null)
            {
                c.Height = (double)e.NewValue;
                c.Root.Height = (double)e.NewValue;
                c.ruler.Height = (double)e.NewValue;
                c.RedrawSections();
            }
        }
        #endregion

        #region FontSize


        public double fontSize
        {
            get { return (double)GetValue(fontSizeProperty); }
            set { SetValue(fontSizeProperty, value); }
        }

        public static readonly DependencyProperty fontSizeProperty =
            DependencyProperty.Register
                ("fontSize",
                typeof(double),
                typeof(StringDiagramVertical),
                new PropertyMetadata(10D));


        #endregion

        #region DisplayUnit


        public string DisplayUnit
        {
            get { return (string)GetValue(DisplayUnitProperty); }
            set { SetValue(DisplayUnitProperty, value); }
        }

        public static readonly DependencyProperty DisplayUnitProperty =
            DependencyProperty.Register(
                "DisplayUnit",
                typeof(string),
                typeof(StringDiagramVertical),
                new PropertyMetadata("m"));
        //private static void OnDisPlayUnitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        //{
        //    if (d is StringDiagramVertical uc)
        //    {
        //        uc.RedrawSections();
        //    }
        //}

        #endregion

        #region EnableSelectedSection
        public bool EnableSelectedSection
        {
            get { return (bool)GetValue(EnableSelectedSectionProperty); }
            set { SetValue(EnableSelectedSectionProperty, value); }
        }

        public static readonly DependencyProperty EnableSelectedSectionProperty =
            DependencyProperty.Register(
                "EnableSelectedSection",
                typeof(bool),
                typeof(StringDiagramVertical),
                new PropertyMetadata(true, OnEnableSelectedSectionChanged));

        private static void OnEnableSelectedSectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (StringDiagramVertical)d;
            if (c == null) return;

            bool enabled = (bool)e.NewValue;
            if (!enabled)
            {
                // 禁用选中功能时，顺便清空当前选中
                c.SetSelectedSection(-1, -1, false);
            }

        }

        #endregion

        #region RulerMode
        public static readonly DependencyProperty RulerModeProperty =
            DependencyProperty.Register(
            nameof(RulerMode),
            typeof(RulerMode),
            typeof(StringDiagramVertical),
            new PropertyMetadata(RulerMode.Global, OnRulerModeChanged));

        public RulerMode RulerMode
        {
            get => (RulerMode)GetValue(RulerModeProperty);
            set => SetValue(RulerModeProperty, value);
        }

        private static void OnRulerModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (StringDiagramVertical)d;
            c?.RedrawSections();   // 模式切换时重画
        }

        #endregion


        #endregion


        #region Finish
        /// <summary>
        /// index: 插入位置（0 在最上面）
        /// length: 段长
        /// OuterDiameterOfReelEnd: 上端外径
        /// InnerDiameterOfReelEnd: 上端内径
        /// OuterDiameterOfFreeEnd: 下端外径
        /// InnerDiameterOfFreeEnd: 下端内径
        /// </summary>
        public void InsertSection(
            int CTindex,
            int Sectionindex,
            double length,
            double OuterDiameterOfReelEnd,
            double InnerDiameterOfReelEnd,
            double OuterDiameterOfFreeEnd,
            double InnerDiameterOfFreeEnd)
        {
            var sec = new SectionInfo
            {
                Length = length,
                OuterDiameterOfReelEnd = OuterDiameterOfReelEnd,
                InnerDiameterOfReelEnd = InnerDiameterOfReelEnd,
                OuterDiameterOfFreeEnd = OuterDiameterOfFreeEnd,
                InnerDiameterOfFreeEnd = InnerDiameterOfFreeEnd
            };
            if (CTindex < 0 || CTindex >= CTs.Count)
            {
                throw new ArgumentOutOfRangeException(
                 nameof(Sectionindex),
                 $"Sectionindex 超出范围，当前 Section 数量为 {CTs[CTindex].Count}。");
            }
            if (Sectionindex < 0 || Sectionindex > CTs[CTindex].Count)
                Sectionindex = CTs[CTindex].Count;

            CTs[CTindex].Insert(Sectionindex, sec);
            RedrawSections(); //重画
        }

        /// <summary>
        /// 清空某一根连续管的所有分段
        /// </summary>
        public void ClearSection(int CTindex)
        {
            if (CTindex < 0 || CTindex >= CTs.Count)
                return;

            CTs[CTindex].Clear();
            RedrawSections();
        }

        /// <summary>
        /// 根据索引删除某一根连续管中的一个分段
        /// </summary>
        /// <param name="CTindex">连续管索引</param>
        /// <param name="Sectionindex">分段索引</param>
        public void RemoveSection(int CTindex, int Sectionindex)
        {
            if (CTindex < 0 || CTindex >= CTs.Count) return;
            if (Sectionindex < 0 || Sectionindex >= CTs[CTindex].Count) return;

            CTs[CTindex].RemoveAt(Sectionindex);
            RedrawSections();
        }



        /// <summary>
        /// 从某个连续管上端开始向下多少米处增加一条自由焊缝
        /// </summary>
        /// <param name="CTindex">连续管索引</param>
        /// <param name="Postion">连续管上端往下多少米处</param>
        public void InsertWeld(int CTindex, double Postion)
        {
            if (CTindex < 0 || CTindex >= CTs.Count)
                return;

            var ct = CTs[CTindex];

            // 连续管总长
            double ctLength = 0;
            foreach (var s in ct) ctLength += s.Length;

            if (ctLength <= 0)
                return;

            const double eps = 1e-6;

            //焊缝刚好落在整根连续管的上下边界
            if (Postion <= eps || Postion >= ctLength - eps)
                return;

            // 超出范围
            if (Postion < 0 || Postion > ctLength)
                return;

            //寻找自由焊缝所处分段位置
            double acc = 0; // 从 CT 顶端累计长度
            for (int i = 0; i < ct.Count; i++)
            {
                var sec = ct[i];
                double nextAcc = acc + sec.Length;

                if (Postion <= nextAcc)
                {
                    // 落在当前 sec 里
                    double local = Postion - acc;   // 相对于本段上端的距离

                    // 再防一下刚好贴在本段上下边界的情况（很罕见，防浮点误差）
                    if (local <= eps || local >= sec.Length - eps)
                        return;

                    sec.FreeWeldPositions.Add(local);
                    sec.FreeWeldPositions.Sort();

                    RedrawSections();
                    return;
                }

                acc = nextAcc;
            }
        }


        /// <summary>
        /// 根据连续管索引删除对应连续管
        /// </summary>
        /// <param name="CTindex">连续管索引</param>
        public void RemoveCT(int ctIndex)
        {
            if (ctIndex < 0 || ctIndex >= CTs.Count)
                return;

            CTs.RemoveAt(ctIndex);
            if (ctIndex < _ctTopImages.Count)
                _ctTopImages.RemoveAt(ctIndex);

            _deratingZones.RemoveAll(z => z.CTIndex == ctIndex);
            foreach (var z in _deratingZones)
            {
                if (z.CTIndex > ctIndex)
                    z.CTIndex--;
            }

            var newModes = new HashSet<int>();
            foreach (int idx in _zoneModeCTIndices)
            {
                if (idx == ctIndex)
                    continue;
                newModes.Add(idx > ctIndex ? idx - 1 : idx);
            }
            _zoneModeCTIndices.Clear();
            foreach (int idx in newModes)
                _zoneModeCTIndices.Add(idx);

            if (_selectedZoneIndex >= 0)
                _selectedZoneIndex = -1;
            ClearZoneSelectionVisuals(false);

            // 重新修正一下 CTIndex
            for (int i = 0; i < _ctTopImages.Count; i++)
                _ctTopImages[i].CTIndex = i;

            RedrawSections();
        }


        /// <summary>
        /// 清除所有连续管
        /// </summary>
        public void ClearCT()
        {
            CTs.Clear();
            _ctTopImages.Clear();
            _deratingZones.Clear();
            _zoneModeCTIndices.Clear();
            ClearZoneSelectionVisuals(true);
            RedrawSections();
        }


        /// <summary>
        /// 再指定连续管索引后插入连续管
        /// </summary>
        /// <param name="CTIndex">索引</param>
        public void InsertCT(int ctIndex)
        {
            if (ctIndex < 0 || ctIndex > CTs.Count)
                ctIndex = CTs.Count;

            CTs.Insert(ctIndex, new List<SectionInfo>());
            _ctTopImages.Insert(ctIndex, new CtTopImageInfo
            {
                CTIndex = ctIndex,
                TopIconKind = TopIconKind.Default
            });

            foreach (var z in _deratingZones)
            {
                if (z.CTIndex >= ctIndex)
                    z.CTIndex++;
            }

            var newModes = new HashSet<int>();
            foreach (int idx in _zoneModeCTIndices)
                newModes.Add(idx >= ctIndex ? idx + 1 : idx);
            _zoneModeCTIndices.Clear();
            foreach (int idx in newModes)
                _zoneModeCTIndices.Add(idx);

            RedrawSections();
        }

        /// <summary>
        /// 追加连续管
        /// </summary>
        /// <param name="CTcount">追加连续管的数量，默认为1</param>
        public void AppendCT(int ctCount = 1)
        {
            for (int i = 0; i < ctCount; i++)
            {
                CTs.Add(new List<SectionInfo>());
                _ctTopImages.Add(new CtTopImageInfo
                {
                    CTIndex = CTs.Count - 1,
                    TopIconKind = TopIconKind.Default
                });
            }
            RedrawSections();
        }


        /// <summary>
        /// 设置某根连续管的连接器显示 / 隐藏
        /// </summary>
        /// <param name="ctIndex">连续管索引</param>
        /// <param name="visible">true 显示，false 隐藏</param>
        public void ShowConnector(int ctIndex, bool visible)
        {
            if (ctIndex < 0 || ctIndex >= CTs.Count)
                return;

            if (ctIndex >= _ctTopImages.Count)
                return;

            var info = _ctTopImages[ctIndex];

            if (visible)
            {
                // 显示连接器
                info.TopIconKind = TopIconKind.Connector;
            }
            else
            {
                // 只在当前就是连接器时才隐藏回 None
                if (info.TopIconKind == TopIconKind.Connector)
                    info.TopIconKind = TopIconKind.None;
            }

            RedrawSections();
        }

        /// <summary>
        /// 设置某根连续管的滚筒显示 / 隐藏
        /// </summary>
        /// <param name="ctIndex">连续管索引</param>
        /// <param name="visible">true 显示，false 隐藏</param>
        public void ShowReel(int ctIndex, bool visible)
        {
            if (ctIndex < 0 || ctIndex >= CTs.Count)
                return;

            if (ctIndex >= _ctTopImages.Count)
                return;

            var info = _ctTopImages[ctIndex];

            if (visible)
            {
                // 显示滚筒
                info.TopIconKind = TopIconKind.Reel;
            }
            else
            {
                // 只在当前就是滚筒时才隐藏回 None
                if (info.TopIconKind == TopIconKind.Reel)
                    info.TopIconKind = TopIconKind.None;
            }

            RedrawSections();
        }



        /// <summary>
        /// 设置显示单位
        /// </summary>
        /// <param name="unit"></param>
        public void SetDisplayUnit(string unit)
        {
            DisplayUnit = unit;
            Console.WriteLine($"{DateTime.Now}:单位设置成功,当前单位{unit}");
            RedrawSections();
            Console.WriteLine($"{DateTime.Now}:重绘完成,当前单位{unit}");
        }



        #region 导出

        /// <summary>
        /// 图片导出使用 VisualBrush 
        /// </summary>
        /// <param name="width">目标图片宽度（像素）</param>
        /// <param name="height">目标图片高度（像素）</param>
        /// <param name="imagePath">完整文件路径，例如 D:\img1.png</param>
        public void ExportImage(double width, double height, string imagePath)
        {
            //基本检查
            if (UC == null)
                throw new InvalidOperationException("UC 控件未初始化。");

            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException("width/height", "宽度和高度必须大于 0。");

            if (string.IsNullOrWhiteSpace(imagePath))
                throw new ArgumentException("imagePath 不能为空。", nameof(imagePath));

            //路径、目录、扩展名处理
            string directory = Path.GetDirectoryName(imagePath);
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentException("imagePath 必须包含目录。", nameof(imagePath));

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string ext = Path.GetExtension(imagePath);
            if (string.IsNullOrEmpty(ext))
                throw new ArgumentException("imagePath 必须包含文件扩展名，例如 .png、.jpg、.bmp。", nameof(imagePath));

            ext = ext.ToLowerInvariant();

            const int dpi = 96;

            // 选取要拍的元素，这里直接用整个 UserControl
            UIElement element = UC;

            // 确保布局完成，RenderSize 有值
            element.UpdateLayout();

            int originalWidth = (int)Math.Round(element.RenderSize.Width);
            int originalHeight = (int)Math.Round(element.RenderSize.Height);

            if (originalWidth <= 0 || originalHeight <= 0)
                throw new InvalidOperationException("控件的 RenderSize 为 0，无法导出图片。请确保控件已经显示并完成布局。");

            int targetWidth = (int)Math.Round(width);
            int targetHeight = (int)Math.Round(height);

            // 创建目标位图
            var rtb = new RenderTargetBitmap(targetWidth, targetHeight, dpi, dpi, PixelFormats.Pbgra32);

            // 计算等比缩放和居中偏移
            double scaleX = (double)targetWidth / originalWidth;
            double scaleY = (double)targetHeight / originalHeight;
            double scale = Math.Min(scaleX, scaleY);

            double offsetX = (targetWidth - originalWidth * scale) / 2.0;
            double offsetY = (targetHeight - originalHeight * scale) / 2.0;

            //用 VisualBrush“拍照”
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                // 白色背景
                dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, targetWidth, targetHeight));

                // 先平移，再缩放（顺序和你引用的 SaveToImage 一致）
                dc.PushTransform(new TranslateTransform(offsetX, offsetY));
                dc.PushTransform(new ScaleTransform(scale, scale));

                // 把当前控件作为 VisualBrush 画到目标矩形上
                dc.DrawRectangle(
                    new VisualBrush(element),
                    null,
                    new Rect(0, 0, originalWidth, originalHeight));

                // 还原变换
                dc.Pop(); // Scale
                dc.Pop(); // Translate
            }

            rtb.Render(dv);

            // 按扩展名选择编码器并保存
            BitmapEncoder encoder;
            switch (ext)
            {
                case ".jpg":
                case ".jpeg":
                    encoder = new JpegBitmapEncoder();
                    break;
                case ".bmp":
                    encoder = new BmpBitmapEncoder();
                    break;
                case ".png":
                default:
                    encoder = new PngBitmapEncoder();
                    break;
            }

            encoder.Frames.Add(BitmapFrame.Create(rtb));

            using (var fs = new FileStream(imagePath, FileMode.Create, FileAccess.Write))
            {
                encoder.Save(fs);
            }
        }

        #endregion





        public void GetDrawIngRegion(out Point leftTopPoint, out Point rightBottomPoint)
        {
            // 默认值
            leftTopPoint = new Point(0, 0);
            rightBottomPoint = new Point(0, 0);

            if (_sectionRegions == null || _sectionRegions.Count == 0)
                return;

            //  第一段
            var firstRegion = _sectionRegions.First();
            var leftPoly = firstRegion.LeftWall;

            if (leftPoly != null && leftPoly.Points != null && leftPoly.Points.Count > 0)
            {

                double minX = leftPoly.Points.Min(p => p.X);
                double minY = leftPoly.Points.Min(p => p.Y);
                leftTopPoint = new Point(minX, minY);
            }
            else
            {

                leftTopPoint = new Point(firstRegion.Bounds.Left, firstRegion.Bounds.Top);
            }

            // 2. 最后一段
            var lastRegion = _sectionRegions.Last();
            var rightPoly = lastRegion.RightWall;

            if (rightPoly != null && rightPoly.Points != null && rightPoly.Points.Count > 0)
            {

                double maxX = rightPoly.Points.Max(p => p.X);
                double maxY = rightPoly.Points.Max(p => p.Y);
                rightBottomPoint = new Point(maxX, maxY);
            }
            else
            {
                rightBottomPoint = new Point(lastRegion.Bounds.Right, lastRegion.Bounds.Bottom);
            }
        }
        //设置左边距
        public void SetLeftMargin(double LeftWidth)
        {
            return;
        }
        //设置右边距
        public void SetRightMargin(double RightWidth)
        {
            return;
        }
        //是否为调试模式
        public void SetDebugMode(bool DebugMode)
        {
            if (DebugMode)
            {
                ruler.Background = Brushes.Red;
                Root.Background = Brushes.Purple;
            }
            else
            {
                ruler.Background = Brushes.Transparent;
                Root.Background = Brushes.Transparent;
            }
        }

        /// <inheritdoc />
        public void SetZoneMode(int ctIndex, bool isZoneMode)
        {
            if (ctIndex >= 0 && ctIndex < CTs.Count)
            {
                if (isZoneMode)
                    _zoneModeCTIndices.Add(ctIndex);
                else
                {
                    _zoneModeCTIndices.Remove(ctIndex);
                    _deratingZones.RemoveAll(z => z.CTIndex == ctIndex);
                    ClearZoneSelectionVisuals(true);
                }
            }

            RedrawSections();
        }

        /// <inheritdoc />
        public void InsertZone(int ctIndex, int zoneIndex, double startPos, double endPos, double zoneValue)
        {
            if (ctIndex < 0 || ctIndex >= CTs.Count)
                return;
            if (!_zoneModeCTIndices.Contains(ctIndex))
                return;
            if (zoneIndex < 0)
                return;

            if (endPos < startPos)
            {
                double t = startPos;
                startPos = endPos;
                endPos = t;
            }

            if (startPos < 0 || endPos < 0)
                return;

            double ctLength = 0;
            foreach (var sec in CTs[ctIndex])
                ctLength += sec.Length;
            if (ctLength <= 0)
                return;

            startPos = Math.Max(0, Math.Min(ctLength, startPos));
            endPos = Math.Max(0, Math.Min(ctLength, endPos));
            if (endPos - startPos <= 1e-6)
                return;

            zoneValue = Math.Max(0, Math.Min(1, zoneValue));
            _deratingZones.Add(new DeratingZoneItem
            {
                CTIndex = ctIndex,
                ZoneIndex = zoneIndex,
                StartPos = startPos,
                EndPos = endPos,
                ZoneValue = zoneValue
            });
            RedrawSections();
        }

        /// <inheritdoc />
        public void SelectZone(int ctIndex, int zoneIndex)
        {
            if (ctIndex < 0 || ctIndex >= CTs.Count || zoneIndex < 0)
            {
                SetSelectedZoneByIndex(-1, false);
                return;
            }

            int hitIndex = -1;
            for (int i = _deratingZones.Count - 1; i >= 0; i--)
            {
                var z = _deratingZones[i];
                if (z.CTIndex == ctIndex &&
                    z.ZoneIndex == zoneIndex)
                {
                    hitIndex = i;
                    break;
                }
            }
            Console.WriteLine("执行选中函数，不触发委托");
            SetSelectedZoneByIndex(hitIndex, false);
        }

        /// <inheritdoc />
        public void ClearZone(int ctIndex)
        {
            if (ctIndex < 0 || ctIndex >= CTs.Count)
                return;

            _deratingZones.RemoveAll(z => z.CTIndex == ctIndex);
            _zoneModeCTIndices.Remove(ctIndex);
            if (_selectedZoneIndex >= 0)
                ClearZoneSelectionVisuals(true);
            RedrawSections();
        }

        private void SetSelectedZoneByIndex(int zoneIndex, bool raiseEvent)
        {
            if (zoneIndex < 0 || zoneIndex >= _deratingZones.Count)
            {
                ClearZoneSelectionVisuals(true);
                RedrawSections();
                return;
            }

            // 降额选中与分段选中互斥
            SetSelectedSection(-1, -1, false);
            ClearZoneSelectionVisuals(false);

            _selectedZoneIndex = zoneIndex;
            if (raiseEvent)
            {
                var zone = _deratingZones[zoneIndex];
                OnSelectedZonehandler?.Invoke(zone.CTIndex, zone.ZoneIndex);
            }
            RedrawSections();
        }

        private void ClearZoneSelectionVisuals(bool resetIndex)
        {
            if (_selectedZoneOverlays.Count > 0)
            {
                foreach (var shape in _selectedZoneOverlays)
                {
                    Root?.Children.Remove(shape);
                    Zone?.Children.Remove(shape);
                    SelectionOverlay?.Children.Remove(shape);
                }
                _selectedZoneOverlays.Clear();
            }

            if (resetIndex)
                _selectedZoneIndex = -1;
        }

        private void ApplyZoneSelectionVisuals()
        {
            if (_selectedZoneIndex < 0)
                return;

            if (Zone != null)
            {
                var zoneBases = Zone.Children
                    .OfType<Rectangle>()
                    .Where(r => r.Tag is ZoneVisualTag t && !t.IsOverlay && t.ZoneIndex == _selectedZoneIndex)
                    .ToList();

                foreach (var baseRect in zoneBases)
                {
                    var overlay = new Rectangle
                    {
                        Width = baseRect.Width,
                        Height = baseRect.Height,
                        Fill = SelectedZoneBrush,
                        Stroke = null,
                        IsHitTestVisible = false,
                        Tag = new ZoneVisualTag { ZoneIndex = _selectedZoneIndex, IsOverlay = true }
                    };
                    Canvas.SetLeft(overlay, Canvas.GetLeft(baseRect));
                    Canvas.SetTop(overlay, Canvas.GetTop(baseRect));
                    Zone.Children.Add(overlay);
                    _selectedZoneOverlays.Add(overlay);
                }
            }
        }

        private void DrawSelectedZoneOverlayOnRoot(
            double marginTop,
            double centerX,
            double meterToPixel,
            double connectorGapPixel,
            double diameterToPixel,
            double minWall,
            double maxWall,
            List<CtMetric> ctMetrics)
        {
            if (SelectionOverlay == null || _selectedZoneIndex < 0 || _selectedZoneIndex >= _deratingZones.Count)
                return;

            var selected = _deratingZones[_selectedZoneIndex];
            if (selected.CTIndex < 0 || selected.CTIndex >= CTs.Count)
                return;
            if (!_zoneModeCTIndices.Contains(selected.CTIndex))
                return;

            int ctIndex = selected.CTIndex;
            var ctMetric = ctMetrics[ctIndex];
            var ct = CTs[ctIndex];
            double zStart = Math.Min(selected.StartPos, selected.EndPos);
            double zEnd = Math.Max(selected.StartPos, selected.EndPos);
            const double eps = 1e-6;
            if (zEnd - zStart < eps)
                return;

            double ctLocalAcc = 0.0;
            double ctOffsetPixel = connectorGapPixel * ctIndex;
            for (int secIndex = 0; secIndex < ct.Count; secIndex++)
            {
                var s = ct[secIndex];
                double topM = ctLocalAcc;
                double bottomM = ctLocalAcc + s.Length;

                double intersectStart = Math.Max(zStart, topM);
                double intersectEnd = Math.Min(zEnd, bottomM);
                if (intersectEnd - intersectStart < eps)
                {
                    ctLocalAcc = bottomM;
                    continue;
                }

                double len = s.Length;
                if (len < eps)
                {
                    ctLocalAcc = bottomM;
                    continue;
                }

                double outerTopR = s.OuterDiameterOfReelEnd * diameterToPixel;
                double outerBotR = s.OuterDiameterOfFreeEnd * diameterToPixel;
                double wallTopPhys = s.OuterDiameterOfReelEnd - s.InnerDiameterOfReelEnd;
                double wallBotPhys = s.OuterDiameterOfFreeEnd - s.InnerDiameterOfFreeEnd;

                double tTop = 0.5;
                double tBot = 0.5;
                if (maxWall - minWall > eps)
                {
                    tTop = (wallTopPhys - minWall) / (maxWall - minWall);
                    tBot = (wallBotPhys - minWall) / (maxWall - minWall);
                    tTop = Math.Max(0.0, Math.Min(1.0, tTop));
                    tBot = Math.Max(0.0, Math.Min(1.0, tBot));
                }

                double wallFracTopTotal = MinWallFracTotal + (MaxWallFracTotal - MinWallFracTotal) * tTop;
                double wallFracBotTotal = MinWallFracTotal + (MaxWallFracTotal - MinWallFracTotal) * tBot;
                double innerTopR = outerTopR * (1 - wallFracTopTotal);
                double innerBotR = outerBotR * (1 - wallFracBotTotal);

                double t0 = (intersectStart - topM) / len;
                double t1 = (intersectEnd - topM) / len;
                t0 = Math.Max(0.0, Math.Min(1.0, t0));
                t1 = Math.Max(0.0, Math.Min(1.0, t1));

                double innerR0 = innerTopR + t0 * (innerBotR - innerTopR);
                double innerR1 = innerTopR + t1 * (innerBotR - innerTopR);

                double yTop = SnapY(marginTop + (ctMetric.StartMeter + intersectStart) * meterToPixel + ctOffsetPixel);
                double yBottom = SnapY(marginTop + (ctMetric.StartMeter + intersectEnd) * meterToPixel + ctOffsetPixel);
                if (yBottom < yTop)
                {
                    double tmp = yTop;
                    yTop = yBottom;
                    yBottom = tmp;
                }

                var overlay = new System.Windows.Shapes.Polygon
                {
                    Fill = SelectedZoneBrush,
                    Stroke = null,
                    StrokeThickness = 0,
                    IsHitTestVisible = false,
                    Tag = new ZoneVisualTag { ZoneIndex = _selectedZoneIndex, IsOverlay = true },
                    Points = new PointCollection
                    {
                        new Point(centerX - innerR0, yTop),
                        new Point(centerX + innerR0, yTop),
                        new Point(centerX + innerR1, yBottom),
                        new Point(centerX - innerR1, yBottom)
                    }
                };
                SelectionOverlay.Children.Add(overlay);
                _selectedZoneOverlays.Add(overlay);

                ctLocalAcc = bottomM;
            }
        }
        #endregion



        #region 绘制
        private bool _isRedrawing = false;     // 当前是否正在重绘
        private bool _redrawPending = false;   // 重绘期间是否又收到了新的请求
        private readonly object _redrawLock = new object();

        private void RedrawSections()
        {
            lock (_redrawLock)
            {
                // 如果正在绘制，则只记录“还有一次最新请求”
                if (_isRedrawing)
                {
                    _redrawPending = true;
                    return;
                }

                // 当前没有绘制，则启动处理
                _isRedrawing = true;
            }

            // 保证在 UI 线程执行
            Dispatcher.BeginInvoke(
                new Action(ProcessRedrawQueue),
                System.Windows.Threading.DispatcherPriority.Render);
        }

        private void ProcessRedrawQueue()
        {
            while (true)
            {
                // 开始本轮前，先清掉 pending 标记
                lock (_redrawLock)
                {
                    _redrawPending = false;
                }

                // 真正执行绘制
                RedrawSectionsCore();

                lock (_redrawLock)
                {
                    // 如果本轮执行期间没有新的请求，则结束
                    if (!_redrawPending)
                    {
                        _isRedrawing = false;
                        return;
                    }

                    // 如果执行期间又来了新请求，则继续下一轮
                }
            }
        }

        private int count = 0;
        private void RedrawSectionsCore()
        {
            count++;
            Console.WriteLine($"第{count}次重绘");
            if (Root == null)
                return;

            Root.Children.Clear();
            Zone?.Children.Clear();
            _selectedZoneOverlays.Clear();
            if (ruler != null)
                ruler.Children.Clear();
            if (SelectionOverlay != null)
                SelectionOverlay.Children.Clear();
            ResetSelectedGuideRefs();

            if (CTs.Count == 0)
                return;

            Root.Height = ConTainerHeight;
            ruler.Height = ConTainerHeight;

            double rootHeight = ConTainerHeight;
            double rootWidth = Root.ActualWidth > 0 ? Root.ActualWidth : Root.Width;
            if (rootWidth <= 0 || rootHeight <= 0)
                return;

            double marginX = 0;
            double marginTop = 25;
            double marginBottom = 25;
            double usableWidth = rootWidth - 2 * marginX;
            double usableHeight = rootHeight - marginBottom - marginTop;
            if (usableWidth <= 0 || usableHeight <= 0)
                return;

            _sectionRegions.Clear();

            double totalLength = 0;
            double maxOuterDiameter = 0;

            double minWall = double.MaxValue;
            double maxWall = double.MinValue;

            foreach (var ct in CTs)
            {
                foreach (var s in ct)
                {
                    totalLength += s.Length;

                    maxOuterDiameter = Math.Max(
                        maxOuterDiameter,
                        Math.Max(s.OuterDiameterOfReelEnd, s.OuterDiameterOfFreeEnd));

                    double wallTop = s.OuterDiameterOfReelEnd - s.InnerDiameterOfReelEnd;
                    if (wallTop > 0)
                    {
                        minWall = Math.Min(minWall, wallTop);
                        maxWall = Math.Max(maxWall, wallTop);
                    }

                    double wallBot = s.OuterDiameterOfFreeEnd - s.InnerDiameterOfFreeEnd;
                    if (wallBot > 0)
                    {
                        minWall = Math.Min(minWall, wallBot);
                        maxWall = Math.Max(maxWall, wallBot);
                    }
                }
            }

            if (totalLength <= 0 || maxOuterDiameter <= 0)
                return;

            if (minWall == double.MaxValue)
            {
                minWall = 1;
                maxWall = 1;
            }

            double connectorGapPixel = TopIconHeight;
            double totalGapPixel = connectorGapPixel * Math.Max(CTs.Count - 1, 0);

            double meterToPixel = MeterToPixel;
            double maxPhysicalHeight = usableHeight - totalGapPixel;
            if (maxPhysicalHeight <= 0)
                return;

            if (meterToPixel * totalLength > maxPhysicalHeight)
            {
                meterToPixel = maxPhysicalHeight / totalLength;
            }

            double pipeCoverage = 0.8;
            double halfWidthMax = usableWidth * pipeCoverage / 2.0;

            double diameterToPixel = maxOuterDiameter > 0
                ? halfWidthMax / maxOuterDiameter
                : 0;

            double centerX = rootWidth / 2.0;
            var ctMetrics = BuildCtMetrics();
            DrawZoneOverview(marginTop, meterToPixel, connectorGapPixel, ctMetrics);

            var structuralWeldFromTop = new List<double>();
            double globalMeter = 0.0;

            const double eps = 1e-6;

            // 降额带最先加入 Root，保证在左右管壁描边/管内/焊缝之下（Canvas 后添加的在上层）
            DrawDeratingZones(marginTop, centerX, meterToPixel, connectorGapPixel, diameterToPixel, minWall, maxWall, ctMetrics);

            for (int ctIndex = 0; ctIndex < CTs.Count; ctIndex++)
            {
                var ct = CTs[ctIndex];

                double ctOffsetPixel = connectorGapPixel * ctIndex;

                if (ctIndex >= _ctTopImages.Count)
                {
                    _ctTopImages.Add(new CtTopImageInfo
                    {
                        CTIndex = ctIndex,
                        TopIconKind = TopIconKind.Default
                    });
                }

                var info = _ctTopImages[ctIndex];
                info.CTIndex = ctIndex;

                {
                    TopIconKind kind = info.TopIconKind;
                    if (kind == TopIconKind.Default)
                    {
                        kind = (ctIndex == 0) ? TopIconKind.Reel : TopIconKind.Connector;
                    }

                    if (kind == TopIconKind.None)
                    {
                        info.Image = null;
                    }
                    else
                    {
                        var img = new Image
                        {
                            Width = TopIconWidth,
                            Height = TopIconHeight,
                            Stretch = Stretch.Uniform
                        };

                        if (kind == TopIconKind.Reel)
                            img.Source = _reelIconSource;
                        else if (kind == TopIconKind.Connector)
                        {
                            if (info.ImageSource == null)
                                img.Source = _connectorIconSource;
                            else
                                img.Source = info.ImageSource;
                        }

                        double iconCenterX = centerX;
                        double iconX = iconCenterX - TopIconWidth / 2.0;
                        double iconY;

                        if (ctIndex == 0)
                        {
                            double ctTopM = globalMeter;
                            double ctTopY = marginTop + ctOffsetPixel + ctTopM * meterToPixel;
                            iconY = ctTopY - TopIconHeight;
                        }
                        else
                        {
                            double prevBottomY = marginTop + (connectorGapPixel * (ctIndex - 1))
                                                 + globalMeter * meterToPixel;
                            double currTopY = marginTop + ctOffsetPixel
                                              + globalMeter * meterToPixel;
                            double iconCenterY = (prevBottomY + currTopY) / 2.0;
                            iconY = iconCenterY - TopIconHeight / 2.0;
                        }

                        Canvas.SetLeft(img, iconX);
                        Canvas.SetTop(img, iconY);

                        Root.Children.Add(img);
                        info.Image = img;
                    }
                }

                for (int secIndex = 0; secIndex < ct.Count; secIndex++)
                {
                    var s = ct[secIndex];

                    double topM = globalMeter;
                    double bottomM = globalMeter + s.Length;

                    double topY = SnapY(marginTop + topM * meterToPixel + ctOffsetPixel);
                    double bottomY = SnapY(marginTop + bottomM * meterToPixel + ctOffsetPixel);

                    double outerTopR = s.OuterDiameterOfReelEnd * diameterToPixel;
                    double outerBotR = s.OuterDiameterOfFreeEnd * diameterToPixel;

                    double wallTopPhys = s.OuterDiameterOfReelEnd - s.InnerDiameterOfReelEnd;
                    double wallBotPhys = s.OuterDiameterOfFreeEnd - s.InnerDiameterOfFreeEnd;

                    double tTop = 0.5;
                    double tBot = 0.5;
                    if (maxWall - minWall > eps)
                    {
                        tTop = (wallTopPhys - minWall) / (maxWall - minWall);
                        tBot = (wallBotPhys - minWall) / (maxWall - minWall);
                        tTop = Math.Max(0.0, Math.Min(1.0, tTop));
                        tBot = Math.Max(0.0, Math.Min(1.0, tBot));
                    }

                    double wallFracTopTotal = MinWallFracTotal +
                                              (MaxWallFracTotal - MinWallFracTotal) * tTop;
                    double wallFracBotTotal = MinWallFracTotal +
                                              (MaxWallFracTotal - MinWallFracTotal) * tBot;

                    double innerTopR = outerTopR * (1 - wallFracTopTotal);
                    double innerBotR = outerBotR * (1 - wallFracBotTotal);

                    var leftPoly = new System.Windows.Shapes.Polygon
                    {
                        Tag = "left",
                        Fill = SectionBrush,
                        Stroke = LineBrush,
                        StrokeThickness = LineWeight,
                        Points = new PointCollection
                {
                    new Point(centerX - outerTopR, topY),
                    new Point(centerX - outerBotR, bottomY),
                    new Point(centerX - innerBotR, bottomY),
                    new Point(centerX - innerTopR, topY)
                }
                    };
                    Root.Children.Add(leftPoly);

                    var rightPoly = new System.Windows.Shapes.Polygon
                    {
                        Tag = "right",
                        Fill = SectionBrush,
                        Stroke = LineBrush,
                        StrokeThickness = LineWeight,
                        Points = new PointCollection
                {
                    new Point(centerX + outerTopR, topY),
                    new Point(centerX + outerBotR, bottomY),
                    new Point(centerX + innerBotR, bottomY),
                    new Point(centerX + innerTopR, topY)
                }
                    };
                    Root.Children.Add(rightPoly);

                    double halfStroke = LineWeight / 2.0;
                    double topYInside = topY + halfStroke;
                    double bottomYInside = bottomY - halfStroke;

                    double innerTopRInside = Math.Max(0, innerTopR - halfStroke);
                    double innerBotRInside = Math.Max(0, innerBotR - halfStroke);

                    var insidePoly = new System.Windows.Shapes.Polygon
                    {
                        Fill = InsideBrush,
                        Stroke = null,
                        StrokeThickness = 0,
                        Points = new PointCollection
                {
                    new Point(centerX - innerTopRInside, topYInside),
                    new Point(centerX - innerBotRInside, bottomYInside),
                    new Point(centerX + innerBotRInside, bottomYInside),
                    new Point(centerX + innerTopRInside, topYInside)
                }
                    };
                    Root.Children.Add(insidePoly);

                    double maxOuterR = Math.Max(outerTopR, outerBotR);
                    double leftX = centerX - maxOuterR;
                    double width = maxOuterR * 2;
                    double height = bottomY - topY;

                    _sectionRegions.Add(new SectionRegion
                    {
                        CTIndex = ctIndex,
                        SectionIndex = secIndex,
                        Bounds = new Rect(leftX, topY, width, height),
                        LeftWall = leftPoly,
                        RightWall = rightPoly,
                        InsideShape = insidePoly,
                        InsideOriginalBrush = insidePoly.Fill
                    });

                    bool isLastSectionOverall =
                        (ctIndex == CTs.Count - 1) && (secIndex == ct.Count - 1);

                    if (!isLastSectionOverall)
                    {
                        var weldLine = new System.Windows.Shapes.Line
                        {
                            X1 = centerX - outerBotR,
                            X2 = centerX + outerBotR,
                            Y1 = bottomY,
                            Y2 = bottomY,
                            Stroke = WeldBrush,
                            StrokeThickness = LineWeight
                        };
                        Root.Children.Add(weldLine);

                        structuralWeldFromTop.Add(bottomM);
                    }

                    // 自由焊缝默认不在常规重绘中显示；
                    // 仅在分段选中效果里，用虚斜线样式高亮展示。

                    globalMeter = bottomM;
                }
            }

            DrawSelectedZoneOverlayOnRoot(marginTop, centerX, meterToPixel, connectorGapPixel, diameterToPixel, minWall, maxWall, ctMetrics);

            DrawRuler(totalLength);
        }

        private List<CtMetric> BuildCtMetrics()
        {
            var metrics = new List<CtMetric>(CTs.Count);
            double acc = 0.0;
            for (int i = 0; i < CTs.Count; i++)
            {
                double len = 0.0;
                var ct = CTs[i];
                for (int j = 0; j < ct.Count; j++)
                    len += ct[j].Length;
                metrics.Add(new CtMetric { CTIndex = i, StartMeter = acc, LengthMeter = len });
                acc += len;
            }
            return metrics;
        }

        /// <summary>
        /// 将单根连续管的重叠降额区间解析成不重叠片段。
        /// 规则：同一轴向位置存在多个降额时，后插入者优先，避免半透明叠色。
        /// </summary>
        private List<ResolvedZoneSpan> ResolveDeratingSpansForCt(int ctIndex, double ctLength)
        {
            const double eps = 1e-6;
            var spans = new List<ResolvedZoneSpan>();
            if (ctLength <= eps || _deratingZones.Count == 0)
                return spans;

            var candidates = new List<ZoneCandidate>();
            var breakpoints = new List<double>();

            for (int i = 0; i < _deratingZones.Count; i++)
            {
                var z = _deratingZones[i];
                if (z.CTIndex != ctIndex)
                    continue;

                double start = Math.Min(z.StartPos, z.EndPos);
                double end = Math.Max(z.StartPos, z.EndPos);
                if (end - start < eps)
                    continue;

                start = Math.Max(0, Math.Min(ctLength, start));
                end = Math.Max(0, Math.Min(ctLength, end));
                if (end - start < eps)
                    continue;

                candidates.Add(new ZoneCandidate
                {
                    Index = i,
                    Start = start,
                    End = end,
                    Value = Math.Max(0, Math.Min(1, z.ZoneValue))
                });
                breakpoints.Add(start);
                breakpoints.Add(end);
            }

            if (candidates.Count == 0 || breakpoints.Count < 2)
                return spans;

            breakpoints.Sort();
            var unique = new List<double>(breakpoints.Count);
            for (int i = 0; i < breakpoints.Count; i++)
            {
                if (unique.Count == 0 || Math.Abs(breakpoints[i] - unique[unique.Count - 1]) > eps)
                    unique.Add(breakpoints[i]);
            }
            if (unique.Count < 2)
                return spans;

            for (int i = 0; i < unique.Count - 1; i++)
            {
                double a = unique[i];
                double b = unique[i + 1];
                if (b - a < eps)
                    continue;

                double mid = (a + b) * 0.5;
                int winnerIndex = -1;
                double winnerValue = 0.0;

                for (int j = 0; j < candidates.Count; j++)
                {
                    var c = candidates[j];
                    if (mid >= c.Start && mid <= c.End)
                    {
                        winnerIndex = c.Index;
                        winnerValue = c.Value;
                    }
                }

                if (winnerIndex < 0)
                    continue;

                var last = spans.Count > 0 ? spans[spans.Count - 1] : null;
                if (last != null && last.ZoneIndex == winnerIndex &&
                    Math.Abs(last.ZoneValue - winnerValue) <= eps &&
                    Math.Abs(last.End - a) <= eps)
                {
                    last.End = b;
                }
                else
                {
                    spans.Add(new ResolvedZoneSpan
                    {
                        ZoneIndex = winnerIndex,
                        ZoneValue = winnerValue,
                        Start = a,
                        End = b
                    });
                }
            }

            return spans;
        }

        /// <summary>
        /// 在管柱几何之下绘制降额半透明带（先于左右壁/管内/焊缝加入 Canvas）。
        /// </summary>
        private void DrawDeratingZones(
            double marginTop,
            double centerX,
            double meterToPixel,
            double connectorGapPixel,
            double diameterToPixel,
            double minWall,
            double maxWall,
            List<CtMetric> ctMetrics)
        {
            if (_zoneModeCTIndices.Count == 0 || _deratingZones.Count == 0)
                return;

            const double eps = 1e-6;
            for (int ctIndex = 0; ctIndex < CTs.Count; ctIndex++)
            {
                if (!_zoneModeCTIndices.Contains(ctIndex))
                    continue;

                var ctMetric = ctMetrics[ctIndex];
                if (ctMetric.LengthMeter <= eps)
                    continue;

                var resolvedSpans = ResolveDeratingSpansForCt(ctIndex, ctMetric.LengthMeter);
                if (resolvedSpans.Count == 0)
                    continue;

                for (int zi = 0; zi < resolvedSpans.Count; zi++)
                {
                    var zone = resolvedSpans[zi];
                    double zStart = zone.Start;
                    double zEnd = zone.End;
                    if (zEnd - zStart < eps)
                        continue;

                    var brush = new SolidColorBrush(DeratingZonePalette.GetZoneColor(zone.ZoneIndex, zone.ZoneValue));
                    if (brush.CanFreeze)
                        brush.Freeze();

                    var ct = CTs[ctIndex];
                    double ctOffsetPixel = connectorGapPixel * ctIndex;
                    double ctLocalAcc = 0.0;

                    for (int secIndex = 0; secIndex < ct.Count; secIndex++)
                    {
                        var s = ct[secIndex];
                        double topLocal = ctLocalAcc;
                        double bottomLocal = ctLocalAcc + s.Length;

                        double intersectStart = Math.Max(zStart, topLocal);
                        double intersectEnd = Math.Min(zEnd, bottomLocal);
                        if (intersectEnd - intersectStart < eps)
                        {
                            ctLocalAcc = bottomLocal;
                            continue;
                        }

                        double len = s.Length;
                        if (len < eps)
                        {
                            ctLocalAcc = bottomLocal;
                            continue;
                        }

                        double outerTopR = s.OuterDiameterOfReelEnd * diameterToPixel;
                        double outerBotR = s.OuterDiameterOfFreeEnd * diameterToPixel;

                        double wallTopPhys = s.OuterDiameterOfReelEnd - s.InnerDiameterOfReelEnd;
                        double wallBotPhys = s.OuterDiameterOfFreeEnd - s.InnerDiameterOfFreeEnd;

                        double tTop = 0.5;
                        double tBot = 0.5;
                        if (maxWall - minWall > eps)
                        {
                            tTop = (wallTopPhys - minWall) / (maxWall - minWall);
                            tBot = (wallBotPhys - minWall) / (maxWall - minWall);
                            tTop = Math.Max(0.0, Math.Min(1.0, tTop));
                            tBot = Math.Max(0.0, Math.Min(1.0, tBot));
                        }

                        double wallFracTopTotal = MinWallFracTotal +
                                                  (MaxWallFracTotal - MinWallFracTotal) * tTop;
                        double wallFracBotTotal = MinWallFracTotal +
                                                  (MaxWallFracTotal - MinWallFracTotal) * tBot;

                        double innerTopR = outerTopR * (1 - wallFracTopTotal);
                        double innerBotR = outerBotR * (1 - wallFracBotTotal);

                        double t0 = (intersectStart - topLocal) / len;
                        double t1 = (intersectEnd - topLocal) / len;
                        t0 = Math.Max(0.0, Math.Min(1.0, t0));
                        t1 = Math.Max(0.0, Math.Min(1.0, t1));

                        double innerR0 = innerTopR + t0 * (innerBotR - innerTopR);
                        double innerR1 = innerTopR + t1 * (innerBotR - innerTopR);

                        double yTop = SnapY(marginTop + (ctMetric.StartMeter + intersectStart) * meterToPixel + ctOffsetPixel);
                        double yBottom = SnapY(marginTop + (ctMetric.StartMeter + intersectEnd) * meterToPixel + ctOffsetPixel);
                        if (yBottom < yTop)
                        {
                            double tmp = yTop;
                            yTop = yBottom;
                            yBottom = tmp;
                        }

                        var poly = new System.Windows.Shapes.Polygon
                        {
                            Fill = brush,
                            Stroke = null,
                            StrokeThickness = 0,
                            IsHitTestVisible = false,
                            Tag = new ZoneVisualTag { ZoneIndex = zone.ZoneIndex, IsOverlay = false }
                        };
                        poly.Points = new PointCollection
                        {
                            new Point(centerX - innerR0, yTop),
                            new Point(centerX + innerR0, yTop),
                            new Point(centerX + innerR1, yBottom),
                            new Point(centerX - innerR1, yBottom)
                        };
                        Root.Children.Add(poly);

                        ctLocalAcc = bottomLocal;
                    }
                }
            }
        }

        /// <summary>
        /// 在 Zone 区域绘制降额示意图：右起左移追加，每条宽度 10，完全不透明。
        /// </summary>
        private void DrawZoneOverview(
            double marginTop,
            double meterToPixel,
            double connectorGapPixel,
            List<CtMetric> ctMetrics)
        {
            if (Zone == null)
                return;

            Zone.Children.Clear();
            Zone.Height = ConTainerHeight;

            const double eps = 1e-6;
            if (_zoneModeCTIndices.Count == 0 || _deratingZones.Count == 0)
            {
                Zone.Width = 0;
                return;
            }

            const double defaultRectWidth = 15.0;
            const int maxVisibleRects = 6;
            var activeCtIndices = _zoneModeCTIndices
                .Where(i => i >= 0 && i < CTs.Count)
                .Where(i => _deratingZones.Any(z => z.CTIndex == i))
                .OrderBy(i => i)
                .ToList();
            if (activeCtIndices.Count == 0)
            {
                Zone.Width = 0;
                return;
            }

            double maxContainerWidth = 0.0;
            var widthByCt = new Dictionary<int, double>();
            var rectWidthByCt = new Dictionary<int, double>();

            foreach (int ctIndex in activeCtIndices)
            {
                int zoneCount = _deratingZones.Count(z => z.CTIndex == ctIndex);
                if (zoneCount <= 0)
                    continue;

                double maxContentWidth = maxVisibleRects * defaultRectWidth;
                double containerWidth = zoneCount <= maxVisibleRects ? zoneCount * defaultRectWidth : maxContentWidth;
                double rectWidth = containerWidth / zoneCount;
                widthByCt[ctIndex] = containerWidth;
                rectWidthByCt[ctIndex] = rectWidth;
                maxContainerWidth = Math.Max(maxContainerWidth, containerWidth);
            }

            Zone.Width = maxContainerWidth;
            if (maxContainerWidth <= eps)
                return;

            foreach (int ctIndex in activeCtIndices)
            {
                if (!widthByCt.ContainsKey(ctIndex))
                    continue;

                double containerWidth = widthByCt[ctIndex];
                double rectWidth = rectWidthByCt[ctIndex];
                double xContainer = maxContainerWidth - containerWidth;

                var metric = ctMetrics[ctIndex];
                double ctOffsetPixel = connectorGapPixel * ctIndex;
                double zoneTop = SnapY(marginTop + metric.StartMeter * meterToPixel + ctOffsetPixel);
                double zoneBottom = SnapY(marginTop + (metric.StartMeter + metric.LengthMeter) * meterToPixel + ctOffsetPixel);
                if (zoneBottom < zoneTop)
                {
                    double t = zoneTop;
                    zoneTop = zoneBottom;
                    zoneBottom = t;
                }

                var containerBackground = new Rectangle
                {
                    Width = containerWidth,
                    Height = Math.Max(0, zoneBottom - zoneTop),
                    Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CA9B9B")),
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(containerBackground, xContainer);
                Canvas.SetTop(containerBackground, zoneTop);
                Zone.Children.Add(containerBackground);

                var zonesOfCt = _deratingZones
                    .Select((z, idx) => new { z, idx })
                    .Where(x => x.z.CTIndex == ctIndex)
                    .ToList();

                for (int localIdx = 0; localIdx < zonesOfCt.Count; localIdx++)
                {
                    var zoneEntry = zonesOfCt[localIdx];
                    var z = zoneEntry.z;
                    double zStart = Math.Min(z.StartPos, z.EndPos);
                    double zEnd = Math.Max(z.StartPos, z.EndPos);
                    if (zEnd - zStart < eps)
                        continue;

                    double xLeft = xContainer + containerWidth - rectWidth - localIdx * rectWidth;
                    var rgb = DeratingZonePalette.GetRgb(zoneEntry.idx);
                    var fill = new SolidColorBrush(Color.FromRgb(rgb.R, rgb.G, rgb.B));
                    if (fill.CanFreeze)
                        fill.Freeze();

                    double yTop = SnapY(marginTop + (metric.StartMeter + zStart) * meterToPixel + ctOffsetPixel);
                    double yBottom = SnapY(marginTop + (metric.StartMeter + zEnd) * meterToPixel + ctOffsetPixel);
                    if (yBottom < yTop)
                    {
                        double tmp = yTop;
                        yTop = yBottom;
                        yBottom = tmp;
                    }

                    var rect = new Rectangle
                    {
                        Width = rectWidth,
                        Height = Math.Max(0, yBottom - yTop),
                        Fill = fill,
                        Stroke = null,
                        IsHitTestVisible = true,
                        Tag = new ZoneVisualTag { ZoneIndex = zoneEntry.idx, IsOverlay = false }
                    };
                    Canvas.SetLeft(rect, xLeft);
                    Canvas.SetTop(rect, yTop);
                    Zone.Children.Add(rect);
                }

                var containerBorder = new Rectangle
                {
                    Width = containerWidth,
                    Height = Math.Max(0, zoneBottom - zoneTop),
                    Fill = Brushes.Transparent,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(containerBorder, xContainer);
                Canvas.SetTop(containerBorder, zoneTop);
                Zone.Children.Add(containerBorder);
            }

            ApplyZoneSelectionVisuals();
        }

        private void RedrawSections1()
        {
            count++;
            Console.WriteLine($"第{count}次重绘");
            if (Root == null)
                return;

            Root.Children.Clear();
            Zone?.Children.Clear();
            _selectedZoneOverlays.Clear();
            if (ruler != null)
                ruler.Children.Clear();

            if (CTs.Count == 0)
                return;

            Root.Height = ConTainerHeight;
            ruler.Height = ConTainerHeight;

            double rootHeight = ConTainerHeight;
            double rootWidth = Root.ActualWidth > 0 ? Root.ActualWidth : Root.Width;
            if (rootWidth <= 0 || rootHeight <= 0)
                return;

            double marginX = 0;
            double marginTop = 25;
            double marginBottom = 25;
            double usableWidth = rootWidth - 2 * marginX;
            double usableHeight = rootHeight - marginBottom - marginTop;
            if (usableWidth <= 0 || usableHeight <= 0)
                return;

            _sectionRegions.Clear();

            double totalLength = 0;
            double maxOuterDiameter = 0;

            double minWall = double.MaxValue;
            double maxWall = double.MinValue;

            foreach (var ct in CTs)
            {
                foreach (var s in ct)
                {
                    totalLength += s.Length;

                    maxOuterDiameter = Math.Max(
                        maxOuterDiameter,
                        Math.Max(s.OuterDiameterOfReelEnd, s.OuterDiameterOfFreeEnd));

                    double wallTop = s.OuterDiameterOfReelEnd - s.InnerDiameterOfReelEnd;
                    if (wallTop > 0)
                    {
                        minWall = Math.Min(minWall, wallTop);
                        maxWall = Math.Max(maxWall, wallTop);
                    }

                    double wallBot = s.OuterDiameterOfFreeEnd - s.InnerDiameterOfFreeEnd;
                    if (wallBot > 0)
                    {
                        minWall = Math.Min(minWall, wallBot);
                        maxWall = Math.Max(maxWall, wallBot);
                    }
                }
            }

            if (totalLength <= 0 || maxOuterDiameter <= 0)
                return;

            if (minWall == double.MaxValue)
            {
                minWall = 1;
                maxWall = 1;
            }

            double connectorGapPixel = TopIconHeight;
            double totalGapPixel = connectorGapPixel * Math.Max(CTs.Count - 1, 0);

            double meterToPixel = MeterToPixel;
            double maxPhysicalHeight = usableHeight - totalGapPixel;
            if (maxPhysicalHeight <= 0)
                return;

            if (meterToPixel * totalLength > maxPhysicalHeight)
            {
                meterToPixel = maxPhysicalHeight / totalLength;
            }

            double pipeCoverage = 0.8;
            double halfWidthMax = usableWidth * pipeCoverage / 2.0;

            double diameterToPixel = maxOuterDiameter > 0
                ? halfWidthMax / maxOuterDiameter
                : 0;

            double centerX = rootWidth / 2.0;
            var ctMetrics = BuildCtMetrics();
            DrawZoneOverview(marginTop, meterToPixel, connectorGapPixel, ctMetrics);

            var structuralWeldFromTop = new List<double>();
            double globalMeter = 0.0;

            const double eps = 1e-6;

            // 降额带最先加入 Root，保证在左右管壁描边/管内/焊缝之下（Canvas 后添加的在上层）
            DrawDeratingZones(marginTop, centerX, meterToPixel, connectorGapPixel, diameterToPixel, minWall, maxWall, ctMetrics);

            for (int ctIndex = 0; ctIndex < CTs.Count; ctIndex++)
            {
                var ct = CTs[ctIndex];

                double ctOffsetPixel = connectorGapPixel * ctIndex;

                if (ctIndex >= _ctTopImages.Count)
                {
                    _ctTopImages.Add(new CtTopImageInfo
                    {
                        CTIndex = ctIndex,
                        TopIconKind = TopIconKind.Default
                    });
                }

                var info = _ctTopImages[ctIndex];
                info.CTIndex = ctIndex;

                {
                    TopIconKind kind = info.TopIconKind;
                    if (kind == TopIconKind.Default)
                    {
                        kind = (ctIndex == 0) ? TopIconKind.Reel : TopIconKind.Connector;
                    }

                    if (kind == TopIconKind.None)
                    {
                        info.Image = null;
                    }
                    else
                    {
                        var img = new Image
                        {
                            Width = TopIconWidth,
                            Height = TopIconHeight,
                            Stretch = Stretch.Uniform
                        };

                        if (kind == TopIconKind.Reel)
                            img.Source = _reelIconSource;
                        else if (kind == TopIconKind.Connector)
                        {
                            if (info.ImageSource == null)
                                img.Source = _connectorIconSource;
                            else
                                img.Source = info.ImageSource;
                        }

                        double iconCenterX = centerX;
                        double iconX = iconCenterX - TopIconWidth / 2.0;
                        double iconY;

                        if (ctIndex == 0)
                        {
                            double ctTopM = globalMeter;
                            double ctTopY = marginTop + ctOffsetPixel + ctTopM * meterToPixel;
                            iconY = ctTopY - TopIconHeight;
                        }
                        else
                        {
                            double prevBottomY = marginTop + (connectorGapPixel * (ctIndex - 1))
                                                 + globalMeter * meterToPixel;
                            double currTopY = marginTop + ctOffsetPixel
                                              + globalMeter * meterToPixel;
                            double iconCenterY = (prevBottomY + currTopY) / 2.0;
                            iconY = iconCenterY - TopIconHeight / 2.0;
                        }

                        Canvas.SetLeft(img, iconX);
                        Canvas.SetTop(img, iconY);

                        Root.Children.Add(img);
                        info.Image = img;
                    }
                }

                for (int secIndex = 0; secIndex < ct.Count; secIndex++)
                {
                    var s = ct[secIndex];

                    double topM = globalMeter;
                    double bottomM = globalMeter + s.Length;

                    double topY = SnapY(marginTop + topM * meterToPixel + ctOffsetPixel);
                    double bottomY = SnapY(marginTop + bottomM * meterToPixel + ctOffsetPixel);

                    double outerTopR = s.OuterDiameterOfReelEnd * diameterToPixel;
                    double outerBotR = s.OuterDiameterOfFreeEnd * diameterToPixel;

                    double wallTopPhys = s.OuterDiameterOfReelEnd - s.InnerDiameterOfReelEnd;
                    double wallBotPhys = s.OuterDiameterOfFreeEnd - s.InnerDiameterOfFreeEnd;

                    double tTop = 0.5;
                    double tBot = 0.5;
                    if (maxWall - minWall > eps)
                    {
                        tTop = (wallTopPhys - minWall) / (maxWall - minWall);
                        tBot = (wallBotPhys - minWall) / (maxWall - minWall);
                        tTop = Math.Max(0.0, Math.Min(1.0, tTop));
                        tBot = Math.Max(0.0, Math.Min(1.0, tBot));
                    }

                    double wallFracTopTotal = MinWallFracTotal +
                                              (MaxWallFracTotal - MinWallFracTotal) * tTop;
                    double wallFracBotTotal = MinWallFracTotal +
                                              (MaxWallFracTotal - MinWallFracTotal) * tBot;

                    double innerTopR = outerTopR * (1 - wallFracTopTotal);
                    double innerBotR = outerBotR * (1 - wallFracBotTotal);

                    var leftPoly = new System.Windows.Shapes.Polygon
                    {
                        Tag = "left",
                        Fill = SectionBrush,
                        Stroke = LineBrush,
                        StrokeThickness = LineWeight,
                        Points = new PointCollection
                {
                    new Point(centerX - outerTopR, topY),
                    new Point(centerX - outerBotR, bottomY),
                    new Point(centerX - innerBotR, bottomY),
                    new Point(centerX - innerTopR, topY)
                }
                    };
                    Root.Children.Add(leftPoly);

                    var rightPoly = new System.Windows.Shapes.Polygon
                    {
                        Tag = "right",
                        Fill = SectionBrush,
                        Stroke = LineBrush,
                        StrokeThickness = LineWeight,
                        Points = new PointCollection
                {
                    new Point(centerX + outerTopR, topY),
                    new Point(centerX + outerBotR, bottomY),
                    new Point(centerX + innerBotR, bottomY),
                    new Point(centerX + innerTopR, topY)
                }
                    };
                    Root.Children.Add(rightPoly);

                    double halfStroke = LineWeight / 2.0;
                    double topYInside = topY + halfStroke;
                    double bottomYInside = bottomY - halfStroke;

                    double innerTopRInside = Math.Max(0, innerTopR - halfStroke);
                    double innerBotRInside = Math.Max(0, innerBotR - halfStroke);

                    var insidePoly = new System.Windows.Shapes.Polygon
                    {
                        Fill = InsideBrush,
                        Stroke = null,
                        StrokeThickness = 0,
                        Points = new PointCollection
                {
                    new Point(centerX - innerTopRInside, topYInside),
                    new Point(centerX - innerBotRInside, bottomYInside),
                    new Point(centerX + innerBotRInside, bottomYInside),
                    new Point(centerX + innerTopRInside, topYInside)
                }
                    };
                    Root.Children.Add(insidePoly);

                    double maxOuterR = Math.Max(outerTopR, outerBotR);
                    double leftX = centerX - maxOuterR;
                    double width = maxOuterR * 2;
                    double height = bottomY - topY;

                    _sectionRegions.Add(new SectionRegion
                    {
                        CTIndex = ctIndex,
                        SectionIndex = secIndex,
                        Bounds = new Rect(leftX, topY, width, height),
                        LeftWall = leftPoly,
                        RightWall = rightPoly,
                        InsideShape = insidePoly,
                        InsideOriginalBrush = insidePoly.Fill
                    });

                    bool isLastSectionOverall =
                        (ctIndex == CTs.Count - 1) && (secIndex == ct.Count - 1);

                    if (!isLastSectionOverall)
                    {
                        var weldLine = new System.Windows.Shapes.Line
                        {
                            X1 = centerX - outerBotR,
                            X2 = centerX + outerBotR,
                            Y1 = bottomY,
                            Y2 = bottomY,
                            Stroke = WeldBrush,
                            StrokeThickness = LineWeight
                        };
                        Root.Children.Add(weldLine);

                        structuralWeldFromTop.Add(bottomM);
                    }

                    if (s.FreeWeldPositions != null && s.FreeWeldPositions.Count > 0)
                    {
                        foreach (var localPos in s.FreeWeldPositions)
                        {
                            if (localPos < 0 || localPos > s.Length)
                                continue;

                            double tt = s.Length <= 0 ? 0 : localPos / s.Length;
                            double outerRAtWeld = outerTopR + (outerBotR - outerTopR) * tt;
                            double weldGlobalM = topM + localPos;
                            double weldY = marginTop + weldGlobalM * meterToPixel + ctOffsetPixel;

                            var freeWeldLine = new System.Windows.Shapes.Line
                            {
                                X1 = centerX - outerRAtWeld,
                                X2 = centerX + outerRAtWeld,
                                Y1 = weldY,
                                Y2 = weldY,
                                Stroke = FreeWeldBrush,
                                StrokeThickness = LineWeight
                            };
                            Root.Children.Add(freeWeldLine);
                        }
                    }

                    globalMeter = bottomM;
                }
            }

            DrawSelectedZoneOverlayOnRoot(marginTop, centerX, meterToPixel, connectorGapPixel, diameterToPixel, minWall, maxWall, ctMetrics);

            DrawRuler(totalLength);
        }

        #region 标尺重绘
        /// <summary>
        /// 在右侧 ruler 画标尺
        /// 完全依赖 CTs 和 _sectionRegions：
        ///   - Y 坐标从 _sectionRegions.Bounds.Bottom 取；
        ///   - 数值用 CTs 的 Length 累加得到，从底部开始算。
        ///两种绘制标尺的模式
        /// </summary>
        private void DrawRuler(double totalLength)
        {
            if (ruler == null || _sectionRegions.Count == 0)
                return;

            ruler.Children.Clear();

            switch (RulerMode)
            {
                case RulerMode.Global:
                    DrawRuler_Global(totalLength);
                    break;

                case RulerMode.PerCT:
                    DrawRuler_PerCT();
                    break;
            }
        }


        /// <summary>
        /// 全量模式：所有 CT 合并成一根总标尺
        /// </summary>
        private void DrawRuler_Global(double totalLength)
        {
            //double rulerWidth = ruler.ActualWidth;
            //double rulerHeight = Root.ActualHeight;

            //if (rulerWidth <= 0) rulerWidth = 50;
            //if (rulerHeight <= 0) rulerHeight = Root.Height > 0 ? Root.Height : ConTainerHeight;


            double rulerHeight = Root.ActualHeight > 0 ? Root.ActualHeight : ConTainerHeight;
            double requiredWidth = 0.0;

            ruler.Width = 0;
            ruler.Height = rulerHeight;

            // 整体最顶 / 最底的像素 Y
            double yTop = _sectionRegions.Min(r => r.Bounds.Top);
            double yBottom = _sectionRegions.Max(r => r.Bounds.Bottom);

            double lineX = 10;          // 竖线 X
            double tickLength = 6;      // 刻度线长度
            double tickStartX = lineX;
            double tickEndX = lineX + tickLength;
            double textLeftGap = 3;     // 文本和刻度线间距

            // 主竖线
            var mainLine = new System.Windows.Shapes.Line
            {
                X1 = lineX,
                X2 = lineX,
                Y1 = yTop,
                Y2 = yBottom,
                Stroke = LineBrush,
                StrokeThickness = 1
            };
            ruler.Children.Add(mainLine);

            // 画一条刻度 + 文本
            void DrawTick(double y, double valueFromBottom)
            {
                var tick = new System.Windows.Shapes.Line
                {
                    X1 = tickStartX,
                    X2 = tickEndX,
                    Y1 = y,
                    Y2 = y,
                    Stroke = LineBrush,
                    StrokeThickness = 1
                };
                ruler.Children.Add(tick);

                var tb = new TextBlock
                {
                    Text = valueFromBottom.ToString("0.0") + DisplayUnit,
                    Foreground = FontBrush,
                    FontSize = fontSize
                };
                // 供“选中分段时，右侧标尺刻度文字变色”使用
                tb.Tag = valueFromBottom;
                tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                requiredWidth = Math.Max(requiredWidth, tickEndX + textLeftGap + tb.DesiredSize.Width + 2);

                Canvas.SetLeft(tb, tickEndX + textLeftGap);
                Canvas.SetTop(tb, y - tb.FontSize * 0.7);

                ruler.Children.Add(tb);
            }

            // 底部 0
            DrawTick(yBottom, 0.0);

            double accFromTop = 0.0;

            //从上往下遍历，按分段底部画刻度
            for (int ctIndex = 0; ctIndex < CTs.Count; ctIndex++)
            {
                var ct = CTs[ctIndex];

                for (int secIndex = 0; secIndex < ct.Count; secIndex++)
                {
                    var sec = ct[secIndex];

                    accFromTop += sec.Length;
                    double valueFromBottom = totalLength - accFromTop;

                    if (valueFromBottom <= 0)
                        continue;

                    if (Math.Abs(valueFromBottom - totalLength) < 1e-6)
                        continue;

                    var region = _sectionRegions
                        .Find(r => r.CTIndex == ctIndex && r.SectionIndex == secIndex);

                    if (region == null)
                        continue;

                    double y = region.Bounds.Bottom;
                    DrawTick(y, valueFromBottom);
                }
            }

            // 顶部 totalLength
            DrawTick(yTop, totalLength);

            ruler.Width = Math.Max(20, requiredWidth);
        }



        /// <summary>
        /// 分管模式：每根 CT 自己一根标尺，0 从各自底部开始
        /// </summary>
        private void DrawRuler_PerCT()
        {
            //double rulerWidth = ruler.ActualWidth;
            //double rulerHeight = Root.ActualHeight;

            //if (rulerWidth <= 0) rulerWidth = 50;
            //if (rulerHeight <= 0) rulerHeight = Root.Height > 0 ? Root.Height : ConTainerHeight;


            double rulerHeight = Root.Height > 0 ? Root.Height : ConTainerHeight;
            double requiredWidth = 0.0;

            ruler.Width = 0;
            ruler.Height = rulerHeight;

            double lineX = 10;          // 所有 CT 共用同一个 X
            double tickLength = 6;
            double tickStartX = lineX;
            double tickEndX = lineX + tickLength;
            double textLeftGap = 3;

            void DrawTick(double y, double valueFromBottom)
            {
                var tick = new System.Windows.Shapes.Line
                {
                    X1 = tickStartX,
                    X2 = tickEndX,
                    Y1 = y,
                    Y2 = y,
                    Stroke = LineBrush,
                    StrokeThickness = 1
                };
                ruler.Children.Add(tick);

                var tb = new TextBlock
                {
                    Text = valueFromBottom.ToString("0.0") + DisplayUnit,
                    Foreground = FontBrush,
                    FontSize = fontSize
                };
                // 供“选中分段时，右侧标尺刻度文字变色”使用
                tb.Tag = valueFromBottom;
                tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                requiredWidth = Math.Max(requiredWidth, tickEndX + textLeftGap + tb.DesiredSize.Width + 2);

                Canvas.SetLeft(tb, tickEndX + textLeftGap);
                Canvas.SetTop(tb, y - tb.FontSize * 0.7);

                ruler.Children.Add(tb);
            }

            for (int ctIndex = 0; ctIndex < CTs.Count; ctIndex++)
            {
                var ct = CTs[ctIndex];
                if (ct.Count == 0)
                    continue;

                // 本 CT 的总长度
                double ctLength = 0.0;
                foreach (var s in ct)
                    ctLength += s.Length;
                if (ctLength <= 0)
                    continue;

                // 本 CT 的像素范围（只看该 CT 的 SectionRegion）
                var regionsOfCt = _sectionRegions.Where(r => r.CTIndex == ctIndex).ToList();
                if (regionsOfCt.Count == 0)
                    continue;

                double yTopCt = regionsOfCt.Min(r => r.Bounds.Top);
                double yBottomCt = regionsOfCt.Max(r => r.Bounds.Bottom);

                //本 CT 的主竖线
                var mainLine = new System.Windows.Shapes.Line
                {
                    X1 = lineX,
                    X2 = lineX,
                    Y1 = yTopCt,
                    Y2 = yBottomCt,
                    Stroke = LineBrush,
                    StrokeThickness = 1
                };
                ruler.Children.Add(mainLine);

                // 底部 0
                DrawTick(yBottomCt, 0.0);

                // 按分段底部画刻度（数值从本 CT 底部开始累加）
                double accFromTopCt = 0.0;
                for (int secIndex = 0; secIndex < ct.Count; secIndex++)
                {
                    var sec = ct[secIndex];

                    accFromTopCt += sec.Length;
                    double valueFromBottom = ctLength - accFromTopCt;

                    if (valueFromBottom <= 0)
                        continue;

                    if (Math.Abs(valueFromBottom - ctLength) < 1e-6)
                        continue;

                    var region = regionsOfCt.Find(r => r.SectionIndex == secIndex);
                    if (region == null)
                        continue;

                    double y = region.Bounds.Bottom;
                    DrawTick(y, valueFromBottom);
                }

                // 顶部 ctLength
                DrawTick(yTopCt, ctLength);
            }

            ruler.Width = Math.Max(20, requiredWidth);
        }

        #endregion

        private void UpdateRulerSelectedSectionTextColor()
        {
            if (ruler == null)
                return;

            foreach (var child in ruler.Children)
            {
                if (child is TextBlock tb)
                    tb.Foreground = FontBrush;
            }

            if (_selectedCTIndex < 0 || _selectedSectionIndex < 0)
                return;

            double topDepth;
            double bottomDepth;

            if (RulerMode == RulerMode.Global)
            {
                GetSectionDepthValuesFromBottom(_selectedCTIndex, _selectedSectionIndex, out topDepth, out bottomDepth);
            }
            else
            {
                GetSectionDepthValuesFromBottom_PerCT(_selectedCTIndex, _selectedSectionIndex, out topDepth, out bottomDepth);
            }

            double topRounded = Math.Round(topDepth, 1);
            double bottomRounded = Math.Round(bottomDepth, 1);

            foreach (var child in ruler.Children)
            {
                if (child is TextBlock tb && tb.Tag is double v)
                {
                    double vRounded = Math.Round(v, 1);
                    if (vRounded == topRounded || vRounded == bottomRounded)
                        tb.Foreground = Brushes.Red;
                }
            }
        }

        private void GetSectionDepthValuesFromBottom_PerCT(int ctIndex, int sectionIndex, out double topDepth, out double bottomDepth)
        {
            topDepth = 0.0;
            bottomDepth = 0.0;

            if (ctIndex < 0 || ctIndex >= CTs.Count)
                return;

            var ct = CTs[ctIndex];
            if (sectionIndex < 0 || sectionIndex >= ct.Count)
                return;

            double ctLength = 0.0;
            foreach (var sec in ct)
                ctLength += sec.Length;

            if (ctLength <= 0)
                return;

            double accBefore = 0.0;
            for (int i = 0; i < sectionIndex; i++)
                accBefore += ct[i].Length;

            double sectionLength = ct[sectionIndex].Length;
            topDepth = ctLength - accBefore;
            bottomDepth = ctLength - (accBefore + sectionLength);
        }

        /// <summary>
        /// 设置选中效果
        /// </summary>
        /// <param name="ctIndex">连续管索引</param>
        /// <param name="sectionIndex">分段索引</param>
        /// <param name="raiseEvent">是否触发委托</param>
        public void SetSelectedSection(int ctIndex, int sectionIndex, bool raiseEvent=false)
        {
            // 分段选中与降额选中互斥：选中分段时清空降额选中效果
            if (ctIndex >= 0 && sectionIndex >= 0)
                ClearZoneSelectionVisuals(true);

            // 还原旧的选中段
            if (_selectedCTIndex >= 0 && _selectedSectionIndex >= 0)
            {
                var old = _sectionRegions
                    .Find(r => r.CTIndex == _selectedCTIndex && r.SectionIndex == _selectedSectionIndex);
                if (old != null)
                {
                    old.LeftWall.Fill = SectionBrush;
                    old.RightWall.Fill = SectionBrush;
                    if (old.InsideShape != null)
                        old.InsideShape.Fill = old.InsideOriginalBrush;
                }
            }
            ClearSelectedGuideVisuals();

            _selectedCTIndex = ctIndex;
            _selectedSectionIndex = sectionIndex;

            // 新选中段
            if (ctIndex >= 0 && sectionIndex >= 0)
            {
                var cur = _sectionRegions
                    .Find(r => r.CTIndex == ctIndex && r.SectionIndex == sectionIndex);
                if (cur != null)
                {
                    cur.LeftWall.Fill = SectionSelectedBrush;
                    cur.RightWall.Fill = SectionSelectedBrush;
                    if (cur.InsideShape != null)
                        cur.InsideShape.Fill = SectionSelectedBrush;

                    DrawSelectedSectionFreeWelds(cur);

                    if (raiseEvent)
                        OnSelectedSectionhandler?.Invoke(ctIndex, sectionIndex);
                }
            }

            // 选中态改为：右侧标尺对应的上下刻度文字变红
            UpdateRulerSelectedSectionTextColor();
        }

        private void DrawSelectedSectionGuides(SectionRegion region)
        {
            if (region == null || SelectionOverlay == null)
                return;

            // 从分段左上/左下两点向左画短线，再向左留出文本间距显示深度值
            const double guideWidth = 15.0;
            const double textGap = 5.0;

            double yTop = region.Bounds.Top;
            double yBottom = region.Bounds.Bottom;
            double xLeft = region.Bounds.Left;

            double xGuideStart = xLeft;
            double xGuideEnd = xLeft - guideWidth;
            double textAnchorX = xGuideEnd - textGap;

            var topGuide = new System.Windows.Shapes.Line
            {
                X1 = xGuideStart,
                X2 = xGuideEnd,
                Y1 = yTop,
                Y2 = yTop,
                Stroke = LineBrush,
                StrokeThickness = LineWeight
            };
            SelectionOverlay.Children.Add(topGuide);

            var bottomGuide = new System.Windows.Shapes.Line
            {
                X1 = xGuideStart,
                X2 = xGuideEnd,
                Y1 = yBottom,
                Y2 = yBottom,
                Stroke = LineBrush,
                StrokeThickness = LineWeight
            };
            SelectionOverlay.Children.Add(bottomGuide);

            GetSectionDepthValuesFromBottom(region.CTIndex, region.SectionIndex, out double topDepth, out double bottomDepth);

            var topText = new TextBlock
            {
                Text = topDepth.ToString("0.0") + DisplayUnit,
                Foreground = FontBrush,
                FontSize = fontSize
            };
            topText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(topText, textAnchorX - topText.DesiredSize.Width);
            Canvas.SetTop(topText, yTop - topText.DesiredSize.Height / 2.0);
            SelectionOverlay.Children.Add(topText);

            var bottomText = new TextBlock
            {
                Text = bottomDepth.ToString("0.0") + DisplayUnit,
                Foreground = FontBrush,
                FontSize = fontSize
            };
            bottomText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(bottomText, textAnchorX - bottomText.DesiredSize.Width);
            Canvas.SetTop(bottomText, yBottom - bottomText.DesiredSize.Height / 2.0);
            SelectionOverlay.Children.Add(bottomText);

            _selectedTopGuideLine = topGuide;
            _selectedBottomGuideLine = bottomGuide;
            _selectedTopDepthText = topText;
            _selectedBottomDepthText = bottomText;
        }

        private void DrawSelectedSectionFreeWelds(SectionRegion region)
        {
            if (region == null || SelectionOverlay == null)
                return;

            if (region.CTIndex < 0 || region.CTIndex >= CTs.Count)
                return;

            var ct = CTs[region.CTIndex];
            if (region.SectionIndex < 0 || region.SectionIndex >= ct.Count)
                return;

            var sec = ct[region.SectionIndex];
            if (sec.FreeWeldPositions == null || sec.FreeWeldPositions.Count == 0 || sec.Length <= 0)
                return;

            // 取外壁左右边界（上端/下端）用于线性插值，得到每条自由焊缝在当前分段内的显示宽度
            var leftPts = region.LeftWall?.Points;
            var rightPts = region.RightWall?.Points;
            if (leftPts == null || rightPts == null || leftPts.Count < 2 || rightPts.Count < 2)
                return;

            var leftTop = leftPts[0];
            var leftBottom = leftPts[1];
            var rightTop = rightPts[0];
            var rightBottom = rightPts[1];

            foreach (var localPos in sec.FreeWeldPositions)
            {
                if (localPos < 0 || localPos > sec.Length)
                    continue;

                double t = localPos / sec.Length;
                double y = leftTop.Y + (leftBottom.Y - leftTop.Y) * t;
                double xLeft = leftTop.X + (leftBottom.X - leftTop.X) * t;
                double xRight = rightTop.X + (rightBottom.X - rightTop.X) * t;

                DrawSlantedDashedLine(xLeft, xRight, y, FreeWeldBrush, LineWeight);
            }
        }

        private void DrawSlantedDashedLine(double xStart, double xEnd, double yCenter, Brush brush, double thickness)
        {
            if (SelectionOverlay == null)
                return;

            // 斜短线组成的虚线：单段垂直高度约 2（+1 到 -1）
            const double segmentLength = 4.0;
            const double gap = 3.0;
            const double halfHeight = 1.0;

            double left = Math.Min(xStart, xEnd);
            double right = Math.Max(xStart, xEnd);
            if (right <= left)
                return;

            for (double x = left; x < right; x += segmentLength + gap)
            {
                double x2 = Math.Min(x + segmentLength, right);
                var dash = new System.Windows.Shapes.Line
                {
                    X1 = x,
                    Y1 = yCenter + halfHeight,
                    X2 = x2,
                    Y2 = yCenter - halfHeight,
                    Stroke = brush ?? FreeWeldBrush,
                    StrokeThickness = thickness
                };
                SelectionOverlay.Children.Add(dash);
                _selectedFreeWeldMarks.Add(dash);
            }
        }

        private void GetSectionDepthValuesFromBottom(int ctIndex, int sectionIndex, out double topDepth, out double bottomDepth)
        {
            topDepth = 0.0;
            bottomDepth = 0.0;

            if (ctIndex < 0 || ctIndex >= CTs.Count)
                return;

            var ct = CTs[ctIndex];
            if (sectionIndex < 0 || sectionIndex >= ct.Count)
                return;

            double totalLength = 0.0;
            foreach (var list in CTs)
            {
                foreach (var sec in list)
                {
                    totalLength += sec.Length;
                }
            }

            double accBefore = 0.0;
            for (int i = 0; i < ctIndex; i++)
            {
                foreach (var sec in CTs[i])
                {
                    accBefore += sec.Length;
                }
            }
            for (int i = 0; i < sectionIndex; i++)
            {
                accBefore += ct[i].Length;
            }

            double sectionLength = ct[sectionIndex].Length;
            topDepth = totalLength - accBefore;
            bottomDepth = totalLength - (accBefore + sectionLength);
        }

        private void ClearSelectedGuideVisuals()
        {
            if (SelectionOverlay == null)
            {
                ResetSelectedGuideRefs();
                return;
            }

            if (_selectedTopGuideLine != null)
                SelectionOverlay.Children.Remove(_selectedTopGuideLine);
            if (_selectedBottomGuideLine != null)
                SelectionOverlay.Children.Remove(_selectedBottomGuideLine);
            if (_selectedTopDepthText != null)
                SelectionOverlay.Children.Remove(_selectedTopDepthText);
            if (_selectedBottomDepthText != null)
                SelectionOverlay.Children.Remove(_selectedBottomDepthText);
            if (_selectedFreeWeldMarks.Count > 0)
            {
                foreach (var mark in _selectedFreeWeldMarks)
                {
                    SelectionOverlay.Children.Remove(mark);
                }
                _selectedFreeWeldMarks.Clear();
            }

            ResetSelectedGuideRefs();
        }

        private void ResetSelectedGuideRefs()
        {
            _selectedTopGuideLine = null;
            _selectedBottomGuideLine = null;
            _selectedTopDepthText = null;
            _selectedBottomDepthText = null;
            _selectedFreeWeldMarks.Clear();
        }


        #endregion

        #region 事件
        private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!EnableSelectedSection)
                return;

            Point p = e.GetPosition(Root);

            // 找到第一个 Bounds 包含该点的分段
            SectionRegion hit = null;
            foreach (var r in _sectionRegions)
            {
                if (r.Bounds.Contains(p))
                {
                    hit = r;
                    break;
                }
            }

            if (hit != null)
            {
                SetSelectedSection(hit.CTIndex, hit.SectionIndex, raiseEvent: true);
                e.Handled = true;
            }
        }

        private void Zone_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_zoneModeCTIndices.Count == 0 || Zone == null)
                return;

            // 点击降额示意图时，先清理分段引导选中效果
            ClearSelectedGuideVisuals();

            if (e.OriginalSource is Rectangle rect &&
                rect.Tag is ZoneVisualTag tag &&
                !tag.IsOverlay)
            {
                Console.WriteLine("手动选中，触发委托");
                SetSelectedZoneByIndex(tag.ZoneIndex, true);
                e.Handled = true;
            }
        }

        #endregion

        #region 工具

        private double SnapY(double yDip)
        {
            var dpi = VisualTreeHelper.GetDpi(this);
            double scaleY = dpi.DpiScaleY;
            double yPx = yDip * scaleY;
            double snappedPx = Math.Round(yPx); 
            return snappedPx / scaleY;
        }



        private void UpdateReelIcon(Brush brush)
        {
            // 每次从模板重新克隆一份
            var dg = _reelTemplate.CloneCurrentValue();

            Brush newBrush;
            if (brush is SolidColorBrush scb)
                newBrush = new SolidColorBrush(scb.Color);
            else if (brush != null)
                newBrush = brush.CloneCurrentValue();
            else
                newBrush = Brushes.Transparent;

            foreach (var drawing in GetAllGeometryDrawings(dg))
            {
                if (drawing.Brush != null)
                    drawing.Brush = newBrush;

                if (drawing.Pen?.Brush != null)
                    drawing.Pen.Brush = newBrush;
            }

            var img = new DrawingImage(dg);
            img.Freeze(); // 性能好一点

            _reelIconSource = img;
        }

        private void UpdateConnectorIcon(Brush brush)
        {
            // 从连接器模板拷贝一份
            var dg = _connectorTemplate.CloneCurrentValue();

            Brush newBrush;
            if (brush is SolidColorBrush scb)
                newBrush = new SolidColorBrush(scb.Color);
            else if (brush != null)
                newBrush = brush.CloneCurrentValue();
            else
                newBrush = Brushes.Transparent;

            // 递归遍历所有 GeometryDrawing，替换 Brush / Pen.Brush
            foreach (var drawing in GetAllGeometryDrawings(dg))
            {
                if (drawing.Brush != null)
                    drawing.Brush = newBrush;

                if (drawing.Pen?.Brush != null)
                    drawing.Pen.Brush = newBrush;
            }

            var img = new DrawingImage(dg);
            img.Freeze();
            _connectorIconSource = img;
        }


        private static IEnumerable<GeometryDrawing> GetAllGeometryDrawings(Drawing drawing)
        {
            if (drawing is GeometryDrawing gd)
            {
                yield return gd;
            }
            else if (drawing is DrawingGroup group)
            {
                foreach (var child in group.Children)
                {
                    foreach (var childGd in GetAllGeometryDrawings(child))
                    {
                        yield return childGd;
                    }
                }
            }
        }

        public static byte[] ReadPngToBytes(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("文件路径不能为空");

            if (!File.Exists(filePath))
                throw new FileNotFoundException("文件不存在", filePath);

            return File.ReadAllBytes(filePath);
        }

        public void SetConnnectorByFilePath(int CTIndex, string FilePath)
        {
            SetConnectorByByteArry(CTIndex, ReadPngToBytes(FilePath));
        }

        public void SetConnectorByByteArry(int CTIndex, byte[] ByteArry)
        {
            _ctTopImages[CTIndex + 1].ImageSource = ByteArry.ToImageSource();
            RedrawSections();

        }







        #endregion
    }
}
