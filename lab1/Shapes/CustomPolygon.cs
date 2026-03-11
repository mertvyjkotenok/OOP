using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Lab1.Shapes
{
    public class CustomPolygon : PolygonBase
    {
        public CustomPolygon(Point center, List<PointF> relativePoints) : base(center)
        {
            // Превращаем относительные координаты вершин в список сторон (SideStyle)
            Sides = relativePoints.Select(p => new SideStyle(p.X, p.Y)).ToList();
        }
        public CustomPolygon(Point center) : base(center)
        {
            // Этот конструктор нужен для Activator.CreateInstance
        }
    }
}