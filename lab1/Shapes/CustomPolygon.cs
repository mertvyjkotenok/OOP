using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Lab1.Shapes
{
    // Класс-пустышка для создания кастомных полигонов. 
    // Наследуется от PolygonBase, вся магия отрисовки уже внутри!
    public class CustomPolygon : PolygonBase
    {
        public CustomPolygon(Point center) : base(center) { }
    }

    public static class CustomShapeBuilder
    {
        // 1. Создание фигуры из списка абсолютных точек (клики мышкой)
        public static CustomPolygon CreateFromPoints(List<Point> screenPoints, Color color, float thickness = 2f)
        {
            if (screenPoints.Count == 0) return null;

            // Находим центр нарисованной фигуры
            int centerX = (int)screenPoints.Average(p => p.X);
            int centerY = (int)screenPoints.Average(p => p.Y);
            Point center = new Point(centerX, centerY);

            var poly = new CustomPolygon(center);
            poly.FillColor = color; // Можно сделать прозрачным

            // Преобразуем экранные координаты в относительные отступы
            foreach (var pt in screenPoints)
            {
                float relX = pt.X - centerX;
                float relY = pt.Y - centerY;

                poly.Sides.Add(new SideStyle(relX, relY)
                {
                    Color = Color.Black,
                    Thickness = thickness
                });
            }

            return poly;
        }

        // 2. Вычисление новой точки по углу и длине (поможет для интерфейса ввода)
        // Угол в градусах. 0 градусов смотрит вправо, 90 - вниз (в координатах экрана)
        public static Point CalculateNextPoint(Point startPoint, double angleDegrees, double length)
        {
            double angleRadians = angleDegrees * Math.PI / 180.0;
            int nextX = startPoint.X + (int)(length * Math.Cos(angleRadians));
            int nextY = startPoint.Y + (int)(length * Math.Sin(angleRadians));

            return new Point(nextX, nextY);
        }
    }
}