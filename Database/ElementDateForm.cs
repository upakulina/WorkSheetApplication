using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Npgsql;

namespace WorkSheetApplication.Database
{
    public partial class ElementDateForm : Form
    {
        private int disciplineId;
        private string elementName;
        private DateTimePicker datePicker;
        private Button btnSave;

        public ElementDateForm(int disciplineId, string elementName)
        {
            this.disciplineId = disciplineId;
            this.elementName = elementName;
            InitializeComponent();
            InitializeElementDateForm();
            LoadExistingDate();
        }

        private void InitializeElementDateForm()
        {
            // Настройка формы
            this.Text = "Добавление даты";
            this.Size = new Size(400, 280);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            //this.BackColor = Color.White;

            // Заголовок
            Label lblTitle = new Label
            {
                Text = "Добавление даты",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 50, 80),
                AutoSize = true,
                Location = new Point(this.ClientSize.Width / 2 - 80, 30),
                BackColor = Color.Transparent
            };
            //lblTitle.Location = new Point(this.ClientSize.Width / 2 - lblTitle.Width / 2, 20);

            // Название элемента контроля
            Label lblElement = new Label
            {
                Text = elementName,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(50, 100),
                BackColor = Color.Transparent
            };

            // Выбор даты
            datePicker = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd-MM-yy",
                Location = new Point(200, 100),
                Size = new Size(150, 25)
            };

            // Кнопка сохранения
            btnSave = new Button
            {
                Text = "Сохранить",
                Location = new Point(this.ClientSize.Width / 2 - 50, 160),
                Size = new Size(100, 40),

                Font = new Font("Segoe UI Semibold", 12),
                ForeColor = Color.FromArgb(45, 50, 80),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Region = CreateRoundedRegion(btnSave.ClientRectangle, 10);
            btnSave.Click += BtnSave_Click;

            // Добавляем элементы на форму
            this.Controls.AddRange(new Control[]
            {
            lblTitle,
            lblElement,
            datePicker,
            btnSave
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
        private async void LoadExistingDate()
        {
            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    await conn.OpenAsync();
                    string sql = @"
                    SELECT element_date                    
                    FROM public.control_element 
                    WHERE id_discipline = @disciplineId 
                    AND element = @elementName";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("disciplineId", disciplineId);
                        cmd.Parameters.AddWithValue("elementName", elementName);

                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            datePicker.Value = Convert.ToDateTime(result);
                        }
                        else
                        {
                            datePicker.Value = DateTime.Now;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке даты: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    await conn.OpenAsync();
                    string sql = @"
                    UPDATE public.control_element 
                    SET element_date = @date
                    WHERE id_discipline = @disciplineId 
                    AND element = @elementName";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("date", datePicker.Value.Date);
                        cmd.Parameters.AddWithValue("disciplineId", disciplineId);
                        cmd.Parameters.AddWithValue("elementName", elementName);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                MessageBox.Show("Дата успешно сохранена!", "Успех",
                              MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении даты: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
