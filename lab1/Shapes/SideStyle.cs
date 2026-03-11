using System.Drawing;

namespace Lab1.Shapes
{
    public class SideStyle
    {
        public Color Color { get; set; } = Color.Black;
        public float Thickness { get; set; } = 2.0f;

        // ИСПРАВЛЕНО: Тип изменен на PointF
        public PointF RelativeOffset { get; set; }

        // ИСПРАВЛЕНО: Аргументы теперь float
        public SideStyle(float x = 0, float y = 0)
        {
            RelativeOffset = new PointF(x, y);
        }
    }
}