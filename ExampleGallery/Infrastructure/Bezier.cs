using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExampleGallery
{
    class Bezier
    {
        public int id { get; set; }
        public float controlX0 { get; set; }
        public float controlY0 { get; set; }
        public float controlX1 { get; set; }
        public float controlY1 { get; set; }
        public float controlX2 { get; set; }
        public float controlY2 { get; set; }
        public float controlX3 { get; set; }
        public float controlY3 { get; set; }
        public float length { get; set; }
        public float roc { get; set; }
        public int boundX0 { get; set; }
        public int boundY0 { get; set; }
        public int boundX1 { get; set; }
        public int boundY1 { get; set; }
    }
}
