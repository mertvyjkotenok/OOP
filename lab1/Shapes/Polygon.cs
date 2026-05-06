using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Numerics;

namespace Lab1.Shapes
{
    public class Polygon : Figure
    {
        // Конструктор с центром и относительными вершинами
        public Polygon(Point center, IEnumerable<PointF> vertices) : base(center)
        {
            Sides = vertices.Select(v => new SideStyle(v.X, v.Y)).ToList();
        }

        // Пустой конструктор
        public Polygon() : this(Point.Empty, new List<PointF>()) { }

        // Конструктор только с центром (вершины задаются позже)
        public Polygon(Point center) : base(center) { }

        public PointF[] GetVertices()
        {
            float scale = Size / 100f;
            return Sides.Select(s => new PointF(
                Center.X + s.RelativeOffset.X * scale,
                Center.Y + s.RelativeOffset.Y * scale
            )).ToArray();
        }

        public override RectangleF GetBounds()
        {
            var vertices = GetVertices();
            int n = vertices.Length;
            if (n == 0) return new RectangleF(Center.X, Center.Y, 0, 0);
            if (n == 1)
            {
                float h = Sides[0].Thickness / 2f;
                return new RectangleF(vertices[0].X - h, vertices[0].Y - h, Sides[0].Thickness, Sides[0].Thickness);
            }

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            Vector2[] dirs = new Vector2[n];
            Vector2[] normals = new Vector2[n];
            float[] halfThick = new float[n];

            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;
                Vector2 p1 = new Vector2(vertices[i].X, vertices[i].Y);
                Vector2 p2 = new Vector2(vertices[next].X, vertices[next].Y);
                Vector2 d = p2 - p1;
                float len = d.Length();
                d = len < 0.001f ? new Vector2(1, 0) : Vector2.Normalize(d);
                dirs[i] = d;
                normals[i] = new Vector2(-d.Y, d.X);
                halfThick[i] = Sides[i].Thickness / 2f;
            }

            for (int i = 0; i < n; i++)
            {
                int prev = (i + n - 1) % n;
                PointF outer = GetIntersect(vertices[i], dirs[prev], normals[prev], halfThick[prev],
                                            dirs[i], normals[i], halfThick[i], true);
                PointF inner = GetIntersect(vertices[i], dirs[prev], normals[prev], halfThick[prev],
                                            dirs[i], normals[i], halfThick[i], false);

                UpdateBounds(outer);
                UpdateBounds(inner);
            }

            void UpdateBounds(PointF p)
            {
                if (p.X < minX) minX = p.X;
                if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Y > maxY) maxY = p.Y;
            }

            return RectangleF.FromLTRB(minX, minY, maxX, maxY);
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
                    outer[i] = GetIntersect(pts[i], dirs[prev], normals[prev], halfThick[prev],
                                            dirs[curr], normals[curr], halfThick[curr], true);
                    inner[i] = GetIntersect(pts[i], dirs[prev], normals[prev], halfThick[prev],
                                            dirs[curr], normals[curr], halfThick[curr], false);
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

        private PointF GetIntersect(PointF P, Vector2 d1, Vector2 n1, float h1,
                                    Vector2 d2, Vector2 n2, float h2, bool isOuter)
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