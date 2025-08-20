using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace WorkSheetApplication.Database
{
    public partial class GradesForm : Form
    {
        private int teacherId;
        private int disciplineId;
        private int groupId;
        private ToolTip elementsTooltip;
        private string disciplineName;
        private string groupName;
        private Button btnBack;
        private Button btnSave;
        private Button btnExport;
        private Panel formulaPanel;
        private LinkLabel lnkGrades;
        private LinkLabel lnkAttendance;
        private DataGridView gradesGrid;
        private LinkLabel lnkApproveGrades;
        private Dictionary<string, decimal> formulaElements;
        private bool areGradesApproved = false;
        private GroupSelectForm _previousForm;

        public GradesForm(int teacherId, int disciplineId, int groupId, string disciplineName, string groupName, GroupSelectForm previousForm)
        {
            _previousForm = previousForm;
            this.teacherId = teacherId;
            this.disciplineId = disciplineId;
            this.groupId = groupId;
            this.disciplineName = disciplineName;
            this.groupName = groupName;

            InitializeComponent();
            InitializeGradesForm();

            // Запускаем асинхронную инициализацию данных
            _ = InitializeDataAsync();
            _previousForm = previousForm;
        }

        private async Task InitializeDataAsync()
        {
            try
            {
                await LoadTeacherInfo();
                await LoadFormulaDisplay();
                await LoadGradesData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при инициализации данных: {ex.Message}");
            }
        }
    
        private void InitializeGradesForm()
        {
            // Настройка формы
            this.Text = "Ведомость группы";
            this.Size = new Size(1020, 800);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Создаем DataGridView
            gradesGrid = new DataGridView
            {
                Location = new Point(20, 250),
                Size = new Size(800, 400),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                BackgroundColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                MultiSelect = false
            };

            // Теперь можно настроить стиль заголовков
            gradesGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Arial", 10, FontStyle.Underline);
            gradesGrid.ColumnHeaderMouseClick += GradesGrid_ColumnHeaderMouseClick;
            gradesGrid.CellValueChanged += GradesGrid_CellValueChanged;
            gradesGrid.CellValidating += GradesGrid_CellValidating;

            // Добавляем базовую колонку ФИО
            gradesGrid.Columns.Add("FullName", "ФИО");
            gradesGrid.Columns[0].ReadOnly = true;

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

            // Информация о преподавателе
            Label lblTeacherInfo = new Label
            {
                //Size = new Size(700,40),
                Location = new Point(750, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight
            };

            // Заголовок с информацией о курсе и дисциплине
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
            // Панель формулы
            Label lblFormulaTitle = new Label
            {
                Text = "Формула оценки",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(415, 150),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                AutoSize = true
            };

            formulaPanel = new Panel
            {
                Location = new Point(250, 180),
                Size = new Size(480, 30),
                BackColor = Color.White
            };

            // Кнопки сохранения и экспорта
            btnExport = new Button
            {
                Text = "Экспорт",
                Location = new Point(850, 580),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(0, 123, 255), // Синий цвет
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 12),
                Cursor = Cursors.Hand
            };
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.Region = CreateRoundedRegion(btnExport.ClientRectangle, 10);
            btnExport.Click += BtnExport_Click;

            btnSave = new Button
            {


                Text = "Сохранить",
                Location = new Point(850, 620),
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

            // Переключатели Оценки/Посещаемость
            Panel navPanel = new Panel
            {
                Location = new Point(20, 215),
                Size = new Size(200, 30),
                BackColor = Color.Transparent
            };

            lnkGrades = new LinkLabel
            {
                Text = "Оценки",
                Location = new Point(0, 5),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                LinkBehavior = LinkBehavior.NeverUnderline,
                LinkColor = Color.Black,
                Enabled = false
            };

            lnkAttendance = new LinkLabel
            {
                Text = "Посещаемость",
                Location = new Point(100, 5),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                LinkBehavior = LinkBehavior.NeverUnderline,
                LinkColor = Color.Gray
            };
            lnkAttendance.Click += LnkAttendance_Click;

            navPanel.Controls.AddRange(new Control[] { lnkGrades, lnkAttendance });
            // Добавляем ссылку для утверждения оценок
            lnkApproveGrades = new LinkLabel
            {
                Text = "Утвердить итоговые оценки",
                LinkColor = Color.FromArgb(102, 102, 102),
                ActiveLinkColor = Color.FromArgb(0, 123, 255),
                Font = new Font("Segoe UI", 8),
                Location = new Point(830, 250),
                BackColor = Color.Transparent,
                AutoSize = true,
                Enabled = false,
                Visible = false
            };
            lnkApproveGrades.Click += ApproveGrades_Click;
            // Добавляем все элементы на форму
            this.Controls.AddRange(new Control[]
            {
                btnBack,
                lblTeacherInfo,
                lblHeader,
                lblFormulaTitle,
                formulaPanel,
                btnExport,
                btnSave,
                navPanel,
                gradesGrid,
                lnkApproveGrades
            });
            elementsTooltip = new ToolTip
            {
                InitialDelay = 500,  // Задержка перед появлением подсказки (в миллисекундах)
                AutoPopDelay = 5000, // Время отображения подсказки
                ReshowDelay = 500    // Задержка перед повторным появлением
            };

            // Добавляем обработчик для отображения подсказок
            gradesGrid.CellMouseEnter += GradesGrid_CellMouseEnter;
            gradesGrid.CellMouseLeave += GradesGrid_CellMouseLeave;
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
        private async void GradesGrid_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                // Проверяем, что это заголовок столбца
                if (e.RowIndex >= 0 || e.ColumnIndex < 0)
                    return;

                var column = gradesGrid.Columns[e.ColumnIndex];

                // Проверяем, что это столбец элемента контроля
                if (!formulaElements.ContainsKey(column.Name))
                    return;

                // Получаем дату для элемента контроля
                using (var conn = DatabaseConnection.GetConnection())
                {
                    await conn.OpenAsync();
                    string sql = @"
                SELECT element_date 
                FROM public.control_element 
                WHERE id_discipline = @disciplineId 
                AND element = @elementName";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("disciplineId", disciplineId);
                        cmd.Parameters.AddWithValue("elementName", column.Name);

                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            DateTime date = Convert.ToDateTime(result);
                            // Показываем подсказку у заголовка столбца
                            Rectangle headerRect = gradesGrid.GetCellDisplayRectangle(e.ColumnIndex, -1, true);
                            var headerLocation = gradesGrid.Location;
                            headerLocation.Offset(headerRect.Location);

                            elementsTooltip.SetToolTip(gradesGrid, $"Дата: {date:dd.MM.yyyy}");
                        }
                        else
                        {
                            elementsTooltip.SetToolTip(gradesGrid, "Дата не задана");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но не показываем пользователю
                Console.WriteLine($"Ошибка при получении даты: {ex.Message}");
            }
        }
        private void GradesGrid_CellMouseLeave(object sender, DataGridViewCellEventArgs e)
        {
            elementsTooltip.RemoveAll();
        }

        private async Task LoadGradesData()
        {
            try
            {
                // Загружаем элементы формулы
                await LoadFormulaElements();

                // Добавляем колонки для элементов контроля
                foreach (var element in formulaElements)
                {
                    if (element.Key != "ЭКЗ")
                    {
                        var column = new DataGridViewTextBoxColumn
                        {
                            Name = element.Key,
                            HeaderText = element.Key,
                            DefaultCellStyle = new DataGridViewCellStyle
                            {
                                Format = "N2"
                            }
                        };
                        gradesGrid.Columns.Add(column);
                    }
                }

                // Добавляем специальные колонки
                gradesGrid.Columns.Add("Накоп", "Накоп");
                gradesGrid.Columns["Накоп"].ReadOnly = true;

                if (formulaElements.ContainsKey("ЭКЗ"))
                {
                    gradesGrid.Columns.Add("ЭКЗ", "Экзамен");
                }

                var итогColumn = new DataGridViewTextBoxColumn
                {
                    Name = "Итог",
                    HeaderText = "Итог",
                    DefaultCellStyle = new DataGridViewCellStyle
                    {
                        BackColor = Color.LightYellow
                    }
                };
                gradesGrid.Columns.Add(итогColumn);

                // Загружаем список студентов и их оценки
                using (var conn = DatabaseConnection.GetConnection())
                {
                    await conn.OpenAsync();
                    string sql = @"
                    SELECT 
                        u.lastname, 
                        u.firstname, 
                        u.middlename,
                        s.id_student,
                        fg.final_grade
                    FROM public.students s
                    JOIN public.""user"" u ON s.id_user = u.id_user
                    LEFT JOIN public.final_grade fg ON fg.id_student = s.id_student 
                        AND fg.id_discipline = @disciplineId
                    WHERE s.id_group = @groupId
                    ORDER BY u.lastname, u.firstname, u.middlename";


                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("groupId", groupId);
                        cmd.Parameters.AddWithValue("disciplineId", disciplineId);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string lastName = reader.GetString(0);
                                string firstName = reader.GetString(1);
                                string middleName = reader.GetString(2);
                                int studentId = reader.GetInt32(3);
                                string finalGradeStr = reader.IsDBNull(4) ? null : reader.GetInt32(4).ToString();

                                string fullName = $"{lastName} {firstName[0]}.{middleName[0]}.";
                                int rowIndex = gradesGrid.Rows.Add();
                                var row = gradesGrid.Rows[rowIndex];
                                row.Tag = studentId;
                                row.Cells["FullName"].Value = fullName;

                                if (!string.IsNullOrEmpty(finalGradeStr))
                                {
                                    row.Cells["Итог"].Value = finalGradeStr;
                                    row.Cells["Итог"].Style.BackColor = Color.LightGreen;
                                }
                            }
                        }
                    }

                    // Загружаем существующие оценки
                    await LoadExistingGrades();
                }

                // Добавляем обработчики событий
                gradesGrid.CellValueChanged += GradesGrid_CellValueChanged;
                gradesGrid.CellValidating += GradesGrid_CellValidating;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private async Task LoadFormulaElements()
        {
            formulaElements = new Dictionary<string, decimal>();
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
                        while (await reader.ReadAsync())
                        {
                            string element = reader.GetString(0);
                            decimal weight = reader.GetDecimal(1);
                            formulaElements[element] = weight;
                        }
                    }
                }
            }
        }
        private async Task LoadExistingGrades()
        {
            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    await conn.OpenAsync();
                    string sql = @"
                SELECT s.id_student, ce.element, g.grade
                FROM public.students s
                JOIN public.grades g ON s.id_student = g.id_student
                JOIN public.control_element ce ON g.id_element = ce.id_element
                WHERE ce.id_discipline = @disciplineId";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("disciplineId", disciplineId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                int studentId = reader.GetInt32(0);
                                string element = reader.GetString(1);
                                decimal grade = reader.GetDecimal(2);

                                var row = gradesGrid.Rows.Cast<DataGridViewRow>()
                                    .FirstOrDefault(r => (int)r.Tag == studentId);

                                if (row != null && gradesGrid.Columns.Contains(element))
                                {
                                    row.Cells[element].Value = grade;
                                }
                            }
                        }
                    }
                }

                // Пересчитываем все оценки
                foreach (DataGridViewRow row in gradesGrid.Rows)
                {
                    CalculateGrades(row);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке оценок: {ex.Message}");
            }
        }
        private void GradesGrid_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.ColumnIndex > 0 && e.ColumnIndex < gradesGrid.Columns.Count - 3) // Проверяем только оценки
            {
                if (!string.IsNullOrEmpty(e.FormattedValue.ToString()))
                {
                    if (!decimal.TryParse(e.FormattedValue.ToString(), out decimal grade) || grade < 0 || grade > 10)
                    {
                        e.Cancel = true;
                        MessageBox.Show("Оценка должна быть числом от 0 до 10", "Ошибка",
                                      MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }
        private void CalculateGrades(DataGridViewRow row)
        {
            decimal accumGrade = 0;
            decimal examGrade = 0;
            bool hasAllGrades = true;

            foreach (var element in formulaElements)
            {
                if (element.Key == "ЭКЗ")
                {
                    if (row.Cells["ЭКЗ"].Value != null && decimal.TryParse(row.Cells["ЭКЗ"].Value.ToString(), out decimal grade))
                    {
                        examGrade = grade * element.Value;
                    }
                    else
                    {
                        hasAllGrades = false;
                    }
                }
                else
                {
                    if (row.Cells[element.Key].Value != null && decimal.TryParse(row.Cells[element.Key].Value.ToString(), out decimal grade))
                    {
                        accumGrade += grade * element.Value;
                    }
                    else
                    {
                        hasAllGrades = false;
                    }
                }
            }

            row.Cells["Накоп"].Value = accumGrade;
            decimal finalGrade = accumGrade + examGrade;
            row.Cells["Итог"].Value = Math.Round(finalGrade);

            if (!areGradesApproved)
            {
                row.Cells["Итог"].Style.BackColor = Color.LightYellow;
            }
        }
        private void CheckAllGradesFilled()
        {
            if (lnkApproveGrades == null) return;

            bool allFilled = true;
            foreach (DataGridViewRow row in gradesGrid.Rows)
            {
                foreach (var element in formulaElements)
                {
                    if (row.Cells[element.Key].Value == null ||
                        string.IsNullOrEmpty(row.Cells[element.Key].Value.ToString()))
                    {
                        allFilled = false;
                        break;
                    }
                }
                if (!allFilled) break;
            }

            lnkApproveGrades.Enabled = allFilled;
            lnkApproveGrades.Visible = allFilled;
        }
        private async void ApproveGrades_Click(object sender, EventArgs e)
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
                            // Получаем следующий id_final
                            string getMaxIdSql = "SELECT COALESCE(MAX(id_final), 0) FROM public.final_grade";
                            int nextId;
                            using (var cmd = new NpgsqlCommand(getMaxIdSql, conn, transaction))
                            {
                                nextId = Convert.ToInt32(await cmd.ExecuteScalarAsync()) + 1;
                            }

                            foreach (DataGridViewRow row in gradesGrid.Rows)
                            {
                                if (row.Cells["Итог"].Value == null) continue;

                                int studentId = (int)row.Tag;
                                int finalGrade = Convert.ToInt32(row.Cells["Итог"].Value);

                                // Проверяем существование записи
                                string checkSql = @"
                            SELECT id_final 
                            FROM public.final_grade 
                            WHERE id_student = @studentId 
                            AND id_discipline = @disciplineId";

                                int? existingId = null;
                                using (var cmdCheck = new NpgsqlCommand(checkSql, conn, transaction))
                                {
                                    cmdCheck.Parameters.AddWithValue("studentId", studentId);
                                    cmdCheck.Parameters.AddWithValue("disciplineId", disciplineId);
                                    var result = await cmdCheck.ExecuteScalarAsync();
                                    if (result != DBNull.Value && result != null)
                                    {
                                        existingId = Convert.ToInt32(result);
                                    }
                                }

                                if (existingId.HasValue)
                                {
                                    // Обновляем существующую оценку
                                    string updateSql = @"
                                UPDATE public.final_grade 
                                SET final_grade = @finalGrade
                                WHERE id_final = @idFinal";

                                    using (var cmd = new NpgsqlCommand(updateSql, conn, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("idFinal", existingId.Value);
                                        cmd.Parameters.AddWithValue("finalGrade", finalGrade);
                                        await cmd.ExecuteNonQueryAsync();
                                    }
                                }
                                else
                                {
                                    // Вставляем новую оценку
                                    string insertSql = @"
                                INSERT INTO public.final_grade 
                                (id_final, id_student, id_discipline, final_grade)
                                VALUES (@idFinal, @studentId, @disciplineId, @finalGrade)";

                                    using (var cmd = new NpgsqlCommand(insertSql, conn, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("idFinal", nextId++);
                                        cmd.Parameters.AddWithValue("studentId", studentId);
                                        cmd.Parameters.AddWithValue("disciplineId", disciplineId);
                                        cmd.Parameters.AddWithValue("finalGrade", finalGrade);
                                        await cmd.ExecuteNonQueryAsync();
                                    }
                                }

                                row.Cells["Итог"].Style.BackColor = Color.LightGreen;
                            }

                            transaction.Commit();
                            areGradesApproved = true;
                            MessageBox.Show("Итоговые оценки утверждены!", "Успех",
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
                MessageBox.Show($"Ошибка при утверждении оценок: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void GradesGrid_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            var column = gradesGrid.Columns[e.ColumnIndex];
            // Проверяем, что это столбец с элементом контроля (не ФИО, не Накоп, не Итог)
            if (formulaElements.ContainsKey(column.Name))
            {
                var dateForm = new ElementDateForm(disciplineId, column.Name);
                dateForm.ShowDialog();
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
                            // Получаем id элементов контроля для текущей дисциплины
                            Dictionary<string, int> elementIds = new Dictionary<string, int>();
                            string getElementsSql = @"
                        SELECT id_element, element 
                        FROM public.control_element 
                        WHERE id_discipline = @disciplineId";

                            using (var cmd = new NpgsqlCommand(getElementsSql, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("disciplineId", disciplineId);
                                using (var reader = await cmd.ExecuteReaderAsync())
                                {
                                    while (await reader.ReadAsync())
                                    {
                                        elementIds[reader.GetString(1)] = reader.GetInt32(0);
                                    }
                                }
                            }

                            // Сохраняем оценки для каждого студента
                            foreach (DataGridViewRow row in gradesGrid.Rows)
                            {
                                int studentId = (int)row.Tag;

                                // Проходим по всем столбцам таблицы
                                foreach (DataGridViewColumn column in gradesGrid.Columns)
                                {
                                    // Пропускаем столбцы ФИО, Накоп и Итог
                                    if (column.Name == "ФИО" || column.Name == "Накоп" || column.Name == "Итог")
                                        continue;

                                    // Проверяем, есть ли значение в ячейке
                                    if (row.Cells[column.Name].Value != null &&
                                        decimal.TryParse(row.Cells[column.Name].Value.ToString(), out decimal grade))
                                    {
                                        // Получаем id элемента контроля
                                        if (!elementIds.ContainsKey(column.Name))
                                            continue;

                                        int elementId = elementIds[column.Name];

                                        // Проверяем существование оценки
                                        string checkSql = @"
                                    SELECT id_grade 
                                    FROM public.grades 
                                    WHERE id_student = @studentId 
                                    AND id_element = @elementId";

                                        int? existingGradeId = null;
                                        using (var cmdCheck = new NpgsqlCommand(checkSql, conn, transaction))
                                        {
                                            cmdCheck.Parameters.AddWithValue("studentId", studentId);
                                            cmdCheck.Parameters.AddWithValue("elementId", elementId);
                                            var result = await cmdCheck.ExecuteScalarAsync();
                                            if (result != DBNull.Value && result != null)
                                            {
                                                existingGradeId = Convert.ToInt32(result);
                                            }
                                        }

                                        if (existingGradeId.HasValue)
                                        {
                                            // Обновляем существующую оценку
                                            string updateSql = @"
                                        UPDATE public.grades 
                                        SET grade = @grade 
                                        WHERE id_grade = @gradeId";

                                            using (var cmd = new NpgsqlCommand(updateSql, conn, transaction))
                                            {
                                                cmd.Parameters.AddWithValue("grade", grade);
                                                cmd.Parameters.AddWithValue("gradeId", existingGradeId.Value);
                                                await cmd.ExecuteNonQueryAsync();
                                            }
                                        }
                                        else
                                        {
                                            // Получаем следующий id_grade
                                            string getMaxGradeIdSql = "SELECT COALESCE(MAX(id_grade), 0) FROM public.grades";
                                            int nextGradeId;
                                            using (var cmd = new NpgsqlCommand(getMaxGradeIdSql, conn, transaction))
                                            {
                                                nextGradeId = Convert.ToInt32(await cmd.ExecuteScalarAsync()) + 1;
                                            }

                                            // Вставляем новую оценку
                                            string insertSql = @"
                                        INSERT INTO public.grades 
                                        (id_grade, id_student, id_teacher, id_element, grade)
                                        VALUES (@gradeId, @studentId, @teacherId, @elementId, @grade)";

                                            using (var cmd = new NpgsqlCommand(insertSql, conn, transaction))
                                            {
                                                cmd.Parameters.AddWithValue("gradeId", nextGradeId);
                                                cmd.Parameters.AddWithValue("studentId", studentId);
                                                cmd.Parameters.AddWithValue("teacherId", teacherId);
                                                cmd.Parameters.AddWithValue("elementId", elementId);
                                                cmd.Parameters.AddWithValue("grade", grade);
                                                await cmd.ExecuteNonQueryAsync();
                                            }
                                        }
                                    }
                                }
                            }

                            transaction.Commit();
                            MessageBox.Show("Оценки успешно сохранены!", "Успех",
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

        private async Task<Dictionary<string, int>> GetElementIds(NpgsqlConnection conn, NpgsqlTransaction transaction)
        {
            Dictionary<string, int> elementIds = new Dictionary<string, int>();
            string sql = "SELECT id_element, element FROM public.control_element WHERE id_discipline = @disciplineId";

            using (var cmd = new NpgsqlCommand(sql, conn, transaction))
            {
                cmd.Parameters.AddWithValue("disciplineId", disciplineId);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        elementIds[reader.GetString(1)] = reader.GetInt32(0);
                    }
                }
            }
            return elementIds;
        }
        private void BtnExport_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx",
                    FileName = $"Ведомость_{disciplineName}_{groupName}_{DateTime.Now:yyyy-MM-dd}.xlsx"
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Ведомость");

                        worksheet.Cell(1, 1).Value = $"{disciplineName} - {groupName}";
                        worksheet.Range(1, 1, 1, gradesGrid.Columns.Count).Merge();
                        worksheet.Cell(1, 1).Style.Font.Bold = true;
                        worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        for (int i = 0; i < gradesGrid.Columns.Count; i++)
                        {
                            worksheet.Cell(2, i + 1).Value = gradesGrid.Columns[i].HeaderText;
                            worksheet.Cell(2, i + 1).Style.Font.Bold = true;
                        }

                        for (int i = 0; i < gradesGrid.Rows.Count; i++)
                        {
                            for (int j = 0; j < gradesGrid.Columns.Count; j++)
                            {
                                var value = gradesGrid.Rows[i].Cells[j].Value;
                                worksheet.Cell(i + 3, j + 1).Value = value?.ToString() ?? "";
                            }
                        }

                        worksheet.Columns().AdjustToContents();

                        workbook.SaveAs(saveDialog.FileName);

                        MessageBox.Show("Экспорт успешно завершен!", "Успех",
                                      MessageBoxButtons.OK, MessageBoxIcon.Information);

                        if (MessageBox.Show("Открыть созданный файл?", "Подтверждение",
                                          MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            Process.Start(new ProcessStartInfo(saveDialog.FileName) { UseShellExecute = true });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте данных: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        private async Task LoadFormulaDisplay()
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
                MessageBox.Show($"Ошибка при загрузке формулы: {ex.Message}");
            }
        }
        private void EditFormula_Click(object sender, EventArgs e)
        {
            var formulaForm = new FormulaEditForm(disciplineId);
            if (formulaForm.ShowDialog() == DialogResult.OK)
            {
                LoadFormulaDisplay();
            }
        }
        private void LnkAttendance_Click(object sender, EventArgs e)
        {
            var attendanceForm = new AttendanceForm(
                teacherId,
                disciplineId,
                groupId,
                disciplineName,
                groupName,
                this, 
                _previousForm
            );
            this.Hide();
            attendanceForm.FormClosed += (s, args) => this.Close();
            attendanceForm.ShowDialog();
            
        }
        private void GradesGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                var row = gradesGrid.Rows[e.RowIndex];
                CalculateGrades(row);
                CheckAllGradesFilled();
            }
        }
    }
}
