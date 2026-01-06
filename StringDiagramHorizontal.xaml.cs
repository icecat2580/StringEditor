using StringDiagram.Enums;
using StringDiagram.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Path = System.IO.Path;

namespace StringDiagram
{
    /// <summary>
    /// StringDiagramHorizontal.xaml 的交互逻辑
    /// </summary>
    internal sealed partial class StringDiagramHorizontal : UserControl, StringDiagram.Interfaces.IStringEditor
    {
        private readonly List<List<SectionInfo>> CTs = new List<List<SectionInfo>>();
        private readonly List<SectionRegion> _sectionRegions = new List<SectionRegion>();
        private readonly List<CtTopImageInfo> _ctTopImages = new List<CtTopImageInfo>();

        public event Action<int, int> OnSelectedSectionhandler;

        // 长度方向缩放（X 轴）
        public double MeterToPixel { get; set; } = 1 * 10;


        private int _selectedCTIndex = -1;
        private int _selectedSectionIndex = -1;

        private const double TopIconWidth = 24;
        private const double TopIconHeight = 24;

        // 模板
        private readonly DrawingGroup _reelTemplate;
        private readonly DrawingGroup _connectorTemplate;

        // 实际使用的 ImageSource（每次换色后重新生成）
        private ImageSource _reelIconSource;
        private ImageSource _connectorIconSource;

        public StringDiagramHorizontal()
        {
            InitializeComponent();
            Root.MouseLeftButtonDown += Root_MouseLeftButtonDown;

            _reelTemplate = ((DrawingGroup)FindResource("Drawing.滚筒")).CloneCurrentValue();
            _connectorTemplate = ((DrawingGroup)FindResource("Drawing.连接器")).CloneCurrentValue();
            UpdateReelIcon(ReelBrush ?? Brushes.Black);
            UpdateConnectorIcon(ConnectorBrush ?? Brushes.Black);
        }

        public StringDiagramHorizontal(List<List<SectionInfo>> cts) : this()
        {
            this.CTs = cts ?? new List<List<SectionInfo>>();
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
                typeof(StringDiagramHorizontal),
                new PropertyMetadata(0.5D));


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
                typeof(StringDiagramHorizontal),
                new PropertyMetadata(0.7D));


        #endregion

        #region SectionSelectedBrush
        public static readonly DependencyProperty SectionSelectedBrushProperty =
            DependencyProperty.Register(
                nameof(SectionSelectedBrush),
                typeof(Brush),
                typeof(StringDiagramHorizontal),
                new PropertyMetadata(Brushes.LightYellow));

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
                typeof(StringDiagramHorizontal),
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
                typeof(StringDiagramHorizontal),
                new PropertyMetadata(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0F0E3")), OnReelBrushChanged));

        private static void OnReelBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (StringDiagramHorizontal)d;
            if (c == null) return;

            var brush = e.NewValue as Brush;
            c.UpdateReelIcon(brush);
            c.RedrawSections();
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
            DependencyProperty.Register(
                "ConnectorBrush",
                typeof(Brush),
                typeof(StringDiagramHorizontal),
                new PropertyMetadata(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0F0E3")), OnConnectorBrushChanged));

        private static void OnConnectorBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (StringDiagramHorizontal)d;
            if (c == null) return;

            var brush = e.NewValue as Brush;
            c.UpdateConnectorIcon(brush);
            c.RedrawSections();
        }
        #endregion

        #region WeldBrush
        public static readonly DependencyProperty WeldBrushProperty =
            DependencyProperty.Register(
                nameof(WeldBrush),
                typeof(Brush),
                typeof(StringDiagramHorizontal),
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
                typeof(StringDiagramHorizontal), 
                new PropertyMetadata(Brushes.Black));


        #endregion

        #region InsideBrush
        public static readonly DependencyProperty InsideBrushProperty =
            DependencyProperty.Register(
                nameof(InsideBrush),
                typeof(Brush),
                typeof(StringDiagramHorizontal),
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
                typeof(StringDiagramHorizontal),
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
                typeof(StringDiagramHorizontal),
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
                typeof(StringDiagramHorizontal),
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
                typeof(StringDiagramHorizontal),
                new PropertyMetadata(Brushes.Black));

        public Brush FontBrush
        {
            get => (Brush)GetValue(FontBrushProperty);
            set => SetValue(FontBrushProperty, value);
        }
        #endregion

        #region FontSize
        public double fontSize
        {
            get { return (double)GetValue(fontSizeProperty); }
            set { SetValue(fontSizeProperty, value); }
        }

        public static readonly DependencyProperty fontSizeProperty =
            DependencyProperty.Register(
                "fontSize",
                typeof(double),
                typeof(StringDiagramHorizontal),
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
                typeof(StringDiagramHorizontal),
                new PropertyMetadata("m"));


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
                typeof(StringDiagramHorizontal),
                new PropertyMetadata(true, OnEnableSelectedSectionChanged));

        private static void OnEnableSelectedSectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (StringDiagramHorizontal)d;
            if (c == null) return;

            bool enabled = (bool)e.NewValue;
            if (!enabled)
            {
                // 禁用选中功能时清掉当前选中
                c.SetSelectedSection(-1, -1, false);
            }
        }
        #endregion

        #region ConTainerWidth
        public double ConTainerWidth
        {
            get { return (double)GetValue(ConTainerWidthProperty); }
            set { SetValue(ConTainerWidthProperty, value); }
        }

        public static readonly DependencyProperty ConTainerWidthProperty =
            DependencyProperty.Register(
                "ConTainerWidth",
                typeof(double),
                typeof(StringDiagramHorizontal),
                new PropertyMetadata(600D, OnWidthChanged));

        private static void OnWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (StringDiagramHorizontal)d;
            if (c != null)
            {
                c.Width = (double)e.NewValue;
            }
        }


        #endregion


        #region RulerMode
        public static readonly DependencyProperty RulerModeProperty =
            DependencyProperty.Register(
            nameof(RulerMode),
            typeof(RulerMode),
            typeof(StringDiagramHorizontal),
            new PropertyMetadata(RulerMode.PerCT, OnRulerModeChanged));

        public RulerMode RulerMode
        {
            get => (RulerMode)GetValue(RulerModeProperty);
            set => SetValue(RulerModeProperty, value);
        }

        private static void OnRulerModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (StringDiagramHorizontal)d;
            c?.RedrawSections();   // 模式切换时重画
        }

        #endregion
        #endregion

        #region Finish

        public void InsertSection(
            int CTindex,
            int Sectionindex,
            double length,
            double OuterDiameterOfReelEnd,
            double InnerDiameterOfReelEnd,
            double OuterDiameterOfFreeEnd,
            double InnerDiameterOfFreeEnd)
        {
            if (CTindex < 0 || CTindex >= CTs.Count)
            {
                int ctCount = CTs.Count;
                throw new ArgumentOutOfRangeException(
                    nameof(CTindex),
                    $"CTindex 超出范围，当前 CT 数量为 {ctCount}。");
            }

            var sec = new SectionInfo
            {
                Length = length,
                OuterDiameterOfReelEnd = OuterDiameterOfReelEnd,
                InnerDiameterOfReelEnd = InnerDiameterOfReelEnd,
                OuterDiameterOfFreeEnd = OuterDiameterOfFreeEnd,
                InnerDiameterOfFreeEnd = InnerDiameterOfFreeEnd
            };

            if (Sectionindex < 0 || Sectionindex > CTs[CTindex].Count)
                Sectionindex = CTs[CTindex].Count;

            CTs[CTindex].Insert(Sectionindex, sec);
            RedrawSections();
        }

        public void ClearSection(int CTindex)
        {
            if (CTindex < 0 || CTindex >= CTs.Count)
                return;

            CTs[CTindex].Clear();
            RedrawSections();
        }

        public void RemoveSection(int CTindex, int Sectionindex)
        {
            if (CTindex < 0 || CTindex >= CTs.Count) return;
            if (Sectionindex < 0 || Sectionindex >= CTs[CTindex].Count) return;

            CTs[CTindex].RemoveAt(Sectionindex);
            RedrawSections();
        }

        public void InsertWeld(int CTindex, double Postion)
        {
            if (CTindex < 0 || CTindex >= CTs.Count)
                return;

            var ct = CTs[CTindex];

            double ctLength = 0;
            foreach (var s in ct) ctLength += s.Length;
            if (ctLength <= 0) return;

            const double eps = 1e-6;

            if (Postion <= eps || Postion >= ctLength - eps) return;
            if (Postion < 0 || Postion > ctLength) return;

            double acc = 0;
            for (int i = 0; i < ct.Count; i++)
            {
                var sec = ct[i];
                double nextAcc = acc + sec.Length;

                if (Postion <= nextAcc)
                {
                    double local = Postion - acc;
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

        public void RemoveCT(int ctIndex)
        {
            if (ctIndex < 0 || ctIndex >= CTs.Count)
                return;

            CTs.RemoveAt(ctIndex);
            if (ctIndex < _ctTopImages.Count)
                _ctTopImages.RemoveAt(ctIndex);

            for (int i = 0; i < _ctTopImages.Count; i++)
                _ctTopImages[i].CTIndex = i;

            RedrawSections();
        }

        public void ClearCT()
        {
            CTs.Clear();
            _ctTopImages.Clear();
            RedrawSections();
        }

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

            RedrawSections();
        }

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

        #region 绘图区域获取

        // Canvas 左上为原点，返回：
        //   - 第一个分段上侧矩形的“左下角”
        //   - 最后一个分段下侧矩形的“右下角”
        public void GetDrawIngRegion(out Point LeftBottomPoint, out Point RightBottomPoint)
        {
            LeftBottomPoint = new Point(0, 0);
            RightBottomPoint = new Point(0, 0);

            if (_sectionRegions == null || _sectionRegions.Count == 0)
                return;

            var firstRegion = _sectionRegions.First();
            var topPoly = firstRegion.LeftWall;  // 上侧
            if (topPoly != null && topPoly.Points != null && topPoly.Points.Count > 0)
            {
                double minX = topPoly.Points.Min(p => p.X);
                double maxY = topPoly.Points.Max(p => p.Y);
                LeftBottomPoint = new Point(minX, maxY);
            }
            else
            {
                LeftBottomPoint = new Point(firstRegion.Bounds.Left, firstRegion.Bounds.Bottom);
            }

            var lastRegion = _sectionRegions.Last();
            var bottomPoly = lastRegion.RightWall;  // 下侧
            if (bottomPoly != null && bottomPoly.Points != null && bottomPoly.Points.Count > 0)
            {
                double maxX = bottomPoly.Points.Max(p => p.X);
                double maxY = bottomPoly.Points.Max(p => p.Y);
                RightBottomPoint = new Point(maxX, maxY);
            }
            else
            {
                RightBottomPoint = new Point(lastRegion.Bounds.Right, lastRegion.Bounds.Bottom);
            }
        }

        #endregion


        #endregion

        #region 绘制（水平）

        private void RedrawSections()
        {
            if (Root == null)
                return;

            Root.Children.Clear();
            ruler?.Children.Clear();

            if (CTs.Count == 0)
                return;

            Root.Width = ConTainerWidth;
            ruler.Width = ConTainerWidth;

            double rootWidth = ConTainerWidth;
            double rootHeight = Root.ActualHeight > 0 ? Root.ActualHeight : Root.Height;
            if (rootWidth <= 0 || rootHeight <= 0)
                return;

            double marginLeft = 25;
            double marginRight = 25;
            double marginY = 10;

            double usableWidth = rootWidth - marginLeft - marginRight;
            double usableHeight = rootHeight - 2 * marginY;
            if (usableWidth <= 0 || usableHeight <= 0)
                return;

            _sectionRegions.Clear();

            //统计总长、最大外径、物理壁厚范围 
            double totalLength = 0;
            double maxOuterDiameter = 0;

            double minWall = double.MaxValue; // 物理壁厚 = outer - inner
            double maxWall = double.MinValue;

            foreach (var ct in CTs)
            {
                foreach (var s in ct)
                {
                    totalLength += s.Length;

                    maxOuterDiameter = Math.Max(
                        maxOuterDiameter,
                        Math.Max(s.OuterDiameterOfReelEnd, s.OuterDiameterOfFreeEnd));

                    // 左端壁厚
                    double wallLeft = s.OuterDiameterOfReelEnd - s.InnerDiameterOfReelEnd;
                    if (wallLeft > 0)
                    {
                        minWall = Math.Min(minWall, wallLeft);
                        maxWall = Math.Max(maxWall, wallLeft);
                    }

                    // 右端壁厚
                    double wallRight = s.OuterDiameterOfFreeEnd - s.InnerDiameterOfFreeEnd;
                    if (wallRight > 0)
                    {
                        minWall = Math.Min(minWall, wallRight);
                        maxWall = Math.Max(maxWall, wallRight);
                    }
                }
            }

            if (totalLength <= 0 || maxOuterDiameter <= 0)
                return;

            // 没有有效壁厚就给个默认值，避免除 0
            if (minWall == double.MaxValue)
            {
                minWall = 1;
                maxWall = 1;
            }

            // 长度方向缩放（X 轴）
            double connectorGapPixel = TopIconWidth;                       // 每根管之间水平间隔
            double totalGapPixel = connectorGapPixel * Math.Max(CTs.Count - 1, 0);

            double meterToPixel = MeterToPixel;
            double maxPhysicalWidth = usableWidth - totalGapPixel;
            if (maxPhysicalWidth <= 0)
                return;

            if (meterToPixel * totalLength > maxPhysicalWidth)
            {
                meterToPixel = maxPhysicalWidth / totalLength;
            }

            //  直径缩放（Y 轴）：用可绘高度的一部分
            double pipeCoverage = 0.8;                         // 整根管子占用可绘高度的 80%
            double halfHeightMax = usableHeight * pipeCoverage / 2.0;
            double diameterToPixel = maxOuterDiameter > 0
                ? halfHeightMax / maxOuterDiameter
                : 0;

            double centerY = rootHeight / 2.0;


            const double eps = 1e-6;

            double globalMeter = 0.0; // 从总左端累计长度（米）

            for (int ctIndex = 0; ctIndex < CTs.Count; ctIndex++)
            {
                var ct = CTs[ctIndex];

                double ctOffsetPixel = connectorGapPixel * ctIndex;

                // 确保 _ctTopImages 至少有 ctIndex + 1 项
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

                //左侧图标（滚筒/连接器）
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
                            Stretch = Stretch.Uniform,
                            RenderTransform = new RotateTransform(-90, TopIconWidth / 2.0, TopIconHeight / 2.0)
                        };

                        if (kind == TopIconKind.Reel)
                            img.Source = _reelIconSource;
                        else if (kind == TopIconKind.Connector)
                            img.Source = _connectorIconSource;

                        double iconX;
                        double iconCenterY = centerY;

                        if (ctIndex == 0)
                        {
                            double ctLeftM = globalMeter;
                            double ctLeftX = marginLeft + ctOffsetPixel + ctLeftM * meterToPixel;
                            iconX = ctLeftX - TopIconWidth;
                        }
                        else
                        {
                            double prevRightX = marginLeft + connectorGapPixel * (ctIndex - 1)
                                                + globalMeter * meterToPixel;
                            double currLeftX = marginLeft + ctOffsetPixel
                                               + globalMeter * meterToPixel;
                            double iconCenterX = (prevRightX + currLeftX) / 2.0;
                            iconX = iconCenterX - TopIconWidth / 2.0;
                        }

                        double iconY = iconCenterY - TopIconHeight / 2.0;

                        Canvas.SetLeft(img, iconX);
                        Canvas.SetTop(img, iconY);

                        Root.Children.Add(img);
                        info.Image = img;
                    }
                }

                //画当前 CT 的各分段（水平）
                for (int secIndex = 0; secIndex < ct.Count; secIndex++)
                {
                    var s = ct[secIndex];

                    double leftM = globalMeter;
                    double rightM = globalMeter + s.Length;

                    double leftX = marginLeft + ctOffsetPixel + leftM * meterToPixel;
                    double rightX = marginLeft + ctOffsetPixel + rightM * meterToPixel;

                    // 外半径（像素）
                    double outerLeftR = s.OuterDiameterOfReelEnd * diameterToPixel;
                    double outerRightR = s.OuterDiameterOfFreeEnd * diameterToPixel;

                    // 物理壁厚
                    double wallLeftPhys = s.OuterDiameterOfReelEnd - s.InnerDiameterOfReelEnd;
                    double wallRightPhys = s.OuterDiameterOfFreeEnd - s.InnerDiameterOfFreeEnd;

                    // 映射到 [0.5, 0.7] 的总壁厚占比
                    double tLeft = 0.5;
                    double tRight = 0.5;
                    if (maxWall - minWall > eps)
                    {
                        tLeft = (wallLeftPhys - minWall) / (maxWall - minWall);
                        tRight = (wallRightPhys - minWall) / (maxWall - minWall);
                        tLeft = Math.Max(0.0, Math.Min(1.0, tLeft));
                        tRight = Math.Max(0.0, Math.Min(1.0, tRight));
                    }

                    double wallFracLeftTotal = MinWallFracTotal +
                                               (MaxWallFracTotal - MinWallFracTotal) * tLeft;
                    double wallFracRightTotal = MinWallFracTotal +
                                                (MaxWallFracTotal - MinWallFracTotal) * tRight;

                    // 内半径：R_inner = (1 - 总壁厚比例) * R_outer
                    double innerLeftR = outerLeftR * (1 - wallFracLeftTotal);
                    double innerRightR = outerRightR * (1 - wallFracRightTotal);

                    // 上侧管壁
                    var topPoly = new System.Windows.Shapes.Polygon
                    {
                        Fill = SectionBrush,
                        Stroke = LineBrush,
                        StrokeThickness = LineWeight,
                        Points = new PointCollection
                {
                    new Point(leftX,  centerY - outerLeftR),
                    new Point(rightX, centerY - outerRightR),
                    new Point(rightX, centerY - innerRightR),
                    new Point(leftX,  centerY - innerLeftR)
                }
                    };
                    Root.Children.Add(topPoly);

                    // 下侧管壁
                    var bottomPoly = new System.Windows.Shapes.Polygon
                    {
                        Fill = SectionBrush,
                        Stroke = LineBrush,
                        StrokeThickness = LineWeight,
                        Points = new PointCollection
                {
                    new Point(leftX,  centerY + outerLeftR),
                    new Point(rightX, centerY + outerRightR),
                    new Point(rightX, centerY + innerRightR),
                    new Point(leftX,  centerY + innerLeftR)
                }
                    };
                    Root.Children.Add(bottomPoly);

                    // 管内液体区域
                    double halfStroke = LineWeight / 2.0;

                    // 左右往里收 halfStroke，上下用内半径减去 halfStroke
                    double leftXInside = leftX + halfStroke;
                    double rightXInside = rightX - halfStroke;

                    double innerLeftRInside = Math.Max(0, innerLeftR - halfStroke);
                    double innerRightRInside = Math.Max(0, innerRightR - halfStroke);

                    var insidePoly = new System.Windows.Shapes.Polygon
                    {
                        Fill = InsideBrush,
                        Stroke = null,
                        StrokeThickness = 0,
                        Points = new PointCollection
                            {
                                new Point(leftXInside,  centerY - innerLeftRInside),   // 左上
                                new Point(rightXInside, centerY - innerRightRInside),  // 右上
                                new Point(rightXInside, centerY + innerRightRInside),  // 右下
                                new Point(leftXInside,  centerY + innerLeftRInside)    // 左下
                            }
                    };
                    Root.Children.Add(insidePoly);


                    double maxOuterR = Math.Max(outerLeftR, outerRightR);
                    double topRectY = centerY - maxOuterR;
                    double heightRect = maxOuterR * 2;
                    double widthRect = rightX - leftX;

                    _sectionRegions.Add(new SectionRegion
                    {
                        CTIndex = ctIndex,
                        SectionIndex = secIndex,
                        Bounds = new Rect(leftX, topRectY, widthRect, heightRect),
                        LeftWall = topPoly,
                        RightWall = bottomPoly,
                        InsideShape = insidePoly,
                        InsideOriginalBrush = insidePoly.Fill
                    });

                    // 结构焊缝（竖线）
                    bool isLastSectionOverall =
                        (ctIndex == CTs.Count - 1) && (secIndex == ct.Count - 1);

                    if (!isLastSectionOverall)
                    {
                        double weldX = rightX;
                        double outerRAtWeld = outerRightR;

                        var weldLine = new System.Windows.Shapes.Line
                        {
                            X1 = weldX,
                            X2 = weldX,
                            Y1 = centerY - outerRAtWeld,
                            Y2 = centerY + outerRAtWeld,
                            Stroke = WeldBrush,
                            StrokeThickness = LineWeight
                        };
                        Root.Children.Add(weldLine);
                    }

                    // 自由焊缝
                    if (s.FreeWeldPositions != null && s.FreeWeldPositions.Count > 0)
                    {
                        foreach (var localPos in s.FreeWeldPositions)
                        {
                            if (localPos < 0 || localPos > s.Length)
                                continue;

                            double tt = s.Length <= 0 ? 0 : localPos / s.Length;
                            double outerRAtWeld = outerLeftR + (outerRightR - outerLeftR) * tt;
                            double weldGlobalM = leftM + localPos;
                            double weldX = marginLeft + ctOffsetPixel + weldGlobalM * meterToPixel;

                            var freeWeldLine = new System.Windows.Shapes.Line
                            {
                                X1 = weldX,
                                X2 = weldX,
                                Y1 = centerY - outerRAtWeld,
                                Y2 = centerY + outerRAtWeld,
                                Stroke = FreeWeldBrush,
                                StrokeThickness = LineWeight
                            };
                            Root.Children.Add(freeWeldLine);
                        }
                    }

                    globalMeter = rightM;
                }
            }

            DrawRulerHorizontal(totalLength);
        }



        #region 标尺绘制
        /// <summary>
        /// 在上方 ruler 画水平标尺
        /// RulerMode.Global : 所有 CT 共用一根标尺（总长）
        /// RulerMode.PerCT  : 每根 CT 单独一根标尺（各自长度）
        /// </summary>
        private void DrawRulerHorizontal(double totalLength)
        {
            if (ruler == null || _sectionRegions.Count == 0)
                return;

            ruler.Children.Clear();

            // 根据模式分发
            if (RulerMode == RulerMode.Global)
            {
                DrawRulerHorizontal_Global(totalLength);
            }
            else // RulerMode.PerCT
            {
                DrawRulerHorizontal_PerCT();
            }
        }


        /// <summary>
        /// 全量模式：所有 CT 共用一根标尺，右端 0，左端 totalLength
        /// </summary>
        private void DrawRulerHorizontal_Global(double totalLength)
        {
            double rulerWidth = Root.ActualWidth > 0 ? Root.ActualWidth : ConTainerWidth;
            double rulerHeight = ruler.ActualHeight > 0 ? ruler.ActualHeight : 30;

            ruler.Width = rulerWidth;
            ruler.Height = rulerHeight;

            // 整个管柱绘制区域的最左 / 最右 X
            double xLeft = _sectionRegions.Min(r => r.Bounds.Left);
            double xRight = _sectionRegions.Max(r => r.Bounds.Right);

            double lineY = rulerHeight;
            double tickLength = 6;
            double tickStartY = lineY;
            double tickEndY = lineY - tickLength;
            double textGap = 2;

            // 主水平线
            var mainLine = new System.Windows.Shapes.Line
            {
                X1 = xLeft,
                X2 = xRight,
                Y1 = lineY,
                Y2 = lineY,
                Stroke = LineBrush,
                StrokeThickness = 1
            };
            ruler.Children.Add(mainLine);

            // valueFromRight：从右端起算的长度值（0 在右边）
            void DrawTick(double x, double valueFromRight)
            {
                var tick = new System.Windows.Shapes.Line
                {
                    X1 = x,
                    X2 = x,
                    Y1 = tickStartY,
                    Y2 = tickEndY,
                    Stroke = LineBrush,
                    StrokeThickness = 1
                };
                ruler.Children.Add(tick);

                var tb = new TextBlock
                {
                    Text = valueFromRight.ToString("0.0") + DisplayUnit,
                    Foreground = FontBrush,
                    FontSize = fontSize
                };

                tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                double textWidth = tb.DesiredSize.Width;

                Canvas.SetLeft(tb, x - textWidth / 2.0);
                Canvas.SetTop(tb, tickEndY - tb.FontSize - textGap);

                ruler.Children.Add(tb);
            }

            // 右端 0
            DrawTick(xRight, 0.0);

            // 中间刻度：用分段右边界的 X 当作刻度位置
            double accFromLeft = 0.0;
            for (int ctIndex = 0; ctIndex < CTs.Count; ctIndex++)
            {
                var ct = CTs[ctIndex];
                for (int secIndex = 0; secIndex < ct.Count; secIndex++)
                {
                    var sec = ct[secIndex];
                    accFromLeft += sec.Length;

                    if (accFromLeft <= 0 || accFromLeft >= totalLength)
                        continue;

                    var region = _sectionRegions
                        .Find(r => r.CTIndex == ctIndex && r.SectionIndex == secIndex);
                    if (region == null)
                        continue;

                    double x = region.Bounds.Right;
                    double valueFromRight = totalLength - accFromLeft;

                    DrawTick(x, valueFromRight);
                }
            }

            // 左端 totalLength
            DrawTick(xLeft, totalLength);
        }





        /// <summary>
        /// 分管模式：每根 CT 单独一根标尺，右端 0，左端为该 CT 自己的总长
        /// </summary>
        private void DrawRulerHorizontal_PerCT()
        {
            double rulerWidth = Root.ActualWidth > 0 ? Root.ActualWidth : ConTainerWidth;
            double rulerHeight = ruler.ActualHeight > 0 ? ruler.ActualHeight : 30;

            ruler.Width = rulerWidth;
            ruler.Height = rulerHeight;

            double lineY = rulerHeight;
            double tickLength = 6;
            double tickStartY = lineY;
            double tickEndY = lineY - tickLength;
            double textGap = 2;

            // 画一条刻度 + 文本（valueFromRight：从右端起算）
            void DrawTick(double x, double valueFromRight)
            {
                var tick = new System.Windows.Shapes.Line
                {
                    X1 = x,
                    X2 = x,
                    Y1 = tickStartY,
                    Y2 = tickEndY,
                    Stroke = LineBrush,
                    StrokeThickness = 1
                };
                ruler.Children.Add(tick);

                var tb = new TextBlock
                {
                    Text = valueFromRight.ToString("0.0") + DisplayUnit,
                    Foreground = FontBrush,
                    FontSize = fontSize
                };

                tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                double textWidth = tb.DesiredSize.Width;

                Canvas.SetLeft(tb, x - textWidth / 2.0);
                Canvas.SetTop(tb, tickEndY - tb.FontSize - textGap);

                ruler.Children.Add(tb);
            }

            // 对每一根 CT 单独画一根标尺
            for (int ctIndex = 0; ctIndex < CTs.Count; ctIndex++)
            {
                var ct = CTs[ctIndex];
                if (ct == null || ct.Count == 0)
                    continue;

                // 该 CT 的所有分段区域
                var regionsOfCt = _sectionRegions
                    .Where(r => r.CTIndex == ctIndex)
                    .ToList();

                if (regionsOfCt.Count == 0)
                    continue;

                double xLeft = regionsOfCt.Min(r => r.Bounds.Left);
                double xRight = regionsOfCt.Max(r => r.Bounds.Right);

                // 该 CT 自己的总长度
                double ctLength = 0.0;
                foreach (var sec in ct)
                    ctLength += sec.Length;

                if (ctLength <= 0)
                    continue;

                // 该 CT 的主水平线
                var mainLine = new System.Windows.Shapes.Line
                {
                    X1 = xLeft,
                    X2 = xRight,
                    Y1 = lineY,
                    Y2 = lineY,
                    Stroke = LineBrush,
                    StrokeThickness = 1
                };
                ruler.Children.Add(mainLine);

                // 右端 0
                DrawTick(xRight, 0.0);

                // 中间刻度：只累加当前 CT 的长度
                double accFromLeft = 0.0;
                for (int secIndex = 0; secIndex < ct.Count; secIndex++)
                {
                    var sec = ct[secIndex];
                    accFromLeft += sec.Length;

                    if (accFromLeft <= 0 || accFromLeft >= ctLength)
                        continue;

                    var region = regionsOfCt
                        .Find(r => r.SectionIndex == secIndex);
                    if (region == null)
                        continue;

                    double x = region.Bounds.Right;
                    double valueFromRight = ctLength - accFromLeft;
                        
                    DrawTick(x, valueFromRight);
                }

                // 左端 = 该 CT 的总长度
                DrawTick(xLeft, ctLength);
            }
        }

        #endregion


        #endregion

        #region 选中逻辑 & 鼠标事件

        public void SetSelectedSection(int ctIndex, int sectionIndex, bool raiseEvent)
        {
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

            _selectedCTIndex = ctIndex;
            _selectedSectionIndex = sectionIndex;

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

                    if (raiseEvent)
                        OnSelectedSectionhandler?.Invoke(ctIndex, sectionIndex);
                }
            }
        }

        private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!EnableSelectedSection)
                return;

            Point p = e.GetPosition(Root);

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

        #endregion

        #region 工具：更新图标颜色

        private void UpdateReelIcon(Brush brush)
        {
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
            img.Freeze();
            _reelIconSource = img;
        }

        private void UpdateConnectorIcon(Brush brush)
        {
            var dg = _connectorTemplate.CloneCurrentValue();

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


        #endregion
    }
}
