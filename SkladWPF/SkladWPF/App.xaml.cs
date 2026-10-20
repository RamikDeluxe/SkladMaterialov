using System.Windows;

namespace SkladWPF
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Запрещаем приложению закрываться, пока открыто окно логина
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            LoginWindow login = new LoginWindow();
            bool? result = login.ShowDialog();

            if (result == true)
            {
                // Если вход успешен, создаем и показываем главное окно
                MainWindow main = new MainWindow();

                // Теперь главное окно управляет жизнью приложения
                MainWindow = main;
                ShutdownMode = ShutdownMode.OnMainWindowClose;

                main.Show();
            }
            else
            {
                // Если нажали "Выход" или закрыли крестиком
                Shutdown();
            }
        }
    }
}