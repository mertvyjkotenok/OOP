using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Lab1.Shapes
{
    public class Ellipse : Figure
    {
        public Ellipse(Point center, float radiusX = 50, float radiusY = 50) : base(center)
        {
            Sides.Add(new SideStyle(radiusX, radiusY));
        }

        public Ellipse() : this(Point.Empty) { }

        public override void Draw(Graphics g)
        {
            float scale = Size / 100f;
            float rx = Math.Abs(Sides[0].RelativeOffset.X) * scale;
            float ry = Math.Abs(Sides[0].RelativeOffset.Y) * scale;

            GraphicsState state = g.Save();
            g.TranslateTransform(Center.X, Center.Y);
            g.RotateTransform(Rotation);
            g.TranslateTransform(-Center.X, -Center.Y);

            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (FillColor != Color.Transparent)
            {
                using (Brush b = new SolidBrush(FillColor))
                    g.FillEllipse(b, Center.X - rx, Center.Y - ry, rx * 2, ry * 2);
            }

            using (Pen p = new Pen(Sides[0].Color, Sides[0].Thickness))
            {
                g.DrawEllipse(p, Center.X - rx, Center.Y - ry, rx * 2, ry * 2);
            }

            g.Restore(state);
        }

        public override RectangleF GetBounds()
        {
            float scale = Size / 100f;
            float rx = Math.Abs(Sides[0].RelativeOffset.X) * scale;
            float ry = Math.Abs(Sides[0].RelativeOffset.Y) * scale;
            float halfThickness = Sides[0].Thickness / 2f;

            float a = rx + halfThickness;
            float b = ry + halfThickness;

            double rad = Rotation * Math.PI / 180.0;
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);

            float extentX = (float)Math.Sqrt(a * a * cos * cos + b * b * sin * sin);
            float extentY = (float)Math.Sqrt(a * a * sin * sin + b * b * cos * cos);

            return new RectangleF(
                Center.X - extentX,
                Center.Y - extentY,
                extentX * 2,
                extentY * 2
            );
        }

        public override bool Contains(Point p)
        {
            float scale = Size / 100f;
            float rx = Math.Abs(Sides[0].RelativeOffset.X) * scale;
            float ry = Math.Abs(Sides[0].RelativeOffset.Y) * scale;
            float halfThickness = Sides[0].Thickness / 2f;
            float a = rx + halfThickness;
            float b = ry + halfThickness;

            PointF local = RotatePoint(p, Center, -Rotation);
            float dx = local.X - Center.X;
            float dy = local.Y - Center.Y;

            return (dx * dx) / (a * a) + (dy * dy) / (b * b) <= 1;
        }

        private PointF RotatePoint(PointF point, PointF center, float angleDegrees)
        {
            double rad = angleDegrees * Math.PI / 180.0;
            float cos = (float)Math.Cos(rad);
            float sin = (float)Math.Sin(rad);
            float dx = point.X - center.X;
            float dy = point.Y - center.Y;
            return new PointF(
                center.X + dx * cos - dy * sin,
                center.Y + dx * sin + dy * cos
            );
        }

        public float RadiusX
        {
            get => Sides[0].RelativeOffset.X;
            set => Sides[0].RelativeOffset = new PointF(value, Sides[0].RelativeOffset.Y);
        }

        public float RadiusY
        {
            get => Sides[0].RelativeOffset.Y;
            set => Sides[0].RelativeOffset = new PointF(Sides[0].RelativeOffset.X, value);
        }

        public float GetScaledRadiusX() => Math.Abs(RadiusX) * Size / 100f;
        public float GetScaledRadiusY() => Math.Abs(RadiusY) * Size / 100f;

        // --- НОВЫЙ КОД ДЛЯ РАБОТЫ С ФОКУСАМИ ---

        public PointF GetFocus1()
        {
            float scale = Size / 100f;
            float rx = Math.Abs(RadiusX) * scale;
            float ry = Math.Abs(RadiusY) * scale;
            float c = (float)Math.Sqrt(Math.Max(0, rx * rx - ry * ry));
            PointF localF1 = (rx >= ry) ? new PointF(-c, 0) : new PointF(0, -c);
            return RotatePoint(new PointF(Center.X + localF1.X, Center.Y + localF1.Y), Center, Rotation);
        }

        public PointF GetFocus2()
        {
            float scale = Size / 100f;
            float rx = Math.Abs(RadiusX) * scale;
            float ry = Math.Abs(RadiusY) * scale;
            float c = (float)Math.Sqrt(Math.Max(0, rx * rx - ry * ry));
            PointF localF2 = (rx >= ry) ? new PointF(c, 0) : new PointF(0, c);
            return RotatePoint(new PointF(Center.X + localF2.X, Center.Y + localF2.Y), Center, Rotation);
        }

        public void SetFoci(PointF f1, PointF f2)
        {
            float scale = Size / 100f;
            PointF newCenter = new PointF((f1.X + f2.X) / 2f, (f1.Y + f2.Y) / 2f);
            float dx = f2.X - f1.X;
            float dy = f2.Y - f1.Y;
            float c = (float)Math.Sqrt(dx * dx + dy * dy) / 2f;

            // Малую полуось (толщину эллипса) оставляем неизменной
            float currentB = Math.Min(Math.Abs(RadiusX), Math.Abs(RadiusY)) * scale;
            if (currentB < 2) currentB = 2f;

            // Пересчитываем большую полуось
            float a = (float)Math.Sqrt(currentB * currentB + c * c);
            float b = currentB;

            float angle = (float)(Math.Atan2(dy, dx) * 180.0 / Math.PI);

            int cx = (int)Math.Round(newCenter.X);
            int cy = (int)Math.Round(newCenter.Y);

            this.BaseLocation = new Point(cx + (int)RelativePivot.X, cy + (int)RelativePivot.Y);
            this.Rotation = angle;
            this.RadiusX = a / scale;
            this.RadiusY = b / scale;
        }
    }
}