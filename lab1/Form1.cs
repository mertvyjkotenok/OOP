using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
        private EditorForm currentEditor = null;

        // Режимы работы
        private bool isDrawingMode = false;
        private List<Point> currentDrawPoints = new List<Point>();
        private Point currentMouseHoverPos;

        private bool isGroupingMode = false;
        private List<Figure> currentGroupSelection = new List<Figure>();
        private Dictionary<string, Figure> customTemplates = new Dictionary<string, Figure>();

        private bool isModifyingGroupMode = false;
        private GroupFigure activeGroupToModify = null;
        public Form1()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            btnModifyGroup.Click += BtnModifyGroup_Click;
            // Привязка событий кнопок
            btnAddShape.Click += (s, e) => CreateFigureFromUI();
            btnClearAll.Click += (s, e) => {
                figures.Clear();
                selectedFigure = null;
                canvasPanel.Invalidate();
            };

            // Настройка кнопок кастомных режимов
            btnDrawCustom.Click += BtnDrawCustom_Click;
            btnGroupShapes.Click += BtnGroupShapes_Click;
            btnUngroup.Click += BtnUngroup_Click; // Событие разгруппировки

            this.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Escape) Application.Exit();
            };

            canvasPanel.Paint += CanvasPanel_Paint;
            canvasPanel.MouseDown += CanvasPanel_MouseDown;
            canvasPanel.MouseMove += CanvasPanel_MouseMove;
            canvasPanel.MouseUp += (s, e) => isDragging = false;
        }

        private void BtnModifyGroup_Click(object sender, EventArgs e)
        {
            if (selectedFigure is GroupFigure group)
            {
                isModifyingGroupMode = true;
                isDrawingMode = false;
                isGroupingMode = false;
                activeGroupToModify = group;
                MessageBox.Show("Режим редактирования группы.\nЛКМ по отдельной фигуре — добавить в группу.\nЛКМ по фигуре внутри группы — удалить из группы.\nПКМ — завершить.", "Редактирование");
            }
            else
            {
                MessageBox.Show("Сначала выделите группу для изменения.");
            }
        }

        private void UpdateGroupCenter(GroupFigure group)
        {
            if (group.SubFigures.Count == 0) return;

            // Находим новые общие границы всех фигур в группе
            float minX = group.SubFigures.Min(f => f.GetBounds().Left);
            float maxX = group.SubFigures.Max(f => f.GetBounds().Right);
            float minY = group.SubFigures.Min(f => f.GetBounds().Top);
            float maxY = group.SubFigures.Max(f => f.GetBounds().Bottom);

            // Вычисляем новый геометрический центр
            Point newCenter = new Point((int)(minX + maxX) / 2, (int)(minY + maxY) / 2);

            // Устанавливаем новую базовую точку группы
            group.BaseLocation = newCenter;
        }

        private void BtnDrawCustom_Click(object sender, EventArgs e)
        {
            isModifyingGroupMode = false;
            isDrawingMode = true;
            isGroupingMode = false;
            currentDrawPoints.Clear();
            selectedFigure = null;
            MessageBox.Show("Режим рисования включен.\nЛКМ - точки, ПКМ - завершить.", "Рисование");
        }

        private void BtnGroupShapes_Click(object sender, EventArgs e)
        {
            isModifyingGroupMode = false;
            isGroupingMode = true;
            isDrawingMode = false;
            currentGroupSelection.Clear();
            selectedFigure = null;
            MessageBox.Show("Режим объединения включен.\nЛКМ по фигурам - выбор, ПКМ - объединить.", "Группировка");
        }

        // ЛОГИКА РАЗГРУППИРОВКИ
        private void BtnUngroup_Click(object sender, EventArgs e)
        {
            if (selectedFigure is GroupFigure group)
            {
                foreach (var subFig in group.SubFigures)
                {
                    figures.Add(subFig);
                }
                figures.Remove(group);
                selectedFigure = null;
                canvasPanel.Invalidate();
                MessageBox.Show("Группа разделена на части.");
            }
            else
            {
                MessageBox.Show("Выберите группу для разделения.");
            }
        }

        private void CreateFigureFromUI()
        {
            if (cbShapeType.SelectedItem == null) return;
            Point center = new Point(canvasPanel.Width / 2, canvasPanel.Height / 2);
            Figure f = null;
            string selectedType = cbShapeType.SelectedItem.ToString();

            if (customTemplates.ContainsKey(selectedType))
                f = CloneFigure(customTemplates[selectedType], center, customTemplates[selectedType].BaseLocation);
            else
            {
                switch (selectedType)
                {
                    case "Прямоугольник": f = new Lab1.Figures.Rectangle(center); break;
                    case "Треугольник": f = new Triangle(center); break;
                    case "Круг": f = new Circle(center); break;
                    case "Трапеция": f = new Trapezium(center); break;
                    case "Пятиугольник": f = new Pentagon(center); break;
                }
            }

            if (f != null) { figures.Add(f); selectedFigure = f; canvasPanel.Invalidate(); }
        }

        private void CanvasPanel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            using (Pen originPen = new Pen(Color.FromArgb(30, 90, 140), 2))
            /*using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(30, 90, 140)))
            {
                e.Graphics.DrawLine(originPen, 0, 0, 30, 0);
                e.Graphics.DrawLine(originPen, 0, 0, 0, 30);
                e.Graphics.DrawString("(0, 0)", this.Font, textBrush, 10, 10);
            }*/

            foreach (var fig in figures) fig.Draw(e.Graphics);

            if (selectedFigure != null && !isDrawingMode && !isGroupingMode)
            {
                var bounds = selectedFigure.GetBounds();

                // Определяем цвет рамки
                Color selectionColor = Color.Black; // По умолчанию черная

                // Проверяем контур. 
                // Если это одиночная фигура (PolygonBase), смотрим на цвет её сторон.
                if (selectedFigure is PolygonBase poly)
                {
                    // Если хотя бы одна сторона черная (или основная первая сторона)
                    if (poly.Sides.Count > 0 &&
                        poly.Sides[0].Color.R == 0 &&
                        poly.Sides[0].Color.G == 0 &&
                        poly.Sides[0].Color.B == 0)
                    {
                        selectionColor = Color.DeepSkyBlue; // Если контур черный -> голубая
                    }
                }
                // Если это группа, можно проверять по первой подфигуре или оставить черной
                else if (selectedFigure is GroupFigure group && group.SubFigures.Count > 0)
                {
                    // Пример логики для группы: если у первой фигуры группы черный контур
                    if (group.SubFigures[0] is PolygonBase firstPoly &&
                        firstPoly.Sides.Count > 0 &&
                        firstPoly.Sides[0].Color == Color.Black)
                    {
                        selectionColor = Color.DeepSkyBlue;
                    }
                }

                using (Pen selectionPen = new Pen(selectionColor, 1))
                {
                    selectionPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    e.Graphics.DrawRectangle(selectionPen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
                }

                // Отрисовка точки центра (Pivot)
                e.Graphics.FillEllipse(Brushes.Blue, selectedFigure.Center.X - 3, selectedFigure.Center.Y - 3, 6, 6);
            }

            if (isGroupingMode)
            {
                using (Pen groupPen = new Pen(Color.Red, 2) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                    foreach (var fig in currentGroupSelection)
                        e.Graphics.DrawRectangle(groupPen, fig.GetBounds());
            }

            if (isModifyingGroupMode && activeGroupToModify != null)
            {
                using (Pen editGroupPen = new Pen(Color.Orange, 3) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                {
                    e.Graphics.DrawRectangle(editGroupPen, activeGroupToModify.GetBounds());
                }
            }

            // Отрисовка линий в режиме создания
            if (isDrawingMode && currentDrawPoints.Count > 0)
            {
                using (Pen solidPen = new Pen(Color.Black, 2))
                using (Font infoFont = new Font("Arial", 10))
                using (SolidBrush textBrush = new SolidBrush(Color.Black))
                {
                    if (currentDrawPoints.Count > 1)
                        e.Graphics.DrawLines(solidPen, currentDrawPoints.ToArray());

                    Point lastPoint = currentDrawPoints.Last();
                    e.Graphics.DrawLine(solidPen, lastPoint, currentMouseHoverPos);

                    // Расчет длины
                    double dx = currentMouseHoverPos.X - lastPoint.X;
                    double dy = currentMouseHoverPos.Y - lastPoint.Y;
                    double length = Math.Sqrt(dx * dx + dy * dy);

                    // Расчет угла (в градусах)
                    double angle = Math.Atan2(dy, dx) * (180 / Math.PI);
                    if (angle < 0) angle += 360; // Чтобы угол был от 0 до 360

                    // Вывод подсказки рядом с курсором
                    string info = $"{length:F1} | {angle:F1}°";
                    e.Graphics.DrawString(info, infoFont, textBrush, currentMouseHoverPos.X + 10, currentMouseHoverPos.Y + 10);
                }
            }
        }

        private void CanvasPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (isModifyingGroupMode && activeGroupToModify != null)
            {
                if (e.Button == MouseButtons.Left)
                {
                    // 1. Удаление фигуры из группы
                    Figure clickedInside = activeGroupToModify.SubFigures.LastOrDefault(f => f.Contains(e.Location));
                    if (clickedInside != null)
                    {
                        activeGroupToModify.SubFigures.Remove(clickedInside);
                        figures.Add(clickedInside);

                        if (activeGroupToModify.SubFigures.Count == 0)
                        {
                            figures.Remove(activeGroupToModify);
                            selectedFigure = null;
                            isModifyingGroupMode = false;
                            activeGroupToModify = null;
                            MessageBox.Show("В группе не осталось фигур. Группа удалена.");
                        }
                        else
                        {
                            // ПЕРЕСЧЕТ ЦЕНТРА после удаления
                            UpdateGroupCenter(activeGroupToModify);
                        }
                    }
                    else
                    {
                        // 2. Добавление фигуры в группу
                        Figure clickedOutside = figures.LastOrDefault(f => f != activeGroupToModify && f.Contains(e.Location));
                        if (clickedOutside != null)
                        {
                            figures.Remove(clickedOutside);
                            activeGroupToModify.SubFigures.Add(clickedOutside);

                            // ПЕРЕСЧЕТ ЦЕНТРА после добавления
                            UpdateGroupCenter(activeGroupToModify);
                        }
                    }
                }
                else if (e.Button == MouseButtons.Right)
                {
                    isModifyingGroupMode = false;
                    activeGroupToModify = null;
                }
                canvasPanel.Invalidate();
                return;
            }
            if (isDrawingMode)
            {
                if (e.Button == MouseButtons.Left) currentDrawPoints.Add(e.Location);
                else if (e.Button == MouseButtons.Right && currentDrawPoints.Count > 2) FinishCustomDrawing();
                canvasPanel.Invalidate(); return;
            }

            if (isGroupingMode)
            {
                if (e.Button == MouseButtons.Left)
                {
                    Figure clicked = figures.LastOrDefault(f => f.Contains(e.Location));
                    if (clicked != null)
                    {
                        if (currentGroupSelection.Contains(clicked)) currentGroupSelection.Remove(clicked);
                        else currentGroupSelection.Add(clicked);
                    }
                }
                else if (e.Button == MouseButtons.Right) FinishGrouping();
                canvasPanel.Invalidate(); return;
            }

            Figure hitFig = figures.LastOrDefault(f => f.Contains(e.Location));
            if (hitFig != null)
            {
                selectedFigure = hitFig;
                if (e.Button == MouseButtons.Left) { isDragging = true; lastMousePos = e.Location; }
                else if (e.Button == MouseButtons.Right)
                {
                    if (currentEditor != null && !currentEditor.IsDisposed) currentEditor.Close();
                    currentEditor = new EditorForm(selectedFigure, this, canvasPanel) { Owner = this };
                    currentEditor.Show();
                }
            }
            else selectedFigure = null;
            canvasPanel.Invalidate();
        }

        private void CanvasPanel_MouseMove(object sender, MouseEventArgs e)
        {
            currentMouseHoverPos = e.Location;
            if (isDragging && selectedFigure != null)
            {
                selectedFigure.Move(e.X - lastMousePos.X, e.Y - lastMousePos.Y);
                lastMousePos = e.Location;
            }
            canvasPanel.Invalidate();
        }

        private void FinishCustomDrawing()
        {
            if (currentDrawPoints.Count < 3)
            {
                MessageBox.Show("Нужно хотя бы 3 точки для создания фигуры.");
                isDrawingMode = false;
                currentDrawPoints.Clear();
                canvasPanel.Invalidate();
                return;
            }

            string name = ShowInputDialog("Имя фигуры:", "Сохранение");
            if (!string.IsNullOrWhiteSpace(name))
            {
                // 1. Находим геометрический центр нарисованных точек
                int minX = currentDrawPoints.Min(p => p.X);
                int maxX = currentDrawPoints.Max(p => p.X);
                int minY = currentDrawPoints.Min(p => p.Y);
                int maxY = currentDrawPoints.Max(p => p.Y);
                Point center = new Point((minX + maxX) / 2, (minY + maxY) / 2);

                // 2. Вычисляем относительные координаты (от центра)
                var relPoints = currentDrawPoints.Select(p => new PointF(p.X - center.X, p.Y - center.Y)).ToList();

                // 3. Создаем фигуру. Конструктор внутри сам заполнит Sides.
                var newFig = new CustomPolygon(center, relPoints);

                // Устанавливаем базовые свойства
                newFig.Size = 100; // Базовый масштаб
                newFig.FillColor = Color.FromArgb(100, Color.White); // Чтобы было легче попасть кликом

                // Настраиваем цвет и толщину каждой стороны (они уже созданы в конструкторе)
                foreach (var side in newFig.Sides)
                {
                    side.Color = Color.Black;
                    side.Thickness = 2;
                }

                customTemplates[name] = newFig;
                if (!cbShapeType.Items.Contains(name))
                    cbShapeType.Items.Add(name);

                figures.Add(newFig);
                selectedFigure = newFig;
            }

            isDrawingMode = false;
            currentDrawPoints.Clear();
            canvasPanel.Invalidate();
        }

        private void FinishGrouping()
        {
            if (currentGroupSelection.Count < 2) { isGroupingMode = false; return; }
            string name = ShowInputDialog("Имя группы:", "Группировка");
            if (!string.IsNullOrWhiteSpace(name))
            {
                // 1. Находим границы всей области выделенных фигур
                float minX = currentGroupSelection.Min(f => f.GetBounds().Left);
                float maxX = currentGroupSelection.Max(f => f.GetBounds().Right);
                float minY = currentGroupSelection.Min(f => f.GetBounds().Top);
                float maxY = currentGroupSelection.Max(f => f.GetBounds().Bottom);

                // 2. Рассчитываем истинный геометрический центр этой области
                Point groupCenter = new Point((int)(minX + maxX) / 2, (int)(minY + maxY) / 2);

                GroupFigure group = new GroupFigure(groupCenter);

                foreach (var f in currentGroupSelection)
                {
                    group.SubFigures.Add(f);
                    figures.Remove(f);
                }

                customTemplates[name] = group;
                cbShapeType.Items.Add(name);
                figures.Add(group);
                selectedFigure = group;
            }
            isGroupingMode = false;
            currentGroupSelection.Clear();
            canvasPanel.Invalidate();
        }

        private Figure CloneFigure(Figure source, Point newCenter, Point oldCenter)
        {
            Figure clone;
            if (source is GroupFigure g)
            {
                clone = new GroupFigure(newCenter);
                int dx = newCenter.X - oldCenter.X;
                int dy = newCenter.Y - oldCenter.Y;
                foreach (var sub in g.SubFigures)
                {
                    // Рекурсивно клонируем подфигуры
                    var subClone = CloneFigure(sub, new Point(sub.BaseLocation.X + dx, sub.BaseLocation.Y + dy), sub.BaseLocation);
                    ((GroupFigure)clone).SubFigures.Add(subClone);
                }
            }
            else if (source is CustomPolygon cp)
            {
                // Для кастомного полигона берем его геометрию (точки)
                var pointsCopy = cp.Sides.Select(s => s.RelativeOffset).ToList();
                clone = new CustomPolygon(newCenter, pointsCopy);
            }
            else
            {
                // Для стандартных фигур (Круг, Прямоугольник и т.д.)
                clone = (Figure)Activator.CreateInstance(source.GetType(), new object[] { newCenter });
            }

            // --- УСТАНОВКА БАЗОВЫХ СВОЙСТВ ДЛЯ КОПИИ ---

            // Оставляем только размер от оригинала, чтобы масштаб не ломался
            clone.Size = source.Size;

            // Устанавливаем базовую заливку (белый цвет)
            clone.FillColor = Color.Transparent;

            // Пересоздаем список сторон с базовыми параметрами (черный цвет, толщина 2)
            clone.Sides = source.Sides.Select(s => new SideStyle(s.RelativeOffset.X, s.RelativeOffset.Y)
            {
                Color = Color.Black, // Базовый цвет линий
                Thickness = 2        // Базовая толщина
            }).ToList();

            return clone;
        }

        public static string ShowInputDialog(string text, string caption)
        {
            Form prompt = new Form() { Width = 300, Height = 160, Text = caption, StartPosition = FormStartPosition.CenterScreen };
            Label lbl = new Label() { Left = 10, Top = 10, Width=250, Text = text };
            TextBox txt = new TextBox() { Left = 10, Top = 40, Width = 180 };
            Button ok = new Button() { Text = "ОК", Left = 200, Top = 38, Height= 30, Width = 70, DialogResult=DialogResult.OK };
            prompt.Controls.AddRange(new Control[] { lbl, txt, ok });
            return prompt.ShowDialog() == DialogResult.OK ? txt.Text : "";
        }
    }

    // ВТОРАЯ ЧАСТЬ КЛАССА (ДИЗАЙН)
    public partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private Panel toolbar;
        private DoubleBufferedPanel canvasPanel;
        private ComboBox cbShapeType;
        private Button btnAddShape;
        private Button btnClearAll;

        // ОБЪЯВЛЯЕМ НЕДОСТАЮЩИЕ КНОПКИ ЗДЕСЬ
        private Button btnDrawCustom;
        private Button btnGroupShapes;
        private Button btnUngroup;
        private Button btnModifyGroup;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.Text = "Графический редактор";
            this.Size = new Size(1300, 800);
            this.KeyPreview = true;
            this.Font = new Font("Segoe UI", 12F);

            toolbar = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(90, 150, 200), Padding = new Padding(10) };

            cbShapeType = new ComboBox { Location = new Point(15, 20), Size = new Size(200, 35), DropDownStyle = ComboBoxStyle.DropDownList };
            cbShapeType.Items.AddRange(new object[] { "Прямоугольник", "Треугольник", "Круг", "Трапеция", "Пятиугольник" });
            cbShapeType.SelectedIndex = 0;

            btnAddShape = new Button { Text = "Добавить", Location = new Point(230, 18), Size = new Size(130, 40), BackColor = Color.FromArgb(30, 90, 140), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnClearAll = new Button { Text = "Очистить", Location = new Point(370, 18), Size = new Size(130, 40), BackColor = Color.FromArgb(30, 90, 140), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            // Инициализация кнопок
            btnDrawCustom = new Button { Text = "Нарисовать", Location = new Point(510, 18), Size = new Size(150, 40), BackColor = Color.FromArgb(30, 90, 140), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnGroupShapes = new Button { Text = "Объединить", Location = new Point(670, 18), Size = new Size(150, 40), BackColor = Color.FromArgb(30, 90, 140), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnUngroup = new Button { Text = "Разделить", Location = new Point(830, 18), Size = new Size(150, 40), BackColor = Color.FromArgb(30, 90, 140), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            // Инициализация кнопки изменения
            btnModifyGroup = new Button { Text = "Изменить", Location = new Point(990, 18), Size = new Size(130, 40), BackColor = Color.FromArgb(30, 90, 140), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            // Обновленная строка добавления (добавили btnModifyGroup в конец)
            toolbar.Controls.AddRange(new Control[] { cbShapeType, btnAddShape, btnClearAll, btnDrawCustom, btnGroupShapes, btnUngroup, btnModifyGroup });
          //  toolbar.Controls.AddRange(new Control[] { cbShapeType, btnAddShape, btnClearAll, btnDrawCustom, btnGroupShapes, btnUngroup });
            canvasPanel = new DoubleBufferedPanel { Dock = DockStyle.Fill, BackColor = Color.WhiteSmoke };

            this.Controls.Add(canvasPanel);
            this.Controls.Add(toolbar);
        }
    }
    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }
    }
}