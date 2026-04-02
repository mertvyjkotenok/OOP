using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
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

        // НОВОЕ: Переменная для хранения индекса подсвеченной стороны
        public int HighlightedSideIndex { get; set; } = -1;

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

            btnAddShape.Click += (s, e) => CreateFigureFromUI();
            btnClearAll.Click += (s, e) => {
                figures.Clear();
                selectedFigure = null;
                canvasPanel.Invalidate();
            };

            btnDrawCustom.Click += BtnDrawCustom_Click;
            btnGroupShapes.Click += BtnGroupShapes_Click;
            btnUngroup.Click += BtnUngroup_Click;

            btnSave.Click += BtnSave_Click;
            btnLoad.Click += BtnLoad_Click;

            this.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Escape) Application.Exit();
            };

            canvasPanel.Paint += CanvasPanel_Paint;
            canvasPanel.MouseDown += CanvasPanel_MouseDown;
            canvasPanel.MouseMove += CanvasPanel_MouseMove;
            canvasPanel.MouseUp += (s, e) => isDragging = false;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "JSON файлы (*.json)|*.json|Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
                sfd.Title = "Сохранить проект";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        List<FigureData> dataList = figures.Select(MapToData).ToList();
                        var options = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
                        string json = JsonSerializer.Serialize(dataList, options);
                        File.WriteAllText(sfd.FileName, json);
                        MessageBox.Show("Фигуры успешно сохранены!", "Успех");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при сохранении: " + ex.Message, "Ошибка");
                    }
                }
            }
        }

        private void BtnLoad_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "JSON файлы (*.json)|*.json|Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
                ofd.Title = "Загрузить проект";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string json = File.ReadAllText(ofd.FileName);
                        List<FigureData> dataList = JsonSerializer.Deserialize<List<FigureData>>(json);

                        if (dataList != null)
                        {
                            figures.Clear();
                            selectedFigure = null;
                            HighlightedSideIndex = -1; // Сбрасываем выделение

                            foreach (var data in dataList)
                            {
                                Figure fig = MapToFigure(data);
                                if (fig != null) figures.Add(fig);
                            }
                            canvasPanel.Invalidate();
                            MessageBox.Show("Проект успешно загружен!", "Успех");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при загрузке: " + ex.Message, "Ошибка");
                    }
                }
            }
        }

        private FigureData MapToData(Figure f)
        {
            var data = new FigureData
            {
                TypeName = f.GetType().Name,
                BaseX = f.BaseLocation.X,
                BaseY = f.BaseLocation.Y,
                RelX = f.RelativePivot.X,
                RelY = f.RelativePivot.Y,
                Size = f.Size,
                FillColorArgb = f.FillColor.ToArgb(),
                Sides = new List<SideData>()
            };

            // НОВОЕ: Высчитываем длину стороны для удобного чтения JSON
            for (int i = 0; i < f.Sides.Count; i++)
            {
                var currentSide = f.Sides[i];
                var nextSide = f.Sides[(i + 1) % f.Sides.Count];

                // Считаем расстояние между вершинами (используем теорему Пифагора)
                float dx = nextSide.RelativeOffset.X - currentSide.RelativeOffset.X;
                float dy = nextSide.RelativeOffset.Y - currentSide.RelativeOffset.Y;
                double length = Math.Round(Math.Sqrt(dx * dx + dy * dy), 2);

                data.Sides.Add(new SideData
                {
                    Info = $"Сторона {i + 1}: длина {length}, толщина {currentSide.Thickness}", // Понятное описание!
                    ColorArgb = currentSide.Color.ToArgb(),
                    Thickness = currentSide.Thickness,
                    RelOffsetX = currentSide.RelativeOffset.X,
                    RelOffsetY = currentSide.RelativeOffset.Y
                });
            }

            if (f is GroupFigure group)
            {
                data.SubFigures = group.SubFigures.Select(MapToData).ToList();
            }
            return data;
        }

        private Figure MapToFigure(FigureData data)
        {
            Point baseLoc = new Point(data.BaseX, data.BaseY);
            Figure f = null;

            switch (data.TypeName)
            {
                case "Rectangle": f = new Lab1.Figures.Rectangle(baseLoc); break;
                case "Triangle": f = new Triangle(baseLoc); break;
                case "Circle": f = new Circle(baseLoc); break;
                case "Trapezium": f = new Trapezium(baseLoc); break;
                case "Pentagon": f = new Pentagon(baseLoc); break;
                case "CustomPolygon": f = new CustomPolygon(baseLoc); break;
                case "GroupFigure":
                    var group = new GroupFigure(baseLoc);
                    foreach (var subData in data.SubFigures)
                    {
                        var subFig = MapToFigure(subData);
                        if (subFig != null) group.SubFigures.Add(subFig);
                    }
                    f = group;
                    break;
            }

            if (f != null)
            {
                f.RelativePivot = new PointF(data.RelX, data.RelY);
                f.Size = data.Size;
                f.FillColor = Color.FromArgb(data.FillColorArgb);

                f.Sides.Clear();
                foreach (var sData in data.Sides)
                {
                    f.Sides.Add(new SideStyle(sData.RelOffsetX, sData.RelOffsetY)
                    {
                        Color = Color.FromArgb(sData.ColorArgb),
                        Thickness = sData.Thickness
                    });
                }
            }
            return f;
        }

        private void BtnModifyGroup_Click(object sender, EventArgs e)
        {
            if (selectedFigure is GroupFigure group)
            {
                isModifyingGroupMode = true;
                isDrawingMode = false;
                isGroupingMode = false;
                activeGroupToModify = group;
                MessageBox.Show("Режим редактирования группы.\nЛКМ по фигуре вне группы — добавить в группу.\nЛКМ по фигуре внутри группы — извлечь из группы.\nПКМ по фигуре внутри группы — изменить её свойства.\nПКМ по пустому месту — завершить.", "Редактирование");
            }
            else MessageBox.Show("Сначала выделите группу для изменения.");
        }

        public void DeleteFigure(Figure fig)
        {
            if (figures.Contains(fig))
            {
                figures.Remove(fig);
            }
            else
            {
                // Если фигуры нет в главном списке, ищем её внутри групп
                foreach (var group in figures.OfType<GroupFigure>().ToList())
                {
                    if (group.SubFigures.Contains(fig))
                    {
                        group.SubFigures.Remove(fig);
                        // Если группа стала пустой, удаляем и её
                        if (group.SubFigures.Count == 0) figures.Remove(group);
                        break;
                    }
                }
            }

            if (selectedFigure == fig) selectedFigure = null;
            HighlightedSideIndex = -1;
            canvasPanel.Invalidate();
        }

        private void UpdateGroupCenter(GroupFigure group)
        {
            if (group.SubFigures.Count == 0) return;
            float minX = group.SubFigures.Min(f => f.GetBounds().Left);
            float maxX = group.SubFigures.Max(f => f.GetBounds().Right);
            float minY = group.SubFigures.Min(f => f.GetBounds().Top);
            float maxY = group.SubFigures.Max(f => f.GetBounds().Bottom);
            Point newCenter = new Point((int)(minX + maxX) / 2, (int)(minY + maxY) / 2);
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

        private void BtnUngroup_Click(object sender, EventArgs e)
        {
            if (selectedFigure is GroupFigure group)
            {
                foreach (var subFig in group.SubFigures) figures.Add(subFig);
                figures.Remove(group);
                selectedFigure = null;
                canvasPanel.Invalidate();
                MessageBox.Show("Группа разделена на части.");
            }
            else MessageBox.Show("Выберите группу для разделения.");
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

            foreach (var fig in figures) fig.Draw(e.Graphics);

            if (selectedFigure != null && !isDrawingMode && !isGroupingMode)
            {
                var bounds = selectedFigure.GetBounds();
                Color selectionColor = Color.Black;

                if (selectedFigure is PolygonBase poly)
                {
                    if (poly.Sides.Count > 0 && poly.Sides[0].Color.R == 0 && poly.Sides[0].Color.G == 0 && poly.Sides[0].Color.B == 0)
                        selectionColor = Color.DeepSkyBlue;
                }
                else if (selectedFigure is GroupFigure group && group.SubFigures.Count > 0)
                {
                    if (group.SubFigures[0] is PolygonBase firstPoly && firstPoly.Sides.Count > 0 && firstPoly.Sides[0].Color == Color.Black)
                        selectionColor = Color.DeepSkyBlue;
                }

                using (Pen selectionPen = new Pen(selectionColor, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                {
                    e.Graphics.DrawRectangle(selectionPen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
                }
                e.Graphics.FillEllipse(Brushes.Blue, selectedFigure.Center.X - 3, selectedFigure.Center.Y - 3, 6, 6);

                // НОВОЕ: Отрисовка подсветки выбранной стороны поверх фигуры
                if (HighlightedSideIndex >= 0 && HighlightedSideIndex < selectedFigure.Sides.Count)
                {
                    var side = selectedFigure.Sides[HighlightedSideIndex];
                    var nextSide = selectedFigure.Sides[(HighlightedSideIndex + 1) % selectedFigure.Sides.Count];

                    // Учитываем текущий масштаб фигуры
                    float scale = selectedFigure.Size / 100f;

                    // Вычисляем абсолютные координаты начала и конца линии
                    PointF p1 = new PointF(selectedFigure.Center.X + side.RelativeOffset.X * scale, selectedFigure.Center.Y + side.RelativeOffset.Y * scale);
                    PointF p2 = new PointF(selectedFigure.Center.X + nextSide.RelativeOffset.X * scale, selectedFigure.Center.Y + nextSide.RelativeOffset.Y * scale);

                    // Рисуем толстую пурпурную линию для стороны и круг на ее "базовой" вершине
                    using (Pen highlightPen = new Pen(Color.Magenta, side.Thickness + 3))
                    {
                        e.Graphics.DrawLine(highlightPen, p1, p2);
                    }
                    e.Graphics.FillEllipse(Brushes.Magenta, p1.X - 6, p1.Y - 6, 12, 12);
                }
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
                    e.Graphics.DrawRectangle(editGroupPen, activeGroupToModify.GetBounds());
            }

            if (isDrawingMode && currentDrawPoints.Count > 0)
            {
                using (Pen solidPen = new Pen(Color.Black, 2))
                using (Font infoFont = new Font("Arial", 10))
                using (SolidBrush textBrush = new SolidBrush(Color.Black))
                {
                    if (currentDrawPoints.Count > 1) e.Graphics.DrawLines(solidPen, currentDrawPoints.ToArray());

                    Point lastPoint = currentDrawPoints.Last();
                    e.Graphics.DrawLine(solidPen, lastPoint, currentMouseHoverPos);

                    double dx = currentMouseHoverPos.X - lastPoint.X;
                    double dy = currentMouseHoverPos.Y - lastPoint.Y;
                    double length = Math.Sqrt(dx * dx + dy * dy);

                    double angle = Math.Atan2(dy, dx) * (180 / Math.PI);
                    if (angle < 0) angle += 360;

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
                        else UpdateGroupCenter(activeGroupToModify);
                    }
                    else
                    {
                        Figure clickedOutside = figures.LastOrDefault(f => f != activeGroupToModify && f.Contains(e.Location));
                        if (clickedOutside != null)
                        {
                            figures.Remove(clickedOutside);
                            activeGroupToModify.SubFigures.Add(clickedOutside);
                            UpdateGroupCenter(activeGroupToModify);
                        }
                    }
                }
                else if (e.Button == MouseButtons.Right) 
                {
                    Figure clickedInside = activeGroupToModify.SubFigures.LastOrDefault(f => f.Contains(e.Location));
                    if (clickedInside != null)
                    {
                        // Открываем редактор только для одной фигуры из группы!
                        if (currentEditor != null && !currentEditor.IsDisposed) currentEditor.Close();
                        currentEditor = new EditorForm(clickedInside, this, canvasPanel) { Owner = this };
                        currentEditor.Show();
                    }
                    else
                    {
                        // Если кликнули мимо фигур - выходим из режима редактирования
                        isModifyingGroupMode = false;
                        activeGroupToModify = null;
                    }
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
            else
            {
                selectedFigure = null;
                HighlightedSideIndex = -1; // Сброс подсветки при клике в пустое место
            }
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
                int minX = currentDrawPoints.Min(p => p.X);
                int maxX = currentDrawPoints.Max(p => p.X);
                int minY = currentDrawPoints.Min(p => p.Y);
                int maxY = currentDrawPoints.Max(p => p.Y);
                Point center = new Point((minX + maxX) / 2, (minY + maxY) / 2);

                var relPoints = currentDrawPoints.Select(p => new PointF(p.X - center.X, p.Y - center.Y)).ToList();
                var newFig = new CustomPolygon(center, relPoints);

                newFig.Size = 100;
                newFig.FillColor = Color.FromArgb(100, Color.White);

                foreach (var side in newFig.Sides) { side.Color = Color.Black; side.Thickness = 2; }

                customTemplates[name] = newFig;
                if (!cbShapeType.Items.Contains(name)) cbShapeType.Items.Add(name);

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
                float minX = currentGroupSelection.Min(f => f.GetBounds().Left);
                float maxX = currentGroupSelection.Max(f => f.GetBounds().Right);
                float minY = currentGroupSelection.Min(f => f.GetBounds().Top);
                float maxY = currentGroupSelection.Max(f => f.GetBounds().Bottom);

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
                    var subClone = CloneFigure(sub, new Point(sub.BaseLocation.X + dx, sub.BaseLocation.Y + dy), sub.BaseLocation);
                    ((GroupFigure)clone).SubFigures.Add(subClone);
                }
            }
            else if (source is CustomPolygon cp)
            {
                var pointsCopy = cp.Sides.Select(s => s.RelativeOffset).ToList();
                clone = new CustomPolygon(newCenter, pointsCopy);
            }
            else
            {
                clone = (Figure)Activator.CreateInstance(source.GetType(), new object[] { newCenter });
            }

            clone.Size = source.Size;
            clone.FillColor = Color.Transparent;
            clone.Sides = source.Sides.Select(s => new SideStyle(s.RelativeOffset.X, s.RelativeOffset.Y)
            {
                Color = Color.Black,
                Thickness = 2
            }).ToList();

            return clone;
        }

        public static string ShowInputDialog(string text, string caption)
        {
            Form prompt = new Form() { Width = 300, Height = 160, Text = caption, StartPosition = FormStartPosition.CenterScreen };
            Label lbl = new Label() { Left = 10, Top = 10, Width = 250, Text = text };
            TextBox txt = new TextBox() { Left = 10, Top = 40, Width = 180 };
            Button ok = new Button() { Text = "ОК", Left = 200, Top = 38, Height = 30, Width = 70, DialogResult = DialogResult.OK };
            prompt.Controls.AddRange(new Control[] { lbl, txt, ok });
            return prompt.ShowDialog() == DialogResult.OK ? txt.Text : "";
        }
    }

    public partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private Panel toolbar;
        private DoubleBufferedPanel canvasPanel;
        private ComboBox cbShapeType;
        private Button btnAddShape;
        private Button btnClearAll;
        private Button btnDrawCustom;
        private Button btnGroupShapes;
        private Button btnUngroup;
        private Button btnModifyGroup;
        private Button btnSave;
        private Button btnLoad;

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

            toolbar = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(100, 160, 210), Padding = new Padding(10) };

            cbShapeType = new ComboBox { Location = new Point(15, 20), Size = new Size(200, 35), DropDownStyle = ComboBoxStyle.DropDownList };
            cbShapeType.Items.AddRange(new object[] { "Прямоугольник", "Треугольник", "Круг", "Трапеция", "Пятиугольник" });
            cbShapeType.SelectedIndex = 0;

            btnAddShape = new Button { Text = "Добавить", Location = new Point(230, 18), Size = new Size(130, 40), BackColor = Color.FromArgb(30, 90, 140), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnClearAll = new Button { Text = "Очистить", Location = new Point(370, 18), Size = new Size(130, 40), BackColor = Color.FromArgb(30, 90, 140), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            btnDrawCustom = new Button { Text = "Нарисовать", Location = new Point(510, 18), Size = new Size(150, 40), BackColor = Color.FromArgb(30, 90, 140), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnGroupShapes = new Button { Text = "Объединить", Location = new Point(670, 18), Size = new Size(150, 40), BackColor = Color.FromArgb(30, 90, 140), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnUngroup = new Button { Text = "Разделить", Location = new Point(830, 18), Size = new Size(150, 40), BackColor = Color.FromArgb(30, 90, 140), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnModifyGroup = new Button { Text = "Изменить", Location = new Point(990, 18), Size = new Size(130, 40), BackColor = Color.FromArgb(30, 90, 140), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            btnSave = new Button { Text = "Сохранить", Location = new Point(1130, 18), Size = new Size(130, 40), BackColor = Color.FromArgb(60, 120, 170), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnLoad = new Button { Text = "Загрузить", Location = new Point(1270, 18), Size = new Size(130, 40), BackColor = Color.FromArgb(60, 120, 170), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            toolbar.Controls.AddRange(new Control[] { cbShapeType, btnAddShape, btnClearAll, btnDrawCustom, btnGroupShapes, btnUngroup, btnModifyGroup, btnSave, btnLoad });

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

    public class FigureData
    {
        public string TypeName { get; set; }
        public int BaseX { get; set; }
        public int BaseY { get; set; }
        public float RelX { get; set; }
        public float RelY { get; set; }
        public int Size { get; set; }
        public int FillColorArgb { get; set; }
        public List<SideData> Sides { get; set; } = new List<SideData>();
        public List<FigureData> SubFigures { get; set; } = new List<FigureData>();
    }

    public class SideData
    {
        public string Info { get; set; } // НОВОЕ: Читаемое описание стороны в JSON
        public int ColorArgb { get; set; }
        public float Thickness { get; set; }
        public float RelOffsetX { get; set; }
        public float RelOffsetY { get; set; }
    }
}