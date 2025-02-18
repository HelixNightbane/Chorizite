﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Chorizite.Core.Render {
    [StructLayout(LayoutKind.Sequential)]
    public struct ColorVec {
        public float Red;
        public float Green;
        public float Blue;
        public float Alpha;

        public ColorVec(float alpha, float red, float green, float blue) {
            Alpha = alpha;
            Red = red;
            Green = green;
            Blue = blue;
        }
    }
}
