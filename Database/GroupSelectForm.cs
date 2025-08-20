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
using System.Drawing.Drawing2D;

namespace WorkSheetApplication.Database
{
    public partial class GroupSelectForm : Form
    {
        private int teacherId;
        private int disciplineId;
        private string disciplineName;
        private Button btnBack;
        private Panel formulaPanel;
        private string gradeFormula = "";  // Будет хранить формулу оценки

        private FlowLayoutPanel groupsPanel;
        private TeacherForm _previousForm; // Ссылка на предыдущую форму

        public GroupSelectForm(int teacherId, int disciplineId, string disciplineName, TeacherForm previousForm)
        {
            _previousForm = previousForm;
            this.teacherId = teacherId;
            this.disciplineId = disciplineId;
            this.disciplineName = disciplineName;
            InitializeComponent();
            InitializeGroupSelectForm();
            LoadTeacherInfo();
            LoadDisciplineInfo();
            LoadGroups();
            LoadFormulaDisplay(); 
        }

        private void InitializeGroupSelectForm()
        {
            // Настройка формы
            this.Text = "Выбор группы";
            this.Size = new Size(1000, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            //this.BackColor = Color.White;

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

            // Информация о преподавателе (будет заполнена в LoadTeacherInfo)
            Label lblTeacherInfo = new Label
            {
                AutoSize = true,
                Location = new Point(730, 20),
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight
            };

            // Информация о дисциплине (будет заполнена в LoadDisciplineInfo)
            Label lblCourseInfo = new Label
            {
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 50, 80),
                BackColor = Color.Transparent,
                AutoSize = false,
                MaximumSize = new Size(100, 0),
                Size = new Size(100, 50),


                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point((this.Width - 90) / 2, 80),
            };
            this.SizeChanged += (sender, e) =>
            {
                lblCourseInfo.Left = (this.ClientSize.Width - lblCourseInfo.Width) / 2;
            };
            Label lblDisciplineInfo = new Label
            {
                Text = $"{disciplineName}",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 50, 90),
                AutoSize = false,          // Отключаем авторазмер
                //WordWrap = true,           // Включаем перенос слов
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
            // Панель формулы оценки
            Panel formulaSection = new Panel
            {
                Location = new Point((this.Width - 510) / 2, 200),
                Size = new Size(500, 80),
                BackColor = Color.WhiteSmoke
            };

            Label lblFormulaTitle = new Label
            {
                Text = "Формула оценки",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(210, 10)
            };

            PictureBox editIcon = new PictureBox
            {
                Size = new Size(20, 20),
                Location = new Point(460, 10),
                Cursor = Cursors.Hand,
                SizeMode = PictureBoxSizeMode.Zoom,  // Добавляем это свойство
                Image = WorkSheetApplication.Properties.Resources.edit_icon  // Указываем полное имя
            };
            editIcon.Click += EditFormula_Click;

            formulaPanel = new Panel
            {
                Location = new Point(10, 40),
                Size = new Size(480, 30),
                BackColor = Color.White
            };

            formulaSection.Controls.AddRange(new Control[] { lblFormulaTitle, editIcon, formulaPanel });

            // Добавление элементов на форму
            this.Controls.AddRange(new Control[]
            {
            btnBack,
            lblTeacherInfo,
            lblCourseInfo,
            lblDisciplineInfo,
            formulaSection
            });

            groupsPanel = new FlowLayoutPanel
            {
                Location = new Point((this.Width - 490) / 2, 320),
                Size = new Size(500, 250),
                AutoScroll = true,
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            // Добавляем groupsPanel в Controls
            this.Controls.AddRange(new Control[]
            {
            btnBack,
            lblTeacherInfo,
            lblCourseInfo,
            lblDisciplineInfo,
            formulaSection,
            groupsPanel
            });
        }

        private async void LoadTeacherInfo()
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
                    WHERE t.id_teacher = @teacherId";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("teacherId", teacherId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string fullName = $"{reader.GetString(1)} {reader.GetString(0)} {reader.GetString(2)}";
                                string email = reader.GetString(3);

                                var lblTeacherInfo = this.Controls.OfType<Label>().FirstOrDefault();
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
        private async void LoadGroups()
        {
            try
            {
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

                    // Затем получаем группы
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
        private async void LoadDisciplineInfo()
        {
            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    await conn.OpenAsync();
                    string sql = @"
                    SELECT d.course
                    FROM public.disciplines d
                    WHERE d.id_discipline = @disciplineId";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("disciplineId", disciplineId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                int course = reader.GetInt32(0);

                                var lblCourseInfo = this.Controls.OfType<Label>().ElementAtOrDefault(1);
                                if (lblCourseInfo != null)
                                {
                                    lblCourseInfo.Text = $"{course} курс";
                                }

                                var lblDisciplineInfo = this.Controls.OfType<Label>().ElementAtOrDefault(2);
                                if (lblDisciplineInfo != null)
                                {
                                    lblDisciplineInfo.Text = disciplineName;
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
        private void EditFormula_Click(object sender, EventArgs e)
        {
            var formulaForm = new FormulaEditForm(disciplineId);
            if (formulaForm.ShowDialog() == DialogResult.OK)
            {
                // Обновляем отображение формулы если нужно
                LoadFormulaDisplay();
            }
        }
        private void AddGroupPanel(int groupId, string groupName)
        {
            // Создаем панель для группы
            Panel groupPanel = new Panel
            {
                Size = new Size(480, 60),
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 10),
                Cursor = Cursors.Hand,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Создаем круглую иконку с первой буквой
            Label lblIcon = new Label
            {
                Text = groupName[0].ToString().ToUpper(),
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Size = new Size(40, 40),
                Location = new Point(10, 10),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(45, 50, 80),
                ForeColor = Color.White
            };
            groupPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = GetRoundedRectPath(groupPanel.ClientRectangle, 8))
                {
                    e.Graphics.FillPath(Brushes.White, path);
                    e.Graphics.DrawPath(new Pen(Color.FromArgb(200, 200, 200)), path);
                }
            };

            // Делаем иконку круглой
            lblIcon.Paint += (sender, e) =>
            {
                using (var gp = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    gp.AddEllipse(0, 0, lblIcon.Width - 1, lblIcon.Height - 1);
                    lblIcon.Region = new Region(gp);
                }
            };

            // Название группы
            Label lblGroupName = new Label
            {
                Text = groupName,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(60, 20),
                AutoSize = true,
                BackColor = Color.Transparent
                
            };

            // Стрелка "Перейти"
            Label lblArrow = new Label
            {
                Text = "Перейти →",
                Font = new Font("Segoe UI", 10),
                Location = new Point(380, 20),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            // Добавляем элементы на панель группы
            groupPanel.Controls.AddRange(new Control[] { lblIcon, lblGroupName, lblArrow });

            // Добавляем обработчик клика для всей панели
            groupPanel.Click += (sender, e) => GroupPanel_Click(teacherId, disciplineId, groupId, disciplineName, groupName);
            foreach (Control control in groupPanel.Controls)
            {
                control.Click += (sender, e) => GroupPanel_Click(teacherId, disciplineId, groupId, disciplineName, groupName);
            }

            // Добавляем эффект при наведении
            groupPanel.MouseEnter += (sender, e) =>
            {
                groupPanel.BackColor = Color.LightGray;
            };
            groupPanel.MouseLeave += (sender, e) =>
            {
                groupPanel.BackColor = Color.Gainsboro;
            };

            // Добавляем панель группы в основную панель
            groupsPanel.Controls.Add(groupPanel);
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
        private void GroupPanel_Click(int teacherId, int disciplineId, int groupId, string disciplineName, string groupName)
        {
          
            // После реализации формы с оценками:
            var gradesForm = new GradesForm(teacherId, disciplineId, groupId, disciplineName, groupName,this);
            this.Hide();
            gradesForm.ShowDialog();
            
        }
        private async void LoadFormulaDisplay()
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
                                Font = new Font("Arial", 10)
                            };

                            formulaPanel.Controls.Add(lblFormula);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке формулы: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}
