using StringDiagram.Enums;
using System;
using System.Windows;
using System.Windows.Media;

namespace StringDiagram.Interfaces
{

    public interface IStringEditor
    {
        //分段选中颜色
        Brush SectionSelectedBrush { get; set; }
        //分段颜色
        Brush SectionBrush { get; set; }
        //滚筒颜色
        Brush ReelBrush { get; set; }
        //连接器颜色
        Brush ConnectorBrush { get; set; }
        //结构焊缝颜色
        Brush WeldBrush { get; set; }
        //自由焊缝颜色
        Brush FreeWeldBrush { get; set; }
        //管内颜色
        Brush InsideBrush { get; set; }
        //容器背景色
        Brush ContainerBrush { get; set; }
        //绘制线条颜色
        Brush LineBrush { get; set; }
        //绘制线条颜色
        double LineWeight { get; set; }
        //标尺字体颜色
        Brush FontBrush { get; set; }
        //标尺字体大小
        double fontSize { get; set; }
        //显示单位
        string DisplayUnit { get; set; }
        //标尺绘制模式
        RulerMode RulerMode { get; set; }
        //是否启用分选选中功能
        bool EnableSelectedSection { get; set; }
        
        //最小壁厚系数
        double MinWallFracTotal { get; set; }
        //最大壁厚系数
        double MaxWallFracTotal { get; set; }

        //选中分段时触发委托
        event Action<int, int> OnSelectedSectionhandler;

        //分段选中
        void SetSelectedSection(int CTindex, int Sectionindex,bool RaiseEvent=false);
        //增加分段
        void InsertSection(int Stindex,int Sectionindex, double length, double OuterDiameterOfReelEnd, double InnerDiameterOfReelEnd, double OuterDiameterOfFreeEnd, double InnerDiameterOfFreeEnd);
        //删除分段
        void RemoveSection(int CTindex,int index);
        //清空分段
        void ClearSection(int CtIndex);

        //删除连续管
        void RemoveCT(int CTindex);
        //追加连续管
        void AppendCT(int CTcount);
        //插入连续管
        void InsertCT(int CTindex);   
        //清空
        void ClearCT();

        //插入自由焊缝
        void InsertWeld(int CTindex,double Postion);
        //显示与隐藏滚筒
        void ShowReel(int Ctindex, bool IsVisible);
        //显示与隐藏连接器
        void ShowConnector(int CTindex, bool IsVisible);
        //导出为图片
        void ExportImage(double width, double height, string ImagePath);
        //获取绘图区域
        void   GetDrawIngRegion(out Point LeftBottomPoint,out Point RightBottomPoint);
        //设置左侧边距
        void SetLeftMargin(double LeftWidth);   
        //设置右侧边距
        void SetRightMargin(double RightWidth);
        //设置显示单位
        void SetDisplayUnit(string unit);
        //根据文件路径设置当前连接器样式
        void SetConnnectorByFilePath(int CTIndex, string FilePath);
        //根据字节数组设置连接器样式
        void SetConnectorByByteArry(int CTIndex,byte[] ByteArry);
        //是否切换到调试模式(true显示绘图区域，false不显示绘图区域)
        void SetDebugMode(bool DebugMode);
        //切换到降额显示模式，默认为false
        void SetZoneMode(bool IsZoneMode);
        /// <summary>
        /// 插入降额信息
        /// </summary>
        /// <param name="StartPos">从滚筒端开始的起始位置</param>
        /// <param name="EndPos">从滚筒端开始的结束位置</param>
        /// <param name="ZoneValue">当前的降额值（0到1）</param>
        void InsertZone(double StartPos,double EndPos,double ZoneValue);
        /// <summary>
        /// 设置选中的降额段（与连续管分段选中互斥）。
        /// 若参数无效（如负数、NaN、ZoneValue 不在 0～1、区间长度为零等），则取消降额选中。
        /// </summary>
        void SelectZone(double StartPos,double EndPos,double ZoneValue);
        /// <summary>清空所有降额数据与绘制（连续管内降额带、Zone 示意图、降额选中效果）。</summary>
        void ClearZone();
      
    }
}
