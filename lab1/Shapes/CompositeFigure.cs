using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Lab1.Shapes
{
    public class CompositeFigure : Figure
    {
        public List<Figure> SubFigures { get; set; } = new List<Figure>();

        public CompositeFigure(Point center) : base(center) { }

        public override void Draw(Graphics g)
        {
            // Рисуем все дочерние фигуры
            foreach (var figure in SubFigures)
            {
                figure.Draw(g);
            }
        }

        public override bool Contains(Point p)
        {
            // Фигура содержит точку, если хотя бы одна из её частей содержит эту точку
            return SubFigures.Any(f => f.Contains(p));
        }

        public override RectangleF GetBounds()
        {
            if (SubFigures.Count == 0) return new RectangleF(Center.X, Center.Y, 0, 0);

            // Находим общую границу для всех фигур в группе
            var firstBounds = SubFigures.First().GetBounds();
            float minX = firstBounds.Left;
            float minY = firstBounds.Top;
            float maxX = firstBounds.Right;
            float maxY = firstBounds.Bottom;

            foreach (var fig in SubFigures.Skip(1))
            {
                var b = fig.GetBounds();
                if (b.Left < minX) minX = b.Left;
                if (b.Top < minY) minY = b.Top;
                if (b.Right > maxX) maxX = b.Right;
                if (b.Bottom > maxY) maxY = b.Bottom;
            }

            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }

        public override void Move(int dx, int dy)
        {
            base.Move(dx, dy);
            // При перемещении группы двигаем все вложенные фигуры
            foreach (var fig in SubFigures)
            {
                fig.Move(dx, dy);
            }
        }
    }
}