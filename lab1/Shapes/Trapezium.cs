using System.Drawing;
using Lab1.Shapes;

namespace Lab1.Figures
{
    public class Trapezium : PolygonBase
    {
        public Trapezium(Point center) : base(center)
        {
            Sides.Add(new SideStyle(-30, -30));
            Sides.Add(new SideStyle(30, -30));
            Sides.Add(new SideStyle(60, 30));
            Sides.Add(new SideStyle(-60, 30));
        }
    }
}