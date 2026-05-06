using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
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

        public int HighlightedSideIndex { get; set; } = -1;

        private bool isDrawingMode = false;
        private List<Point> currentDrawPoints = new List<Point>();
        private Point currentMouseHoverPos;

        private bool isGroupingMode = false;
        private List<Figure> currentGroupSelection = new List<Figure>();
        private Dictionary<string, Figure> customTemplates = new Dictionary<string, Figure>();

        private bool isModifyingGroupMode = false;
        private GroupFigure activeGroupToModify = null;

        private enum HandleType
        {
            None, Move, ResizeN, ResizeS, ResizeE, ResizeW,
            ResizeNE, ResizeNW, ResizeSE, ResizeSW, Rotate,
            Focus1, Focus2
        }
        private HandleType activeHandle = HandleType.None;
        private PointF handleStartMouse;
        private RectangleF handleStartBounds;
        private float handleStartRotation;
        private float handleStartRadiusX, handleStartRadiusY;

        private const int HandleSize = 10;

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
                UpdateTreeView();
            };
            btnGroupShapes.Click += BtnGroupShapes_Click;
            btnUngroup.Click += BtnUngroup_Click;
           
            btnLoad.Click += BtnLoad_Click;

            this.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Escape) Application.Exit();
            };

            canvasPanel.Paint += CanvasPanel_Paint;
            canvasPanel.MouseDown += CanvasPanel_MouseDown;
            canvasPanel.MouseMove += CanvasPanel_MouseMove;
            canvasPanel.MouseUp += (s, e) => isDragging = false;
            canvasPanel.MouseUp += CanvasPanel_MouseUp;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.AutoUpgradeEnabled = false;
                sfd.Filter = "JSON файлы (*.json)|*.json|Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
                sfd.Title = "Сохранить проект";
                
                if (sfd.ShowDialog(this) == DialogResult.OK)
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
                ofd.AutoUpgradeEnabled = false;
                ofd.Filter = "JSON файлы (*.json)|*.json";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string json = File.ReadAllText(ofd.FileName).Trim();
                        List<FigureData> dataToProcess = new List<FigureData>();

                        if (json.StartsWith("["))
                        {
                            dataToProcess = JsonSerializer.Deserialize<List<FigureData>>(json);
                        }
                        else
                        {
                            var single = JsonSerializer.Deserialize<FigureData>(json);
                            if (single != null) dataToProcess.Add(single);
                        }

                        if (dataToProcess != null)
                        {
                            foreach (var data in dataToProcess)
                            {
                                Figure fig = MapToFigure(data);
                                if (fig != null)
                                {
                                    if (figures.Count > 0) fig.Move(20, 20);
                                    figures.Add(fig);
                                }
                            }
                            UpdateTreeView();
                            canvasPanel.Invalidate();
                            MessageBox.Show($"Добавлено объектов: {dataToProcess.Count}");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка загрузки: " + ex.Message);
                    }
                }
            }
        }

        private void BtnSaveSelected_Click(object sender, EventArgs e)
        {
            // Определяем список фигур для сохранения
            List<Figure> toSave = new List<Figure>();

            // 1. Сначала собираем все фигуры, отмеченные галочками в списке (TreeView)
            CollectCheckedFigures(tvFigures.Nodes, toSave);

            // 2. Если галочки не расставлены, проверяем режим объединения
            if (toSave.Count == 0 && isGroupingMode && currentGroupSelection.Count > 0)
            {
                toSave.AddRange(currentGroupSelection);
            }
            // 3. Если и там пусто, берем просто выделенную на холсте фигуру
            else if (toSave.Count == 0 && selectedFigure != null)
            {
                toSave.Add(selectedFigure);
            }

            // Если вообще ничего не выбрано, предупреждаем пользователя
            if (toSave.Count == 0)
            {
                MessageBox.Show("Сначала выберите одну или несколько фигур (отметьте галочкой в списке).", "Внимание");
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.AutoUpgradeEnabled = false;
                sfd.Filter = "JSON фигуры (*.json)|*.json";
                sfd.Title = "Сохранить выбранные фигуры";

                // Предлагаем имя файла по первой фигуре или общее
                sfd.FileName = toSave.Count == 1 ? (toSave[0].Name ?? "Figure") : "SelectedShapes";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Преобразуем список фигур в список данных для JSON
                        List<FigureData> dataList = toSave.Select(MapToData).ToList();

                        var options = new JsonSerializerOptions
                        {
                            WriteIndented = true,
                            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        };

                        string json = JsonSerializer.Serialize(dataList, options);
                        File.WriteAllText(sfd.FileName, json);

                        MessageBox.Show($"Успешно сохранено объектов: {toSave.Count}", "Успех");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при сохранении: " + ex.Message, "Ошибка");
                    }
                }
            }
        }

        private void CollectCheckedFigures(TreeNodeCollection nodes, List<Figure> toSave)
        {
            foreach (TreeNode node in nodes)
            {
                // Если узел отмечен галочкой и содержит фигуру
                if (node.Checked && node.Tag is Figure fig)
                {
                    toSave.Add(fig);
                    // Если мы сохраняем группу целиком, нам не нужно дублировать и сохранять 
                    // её отдельные внутренние элементы, поэтому пропускаем их
                    continue;
                }

                // Если текущий узел не отмечен (или это развернутая группа), 
                // рекурсивно проверяем вложенные элементы
                CollectCheckedFigures(node.Nodes, toSave);
            }
        }

        private FigureData MapToData(Figure f)
        {
            var data = new FigureData
            {
                Id = f.Id.ToString(),
                Name = f.Name,
                TypeName = f.GetType().Name,
                BaseX = f.BaseLocation.X,
                BaseY = f.BaseLocation.Y,
                RelX = f.RelativePivot.X,
                RelY = f.RelativePivot.Y,
                Size = f.Size,
                FillColorArgb = f.FillColor.ToArgb(),
                Sides = new List<SideData>()
            };

            for (int i = 0; i < f.Sides.Count; i++)
            {
                var currentSide = f.Sides[i];
                var nextSide = f.Sides[(i + 1) % f.Sides.Count];

                float dx = nextSide.RelativeOffset.X - currentSide.RelativeOffset.X;
                float dy = nextSide.RelativeOffset.Y - currentSide.RelativeOffset.Y;
                double length = Math.Round(Math.Sqrt(dx * dx + dy * dy), 2);

                data.Sides.Add(new SideData
                {
                    Info = $"Сторона {i + 1}: длина {length}, толщина {currentSide.Thickness}",
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
                case "Polygon":
                    var vertices = data.Sides.Select(s => new PointF(s.RelOffsetX, s.RelOffsetY)).ToArray();
                    f = new Polygon(baseLoc, vertices);
                    break;
                case "Ellipse":
                    float rx = data.Sides.Count > 0 ? data.Sides[0].RelOffsetX : 50;
                    float ry = data.Sides.Count > 0 ? data.Sides[0].RelOffsetY : 50;
                    f = new Ellipse(baseLoc, rx, ry);
                    break;
                case "GroupFigure":
                    var group = new GroupFigure(baseLoc);
                    foreach (var subData in data.SubFigures)
                    {
                        var subFig = MapToFigure(subData);
                        if (subFig != null) group.SubFigures.Add(subFig);
                    }
                    f = group;
                    break;
                default:
                    // Для обратной совместимости со старыми сохранениями (если есть)
                    // Можно добавить обработку старых типов или просто пропустить
                    MessageBox.Show($"Неизвестный тип фигуры: {data.TypeName}. Фигура будет пропущена.");
                    return null;
            }

            if (f != null)
            {
                f.RelativePivot = new PointF(data.RelX, data.RelY);
                f.Size = data.Size;
                f.FillColor = Color.FromArgb(data.FillColorArgb);
                
                f.Name = data.Name;

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
                if (fig is GroupFigure groupFig)
                {
                    RemoveTemplateIfExist(groupFig.Name);
                }
                figures.Remove(fig);
            }
            else
            {
                foreach (var group in figures.OfType<GroupFigure>().ToList())
                {
                    if (group.SubFigures.Contains(fig))
                    {
                        group.SubFigures.Remove(fig);
                        if (group.SubFigures.Count == 0)
                        {
                            RemoveTemplateIfExist(group.Name);
                            figures.Remove(group);
                        }
                        else
                        {
                            UpdateGroupCenter(group);
                        }
                        break;
                    }
                }
            }

            if (selectedFigure == fig) selectedFigure = null;
            HighlightedSideIndex = -1;
            UpdateTreeView();
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
                RemoveTemplateIfExist(group.Name);

                foreach (var subFig in group.SubFigures)
                    figures.Add(subFig);

                figures.Remove(group);
                selectedFigure = null;

                UpdateTreeView();
                canvasPanel.Invalidate();
                MessageBox.Show("Группа разделена, шаблон удален.");
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
            {
                f = CloneFigure(customTemplates[selectedType], center, customTemplates[selectedType].BaseLocation);
                f.Name = selectedType;
            }
            else
            {
                switch (selectedType)
                {
                    case "Прямоугольник":
                        f = new Polygon(center, new[] {
                            new PointF(-50, -50), new PointF(50, -50),
                            new PointF(50, 50), new PointF(-50, 50)
                        });
                        break;
                    case "Треугольник":
                        f = new Polygon(center, new[] {
                            new PointF(0, -50), new PointF(45, 40), new PointF(-45, 40)
                        });
                        break;
                    case "Эллипс":
                        f = new Ellipse(center, 50, 50);
                        break;
                    case "Трапеция":
                        f = new Polygon(center, new[] {
                            new PointF(-30, -30), new PointF(30, -30),
                            new PointF(60, 30), new PointF(-60, 30)
                        });
                        break;
                    case "Пятиугольник":
                        var vertices = new List<PointF>();
                        for (int i = 0; i < 5; i++)
                        {
                            double angle = -Math.PI / 2 + i * 2 * Math.PI / 5;
                            vertices.Add(new PointF((float)(60 * Math.Cos(angle)), (float)(60 * Math.Sin(angle))));
                        }
                        f = new Polygon(center, vertices);
                        break;
                    case "Новая фигура":
                        isModifyingGroupMode = false;
                        isDrawingMode = true;
                        isGroupingMode = false;
                        currentDrawPoints.Clear();
                        selectedFigure = null;
                        MessageBox.Show("Режим рисования включен.\nЛКМ - точки, ПКМ - завершить.", "Рисование");
                        break;
                }
                if (f != null) f.Name = selectedType;
            }

            if (f != null)
            {
                figures.Add(f);
                selectedFigure = f;
                UpdateTreeView();
                canvasPanel.Invalidate();
            }
        }

        private void CanvasPanel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            foreach (var fig in figures) fig.Draw(e.Graphics);

            if (selectedFigure != null && !isDrawingMode && !isGroupingMode && !isModifyingGroupMode)
            {
                var bounds = selectedFigure.GetBounds();
                Color selectionColor = Color.Black;

                if (selectedFigure is Polygon poly)
                {
                    if (poly.Sides.Count > 0 && poly.Sides[0].Color.R == 0 && poly.Sides[0].Color.G == 0 && poly.Sides[0].Color.B == 0)
                        selectionColor = Color.DeepSkyBlue;
                }
                else if (selectedFigure is Ellipse ellipse)
                {
                    if (ellipse.Sides.Count > 0 && ellipse.Sides[0].Color.R == 0 && ellipse.Sides[0].Color.G == 0 && ellipse.Sides[0].Color.B == 0)
                        selectionColor = Color.DeepSkyBlue;
                }
                else if (selectedFigure is GroupFigure group && group.SubFigures.Count > 0)
                {
                    var first = group.SubFigures[0];
                    if ((first is Polygon p && p.Sides.Count > 0 && p.Sides[0].Color == Color.Black) ||
                        (first is Ellipse el && el.Sides.Count > 0 && el.Sides[0].Color == Color.Black))
                        selectionColor = Color.DeepSkyBlue;
                }

                using (Pen selectionPen = new Pen(selectionColor, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                {
                    e.Graphics.DrawRectangle(selectionPen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
                }
                e.Graphics.FillEllipse(Brushes.Blue, selectedFigure.Center.X - 3, selectedFigure.Center.Y - 3, 6, 6);

                if (selectedFigure != null && !(selectedFigure is Ellipse) &&
    HighlightedSideIndex >= 0 && HighlightedSideIndex < selectedFigure.Sides.Count)
                {
                    var side = selectedFigure.Sides[HighlightedSideIndex];
                    var nextSide = selectedFigure.Sides[(HighlightedSideIndex + 1) % selectedFigure.Sides.Count];

                    float scale = selectedFigure.Size / 100f;

                    PointF p1 = new PointF(selectedFigure.Center.X + side.RelativeOffset.X * scale,
                                           selectedFigure.Center.Y + side.RelativeOffset.Y * scale);
                    PointF p2 = new PointF(selectedFigure.Center.X + nextSide.RelativeOffset.X * scale,
                                           selectedFigure.Center.Y + nextSide.RelativeOffset.Y * scale);

                    using (Pen highlightPen = new Pen(Color.Magenta, side.Thickness + 3))
                    {
                        e.Graphics.DrawLine(highlightPen, p1, p2);
                    }
                    e.Graphics.FillEllipse(Brushes.Magenta, p1.X - 6, p1.Y - 6, 12, 12);
                }
                if (selectedFigure is Ellipse)
                {
                    DrawHandles(e.Graphics, bounds);
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

        private void DrawHandles(Graphics g, RectangleF bounds)
        {
            float x = bounds.X, y = bounds.Y, w = bounds.Width, h = bounds.Height;
            float midX = x + w / 2, midY = y + h / 2;

            RectangleF[] rects = new RectangleF[]
            {
                new RectangleF(x - HandleSize/2, y - HandleSize/2, HandleSize, HandleSize),
                new RectangleF(midX - HandleSize/2, y - HandleSize/2, HandleSize, HandleSize),
                new RectangleF(x + w - HandleSize/2, y - HandleSize/2, HandleSize, HandleSize),
                new RectangleF(x + w - HandleSize/2, midY - HandleSize/2, HandleSize, HandleSize),
                new RectangleF(x + w - HandleSize/2, y + h - HandleSize/2, HandleSize, HandleSize),
                new RectangleF(midX - HandleSize/2, y + h - HandleSize/2, HandleSize, HandleSize),
                new RectangleF(x - HandleSize/2, y + h - HandleSize/2, HandleSize, HandleSize),
                new RectangleF(x - HandleSize/2, midY - HandleSize/2, HandleSize, HandleSize)
            };

            foreach (var r in rects)
            {
                g.FillRectangle(Brushes.White, r);
                g.DrawRectangle(Pens.Black, r);
            }

            PointF rotateCenter = new PointF(midX, y - 25);
            float rotateRadius = 5;
            g.DrawLine(Pens.Black, midX, y, midX, y - 20);
            g.FillEllipse(Brushes.White, rotateCenter.X - rotateRadius, rotateCenter.Y - rotateRadius, rotateRadius * 2, rotateRadius * 2);
            g.DrawEllipse(Pens.Black, rotateCenter.X - rotateRadius, rotateCenter.Y - rotateRadius, rotateRadius * 2, rotateRadius * 2);

            // --- ОТРИСОВКА ФОКУСОВ ---
            if (selectedFigure is Ellipse ell)
            {
                PointF f1 = ell.GetFocus1();
                PointF f2 = ell.GetFocus2();

                using (Brush fb = new SolidBrush(Color.Orange))
                using (Pen fp = new Pen(Color.DarkRed, 2))
                {
                    g.FillEllipse(fb, f1.X - 5, f1.Y - 5, 10, 10);
                    g.DrawEllipse(fp, f1.X - 5, f1.Y - 5, 10, 10);

                    g.FillEllipse(fb, f2.X - 5, f2.Y - 5, 10, 10);
                    g.DrawEllipse(fp, f2.X - 5, f2.Y - 5, 10, 10);
                }
            }
        }

        private HandleType HitTestHandles(PointF mouse, RectangleF bounds)
        {
            if (selectedFigure == null || !(selectedFigure is Ellipse)) return HandleType.None;

            Ellipse ell = selectedFigure as Ellipse;

            // Проверка попадания в фокусы (делаем в первую очередь, чтобы они перехватывали клик у других маркеров)
            PointF f1 = ell.GetFocus1();
            if (new RectangleF(f1.X - 6, f1.Y - 6, 12, 12).Contains(mouse)) return HandleType.Focus1;
            PointF f2 = ell.GetFocus2();
            if (new RectangleF(f2.X - 6, f2.Y - 6, 12, 12).Contains(mouse)) return HandleType.Focus2;

            float x = bounds.X, y = bounds.Y, w = bounds.Width, h = bounds.Height;
            float midX = x + w / 2, midY = y + h / 2;

            RectangleF rotateRect = new RectangleF(midX - 10, y - 30, 20, 30);
            if (rotateRect.Contains(mouse)) return HandleType.Rotate;

            RectangleF[] handleRects = new RectangleF[]
            {
                new RectangleF(x - HandleSize/2, y - HandleSize/2, HandleSize, HandleSize),
                new RectangleF(midX - HandleSize/2, y - HandleSize/2, HandleSize, HandleSize),
                new RectangleF(x + w - HandleSize/2, y - HandleSize/2, HandleSize, HandleSize),
                new RectangleF(x + w - HandleSize/2, midY - HandleSize/2, HandleSize, HandleSize),
                new RectangleF(x + w - HandleSize/2, y + h - HandleSize/2, HandleSize, HandleSize),
                new RectangleF(midX - HandleSize/2, y + h - HandleSize/2, HandleSize, HandleSize),
                new RectangleF(x - HandleSize/2, y + h - HandleSize/2, HandleSize, HandleSize),
                new RectangleF(x - HandleSize/2, midY - HandleSize/2, HandleSize, HandleSize)
            };

            HandleType[] types = { HandleType.ResizeNW, HandleType.ResizeN, HandleType.ResizeNE,
                                   HandleType.ResizeE, HandleType.ResizeSE, HandleType.ResizeS,
                                   HandleType.ResizeSW, HandleType.ResizeW };

            for (int i = 0; i < handleRects.Length; i++)
                if (handleRects[i].Contains(mouse)) return types[i];

            return HandleType.None;
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
                            SelectNodeByFigure(null);
                            MessageBox.Show("В группе не осталось фигур. Группа удалена.");
                        }
                        else
                        {
                            UpdateGroupCenter(activeGroupToModify);
                            selectedFigure = clickedInside;
                            SelectNodeByFigure(selectedFigure);
                        }
                        UpdateTreeView();
                    }
                    else
                    {
                        Figure clickedOutside = figures.LastOrDefault(f => f != activeGroupToModify && f.Contains(e.Location));
                        if (clickedOutside != null)
                        {
                            figures.Remove(clickedOutside);
                            activeGroupToModify.SubFigures.Add(clickedOutside);
                            UpdateGroupCenter(activeGroupToModify);

                            selectedFigure = activeGroupToModify;
                            SelectNodeByFigure(selectedFigure);
                            UpdateTreeView();
                        }
                    }
                }
                else if (e.Button == MouseButtons.Right)
                {
                    Figure clickedInside = activeGroupToModify.SubFigures.LastOrDefault(f => f.Contains(e.Location));
                    if (clickedInside != null)
                    {
                        selectedFigure = clickedInside;
                        SelectNodeByFigure(selectedFigure);

                        if (currentEditor != null && !currentEditor.IsDisposed) currentEditor.Close();
                        currentEditor = new EditorForm(clickedInside, this, canvasPanel) { Owner = this };
                        currentEditor.Show();
                    }
                    else
                    {
                        isModifyingGroupMode = false;
                        activeGroupToModify = null;
                        SelectNodeByFigure(null);
                    }
                }
                canvasPanel.Invalidate();
                return;
            }

            if (isDrawingMode)
            {
                if (e.Button == MouseButtons.Left) currentDrawPoints.Add(e.Location);
                else if (e.Button == MouseButtons.Right && currentDrawPoints.Count > 2)
                {
                    FinishCustomDrawing();
                    SelectNodeByFigure(selectedFigure);
                }
                canvasPanel.Invalidate();
                return;
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

                        selectedFigure = clicked;
                        SelectNodeByFigure(selectedFigure);
                    }
                }
                else if (e.Button == MouseButtons.Right)
                {
                    FinishGrouping();
                    SelectNodeByFigure(selectedFigure);
                }
                canvasPanel.Invalidate();
                return;
            }

            if (!isModifyingGroupMode && !isDrawingMode && !isGroupingMode)
            {
                // Сначала проверим, попали ли в маркер выделенной фигуры
                if (selectedFigure is Ellipse)
                {
                    var bounds = selectedFigure.GetBounds();
                    HandleType hit = HitTestHandles(e.Location, bounds);
                    if (hit != HandleType.None)
                    {
                        activeHandle = hit;
                        handleStartMouse = e.Location;
                        handleStartBounds = bounds;
                        handleStartRotation = selectedFigure.Rotation;
                        Ellipse ell = selectedFigure as Ellipse;
                        handleStartRadiusX = ell.RadiusX;
                        handleStartRadiusY = ell.RadiusY;
                        canvasPanel.Capture = true;
                        return;
                    }
                }

                // Обычное выделение/перетаскивание
                Figure hitFig = figures.LastOrDefault(f => f.Contains(e.Location));
                if (hitFig != null)
                {
                    selectedFigure = hitFig;
                    SelectNodeByFigure(selectedFigure);

                    if (e.Button == MouseButtons.Left)
                    {
                        isDragging = true;
                        lastMousePos = e.Location;
                        activeHandle = HandleType.Move; // помечаем, что перетаскиваем всю фигуру
                    }
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
                    HighlightedSideIndex = -1;
                    SelectNodeByFigure(null);
                }
                canvasPanel.Invalidate();
            }
        }

        private void CanvasPanel_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
            activeHandle = HandleType.None;
            canvasPanel.Capture = false;
        }

        private void CanvasPanel_MouseMove(object sender, MouseEventArgs e)
        {
            currentMouseHoverPos = e.Location;
            if (activeHandle != HandleType.None && activeHandle != HandleType.Move && selectedFigure is Ellipse ell)
            {
                // --- ЛОГИКА ДЛЯ ПЕРЕТАСКИВАНИЯ ФОКУСОВ ---
                if (activeHandle == HandleType.Focus1 || activeHandle == HandleType.Focus2)
                {
                    PointF currentF1 = ell.GetFocus1();
                    PointF currentF2 = ell.GetFocus2();

                    if (activeHandle == HandleType.Focus1)
                        ell.SetFoci(e.Location, currentF2);
                    else
                        ell.SetFoci(currentF1, e.Location);

                    canvasPanel.Invalidate();
                    return;
                }

                float scale = selectedFigure.Size / 100f;
                PointF localMouse = RotatePoint(e.Location, selectedFigure.Center, -selectedFigure.Rotation);
                PointF localStart = RotatePoint(handleStartMouse, selectedFigure.Center, -selectedFigure.Rotation);

                float dx = localMouse.X - localStart.X;
                float dy = localMouse.Y - localStart.Y;

                float startRx = Math.Abs(handleStartRadiusX) * scale;
                float startRy = Math.Abs(handleStartRadiusY) * scale;

                float newRx = startRx;
                float newRy = startRy;

                switch (activeHandle)
                {
                    case HandleType.ResizeE: newRx += dx; break;
                    case HandleType.ResizeW: newRx -= dx; break;
                    case HandleType.ResizeN: newRy -= dy; break;
                    case HandleType.ResizeS: newRy += dy; break;
                    case HandleType.ResizeNE: newRx += dx; newRy -= dy; break;
                    case HandleType.ResizeNW: newRx -= dx; newRy -= dy; break;
                    case HandleType.ResizeSE: newRx += dx; newRy += dy; break;
                    case HandleType.ResizeSW: newRx -= dx; newRy += dy; break;
                    case HandleType.Rotate:
                        PointF center = selectedFigure.Center;
                        float startAngle = (float)(Math.Atan2(handleStartMouse.Y - center.Y, handleStartMouse.X - center.X) * 180 / Math.PI);
                        float currentAngle = (float)(Math.Atan2(e.Y - center.Y, e.X - center.X) * 180 / Math.PI);
                        float deltaAngle = currentAngle - startAngle;
                        selectedFigure.Rotation = handleStartRotation + deltaAngle;
                        canvasPanel.Invalidate();
                        return;
                }

                float minRadius = 5;
                newRx = Math.Max(minRadius, newRx);
                newRy = Math.Max(minRadius, newRy);
                ell.RadiusX = newRx / scale;
                ell.RadiusY = newRy / scale;
                canvasPanel.Invalidate();
                return;
            }

            if (isDragging && selectedFigure != null && activeHandle == HandleType.Move)
            {
                selectedFigure.Move(e.X - lastMousePos.X, e.Y - lastMousePos.Y);
                lastMousePos = e.Location;
            }
            canvasPanel.Invalidate();
        }

        private PointF RotatePoint(PointF point, PointF center, float angleDegrees)
        {
            double rad = angleDegrees * Math.PI / 180.0;
            float cos = (float)Math.Cos(rad);
            float sin = (float)Math.Sin(rad);
            float dx = point.X - center.X;
            float dy = point.Y - center.Y;
            return new PointF(
                center.X + dx * cos - dy * sin,
                center.Y + dx * sin + dy * cos
            );
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
                var newFig = new Polygon(center, relPoints);

                newFig.Name = name;
                newFig.Size = 100;
                newFig.FillColor = Color.FromArgb(100, Color.White);

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
                UpdateTreeView();
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
                group.Name = name;

                foreach (var f in currentGroupSelection)
                {
                    group.SubFigures.Add(f);
                    figures.Remove(f);
                }

                customTemplates[name] = group;
                cbShapeType.Items.Add(name);
                figures.Add(group);
                selectedFigure = group;
                UpdateTreeView();
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
            else if (source is Polygon poly)
            {
                var vertices = poly.Sides.Select(s => s.RelativeOffset).ToArray();
                clone = new Polygon(newCenter, vertices);
            }
            else if (source is Ellipse ellipse)
            {
                clone = new Ellipse(newCenter, ellipse.RadiusX, ellipse.RadiusY);
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

        public void UpdateTreeView()
        {
            tvFigures.Nodes.Clear();
            foreach (var fig in figures)
            {
                AddFigureToNode(fig, tvFigures.Nodes);
            }
            tvFigures.ExpandAll();
        }

        private void AddFigureToNode(Figure fig, TreeNodeCollection nodes)
        {
            TreeNode node = new TreeNode(GetFigureReadableName(fig));
            node.Tag = fig;
            nodes.Add(node);

            if (fig is GroupFigure group)
            {
                foreach (var subFig in group.SubFigures)
                {
                    AddFigureToNode(subFig, node.Nodes);
                }
            }
        }

        private string GetFigureReadableName(Figure fig)
        {
            string displayName = !string.IsNullOrEmpty(fig.Name) ? fig.Name : GetDefaultTypeName(fig);
            string shortId = fig.Id.ToString().Substring(0, 8);
            return $"{displayName} [{shortId}]";
        }

        private string GetDefaultTypeName(Figure fig)
        {
            if (fig is GroupFigure) return "Группа";
            if (fig is Polygon) return "Многоугольник";
            if (fig is Ellipse) return "Эллипс";
            return fig.GetType().Name;
        }

        private void RemoveTemplateIfExist(string name)
        {
            string[] standardShapes = { "Прямоугольник", "Треугольник", "Круг", "Трапеция", "Пятиугольник" };

            if (!string.IsNullOrEmpty(name) && !standardShapes.Contains(name))
            {
                if (customTemplates.ContainsKey(name))
                {
                    customTemplates.Remove(name);
                    cbShapeType.Items.Remove(name);

                    if (cbShapeType.SelectedIndex == -1)
                        cbShapeType.SelectedIndex = 0;
                }
            }
        }

        private void TvFigures_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag is Figure fig)
            {
                selectedFigure = fig;
                HighlightedSideIndex = -1;
                canvasPanel.Invalidate();
            }
        }

        private void SelectNodeByFigure(Figure fig)
        {
            if (fig == null)
            {
                tvFigures.SelectedNode = null;
                return;
            }

            TreeNode node = FindNodeByTag(tvFigures.Nodes, fig);
            if (node != null)
            {
                tvFigures.SelectedNode = node;
                node.EnsureVisible();
            }
        }

        private TreeNode FindNodeByTag(TreeNodeCollection nodes, object tag)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Tag == tag) return node;
                TreeNode child = FindNodeByTag(node.Nodes, tag);
                if (child != null) return child;
            }
            return null;
        }

        private void TvFigures_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            if (e.Label != null && !string.IsNullOrWhiteSpace(e.Label))
            {
                if (e.Node.Tag is Figure fig)
                {
                    fig.Name = e.Label;
                    e.CancelEdit = true;
                    UpdateTreeView();
                }
            }
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
        private Button btnGroupShapes;
        private Button btnUngroup;
        private Button btnModifyGroup;
        private Button btnSaveSelected;
        private Button btnSave;
        private Button btnLoad;
        private Panel rightPanel;
        private TreeView tvFigures;
        private Label lblTree;

        private void InitializeComponent()
        {
            this.Text = "Графический редактор";
            this.Size = new Size(1300, 800);
            this.KeyPreview = true;
            this.Font = new Font("Segoe UI", 12F);

            toolbar = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(100, 160, 210), Padding = new Padding(10) };

            cbShapeType = new ComboBox { Location = new Point(15, 20), Size = new Size(200, 35), DropDownStyle = ComboBoxStyle.DropDownList };
            cbShapeType.Items.AddRange(new object[] { "Прямоугольник", "Треугольник", "Эллипс", "Трапеция", "Пятиугольник", "Новая фигура" });
            cbShapeType.SelectedIndex = 0;

            btnAddShape = new Button { Text = "Добавить", Location = new Point(230, 18), Size = new Size(130, 40), BackColor = Color.FromArgb(30, 90, 140), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnClearAll = new Button { Text = "Очистить", Location = new Point(380, 18), Size = new Size(130, 40), BackColor = Color.FromArgb(30, 90, 140), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnGroupShapes = new Button { Text = "Объединить", Location = new Point(530, 18), Size = new Size(140, 40), BackColor = Color.FromArgb(30, 90, 140), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnUngroup = new Button { Text = "Разделить", Location = new Point(680, 18), Size = new Size(130, 40), BackColor = Color.FromArgb(30, 90, 140), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnModifyGroup = new Button { Text = "Изменить", Location = new Point(820, 18), Size = new Size(130, 40), BackColor = Color.FromArgb(30, 90, 140), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnSaveSelected = new Button { Text = "Сохранить", Location = new Point(970, 18), Size = new Size(130, 40), BackColor = Color.FromArgb(60, 120, 170), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnSave = new Button { Text = "Сохранить все", Location = new Point(1110, 18), Size = new Size(160, 40), BackColor = Color.FromArgb(60, 120, 170), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnLoad = new Button { Text = "Загрузить", Location = new Point(1280, 18), Size = new Size(130, 40), BackColor = Color.FromArgb(60, 120, 170), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            btnSaveSelected.Click += BtnSaveSelected_Click;
            btnSave.Click += BtnSave_Click;

            toolbar.Controls.AddRange(new Control[] { cbShapeType, btnAddShape, btnClearAll, btnGroupShapes, btnUngroup, btnModifyGroup, btnSaveSelected, btnSave, btnLoad });

            rightPanel = new Panel { Dock = DockStyle.Right, Width = 250, BackColor = Color.FromArgb(100, 160, 210), Padding = new Padding(5) };
            lblTree = new Label { Text = "Список фигур", Dock = DockStyle.Top, Height = 30, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 12F, FontStyle.Bold) };
            tvFigures = new TreeView { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12F), HideSelection = false, CheckBoxes = true };

            tvFigures.AfterSelect += TvFigures_AfterSelect;
            tvFigures.LabelEdit = true;
            tvFigures.AfterLabelEdit += TvFigures_AfterLabelEdit;

            rightPanel.Controls.Add(tvFigures);
            rightPanel.Controls.Add(lblTree);

            canvasPanel = new DoubleBufferedPanel { Dock = DockStyle.Fill, BackColor = Color.WhiteSmoke };

            this.Controls.Add(canvasPanel);
            this.Controls.Add(rightPanel);
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
        public string Id { get; set; }
        public string Name { get; set; }
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
        public string Info { get; set; }
        public int ColorArgb { get; set; }
        public float Thickness { get; set; }
        public float RelOffsetX { get; set; }
        public float RelOffsetY { get; set; }
    }

}