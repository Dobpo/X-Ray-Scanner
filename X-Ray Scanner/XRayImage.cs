using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace X_Ray_Scanner
{
    internal class XRayImage
    {
        const int ImageLenghtByte = 10616832;
        const int ImageLenghtShort = 5308416;
        public readonly int Width;
        public readonly int Heigth;

        private BitmapSource _bitmapSource;
        private short[] ImageDataArray = new short[ImageLenghtByte];

        public XRayImage(byte[] arrayBytes)
        {
            ImageDataArray[0] = (short)arrayBytes[0];
        }

        public XRayImage(short[] array)
        {
            ImageDataArray = array;
        }

        public void MakeImage()
        {

        }

        public BitmapSource GetBitmapSource()
        {
            int width = 2048;
            int height = 2048;

            double dipY = 96d;
            double dipX = 96d;

            PixelFormat pf = PixelFormats.Gray16;
            int rawStride = (width * pf.BitsPerPixel + 7) / 8;

            if (_bitmapSource == null)
            {
                _bitmapSource = BitmapSource.Create(width, height, dipX, dipY, pf, null, ImageDataArray, rawStride);
            }

            return _bitmapSource;
        }
    }
}
