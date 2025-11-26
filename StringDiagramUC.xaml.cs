using StringDiagram.Enums;
using StringDiagram.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Orientation = StringDiagram.Enums.Orientation;

namespace StringDiagram
{
    /// <summary>
    /// StringDiagramUC.xaml 的交互逻辑
    /// </summary>
    public partial class StringDiagramUC : UserControl,IStringEditor
    {
        private StringDiagramVertical _Vertical;
        private StringDiagramHorizontal _Horizontal;
        private IStringEditor _current=>Orientation==Orientation.Horizontal?(IStringEditor)_Horizontal:(IStringEditor)_Vertical;
        public StringDiagramUC()
        {
            InitializeComponent();
            CreateChildren();
            UpdateContent();
        }

        private void CreateChildren()
        {
            _Vertical = new StringDiagramVertical();
            _Horizontal = new StringDiagramHorizontal();

            // 事件转发：内部控件选中分段时，冒泡到 Host
            _Vertical.OnSelectedSectionhandler += (ct, sec) =>
                OnSelectedSectionhandler?.Invoke(ct, sec);
            _Horizontal.OnSelectedSectionhandler += (ct, sec) =>
                OnSelectedSectionhandler?.Invoke(ct, sec);
        }

        private void UpdateContent()
        {
            if (PART_Content == null) return;

            PART_Content.Content = Orientation == Orientation.Vertical
                ? (Control)_Vertical
                : (Control)_Horizontal;
        }


        #region Orientation 依赖属性

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
                typeof(StringDiagramUC),
                new PropertyMetadata(0.5D,OnMinWallFracTotalChanged));

        private static void OnMinWallFracTotalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var host = (StringDiagramUC)d;
            var value = (double)e.NewValue;

            host._Vertical?.SetValue(StringDiagramVertical.MinWallFracTotalProperty, value);
            host._Horizontal?.SetValue(StringDiagramHorizontal.MinWallFracTotalProperty, value);
        }
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
                typeof(StringDiagramUC),
                new PropertyMetadata(0.7D,OnMaxWallFracTotalChanged));

        private static void OnMaxWallFracTotalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var host = (StringDiagramUC)d;
            var value =(double)e.NewValue;

            host._Vertical?.SetValue(StringDiagramVertical.MaxWallFracTotalProperty, value);
            host._Horizontal?.SetValue(StringDiagramHorizontal.MaxWallFracTotalProperty, value);
        }
        #endregion


        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(
                nameof(Orientation),
                typeof(Orientation),
                typeof(StringDiagramUC),
                new PropertyMetadata(Orientation.Horizontal, OnOrientationChanged));

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        private static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var host = (StringDiagramUC)d;
            host.UpdateContent();

            if (host.Orientation == Orientation.Vertical && host._Vertical != null)
            {
                host._Vertical.ConTainerHeight = host.ConTainerHeight;
            }
            else if (host.Orientation == Orientation.Horizontal && host._Horizontal != null)
            {
                host._Horizontal.ConTainerWidth = host.ConTainerWidth;
            }
        }

        #endregion


        #region 颜色 & 样式依赖属性（转发到内部控件）

        // 分段选中颜色
        public static readonly DependencyProperty SectionSelectedBrushProperty =
            DependencyProperty.Register(
                nameof(SectionSelectedBrush),
                typeof(Brush),
                typeof(StringDiagramUC),
                new PropertyMetadata(Brushes.LightYellow, OnSectionSelectedBrushChanged));

        public Brush SectionSelectedBrush
        {
            get => (Brush)GetValue(SectionSelectedBrushProperty);
            set => SetValue(SectionSelectedBrushProperty, value);
        }

        private static void OnSectionSelectedBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var host = (StringDiagramUC)d;
            var brush = e.NewValue as Brush;

            host._Vertical?.SetValue(StringDiagramVertical.SectionSelectedBrushProperty, brush);
            host._Horizontal?.SetValue(StringDiagramHorizontal.SectionSelectedBrushProperty, brush);
        }


        // 分段颜色
        public static readonly DependencyProperty SectionBrushProperty =
            DependencyProperty.Register(
                nameof(SectionBrush),
                typeof(Brush),
                typeof(StringDiagramUC),
                new PropertyMetadata(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0F0E3")),
                                     OnSectionBrushChanged));

        public Brush SectionBrush
        {
            get => (Brush)GetValue(SectionBrushProperty);
            set => SetValue(SectionBrushProperty, value);
        }

        private static void OnSectionBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var host = (StringDiagramUC)d;
            var brush = e.NewValue as Brush;

            host._Vertical?.SetValue(StringDiagramVertical.SectionBrushProperty, brush);
            host._Horizontal?.SetValue(StringDiagramHorizontal.SectionBrushProperty, brush);
        }


        // 滚筒颜色
        public static readonly DependencyProperty ReelBrushProperty =
            DependencyProperty.Register(
                nameof(ReelBrush),
                typeof(Brush),
                typeof(StringDiagramUC),
                new PropertyMetadata(Brushes.Transparent, OnReelBrushChanged));

        public Brush ReelBrush
        {
            get => (Brush)GetValue(ReelBrushProperty);
            set => SetValue(ReelBrushProperty, value);
        }

        private static void OnReelBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var host = (StringDiagramUC)d;
            var brush = e.NewValue as Brush;

            host._Vertical?.SetValue(StringDiagramVertical.ReelBrushProperty, brush);
            host._Horizontal?.SetValue(StringDiagramHorizontal.ReelBrushProperty, brush);
        }


        // 连接器颜色
        public static readonly DependencyProperty ConnectorBrushProperty =
            DependencyProperty.Register(
                nameof(ConnectorBrush),
                typeof(Brush),
                typeof(StringDiagramUC),
                new PropertyMetadata(Brushes.Red, OnConnectorBrushChanged));

        public Brush ConnectorBrush
        {
            get => (Brush)GetValue(ConnectorBrushProperty);
            set => SetValue(ConnectorBrushProperty, value);
        }

        private static void OnConnectorBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var host = (StringDiagramUC)d;
            var brush = e.NewValue as Brush;

            host._Vertical?.SetValue(StringDiagramVertical.ConnectorBrushProperty, brush);
            host._Horizontal?.SetValue(StringDiagramHorizontal.ConnectorBrushProperty, brush);
        }


        // 焊缝颜色
        public static readonly DependencyProperty WeldBrushProperty =
            DependencyProperty.Register(
                nameof(WeldBrush),
                typeof(Brush),
                typeof(StringDiagramUC),
                new PropertyMetadata(Brushes.Black, OnWeldBrushChanged));

        public Brush WeldBrush
        {
            get => (Brush)GetValue(WeldBrushProperty);
            set => SetValue(WeldBrushProperty, value);
        }

        private static void OnWeldBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var host = (StringDiagramUC)d;
            var brush = e.NewValue as Brush;

            host._Vertical?.SetValue(StringDiagramVertical.WeldBrushProperty, brush);
            host._Horizontal?.SetValue(StringDiagramHorizontal.WeldBrushProperty, brush);
        }


        //自由焊缝颜色


        public Brush FreeWeldBrush
        {
            get { return (Brush)GetValue(FreeWeldBrushProperty); }
            set { SetValue(FreeWeldBrushProperty, value); }
        }

        public static readonly DependencyProperty FreeWeldBrushProperty =
            DependencyProperty.Register(
                "FreeWeldBrush",
                typeof(Brush), 
                typeof(StringDiagramUC), 
                new PropertyMetadata(Brushes.Black,OnFreeWeldBrushChanged));


        private static void OnFreeWeldBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var host = (StringDiagramUC)d;
            var brush = e.NewValue as Brush;

            host._Vertical?.SetValue(StringDiagramVertical.FreeWeldBrushProperty, brush);
            host._Horizontal?.SetValue(StringDiagramHorizontal.FreeWeldBrushProperty, brush);
        }


        // 管内颜色
        public static readonly DependencyProperty InsideBrushProperty =
            DependencyProperty.Register(
                nameof(InsideBrush),
                typeof(Brush),
                typeof(StringDiagramUC),
                new PropertyMetadata(Brushes.LightGreen, OnInsideBrushChanged));

        public Brush InsideBrush
        {
            get => (Brush)GetValue(InsideBrushProperty);
            set => SetValue(InsideBrushProperty, value);
        }

        private static void OnInsideBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var host = (StringDiagramUC)d;
            var brush = e.NewValue as Brush;

            host._Vertical?.SetValue(StringDiagramVertical.InsideBrushProperty, brush);
            host._Horizontal?.SetValue(StringDiagramHorizontal.InsideBrushProperty, brush);
        }


        // 容器背景色（顺便绑定到 Host 根 Grid 的 Background）
        public static readonly DependencyProperty ContainerBrushProperty =
            DependencyProperty.Register(
                nameof(ContainerBrush),
                typeof(Brush),
                typeof(StringDiagramUC),
                new PropertyMetadata(Brushes.Transparent, OnContainerBrushChanged));

        public Brush ContainerBrush
        {
            get => (Brush)GetValue(ContainerBrushProperty);
            set => SetValue(ContainerBrushProperty, value);
        }

        private static void OnContainerBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var host = (StringDiagramUC)d;
            var brush = e.NewValue as Brush;

            // 里面两个控件的 ContainerBrush 也一起改
            host._Vertical?.SetValue(StringDiagramVertical.ContainerBrushProperty, brush);
            host._Horizontal?.SetValue(StringDiagramHorizontal.ContainerBrushProperty, brush);
        }


        // 绘制线条颜色
        public static readonly DependencyProperty LineBrushProperty =
            DependencyProperty.Register(
                nameof(LineBrush),
                typeof(Brush),
                typeof(StringDiagramUC),
                new PropertyMetadata(Brushes.Black, OnLineBrushChanged));

        public Brush LineBrush
        {
            get => (Brush)GetValue(LineBrushProperty);
            set => SetValue(LineBrushProperty, value);
        }

        private static void OnLineBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var host = (StringDiagramUC)d;
            var brush = e.NewValue as Brush;

            host._Vertical?.SetValue(StringDiagramVertical.LineBrushProperty, brush);
            host._Horizontal?.SetValue(StringDiagramHorizontal.LineBrushProperty, brush);
        }


        // 线宽
        public static readonly DependencyProperty LineWeightProperty =
            DependencyProperty.Register(
                nameof(LineWeight),
                typeof(double),
                typeof(StringDiagramUC),
                new PropertyMetadata(1d, OnLineWeightChanged));

        public double LineWeight
        {
            get => (double)GetValue(LineWeightProperty);
            set => SetValue(LineWeightProperty, value);
        }

        private static void OnLineWeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var host = (StringDiagramUC)d;
            var v = (double)e.NewValue;

            host._Vertical?.SetValue(StringDiagramVertical.LineWeightProperty, v);
            host._Horizontal?.SetValue(StringDiagramHorizontal.LineWeightProperty, v);
        }


        // 标尺字体颜色
        public static readonly DependencyProperty FontBrushProperty =
            DependencyProperty.Register(
                nameof(FontBrush),
                typeof(Brush),
                typeof(StringDiagramUC),
                new PropertyMetadata(Brushes.Black, OnFontBrushChanged));

        public Brush FontBrush
        {
            get => (Brush)GetValue(FontBrushProperty);
            set => SetValue(FontBrushProperty, value);
        }

        private static void OnFontBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var host = (StringDiagramUC)d;
            var brush = e.NewValue as Brush;

            host._Vertical?.SetValue(StringDiagramVertical.FontBrushProperty, brush);
            host._Horizontal?.SetValue(StringDiagramHorizontal.FontBrushProperty, brush);
        }


        // 标尺字体大小
        public static readonly DependencyProperty fontSizeProperty =
            DependencyProperty.Register(
                nameof(fontSize),
                typeof(double),
                typeof(StringDiagramUC),
                new PropertyMetadata(10d, OnFontSizeChanged));

        public double fontSize
        {
            get => (double)GetValue(fontSizeProperty);
            set => SetValue(fontSizeProperty, value);
        }

        private static void OnFontSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var host = (StringDiagramUC)d;
            var v = (double)e.NewValue;

            host._Vertical?.SetValue(StringDiagramVertical.fontSizeProperty, v);
            host._Horizontal?.SetValue(StringDiagramHorizontal.fontSizeProperty, v);
        }





        public string DisplayUnit
        {
            get { return (string)GetValue(DisplayUnitProperty); }
            set { SetValue(DisplayUnitProperty, value); }
        }

        public static readonly DependencyProperty DisplayUnitProperty =
            DependencyProperty.Register(
                "DisplayUnit", 
                typeof(string),
                typeof(StringDiagramUC), 
                new PropertyMetadata("m",OnDisplayUnitChanged));

        private static void OnDisplayUnitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var host = (StringDiagramUC)d;
            var v = (string)e.NewValue;

            host._Vertical?.SetValue(StringDiagramVertical.DisplayUnitProperty, v);
            host._Horizontal?.SetValue(StringDiagramHorizontal.DisplayUnitProperty, v);
        }


        // 是否启用分段选中功能
        public static readonly DependencyProperty EnableSelectedSectionProperty =
            DependencyProperty.Register(
                "EnableSelectedSection",
                typeof(bool), 
                typeof(StringDiagramUC), 
                new PropertyMetadata(true, OnEnableSelectedSectionChanged));



        public bool EnableSelectedSection
        {
            get { return (bool)GetValue(EnableSelectedSectionProperty); }
            set { SetValue(EnableSelectedSectionProperty, value); }
        }

        private static void OnEnableSelectedSectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var host = (StringDiagramUC)d;
            var v = (bool)e.NewValue;

            host._Vertical?.SetValue(StringDiagramVertical.EnableSelectedSectionProperty, v);
            host._Horizontal?.SetValue(StringDiagramHorizontal.EnableSelectedSectionProperty, v);
        }



        // 容器宽度
        public static readonly DependencyProperty ConTainerHeightProperty =
            DependencyProperty.Register(
                "ConTainerHeight", 
                typeof(double),
                typeof(StringDiagramUC), 
                new PropertyMetadata(600D,OnContainerHeightChanged));

        private static void OnContainerHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var host = (StringDiagramUC)d;
            var v = (double)e.NewValue;

            host._Vertical?.SetValue(StringDiagramVertical.ConTainerHeightProperty, v);
        }

        public double ConTainerHeight
        {
            get { return (double)GetValue(ConTainerHeightProperty); }
            set { SetValue(ConTainerHeightProperty, value); }
        }




        // 容器宽度
        public double ConTainerWidth
        {
            get { return (double)GetValue(ConTainerWidthProperty); }
            set { SetValue(ConTainerWidthProperty, value); }
        }

        public static readonly DependencyProperty ConTainerWidthProperty =
            DependencyProperty.Register(
                "ConTainerWidth", 
                typeof(double), 
                typeof(StringDiagramUC), 
                new PropertyMetadata(600D, OnConTainerWidthChanged));

        private static void OnConTainerWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var host = (StringDiagramUC)d;
            var v = (double)e.NewValue;

            host._Horizontal?.SetValue(StringDiagramHorizontal.ConTainerWidthProperty, v);
        }



        #endregion




        #region IStringEditor 实现 —— 全部转发给 _current

        public event Action<int, int> OnSelectedSectionhandler;

        public void InsertSection(int ctIndex, int sectionIndex,
            double length,
            double outerReel, double innerReel,
            double outerFree, double innerFree)
            => _current.InsertSection(ctIndex, sectionIndex, length, outerReel, innerReel, outerFree, innerFree);

        public void ClearSection(int ctIndex)
            => _current.ClearSection(ctIndex);

        public void RemoveSection(int ctIndex, int sectionIndex)
            => _current.RemoveSection(ctIndex, sectionIndex);

        public void InsertWeld(int ctIndex, double position)
            => _current.InsertWeld(ctIndex, position);

        public void RemoveCT(int ctIndex)
            => _current.RemoveCT(ctIndex);

        public void ClearCT()
            => _current.ClearCT();

        public void InsertCT(int ctIndex)
            => _current.InsertCT(ctIndex);

        public void AppendCT(int count = 1)
            => _current.AppendCT(count);


        public void ShowReel(int CTindex,bool IsVisible)
            =>_current.ShowReel(CTindex,IsVisible);

        public void ShowConnector(int CTindex, bool IsVisible)
            => _current.ShowConnector(CTindex, IsVisible);
        public void ExportImage(double width, double height, string path)
            => _current.ExportImage(width, height, path);

        public void GetDrawIngRegion(out Point leftBottom, out Point rightBottom)
            => _current.GetDrawIngRegion(out leftBottom, out rightBottom);

        #endregion


     
    }
}
