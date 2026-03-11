using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Lab1.Shapes
{
    public class GroupFigure : Figure
    {
        // Список фигур, входящих в группу
        public List<Figure> SubFigures { get; set; } = new List<Figure>();
      
        public GroupFigure(Point center) : base(center) { }

        // Отрисовываем все вложенные фигуры
        public override void Draw(Graphics g)
        {
            foreach (var fig in SubFigures)
            {
                fig.Draw(g);
            }
        }

        // Клик попал в группу, если он попал в любую из ее подфигур
        public override bool Contains(Point p)
        {
            return SubFigures.Any(fig => fig.Contains(p));
        }

        // Общая рамка выделения - это объединение рамок всех подфигур
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

        // Переопределяем перемещение: двигаем саму группу и все ее подфигуры
        public override void Move(int dx, int dy)
        {
            base.Move(dx, dy); // Двигаем базовую точку группы
            foreach (var fig in SubFigures)
            {
                fig.Move(dx, dy); // Двигаем каждую фигуру внутри
            }
        }
    }
}