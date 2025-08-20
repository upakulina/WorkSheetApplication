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

namespace WorkSheetApplication.Database
{

    public partial class StudentAttendanceForm : Form
    {
        private int studentId;
        private int disciplineId;
        private string disciplineName;
        private Button btnBack;
        private LinkLabel lnkGrades;
        private LinkLabel lnkAttendance;
        private DataGridView attendanceGrid;
        private Label lblAttendancePercent;
        private StudentForm _studentForm;
        private DisciplineForm _previousForm;
        private Panel formulaPanel;

        public StudentAttendanceForm(int studentId, int disciplineId, string disciplineName, StudentForm studentForm, DisciplineForm previousForm)
        {
            this.studentId = studentId;
            this.disciplineId = disciplineId;
            this.disciplineName = disciplineName;
            InitializeComponent();
            InitializeStudentAttendanceForm();
            LoadData();
            _studentForm = studentForm;
            _previousForm = previousForm;
        }

        private void InitializeStudentAttendanceForm()
        {
            // Настройка формы
            this.Text = "Посещаемость";
            this.Size = new Size(1000, 800);
            this.StartPosition = FormStartPosition.CenterScreen;

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
                Location = new Point(780, 20),
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight
            };

            // Информация о дисциплине и преподавателе
            Label lblDisciplineInfo = new Label
            {
                Text = disciplineName,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 50, 90),
                AutoSize = false,          // Отключаем авторазмер

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
                //WordWrap = true,           // Включаем перенос слов
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

            //панель для формулы
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

            // Переключатели Оценки/Посещаемость
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
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                LinkBehavior = LinkBehavior.NeverUnderline,
                LinkColor = Color.Gray
            };
            lnkGrades.Click += LnkGrades_Click;

            lnkAttendance = new LinkLabel
            {
                Text = "Посещаемость",
                Location = new Point(90, 5),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                LinkBehavior = LinkBehavior.NeverUnderline,
                LinkColor = Color.Black,
                Enabled = false
            };

            navPanel.Controls.AddRange(new Control[] { lnkGrades, lnkAttendance });

            // Таблица посещаемости
            attendanceGrid = new DataGridView
            {
                Location = new Point(20, 280),
                Size = new Size(760, 250),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            // Процент посещаемости
            lblAttendancePercent = new Label
            {
                Location = new Point(790, 280),
                Size = new Size(200, 50),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 50, 80),
                BackColor = Color.Transparent,
                //TextAlign = ContentAlignment.MiddleCenter
            };

            // Добавляем элементы на форму
            this.Controls.AddRange(new Control[]
            {
            btnBack,
            lblStudentInfo,
            lblDisciplineInfo,
            lblTeacherInfo,
            navPanel,
            attendanceGrid,
            lblAttendancePercent,
            lblFormulaTitle,
            formulaPanel
            });
        }

        private async void LoadData()
        {
            await LoadStudentInfo();
            await LoadTeacherInfo();
            await LoadAttendance();
            await LoadFormula();
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

        private async Task LoadAttendance()
        {
            try
            {
                attendanceGrid.Columns.Clear();
                attendanceGrid.Rows.Clear();

                using (var conn = DatabaseConnection.GetConnection())
                {
                    await conn.OpenAsync();

                    // Получаем даты из control_element
                    string sql = @"
                    SELECT element_date
                    FROM public.control_element
                    WHERE id_discipline = @disciplineId 
                    AND element_date IS NOT NULL
                    ORDER BY element_date";

                    List<DateTime> dates = new List<DateTime>();
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("disciplineId", disciplineId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                dates.Add(reader.GetDateTime(0));
                            }
                        }
                    }

                    if (dates.Count > 0)
                    {
                        // Настраиваем столбцы
                        attendanceGrid.Columns.Add("Date", "Дата");
                        attendanceGrid.Rows.Add();

                        // Получаем посещаемость
                        sql = @"
                        SELECT date, status
                        FROM public.attendance
                        WHERE id_student = @studentId 
                        AND id_discipline = @disciplineId
                        AND date = ANY(@dates)";

                        Dictionary<DateTime, bool> attendance = new Dictionary<DateTime, bool>();
                        using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("studentId", studentId);
                            cmd.Parameters.AddWithValue("disciplineId", disciplineId);
                            cmd.Parameters.AddWithValue("dates", dates.ToArray());
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    DateTime date = reader.GetDateTime(0);
                                    bool status = reader.GetBoolean(1);
                                    attendance[date] = status;
                                }
                            }
                        }

                        // Заполняем таблицу
                        int totalDates = 0;
                        int presentDates = 0;

                        foreach (var date in dates)
                        {
                            string columnName = date.ToString("dd.MM.yy");
                            var column = new DataGridViewCheckBoxColumn
                            {
                                Name = columnName,
                                HeaderText = columnName,
                                ReadOnly = true
                            };
                            attendanceGrid.Columns.Add(column);

                            bool isPresent = attendance.ContainsKey(date) && attendance[date];
                            attendanceGrid.Rows[0].Cells[columnName].Value = isPresent;

                            if (isPresent)
                                presentDates++;
                            totalDates++;
                        }

                        // Вычисляем и отображаем процент посещаемости
                        if (totalDates > 0)
                        {
                            double percent = (double)presentDates / totalDates * 100;
                            lblAttendancePercent.Text = $"Процент посещаемости: {percent:F1}%";
                        }
                        else
                        {
                            lblAttendancePercent.Text = "Нет данных";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке посещаемости: {ex.Message}");
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
                                Location = new Point(10, 5),
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
        private void LnkGrades_Click(object sender, EventArgs e)
        {
            var gradesForm = new DisciplineForm(studentId, disciplineId, disciplineName, _studentForm);
            this.Hide();
            gradesForm.FormClosed += (s, args) => this.Close();
            gradesForm.ShowDialog();
        }
    }

}
