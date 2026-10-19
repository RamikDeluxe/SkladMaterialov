using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;

namespace SkladWPF
{
    public partial class PostavshikEditWindow : Window
    {
        private readonly string _conn =
            ConfigurationManager.ConnectionStrings["SkladConn"].ConnectionString;

        private readonly int? _id;

        public PostavshikEditWindow(int? id)
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
                var cmd = new SqlCommand("SELECT * FROM [30_09_Postavshik] WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        txtName.Text = r["name"].ToString();
                        txtAdres.Text = r["adres"].ToString();
                        txtFamilia.Text = r["familia"].ToString();
                        txtImya.Text = r["imya"].ToString();
                        txtOtchestvo.Text = r["otchestvo"].ToString();
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
                        sql = @"UPDATE [30_09_Postavshik] SET
                                  name = @n, adres = @a, familia = @f,
                                  imya = @i, otchestvo = @o
                                WHERE id = @id";
                        cmd = new SqlCommand(sql, conn);
                        cmd.Parameters.AddWithValue("@id", _id.Value);
                    }
                    else
                    {
                        sql = @"INSERT INTO [30_09_Postavshik]
                                  (name, adres, familia, imya, otchestvo)
                                VALUES (@n, @a, @f, @i, @o)";
                        cmd = new SqlCommand(sql, conn);
                    }

                    cmd.Parameters.AddWithValue("@n", txtName.Text);
                    cmd.Parameters.AddWithValue("@a", txtAdres.Text);
                    cmd.Parameters.AddWithValue("@f", txtFamilia.Text);
                    cmd.Parameters.AddWithValue("@i", txtImya.Text);
                    cmd.Parameters.AddWithValue("@o", txtOtchestvo.Text);

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