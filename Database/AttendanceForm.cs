using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Drawing.Drawing2D;

namespace WorkSheetApplication.Database
{
    public partial class AttendanceForm : Form
    {
        private int teacherId;
        private int disciplineId;
        private int groupId;
        private string disciplineName;
        private string groupName;
        private Button btnBack;
        private DataGridView attendanceGrid;
        private Button btnSave;
        private LinkLabel lnkGrades;
        private LinkLabel lnkAttendance;
        private GradesForm _previousForm;
        private GroupSelectForm _groupSelectForm;

        public AttendanceForm(int teacherId, int disciplineId, int groupId, string disciplineName, string groupName, GradesForm previousForm, GroupSelectForm groupSelectForm)
        {
            this.teacherId = teacherId;
            this.disciplineId = disciplineId;
            this.groupId = groupId;
            this.disciplineName = disciplineName;
            this.groupName = groupName;

            InitializeComponent();
            InitializeAttendanceForm();
            LoadData();
            _previousForm = previousForm;
            _groupSelectForm = groupSelectForm;
        }

        private void InitializeAttendanceForm()
        {
            // Настройка формы
            this.Text = "Посещаемость";
            this.Size = new Size(1020, 800);
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

            // Информация о преподавателе (будет заполнена в LoadTeacherInfo)
            Label lblTeacherInfo = new Label
            {
                AutoSize = true,
                Location = new Point(750, 20),
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight
            };

            // Заголовок с информацией о группе и дисциплине
            Label lblHeader = new Label
            {
                Text = $"{groupName}\n{disciplineName}",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 50, 80),
                BackColor = Color.Transparent,
                AutoSize = false,          // Отключаем авторазмер
                //WordWrap = true,           // Включаем перенос слов
                MaximumSize = new Size(600, 0), // Макс. ширина (настройте под вашу форму)
                Size = new Size(600, 60),
                Location = new Point((this.ClientSize.Width - 590) / 2, 60),
                //Location = new Point(400, 60),
                TextAlign = ContentAlignment.MiddleCenter

            };

            this.SizeChanged += (sender, e) =>
            {
                lblHeader.Left = (this.ClientSize.Width - lblHeader.Width) / 2;
            };

            // Таблица посещаемости
            attendanceGrid = new DataGridView
            {
                Location = new Point(20, 200),
                Size = new Size(960, 500),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                BackgroundColor = Color.White
            };

            // Добавляем элементы на форму
            this.Controls.AddRange(new Control[]
            {
            btnBack,
            lblTeacherInfo,
            lblHeader,
            attendanceGrid
            });

            LoadTeacherInfo();
            // Добавляем переключатели Оценки/Посещаемость
            Panel navPanel = new Panel
            {
                Location = new Point(20, 160),
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
                LinkColor = Color.Black
                
                //Cursor = Cursor.Hand()
            };
            lnkGrades.Click += LnkGrades_Click;

            lnkAttendance = new LinkLabel
            {
                Text = "Посещаемость",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(80, 5),
                AutoSize = true,
                LinkBehavior = LinkBehavior.NeverUnderline,
                LinkColor = Color.Black,
                Enabled = false
            };

            navPanel.Controls.AddRange(new Control[] { lnkGrades, lnkAttendance });

            // Кнопка "Сохранить"
            btnSave = new Button
            {
                Text = "Сохранить",
                
                Location = new Point(860, 150),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(0, 123, 255), // Синий цвет
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 12),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Region = CreateRoundedRegion(btnSave.ClientRectangle, 10);
            btnSave.Click += BtnSave_Click;

            // Обновляем положение таблицы
            attendanceGrid.Location = new Point(20, 200);

            // Добавляем новые элементы управления на форму
            this.Controls.AddRange(new Control[] { navPanel, btnSave });

            // Настраиваем таблицу для работы с чекбоксами
            attendanceGrid.CellContentClick += AttendanceGrid_CellContentClick;
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

                                var lblTeacherInfo = this.Controls.OfType<Label>()
                                    .FirstOrDefault(l => l.Location.X == 750 && l.Location.Y == 20);
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

        private async void LoadData()
        {
            try
            {
                // Получаем даты проведения элементов контроля
                Dictionary<DateTime, int> dateElements = new Dictionary<DateTime, int>();
                using (var conn = DatabaseConnection.GetConnection())
                {
                    await conn.OpenAsync();
                    string sql = @"
                SELECT DISTINCT element_date, id_element
                FROM public.control_element
                WHERE id_discipline = @disciplineId
                AND element_date IS NOT NULL
                ORDER BY element_date";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("disciplineId", disciplineId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                if (!reader.IsDBNull(0))
                                {
                                    DateTime date = reader.GetDateTime(0);
                                    int elementId = reader.GetInt32(1);
                                    if (!dateElements.ContainsKey(date))
                                    {
                                        dateElements.Add(date, elementId);
                                    }
                                }
                            }
                        }
                    }

                    // Настраиваем столбцы таблицы
                    attendanceGrid.Columns.Clear();
                    attendanceGrid.Columns.Add("FullName", "ФИО");
                    foreach (var date in dateElements.Keys)
                    {
                        var column = new DataGridViewCheckBoxColumn
                        {
                            Name = date.ToString("dd.MM.yy"),
                            HeaderText = date.ToString("dd.MM.yy"),
                            Tag = dateElements[date]
                        };
                        attendanceGrid.Columns.Add(column);
                    }

                    // Загружаем список студентов
                    sql = @"
                SELECT u.lastname, u.firstname, u.middlename, s.id_student
                FROM public.students s
                JOIN public.""user"" u ON s.id_user = u.id_user
                WHERE s.id_group = @groupId
                ORDER BY u.lastname, u.firstname, u.middlename";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("groupId", groupId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string lastName = reader.GetString(0);
                                string firstName = reader.GetString(1);
                                string middleName = reader.GetString(2);
                                int studentId = reader.GetInt32(3);

                                string fullName = $"{lastName} {firstName[0]}.{middleName[0]}.";
                                int rowIndex = attendanceGrid.Rows.Add();
                                var row = attendanceGrid.Rows[rowIndex];
                                row.Tag = studentId;
                                row.Cells["FullName"].Value = fullName;

                                // Устанавливаем серый цвет по умолчанию для всех ячеек посещаемости
                                for (int i = 1; i < attendanceGrid.Columns.Count; i++)
                                {
                                    row.Cells[i].Style.BackColor = Color.LightGray;
                                }
                            }
                        }
                    }

                    // Загружаем существующие отметки посещаемости
                    if (dateElements.Any())
                    {
                        sql = @"
                    SELECT a.id_student, a.date, a.status
                    FROM public.attendance a
                    WHERE a.id_discipline = @disciplineId
                    AND a.date IN (SELECT element_date FROM public.control_element 
                                 WHERE id_discipline = @disciplineId AND element_date IS NOT NULL)";

                        using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("disciplineId", disciplineId);
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    int studentId = reader.GetInt32(0);
                                    DateTime date = reader.GetDateTime(1);
                                    bool status = reader.GetBoolean(2);

                                    var row = attendanceGrid.Rows.Cast<DataGridViewRow>()
                                        .FirstOrDefault(r => (int)r.Tag == studentId);

                                    if (row != null)
                                    {
                                        string dateColumn = date.ToString("dd.MM.yy");
                                        if (attendanceGrid.Columns.Contains(dateColumn))
                                        {
                                            row.Cells[dateColumn].Value = status;
                                            row.Cells[dateColumn].Style.BackColor = status ?
                                                Color.LightGreen : Color.LightCoral;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void AttendanceGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex > 0)
            {
                var cell = attendanceGrid.Rows[e.RowIndex].Cells[e.ColumnIndex];
                if (cell.Value == null || !(bool)cell.Value)
                {
                    cell.Value = true;
                    cell.Style.BackColor = Color.LightGreen;
                }
                else
                {
                    cell.Value = false;
                    cell.Style.BackColor = Color.LightCoral;
                }
            }
        }

        private void UpdateCellColor(DataGridViewCell cell)
        {
            if (cell.Value != null)
            {
                cell.Style.BackColor = (bool)cell.Value ? Color.LightGreen : Color.LightCoral;
            }
            else
            {
                cell.Style.BackColor = Color.LightGray;
            }
        }

        private async void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    await conn.OpenAsync();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // Сначала удалим существующие записи для этой дисциплины
                            string deleteSql = @"
                        DELETE FROM public.attendance 
                        WHERE id_teacher = @teacherId 
                        AND id_discipline = @disciplineId";

                            using (var cmd = new NpgsqlCommand(deleteSql, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("teacherId", teacherId);
                                cmd.Parameters.AddWithValue("disciplineId", disciplineId);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            // Получаем следующий id_attend
                            string getMaxIdSql = "SELECT COALESCE(MAX(id_attend), 0) FROM public.attendance";
                            int nextAttendId;
                            using (var cmd = new NpgsqlCommand(getMaxIdSql, conn, transaction))
                            {
                                nextAttendId = Convert.ToInt32(await cmd.ExecuteScalarAsync()) + 1;
                            }

                            // Вставляем новые записи
                            string insertSql = @"
                        INSERT INTO public.attendance 
                        (id_attend, id_teacher, id_discipline, id_student, status, date)
                        VALUES (@id_attend, @teacherId, @disciplineId, @studentId, @status, @date)";

                            foreach (DataGridViewRow row in attendanceGrid.Rows)
                            {
                                int studentId = (int)row.Tag;

                                for (int i = 1; i < attendanceGrid.Columns.Count; i++)
                                {
                                    var cell = row.Cells[i];
                                    if (cell.Value != null) // Проверяем, что ячейка имеет значение
                                    {
                                        var column = attendanceGrid.Columns[i];
                                        bool status = (bool)cell.Value;
                                        DateTime date = DateTime.ParseExact(column.Name, "dd.MM.yy", null);

                                        using (var cmd = new NpgsqlCommand(insertSql, conn, transaction))
                                        {
                                            cmd.Parameters.AddWithValue("id_attend", nextAttendId++);
                                            cmd.Parameters.AddWithValue("teacherId", teacherId);
                                            cmd.Parameters.AddWithValue("disciplineId", disciplineId);
                                            cmd.Parameters.AddWithValue("studentId", studentId);
                                            cmd.Parameters.AddWithValue("status", status);
                                            cmd.Parameters.AddWithValue("date", date);
                                            await cmd.ExecuteNonQueryAsync();
                                        }
                                    }
                                }
                            }

                            transaction.Commit();
                            MessageBox.Show("Данные успешно сохранены!", "Успех",
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении данных: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LnkGrades_Click(object sender, EventArgs e)
        {
            var gradesForm = new GradesForm(teacherId, disciplineId, groupId, disciplineName, groupName, _groupSelectForm);
            this.Hide();
            gradesForm.FormClosed += (s, args) => this.Close();
            gradesForm.ShowDialog();
        }
    }
}
