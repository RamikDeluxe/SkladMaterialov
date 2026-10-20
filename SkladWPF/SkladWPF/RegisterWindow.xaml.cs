using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;

namespace SkladWPF
{
    public partial class RegisterWindow : Window
    {
        private readonly string _conn =
            ConfigurationManager.ConnectionStrings["SkladConn"].ConnectionString;

        private readonly string _regType;

        public RegisterWindow(string type)
        {
            InitializeComponent();
            _regType = type;

            if (type == "Клиент")
            {
                txtTitle.Text = "Регистрация компании-клиента";
                pnlKlient.Visibility = Visibility.Visible;
                pnlStaff.Visibility = Visibility.Collapsed;
                pnlRole.Visibility = Visibility.Collapsed;
            }
            else
            {
                txtTitle.Text = "Регистрация сотрудника";
                pnlKlient.Visibility = Visibility.Collapsed;
                pnlStaff.Visibility = Visibility.Visible;
                pnlRole.Visibility = Visibility.Visible;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string pass = txtPassword.Password.Trim();

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(pass))
            {
                MessageBox.Show("Логин и пароль обязательны!");
                return;
            }

            try
            {
                using (var conn = new SqlConnection(_conn))
                {
                    conn.Open();

                    // 1. Проверка на существующий логин (ИСПРАВЛЕНО)
                    var checkLogin = new SqlCommand("SELECT COUNT(*) FROM [30_09_User] WHERE Login = @l", conn);
                    checkLogin.Parameters.AddWithValue("@l", login);
                    if ((int)checkLogin.ExecuteScalar() > 0)
                    {
                        MessageBox.Show("Пользователь с таким логином уже существует!");
                        return;
                    }

                    int roleId = 0;
                    int newUserId = 0;

                    // 2. Вставка в таблицу User
                    if (_regType == "Клиент")
                    {
                        roleId = 4; // Роль Клиент
                        InsertUser(conn, login, pass, roleId, out newUserId);

                        // Проверки на уникальность компании и телефона
                        string company = txtCompany.Text.Trim();
                        string phone = txtPhone.Text.Trim();

                        var checkCompany = new SqlCommand("SELECT COUNT(*) FROM [30_09_Klient] WHERE nazvanie = @n", conn);
                        checkCompany.Parameters.AddWithValue("@n", company);
                        if ((int)checkCompany.ExecuteScalar() > 0)
                        {
                            MessageBox.Show("Компания с таким названием уже зарегистрирована!");
                            return;
                        }

                        var checkPhone = new SqlCommand("SELECT COUNT(*) FROM [30_09_Klient] WHERE telephone = @p", conn);
                        checkPhone.Parameters.AddWithValue("@p", phone);
                        if ((int)checkPhone.ExecuteScalar() > 0)
                        {
                            MessageBox.Show("Этот номер телефона уже привязан к другой компании!");
                            return;
                        }

                        // Вставка в Klient
                        var cmdK = new SqlCommand(@"
                            INSERT INTO [30_09_Klient] (nazvanie, adres, telephone, familia, imya, skidka, userID)
                            VALUES (@n, @a, @t, @f, @i, 0, @uid)", conn);
                        cmdK.Parameters.AddWithValue("@n", company);
                        cmdK.Parameters.AddWithValue("@a", txtAddress.Text.Trim());
                        cmdK.Parameters.AddWithValue("@t", phone);
                        cmdK.Parameters.AddWithValue("@f", txtFam.Text.Trim());
                        cmdK.Parameters.AddWithValue("@i", txtImya.Text.Trim());
                        cmdK.Parameters.AddWithValue("@uid", newUserId);
                        cmdK.ExecuteNonQuery();
                    }
                    else // Сотрудник
                    {
                        roleId = (cmbRole.SelectedIndex == 0) ? 2 : 3; // 2 - Кладовщик, 3 - Менеджер
                        InsertUser(conn, login, pass, roleId, out newUserId);

                        // Вставка в Staff
                        var cmdS = new SqlCommand(@"
                            INSERT INTO [30_09_Staff] (imya, familia, otchestvo, telephone, userid)
                            VALUES (@i, @f, @o, @t, @uid)", conn);
                        cmdS.Parameters.AddWithValue("@i", txtStaffImya.Text.Trim());
                        cmdS.Parameters.AddWithValue("@f", txtStaffFam.Text.Trim());
                        cmdS.Parameters.AddWithValue("@o", txtStaffOtch.Text.Trim());
                        cmdS.Parameters.AddWithValue("@t", txtStaffPhone.Text.Trim());
                        cmdS.Parameters.AddWithValue("@uid", newUserId);
                        cmdS.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Регистрация прошла успешно! Теперь вы можете войти.");
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка регистрации: " + ex.Message);
            }
        }

        // Вспомогательный метод для создания юзера и получения его ID (ИСПРАВЛЕНО)
        private void InsertUser(SqlConnection conn, string login, string pass, int roleId, out int newId)
        {
            var cmd = new SqlCommand(@"
                INSERT INTO [30_09_User] (Login, Password, RoleID) 
                OUTPUT INSERTED.id 
                VALUES (@l, @p, @r)", conn);
            cmd.Parameters.AddWithValue("@l", login);
            cmd.Parameters.AddWithValue("@p", pass);
            cmd.Parameters.AddWithValue("@r", roleId);
            newId = (int)cmd.ExecuteScalar();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}