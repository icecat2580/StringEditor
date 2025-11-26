using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StringDiagram.Enums
{
    public enum TopIconKind
    {
        Default,   // 默认规则：第 0 根 = 滚筒，其它 = 连接器
        None,      // 不显示任何图标
        Reel,      // 强制滚筒
        Connector  // 强制连接器
    }
}
