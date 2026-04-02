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

        private TextBox txtBaseX, txtBaseY;
        private TextBox txtRelX, txtRelY;
        private TextBox txtCenterX, txtCenterY;
        private TextBox txtScale, txtThick, txtSideRelX, txtSideRelY;
        private Panel pnlFillColor, pnlSideColor;
        private ComboBox cbSides;

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

            // НОВОЕ: Когда форма закрывается, снимаем подсветку стороны
            this.FormClosed += (s, e) =>
            {
                mainForm.HighlightedSideIndex = -1;
                canvas.Invalidate();
            };
        }

        private void InitializeUI()
        {
            this.Text = "Свойства";
            this.Size = new Size(400, 1300);
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(Cursor.Position.X, Cursor.Position.Y);
            this.TopMost = true;
            this.Font = new Font("Segoe UI", 14F);

            System.Drawing.Rectangle workingArea = Screen.GetWorkingArea(this);
            this.Location = new Point(workingArea.Right - this.Width, workingArea.Top);

            int y = 10;
            int labelHeight = 40;
            int controlHeight = 35;

            AddLabel("Абс. координаты точки (X, Y):", ref y, labelHeight);
            txtBaseX = AddTextBox(ref y, controlHeight);
            txtBaseY = AddTextBox(ref y, controlHeight);

            AddLabel("Отн. координаты точки (X, Y):", ref y, labelHeight);
            txtRelX = AddTextBox(ref y, controlHeight);
            txtRelY = AddTextBox(ref y, controlHeight);

            AddLabel("Центр фигуры (X, Y):", ref y, labelHeight);
            txtCenterX = AddTextBox(ref y, controlHeight);
            txtCenterY = AddTextBox(ref y, controlHeight);

            AddLabel("Масштаб (1.0 = 100%):", ref y, labelHeight);
            txtScale = AddTextBox(ref y, controlHeight);

            AddLabel("Цвет фигуры:", ref y, labelHeight);
            pnlFillColor = new Panel { Location = new Point(10, y), Size = new Size(35, 30), BorderStyle = BorderStyle.FixedSingle };
            Button btnFill = new Button { Text = "Выбрать цвет заливки", Location = new Point(55, y), Size = new Size(315, 40) };
            btnFill.Click += (s, e) => PickColor(pnlFillColor);
            this.Controls.Add(pnlFillColor);
            this.Controls.Add(btnFill);
            y += 65;

            AddLabel("Сторона:", ref y, labelHeight);
            cbSides = new ComboBox { Location = new Point(10, y), Size = new Size(360, 35), DropDownStyle = ComboBoxStyle.DropDownList };

            // НОВОЕ: Передаем информацию в главную форму для перерисовки подсветки
            cbSides.SelectedIndexChanged += (s, e) =>
            {
                LoadSideData();
                mainForm.HighlightedSideIndex = cbSides.SelectedIndex; // Указываем индекс активной стороны
                canvas.Invalidate(); // Запускаем перерисовку холста
            };

            this.Controls.Add(cbSides);
            y += 45;

            AddLabel("Цвет:", ref y, labelHeight);
            pnlSideColor = new Panel { Location = new Point(10, y), Size = new Size(35, 30), BorderStyle = BorderStyle.FixedSingle };
            Button btnSide = new Button { Text = "Выбрать цвет стороны", Location = new Point(55, y), Size = new Size(315, 40) };
            btnSide.Click += (s, e) => PickColor(pnlSideColor);
            this.Controls.Add(pnlSideColor);
            this.Controls.Add(btnSide);
            y += 50;

            AddLabel("Толщина:", ref y, labelHeight);
            txtThick = AddTextBox(ref y, controlHeight);

            AddLabel("Отн. координаты вершины (X, Y):", ref y, labelHeight);
            txtSideRelX = AddTextBox(ref y, controlHeight);
            txtSideRelY = AddTextBox(ref y, controlHeight);
            y += 25;

            Button btnApply = new Button { Text = "Применить", Location = new Point(10, y), Size = new Size(360, 45), BackColor = Color.FromArgb(100, 160, 210), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 14F, FontStyle.Bold) };
            btnApply.Click += ApplyChanges;
            this.Controls.Add(btnApply);
            y += 55;

            Button btnDel = new Button { Text = "Удалить", Location = new Point(10, y), Size = new Size(360, 45), BackColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 14F, FontStyle.Bold) };
            btnDel.Click += (s, e) =>
            {
                mainForm.DeleteFigure(targetFigure);
                this.Close();
            };
            this.Controls.Add(btnDel);
        }

        private void AddLabel(string text, ref int y, int height)
        {
            Label lbl = new Label { Text = text, Location = new Point(10, y), AutoSize = true, Font = new Font("Segoe UI", 14F) };
            this.Controls.Add(lbl);
            y += height + 5;
        }

        private TextBox AddTextBox(ref int y, int height)
        {
            TextBox tb = new TextBox { Location = new Point(10, y), Size = new Size(360, height), Font = new Font("Segoe UI", 14F) };
            this.Controls.Add(tb);
            y += height + 8;
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

            int selectedIndex = cbSides.SelectedIndex;

            cbSides.Items.Clear();
            for (int i = 0; i < targetFigure.Sides.Count; i++)
                cbSides.Items.Add($"Сторона {i + 1}");

            if (cbSides.Items.Count > 0)
            {
                if (selectedIndex >= 0 && selectedIndex < cbSides.Items.Count)
                    cbSides.SelectedIndex = selectedIndex;
                else
                    cbSides.SelectedIndex = 0; // Это действие автоматически запустит SelectedIndexChanged и включит подсветку!
            }
        }

        private void LoadSideData()
        {
            if (cbSides.SelectedIndex < 0) return;
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

                if (cbSides.SelectedIndex >= 0)
                {
                    var side = targetFigure.Sides[cbSides.SelectedIndex];
                    side.Color = pnlSideColor.BackColor;
                    side.Thickness = float.Parse(txtThick.Text);
                    side.RelativeOffset = new PointF(float.Parse(txtSideRelX.Text), float.Parse(txtSideRelY.Text));
                }

                LoadData();
                canvas.Invalidate();

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