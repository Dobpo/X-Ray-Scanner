using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace X_Ray_Scanner
{
    class XRayImage
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public string testGit;

        public XRayImage(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }
}
