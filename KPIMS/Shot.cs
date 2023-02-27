using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KPI_measuring_software
{
    internal class Shot
    {
        public Mat image { get; }
        public int time { get; }
        
        public Shot(Bitmap image, int time) 
        { 
            this.image = image.ToImage<Bgr,byte>().Mat;
            this.time = time;
        }
    }
}
