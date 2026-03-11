using System;
using System.Drawing;

namespace Lab1.Shapes
{
    public class Circle : Figure
    {
        public Circle(Point center) : base(center)
        {
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
                g.DrawEllipse(p, Center.X - r, Center.Y - r, r * 2, r * 2);
        }

        // НОВОЕ: Границы круга
        public override RectangleF GetBounds()
        {
            float scale = Size / 100f;
            float r = Math.Abs(Sides[0].RelativeOffset.X) * scale;
            float padding = MaxThickness / 2f;
            return new RectangleF(Center.X - r - 10, Center.Y - r - 10, (r * 2) + 20, (r * 2) + 20);
        }

        public override bool Contains(Point p)
        {
            float scale = Size / 100f;
            float r = Math.Abs(Sides[0].RelativeOffset.X) * scale;
            return (Math.Pow(p.X - Center.X, 2) + Math.Pow(p.Y - Center.Y, 2)) <= r * r;
        }
    }
}