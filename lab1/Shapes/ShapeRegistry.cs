using System;
using System.Collections.Generic;

namespace Lab1.Shapes
{
    public static class ShapeRegistry
    {
        // Словарь хранит Название фигуры -> Функция(Фабрика) для создания новой копии фигуры
        public static Dictionary<string, Func<Point, Figure>> AvailableShapes { get; private set; }
            = new Dictionary<string, Func<Point, Figure>>();

        // Регистрация новой фигуры
        public static void RegisterShape(string name, Func<Point, Figure> creatorFunc)
        {
            AvailableShapes[name] = creatorFunc;
        }

        // Получить список всех названий фигур (для загрузки в ComboBox)
        public static List<string> GetShapeNames()
        {
            return new List<string>(AvailableShapes.Keys);
        }

        // Создать фигуру по имени
        public static Figure CreateShape(string name, Point center)
        {
            if (AvailableShapes.TryGetValue(name, out var creator))
            {
                return creator(center);
            }
            return null;
        }
    }
}