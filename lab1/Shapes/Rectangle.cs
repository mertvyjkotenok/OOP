using System.Drawing;
using Lab1.Shapes;

namespace Lab1.Figures
{
    public class Rectangle : PolygonBase
    {
        public Rectangle(Point center) : base(center)
        {
            Sides.Add(new SideStyle(-50, -50));
            Sides.Add(new SideStyle(50, -50));
            Sides.Add(new SideStyle(50, 50));
            Sides.Add(new SideStyle(-50, 50));
        }
    }
}