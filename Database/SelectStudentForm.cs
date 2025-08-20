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
    public partial class SelectStudentForm : Form
    {
        private int assistantId;
        private ComboBox cmbCourses;
        private ComboBox cmbPrograms;
        
        private TextBox txtSearch;
        private Button btnSort;
        private FlowLayoutPanel studentsPanel;
        private bool isAscendingSort = true;
        private AssistantForm _previousForm;

        public SelectStudentForm(int assistantId, AssistantForm previousForm)
        {
            this.assistantId = assistantId;
            InitializeComponent();
            InitializeSelectStudentForm();
            LoadData();
            _previousForm = previousForm;
        }

        private void InitializeSelectStudentForm()
        {
            // Настройка формы
            this.Text = "Поиск студента";
            this.Size = new Size(1000, 800);
            this.StartPosition = FormStartPosition.CenterScreen;

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

            // Заголовок
            Label lblTitle = new Label
            {
                Text = "Поиск студента",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 50, 80),
                BackColor = Color.Transparent,
                Location = new Point(420, 80),
                AutoSize = true
            };

            // Панель фильтров
            Panel filterPanel = new Panel
            {
                //Location = new Point(20, 100),
                //Size = new Size(960, 40)
                Size = new Size(960, 40),
                Location = new Point(20, 140),
                BackColor = Color.Transparent
            };

            // Выбор курса
            cmbCourses = new ComboBox
            {
                Items = { "Все курсы", "1 курс", "2 курс", "3 курс", "4 курс" },
                SelectedIndex = 0,
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(64, 64, 64),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(120, 40),
                Location = new Point(130, 5),
                //Size = new Size(120, 30)
            };
            cmbCourses.SelectedIndexChanged += ApplyFilters;

            // Выбор ОП
            cmbPrograms = new ComboBox
            {
                Items = { "Все ОП", "РИС", "МБ", "Ю", "ИЯ" },
                SelectedIndex = 0,
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(64, 64, 64),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(260, 5),
                Size = new Size(120, 40)
            };
            cmbPrograms.SelectedIndexChanged += Program_SelectedIndexChanged;

            

            // Поиск
            txtSearch = new TextBox
            {
                Location = new Point(390, 5),
                Size = new Size(300, 40),
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(64, 64, 64),
                BackColor = Color.White,
                Margin = new Padding(0, 0, 40, 0)

            };
            txtSearch.TextChanged += ApplyFilters;
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
                Location = new Point(700, 5),
                Size = new Size(120, 28),
                Font = new Font("Segoe UI", 11, FontStyle.Regular),
                ForeColor = Color.FromArgb(64, 64, 64),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSort.FlatAppearance.BorderColor = Color.FromArgb(0, 123, 255);
            btnSort.FlatAppearance.BorderSize = 1;
            btnSort.Click += (s, e) =>
            {
                isAscendingSort = !isAscendingSort;
                LoadStudents();
            };

            filterPanel.Controls.AddRange(new Control[]
            {
            cmbCourses,
            cmbPrograms,
            
            txtSearch,
            btnSort
            });

            // Панель для списка студентов
            studentsPanel = new FlowLayoutPanel
            {
                Location = new Point(150, 200),
                Size = new Size(700, 600),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false

            };

            // Добавляем элементы на форму
            this.Controls.AddRange(new Control[]
            {
            btnBack,
            lblAssistantInfo,
            lblTitle,
            filterPanel,
            studentsPanel
            });
        }

        private async void LoadData()
        {
            await LoadAssistantInfo();
            await LoadStudents();
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
        private async Task LoadStudents()
        {
            try
            {
                studentsPanel.Controls.Clear();

                using (var conn = DatabaseConnection.GetConnection())
                {
                    await conn.OpenAsync();
                    string sql = @"
                SELECT s.id_student, u.lastname, u.firstname, u.middlename, g.group
                FROM public.students s
                JOIN public.""user"" u ON s.id_user = u.id_user
                JOIN public.groups g ON s.id_group = g.id_group
                WHERE 1=1";

                    // Фильтр по ОП
                    if (cmbPrograms.SelectedIndex > 0)
                    {
                        sql += " AND g.group LIKE @programPrefix || '%'";
                    }

                    // Поиск
                    if (!string.IsNullOrEmpty(txtSearch.Text))
                    {
                        sql += @" AND (
                    LOWER(u.lastname) LIKE LOWER(@search) OR 
                    LOWER(u.firstname) LIKE LOWER(@search) OR 
                    LOWER(u.middlename) LIKE LOWER(@search) OR 
                    LOWER(g.group) LIKE LOWER(@search)
                )";
                    }

                    // Сортировка
                    sql += $" ORDER BY u.lastname {(isAscendingSort ? "ASC" : "DESC")}, u.firstname, u.middlename";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        if (cmbPrograms.SelectedIndex > 0)
                        {
                            cmd.Parameters.AddWithValue("programPrefix", cmbPrograms.SelectedItem.ToString());
                        }
                        if (!string.IsNullOrEmpty(txtSearch.Text))
                        {
                            cmd.Parameters.AddWithValue("search", $"%{txtSearch.Text}%");
                        }

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                int studentId = reader.GetInt32(0);
                                string fullName = $"{reader.GetString(1)} {reader.GetString(2)} {reader.GetString(3)}";
                                string groupName = reader.GetString(4);

                                // Применяем фильтр по курсу после получения данных
                                if (cmbCourses.SelectedIndex > 0)
                                {
                                    int studentCourse = GetCurrentCourse(groupName);
                                    if (studentCourse != cmbCourses.SelectedIndex)
                                        continue;
                                }

                                AddStudentPanel(studentId, fullName, groupName);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке студентов: {ex.Message}");
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

                // Берем среднюю часть (22 из РИС-22-3)
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
        private void AddStudentPanel(int studentId, string fullName, string groupName)
        {
            Panel panel = new Panel
            {
                Size = new Size(680, 60),
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 10),
                Cursor = Cursors.Hand,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Иконка (первая буква фамилии)
            Label lblIcon = new Label
            {
                Text = fullName[0].ToString(),
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Size = new Size(40, 40),
                Location = new Point(10, 10),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(45, 50, 80),
                ForeColor = Color.White
            };

            // ФИО студента
            Label lblName = new Label
            {
                Text = fullName,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(60, 10),
                AutoSize = true
            };

            // Группа
            Label lblGroup = new Label
            {
                Text = groupName,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                Location = new Point(60, 30),
                Size = new Size(400, 20)
            };

            // Кнопка "Перейти"
            Button btnGoto = new Button
            {
                Text = "Перейти →",
                Location = new Point(575, 15),
                Size = new Size(90, 30),
                Font = new Font("Segoe UI", 10),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(45, 50, 80),
                Cursor = Cursors.Hand
            };
            btnGoto.Click += (s, e) => OpenWorksheetForm(studentId, fullName);

            panel.Controls.AddRange(new Control[] { lblIcon, lblName, lblGroup, btnGoto });
            studentsPanel.Controls.Add(panel);
        }

        private void Program_SelectedIndexChanged(object sender, EventArgs e)
        {
            
            ApplyFilters(sender, e);
        }

        private void ApplyFilters(object sender, EventArgs e)
        {
            LoadStudents();
        }

        private void OpenWorksheetForm(int studentId, string studentName)
        {
            var worksheetForm = new WorkSheetForm(assistantId, studentId, studentName,this);
            this.Hide();
            worksheetForm.FormClosed += (s, args) => this.Close();
            worksheetForm.ShowDialog();
        }
    }
}
