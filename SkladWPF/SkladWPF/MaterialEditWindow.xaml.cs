using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Windows;

namespace SkladWPF
{
    public partial class MaterialEditWindow : Window
    {
        private readonly string _conn =
            ConfigurationManager.ConnectionStrings["SkladConn"].ConnectionString;

        private readonly int? _materialId;

        public MaterialEditWindow(int? id)
        {
            InitializeComponent();
            _materialId = id;
            LoadProizvoditels();
            if (id.HasValue) LoadMaterial(id.Value);
        }

        private void LoadProizvoditels()
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                var da = new SqlDataAdapter(
                    "SELECT id, nazvanie FROM [30_09_Proizvoditel] ORDER BY nazvanie", conn);
                var dt = new DataTable();
                da.Fill(dt);
                cmbProizvoditel.ItemsSource = dt.DefaultView;
            }
        }

        private void LoadMaterial(int id)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT * FROM [30_09_Material] WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);

                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        txtNazvanie.Text = r["nazvanie"].ToString();
                        txtEdinIzm.Text = r["EdinIzm"].ToString();
                        txtPrice.Text = r["price"].ToString();
                        txtSrok.Text = r["srok_hranenia"].ToString();
                        if (r["data_prostuplenia"] != DBNull.Value)
                            dpData.SelectedDate = Convert.ToDateTime(r["data_prostuplenia"]);
                        if (r["proizvoditelID"] != DBNull.Value)
                            cmbProizvoditel.SelectedValue = Convert.ToInt32(r["proizvoditelID"]);
                    }
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var conn = new SqlConnection(_conn))
                {
                    conn.Open();
                    string sql;
                    SqlCommand cmd;

                    if (_materialId.HasValue)
                    {
                        sql = @"UPDATE [30_09_Material] SET
                                  nazvanie = @n, EdinIzm = @e, price = @p,
                                  data_prostuplenia = @d, srok_hranenia = @s,
                                  proizvoditelID = @pid
                                WHERE id = @id";
                        cmd = new SqlCommand(sql, conn);
                        cmd.Parameters.AddWithValue("@id", _materialId.Value);
                    }
                    else
                    {
                        sql = @"INSERT INTO [30_09_Material]
                                  (nazvanie, EdinIzm, price, data_prostuplenia, srok_hranenia, proizvoditelID)
                                VALUES (@n, @e, @p, @d, @s, @pid)";
                        cmd = new SqlCommand(sql, conn);
                    }

                    cmd.Parameters.AddWithValue("@n", txtNazvanie.Text);
                    cmd.Parameters.AddWithValue("@e", txtEdinIzm.Text);
                    cmd.Parameters.AddWithValue("@p", int.TryParse(txtPrice.Text, out var price) ? price : 0);
                    cmd.Parameters.AddWithValue("@d", (object)dpData.SelectedDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@s", txtSrok.Text);
                    cmd.Parameters.AddWithValue("@pid", (object)cmbProizvoditel.SelectedValue ?? DBNull.Value);

                    cmd.ExecuteNonQuery();
                }
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
    }
}