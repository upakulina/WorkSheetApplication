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
    public partial class TeacherForm : Form
    {
        private int teacherId;
        private ComboBox cmbCourses;
        private TextBox txtSearch;
        private Button btnSort;
        private FlowLayoutPanel coursesPanel;
        private bool isAscendingSort = true;

        public TeacherForm(int userId)
        {
            InitializeComponent();
            InitializeTeacherForm();
            InitializeDataAsync(userId);
        }

        private void InitializeTeacherForm()
        {
            

            // Настройка формы
            this.Text = "Система ведения рабочих ведомостей";
            this.Size = new Size(1000, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;

            // Панель заголовка с информацией о преподавателе
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(45, 50, 80)
            };

            Label lblTeacherInfo = new Label
            {
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 10),
                TextAlign = ContentAlignment.MiddleLeft
            };
            headerPanel.Controls.Add(lblTeacherInfo);

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

            // Комбобокс для выбора курса
            cmbCourses = new ComboBox
            {
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(64, 64, 64),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(200, 40),
                Items = { "Все курсы", "1 курс", "2 курс", "3 курс", "4 курс" },
                SelectedIndex = 0
            };

            cmbCourses.SelectedIndexChanged += CmbCourses_SelectedIndexChanged;


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

            //иконка для поиска
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


            //расположение
            cmbCourses.Location = new Point(0, 10);
            txtSearch.Location = new Point(220, 10);
            btnSort.Location = new Point(540, 10);

            filterPanel.Controls.AddRange(new Control[] { cmbCourses, txtSearch, btnSort });

            // Панель для курсов и дисциплин
            coursesPanel = new FlowLayoutPanel
            {
                Size = new Size(960, 550),
                Location = new Point(20, 240),
                AutoScroll = true,
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.TopDown
            };

            // Добавление элементов на форму
            this.Controls.AddRange(new Control[] { headerPanel, lblTitle, filterPanel, coursesPanel });
            this.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, filterPanel.ClientRectangle,
                    Color.FromArgb(200, 200, 200), ButtonBorderStyle.Solid);
            };
            btnSort.MouseEnter += (s, e) => btnSort.BackColor = Color.FromArgb(245, 245, 245);
            btnSort.MouseLeave += (s, e) => btnSort.BackColor = Color.White;
        }
        private async void InitializeDataAsync(int userId)
        {
            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    await conn.OpenAsync();
                    string sql = @"
                    SELECT u.firstname, u.lastname, u.middlename, u.email, t.id_teacher
                    FROM public.""user"" u
                    JOIN public.teachers t ON t.id_user = u.id_user
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
                                teacherId = reader.GetInt32(4);

                                var lblTeacherInfo = this.Controls.OfType<Panel>().First()
                                                       .Controls.OfType<Label>().First();
                                lblTeacherInfo.Text = $"{fullName}\n{email}";

                                // Загружаем дисциплины только после получения teacherId
                                await LoadDisciplinesAsync();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при инициализации данных: {ex.Message}");
            }
        }
        private async Task LoadDisciplinesAsync()
        {
            try
            {
                coursesPanel.Controls.Clear();
                using (var conn = DatabaseConnection.GetConnection())
                {
                    await conn.OpenAsync();
                    string sql = @"
                    SELECT DISTINCT d.discipline, d.course, d.id_discipline
                    FROM public.disciplines d
                    JOIN public.teachers_of_disciplines tod ON d.id_discipline = tod.id_discipline
                    JOIN public.teachers t ON tod.id_teacher = t.id_teacher
                    WHERE t.id_teacher = @teacherId";

                    if (cmbCourses.SelectedIndex > 0)
                    {
                        sql += " AND d.course = @course";
                    }
                    if (!string.IsNullOrEmpty(txtSearch.Text))
                    {
                        sql += " AND d.discipline ILIKE @search";
                    }

                    sql += " ORDER BY d.course, " + (isAscendingSort ? "d.discipline ASC" : "d.discipline DESC");

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("teacherId", teacherId);
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
                            int currentCourse = 0;
                            FlowLayoutPanel currentCoursePanel = null;
                            int disciplineCount = 0;

                            while (await reader.ReadAsync())
                            {
                                string disciplineName = reader.GetString(0);
                                int course = reader.GetInt32(1);
                                int disciplineId = reader.GetInt32(2);

                                if (currentCourse != course)
                                {
                                    currentCourse = course;
                                    disciplineCount = 0;

                                    Label lblCourse = new Label
                                    {
                                        Text = $"{course} курс",
                                        Font = new Font("Arial", 14, FontStyle.Bold),
                                        AutoSize = true,
                                        Margin = new Padding(0, 20, 0, 10)
                                    };
                                    coursesPanel.Controls.Add(lblCourse);

                                    currentCoursePanel = new FlowLayoutPanel
                                    {
                                        Width = 960,
                                        Height = 100,
                                        AutoSize = true,
                                        Margin = new Padding(0, 0, 0, 20)
                                    };
                                    coursesPanel.Controls.Add(currentCoursePanel);
                                }

                                if (disciplineCount > 0 && disciplineCount % 3 == 0)
                                {
                                    currentCoursePanel.Height += 80;
                                }

                                AddDisciplineCard(currentCoursePanel, disciplineName, disciplineId);
                                disciplineCount++;
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
        private async Task LoadTeacherInfo(int userId)
        {
            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    await conn.OpenAsync();
                    string sql = @"
                    SELECT u.firstname, u.lastname, u.middlename, u.email, t.id_teacher
                    FROM public.""user"" u
                    JOIN public.teachers t ON t.id_user = u.id_user
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
                                teacherId = reader.GetInt32(4);

                                var lblTeacherInfo = this.Controls.OfType<Panel>().First()
                                                       .Controls.OfType<Label>().First();
                                lblTeacherInfo.Text = $"{fullName}\n{email}";
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

        private async void LoadDisciplines()
        {
            try
            {
                if (teacherId == 0) // Проверка наличия teacherId
                {
                    MessageBox.Show("Ошибка: не удалось определить преподавателя");
                    return;
                }
                coursesPanel.Controls.Clear();
                using (var conn = DatabaseConnection.GetConnection())
                {
                    await conn.OpenAsync();
                    string sql = @"
                    SELECT DISTINCT d.discipline, d.course, d.id_discipline
                    FROM public.disciplines d
                    JOIN public.teachers_of_disciplines tod ON d.id_discipline = tod.id_discipline
                    JOIN public.teachers t ON tod.id_teacher = t.id_teacher
                    WHERE t.id_teacher = @teacherId";

                    // Добавляем фильтры
                    if (cmbCourses.SelectedIndex > 0)
                    {
                        sql += " AND d.course = @course";
                    }
                    if (!string.IsNullOrEmpty(txtSearch.Text))
                    {
                        sql += " AND d.discipline ILIKE @search";
                    }

                    sql += " ORDER BY d.course, " + (isAscendingSort ? "d.discipline ASC" : "d.discipline DESC");

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("teacherId", teacherId);
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
                            int currentCourse = 0;
                            FlowLayoutPanel currentCoursePanel = null;
                            int disciplineCount = 0;

                            while (await reader.ReadAsync())
                            {
                                string disciplineName = reader.GetString(0);
                                int course = reader.GetInt32(1);
                                int disciplineId = reader.GetInt32(2);

                                // Если новый курс или первая дисциплина
                                if (currentCourse != course)
                                {
                                    currentCourse = course;
                                    disciplineCount = 0;

                                    // Добавляем заголовок курса
                                    Label lblCourse = new Label
                                    {
                                        Text = $"{course} курс",
                                        Font = new Font("Arial", 14, FontStyle.Bold),
                                        AutoSize = true,
                                        Margin = new Padding(0, 20, 0, 10)
                                    };
                                    coursesPanel.Controls.Add(lblCourse);

                                    // Создаем новую панель для дисциплин текущего курса
                                    currentCoursePanel = new FlowLayoutPanel
                                    {
                                        Width = 960,
                                        Height = 100,
                                        AutoSize = true,
                                        Margin = new Padding(0, 0, 0, 20)
                                    };
                                    coursesPanel.Controls.Add(currentCoursePanel);
                                }

                                // Если достигли трех дисциплин в ряду
                                if (disciplineCount > 0 && disciplineCount % 3 == 0)
                                {
                                    currentCoursePanel.Height += 80;
                                }

                                // Добавляем карточку дисциплины
                                AddDisciplineCard(currentCoursePanel, disciplineName, disciplineId);
                                disciplineCount++;
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

        private void AddDisciplineCard(FlowLayoutPanel panel, string disciplineName, int disciplineId)
        {
            Panel card = new Panel
            {
                Size = new Size(300, 80),
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 10),
                Cursor = Cursors.Hand,
                BorderStyle = BorderStyle.FixedSingle
            };


            // Первая буква дисциплины
            Label lblIcon = new Label
            {
                Text = disciplineName.Trim().ToUpper()[0].ToString(),
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Size = new Size(40, 40),
                Location = new Point(10, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(45, 50, 80),
                ForeColor = Color.White
            };
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = GetRoundedRectPath(card.ClientRectangle, 8))
                {
                    e.Graphics.FillPath(Brushes.White, path);
                    e.Graphics.DrawPath(new Pen(Color.FromArgb(200, 200, 200)), path);
                }
            };
            // Название дисциплины
            Label lblName = new Label
            {
                Text = disciplineName,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(60, 5),
                Size = new Size(200, 80)
            };

            // Стрелка
            Label lblArrow = new Label
            {
                Text = "→",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(260, 25),
                Size = new Size(30, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            card.Controls.AddRange(new Control[] { lblIcon, lblName, lblArrow });

            // Обработчик клика
            card.Click += (sender, e) => DisciplineCard_Click(disciplineId, disciplineName);
            foreach (Control control in card.Controls)
            {
                control.Click += (sender, e) => DisciplineCard_Click(disciplineId, disciplineName);
            }

            panel.Controls.Add(card);
        }
        
        private GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();
            return path;
        }
        private void DisciplineCard_Click(int disciplineId, string disciplineName)
        {
            // Здесь будет открытие формы выбора группы
            //MessageBox.Show($"Открытие формы выбора группы для дисциплины: {disciplineName}");
            this.Hide();
            var groupSelectForm = new GroupSelectForm(teacherId, disciplineId, disciplineName, this);
            groupSelectForm.ShowDialog();
            //this.Close();
        }
        private async void CmbCourses_SelectedIndexChanged(object sender, EventArgs e)
        {
            await LoadDisciplinesAsync();
        }

        private async void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            await LoadDisciplinesAsync();
        }

        private async void BtnSort_Click(object sender, EventArgs e)
        {
            isAscendingSort = !isAscendingSort;
            await LoadDisciplinesAsync();
        }

        private void TeacherForm_Load(object sender, EventArgs e)
        {

        }
    }
}
