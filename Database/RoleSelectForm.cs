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
    public partial class RoleSelectForm : Form
    {
        private List<(int userId, int roleId)> userRoles;

        public RoleSelectForm(List<(int userId, int roleId)> roles)
        {
            InitializeComponent();
            userRoles = roles;
            InitializeRoleSelectForm();
        }

        private void InitializeRoleSelectForm()
        {
            // Настройка формы
            this.Text = "Выберите роль";
            this.Size = new Size(400, 300);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;

            // Создание кнопок для каждой роли
            int buttonY = 105;
            foreach (var role in userRoles)
            {
                Button btnRole = new Button
                {
                    Text = GetRoleName(role.roleId),
                    Size = new Size(200, 40),
                    Location = new Point(90, buttonY),
                    FlatStyle = FlatStyle.Flat,
                    //Size = new Size(120, 30),
                    BackColor = Color.FromArgb(0, 123, 255), // Синий цвет
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI Semibold", 12),
                    Cursor = Cursors.Hand
                };
                btnRole.FlatAppearance.BorderSize = 0;
                btnRole.Region = CreateRoundedRegion(btnRole.ClientRectangle, 10);
                btnRole.Tag = (role.userId, role.roleId);

                btnRole.Click += BtnRole_Click;

                this.Controls.Add(btnRole);
                buttonY += 50;
            }
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
        private string GetRoleName(int roleId)
        {
            switch (roleId)
            {
                case 1: return "Студент";
                case 2: return "Преподаватель";
                case 3: return "Сотрудник УО";
                default: return "Неизвестная роль";
            }
        }

        private void BtnRole_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            // Извлекаем кортеж (userId, roleId) из Tag кнопки
            (int userId, int roleId) = ((int, int))btn.Tag;

            this.Hide();
            OpenMainForm(userId, roleId); // Передаем оба параметра
            this.Close();
        }

        private void OpenMainForm(int userId, int roleId)
        {
            Form mainForm = null;
            switch (roleId)
            {
                case 2:
                    mainForm = new TeacherForm(userId);
                    break;
                case 3:
                    mainForm = new AssistantForm(userId);
                    break;
            }

            if (mainForm != null)
            {
                this.Hide();
                mainForm.ShowDialog();
                this.Close();
            }
        }
    }
}
