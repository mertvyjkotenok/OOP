using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Lab1.Shapes
{
    public abstract class Figure
    {
        public int Size { get; set; } = 100;

        // Это абсолютная точка привязки (Anchor) на холсте
        public Point BaseLocation { get; set; }

        // Относительное смещение точки привязки от геометрического центра
        public PointF RelativePivot { get; set; } = new PointF(0, 0);

        // Геометрический (визуальный) центр фигуры. Используется для отрисовки.
        public Point Center
        {
            get => new Point(
                BaseLocation.X - (int)RelativePivot.X, // ИЗМЕНЕНО: теперь мы вычитаем
                BaseLocation.Y - (int)RelativePivot.Y  // ИЗМЕНЕНО: теперь мы вычитаем
            );
            set
            {
                // Если мы смещаем всю фигуру целиком, точка привязки смещается вместе с ней
                BaseLocation = new Point(
                    value.X + (int)RelativePivot.X, // ИЗМЕНЕНО: теперь мы прибавляем
                    value.Y + (int)RelativePivot.Y  // ИЗМЕНЕНО: теперь мы прибавляем
                );
            }
        }

        public List<SideStyle> Sides { get; set; } = new List<SideStyle>();
        public Color FillColor { get; set; } = Color.Transparent;
        public float MaxThickness => Sides.Count > 0 ? Sides.Max(s => s.Thickness) : 0;

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