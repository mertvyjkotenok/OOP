using System;
using System.Drawing;

namespace Lab1.Shapes
{
    public class Circle : Figure
    {
        public Circle(Point center) : base(center)
        {
            // По умолчанию радиус 50 (при масштабе 100)
            Sides.Add(new SideStyle(50, 0));
        }

        public override void Draw(Graphics g)
        {
            float scale = Size / 100f;
            float r = Math.Abs(Sides[0].RelativeOffset.X) * scale;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            if (FillColor != Color.Transparent)
            {
                using (Brush b = new SolidBrush(FillColor))
                    g.FillEllipse(b, Center.X - r, Center.Y - r, r * 2, r * 2);
            }

            using (Pen p = new Pen(Sides[0].Color, Sides[0].Thickness))
            {
                // Рисуем эллипс. Линия толщиной Sides[0].Thickness 
                // распределится на 50% внутрь радиуса r и на 50% наружу.
                g.DrawEllipse(p, Center.X - r, Center.Y - r, r * 2, r * 2);
            }
        }

        // Исправленный GetBounds: без лишних отступов, с учетом толщины линии
        public override RectangleF GetBounds()
        {
            float scale = Size / 100f;
            // Чистый радиус по координатам
            float r = Math.Abs(Sides[0].RelativeOffset.X) * scale;

            // Учитываем половину толщины линии, так как перо рисует наружу от линии на Thickness/2
            float halfThickness = Sides[0].Thickness / 2f;
            float totalR = r + halfThickness;

            return new RectangleF(
                Center.X - totalR,
                Center.Y - totalR,
                totalR * 2,
                totalR * 2
            );
        }

        public override bool Contains(Point p)
        {
            float scale = Size / 100f;
            float r = Math.Abs(Sides[0].RelativeOffset.X) * scale;

            // Для попадания по фигуре логично тоже учитывать толщину линии (внешний край)
            float halfThickness = Sides[0].Thickness / 2f;
            float hitRadius = r + halfThickness;

            float dx = p.X - Center.X;
            float dy = p.Y - Center.Y;

            return (dx * dx + dy * dy) <= hitRadius * hitRadius;
        }
    }
}