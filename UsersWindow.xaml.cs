using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Windows;

namespace SkladWPF
{
    public partial class UsersWindow : Window
    {
        private readonly string _conn =
            ConfigurationManager.ConnectionStrings["SkladConn"].ConnectionString;

        public UsersWindow()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (var conn = new SqlConnection(_conn))
                {
                    conn.Open();
                    // Связываем пользователей, роли, контрагентов (клиентов) и сотрудников (staff)
                    var da = new SqlDataAdapter(@"
                        SELECT u.[id] AS [ID],
                               u.[Login] AS [Логин],
                               u.[Password] AS [Пароль],
                               r.[RoleName] AS [Роль],
                               ISNULL(c.[nazvanie], s.[familia] + ' ' + s.[imya]) AS [Профиль]
                        FROM [30_09_User] u
                        JOIN [30_09_Roles] r ON u.[RoleID] = r.[id]
                        LEFT JOIN [30_09_Counterparty] c ON c.[userID] = u.[id]
                        LEFT JOIN [30_09_Staff] s ON s.[userid] = u.[id]
                        ORDER BY u.[id]", conn);
                    var dt = new DataTable();
                    da.Fill(dt);
                    dg.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки: " + ex.Message);
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) => LoadData();

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var w = new UserEditWindow(null);
            if (w.ShowDialog() == true) LoadData();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (dg.SelectedItem is DataRowView row)
            {
                int id = Convert.ToInt32(row["ID"]);
                var w = new UserEditWindow(id);
                if (w.ShowDialog() == true) LoadData();
            }
            else MessageBox.Show("Выберите строку.");
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (!(dg.SelectedItem is DataRowView row)) return;
            int id = Convert.ToInt32(row["ID"]);
            string login = row["Логин"].ToString();

            if (login == "admin")
            {
                MessageBox.Show("Нельзя удалить главного администратора!");
                return;
            }

            if (MessageBox.Show($"Удалить пользователя {login}?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            try
            {
                using (var conn = new SqlConnection(_conn))
                {
                    conn.Open();
                    // Удаляем связанные профили (если есть)
                    var cmdDel = new SqlCommand(@"
                        DELETE FROM [30_09_Counterparty] WHERE userID = @id;
                        DELETE FROM [30_09_Staff] WHERE userid = @id;
                        DELETE FROM [30_09_User] WHERE id = @id;", conn);
                    cmdDel.Parameters.AddWithValue("@id", id);
                    cmdDel.ExecuteNonQuery();
                }
                LoadData();
            }
            catch (Exception ex) { MessageBox.Show("Ошибка удаления: " + ex.Message); }
        }
    }
}