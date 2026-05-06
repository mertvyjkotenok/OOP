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

        private TextBox txtBaseX, txtBaseY;
        private TextBox txtRelX, txtRelY;
        private TextBox txtCenterX, txtCenterY;
        private TextBox txtScale, txtThick, txtSideRelX, txtSideRelY;
        private TextBox txtRadiusX, txtRadiusY;

        // Поля для фокусов
        private TextBox txtFocus1X, txtFocus1Y;
        private TextBox txtFocus2X, txtFocus2Y;

        private Panel pnlFillColor, pnlSideColor;
        private ComboBox cbSides;

        public EditorForm(Figure figure, Form1 main, Panel canvasPanel)
        {
            this.targetFigure = figure;
            this.mainForm = main;
            this.canvas = canvasPanel;

            InitializeUI();
            LoadData();

            this.FormClosed += (s, e) =>
            {
                mainForm.HighlightedSideIndex = -1;
                canvas.Invalidate();
            };
        }

        private void InitializeUI()
        {
            // Включаем прокрутку для формы, так как элементов стало больше
            this.AutoScroll = true;
            bool isEllipse = targetFigure is Ellipse;

            int totalElements = isEllipse ? 29 : 24;
            int step = (Screen.PrimaryScreen.Bounds.Height - 50) / totalElements;
            if (step < 45) step = 45;

            this.Text = "Свойства";
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.StartPosition = FormStartPosition.Manual;
            this.TopMost = true;
            this.Font = new Font("Segoe UI", 14F);

            int captionHeight = SystemInformation.CaptionHeight;
            this.Size = new Size(430, Screen.PrimaryScreen.Bounds.Height + captionHeight);
            this.Location = new Point(Screen.PrimaryScreen.Bounds.Right - this.Width + 10, Screen.PrimaryScreen.Bounds.Top);

            int y = 9;

            AddLabel("Абс. координаты точки (X, Y):", ref y, step);
            txtBaseX = AddTextBox(ref y, step);
            txtBaseY = AddTextBox(ref y, step);

            AddLabel("Отн. координаты точки (X, Y):", ref y, step);
            txtRelX = AddTextBox(ref y, step);
            txtRelY = AddTextBox(ref y, step);

            AddLabel("Центр фигуры (X, Y):", ref y, step);
            txtCenterX = AddTextBox(ref y, step);
            txtCenterY = AddTextBox(ref y, step);

            AddLabel("Масштаб (1.0 = 100%):", ref y, step);
            txtScale = AddTextBox(ref y, step);

            AddLabel("Цвет фигуры:", ref y, step);
            pnlFillColor = new Panel { Location = new Point(10, y), Size = new Size(35, 35), BorderStyle = BorderStyle.FixedSingle };
            Button btnFill = new Button { Text = "Выбрать цвет заливки", Location = new Point(55, y), Size = new Size(315, 40) };
            btnFill.Click += (s, e) => PickColor(pnlFillColor);
            this.Controls.Add(pnlFillColor);
            this.Controls.Add(btnFill);
            y += step;

            if (!isEllipse)
            {
                AddLabel("Сторона:", ref y, step);
                cbSides = new ComboBox { Location = new Point(10, y), Size = new Size(360, 35), DropDownStyle = ComboBoxStyle.DropDownList };
                cbSides.SelectedIndexChanged += (s, e) =>
                {
                    LoadSideData();
                    mainForm.HighlightedSideIndex = cbSides.SelectedIndex;
                    canvas.Invalidate();
                };
                this.Controls.Add(cbSides);
                y += step;

                AddLabel("Цвет:", ref y, step);
            }
            else
            {
                AddLabel("Цвет обводки:", ref y, step - 5);
            }

            pnlSideColor = new Panel { Location = new Point(10, y), Size = new Size(35, 35), BorderStyle = BorderStyle.FixedSingle };
            Button btnSide = new Button { Text = "Выбрать цвет", Location = new Point(55, y), Size = new Size(315, 40) };
            btnSide.Click += (s, e) => PickColor(pnlSideColor);
            this.Controls.Add(pnlSideColor);
            this.Controls.Add(btnSide);
            y += step;

            AddLabel("Толщина:", ref y, step);
            txtThick = AddTextBox(ref y, step);

            if (!isEllipse)
            {
                AddLabel("Отн. координаты вершины (X, Y):", ref y, step);
                txtSideRelX = AddTextBox(ref y, step);
                txtSideRelY = AddTextBox(ref y, step);
            }
            else
            {
                AddLabel("Радиус X:", ref y, step - 5);
                txtRadiusX = AddTextBox(ref y, step - 5);

                AddLabel("Радиус Y:", ref y, step);
                txtRadiusY = AddTextBox(ref y, step - 5);

                // --- Добавляем текстовые поля для фокусов ---
                AddLabel("Абс. Фокус 1 (X, Y):", ref y, step - 5);
                txtFocus1X = AddTextBox(ref y, step - 5);
                txtFocus1Y = AddTextBox(ref y, step - 5);

                AddLabel("Абс. Фокус 2 (X, Y):", ref y, step - 5);
                txtFocus2X = AddTextBox(ref y, step - 5);
                txtFocus2Y = AddTextBox(ref y, step - 5);
            }

            y += 10;

            Button btnApply = new Button { Text = "Применить", Location = new Point(10, y), Size = new Size(360, 45), BackColor = Color.FromArgb(100, 160, 210), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 14F, FontStyle.Bold) };
            btnApply.Click += ApplyChanges;
            this.Controls.Add(btnApply);
            y += Math.Max(55, step);

            Button btnDel = new Button { Text = "Удалить", Location = new Point(10, y), Size = new Size(360, 45), BackColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 14F, FontStyle.Bold) };
            btnDel.Click += (s, e) =>
            {
                mainForm.DeleteFigure(targetFigure);
                this.Close();
            };
            this.Controls.Add(btnDel);
        }

        private void AddLabel(string text, ref int y, int step)
        {
            Label lbl = new Label { Text = text, Location = new Point(10, y), AutoSize = true };
            this.Controls.Add(lbl);
            y += step;
        }

        private TextBox AddTextBox(ref int y, int step)
        {
            TextBox tb = new TextBox { Location = new Point(10, y), Size = new Size(360, 32), Font = new Font("Segoe UI", 14F) };
            this.Controls.Add(tb);
            y += step;
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
            txtBaseX.Text = targetFigure.BaseLocation.X.ToString();
            txtBaseY.Text = targetFigure.BaseLocation.Y.ToString();
            txtRelX.Text = targetFigure.RelativePivot.X.ToString();
            txtRelY.Text = targetFigure.RelativePivot.Y.ToString();

            Point center = targetFigure.Center;
            txtCenterX.Text = center.X.ToString();
            txtCenterY.Text = center.Y.ToString();

            txtScale.Text = (targetFigure.Size / 100f).ToString();
            pnlFillColor.BackColor = targetFigure.FillColor;

            if (targetFigure is Ellipse ellipse)
            {
                txtRadiusX.Text = ellipse.RadiusX.ToString();
                txtRadiusY.Text = ellipse.RadiusY.ToString();

                // Читаем фокусы в текстовые поля
                PointF f1 = ellipse.GetFocus1();
                PointF f2 = ellipse.GetFocus2();
                txtFocus1X.Text = Math.Round(f1.X, 1).ToString();
                txtFocus1Y.Text = Math.Round(f1.Y, 1).ToString();
                txtFocus2X.Text = Math.Round(f2.X, 1).ToString();
                txtFocus2Y.Text = Math.Round(f2.Y, 1).ToString();

                if (targetFigure.Sides.Count > 0)
                {
                    var side = targetFigure.Sides[0];
                    pnlSideColor.BackColor = side.Color;
                    txtThick.Text = side.Thickness.ToString();
                }
            }
            else
            {
                int selectedIndex = cbSides.SelectedIndex;
                cbSides.Items.Clear();
                for (int i = 0; i < targetFigure.Sides.Count; i++)
                    cbSides.Items.Add($"Сторона {i + 1}");

                if (cbSides.Items.Count > 0)
                {
                    if (selectedIndex >= 0 && selectedIndex < cbSides.Items.Count)
                        cbSides.SelectedIndex = selectedIndex;
                    else
                        cbSides.SelectedIndex = 0;
                }
            }
        }

        private void LoadSideData()
        {
            if (targetFigure is Ellipse || cbSides.SelectedIndex < 0) return;
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
                Point oldBase = targetFigure.BaseLocation;
                PointF oldRel = targetFigure.RelativePivot;
                Point oldCenter = targetFigure.Center;

                Point newBase = new Point(int.Parse(txtBaseX.Text), int.Parse(txtBaseY.Text));
                PointF newRel = new PointF(float.Parse(txtRelX.Text), float.Parse(txtRelY.Text));
                Point newCenter = new Point(int.Parse(txtCenterX.Text), int.Parse(txtCenterY.Text));

                bool baseChanged = newBase != oldBase;
                bool relChanged = newRel != oldRel;
                bool centerChanged = newCenter != oldCenter;

                int changedCount = (baseChanged ? 1 : 0) + (relChanged ? 1 : 0) + (centerChanged ? 1 : 0);

                if (changedCount > 0)
                {
                    if (changedCount == 1)
                    {
                        if (baseChanged) newRel = new PointF(newBase.X - oldCenter.X, newBase.Y - oldCenter.Y);
                        else if (relChanged) newBase = new Point(oldCenter.X + (int)newRel.X, oldCenter.Y + (int)newRel.Y);
                        else if (centerChanged) newBase = new Point(newCenter.X + (int)oldRel.X, newCenter.Y + (int)oldRel.Y);
                    }
                    else if (changedCount == 2)
                    {
                        if (baseChanged && relChanged) newCenter = new Point(newBase.X - (int)newRel.X, newBase.Y - (int)newRel.Y);
                        else if (baseChanged && centerChanged) newRel = new PointF(newBase.X - newCenter.X, newBase.Y - newCenter.Y);
                        else newBase = new Point(newCenter.X + (int)newRel.X, newCenter.Y + (int)newRel.Y);
                    }
                    else if (changedCount == 3)
                    {
                        Point computedCenter = new Point(newBase.X - (int)newRel.X, newBase.Y - (int)newRel.Y);
                        if (Math.Abs(computedCenter.X - newCenter.X) > 1 || Math.Abs(computedCenter.Y - newCenter.Y) > 1)
                        {
                            MessageBox.Show("Введённые значения не согласованы");
                            return;
                        }
                    }
                    targetFigure.BaseLocation = newBase;
                    targetFigure.RelativePivot = newRel;
                }

                targetFigure.Size = (int)(float.Parse(txtScale.Text) * 100);
                targetFigure.FillColor = pnlFillColor.BackColor;

                if (targetFigure is Ellipse ellipse)
                {
                    PointF oldF1 = ellipse.GetFocus1();
                    PointF oldF2 = ellipse.GetFocus2();

                    // Сначала устанавливаем радиус (если пользователь изменил только его)
                    ellipse.RadiusX = float.Parse(txtRadiusX.Text);
                    ellipse.RadiusY = float.Parse(txtRadiusY.Text);

                    PointF newF1 = new PointF(float.Parse(txtFocus1X.Text), float.Parse(txtFocus1Y.Text));
                    PointF newF2 = new PointF(float.Parse(txtFocus2X.Text), float.Parse(txtFocus2Y.Text));

                    // Если координаты фокусов в полях изменились, то применяем фокусы и пересчитываем радиус
                    bool fociChanged = Math.Abs(oldF1.X - newF1.X) > 0.5f || Math.Abs(oldF1.Y - newF1.Y) > 0.5f ||
                                       Math.Abs(oldF2.X - newF2.X) > 0.5f || Math.Abs(oldF2.Y - newF2.Y) > 0.5f;

                    if (fociChanged)
                    {
                        ellipse.SetFoci(newF1, newF2);
                    }

                    if (ellipse.Sides.Count > 0)
                    {
                        var side = ellipse.Sides[0];
                        side.Color = pnlSideColor.BackColor;
                        side.Thickness = float.Parse(txtThick.Text);
                    }
                }
                else
                {
                    if (cbSides.SelectedIndex >= 0)
                    {
                        var side = targetFigure.Sides[cbSides.SelectedIndex];
                        side.Color = pnlSideColor.BackColor;
                        side.Thickness = float.Parse(txtThick.Text);
                        side.RelativeOffset = new PointF(float.Parse(txtSideRelX.Text), float.Parse(txtSideRelY.Text));
                    }
                }

                LoadData();
                canvas.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка ввода данных: " + ex.Message);
            }
        }
    }
}