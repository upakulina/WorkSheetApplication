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
    public partial class AllDisciplinesForm : Form
    {
        private int assistantId;
        private ComboBox cmbCourses;
        private TextBox txtSearch;
        private Button btnSort;
        private FlowLayoutPanel disciplinesPanel;
        private bool isAscendingSort = true;
        private AssistantForm _previousForm;
        private int disciplineId;
        public AllDisciplinesForm(int assistantId, AssistantForm previousForm)
        {
            this.assistantId = assistantId;
            InitializeComponent();
            InitializeAllDisciplinesForm();
            LoadData();
            _previousForm = previousForm;
        }

        private void InitializeAllDisciplinesForm()
        {
            // Настройка формы
            this.Text = "Дисциплины";
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
                ForeColor = Color.Black,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight
            };

            // Заголовок
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
                Location = new Point((this.Width - 700) / 2, 160),
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
                Size = new Size(200, 40)
            };
            cmbCourses.SelectedIndexChanged += ApplyFilters;

            // Поиск
            txtSearch = new TextBox
            {
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(64, 64, 64),
                BackColor = Color.White,
                Size = new Size(300, 50),
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
                Font = new Font("Segoe UI", 11, FontStyle.Regular),
                ForeColor = Color.FromArgb(64, 64, 64),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(120, 30),
                Cursor = Cursors.Hand
            };
            btnSort.FlatAppearance.BorderColor = Color.FromArgb(0, 123, 255);
            btnSort.FlatAppearance.BorderSize = 2;
            btnSort.Click += (s, e) =>
            {
                isAscendingSort = !isAscendingSort;
                LoadDisciplines();
            };
            cmbCourses.Location = new Point(0, 10);
            txtSearch.Location = new Point(220, 10);
            btnSort.Location = new Point(540, 10);
            filterPanel.Controls.AddRange(new Control[] { cmbCourses, txtSearch, btnSort });

            // Панель для списка дисциплин
            disciplinesPanel = new FlowLayoutPanel
            {
                Location = new Point(130, 240),
                Size = new Size(960, 550),
                AutoScroll = true,
                BackColor = Color.Transparent,
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
            disciplinesPanel
            });
        }

        private async void LoadData()
        {
            await LoadAssistantInfo();
            await LoadDisciplines();
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

        private async Task LoadDisciplines()
        {
            try
            {
                disciplinesPanel.Controls.Clear();

                using (var conn = DatabaseConnection.GetConnection())
                {
                    await conn.OpenAsync();
                    string sql = @"
                    SELECT DISTINCT 
                        d.id_discipline,
                        d.discipline,
                        d.course,
                        u.lastname,
                        u.firstname,
                        u.middlename
                    FROM public.disciplines d
                    JOIN public.teachers_of_disciplines tod ON d.id_discipline = tod.id_discipline
                    JOIN public.teachers t ON tod.id_teacher = t.id_teacher
                    JOIN public.""user"" u ON t.id_user = u.id_user
                    WHERE 1=1";

                    // Добавляем фильтр по курсу
                    if (cmbCourses.SelectedIndex > 0)
                    {
                        sql += " AND d.course = @course";
                    }

                    // Добавляем поиск по названию
                    if (!string.IsNullOrEmpty(txtSearch.Text))
                    {
                        sql += " AND d.discipline ILIKE @search";
                    }

                    // Добавляем сортировку
                    sql += $" ORDER BY d.discipline {(isAscendingSort ? "ASC" : "DESC")}";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        if (cmbCourses.SelectedIndex > 0)
                        {
                            cmd.Parameters.AddWithValue("course", cmbCourses.SelectedIndex);
                        }
                        if (!string.IsNullOrEmpty(txtSearch.Text))
                        {
                            cmd.Parameters.AddWithValue("search", $"%{txtSearch.Text}%");
                        }

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                int disciplineId = reader.GetInt32(0);
                                string disciplineName = reader.GetString(1);
                                string teacherName = $"{reader.GetString(3)} {reader.GetString(4)[0]}.{reader.GetString(5)[0]}.";
                                AddDisciplinePanel(disciplineId, disciplineName, teacherName);
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

        private void AddDisciplinePanel(int disciplineId, string disciplineName, string teacherName)
        {
            Panel panel = new Panel
            {
                Size = new Size(720, 60),
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 10),
                Cursor = Cursors.Hand,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Иконка (первая буква названия дисциплины)
            Label lblIcon = new Label
            {
                Text = disciplineName[0].ToString(),
                Font = new Font("Arial", 16, FontStyle.Bold),
                Size = new Size(40, 40),
                Location = new Point(10, 10),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(45, 50, 80),
                ForeColor = Color.White
            };

            // Название дисциплины
            Label lblName = new Label
            {
                Text = disciplineName,
                Font = new Font("Arial", 12, FontStyle.Bold),
                Location = new Point(60, 10),
                Size = new Size(400, 20)
            };

            // ФИО преподавателя
            Label lblTeacher = new Label
            {
                Text = teacherName,
                Font = new Font("Arial", 10),
                Location = new Point(60, 30),
                Size = new Size(400, 20)
            };

            // Стрелка "Перейти"
            Label lblArrow = new Label
            {
                Text = "Перейти →",
                Font = new Font("Arial", 10),
                Location = new Point(620, 20),
                AutoSize = true,
                Cursor = Cursors.Hand
            };

            panel.Controls.AddRange(new Control[] { lblIcon, lblName, lblTeacher, lblArrow });
            disciplinesPanel.Controls.Add(panel);

            panel.Click += (s, e) => OpenGroupsView(disciplineId, disciplineName);
            foreach (Control control in panel.Controls)
            {
                control.Click += (s, e) => OpenGroupsView(disciplineId, disciplineName);
            }

        }
        private void OpenGroupsView(int disciplineId, string disciplineName)
        {
            var groupsViewForm = new GroupsViewForm(assistantId, disciplineId, disciplineName,this);
            this.Hide();
            groupsViewForm.FormClosed += (s, args) => this.Close();
            groupsViewForm.ShowDialog();
        }
        private void ApplyFilters(object sender, EventArgs e)
        {
            LoadDisciplines();
        }
    }
}
