using System.Drawing;
using Lab1.Shapes;

namespace Lab1.Figures
{
    public class Triangle : PolygonBase
    {
        public Triangle(Point center) : base(center)
        {
            Sides.Add(new SideStyle(0, -50));
            Sides.Add(new SideStyle(45, 40));
            Sides.Add(new SideStyle(-45, 40));
        }
    }
}