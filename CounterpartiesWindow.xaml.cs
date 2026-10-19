using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Windows;

namespace SkladWPF
{
    public partial class CounterpartiesWindow : Window
    {
        private readonly string _conn =
            ConfigurationManager.ConnectionStrings["SkladConn"].ConnectionString;

        public CounterpartiesWindow()
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
                    var da = new SqlDataAdapter(@"
                        SELECT [id]        AS [ID],
                               [nazvanie]  AS [Название],
                               [adres]     AS [Адрес],
                               [telephone] AS [Телефон],
                               [familia]   AS [Фамилия],
                               [imya]      AS [Имя],
                               [otchestvo] AS [Отчество],
                               [skidka]    AS [Скидка]
                        FROM [30_09_Counterparty] ORDER BY [id]", conn);
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
            var w = new CounterpartyEditWindow(null);
            if (w.ShowDialog() == true) LoadData();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (dg.SelectedItem is DataRowView row)
            {
                int id = Convert.ToInt32(row["ID"]);
                var w = new CounterpartyEditWindow(id);
                if (w.ShowDialog() == true) LoadData();
            }
            else MessageBox.Show("Выберите строку.");
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (!(dg.SelectedItem is DataRowView row)) return;
            int id = Convert.ToInt32(row["ID"]);
            if (MessageBox.Show("Удалить " + row["Название"] + "?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            try
            {
                using (var conn = new SqlConnection(_conn))
                {
                    conn.Open();
                    var cmd = new SqlCommand("DELETE FROM [30_09_Counterparty] WHERE id = @id", conn);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
                LoadData();
            }
            catch (Exception ex) { MessageBox.Show("Ошибка удаления: " + ex.Message); }
        }
    }
}