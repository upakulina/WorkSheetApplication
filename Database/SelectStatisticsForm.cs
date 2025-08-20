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
    public partial class SelectStatisticsForm : Form
    {
        private int assistantId;
        private ComboBox cmbCourses;
        private TextBox txtSearch;
        private Button btnSort;
        private FlowLayoutPanel disciplinesPanel;
        private bool isAscendingSort = true;
        private AssistantForm _previousForm;
        public SelectStatisticsForm(int assistantId, AssistantForm previousForm)
        {
            this.assistantId = assistantId;
            InitializeComponent();
            InitializeSelectStatisticsForm();
            LoadData();
            _previousForm = previousForm;
        }

        private void InitializeSelectStatisticsForm()
        {
            // Настройка формы
            this.Text = "Статистика";
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

            // Заголовок
            Label lblTitle = new Label
            {
                Text = "Сортировка по дисциплине",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 50, 80),
                BackColor = Color.Transparent,
                Location = new Point(340, 80),
                AutoSize = true
            };

            // Панель фильтров
            Panel filterPanel = new Panel
            {
                Location = new Point(190, 140),
                BackColor = Color.Transparent,
                Size = new Size(960, 40)
            };

            // Выбор курса
            cmbCourses = new ComboBox
            {
                Items = { "Все курсы", "1 курс", "2 курс", "3 курс", "4 курс" },
                SelectedIndex = 0,
                Location = new Point(0, 5),
                Size = new Size(150, 40),
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(64, 64, 64),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
            };
            cmbCourses.SelectedIndexChanged += ApplyFilters;

            // Поиск
            txtSearch = new TextBox
            {
                Location = new Point(170, 5),
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
                Location = new Point(490, 5),
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
                LoadDisciplines();
            };

            filterPanel.Controls.AddRange(new Control[] { cmbCourses, txtSearch, btnSort });

            // Панель для списка дисциплин
            disciplinesPanel = new FlowLayoutPanel
            {
                Location = new Point(150, 200),
                Size = new Size(700, 600),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                BackColor = Color.Transparent,
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

                    // Фильтр по курсу
                    if (cmbCourses.SelectedIndex > 0)
                    {
                        sql += " AND d.course = @course";
                    }

                    // Поиск по названию
                    if (!string.IsNullOrEmpty(txtSearch.Text))
                    {
                        sql += " AND d.discipline ILIKE @search";
                    }

                    // Сортировка
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
                                int course = reader.GetInt32(2);
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
                Size = new Size(680, 60),
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 10),
                Cursor = Cursors.Hand,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Иконка (первая буква дисциплины)
            Label lblIcon = new Label
            {
                Text = disciplineName[0].ToString(),
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
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
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(60, 10),
                Size = new Size(400, 20)
            };

            // ФИО преподавателя
            Label lblTeacher = new Label
            {
                Text = teacherName,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(60, 30),
                Size = new Size(400, 20)
            };

            panel.Controls.AddRange(new Control[] { lblIcon, lblName, lblTeacher });

            // Добавляем обработчики событий
            panel.Click += (s, e) => OpenStatisticsForm(disciplineId, disciplineName);
            foreach (Control control in panel.Controls)
            {
                control.Click += (s, e) => OpenStatisticsForm(disciplineId, disciplineName);
            }

            // Эффект при наведении
            panel.MouseEnter += (s, e) => panel.BackColor = Color.FromArgb(230, 230, 230);
            panel.MouseLeave += (s, e) => panel.BackColor = Color.WhiteSmoke;

            disciplinesPanel.Controls.Add(panel);
        }

        private void ApplyFilters(object sender, EventArgs e)
        {
            LoadDisciplines();
        }

        private void OpenStatisticsForm(int disciplineId, string disciplineName)
        {
            var statisticsForm = new StatisticsViewForm(assistantId, disciplineId, disciplineName,this);
            this.Hide();
            statisticsForm.FormClosed += (s, args) => this.Hide();
            statisticsForm.ShowDialog();
        }
    }
}
