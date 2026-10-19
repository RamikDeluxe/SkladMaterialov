using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace SkladWPF
{
    public partial class UserEditWindow : Window
    {
        private readonly string _conn =
            ConfigurationManager.ConnectionStrings["SkladConn"].ConnectionString;

        private readonly int? _userId;
        private int _counterpartyId = 0;
        private int _staffId = 0;

        public UserEditWindow(int? id)
        {
            InitializeComponent();
            _userId = id;

            if (!id.HasValue) cmbRole.SelectedIndex = 3; // По умолчанию Клиент

            if (id.HasValue) LoadData(id.Value);
        }

        private void Role_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbRole.SelectedItem is ComboBoxItem item)
            {
                string tag = item.Tag.ToString();

                pnlStaff.Visibility = Visibility.Collapsed;
                pnlKlient.Visibility = Visibility.Collapsed;

                if (tag == "4") pnlKlient.Visibility = Visibility.Visible;
                else if (tag == "2" || tag == "3") pnlStaff.Visibility = Visibility.Visible;
            }
        }

        private void LoadData(int id)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();

                // 1. Загружаем юзера
                var cmd = new SqlCommand("SELECT * FROM [30_09_User] WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        txtLogin.Text = r["Login"].ToString();
                        txtPassword.Text = r["Password"].ToString();

                        int roleId = Convert.ToInt32(r["RoleID"]);
                        foreach (ComboBoxItem cbi in cmbRole.Items)
                        {
                            if (cbi.Tag.ToString() == roleId.ToString())
                            {
                                cmbRole.SelectedItem = cbi;
                                break;
                            }
                        }
                    }
                }

                // 2. Загружаем профиль клиента (если есть)
                var cmdK = new SqlCommand("SELECT * FROM [30_09_Counterparty] WHERE userID = @uid", conn);
                cmdK.Parameters.AddWithValue("@uid", id);
                using (var r = cmdK.ExecuteReader())
                {
                    if (r.Read())
                    {
                        _counterpartyId = Convert.ToInt32(r["id"]);
                        txtCompany.Text = r["nazvanie"].ToString();
                        txtCompPhone.Text = r["telephone"].ToString();
                        txtAddress.Text = r["adres"].ToString();
                        txtDiscount.Text = r["skidka"].ToString();

                        // Загружаем ФИО клиента
                        txtKlientFam.Text = r["familia"].ToString();
                        txtKlientName.Text = r["imya"].ToString();
                        txtKlientOtch.Text = r["otchestvo"].ToString();
                    }
                }

                // 3. Загружаем профиль сотрудника (если есть)
                var cmdS = new SqlCommand("SELECT * FROM [30_09_Staff] WHERE userid = @uid", conn);
                cmdS.Parameters.AddWithValue("@uid", id);
                using (var r = cmdS.ExecuteReader())
                {
                    if (r.Read())
                    {
                        _staffId = Convert.ToInt32(r["id"]);
                        txtFam.Text = r["familia"].ToString();
                        txtName.Text = r["imya"].ToString();
                        txtOtch.Text = r["otchestvo"].ToString();
                        txtPhone.Text = r["telephone"].ToString();
                    }
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtLogin.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Логин и пароль обязательны!");
                return;
            }

            if (cmbRole.SelectedItem == null) return;
            int roleId = Convert.ToInt32(((ComboBoxItem)cmbRole.SelectedItem).Tag);

            try
            {
                using (var conn = new SqlConnection(_conn))
                {
                    conn.Open();

                    // 1. Сохраняем пользователя
                    int currentUserId = _userId ?? 0;
                    if (_userId.HasValue)
                    {
                        var cmd = new SqlCommand("UPDATE [30_09_User] SET Login=@l, Password=@p, RoleID=@r WHERE id=@id", conn);
                        cmd.Parameters.AddWithValue("@id", _userId.Value);
                        cmd.Parameters.AddWithValue("@l", txtLogin.Text);
                        cmd.Parameters.AddWithValue("@p", txtPassword.Text);
                        cmd.Parameters.AddWithValue("@r", roleId);
                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        var cmd = new SqlCommand("INSERT INTO [30_09_User] (Login, Password, RoleID) OUTPUT INSERTED.id VALUES (@l, @p, @r)", conn);
                        cmd.Parameters.AddWithValue("@l", txtLogin.Text);
                        cmd.Parameters.AddWithValue("@p", txtPassword.Text);
                        cmd.Parameters.AddWithValue("@r", roleId);
                        currentUserId = (int)cmd.ExecuteScalar();
                    }

                    // 2. Сохраняем профиль
                    if (roleId == 4) // Клиент
                    {
                        if (_counterpartyId > 0)
                        {
                            var cmd = new SqlCommand(@"UPDATE [30_09_Counterparty] 
                                SET nazvanie=@n, telephone=@t, adres=@a, skidka=@s, familia=@f, imya=@i, otchestvo=@o 
                                WHERE id=@id", conn);
                            cmd.Parameters.AddWithValue("@id", _counterpartyId);
                            cmd.Parameters.AddWithValue("@n", txtCompany.Text);
                            cmd.Parameters.AddWithValue("@t", txtCompPhone.Text);
                            cmd.Parameters.AddWithValue("@a", txtAddress.Text);
                            cmd.Parameters.AddWithValue("@s", int.TryParse(txtDiscount.Text, out var s) ? s : 0);
                            cmd.Parameters.AddWithValue("@f", txtKlientFam.Text);
                            cmd.Parameters.AddWithValue("@i", txtKlientName.Text);
                            cmd.Parameters.AddWithValue("@o", txtKlientOtch.Text);
                            cmd.ExecuteNonQuery();
                        }
                        else
                        {
                            var cmd = new SqlCommand(@"INSERT INTO [30_09_Counterparty] 
                                (nazvanie, telephone, adres, skidka, familia, imya, otchestvo, userID) 
                                VALUES (@n, @t, @a, @s, @f, @i, @o, @uid)", conn);
                            cmd.Parameters.AddWithValue("@n", txtCompany.Text);
                            cmd.Parameters.AddWithValue("@t", txtCompPhone.Text);
                            cmd.Parameters.AddWithValue("@a", txtAddress.Text);
                            cmd.Parameters.AddWithValue("@s", int.TryParse(txtDiscount.Text, out var s) ? s : 0);
                            cmd.Parameters.AddWithValue("@f", txtKlientFam.Text);
                            cmd.Parameters.AddWithValue("@i", txtKlientName.Text);
                            cmd.Parameters.AddWithValue("@o", txtKlientOtch.Text);
                            cmd.Parameters.AddWithValue("@uid", currentUserId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else if (roleId == 2 || roleId == 3) // Сотрудник
                    {
                        if (_staffId > 0)
                        {
                            var cmd = new SqlCommand(@"UPDATE [30_09_Staff] SET familia=@f, imya=@i, otchestvo=@o, telephone=@t WHERE id=@id", conn);
                            cmd.Parameters.AddWithValue("@id", _staffId);
                            cmd.Parameters.AddWithValue("@f", txtFam.Text);
                            cmd.Parameters.AddWithValue("@i", txtName.Text);
                            cmd.Parameters.AddWithValue("@o", txtOtch.Text);
                            cmd.Parameters.AddWithValue("@t", txtPhone.Text);
                            cmd.ExecuteNonQuery();
                        }
                        else
                        {
                            var cmd = new SqlCommand(@"INSERT INTO [30_09_Staff] (familia, imya, otchestvo, telephone, userid) VALUES (@f, @i, @o, @t, @uid)", conn);
                            cmd.Parameters.AddWithValue("@f", txtFam.Text);
                            cmd.Parameters.AddWithValue("@i", txtName.Text);
                            cmd.Parameters.AddWithValue("@o", txtOtch.Text);
                            cmd.Parameters.AddWithValue("@t", txtPhone.Text);
                            cmd.Parameters.AddWithValue("@uid", currentUserId);
                            cmd.ExecuteNonQuery();
                        }
                    }
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