using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Threading.Tasks;




namespace WorkSheetApplication.Database
{
    public partial class StatisticsViewForm : Form
    {
        private int assistantId;
        private int disciplineId;
        private string disciplineName;
        private ComboBox cmbProgram;
        private ComboBox cmbGroup;
        private ComboBox cmbFilter;
        private Chart chartGrades;
        private Chart chartAttendance;
        private SelectStatisticsForm _previousForm;
        public StatisticsViewForm(int assistantId, int disciplineId, string disciplineName, SelectStatisticsForm previousForm)
        {
            this.assistantId = assistantId;
            this.disciplineId = disciplineId;
            this.disciplineName = disciplineName;
            InitializeComponent();
            InitializeStatisticsForm();
            LoadData();
            _previousForm = previousForm;
        }

        private void InitializeStatisticsForm()
        {
            // Настройка формы
            this.Text = "Статистика дисциплины";
            this.Size = new Size(1200, 800);
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
                Location = new Point(930, 20),
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight
            };

            // Заголовок
            Label lblTitle = new Label
            {
                Text = $"Статистика (дисциплина)\n{disciplineName}",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 50, 80),
                
                AutoSize = false,          // Отключаем авторазмер
                //WordWrap = true,           // Включаем перенос слов
                MaximumSize = new Size(600, 0),
                Size = new Size(600, 60),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point((this.ClientSize.Width - 590) / 2, 60),
            };
            
            this.SizeChanged += (sender, e) =>
            {
                lblTitle.Left = (this.ClientSize.Width - lblTitle.Width) / 2;
            };

            // Панель фильтров
            Panel filterPanel = new Panel
            {
                Location = new Point(400, 165),
                BackColor = Color.Transparent,
                Size = new Size(500, 40)
            };

            // Выбор ОП
            cmbProgram = new ComboBox
            {
                Items = { "Все ОП", "РИС", "МБ", "Ю", "ИЯ" },
                SelectedIndex = 0,
                Location = new Point(0, 5),
                Size = new Size(120, 40),
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(64, 64, 64),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
            };
            cmbProgram.SelectedIndexChanged += ApplyFilters;

            // Выбор группы
            cmbGroup = new ComboBox
            {
                Items = { "Все группы" },
                SelectedIndex = 0,
                Location = new Point(150, 5),
                Size = new Size(120, 40),
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(64, 64, 64),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
            };
            cmbGroup.SelectedIndexChanged += ApplyFilters;

            // Фильтр статистики
            cmbFilter = new ComboBox
            {
                Items = { "Все оценки", "Только сдавшие", "Только несдавшие" },
                SelectedIndex = 0,
                Location = new Point(300, 5),
                Size = new Size(150, 40),
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(64, 64, 64),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
            };
            cmbFilter.SelectedIndexChanged += ApplyFilters;

            filterPanel.Controls.AddRange(new Control[] { cmbProgram, cmbGroup, cmbFilter });

            // График оценок
            chartGrades = new Chart
            {
                Location = new Point(20, 220),
                Size = new Size(560, 300)
            };
            ChartArea gradesArea = new ChartArea("Grades");
            chartGrades.ChartAreas.Add(gradesArea);
            chartGrades.Titles.Add("Распределение оценок");

            // График посещаемости
            chartAttendance = new Chart
            {
                Location = new Point(600, 220),
                Size = new Size(560, 300)
            };
            ChartArea attendanceArea = new ChartArea("Attendance");
            attendanceArea.AxisX.LabelStyle.Format = "dd.MM.yy";
            attendanceArea.AxisX.IntervalType = DateTimeIntervalType.Days;
            chartAttendance.ChartAreas.Add(attendanceArea);
            chartAttendance.Titles.Add("Посещаемость по датам");

            this.Controls.AddRange(new Control[]
            {
                btnBack,
                lblAssistantInfo,
                lblTitle,
                filterPanel,
                chartGrades,
                chartAttendance
            });
        }

        private async void LoadData()
        {
            await LoadAssistantInfo();
            await LoadGroups();
            await UpdateStatistics();
        }

        private async Task LoadGroups()
        {
            try
            {
                if (cmbProgram.SelectedIndex > 0)
                {
                    using (var conn = DatabaseConnection.GetConnection())
                    {
                        await conn.OpenAsync();
                        string sql = @"
                        SELECT DISTINCT g.group
                        FROM public.groups g
                        JOIN public.enrollments e ON g.id_program = e.id_program
                        WHERE e.id_discipline = @disciplineId
                        AND g.group LIKE @prefix || '%'
                        ORDER BY g.group";

                        using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("disciplineId", disciplineId);
                            cmd.Parameters.AddWithValue("prefix", cmbProgram.SelectedItem.ToString());

                            cmbGroup.Items.Clear();
                            cmbGroup.Items.Add("Все группы");

                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    cmbGroup.Items.Add(reader.GetString(0));
                                }
                            }
                            cmbGroup.SelectedIndex = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке групп: {ex.Message}");
            }
        }

        private async Task UpdateStatistics()
        {
            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    await conn.OpenAsync();

                    // Получаем данные для графика оценок
                    string gradesSql = @"
                    SELECT fg.final_grade, COUNT(*) as count
                    FROM public.final_grade fg
                    JOIN public.students s ON fg.id_student = s.id_student
                    JOIN public.groups g ON s.id_group = g.id_group
                    WHERE fg.id_discipline = @disciplineId";

                    if (cmbProgram.SelectedIndex > 0)
                    {
                        gradesSql += " AND g.group LIKE @prefix || '%'";
                    }
                    if (cmbGroup.SelectedIndex > 0)
                    {
                        gradesSql += " AND g.group = @group";
                    }
                    // Фильтр по статусу сдачи
                    if (cmbFilter.SelectedIndex == 1) // "Только сдавшие"
                    {
                        gradesSql += " AND fg.final_grade >= 4";
                    }
                    else if (cmbFilter.SelectedIndex == 2) // "Только несдавшие"
                    {
                        gradesSql += " AND fg.final_grade < 4";
                    }
                    gradesSql += " GROUP BY fg.final_grade ORDER BY fg.final_grade";

                    Dictionary<int, int> gradeDistribution = new Dictionary<int, int>();
                    using (var cmd = new NpgsqlCommand(gradesSql, conn))
                    {
                        cmd.Parameters.AddWithValue("disciplineId", disciplineId);
                        if (cmbProgram.SelectedIndex > 0)
                            cmd.Parameters.AddWithValue("prefix", cmbProgram.SelectedItem.ToString());
                        if (cmbGroup.SelectedIndex > 0)
                            cmd.Parameters.AddWithValue("group", cmbGroup.SelectedItem.ToString());

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                int grade = reader.GetInt32(0);
                                int count = reader.GetInt32(1);
                                gradeDistribution[grade] = count;
                            }
                        }
                    }

                    // Получаем данные посещаемости
                    string attendanceSql = @"
                    SELECT a.date, 
                           COUNT(*) FILTER (WHERE a.status = true) as present,
                           COUNT(*) FILTER (WHERE a.status = false) as absent
                    FROM public.attendance a
                    JOIN public.students s ON a.id_student = s.id_student
                    JOIN public.groups g ON s.id_group = g.id_group
                    WHERE a.id_discipline = @disciplineId";

                    if (cmbProgram.SelectedIndex > 0)
                    {
                        attendanceSql += " AND g.group LIKE @prefix || '%'";
                    }
                    if (cmbGroup.SelectedIndex > 0)
                    {
                        attendanceSql += " AND g.group = @group";
                    }

                    attendanceSql += " GROUP BY a.date ORDER BY a.date";

                    List<DateTime> dates = new List<DateTime>();
                    List<double> presentPercentages = new List<double>();

                    using (var cmd = new NpgsqlCommand(attendanceSql, conn))
                    {
                        cmd.Parameters.AddWithValue("disciplineId", disciplineId);
                        if (cmbProgram.SelectedIndex > 0)
                            cmd.Parameters.AddWithValue("prefix", cmbProgram.SelectedItem.ToString());
                        if (cmbGroup.SelectedIndex > 0)
                            cmd.Parameters.AddWithValue("group", cmbGroup.SelectedItem.ToString());

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                DateTime date = reader.GetDateTime(0);
                                int present = reader.GetInt32(1);
                                int absent = reader.GetInt32(2);
                                double percentage = (double)present / (present + absent) * 100;

                                dates.Add(date);
                                presentPercentages.Add(percentage);
                            }
                        }
                    }

                    // Обновляем график оценок
                    chartGrades.Series.Clear();
                    if (gradeDistribution.Any())
                    {
                        Series gradeSeries = new Series("Оценки");
                        gradeSeries.ChartType = SeriesChartType.Column;
                        gradeSeries.IsValueShownAsLabel = true;

                        foreach (var pair in gradeDistribution.OrderBy(x => x.Key))
                        {
                            gradeSeries.Points.AddXY(pair.Key, pair.Value);
                        }

                        chartGrades.Series.Add(gradeSeries);
                        chartGrades.ChartAreas[0].AxisX.Title = "Оценка";
                        chartGrades.ChartAreas[0].AxisY.Title = "Количество студентов";
                    }

                    chartAttendance.Series.Clear();
                    if (dates.Any())
                    {
                        Series attendanceSeries = new Series("Посещаемость");
                        attendanceSeries.ChartType = SeriesChartType.Line;
                        attendanceSeries.BorderWidth = 2;

                        for (int i = 0; i < dates.Count; i++)
                        {
                            attendanceSeries.Points.AddXY(dates[i], presentPercentages[i]);
                        }

                        chartAttendance.Series.Add(attendanceSeries);
                        chartAttendance.ChartAreas[0].AxisX.Title = "Дата";
                        chartAttendance.ChartAreas[0].AxisY.Title = "Процент присутствующих";
                        chartAttendance.ChartAreas[0].AxisY.Maximum = 100;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении статистики: {ex.Message}");
            }
        }

        private void ApplyFilters(object sender, EventArgs e)
        {
            if (sender == cmbProgram)
            {
                LoadGroups();
            }
            UpdateStatistics();
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
                                    .FirstOrDefault(l => l.Location.X == 930 && l.Location.Y == 20);
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
    }
}
