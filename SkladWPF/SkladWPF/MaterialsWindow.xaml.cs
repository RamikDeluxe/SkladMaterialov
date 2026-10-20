using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Windows;

namespace SkladWPF
{
    public partial class MaterialsWindow : Window
    {
        private readonly string _conn =
            ConfigurationManager.ConnectionStrings["SkladConn"].ConnectionString;

        public MaterialsWindow()
        {
            InitializeComponent();

            if (Session.Role == "Клиент")
            {
                btnAdd.Visibility = Visibility.Collapsed;
                btnEdit.Visibility = Visibility.Collapsed;
                btnDelete.Visibility = Visibility.Collapsed;
            }

            LoadData();
        }

        private void LoadData(string filter = null)
        {
            try
            {
                using (var conn = new SqlConnection(_conn))
                {
                    conn.Open();
                    string sql = @"
                        SELECT m.[id]                AS [ID],
                               m.[nazvanie]          AS [Наименование],
                               m.[EdinIzm]           AS [ЕдИзм],
                               m.[price]             AS [Цена],
                               m.[data_prostuplenia] AS [Дата поступления],
                               m.[srok_hranenia]     AS [Срок хранения],
                               p.[nazvanie]          AS [Производитель]
                        FROM [30_09_Material] m
                        LEFT JOIN [30_09_Proizvoditel] p ON m.[proizvoditelID] = p.[id]";

                    if (!string.IsNullOrWhiteSpace(filter))
                        sql += " WHERE m.[nazvanie] LIKE @f OR p.[nazvanie] LIKE @f";

                    sql += " ORDER BY m.[id]";

                    var da = new SqlDataAdapter(sql, conn);
                    if (!string.IsNullOrWhiteSpace(filter))
                        da.SelectCommand.Parameters.AddWithValue("@f", "%" + filter + "%");

                    var dt = new DataTable();
                    da.Fill(dt);
                    dgMaterials.ItemsSource = dt.DefaultView;
                    txtStatus.Text = "Всего записей: " + dt.Rows.Count;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки: " + ex.Message);
            }
        }

        private void Search_Click(object sender, RoutedEventArgs e) => LoadData(txtSearch.Text.Trim());
        private void Refresh_Click(object sender, RoutedEventArgs e) { txtSearch.Text = ""; LoadData(); }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var w = new MaterialEditWindow(null);
            if (w.ShowDialog() == true) LoadData();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (dgMaterials.SelectedItem is DataRowView row)
            {
                int id = Convert.ToInt32(row["ID"]);
                var w = new MaterialEditWindow(id);
                if (w.ShowDialog() == true) LoadData();
            }
            else MessageBox.Show("Выберите строку.");
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (!(dgMaterials.SelectedItem is DataRowView row)) return;
            int id = Convert.ToInt32(row["ID"]);
            string name = row["Наименование"].ToString();

            if (MessageBox.Show("Удалить " + name + "?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            try
            {
                using (var conn = new SqlConnection(_conn))
                {
                    conn.Open();
                    var cmd = new SqlCommand("DELETE FROM [30_09_Material] WHERE id = @id", conn);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления: " + ex.Message);
            }
        }
    }
}