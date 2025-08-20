using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace WorkSheetApplication.Database
{
    public partial class FormulaEditForm : Form
    {
        private FlowLayoutPanel controlElementsPanel;
        private List<(TextBox coefficient, ComboBox element)> elementControls;
        private readonly string[] defaultElements = new[]
        {
        "ЛР1", "ЛР2", "ЛР3",
        "МКР1", "МКР2", "МКР3",
        "КР1", "КР2",
        "ЭКЗ",
        "другое..."
    };
        private int disciplineId;

        public FormulaEditForm(int disciplineId)
        {
            this.disciplineId = disciplineId;
            InitializeComponent();
            elementControls = new List<(TextBox, ComboBox)>();
            InitializeFormulaEditForm();
            LoadExistingFormula();
        }

        private void InitializeFormulaEditForm()
        {
            // Настройка формы
            this.Text = "Редактирование формулы";
            this.Size = new Size(400, 335);
            this.StartPosition = FormStartPosition.CenterScreen;
            //this.BackColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Заголовок
            Label lblTitle = new Label
            {
                Text = "Редактирование формулы",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                BackColor = Color.Transparent,
                Location = new Point(this.ClientSize.Width / 2 - 130, 20),
                AutoSize = true
            };

            // Панель добавления элемента с текстом и кнопкой
            Panel addElementPanel = new Panel
            {
                Location = new Point(20, 60),
                Size = new Size(250, 30),
                BackColor = Color.Transparent
            };

            Label lblAddElement = new Label
            {
                Text = "Добавить элемент контроля",
                Location = new Point(0, 5),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                BackColor = Color.Transparent
            };

      

            PictureBox btnAdd = new PictureBox
            {
                Size = new Size(25, 25),
                Location = new Point(200, 2),
                Cursor = Cursors.Hand,
                SizeMode = PictureBoxSizeMode.Zoom,  // Добавляем это свойство
                Image = WorkSheetApplication.Properties.Resources.plus_icon  // Указываем полное имя
            };
            btnAdd.Click += BtnAdd_Click;

            addElementPanel.Controls.AddRange(new Control[] { lblAddElement, btnAdd });

            // Панель для элементов контроля
            controlElementsPanel = new FlowLayoutPanel
            {
                Location = new Point(20, 100),
                Size = new Size(340, 120),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent
            };
            Button btnDone = new Button
            {
                Text = "Готово",
                //Size = new Size(80, 30),
                Location = new Point(150, 240),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 40),

                BackColor = Color.FromArgb(0, 123, 255), // Синий цвет
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 12),
                Cursor = Cursors.Hand
            };
            btnDone.FlatAppearance.BorderSize = 0;
            btnDone.Region = CreateRoundedRegion(btnDone.ClientRectangle, 10);
            btnDone.Click += BtnSave_Click;

            

            // Добавляем первый элемент контроля
            AddControlElement();

            // Добавляем все элементы на форму
            this.Controls.AddRange(new Control[]
            {
            lblTitle,
            addElementPanel,
            controlElementsPanel,
            btnDone
            });
        }
        private Region CreateRoundedRegion(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();
            return new Region(path);
        }
        private async void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                // Проверяем, что все поля заполнены и преобразуем значения
                List<(decimal coefficient, string element)> validatedElements = new List<(decimal, string)>();

                foreach (var (coefficientBox, elementCombo) in elementControls)
                {
                    if (string.IsNullOrWhiteSpace(coefficientBox.Text) || elementCombo.SelectedIndex == -1)
                    {
                        MessageBox.Show("Пожалуйста, заполните все поля!", "Предупреждение",
                                      MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Преобразуем строку в decimal с учетом культуры
                    if (!decimal.TryParse(coefficientBox.Text.Replace(',', '.'),
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out decimal coefficientValue))
                    {
                        MessageBox.Show($"Неверный формат числа: {coefficientBox.Text}", "Ошибка",
                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    validatedElements.Add((coefficientValue, elementCombo.Text));
                }

                // Проверяем сумму коэффициентов
                decimal sum = validatedElements.Sum(x => x.coefficient);
                if (Math.Abs(sum - 1m) > 0.001m)
                {
                    MessageBox.Show($"Сумма всех коэффициентов должна быть равна 1! Текущая сумма: {sum}",
                                  "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (var conn = DatabaseConnection.GetConnection())
                {
                    await conn.OpenAsync();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // Сначала удаляем старые элементы контроля для этой дисциплины
                            string deleteOldSql = "DELETE FROM public.control_element WHERE id_discipline = @disciplineId";
                            using (var cmd = new NpgsqlCommand(deleteOldSql, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("disciplineId", disciplineId);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            // Получаем следующий доступный id_element
                            string getMaxIdSql = "SELECT COALESCE(MAX(id_element), 0) FROM public.control_element";
                            int nextId;
                            using (var cmd = new NpgsqlCommand(getMaxIdSql, conn, transaction))
                            {
                                nextId = Convert.ToInt32(await cmd.ExecuteScalarAsync()) + 1;
                            }

                            
                            foreach (var (coefficient, element) in validatedElements)
                            {
                                string insertElementSql = @"
                            INSERT INTO public.control_element (id_element, element, weight, id_discipline)
                            VALUES (@id, @element, @weight, @disciplineId)";

                                using (var cmd = new NpgsqlCommand(insertElementSql, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("id", nextId);
                                    cmd.Parameters.AddWithValue("element", element);
                                    cmd.Parameters.AddWithValue("weight", coefficient);
                                    cmd.Parameters.AddWithValue("disciplineId", disciplineId);
                                    await cmd.ExecuteNonQueryAsync();
                                    nextId++;
                                }
                            }

                            transaction.Commit();
                            MessageBox.Show("Формула успешно сохранена!", "Успех",
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);
                            this.DialogResult = DialogResult.OK;
                            this.Close();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private async void LoadExistingFormula()
        {
            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    await conn.OpenAsync();
                    string sql = @"
                SELECT element, weight 
                FROM public.control_element 
                WHERE id_discipline = @disciplineId 
                ORDER BY id_element";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("disciplineId", disciplineId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            controlElementsPanel.Controls.Clear();
                            elementControls.Clear();

                            bool hasElements = false;
                            while (await reader.ReadAsync())
                            {
                                hasElements = true;
                                string element = reader.GetString(0);
                                decimal weight = reader.GetDecimal(1);
                                AddControlElement(element, weight);
                            }

                            if (!hasElements)
                            {
                                AddControlElement(); // Добавляем пустой элемент, если формула еще не создана
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке формулы: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void AddControlElement(string selectedElement = null, decimal? weight = null)
        {
            Panel elementPanel = new Panel
            {
                Size = new Size(320, 35),
                Margin = new Padding(0, 0, 0, 10),
                BackColor = Color.White
            };

            TextBox txtCoefficient = new TextBox
            {
                Size = new Size(60, 25),
                Location = new Point(0, 7),
                BorderStyle = BorderStyle.FixedSingle,
                Text = weight?.ToString(CultureInfo.InvariantCulture) ?? ""
            };
            txtCoefficient.KeyPress += ValidateNumericInput;
            txtCoefficient.Leave += ValidateCoefficient;

            ComboBox cmbElement = new ComboBox
            {
                Size = new Size(200, 25),
                Location = new Point(70, 7),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            cmbElement.Items.AddRange(defaultElements);
            if (selectedElement != null)
            {
                if (!cmbElement.Items.Contains(selectedElement))
                {
                    cmbElement.Items.Insert(cmbElement.Items.Count - 1, selectedElement);
                }
                cmbElement.SelectedItem = selectedElement;
            }
            cmbElement.SelectedIndexChanged += CmbElement_SelectedIndexChanged;

            // Добавляем кнопку удаления
            //Button btnDelete = new Button
            //{
            //    Text = "✖",
            //    Size = new Size(25, 25),
            //    Location = new Point(280, 5),
            //    FlatStyle = FlatStyle.Flat,
            //    Cursor = Cursors.Hand,
            //    ForeColor = Color.Red
            //};
            PictureBox btnDelete = new PictureBox
            {
                Size = new Size(20, 20),
                Location = new Point(283, 7),
                Cursor = Cursors.Hand,
                SizeMode = PictureBoxSizeMode.Zoom,  // Добавляем это свойство
                Image = WorkSheetApplication.Properties.Resources.btnDelete_icon  // Указываем полное имя
            };
            btnDelete.Click += (s, e) =>
            {
                elementControls.Remove((txtCoefficient, cmbElement));
                controlElementsPanel.Controls.Remove(elementPanel);
            };

            elementPanel.Controls.AddRange(new Control[] { txtCoefficient, cmbElement, btnDelete });
            controlElementsPanel.Controls.Add(elementPanel);
            elementControls.Add((txtCoefficient, cmbElement));
        }

        private void ValidateNumericInput(object sender, KeyPressEventArgs e)
        {
            // Разрешаем только цифры, точку и backspace
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
            {
                e.Handled = true;
            }

            // Разрешаем только одну точку
            if (e.KeyChar == '.' && ((TextBox)sender).Text.Contains("."))
            {
                e.Handled = true;
            }
        }

        private void ValidateCoefficient(object sender, EventArgs e)
        {
            TextBox txt = (TextBox)sender;
            if (decimal.TryParse(txt.Text, out decimal value))
            {
                if (value < 0 || value > 1)
                {
                    MessageBox.Show("Коэффициент должен быть от 0 до 1", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txt.Text = "";
                    return;
                }

                // Проверяем сумму всех коэффициентов
                decimal sum = elementControls
                    .Where(x => !string.IsNullOrWhiteSpace(x.coefficient.Text))
                    .Sum(x => decimal.Parse(x.coefficient.Text));

                if (sum > 1)
                {
                    MessageBox.Show("Сумма коэффициентов не может превышать 1", "Предупреждение",
                                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txt.Text = "";
                }
            }
        }

        private void CmbElement_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox cmb = (ComboBox)sender;
            if (cmb.SelectedItem.ToString() == "другое...")
            {
                string customElement = ShowCustomElementDialog();
                if (!string.IsNullOrEmpty(customElement))
                {
                    int currentIndex = cmb.SelectedIndex;
                    cmb.Items.Insert(currentIndex, customElement);
                    cmb.SelectedIndex = currentIndex;
                }
                else
                {
                    cmb.SelectedIndex = 0;
                }
            }
        }

        private string ShowCustomElementDialog()
        {
            using (var form = new Form())
            {
                form.Text = "Введите название элемента";
                form.Size = new Size(300, 150);
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                TextBox txtCustom = new TextBox
                {
                    Location = new Point(20, 20),
                    Size = new Size(240, 25)
                };

                Button btnOk = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Location = new Point(110, 60)
                };

                form.Controls.AddRange(new Control[] { txtCustom, btnOk });
                form.AcceptButton = btnOk;

                return form.ShowDialog() == DialogResult.OK ? txtCustom.Text : null;
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            AddControlElement();
        }
    }
}
