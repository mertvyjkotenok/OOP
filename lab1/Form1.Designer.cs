using Lab1.Shapes;
using System.Drawing;
using System.Windows.Forms;

namespace Lab1
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private Panel toolbar;
        private DoubleBufferedPanel canvasPanel;

        // Элементы управления в верхней панели
        private ComboBox cbShapeType;
        private Button btnAddShape;
        private Button btnClearAll;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.Text = "Графический редактор";
            this.Size = new Size(1200, 800);
            this.KeyPreview = true;
            this.Font = new Font("Segoe UI", 14F, FontStyle.Regular); // было 10F

            // --- ВЕРХНЯЯ ПАНЕЛЬ ---
            toolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,                      // было 60
                BackColor = Color.FromArgb(90, 150, 200),
                Padding = new Padding(10)
            };

            // 1. Выбор типа фигуры
            cbShapeType = new ComboBox
            {
                Location = new Point(15, 20),
                Size = new Size(200, 35),          // ширина увеличена, высота под шрифт 14
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 14F)
            };
            //cbShapeType.Items.AddRange(new object[] { "Прямоугольник", "Треугольник", "Круг", "Трапеция", "Пятиугольник" });
            cbShapeType.DataSource = ShapeRegistry.GetShapeNames();
            cbShapeType.SelectedIndex = 0;

            // 2. Добавить фигуру
            btnAddShape = new Button
            {
                Text = "Добавить фигуру",
                Location = new Point(230, 18),     // сдвинуто правее
                Size = new Size(160, 40),          // высота увеличена
                BackColor = Color.FromArgb(30, 90, 140),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnAddShape.FlatAppearance.BorderSize = 0;

            // 3. Удалить всё
            btnClearAll = new Button
            {
                Text = "Удалить всё",
                Location = new Point(405, 18),     // сдвинуто правее
                Size = new Size(160, 40),
                BackColor = Color.FromArgb(30, 90, 140),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnClearAll.FlatAppearance.BorderSize = 0;

            toolbar.Controls.Add(cbShapeType);
            toolbar.Controls.Add(btnAddShape);
            toolbar.Controls.Add(btnClearAll);

            // --- ХОЛСТ ---
            canvasPanel = new DoubleBufferedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.WhiteSmoke
            };

            this.Controls.Add(canvasPanel);
            this.Controls.Add(toolbar);
        }
    }

    // Класс для плавной отрисовки без мерцания
    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();
        }
    }
}