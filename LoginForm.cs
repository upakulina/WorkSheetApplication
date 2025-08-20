using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Npgsql;
using WorkSheetApplication.Database;

namespace WorkSheetApplication
{
    public partial class LoginForm : Form
    {
        private PictureBox eyeIcon;
        private bool isPasswordVisible = false;
        private List<(int userId, int roleId)> userRoles;
        public LoginForm()
        {
            InitializeComponent();
            InitializeLoginForm();
        }
        private void InitializeLoginForm()
        {
            this.Text = "Авторизация";
            this.Size = new Size(400, 500);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            
            ConfigureEmailField();
            ConfigurePasswordField();
            ConfigureLoginButton();
            ConfigureForgotPasswordLink();
            ConfigureEyeIcon();
            ConfigurePasswordLabel();
            RepositionForgotPasswordLink();
            ConfigureLoginButtonCursor();
            btnLogin.Click += BtnLogin_Click;
            btnLogin.MouseEnter += (s, e) => btnLogin.BackColor = Color.FromArgb(0, 105, 217);
            btnLogin.MouseLeave += (s, e) => btnLogin.BackColor = Color.FromArgb(0, 123, 255);
            lnkForgotPassword.Click += LnkForgotPassword_Click;
            this.AcceptButton = btnLogin;
        }
        private void ConfigurePasswordLabel()
        {
            Label lblPassword = new Label
            {
                Text = "Пароль",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(64, 64, 64),
                Location = new Point(50, 185),
                AutoSize = true,
                BackColor= Color.Transparent
            };
            this.Controls.Add(lblPassword);
        }
        private void RepositionForgotPasswordLink()
        {
            int centerX = btnLogin.Left + (btnLogin.Width - lnkForgotPassword.Width) / 2;
            lnkForgotPassword.Location = new Point(centerX, btnLogin.Top - 30);
        }
        private void ConfigureLoginButtonCursor()
        {
            btnLogin.Cursor = Cursors.Hand; // Постоянное изменение курсора
                                            // Или для динамического изменения:
            btnLogin.MouseEnter += (s, e) => Cursor = Cursors.Hand;
            btnLogin.MouseLeave += (s, e) => Cursor = Cursors.Default;
        }
        private void ConfigureEmailField()
        {
            int fieldWidth = 265; // Общая ширина для обоих полей
            int fieldHeight = 35;

            txtEmail.Font = new Font("Segoe UI", 11);
            txtEmail.ForeColor = Color.FromArgb(64, 64, 64);
            txtEmail.BackColor = Color.White;
            txtEmail.BorderStyle = BorderStyle.FixedSingle;
            txtEmail.Size = new Size(fieldWidth, fieldHeight);
            txtEmail.Location = new Point(50, 150);
        }
        private void ConfigurePasswordField()
        {
            int fieldWidth = 265; // Теперь одинаковый размер
            int fieldHeight = 35;

            txtPassword.Font = new Font("Segoe UI", 11);
            txtPassword.ForeColor = Color.FromArgb(64, 64, 64);
            txtPassword.BackColor = Color.White;
            txtPassword.BorderStyle = BorderStyle.FixedSingle;
            txtPassword.Size = new Size(fieldWidth, fieldHeight);
            txtPassword.Location = new Point(50, 210);

            txtPassword.UseSystemPasswordChar = true;
        }
        private void ConfigureEyeIcon()
        {
            eyeIcon = new PictureBox
            {
                Image = Properties.Resources.eye_closed_, // Начальная иконка
                SizeMode = PictureBoxSizeMode.StretchImage,
                Size = new Size(25, 20),
                Location = new Point(txtPassword.Right + 5, txtPassword.Top + 5),
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };
            eyeIcon.Click += TogglePasswordVisibility;
            this.Controls.Add(eyeIcon);
        }
        private void TogglePasswordVisibility(object sender, EventArgs e)
        {
            isPasswordVisible = !isPasswordVisible;
            txtPassword.UseSystemPasswordChar = !isPasswordVisible;
            eyeIcon.Image = isPasswordVisible
                ? Properties.Resources.eye_open_icon
                : Properties.Resources.eye_closed_; 
        }
        private void ConfigureLoginButton()
        {
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.BackColor = Color.FromArgb(0, 123, 255); 
            btnLogin.ForeColor = Color.White;
            btnLogin.Font = new Font("Segoe UI Semibold", 12);
            btnLogin.Size = new Size(300, 45);
            btnLogin.Location = new Point(50, 290);
            btnLogin.Text = "Войти";
            btnLogin.Region = CreateRoundedRegion(btnLogin.ClientRectangle, 10);
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
        private void ConfigureForgotPasswordLink()
        {
            lnkForgotPassword.LinkColor = Color.FromArgb(102, 102, 102);
            lnkForgotPassword.ActiveLinkColor = Color.FromArgb(0, 123, 255);
            lnkForgotPassword.Font = new Font("Segoe UI", 9);
            lnkForgotPassword.Location = new Point(50, 350);
        }
        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtEmail.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Пожалуйста, заполните все поля!", "Предупреждение",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnLogin.Enabled = false;
            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    await conn.OpenAsync();
                    string sql = @"SELECT id_user, id_role, password_hash 
                         FROM public.""user"" 
                         WHERE email = @email";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("email", txtEmail.Text.Trim());

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            userRoles = new List<(int, int)>();

                            while (await reader.ReadAsync())
                            {
                                int userId = reader.GetInt32(0);
                                int roleId = reader.GetInt32(1);
                                string storedHash = reader.GetString(2);

                                if (storedHash == txtPassword.Text.Trim())
                                {
                                    userRoles.Add((userId, roleId));
                                }
                            }

                            if (userRoles.Count == 0)
                            {
                                MessageBox.Show("Неверный логин или пароль!", "Ошибка",
                                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }


                            if (userRoles.Count > 1)
                            {
                                var roleSelectForm = new RoleSelectForm(userRoles);
                                this.Hide();
                                roleSelectForm.ShowDialog();
                                this.Close();
                            }
                            else
                            {
                                
                                OpenMainForm(userRoles[0].userId, userRoles[0].roleId);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при входе: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnLogin.Enabled = true;
            }
        }

        private void LnkForgotPassword_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Если у вас возникли вопросы, свяжитесь с нами ulpakulina@edu.hse.ru",
                           "Восстановление пароля",
                           MessageBoxButtons.OK,
                           MessageBoxIcon.Information);
        }

        private void OpenMainForm(int userId, int roleId)
        {
            Form mainForm = null;
            switch (roleId)
            {
                case 1: 
                    mainForm = new StudentForm(userId);
                    break;
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

