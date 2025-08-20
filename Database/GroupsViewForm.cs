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
    public partial class GroupsViewForm : Form
    {
        private int assistantId;
        private int disciplineId;
        private string disciplineName;
        private FlowLayoutPanel groupsPanel;
        private Panel formulaPanel;
        private AllDisciplinesForm _previousForm;
        public GroupsViewForm(int assistantId, int disciplineId, string disciplineName, AllDisciplinesForm previousForm)
        {
            this.assistantId = assistantId;
            this.disciplineId = disciplineId;
            this.disciplineName = disciplineName;
            InitializeComponent();
            InitializeGroupsViewForm();
            LoadData();
            _previousForm = previousForm;
        }

        private void InitializeGroupsViewForm()
        {
            // Настройка формы
            this.Text = "Просмотр групп";
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

            // Информация о дисциплине и преподавателе (будет заполнена позже)
            Label lblCourseInfo = new Label
            {
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 50, 80),
                BackColor = Color.Transparent,
                AutoSize = false,
                MaximumSize = new Size(100, 0),
                Size = new Size(100, 50),
                
                
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point((this.Width - 90) / 2, 60),
            };
            this.SizeChanged += (sender, e) =>
            {
                lblCourseInfo.Left = (this.ClientSize.Width - lblCourseInfo.Width) / 2;
            };

            
            Label lblDisciplineInfo = new Label
            {
                Text = disciplineName,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 50, 90),
                AutoSize = false,          // Отключаем авторазмер
                //WordWrap = true,           // Включаем перенос слов
                MaximumSize = new Size(600, 0), // Макс. ширина (настройте под вашу форму)
                Size = new Size(600, 50),  // Фиксированная ширина + начальная высота
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point((this.ClientSize.Width - 590) / 2, 90) // Центр по горизонтали
            };
            this.SizeChanged += (sender, e) =>
            {
                lblDisciplineInfo.Left = (this.ClientSize.Width - lblDisciplineInfo.Width) / 2;
            };
            Label lblTeacherInfo = new Label
            {
                AutoSize = true,
                Location = new Point(380, 140),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Панель формулы оценки
            Label lblFormulaTitle = new Label
            {
                Text = "Формула оценки",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(420, 220),
                BackColor = Color.Transparent,
                AutoSize = true
            };

            formulaPanel = new Panel
            {
                Location = new Point(250, 250),
                Size = new Size(480, 30),
                BackColor = Color.White
            };

            // Панель для групп
            groupsPanel = new FlowLayoutPanel
            {
                Location = new Point((this.Width - 510) / 2, 320),
                Size = new Size(500, 250),
                BackColor = Color.Transparent,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

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
            groupsPanel
            });
        }

        private async void LoadData()
        {
            await LoadAssistantInfo();
            await LoadDisciplineInfo();
            await LoadFormula();
            await LoadGroups();
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

        private async Task LoadDisciplineInfo()
        {
            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    await conn.OpenAsync();
                    string sql = @"
                    SELECT d.course, u.lastname, u.firstname, u.middlename, u.email
                    FROM public.disciplines d
                    JOIN public.teachers_of_disciplines tod ON d.id_discipline = tod.id_discipline
                    JOIN public.teachers t ON tod.id_teacher = t.id_teacher
                    JOIN public.""user"" u ON t.id_user = u.id_user
                    WHERE d.id_discipline = @disciplineId";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("disciplineId", disciplineId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                int course = reader.GetInt32(0);
                                string teacherName = $"{reader.GetString(1)} {reader.GetString(2)} {reader.GetString(3)}";
                                string email = reader.GetString(4);

                                var lblCourseInfo = this.Controls.OfType<Label>()
                                    .FirstOrDefault(l => l.Location.X == (this.Width - 90) / 2 && l.Location.Y == 60);
                                if (lblCourseInfo != null)
                                {
                                    lblCourseInfo.Text = $"{course} курс";
                                }

                                var lblTeacherInfo = this.Controls.OfType<Label>()
                                    .FirstOrDefault(l => l.Location.X == 380 && l.Location.Y == 140);
                                if (lblTeacherInfo != null)
                                {
                                    lblTeacherInfo.Text = $"{teacherName}\n{email}";
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке информации о дисциплине: {ex.Message}");
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

        private async Task LoadGroups()
        {
            try
            {
                groupsPanel.Controls.Clear();

                using (var conn = DatabaseConnection.GetConnection())
                {
                    await conn.OpenAsync();

                    // Сначала получаем курс дисциплины
                    string sqlDiscipline = @"
                SELECT course 
                FROM public.disciplines 
                WHERE id_discipline = @disciplineId";

                    int disciplineCourse;
                    using (var cmdDiscipline = new NpgsqlCommand(sqlDiscipline, conn))
                    {
                        cmdDiscipline.Parameters.AddWithValue("disciplineId", disciplineId);
                        disciplineCourse = Convert.ToInt32(await cmdDiscipline.ExecuteScalarAsync());
                    }

                    string sql = @"
                    SELECT DISTINCT g.id_group, g.group
                    FROM public.groups g
                    JOIN public.enrollments e ON g.id_program = e.id_program
                    WHERE e.id_discipline = @disciplineId
                    ORDER BY g.group";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("disciplineId", disciplineId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                int groupId = reader.GetInt32(0);
                                string groupName = reader.GetString(1);
                                // Проверяем, соответствует ли группа курсу дисциплины
                                if (GetGroupCourse(groupName) == disciplineCourse)
                                {
                                    AddGroupPanel(groupId, groupName);
                                }
                                
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке групп: {ex.Message}");
            }
        }
        private int GetGroupCourse(string groupName)
        {
            try
            {
                if (string.IsNullOrEmpty(groupName))
                {
                    return 0;
                }

                // Разбиваем строку по тире и проверяем части
                string[] parts = groupName.Split('-');
                if (parts.Length < 3)
                {
                    return 0;
                }

                // Берем среднюю часть (22 из РИС-22-3)
                string yearStr = parts[1];

                if (!int.TryParse(yearStr, out int yearNumber))
                {
                    return 0;
                }

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
        private void AddGroupPanel(int groupId, string groupName)
        {
            Panel panel = new Panel
            {
                Size = new Size(500, 60),
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 10),
                Cursor = Cursors.Hand,
                BorderStyle = BorderStyle.FixedSingle

            };

            // Иконка группы
            Label lblIcon = new Label
            {
                Text = groupName[0].ToString(),
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Size = new Size(40, 40),
                Location = new Point(10, 10),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(45, 50, 80),
                ForeColor = Color.White
            };

            // Название группы
            Label lblName = new Label
            {
                Text = groupName,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(60, 20),
                AutoSize = true
            };

            // Кнопка "Перейти"
            Label lblArrow = new Label
            {
                Text = "Перейти →",
                Font = new Font("Segoe UI", 10),
                Location = new Point(405, 20),
                AutoSize = true
            };

            panel.Controls.AddRange(new Control[] { lblIcon, lblName, lblArrow });

            // Добавляем обработчики событий
            panel.Click += (s, e) => OpenGradesView(groupId, groupName);
            foreach (Control control in panel.Controls)
            {
                control.Click += (s, e) => OpenGradesView(groupId, groupName);
            }

            // Эффект при наведении
            panel.MouseEnter += (s, e) => panel.BackColor = Color.FromArgb(230, 230, 230);
            panel.MouseLeave += (s, e) => panel.BackColor = Color.WhiteSmoke;

            groupsPanel.Controls.Add(panel);
        }

        private void OpenGradesView(int groupId, string groupName)
        {
            var gradesViewForm = new GradesViewForm(assistantId, disciplineId, groupId, disciplineName, groupName,this);
            this.Hide();
            gradesViewForm.FormClosed += (s, args) => this.Close();
            gradesViewForm.ShowDialog();
        }
    }
}
