using ClosedXML.Excel;
using Npgsql;
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

namespace WorkSheetApplication.Database
{
    public partial class WorkSheetForm : Form
    {
        private int assistantId;
        private int studentId;
        private string studentName;
        private DataGridView gradesGrid;
        private SelectStudentForm _previousForm;

        public WorkSheetForm(int assistantId, int studentId, string studentName, SelectStudentForm previousForm)
        {
            this.assistantId = assistantId;
            this.studentId = studentId;
            this.studentName = studentName;
            InitializeComponent();
            InitializeWorkSheetForm();
            LoadData();
            _previousForm = previousForm;
        }

        private void InitializeWorkSheetForm()
        {
            // Настройка формы
            this.Text = "Общая ведомость";
            this.Size = new Size(1000, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;

            // Кнопка "Назад"
            Button btnBack = new Button
            {
                Text = "← Назад",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 123, 255),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 40),
                Location = new Point(20, 20),
                Cursor = Cursors.Hand
            };
            btnBack.FlatAppearance.BorderSize = 0;
            btnBack.Click += (s, e) =>
            {
                _previousForm.Show();
                this.Hide();
            };

            // Информация о сотруднике УО
            Label lblAssistantInfo = new Label
            {
                AutoSize = true,
                Location = new Point(750, 20),
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight
            };

            // Информация о студенте
            Label lblStudentInfo = new Label
            {
                AutoSize = false,
                //Location = new Point(300, 60),
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 50, 80),
                BackColor = Color.Transparent,
                MaximumSize = new Size(600, 0), // Макс. ширина (настройте под вашу форму)
                Size = new Size(600, 120),  // Фиксированная ширина + начальная высота
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point((this.ClientSize.Width - 590) / 2, 60)
            };
            
            this.SizeChanged += (sender, e) =>
            {
                lblStudentInfo.Left = (this.ClientSize.Width - lblStudentInfo.Width) / 2;
            };
            // Заголовок "Общая ведомость"
            Label lblTitle = new Label
            {
                Text = "Общая ведомость",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 50, 80),
                BackColor = Color.Transparent,
                Location = new Point(400, 220),
                AutoSize = true
            };

            // Кнопка экспорта
            Button btnExport = new Button
            {
                Text = "Экспорт",
                Location = new Point(830, 280),
                //Size = new Size(100, 30),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(0, 123, 255), // Синий цвет
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 12),
                Cursor = Cursors.Hand
            };
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.Region = CreateRoundedRegion(btnExport.ClientRectangle, 10);
            btnExport.Click += BtnExport_Click;

            // Таблица оценок
            gradesGrid = new DataGridView
            {
                Location = new Point(200, 280),
                Size = new Size(600, 300),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                BackgroundColor = Color.White,
                RowHeadersVisible = false
            };

            gradesGrid.Columns.Add("Discipline", "Дисциплина");
            gradesGrid.Columns.Add("FinalGrade", "Итоговая оценка");

            // Добавляем элементы на форму
            this.Controls.AddRange(new Control[]
            {
            btnBack,
            lblAssistantInfo,
            lblStudentInfo,
            lblTitle,
            btnExport,
            gradesGrid
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
        private async void LoadData()
        {
            await LoadAssistantInfo();
            await LoadStudentInfo();
            await LoadGrades();
        }

        private async Task LoadAssistantInfo()
        {
            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    await conn.OpenAsync();
                    string sql = @"
                    SELECT firstname, lastname, middlename, email
                    FROM public.""user""
                    WHERE id_user = @userId";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("userId", assistantId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string fullName = $"{reader.GetString(1)} {reader.GetString(0)} {reader.GetString(2)}";
                                string email = reader.GetString(3);

                                var lblAssistantInfo = this.Controls.OfType<Label>()
                                    .FirstOrDefault(l => l.Location.X == 750 && l.Location.Y == 20);
                                if (lblAssistantInfo != null)
                                {
                                    lblAssistantInfo.Text = $"{fullName}\n{email}";
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке информации о сотруднике: {ex.Message}");
            }
        }

        private async Task LoadStudentInfo()
        {
            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    await conn.OpenAsync();
                    string sql = @"
                    SELECT u.firstname, u.lastname, u.middlename, u.email, g.group
                    FROM public.students s
                    JOIN public.""user"" u ON s.id_user = u.id_user
                    JOIN public.groups g ON s.id_group = g.id_group
                    WHERE s.id_student = @studentId";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("studentId", studentId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string fullName = $"{reader.GetString(1)} {reader.GetString(0)} {reader.GetString(2)}";
                                string email = reader.GetString(3);
                                string groupName = reader.GetString(4);
                                int course = GetCurrentCourse(groupName);

                                var lblStudentInfo = this.Controls.OfType<Label>()
                                    .FirstOrDefault(l => l.Location.X == (this.ClientSize.Width - 590) / 2 && l.Location.Y == 60);
                                if (lblStudentInfo != null)
                                {
                                    lblStudentInfo.Text = $"{course} курс  {fullName}\n{groupName}\n{email}";
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке информации о студенте: {ex.Message}");
            }
        }

        private async Task LoadGrades()
        {
            try
            {
                gradesGrid.Rows.Clear();

                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();
                    // Сначала получаем группу студента для определения курса
                    string groupSql = @"
                SELECT g.group
                FROM public.students s
                JOIN public.groups g ON s.id_group = g.id_group
                WHERE s.id_student = @studentId";

                    string groupName = "";
                    using (var cmd = new NpgsqlCommand(groupSql, conn))
                    {
                        cmd.Parameters.AddWithValue("studentId", studentId);
                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null)
                        {
                            groupName = result.ToString();
                        }
                    }

                    int currentCourse = GetCurrentCourse(groupName);

                    // Теперь загружаем дисциплины текущего курса

                    string sql = @"
                SELECT d.discipline, fg.final_grade
                FROM public.disciplines d
                JOIN public.enrollments e ON d.id_discipline = e.id_discipline
                JOIN public.groups g ON e.id_program = g.id_program
                JOIN public.students s ON s.id_group = g.id_group
                LEFT JOIN public.final_grade fg ON fg.id_student = s.id_student 
                    AND fg.id_discipline = d.id_discipline
                WHERE s.id_student = @studentId
                AND d.course = @currentCourse
                ORDER BY d.discipline";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("studentId", studentId);
                        cmd.Parameters.AddWithValue("currentCourse", currentCourse);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string disciplineName = reader.GetString(0);
                                string grade = reader.IsDBNull(1) ? "-" : reader.GetInt32(1).ToString();

                                int rowIndex = gradesGrid.Rows.Add();
                                var row = gradesGrid.Rows[rowIndex];
                                row.Cells["Discipline"].Value = disciplineName;
                                row.Cells["FinalGrade"].Value = grade;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке оценок: {ex.Message}");
            }
        }


        private void BtnExport_Click(object sender, EventArgs e)
        {
            try
            {
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "Excel файлы (*.xlsx)|*.xlsx";
                    sfd.FileName = $"Ведомость_{studentName}_{DateTime.Now:yyyy-MM-dd}.xlsx";

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        using (var workbook = new XLWorkbook())
                        {
                            var worksheet = workbook.Worksheets.Add("Ведомость");

                            // Добавляем заголовок
                            worksheet.Cell(1, 1).Value = $"Ведомость студента {studentName}";
                            worksheet.Range(1, 1, 1, 2).Merge();
                            worksheet.Cell(1, 1).Style.Font.Bold = true;
                            worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            // Добавляем заголовки столбцов
                            worksheet.Cell(2, 1).Value = "Дисциплина";
                            worksheet.Cell(2, 2).Value = "Итоговая оценка";
                            worksheet.Range(2, 1, 2, 2).Style.Font.Bold = true;

                            // Добавляем данные
                            for (int i = 0; i < gradesGrid.Rows.Count; i++)
                            {
                                worksheet.Cell(i + 3, 1).Value = gradesGrid.Rows[i].Cells["Discipline"].Value?.ToString();
                                worksheet.Cell(i + 3, 2).Value = gradesGrid.Rows[i].Cells["FinalGrade"].Value?.ToString();
                            }

                            // Настраиваем ширину столбцов
                            worksheet.Columns().AdjustToContents();

                            workbook.SaveAs(sfd.FileName);
                            MessageBox.Show("Файл успешно создан!", "Успех",
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);

                            if (MessageBox.Show("Открыть созданный файл?", "Подтверждение",
                                              MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                            {
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = sfd.FileName,
                                    UseShellExecute = true
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте данных: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int GetCurrentCourse(string groupName)
        {
            try
            {
                if (string.IsNullOrEmpty(groupName))
                    return 0;

                string[] parts = groupName.Split('-');
                if (parts.Length < 3)
                    return 0;

                string yearStr = parts[1];

                if (!int.TryParse(yearStr, out int yearNumber))
                    return 0;

                int yearAdmission = 2000 + yearNumber;
                int currentYear = DateTime.Now.Year;
                int currentMonth = DateTime.Now.Month;

                if (currentMonth < 9)
                    currentYear--;

                int course = currentYear - yearAdmission + 1;

                return course > 0 && course <= 6 ? course : 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}
