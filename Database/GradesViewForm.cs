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
     public partial class GradesViewForm : Form
    {
        private int assistantId;
        private int disciplineId;
        private int groupId;
        private string disciplineName;
        private string groupName;
        private DataGridView gradesGrid;
        private ComboBox cmbFilter;
        private Panel formulaPanel;
        private GroupsViewForm _previousForm;

        public GradesViewForm(int assistantId, int disciplineId, int groupId, string disciplineName, string groupName, GroupsViewForm previousForm)
        {
            this.assistantId = assistantId;
            this.disciplineId = disciplineId;
            this.groupId = groupId;
            this.disciplineName = disciplineName;
            this.groupName = groupName;
            _previousForm = previousForm;
            InitializeComponent();
            InitializeGradesView();
            LoadData();
        }

        private void InitializeGradesView()
        {
            // Настройка формы
            this.Text = "Просмотр оценок";
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

            // Информация о курсе, группе и дисциплине
            Label lblCourseInfo = new Label
            {
                Text = $"{GetCourseFromGroupName(groupName)} курс\n{groupName}",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 50, 80),
                BackColor = Color.Transparent,
                Location = new Point((this.Width - 120) / 2, 60),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };

           
            //выравнивание текста по середине
            Label lblDisciplineInfo = new Label
            {
                Text = disciplineName,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 50, 80),
                AutoSize = false,          // Отключаем авторазмер
                
                MaximumSize = new Size(600, 0), // Макс. ширина (настройте под вашу форму)
                Size = new Size(600, 50),  // Фиксированная ширина + начальная высота
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point((this.ClientSize.Width - 590) / 2, 120) // Центр по горизонтали
            };
            this.SizeChanged += (sender, e) =>
            {
                lblDisciplineInfo.Left = (this.ClientSize.Width - lblDisciplineInfo.Width) / 2;
            };
            Label lblTeacherInfo = new Label
            {
                AutoSize = true,
                Location = new Point(365, 165),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Панель формулы оценки
            Label lblFormulaTitle = new Label
            {
                Text = "Формула оценки",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(420, 240),
                BackColor = Color.Transparent,
                AutoSize = true
            };

            formulaPanel = new Panel
            {
                Location = new Point(250, 270),
                Size = new Size(480, 30),
                BackColor = Color.White
            };

            // Фильтр оценок
            cmbFilter = new ComboBox
            {
                Items = { "Все оценки", "Неудовлетворительные" },
                SelectedIndex = 0,
                Location = new Point(80, 270),
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(64, 64, 64),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(150, 30)
            };
            cmbFilter.SelectedIndexChanged += (s, e) => LoadGrades();

            // Кнопка экспорта
            Button btnExport = new Button
            {
                Text = "Экспорт",
                Location = new Point(750, 270),
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
                Location = new Point(200, 350),
                Size = new Size(600, 300),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                BackgroundColor = Color.White,
                RowHeadersVisible = false
            };

            gradesGrid.Columns.Add("FullName", "ФИО");
            gradesGrid.Columns.Add("FinalGrade", "Итог");

            // Добавляем элементы на форму
            this.Controls.AddRange(new Control[]
            {
            btnBack,
            lblAssistantInfo,
            lblCourseInfo,
            lblDisciplineInfo,
            lblTeacherInfo,
            lblFormulaTitle,
            formulaPanel,
            cmbFilter,
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
        private string GetCourseFromGroupName(string groupName)
        {
            string[] parts = groupName.Split('-');
            if (parts.Length >= 2)
            {
                string yearStr = parts[1];
                if (int.TryParse(yearStr, out int yearNumber))
                {
                    int yearAdmission = 2000 + yearNumber;
                    int currentYear = DateTime.Now.Year;
                    int currentMonth = DateTime.Now.Month;

                    if (currentMonth < 9)
                        currentYear--;

                    return (currentYear - yearAdmission + 1).ToString();
                }
            }
            return "1";
        }

        private async void LoadData()
        {
            await LoadAssistantInfo();
            await LoadTeacherInfo();
            await LoadFormula();
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

        private async Task LoadTeacherInfo()
        {
            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    await conn.OpenAsync();
                    string sql = @"
                    SELECT u.firstname, u.lastname, u.middlename, u.email
                    FROM public.""user"" u
                    JOIN public.teachers t ON t.id_user = u.id_user
                    JOIN public.teachers_of_disciplines tod ON tod.id_teacher = t.id_teacher
                    WHERE tod.id_discipline = @disciplineId";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("disciplineId", disciplineId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string fullName = $"{reader.GetString(1)} {reader.GetString(0)} {reader.GetString(2)}";
                                string email = reader.GetString(3);

                                var lblTeacherInfo = this.Controls.OfType<Label>()
                                    .FirstOrDefault(l => l.Location.X == 365 && l.Location.Y == 165);
                                if (lblTeacherInfo != null)
                                {
                                    lblTeacherInfo.Text = $"{fullName}\n{email}";
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке информации о преподавателе: {ex.Message}");
            }
        }

        private async Task LoadFormula()
        {
            try
            {
                formulaPanel.Controls.Clear();

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
                            StringBuilder formula = new StringBuilder();
                            while (await reader.ReadAsync())
                            {
                                string element = reader.GetString(0);
                                decimal weight = reader.GetDecimal(1);

                                if (formula.Length > 0)
                                    formula.Append(" + ");

                                formula.Append($"{weight:F2}*{element}");
                            }

                            Label lblFormula = new Label
                            {
                                Text = formula.Length > 0 ? formula.ToString() : "Формула не задана",
                                AutoSize = true,
                                Location = new Point(10, 10),
                                Font = new Font("Arial", 10)
                            };

                            formulaPanel.Controls.Add(lblFormula);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке формулы: {ex.Message}");
            }
        }

        private async Task LoadGrades()
        {
            try
            {
                gradesGrid.Rows.Clear();

                using (var conn = DatabaseConnection.GetConnection())
                {
                    await conn.OpenAsync();
                    string sql = @"
                    SELECT u.lastname, u.firstname, u.middlename, fg.final_grade
                    FROM public.students s
                    JOIN public.""user"" u ON s.id_user = u.id_user
                    LEFT JOIN public.final_grade fg ON s.id_student = fg.id_student 
                        AND fg.id_discipline = @disciplineId
                    WHERE s.id_group = @groupId
                    ORDER BY u.lastname, u.firstname, u.middlename";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("disciplineId", disciplineId);
                        cmd.Parameters.AddWithValue("groupId", groupId);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string lastName = reader.GetString(0);
                                string firstName = reader.GetString(1);
                                string middleName = reader.GetString(2);
                                string fullName = $"{lastName} {firstName[0]}.{middleName[0]}.";

                                int? grade = null;
                                if (!reader.IsDBNull(3))
                                {
                                    grade = reader.GetInt32(3);
                                }

                                if (cmbFilter.SelectedIndex == 0 ||
                                    (cmbFilter.SelectedIndex == 1 && grade.HasValue && grade.Value < 4))
                                {
                                    int rowIndex = gradesGrid.Rows.Add();
                                    var row = gradesGrid.Rows[rowIndex];
                                    row.Cells["FullName"].Value = fullName;
                                    row.Cells["FinalGrade"].Value = grade?.ToString() ?? "-";

                                    if (grade.HasValue && grade.Value < 4)
                                    {
                                        row.Cells["FinalGrade"].Style.ForeColor = Color.Red;
                                    }
                                }
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
                    sfd.FileName = $"Оценки_{disciplineName}_{groupName}_{DateTime.Now:yyyy-MM-dd}.xlsx";

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        using (var workbook = new XLWorkbook())
                        {
                            var worksheet = workbook.Worksheets.Add("Оценки");

                            // Добавляем заголовок
                            worksheet.Cell(1, 1).Value = $"{disciplineName} - {groupName}";
                            worksheet.Range(1, 1, 1, 2).Merge();
                            worksheet.Cell(1, 1).Style.Font.Bold = true;
                            worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            // Добавляем заголовки столбцов
                            worksheet.Cell(2, 1).Value = "ФИО";
                            worksheet.Cell(2, 2).Value = "Итоговая оценка";
                            worksheet.Range(2, 1, 2, 2).Style.Font.Bold = true;

                            // Добавляем данные
                            for (int i = 0; i < gradesGrid.Rows.Count; i++)
                            {
                                worksheet.Cell(i + 3, 1).Value = gradesGrid.Rows[i].Cells["FullName"].Value?.ToString();
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
    }
}
