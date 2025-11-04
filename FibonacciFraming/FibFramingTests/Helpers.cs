using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;

namespace FibFramingTests;

public static class Helpers
{

    public static string ToPath(string filePath) {
        var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        return Path.Combine(basePath, filePath);
    }

    public static void SaveBmp(this Bitmap src, string filePath)
    {
        filePath = ToPath(filePath);

        var p = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(p))
        {
            Directory.CreateDirectory(p);
        }
        if (File.Exists(filePath)) File.Delete(filePath);
        src.Save(filePath, ImageFormat.Bmp);
    }
}