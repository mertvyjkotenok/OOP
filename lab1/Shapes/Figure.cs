using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Lab1.Shapes
{
    public abstract class Figure
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public int Size { get; set; } = 100;

        public Point BaseLocation { get; set; }
        public PointF RelativePivot { get; set; } = new PointF(0, 0);
        public float Rotation { get; set; } = 0f;

        public Point Center
        {
            get => new Point(
                BaseLocation.X - (int)RelativePivot.X,
                BaseLocation.Y - (int)RelativePivot.Y
            );
            set
            {
                BaseLocation = new Point(
                    value.X + (int)RelativePivot.X,
                    value.Y + (int)RelativePivot.Y
                );
            }
        }

        public List<SideStyle> Sides { get; set; } = new List<SideStyle>();
        public Color FillColor { get; set; } = Color.Transparent;
        public float MaxThickness => Sides.Count > 0 ? Sides.Max(s => s.Thickness) : 0;

        protected Figure() { } // для создания без параметров (если нужно)

        public Figure(Point center)
        {
            BaseLocation = center;
        }

        public abstract void Draw(Graphics g);
        public abstract bool Contains(Point p);
        public abstract RectangleF GetBounds();

        public virtual void Move(int dx, int dy)
        {
            BaseLocation = new Point(BaseLocation.X + dx, BaseLocation.Y + dy);
        }
    }
}