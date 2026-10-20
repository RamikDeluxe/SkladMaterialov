using System;
using System.Windows;

namespace SkladWPF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            try
            {
                ApplyRolePermissions();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке главного окна: " + ex.Message);
            }
        }

        private void ApplyRolePermissions()
        {
            // Показываем, кто вошел
            txtHeader.Text = $"Склад стройматериалов | Вы вошли как: {Session.Role} ({Session.Login})";
            txtUserInfo.Text = $"Пользователь: {Session.Login}";

            // Сначала скрываем всё, что связано с управлением
            btnProizvoditels.Visibility = Visibility.Collapsed;
            btnCounterparties.Visibility = Visibility.Collapsed;
            btnReports.Visibility = Visibility.Collapsed;
            btnRegisterStaff.Visibility = Visibility.Collapsed;

            switch (Session.Role)
            {
                case "Администратор":
                    // Админ видит всё
                    btnProizvoditels.Visibility = Visibility.Visible;
                    btnCounterparties.Visibility = Visibility.Visible;
                    btnReports.Visibility = Visibility.Visible;
                    btnRegisterStaff.Visibility = Visibility.Visible;
                    break;

                case "Менеджер":
                    // Менеджер видит справочники и отчеты, но не может регистрировать сотрудников
                    btnProizvoditels.Visibility = Visibility.Visible;
                    btnCounterparties.Visibility = Visibility.Visible;
                    btnReports.Visibility = Visibility.Visible;
                    break;

                case "Кладовщик":
                    // Кладовщик работает только с материалами и накладными (остальное скрыто)
                    break;

                case "Клиент":
                    // Клиент видит только материалы (для ознакомления) и накладные (свои заказы)
                    // Кнопки справочников и отчетов скрыты
                    break;
            }
        }
        private void OpenUsers_Click(object sender, RoutedEventArgs e)
        {
            new UsersWindow().ShowDialog();
        }

        // Обработчики переходов
        private void OpenMaterials_Click(object sender, RoutedEventArgs e) => new MaterialsWindow().ShowDialog();
        private void OpenProizvoditels_Click(object sender, RoutedEventArgs e) => new ProizvoditelsWindow().ShowDialog();
        private void OpenCounterparties_Click(object sender, RoutedEventArgs e) => new CounterpartiesWindow().ShowDialog();
        private void OpenNakladnayas_Click(object sender, RoutedEventArgs e) => new NakladnayasWindow().ShowDialog();
        private void OpenReports_Click(object sender, RoutedEventArgs e) => new ReportsWindow().ShowDialog();
        private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    }
}