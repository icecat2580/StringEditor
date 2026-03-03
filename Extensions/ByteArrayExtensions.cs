using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace StringDiagram.Extensions
{
     public static class ByteArrayExtensions
    {
        /// <summary>
        /// 将图片字节数组转换为 BitmapImage
        /// </summary>
        public static BitmapImage ToBitmapImage(this byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return null;

            using (var ms = new MemoryStream(bytes))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
        }

        /// <summary>
        /// 字节数组转换为Imagesource
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static ImageSource ToImageSource(this byte[] bytes)
        {
            return bytes.ToBitmapImage();
        }
    }
}
