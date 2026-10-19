using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SkladWPF
{
    public partial class ReportsWindow : Window
    {
        private readonly string _conn =
            ConfigurationManager.ConnectionStrings["SkladConn"].ConnectionString;

        public ReportsWindow()
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
                        SELECT m.[id]        AS [ID],
                               m.[nazvanie]  AS [Материал],
                               m.[EdinIzm]   AS [ЕдИзм],
                               m.[price]     AS [Цена],
                               ISNULL(SUM(CASE WHEN n.[OperationType] = N'Приход' THEN mns.[count] ELSE 0 END),0) AS [Приход],
                               ISNULL(SUM(CASE WHEN n.[OperationType] = N'Расход' THEN mns.[count] ELSE 0 END),0) AS [Расход],
                               ISNULL(SUM(CASE WHEN n.[OperationType] = N'Приход' THEN mns.[count] ELSE 0 END),0)
                               - ISNULL(SUM(CASE WHEN n.[OperationType] = N'Расход' THEN mns.[count] ELSE 0 END),0) AS [Остаток],
                               (ISNULL(SUM(CASE WHEN n.[OperationType] = N'Приход' THEN mns.[count] ELSE 0 END),0)
                               - ISNULL(SUM(CASE WHEN n.[OperationType] = N'Расход' THEN mns.[count] ELSE 0 END),0)) * m.[price] AS [СуммаОстатка]
                        FROM [30_09_Material] m
                        LEFT JOIN [30_09_MaterialNaSklade] mns ON m.[id] = mns.[materialid]
                        LEFT JOIN [30_09_nakladnaya]       n   ON mns.[nakladnayaid] = n.[id]
                        GROUP BY m.[id], m.[nazvanie], m.[EdinIzm], m.[price]
                        ORDER BY m.[id]", conn);
                    var dt = new DataTable();
                    da.Fill(dt);
                    dg.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки отчета: " + ex.Message);
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) => LoadData();

        // Теперь меняем только цвет ТЕКСТА всей строки
        private void dg_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            var rowView = e.Row.Item as DataRowView;
            if (rowView == null) return;

            int prihod = Convert.ToInt32(rowView["Приход"]);
            int ostatok = Convert.ToInt32(rowView["Остаток"]);

            if (prihod == 0)
            {
                e.Row.Foreground = Brushes.DarkRed;
                return;
            }

            double percent = (double)ostatok / prihod * 100;

            if (percent > 50)
                e.Row.Foreground = Brushes.DarkGreen;
            else if (percent >= 20)
                e.Row.Foreground = Brushes.DarkGoldenrod;
            else
                e.Row.Foreground = Brushes.DarkRed;
        }
    }
}