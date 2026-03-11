using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Numerics;

namespace Lab1.Shapes
{
    public abstract class PolygonBase : Figure
    {
        protected PolygonBase(Point center) : base(center) { }

        public PointF[] GetVertices()
        {
            float scale = Size / 100f;
            return Sides.Select(s => new PointF(
                Center.X + s.RelativeOffset.X * scale,
                Center.Y + s.RelativeOffset.Y * scale
            )).ToArray();
        }

        // НОВОЕ: Реализация границ для полигонов
        public override RectangleF GetBounds()
        {
            var vertices = GetVertices();
            if (vertices.Length == 0) return new RectangleF(Center.X, Center.Y, 0, 0);

            float minX = vertices.Min(v => v.X);
            float minY = vertices.Min(v => v.Y);
            float maxX = vertices.Max(v => v.X);
            float maxY = vertices.Max(v => v.Y);

            // Увеличиваем отступ: половина толщины линии + 15 пикселей запаса
            float padding = (MaxThickness / 2f) + 15f;
            return new RectangleF(minX - padding, minY - padding, (maxX - minX) + padding * 2, (maxY - minY) + padding * 2);
        }

        public override void Draw(Graphics g)
        {
            var vertices = GetVertices();
            int n = vertices.Length;
            if (n < 2) return;

            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (FillColor != Color.Transparent)
            {
                using (var brush = new SolidBrush(FillColor))
                    g.FillPolygon(brush, vertices);
            }

            PointF[] pts = vertices;
            Vector2[] dirs = new Vector2[n];
            Vector2[] normals = new Vector2[n];
            float[] halfThick = Sides.Select(s => s.Thickness / 2f).ToArray();

            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;
                Vector2 side = new Vector2(pts[next].X - pts[i].X, pts[next].Y - pts[i].Y);
                float len = side.Length();
                dirs[i] = len > 0.1f ? side / len : new Vector2(1, 0);
                Vector2 n1 = new Vector2(-dirs[i].Y, dirs[i].X);
                PointF mid = new PointF((pts[i].X + pts[next].X) / 2, (pts[i].Y + pts[next].Y) / 2);
                Vector2 toCenter = new Vector2(Center.X - mid.X, Center.Y - mid.Y);
                normals[i] = Vector2.Dot(n1, toCenter) > 0 ? -n1 : n1;
            }

            PointF[] outer = new PointF[n];
            PointF[] inner = new PointF[n];

            for (int i = 0; i < n; i++)
            {
                int prev = (i - 1 + n) % n;
                int curr = i;
                float det = dirs[prev].X * dirs[curr].Y - dirs[prev].Y * dirs[curr].X;

                if (Math.Abs(det) < 1e-4)
                {
                    Vector2 shift = normals[curr] * halfThick[curr];
                    outer[i] = new PointF(pts[i].X + shift.X, pts[i].Y + shift.Y);
                    inner[i] = new PointF(pts[i].X - shift.X, pts[i].Y - shift.Y);
                }
                else
                {
                    outer[i] = GetIntersect(pts[i], dirs[prev], normals[prev], halfThick[prev], dirs[curr], normals[curr], halfThick[curr], true);
                    inner[i] = GetIntersect(pts[i], dirs[prev], normals[prev], halfThick[prev], dirs[curr], normals[curr], halfThick[curr], false);
                }
            }

            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;
                PointF[] quad = { outer[i], outer[next], inner[next], inner[i] };
                using (var brush = new SolidBrush(Sides[i].Color))
                    g.FillPolygon(brush, quad);
            }
        }

        private PointF GetIntersect(PointF P, Vector2 d1, Vector2 n1, float h1, Vector2 d2, Vector2 n2, float h2, bool isOuter)
        {
            float s = isOuter ? 1 : -1;
            Vector2 P1 = new Vector2(P.X, P.Y) + n1 * h1 * s;
            Vector2 P2 = new Vector2(P.X, P.Y) + n2 * h2 * s;
            float det = (-d1.X) * (-d2.Y) - (-d1.Y) * (-d2.X);
            Vector2 rhs = P2 - P1;
            float u = ((-d1.X) * rhs.Y - (-d1.Y) * rhs.X) / det;
            Vector2 res = P2 + d2 * u;
            return new PointF(res.X, res.Y);
        }

        public override bool Contains(Point p)
        {
            var poly = GetVertices();
            if (poly.Length < 3) return false;
            bool res = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if (((poly[i].Y > p.Y) != (poly[j].Y > p.Y)) &&
                    (p.X < (poly[j].X - poly[i].X) * (p.Y - poly[i].Y) / (poly[j].Y - poly[i].Y) + poly[i].X))
                    res = !res;
            }
            return res;
        }
    }
}