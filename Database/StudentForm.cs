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
    public partial class StudentForm : Form
    {
        private int studentId;
        private string studentGroup;
        private ComboBox cmbFilter;
        private TextBox txtSearch;
        private Button btnSort;
        private FlowLayoutPanel disciplinesPanel;
        private bool isAscendingSort = true;

        public StudentForm(int userId)
        {
            InitializeComponent();
            InitializeStudentForm(userId);
            LoadStudentInfo(userId);
            //LoadDisciplines("current");
        }

        private void InitializeStudentForm(int userId)
        {
            // Настройка формы
            this.Text = "Система ведения рабочих ведомостей";
            this.Size = new Size(1000, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;

            // Панель заголовка с информацией о студенте
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(45, 50, 80)
            };

            Label lblStudentInfo = new Label
            {
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 10),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Заголовок "Дисциплины"
            Label lblTitle = new Label
            {
                Text = "Дисциплины",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 50, 80),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point((this.Width - 200) / 2, 100) // Центрирование
            };

            // Панель фильтров
            Panel filterPanel = new Panel
            {
                Size = new Size(800, 60),
                Location = new Point((this.Width - 650) / 2, 160),
                BackColor = Color.Transparent
            };

            // Комбобокс для фильтрации
            cmbFilter = new ComboBox
            {
                Items = { "Текущие", "Прошедшие" },
                SelectedIndex = 0,
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(64, 64, 64),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(200, 40),
            };
            cmbFilter.SelectedIndexChanged += CmbFilter_SelectedIndexChanged;

            // Поле поиска
            txtSearch = new TextBox
            {
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(64, 64, 64),
                BackColor = Color.White,
                Size = new Size(300, 50),
                Margin = new Padding(0, 0, 40, 0)
            };
            txtSearch.TextChanged += TxtSearch_TextChanged;

            PictureBox searchIcon = new PictureBox
            {
                Image = Properties.Resources.search_icon,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Size = new Size(20, 20),
                Location = new Point(txtSearch.Width - 33, 2),
                Cursor = Cursors.Default
            };
            txtSearch.Controls.Add(searchIcon);

            // Кнопка сортировки
            
            btnSort = new Button
            {
                Text = "Сортировать",
                Font = new Font("Segoe UI", 11, FontStyle.Regular),
                ForeColor = Color.FromArgb(64, 64, 64),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(120, 30),
                Cursor = Cursors.Hand
            };
            btnSort.FlatAppearance.BorderColor = Color.FromArgb(0, 123, 255);
            btnSort.FlatAppearance.BorderSize = 1;
            btnSort.Click += BtnSort_Click;

            cmbFilter.Location = new Point(0, 10);
            txtSearch.Location = new Point(220, 10);
            btnSort.Location = new Point(540, 10);

            // Панель для списка дисциплин
            disciplinesPanel = new FlowLayoutPanel
            {
                Location = new Point(130, 240),
                Size = new Size(960, 550),
                AutoScroll = true,
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.TopDown
            };

            // Добавление элементов на форму
            headerPanel.Controls.Add(lblStudentInfo);
            filterPanel.Controls.AddRange(new Control[] { cmbFilter, txtSearch, btnSort });
            this.Controls.AddRange(new Control[] { headerPanel, lblTitle, filterPanel, disciplinesPanel });
        }

        private async void LoadStudentInfo(int userId)
        {
            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    await conn.OpenAsync();
                    string sql = @"
                SELECT u.firstname, u.lastname, u.middlename, u.email, s.id_student, g.group
                FROM public.""user"" u
                JOIN public.students s ON s.id_user = u.id_user
                JOIN public.groups g ON s.id_group = g.id_group
                WHERE u.id_user = @userId";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("userId", userId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string fullName = $"{reader.GetString(1)} {reader.GetString(0)} {reader.GetString(2)}";
                                string email = reader.GetString(3);
                                studentId = reader.GetInt32(4);
                                studentGroup = reader.GetString(5);

                                // Проверяем, что группа загрузилась
                                Console.WriteLine($"Загруженная группа: {studentGroup}");

                                // Обновляем информацию в заголовке
                                var lblStudentInfo = this.Controls.OfType<Panel>().First()
                                                       .Controls.OfType<Label>().First();
                                lblStudentInfo.Text = $"{fullName}\n{email}";

                                // Загружаем дисциплины только после успешной загрузки информации о студенте
                                LoadDisciplines("current");
                            }
                            else
                            {
                                MessageBox.Show("Не удалось найти информацию о студенте", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке информации о студенте: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private async void LoadDisciplines(string filter)
        {
            try
            {
                disciplinesPanel.Controls.Clear();
                int currentCourse = GetCurrentCourse(studentGroup);

                using (var conn = DatabaseConnection.GetConnection())
                {
                    await conn.OpenAsync();
                    string sql = @"
                    SELECT DISTINCT d.discipline, u.lastname, u.firstname, u.middlename
                    FROM public.disciplines d
                    JOIN public.teachers_of_disciplines tod ON d.id_discipline = tod.id_discipline
                    JOIN public.teachers t ON tod.id_teacher = t.id_teacher
                    JOIN public.""user"" u ON t.id_user = u.id_user
                    JOIN public.enrollments e ON d.id_discipline = e.id_discipline
                    JOIN public.groups g ON g.id_program = e.id_program
                    JOIN public.students s ON s.id_group = g.id_group
                    WHERE s.id_student = @studentId
                    AND d.course <= @currentCourse";

                    if (filter == "current")
                        sql += " AND d.course = @currentCourse";
                    else if (filter == "past")
                        sql += " AND d.course < @currentCourse";

                    if (!string.IsNullOrEmpty(txtSearch.Text))
                        sql += " AND d.discipline ILIKE @search";

                    sql += " ORDER BY d.discipline" + (isAscendingSort ? " ASC" : " DESC");

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("studentId", studentId);
                        cmd.Parameters.AddWithValue("currentCourse", currentCourse);
                        if (!string.IsNullOrEmpty(txtSearch.Text))
                            cmd.Parameters.AddWithValue("search", $"%{txtSearch.Text}%");

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                AddDisciplinePanel(
                                    reader.GetString(0),
                                    $"{reader.GetString(1)} {reader.GetString(2)} {reader.GetString(3)}"
                                );
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке дисциплин: {ex.Message}");
            }
        }

        private void AddDisciplinePanel(string disciplineName, string teacherName)
        {
            Panel disciplinePanel = new Panel
            {
                Size = new Size(740, 60),
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 10),
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand
            };

            Label lblIcon = new Label
            {
                Text = disciplineName.Trim().ToUpper()[0].ToString(),
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Size = new Size(40, 40),
                Location = new Point(10, 10),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(45, 50, 80),
                ForeColor = Color.White
            };

            Label lblDiscipline = new Label
            {
                Text = disciplineName,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(60, 8),
                AutoSize = true,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };

            Label lblTeacher = new Label
            {
                Text = teacherName,
                Font = new Font("Segoe UI", 10),
                Location = new Point(60, 30),
                AutoSize = true,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };

            // Добавляем обработчик клика для всей панели
            disciplinePanel.Click += (sender, e) => DisciplinePanel_Click(disciplineName, teacherName);
            // Добавляем обработчики и для отдельных элементов, чтобы клик работал везде
            lblIcon.Click += (sender, e) => DisciplinePanel_Click(disciplineName, teacherName);
            lblDiscipline.Click += (sender, e) => DisciplinePanel_Click(disciplineName, teacherName);
            lblTeacher.Click += (sender, e) => DisciplinePanel_Click(disciplineName, teacherName);

            // Добавляем эффект при наведении
            disciplinePanel.MouseEnter += (sender, e) =>
            {
                disciplinePanel.BackColor = Color.LightGray;
            };
            disciplinePanel.MouseLeave += (sender, e) =>
            {
                disciplinePanel.BackColor = Color.White;
            };

            disciplinePanel.Controls.AddRange(new Control[] { lblIcon, lblDiscipline, lblTeacher });
            disciplinesPanel.Controls.Add(disciplinePanel);

        }
        private async void DisciplinePanel_Click(string disciplineName, string teacherName)
        {
            try
            {
                // Получаем email преподавателя и id дисциплины из базы данных
                using (var conn = DatabaseConnection.GetConnection())
                {
                    await conn.OpenAsync();
                    string sql = @"
                SELECT u.email, d.id_discipline
                FROM public.disciplines d
                JOIN public.teachers_of_disciplines tod ON d.id_discipline = tod.id_discipline
                JOIN public.teachers t ON tod.id_teacher = t.id_teacher
                JOIN public.""user"" u ON t.id_user = u.id_user
                WHERE d.discipline = @disciplineName AND 
                      CONCAT(u.lastname, ' ', u.firstname, ' ', u.middlename) = @teacherName";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("disciplineName", disciplineName);
                        cmd.Parameters.AddWithValue("teacherName", teacherName);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string teacherEmail = reader.GetString(0);
                                int disciplineId = reader.GetInt32(1);

                                // Создаем и открываем форму DisciplineForm
                                var disciplineForm = new DisciplineForm(studentId, disciplineId, disciplineName, this /*teacherName, teacherEmail*/);
                                this.Hide();
                                disciplineForm.ShowDialog();
                            }
                            else
                            {
                                MessageBox.Show("Не удалось найти информацию о дисциплине", "Ошибка",
                                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии формы дисциплины: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int GetCurrentCourse(string groupName)
        {
            try
            {
                if (string.IsNullOrEmpty(groupName))
                {
                    MessageBox.Show("Ошибка: строка группы пустая", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return 1;
                }

                // Разбиваем строку по тире и проверяем части
                string[] parts = groupName.Split('-');
                if (parts.Length < 3)
                {
                    MessageBox.Show($"Ошибка: неверный формат группы: {groupName}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return 1;
                }

                // Берем среднюю часть (22 из РИС-22-3)
                string yearStr = parts[1];

                if (!int.TryParse(yearStr, out int yearNumber))
                {
                    MessageBox.Show($"Ошибка: не удалось преобразовать год {yearStr} в число", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return 1;
                }

                int yearAdmission = 2000 + yearNumber;
                int currentYear = DateTime.Now.Year;
                int currentMonth = DateTime.Now.Month;

                if (currentMonth < 9)
                    currentYear--;

                int course = currentYear - yearAdmission + 1;

                // Проверяем валидность курса
                if (course < 1 || course > 6)
                {
                    MessageBox.Show($"Предупреждение: получен неожиданный курс: {course}", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return 1;
                }

                return course;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при определении курса: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }


        }

        private void CmbFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadDisciplines(cmbFilter.SelectedIndex == 0 ? "current" : "past");
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            LoadDisciplines(cmbFilter.SelectedIndex == 0 ? "current" : "past");
        }

        private void BtnSort_Click(object sender, EventArgs e)
        {
            isAscendingSort = !isAscendingSort;
            LoadDisciplines(cmbFilter.SelectedIndex == 0 ? "current" : "past");
        }
    }
}
