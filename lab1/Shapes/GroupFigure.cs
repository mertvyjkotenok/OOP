using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Lab1.Shapes
{
    public class GroupFigure : Figure
    {
        public List<Figure> SubFigures { get; set; } = new List<Figure>();

        public GroupFigure(Point center) : base(center) { }

        public override void Draw(Graphics g)
        {
            foreach (var fig in SubFigures)
                fig.Draw(g);
        }

        public override bool Contains(Point p)
        {
            return SubFigures.Any(fig => fig.Contains(p));
        }

        public override RectangleF GetBounds()
        {
            if (SubFigures.Count == 0) return new RectangleF(Center.X, Center.Y, 0, 0);
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            foreach (var fig in SubFigures)
            {
                var bounds = fig.GetBounds();
                if (bounds.Left < minX) minX = bounds.Left;
                if (bounds.Top < minY) minY = bounds.Top;
                if (bounds.Right > maxX) maxX = bounds.Right;
                if (bounds.Bottom > maxY) maxY = bounds.Bottom;
            }
            return RectangleF.FromLTRB(minX, minY, maxX, maxY);
        }

        public override void Move(int dx, int dy)
        {
            base.Move(dx, dy);
            foreach (var fig in SubFigures)
                fig.Move(dx, dy);
        }
    }
}