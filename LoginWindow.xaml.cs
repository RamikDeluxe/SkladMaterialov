using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;

namespace SkladWPF
{
    public partial class LoginWindow : Window
    {
        private readonly string _conn =
            ConfigurationManager.ConnectionStrings["SkladConn"].ConnectionString;

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Password.Trim();

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите логин и пароль!");
                return;
            }

            try
            {
                using (var conn = new SqlConnection(_conn))
                {
                    conn.Open();
                    var cmd = new SqlCommand(@"
                        SELECT u.id, r.RoleName 
                        FROM [30_09_User] u
                        JOIN [30_09_Roles] r ON u.RoleID = r.id
                        WHERE u.Login = @l AND u.Password = @p", conn);

                    cmd.Parameters.AddWithValue("@l", login);
                    cmd.Parameters.AddWithValue("@p", password);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Сохраняем данные в сессию
                            Session.UserId = Convert.ToInt32(reader["id"]);
                            Session.Login = login;
                            Session.Role = reader["RoleName"].ToString();

                            DialogResult = true;
                            Close();
                        }
                        else
                        {
                            MessageBox.Show("Неверный логин или пароль!");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка БД: " + ex.Message);
            }
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            RegisterWindow reg = new RegisterWindow("Клиент");
            reg.ShowDialog();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}