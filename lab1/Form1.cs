using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Lab1.Figures;
using Lab1.Shapes;

namespace Lab1
{
    public partial class Form1 : Form
    {
        public List<Figure> figures = new List<Figure>();
        public Figure selectedFigure = null;
        private bool isDragging = false;
        private Point lastMousePos;
        private bool isFullScreen = false;
        private FormBorderStyle lastStyle;
        private FormWindowState lastState;
        private EditorForm currentEditor = null;

        public Form1()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;

            // Привязка событий тулбара
            btnAddShape.Click += (s, e) => CreateFigureFromUI();
            btnClearAll.Click += (s, e) => {
                figures.Clear();
                selectedFigure = null;
                
                canvasPanel.Invalidate();
            };
            
            this.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Escape) Application.Exit();
            };

            // События холста
            canvasPanel.Paint += CanvasPanel_Paint;
            canvasPanel.MouseDown += CanvasPanel_MouseDown;
            canvasPanel.MouseMove += CanvasPanel_MouseMove;
            canvasPanel.MouseUp += (s, e) => isDragging = false;

            ShapeRegistry.RegisterShape("Треугольник", center => new Triangle(center));
            ShapeRegistry.RegisterShape("Круг", center => new Circle(center));
            ShapeRegistry.RegisterShape("Прямоугольник", center => new Lab1.Figures.Rectangle(center));
            ShapeRegistry.RegisterShape("Трапеция", center => new Trapezium(center));
            ShapeRegistry.RegisterShape("Пятиугольник", center => new Pentagon(center));

            
        }

        private void CreateFigureFromUI()
        {
            Point center = new Point(canvasPanel.Width / 2, canvasPanel.Height / 2);
            Figure f = null;

            switch (cbShapeType.SelectedItem.ToString())
            {
                case "Прямоугольник": f = new Lab1.Figures.Rectangle(center); break;
                case "Треугольник": f = new Triangle(center); break;
                case "Круг": f = new Circle(center); break;
                case "Трапеция": f = new Trapezium(center); break;
                case "Пятиугольник": f = new Pentagon(center); break;
            }

            if (f != null)
            {
                figures.Add(f);
                selectedFigure = f;
                canvasPanel.Invalidate();
            }
        }

        private void CanvasPanel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            using (Pen originPen = new Pen(Color.FromArgb(30, 90, 140), 2))
            using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(30, 90, 140)))
            {
                // Рисуем небольшие направляющие линии (ось X вправо, ось Y вниз)
                e.Graphics.DrawLine(originPen, 0, 0, 30, 0); // Линия вправо
                e.Graphics.DrawLine(originPen, 0, 0, 0, 30); // Линия вниз

                // Добавляем текстовую подсказку чуть правее и ниже самого угла
                e.Graphics.DrawString("(0, 0)", this.Font, textBrush, 10, 10);
            }

            foreach (var fig in figures)
            {
                fig.Draw(e.Graphics);
            }

            // ОТОБРАЖЕНИЕ ВИРТУАЛЬНЫХ ГРАНИЦ (Оставляем как было)
            if (selectedFigure != null)
            {
                // 1. Точка привязки
                using (Pen p = new Pen(Color.Blue, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                {
                    e.Graphics.DrawEllipse(p, selectedFigure.BaseLocation.X - 5, selectedFigure.BaseLocation.Y - 5, 10, 10);
                }

                // 2. Рамка выделения
                RectangleF bounds = selectedFigure.GetBounds();
                using (Pen borderPen = new Pen(Color.Blue, 1))
                {
                    e.Graphics.DrawRectangle(borderPen, bounds.X - 2, bounds.Y - 2, bounds.Width + 4, bounds.Height + 4);
                }
            }
        }

        private void CanvasPanel_MouseDown(object sender, MouseEventArgs e)
        {
            bool hit = false;
            for (int i = figures.Count - 1; i >= 0; i--)
            {
                if (figures[i].Contains(e.Location))
                {
                    selectedFigure = figures[i];
                    hit = true;

                    if (e.Button == MouseButtons.Left)
                    {
                        isDragging = true;
                        lastMousePos = e.Location;
                    }
                    else if (e.Button == MouseButtons.Right)
                    {
                        // Закрываем предыдущее окно, если оно было открыто
                        if (currentEditor != null && !currentEditor.IsDisposed)
                            currentEditor.Close();

                        // Создаём новое окно и устанавливаем владельца
                        currentEditor = new EditorForm(selectedFigure, this, canvasPanel);
                        currentEditor.Owner = this; // важно!
                        currentEditor.Show();
                    }
                    break;
                }
            }

            if (!hit) selectedFigure = null;
            canvasPanel.Invalidate();
        }

        private void CanvasPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && selectedFigure != null)
            {
                selectedFigure.Move(e.X - lastMousePos.X, e.Y - lastMousePos.Y);
                lastMousePos = e.Location;
                canvasPanel.Invalidate();
            }
        }
    }
}