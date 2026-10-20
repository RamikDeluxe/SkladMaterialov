using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Windows;

namespace SkladWPF
{
    public partial class NakladnayaItemsWindow : Window
    {
        private readonly string _conn =
            ConfigurationManager.ConnectionStrings["SkladConn"].ConnectionString;

        public NakladnayaItemsWindow(int nakladnayaId)
        {
            InitializeComponent();
            txtTitle.Text = "Состав накладной номер " + nakladnayaId;

            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                var da = new SqlDataAdapter(@"
                    SELECT m.[nazvanie] AS [Материал],
                           m.[EdinIzm]  AS [ЕдИзм],
                           mns.[count]  AS [Количество],
                           m.[price]    AS [Цена],
                           (mns.[count] * m.[price]) AS [Сумма]
                    FROM [30_09_MaterialNaSklade] mns
                    JOIN [30_09_Material] m ON mns.[materialId] = m.[id]
                    WHERE mns.[nakladnayaID] = @id", conn);
                da.SelectCommand.Parameters.AddWithValue("@id", nakladnayaId);
                var dt = new DataTable();
                da.Fill(dt);
                dg.ItemsSource = dt.DefaultView;
            }
        }
    }
}