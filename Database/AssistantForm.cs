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
    public partial class AssistantForm : Form
    {
        private int assistantId;

        public AssistantForm(int userId)
        {
            this.assistantId = userId;
            InitializeComponent();
            InitializeAssistantForm();
            LoadAssistantInfo();
        }

        private void InitializeAssistantForm()
        {
            // Настройка формы
            this.Text = "Система ведения рабочих ведомостей";
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
                this.Hide();
                new LoginForm().ShowDialog();
                this.Close();
            };

            // Информация о сотруднике УО
            Label lblAssistantInfo = new Label
            {
                
                Location = new Point(750, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Создаем кнопки-плитки
            TableLayoutPanel tilesPanel = new TableLayoutPanel
            {
                Location = new Point(120, 240),
                Size = new Size(900, 300),
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(0),
                BackColor= Color.Transparent
                //CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
            };

            
            Panel retakePanel = CreateTilePanel("Пересдачи", Properties.Resources.retake_icon, () =>
            {
                var allDisciplinesForm = new AllDisciplinesForm(assistantId,this);
                this.Hide();
                allDisciplinesForm.FormClosed += (s, args) => this.Hide();
                allDisciplinesForm.ShowDialog();
            });

            // Кнопка "Отчет при отчислении"
            Panel reportPanel = CreateTilePanel("Отчет при отчислении", Properties.Resources.report_icon, () =>
            {
                var selectStudentForm = new SelectStudentForm(assistantId,this);
                this.Hide();
                selectStudentForm.FormClosed += (s, args) => this.Hide();
                selectStudentForm.ShowDialog();
            });

            // Кнопка "Статистика"
            Panel statisticsPanel = CreateTilePanel("Статистика", Properties.Resources.statistics_icon, () =>
            {
                var statisticsForm = new SelectStatisticsForm(assistantId,this);
                this.Hide();
                statisticsForm.FormClosed += (s, args) => this.Hide();
                statisticsForm.ShowDialog();
            });

            // Добавляем панели в таблицу
            tilesPanel.Controls.Add(retakePanel, 0, 0);
            tilesPanel.Controls.Add(reportPanel, 1, 0);
            tilesPanel.Controls.Add(statisticsPanel, 2, 0);

            // Добавляем все элементы на форму
            this.Controls.AddRange(new Control[]
            {
            btnBack,
            lblAssistantInfo,
            tilesPanel
            });
        }

        private Panel CreateTilePanel(string text, Image icon, Action onClick)
        {
            Panel panel = new Panel
            {
                Size = new Size(250, 250),
                BackColor = Color.WhiteSmoke,
                Cursor = Cursors.Hand
                
            };
            panel.Paint += (sender, e) =>
            {
                // Рисуем рамку
                using (Pen pen = new Pen(Color.FromArgb(45, 50, 80), 2)) // Цвет и толщина
                {
                    // Рисуем прямоугольник по границам панели (с учетом толщины пера)
                    Rectangle rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
                    e.Graphics.DrawRectangle(pen, rect);
                }
            };
            PictureBox iconBox = new PictureBox
            {
                Image = icon,
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(150, 150),
                Location = new Point(50, 30)
            };

            Label lblText = new Label
            {
                Text = text,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                AutoSize = false,
                Size = new Size(150, 40),
                Location = new Point(50, 200)
            };

            panel.Controls.AddRange(new Control[] { iconBox, lblText });

            // Добавляем обработчики событий для эффекта при наведении
            panel.MouseEnter += (s, e) => panel.BackColor = Color.FromArgb(230, 230, 230);
            panel.MouseLeave += (s, e) => panel.BackColor = Color.WhiteSmoke;
            panel.Click += (s, e) => onClick();

            // Добавляем обработчики и для дочерних элементов
            foreach (Control control in panel.Controls)
            {
                control.Click += (s, e) => onClick();
                control.Cursor = Cursors.Hand;
            }

            return panel;
        }

        private async void LoadAssistantInfo()
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
    }
}
