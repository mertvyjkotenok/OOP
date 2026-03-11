using Lab1.Figures;
using Lab1.Shapes;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Lab1
{
    public partial class EditorForm : Form
    {
        private Figure targetFigure;
        private Form1 mainForm;
        private Panel canvas;

        //private Label lblBounds;
        private TextBox txtBaseX, txtBaseY;      // переименовано
        private TextBox txtRelX, txtRelY;
        private TextBox txtCenterX, txtCenterY;
        private TextBox txtScale, txtThick, txtSideRelX, txtSideRelY;
        private Panel pnlFillColor, pnlSideColor;
        private ComboBox cbSides;

        // Для хранения исходных значений
        private Point originalBaseLocation;
        private PointF originalRelativePivot;
        private Point originalCenter;

        public EditorForm(Figure figure, Form1 main, Panel canvasPanel)
        {
            this.targetFigure = figure;
            this.mainForm = main;
            this.canvas = canvasPanel;

            InitializeUI();
            LoadData();

            originalBaseLocation = targetFigure.BaseLocation;
            originalRelativePivot = targetFigure.RelativePivot;
            originalCenter = targetFigure.Center;
        }

        private void InitializeUI()
        {
            this.Text = "Свойства";
            this.Size = new Size(400, 1300);          // шире и выше, чтобы всё поместилось
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(Cursor.Position.X, Cursor.Position.Y);
            this.TopMost = true;
            this.Font = new Font("Segoe UI", 14F);   // основной шрифт формы

            System.Drawing.Rectangle workingArea = Screen.GetWorkingArea(this);
            this.Location = new Point(workingArea.Right - this.Width, workingArea.Top);

            int y = 10;
            int labelHeight = 40;      // примерная высота строки с учётом шрифта
            int controlHeight = 35;    // высота полей ввода, кнопок
            int spacing = 25;          // доп. отступ между элементами

            // --- БАЗОВАЯ ТОЧКА (переименовано) ---
            AddLabel("Абс. координаты точки (X, Y):", ref y, labelHeight);
            txtBaseX = AddTextBox(ref y, controlHeight);
            txtBaseY = AddTextBox(ref y, controlHeight);

            // --- СДВИГ ЦЕНТРА (без изменений) ---
            AddLabel("Отн. координаты точки (X, Y):", ref y, labelHeight);
            txtRelX = AddTextBox(ref y, controlHeight);
            txtRelY = AddTextBox(ref y, controlHeight);

            AddLabel("Центр фигуры (X, Y):", ref y, labelHeight);
            txtCenterX = AddTextBox(ref y, controlHeight);
            txtCenterY = AddTextBox(ref y, controlHeight);
           

            // Масштаб
            AddLabel("Масштаб (1.0 = 100%):", ref y, labelHeight);
            txtScale = AddTextBox(ref y, controlHeight);

            // Цвет заливки
            AddLabel("Цвет фигуры:", ref y, labelHeight);
            pnlFillColor = new Panel
            {
                Location = new Point(10, y),
                Size = new Size(35, 30),
                BorderStyle = BorderStyle.FixedSingle
            };
            Button btnFill = new Button
            {
                Text = "Выбрать цвет заливки",
                Location = new Point(55, y),
                Size = new Size(315, 40)
            };
            btnFill.Click += (s, e) => PickColor(pnlFillColor);
            this.Controls.Add(pnlFillColor);
            this.Controls.Add(btnFill);
            y += 65; // высота кнопки + отступ

            // Выбор стороны
            AddLabel("Сторона:", ref y, labelHeight);
            cbSides = new ComboBox
            {
                Location = new Point(10, y),
                Size = new Size(360, 35),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cbSides.SelectedIndexChanged += (s, e) => LoadSideData();
            this.Controls.Add(cbSides);
            y += 45;

            // Цвет стороны
            AddLabel("Цвет:", ref y, labelHeight);
            pnlSideColor = new Panel
            {
                Location = new Point(10, y),
                Size = new Size(35, 30),
                BorderStyle = BorderStyle.FixedSingle
            };
            Button btnSide = new Button
            {
                Text = "Выбрать цвет стороны",
                Location = new Point(55, y),
                Size = new Size(315, 40)
            };
            btnSide.Click += (s, e) => PickColor(pnlSideColor);
            this.Controls.Add(pnlSideColor);
            this.Controls.Add(btnSide);
            y += 50;

            // Толщина стороны
            AddLabel("Толщина:", ref y, labelHeight);
            txtThick = AddTextBox(ref y, controlHeight);

            // Смещение стороны (RelX, RelY)
            AddLabel("Отн. координаты вершины (X, Y):", ref y, labelHeight);
            txtSideRelX = AddTextBox(ref y, controlHeight);
            txtSideRelY = AddTextBox(ref y, controlHeight);
            y += 25;
            // Кнопка "Применить"
            Button btnApply = new Button
            {
                Text = "Применить",
                Location = new Point(10, y),
                Size = new Size(360, 45),
                BackColor = Color.FromArgb(100, 160, 210),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(90, 150, 200) },
                Font = new Font("Segoe UI", 14F, FontStyle.Bold)
            };
            btnApply.Click += ApplyChanges;
            this.Controls.Add(btnApply);
            y += 55;

            // Кнопка "Удалить фигуру"
            Button btnDel = new Button
            {
                Text = "Удалить",
                Location = new Point(10, y),
                Size = new Size(360, 45),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(90, 150, 200) },
                Font = new Font("Segoe UI", 14F, FontStyle.Bold)
            };
            btnDel.Click += (s, e) =>
            {
                mainForm.figures.Remove(targetFigure);
                mainForm.selectedFigure = null;
                canvas.Invalidate();
                this.Close();
            };
            this.Controls.Add(btnDel);
        }

        // Вспомогательные методы с параметрами высоты
        private void AddLabel(string text, ref int y, int height)
        {
            Label lbl = new Label
            {
                Text = text,
                Location = new Point(10, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 14F)
            };
            this.Controls.Add(lbl);
            y += height + 5; // небольшой отступ после метки
        }

        private TextBox AddTextBox(ref int y, int height)
        {
            TextBox tb = new TextBox
            {
                Location = new Point(10, y),
                Size = new Size(360, height),
                Font = new Font("Segoe UI", 14F)
            };
            this.Controls.Add(tb);
            y += height + 8; // отступ после поля
            return tb;
        }

        private void PickColor(Panel p)
        {
            using (ColorDialog cd = new ColorDialog())
            {
                cd.Color = p.BackColor;
                if (cd.ShowDialog() == DialogResult.OK) p.BackColor = cd.Color;
            }
        }

        private void LoadData()
        {
            RectangleF b = targetFigure.GetBounds();
            
            // Базовая точка
            txtBaseX.Text = targetFigure.BaseLocation.X.ToString();
            txtBaseY.Text = targetFigure.BaseLocation.Y.ToString();

            // Сдвиг
            txtRelX.Text = targetFigure.RelativePivot.X.ToString();
            txtRelY.Text = targetFigure.RelativePivot.Y.ToString();

            Point center = targetFigure.Center;
            txtCenterX.Text = center.X.ToString();
            txtCenterY.Text = center.Y.ToString();

            txtScale.Text = (targetFigure.Size / 100f).ToString();
            pnlFillColor.BackColor = targetFigure.FillColor;

            // Сохраняем выбранный индекс
            int selectedIndex = cbSides.SelectedIndex;

            cbSides.Items.Clear();
            for (int i = 0; i < targetFigure.Sides.Count; i++)
                cbSides.Items.Add($"Сторона {i + 1}");

            if (cbSides.Items.Count > 0)
            {
                // Восстанавливаем индекс, если он был допустимым
                if (selectedIndex >= 0 && selectedIndex < cbSides.Items.Count)
                    cbSides.SelectedIndex = selectedIndex;
                else
                    cbSides.SelectedIndex = 0;
            }
        }

        private void LoadSideData()
        {
            var side = targetFigure.Sides[cbSides.SelectedIndex];
            pnlSideColor.BackColor = side.Color;
            txtThick.Text = side.Thickness.ToString();
            txtSideRelX.Text = side.RelativeOffset.X.ToString();
            txtSideRelY.Text = side.RelativeOffset.Y.ToString();
        }

        private void ApplyChanges(object sender, EventArgs e)
        {
            try
            {
                // Текущие значения фигуры до изменений
                Point oldBase = targetFigure.BaseLocation;
                PointF oldRel = targetFigure.RelativePivot;
                Point oldCenter = targetFigure.Center;

                // Считываем новые значения из UI
                Point newBase = new Point(int.Parse(txtBaseX.Text), int.Parse(txtBaseY.Text));
                PointF newRel = new PointF(float.Parse(txtRelX.Text), float.Parse(txtRelY.Text));
                Point newCenter = new Point(int.Parse(txtCenterX.Text), int.Parse(txtCenterY.Text));

                // Определяем, что изменилось
                bool baseChanged = newBase != oldBase;
                bool relChanged = newRel != oldRel;
                bool centerChanged = newCenter != oldCenter;

                // Счётчик изменённых полей
                int changedCount = (baseChanged ? 1 : 0) + (relChanged ? 1 : 0) + (centerChanged ? 1 : 0);

                // === КООРДИНАТЫ ===
                if (changedCount > 0)
                {
                    if (changedCount == 1)
                    {
                        if (baseChanged)
                        {
                            // Пользователь двигает точку → центр остаётся
                            newRel = new PointF(
                                newBase.X - oldCenter.X,
                                newBase.Y - oldCenter.Y
                            );
                        }
                        else if (relChanged)
                        {
                            // Пользователь двигает относительную точку → фигура НЕ двигается
                            newBase = new Point(
                                oldCenter.X + (int)newRel.X,
                                oldCenter.Y + (int)newRel.Y
                            );
                        }
                        else if (centerChanged)
                        {
                            // Пользователь двигает фигуру целиком
                            newBase = new Point(
                                newCenter.X + (int)oldRel.X,
                                newCenter.Y + (int)oldRel.Y
                            );
                        }
                    }
                    else if (changedCount == 2)
                    {
                        if (baseChanged && relChanged)
                            newCenter = new Point(newBase.X - (int)newRel.X, newBase.Y - (int)newRel.Y);
                        else if (baseChanged && centerChanged)
                            newRel = new PointF(newBase.X - newCenter.X, newBase.Y - newCenter.Y);
                        else
                            newBase = new Point(newCenter.X + (int)newRel.X, newCenter.Y + (int)newRel.Y);
                    }
                    else if (changedCount == 3)
                    {
                        Point computedCenter = new Point(newBase.X - (int)newRel.X, newBase.Y - (int)newRel.Y);
                        if (Math.Abs(computedCenter.X - newCenter.X) > 1 ||
                            Math.Abs(computedCenter.Y - newCenter.Y) > 1)
                        {
                            MessageBox.Show("Введённые значения не согласованы");
                            return;
                        }
                    }

                    targetFigure.BaseLocation = newBase;
                    targetFigure.RelativePivot = newRel;
                }

                // === ВСЕ ОСТАЛЬНЫЕ СВОЙСТВА (ВСЕГДА!) ===
                targetFigure.Size = (int)(float.Parse(txtScale.Text) * 100);
                targetFigure.FillColor = pnlFillColor.BackColor;

                if (cbSides.SelectedIndex >= 0)
                {
                    var side = targetFigure.Sides[cbSides.SelectedIndex];
                    side.Color = pnlSideColor.BackColor;
                    side.Thickness = float.Parse(txtThick.Text);
                    float x = float.Parse(txtSideRelX.Text);
                    float y = float.Parse(txtSideRelY.Text);
                    side.RelativeOffset = new PointF(x, y);
                }

                // Применяем новые значения к фигуре
                targetFigure.BaseLocation = newBase;
                targetFigure.RelativePivot = newRel;
                // Центр установится автоматически через свойство, но мы можем убедиться:
                // (можно оставить так, свойство Center пересчитается при обращении)

                // Обновляем остальные параметры (размер, цвет, стороны)...
                targetFigure.Size = (int)(float.Parse(txtScale.Text) * 100);
                targetFigure.FillColor = pnlFillColor.BackColor;

                if (cbSides.SelectedIndex >= 0)
                {
                    var side = targetFigure.Sides[cbSides.SelectedIndex];
                    side.Color = pnlSideColor.BackColor;
                    side.Thickness = float.Parse(txtThick.Text);
                    side.RelativeOffset = new PointF(float.Parse(txtSideRelX.Text), float.Parse(txtSideRelY.Text));
                }

                // Обновляем отображение данных в форме (включая пересчитанные поля)
                LoadData();

                // Перерисовываем холст
                canvas.Invalidate();

                // Сохраняем новые исходные значения для следующего применения
                originalBaseLocation = targetFigure.BaseLocation;
                originalRelativePivot = targetFigure.RelativePivot;
                originalCenter = targetFigure.Center;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка ввода данных: " + ex.Message);
            }
        }
    }
}