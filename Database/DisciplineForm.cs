using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace WorkSheetApplication.Database
{
    public partial class DisciplineForm : Form
    {
        private int studentId;
        private int disciplineId;
        private string disciplineName;
        private Button btnBack;
        private Panel formulaPanel;
        private LinkLabel lnkGrades;
        private LinkLabel lnkAttendance;
        private DataGridView gradesGrid;
        private Panel legendPanel;
        private Dictionary<string, decimal> formulaElements;
        private StudentForm _previousForm;
        public DisciplineForm(int studentId, int disciplineId, string disciplineName, StudentForm previousForm)
        {
            this.studentId = studentId;
            this.disciplineId = disciplineId;
            this.disciplineName = disciplineName;
            InitializeComponent();
            InitializeDisciplineForm();
            LoadData();
            _previousForm = previousForm;
        }

        private void InitializeDisciplineForm()
        {
            // Настройка формы
            this.Text = "Ведомость";
            this.Size = new Size(1020, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;

            // Кнопка "Назад"
            btnBack = new Button
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

            // Информация о студенте (будет заполнена в LoadStudentInfo)
            Label lblStudentInfo = new Label
            {
                
                AutoSize = true,
                //Size = new Size(300,40),
                Location = new Point(780, 20),
                
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight
            };
            this.Controls.Add(lblStudentInfo);
            lblStudentInfo.BringToFront();

            // Информация о дисциплине и преподавателе (будет заполнена в LoadTeacherInfo)
            Label lblDisciplineInfo = new Label
            {
                Text = disciplineName,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 50, 90),
                AutoSize = false,          // Отключаем авторазмер
                //WordWrap = true,           // Включаем перенос слов
                MaximumSize = new Size(600, 0), // Макс. ширина (настройте под вашу форму)
                Size = new Size(600, 40),  // Фиксированная ширина + начальная высота
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point((this.ClientSize.Width - 590) / 2, 60) // Центр по горизонтали
            };
            this.SizeChanged += (sender, e) =>
            {
                lblDisciplineInfo.Left = (this.ClientSize.Width - lblDisciplineInfo.Width) / 2;
            };
            Label lblTeacherInfo = new Label
            {
                //AutoSize = true,
                //Location = new Point(325, 90),
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                ForeColor = Color.FromArgb(45, 50, 80),
                AutoSize = false,          // Отключаем авторазмер
                MaximumSize = new Size(400, 0), // Макс. ширина (настройте под вашу форму)
                Size = new Size(400, 50),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point((this.ClientSize.Width - 390) / 2, 100) // Центр по горизонтали
            };
            this.SizeChanged += (sender, e) =>
            {
                lblTeacherInfo.Left = (this.ClientSize.Width - lblTeacherInfo.Width) / 2;
            };
            // Панель формулы оценки
            Label lblFormulaTitle = new Label
            {
                Text = "Формула оценки",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(420, 160),
                BackColor = Color.Transparent,
                AutoSize = true
            };

            formulaPanel = new Panel
            {
                Location = new Point(250, 190),
                Size = new Size(480, 30),
                BackColor = Color.White
            };

            // Добавляем элементы на форму
            this.Controls.AddRange(new Control[]
            {
            btnBack,
            lblStudentInfo,
            lblDisciplineInfo,
            lblTeacherInfo,
            lblFormulaTitle,
            formulaPanel
            });
            // Добавляем переключатели Оценки/Посещаемость
            Panel navPanel = new Panel
            {
                Location = new Point(20, 240),
                Size = new Size(200, 30),
                BackColor = Color.Transparent
                
            };

            lnkGrades = new LinkLabel
            {
                Text = "Оценки",
                Location = new Point(0, 5),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                LinkBehavior = LinkBehavior.NeverUnderline,
                LinkColor = Color.Black,
                Enabled = false

            };

            lnkAttendance = new LinkLabel
            {
                Text = "Посещаемость",
                Location = new Point(100, 5),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                LinkBehavior = LinkBehavior.NeverUnderline,
                LinkColor = Color.Gray
            };
            lnkAttendance.Click += LnkAttendance_Click;

            navPanel.Controls.AddRange(new Control[] { lnkGrades, lnkAttendance });

            // Добавляем легенду
            legendPanel = new Panel
            {
                Location = new Point(600, 450),
                Size = new Size(190, 100),
                BackColor = Color.White
            };

            AddLegendItem(legendPanel, Color.Red, "Оценка ниже 4", 10);
            AddLegendItem(legendPanel, Color.LightGreen, "Итоговая оценка выставлена", 35);
            AddLegendItem(legendPanel, Color.LightYellow, "Ожидает утверждения", 60);

            // Добавляем таблицу оценок
            gradesGrid = new DataGridView
            {
                Location = new Point(20, 280),
                Size = new Size(760, 250),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                BackgroundColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            // Добавляем новые элементы на форму
            this.Controls.AddRange(new Control[]
            {
            navPanel,
            legendPanel,
            gradesGrid
            });
            // Добавляем панель для накопленной и итоговой оценок
            Panel gradesInfoPanel = new Panel
            {
                Location = new Point(790, 270),
                Size = new Size(180, 80),
                BackColor = Color.Transparent
            };

            Label lblAccumGrade = new Label
            {
                Text = "Накопленная оценка:",
                Location = new Point(10, 10),
                AutoSize = true
            };

            Label lblAccumValue = new Label
            {
                Name = "lblAccumValue",
                Location = new Point(140, 10),
                AutoSize = true,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };

            Label lblFinalGrade = new Label
            {
                Text = "Итог:",
                Location = new Point(10, 30),
                AutoSize = true
            };

            Label lblFinalValue = new Label
            {
                Name = "lblFinalValue",
                Location = new Point(140, 30),
                AutoSize = true,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };

            gradesInfoPanel.Controls.AddRange(new Control[]
            {
        lblAccumGrade,
        lblAccumValue,
        lblFinalGrade,
        lblFinalValue
            });

            // Изменяем расположение легенды
            legendPanel.Location = new Point(800, 350);

            // Добавляем gradesInfoPanel в форму
            this.Controls.Add(gradesInfoPanel);
            //lblStudentInfo.Location = new Point(600, 50);
        }

        private async void LoadData()
        {
            await LoadStudentInfo();
            await LoadTeacherInfo();
            await LoadFormula();
            await LoadGrades();
        }
        private void AddLegendItem(Panel panel, Color color, string text, int yOffset)
        {
            Panel colorBox = new Panel
            {
                BackColor = color,
                Size = new Size(20, 20),
                Location = new Point(5, yOffset + 5)
            };

            Label description = new Label
            {
                Text = text,
                AutoSize = true,
                Location = new Point(30, yOffset + 5)
            };

            panel.Controls.AddRange(new Control[] { colorBox, description });
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
                                Location = new Point(10, 5),
                                //TextAlign = ContentAlignment.MiddleCenter,
                                Font = new Font("Segoe UI", 10, FontStyle.Regular)
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
                gradesGrid.Columns.Clear();
                gradesGrid.Rows.Clear();

                formulaElements = new Dictionary<string, decimal>();
                Dictionary<string, DateTime?> elementDates = new Dictionary<string, DateTime?>();

                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();
                    // Загружаем элементы контроля и их веса
                    string sql = @"
                SELECT element, weight, element_date
                FROM public.control_element
                WHERE id_discipline = @disciplineId
                ORDER BY id_element";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("disciplineId", disciplineId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            // В методе LoadGrades исправьте этот блок:
                            while (await reader.ReadAsync())
                            {
                                string element = reader.GetString(0);
                                // Исправляем получение веса - теперь не делим на 100
                                decimal weight = reader.GetDecimal(1);

                                DateTime? date;
                                if (reader.IsDBNull(2))
                                {
                                    date = null;
                                }
                                else
                                {
                                    date = reader.GetDateTime(2);
                                }

                                formulaElements[element] = weight;
                                elementDates[element] = date;
                            }

                        }
                    }

                    // Настраиваем столбцы только для элементов контроля
                    gradesGrid.Columns.Add("ElementName", "Элемент контроля");
                    foreach (var element in formulaElements.Keys)
                    {
                        gradesGrid.Columns.Add(element, element);
                    }

                    // Добавляем строку дат
                    int dateRow = gradesGrid.Rows.Add();
                    
                    gradesGrid.Rows[dateRow].Cells[0].Value = "Дата";
                    foreach (var element in formulaElements.Keys)
                    {
                        if (elementDates[element].HasValue)
                        {
                            gradesGrid.Rows[dateRow].Cells[element].Value =
                                elementDates[element].Value.ToString("dd.MM.yy");
                        }
                    }

                    // Добавляем строку оценок
                    int gradeRow = gradesGrid.Rows.Add();
                    gradesGrid.Rows[gradeRow].Cells[0].Value = "Оценка";

                    // Загружаем оценки
                    sql = @"
                SELECT ce.element, g.grade
                FROM public.grades g
                JOIN public.control_element ce ON g.id_element = ce.id_element
                WHERE g.id_student = @studentId AND ce.id_discipline = @disciplineId";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("studentId", studentId);
                        cmd.Parameters.AddWithValue("disciplineId", disciplineId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string element = reader.GetString(0);
                                int grade = reader.GetInt32(1);

                                if (gradesGrid.Columns.Contains(element))
                                {
                                    var cell = gradesGrid.Rows[gradeRow].Cells[element];
                                    cell.Value = grade;
                                    if (grade < 4)
                                    {
                                        cell.Style.BackColor = Color.LightCoral;
                                    }
                                }
                            }
                        }
                    }



                    // Вычисляем и отображаем накопленную оценку
                    decimal accumGrade = CalculateAccumGrade(gradesGrid.Rows[1]); // индекс 1 - строка с оценками

                    var lblAccumValue = this.Controls.Find("lblAccumValue", true).FirstOrDefault() as Label;
                    if (lblAccumValue != null)
                    {
                        lblAccumValue.Text = Math.Round(accumGrade, 2).ToString();
                    }
                    // Загружаем и отображаем итоговую оценку
                    sql = "SELECT final_grade FROM public.final_grade WHERE id_student = @studentId AND id_discipline = @disciplineId";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("studentId", studentId);
                        cmd.Parameters.AddWithValue("disciplineId", disciplineId);
                        var result = await cmd.ExecuteScalarAsync();

                        var lblFinalValue = this.Controls.Find("lblFinalValue", true).FirstOrDefault() as Label;
                        if (lblFinalValue != null)
                        {
                            if (result != null && result != DBNull.Value)
                            {
                                int finalGrade = Convert.ToInt32(result);
                                lblFinalValue.Text = finalGrade.ToString();
                                lblFinalValue.ForeColor = finalGrade < 4 ? Color.Red : Color.Green;
                            }
                            else
                            {
                                lblFinalValue.Text = "-";
                                lblFinalValue.ForeColor = Color.Black;
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
        private decimal CalculateAccumGrade(DataGridViewRow row)
        {
            decimal accumGrade = 0;

            foreach (var element in formulaElements)
            {
                // Пропускаем экзамен при вычислении накопленной оценки
                if (element.Key == "ЭКЗ")
                    continue;

                if (row.Cells[element.Key].Value != null &&
                    decimal.TryParse(row.Cells[element.Key].Value.ToString(), out decimal grade))
                {
                    // Используем вес напрямую, так как он уже в правильном формате
                    accumGrade += grade * element.Value;
                }
            }

            return accumGrade;
        }

        private void LnkAttendance_Click(object sender, EventArgs e)
        {
            var attendanceForm = new StudentAttendanceForm(studentId, disciplineId, disciplineName, _previousForm, this);
            this.Hide();
            attendanceForm.FormClosed += (s, args) => this.Close();
            attendanceForm.ShowDialog();
        }
        private async Task LoadStudentInfo()
        {
            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    await conn.OpenAsync();
                    string sql = @"
                    SELECT u.firstname, u.lastname, u.middlename, u.email
                    FROM public.""user"" u
                    JOIN public.students s ON s.id_user = u.id_user
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

                                var lblStudentInfo = this.Controls.OfType<Label>()
                                    .FirstOrDefault(l => l.Location.X == 780 && l.Location.Y == 20);
                                if (lblStudentInfo != null)
                                {
                                    lblStudentInfo.Text = $"{fullName}\n{email}";
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
                                    .FirstOrDefault(l => l.Location.X == (this.ClientSize.Width - 390) / 2 && l.Location.Y == 100);
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

        
    }
}
