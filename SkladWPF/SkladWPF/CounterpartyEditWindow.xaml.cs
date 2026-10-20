using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;

namespace SkladWPF
{
    public partial class CounterpartyEditWindow : Window
    {
        private readonly string _conn =
            ConfigurationManager.ConnectionStrings["SkladConn"].ConnectionString;

        private readonly int? _id;

        public CounterpartyEditWindow(int? id)
        {
            InitializeComponent();
            _id = id;
            if (id.HasValue) LoadData(id.Value);
        }

        private void LoadData(int id)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT * FROM [30_09_Counterparty] WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        txtNazvanie.Text = r["nazvanie"].ToString();
                        txtAdres.Text = r["adres"].ToString();
                        txtTelephone.Text = r["telephone"].ToString();
                        txtFamilia.Text = r["familia"].ToString();
                        txtImya.Text = r["imya"].ToString();
                        txtOtchestvo.Text = r["otchestvo"].ToString();
                        txtSkidka.Text = r["skidka"].ToString();
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

                    if (_id.HasValue)
                    {
                        sql = @"UPDATE [30_09_Counterparty] SET
                                  nazvanie = @n, adres = @a, telephone = @t,
                                  familia = @f, imya = @i, otchestvo = @o, skidka = @s
                                WHERE id = @id";
                        cmd = new SqlCommand(sql, conn);
                        cmd.Parameters.AddWithValue("@id", _id.Value);
                    }
                    else
                    {
                        sql = @"INSERT INTO [30_09_Counterparty]
                                  (nazvanie, adres, telephone, familia, imya, otchestvo, skidka)
                                VALUES (@n, @a, @t, @f, @i, @o, @s)";
                        cmd = new SqlCommand(sql, conn);
                    }

                    cmd.Parameters.AddWithValue("@n", txtNazvanie.Text);
                    cmd.Parameters.AddWithValue("@a", txtAdres.Text);
                    cmd.Parameters.AddWithValue("@t", txtTelephone.Text);
                    cmd.Parameters.AddWithValue("@f", txtFamilia.Text);
                    cmd.Parameters.AddWithValue("@i", txtImya.Text);
                    cmd.Parameters.AddWithValue("@o", txtOtchestvo.Text);
                    cmd.Parameters.AddWithValue("@s", int.TryParse(txtSkidka.Text, out var s) ? s : 0);

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