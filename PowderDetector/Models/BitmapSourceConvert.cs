using Emgu.CV;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using System.Windows;
using System;

public static class BitmapSourceConvert
{
    [DllImport("gdi32")]
    private static extern int DeleteObject(IntPtr o);

    public static BitmapSource ToBitmapSource(this IImage image)
    {
        using (System.Drawing.Bitmap source = image.Bitmap)
        {
            IntPtr ptr = source.GetHbitmap();

            BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                ptr,
                IntPtr.Zero,
                Int32Rect.Empty,
                System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

            DeleteObject(ptr);
            return bs;
        }
    }
}