using System.Drawing;
using Image = System.Drawing.Image;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace MapMaven.Extensions
{
    public static class ImageExtensions
    {
        public static string ToDataUrl(this Image image)
        {
            using MemoryStream ms = new MemoryStream();

            image.Save(ms, ImageFormat.Jpeg);
            byte[] imageBytes = ms.ToArray();

            return $"data:image/jpeg;base64,{Convert.ToBase64String(imageBytes)}";
        }

        public static string ToBase64PrependedString(this Image image)
        {
            using MemoryStream ms = new MemoryStream();

            image.Save(ms, ImageFormat.Jpeg);
            byte[] imageBytes = ms.ToArray();

            return $"base64,{Convert.ToBase64String(imageBytes)}";
        }

        public static Image GetResizedImage(this Image image, int width, int height)
        {
            return new Bitmap(image, width, height);
        }
    }
}
