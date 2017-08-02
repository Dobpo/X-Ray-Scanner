using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace X_Ray_Scanner
{
    internal class XRayImage
    {
        const int ImageLenghtByte = 10616832;
        const int ImageWidth = 2048;
        const int ImageHeight = 2048;

        //Test bitmapsource
        private readonly short[] TestImageData = new short[10000];
        //Test bitmapsource

        private BitmapSource _bitmapSource;
        private byte[] aBytes = new byte[ImageLenghtByte];

        public XRayImage(byte[] arrayBytes)
        {
            aBytes = arrayBytes;
            FillData();
        }

        public XRayImage()
        {
            FillData();
        }

        public void MakeImage()
        {

        }

        private void FillData()
        {
            for (var i = 0; i < 3333; i++)
            {
                TestImageData[i] = 1;
                TestImageData[i + 3333] = 15000;
                TestImageData[i + 6666] = 30000;
            }
            TestImageData[9999] = 0xFF;
        }

        public BitmapSource GetBitmapSource()
        {
            int width = 100;
            int height = 100;

            double dipY = 96d;
            double dipX = 96d;

            PixelFormat pf = PixelFormats.Gray16;
            int rawStride = (width * pf.BitsPerPixel + 7) / 8;

            if (_bitmapSource == null)
            {
                _bitmapSource = BitmapSource.Create(width, height, dipX, dipY, pf, null, TestImageData, rawStride);
            }

            return _bitmapSource;
        }

    }
}
