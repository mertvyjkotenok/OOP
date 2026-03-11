using System;
using System.Drawing;
using Lab1.Shapes;

namespace Lab1.Figures
{
    public class Pentagon : PolygonBase
    {
        public Pentagon(Point center) : base(center)
        {
            for (int i = 0; i < 5; i++)
            {
                double angle = -Math.PI / 2 + i * 2 * Math.PI / 5;
                Sides.Add(new SideStyle((int)(60 * Math.Cos(angle)), (int)(60 * Math.Sin(angle))));
            }
        }
    }
}