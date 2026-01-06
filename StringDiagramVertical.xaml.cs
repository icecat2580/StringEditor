using StringDiagram.Enums;
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
        public event Action<int, int> OnSelectedSectionhandler;
        // 纵向缩放
        public double MeterToPixel { get; set; } = 0.5 * 10;

        //当前选中的CT索引
        private int _selectedCTIndex = -1;
        //当前选中的分段索引
        private int _selectedSectionIndex = -1;

        private const double TopIconWidth = 24;
        private const double TopIconHeight = 24;


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
            new PropertyMetadata(RulerMode.PerCT, OnRulerModeChanged));

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
        #endregion



        #region 绘制
        private void RedrawSections()
        {
            if (Root == null)
                return;

            Root.Children.Clear();
            if (ruler != null)
                ruler.Children.Clear();

            if (CTs.Count == 0)
                return;

            Root.Height = ConTainerHeight;
            ruler.Height = ConTainerHeight;

            // 根容器尺寸
            double rootWidth = Root.ActualWidth;
            if (rootWidth <= 0)
                rootWidth = Root.Width;

            double rootHeight = ConTainerHeight;
            if (rootWidth <= 0 || rootHeight <= 0)
                return;

            double marginX = 0;
            double marginTop = 25;   // 顶部留给图标
            double marginBottom =25; // 底部不要留空
            double usableWidth = rootWidth - 2 * marginX;
            double usableHeight = rootHeight - marginBottom - marginTop;
            if (usableWidth <= 0 || usableHeight <= 0)
                return;

            _sectionRegions.Clear();

            //统计总长、最大外径、最大/最小壁厚

            double totalLength = 0;
            double maxOuterDiameter = 0;

            // 壁厚（按半径算）：outerRadius - innerRadius
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

                    // 上端壁厚
                    double wallTop = s.OuterDiameterOfReelEnd - s.InnerDiameterOfReelEnd;
                    if (wallTop > 0)
                    {
                        minWall = Math.Min(minWall, wallTop);
                        maxWall = Math.Max(maxWall, wallTop);
                    }

                    // 下端壁厚
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

            // 如果所有壁厚都一样/或都是 0 就给一个默认值，避免除 0
            if (minWall == double.MaxValue)
            {
                minWall = 1;
                maxWall = 1;
            }

            // 纵向缩放

            double connectorGapPixel = TopIconHeight; // 管子之间的间隔（像素）
            double totalGapPixel = connectorGapPixel * Math.Max(CTs.Count - 1, 0);

            double meterToPixel = MeterToPixel;
            double maxPhysicalHeight = usableHeight - totalGapPixel;
            if (maxPhysicalHeight <= 0)
                return;

            if (meterToPixel * totalLength > maxPhysicalHeight)
            {
                meterToPixel = maxPhysicalHeight / totalLength;
            }

            //横向缩放（最大外径占可用宽度的固定比例）
            double pipeCoverage =0.8;//绘图区域占比
            double halfWidthMax = usableWidth * pipeCoverage / 2.0;

            double diameterToPixel = maxOuterDiameter > 0
                ? halfWidthMax / maxOuterDiameter
                : 0;

            double centerX = rootWidth / 2.0;

            // 准备焊缝刻度数据
            var structuralWeldFromTop = new List<double>();
            double globalMeter = 0.0; // 从总顶端累计的长度

            const double eps = 1e-6;

            for (int ctIndex = 0; ctIndex < CTs.Count; ctIndex++)
            {
                var ct = CTs[ctIndex];

                double ctOffsetPixel = connectorGapPixel * ctIndex;

                // 确保 _ctTopImages 至少有 ctIndex+1 项
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

                //顶部图标
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
                            img.Source = _connectorIconSource;

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

                    double topY = marginTop + topM * meterToPixel + ctOffsetPixel;
                    double bottomY = marginTop + bottomM * meterToPixel + ctOffsetPixel;

                    // 外半径（像素）
                    double outerTopR = s.OuterDiameterOfReelEnd * diameterToPixel;
                    double outerBotR = s.OuterDiameterOfFreeEnd * diameterToPixel;

                    // 物理壁厚（按半径）
                    double wallTopPhys = s.OuterDiameterOfReelEnd - s.InnerDiameterOfReelEnd;
                    double wallBotPhys = s.OuterDiameterOfFreeEnd - s.InnerDiameterOfFreeEnd;

                    // 把物理壁厚 [minWall, maxWall] 映射到总壁厚比例 [0.4, 0.6]
                    double tTop = 0.5;
                    double tBot = 0.5;
                    if (maxWall - minWall > eps)
                    {
                        tTop = (wallTopPhys - minWall) / (maxWall - minWall);
                        tBot = (wallBotPhys - minWall) / (maxWall - minWall);
                        tTop = Math.Max(0.0, Math.Min(1.0, tTop));
                        tBot = Math.Max(0.0, Math.Min(1.0, tBot));
                    }

                    // 总壁厚占直径的比例
                    double wallFracTopTotal = MinWallFracTotal +
                                              (MaxWallFracTotal - MinWallFracTotal) * tTop;
                    double wallFracBotTotal = MinWallFracTotal +
                                              (MaxWallFracTotal - MinWallFracTotal) * tBot;

                    // 内半径：R_inner = (1 - 壁厚总比例) * R_outer
                    // 说明：壁厚总比例 = 总壁厚 / 直径 = 单侧壁厚 / 外半径
                    //       单侧壁厚 = wallFracTotal * R_outer
                    //       R_inner = R_outer - 单侧壁厚
                    double innerTopR = outerTopR * (1 - wallFracTopTotal);
                    double innerBotR = outerBotR * (1 - wallFracBotTotal);

                    //两侧管壁
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

                    //管内液体区域 
                    
                    double halfStroke = LineWeight / 2.0;// 线宽一半，用于补偿
                    // 把液体区域整体往里缩 halfStroke
                    double topYInside = topY + halfStroke;
                    double bottomYInside = bottomY - halfStroke;

                    double innerTopRInside = Math.Max(0, innerTopR - halfStroke);
                    double innerBotRInside = Math.Max(0, innerBotR - halfStroke);

                    // 管内液体区域
                    var insidePoly = new System.Windows.Shapes.Polygon
                    {
                        Fill = InsideBrush,
                        Stroke = null,
                        StrokeThickness = 0,
                        Points = new PointCollection
                        {
                            new Point(centerX - innerTopRInside, topYInside),    // 左上
                            new Point(centerX - innerBotRInside, bottomYInside), // 左下
                            new Point(centerX + innerBotRInside, bottomYInside), // 右下
                            new Point(centerX + innerTopRInside, topYInside)     // 右上
                        }
                    };
                    Root.Children.Add(insidePoly);


                    // 实例引用
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

                    //结构焊缝 
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

                    //自由焊缝
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

                    globalMeter = bottomM; // 下一段
                }
            }

            // 画右侧标尺
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
            double rulerWidth = ruler.ActualWidth;
            double rulerHeight = Root.ActualHeight;

            if (rulerWidth <= 0) rulerWidth = 50;
            if (rulerHeight <= 0) rulerHeight = Root.Height > 0 ? Root.Height : ConTainerHeight;

            ruler.Width = rulerWidth;
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
        }



        /// <summary>
        /// 分管模式：每根 CT 自己一根标尺，0 从各自底部开始
        /// </summary>
        private void DrawRuler_PerCT()
        {
            double rulerWidth = ruler.ActualWidth;
            double rulerHeight = Root.ActualHeight;

            if (rulerWidth <= 0) rulerWidth = 50;
            if (rulerHeight <= 0) rulerHeight = Root.Height > 0 ? Root.Height : ConTainerHeight;

            ruler.Width = rulerWidth;
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
        }

        #endregion





        /// <summary>
        /// 设置选中效果
        /// </summary>
        /// <param name="ctIndex">连续管索引</param>
        /// <param name="sectionIndex">分段索引</param>
        /// <param name="raiseEvent">是否触发委托</param>
        public void SetSelectedSection(int ctIndex, int sectionIndex, bool raiseEvent=false)
        {
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

                    if (raiseEvent)
                        OnSelectedSectionhandler?.Invoke(ctIndex, sectionIndex);
                }
            }
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

        #endregion

        #region 工具
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



        #endregion
    }
}
