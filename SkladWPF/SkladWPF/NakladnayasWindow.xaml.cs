using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace SkladWPF
{
    public partial class NakladnayasWindow : Window
    {
        private readonly string _conn =
            ConfigurationManager.ConnectionStrings["SkladConn"].ConnectionString;

        public NakladnayasWindow()
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

                    // Если зашел клиент, грузим только его расходные накладные, а приход оставляем пустым
                    if (Session.Role == "Клиент")
                    {
                        // Находим ID контрагента по ID пользователя
                        var cmdGetKlient = new SqlCommand("SELECT id FROM [30_09_Counterparty] WHERE userID = @uid", conn);
                        cmdGetKlient.Parameters.AddWithValue("@uid", Session.UserId);
                        object klientIdObj = cmdGetKlient.ExecuteScalar();

                        if (klientIdObj != null)
                        {
                            int klientId = Convert.ToInt32(klientIdObj);

                            // Загружаем ТОЛЬКО расходные накладные этого клиента
                            var cmdOut = new SqlCommand(@"
                        SELECT n.[id] AS [Номер], n.[data] AS [Дата], 
                               c.[nazvanie] AS [Клиент], c.[telephone] AS [Телефон], 
                               n.[Total] AS [Сумма]
                        FROM [30_09_nakladnaya] n
                        JOIN [30_09_Counterparty] c ON n.[CounterpartyID] = c.[id]
                        WHERE n.[OperationType] = N'Расход' AND n.[CounterpartyID] = @kid
                        ORDER BY n.[id]", conn);
                            cmdOut.Parameters.AddWithValue("@kid", klientId);

                            var daOut = new SqlDataAdapter(cmdOut);
                            var dtOut = new DataTable();
                            daOut.Fill(dtOut);
                            dgOutgoing.ItemsSource = dtOut.DefaultView;
                        }
                        else
                        {
                            // Если клиент почему-то не найден в таблице контрагентов, просто оставляем пустые таблицы
                            dgIncoming.ItemsSource = null;
                            dgOutgoing.ItemsSource = null;
                        }

                        // Очищаем левую таблицу (Приход), чтобы она была пустой
                        dgIncoming.ItemsSource = null;
                    }
                    else
                    {
                        // Для сотрудников (Админ, Менеджер, Кладовщик) грузим обе таблицы

                        // Приход
                        var daIn = new SqlDataAdapter(@"
                    SELECT n.[id] AS [Номер], n.[data] AS [Дата], 
                           c.[nazvanie] AS [Поставщик], c.[telephone] AS [Телефон], 
                           n.[Total] AS [Сумма]
                    FROM [30_09_nakladnaya] n
                    JOIN [30_09_Counterparty] c ON n.[CounterpartyID] = c.[id]
                    WHERE n.[OperationType] = N'Приход'
                    ORDER BY n.[id]", conn);
                        var dtIn = new DataTable();
                        daIn.Fill(dtIn);
                        dgIncoming.ItemsSource = dtIn.DefaultView;

                        // Расход
                        var daOut = new SqlDataAdapter(@"
                    SELECT n.[id] AS [Номер], n.[data] AS [Дата], 
                           c.[nazvanie] AS [Клиент], c.[telephone] AS [Телефон], 
                           n.[Total] AS [Сумма]
                    FROM [30_09_nakladnaya] n
                    JOIN [30_09_Counterparty] c ON n.[CounterpartyID] = c.[id]
                    WHERE n.[OperationType] = N'Расход'
                    ORDER BY n.[id]", conn);
                        var dtOut = new DataTable();
                        daOut.Fill(dtOut);
                        dgOutgoing.ItemsSource = dtOut.DefaultView;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки накладных:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) => LoadData();

        private void ShowIncomingItems_Click(object sender, RoutedEventArgs e)
        {
            if (dgIncoming.SelectedItem is DataRowView row)
                ExportToPdf(Convert.ToInt32(row["Номер"]), "Приход");
            else
                MessageBox.Show("Выберите приходную накладную.");
        }

        private void ShowOutgoingItems_Click(object sender, RoutedEventArgs e)
        {
            if (dgOutgoing.SelectedItem is DataRowView row)
                ExportToPdf(Convert.ToInt32(row["Номер"]), "Расход");
            else
                MessageBox.Show("Выберите расходную накладную.");
        }

        private void ExportToPdf(int nakladnayaId, string type)
        {
            try
            {
                DataTable dtInfo = new DataTable();
                DataTable dtItems = new DataTable();

                // 1. Получаем данные из БД
                using (var conn = new SqlConnection(_conn))
                {
                    conn.Open();

                    string sqlInfo = @"
                        SELECT n.[id], n.[data], n.[total], c.[nazvanie] AS kontragent 
                        FROM [30_09_nakladnaya] n 
                        JOIN [30_09_Counterparty] c ON n.[CounterpartyID] = c.[id] 
                        WHERE n.[id] = @id";

                    var cmdInfo = new SqlCommand(sqlInfo, conn);
                    cmdInfo.Parameters.AddWithValue("@id", nakladnayaId);
                    new SqlDataAdapter(cmdInfo).Fill(dtInfo);

                    var cmdItems = new SqlCommand(@"
                        SELECT m.[nazvanie], m.[EdinIzm], mns.[count], m.[price], (mns.[count] * m.[price]) AS summa
                        FROM [30_09_MaterialNaSklade] mns
                        JOIN [30_09_Material] m ON mns.[materialid] = m.[id]
                        WHERE mns.[nakladnayaid] = @id", conn);
                    cmdItems.Parameters.AddWithValue("@id", nakladnayaId);
                    new SqlDataAdapter(cmdItems).Fill(dtItems);
                }

                if (dtInfo.Rows.Count == 0)
                {
                    MessageBox.Show("Накладная не найдена в базе данных.");
                    return;
                }

                DataRow info = dtInfo.Rows[0];

                // 2. Формируем путь для сохранения на Рабочий стол
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string pdfPath = Path.Combine(desktopPath, $"Накладная_{type}_{nakladnayaId}.pdf");

                // 3. Настройка шрифта для кириллицы (берем Arial из системной папки)
                string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");

                // Используем полные имена классов, чтобы избежать конфликтов с WPF
                iTextSharp.text.pdf.BaseFont baseFont = iTextSharp.text.pdf.BaseFont.CreateFont(fontPath, iTextSharp.text.pdf.BaseFont.IDENTITY_H, iTextSharp.text.pdf.BaseFont.EMBEDDED);

                iTextSharp.text.Font titleFont = new iTextSharp.text.Font(baseFont, 16, iTextSharp.text.Font.BOLD);
                iTextSharp.text.Font headerFont = new iTextSharp.text.Font(baseFont, 10, iTextSharp.text.Font.BOLD);
                iTextSharp.text.Font normalFont = new iTextSharp.text.Font(baseFont, 10, iTextSharp.text.Font.NORMAL);

                // 4. Генерация PDF
                using (FileStream fs = new FileStream(pdfPath, FileMode.Create))
                {
                    iTextSharp.text.Document doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 30f, 30f, 30f, 30f);
                    iTextSharp.text.pdf.PdfWriter.GetInstance(doc, fs);
                    doc.Open();

                    // Заголовок
                    iTextSharp.text.Paragraph title = new iTextSharp.text.Paragraph($"Накладная № {info["id"]} ({type})", titleFont);
                    title.Alignment = iTextSharp.text.Element.ALIGN_CENTER;
                    doc.Add(title);
                    doc.Add(new iTextSharp.text.Paragraph("\n"));

                    // Информация
                    doc.Add(new iTextSharp.text.Paragraph($"Дата: {Convert.ToDateTime(info["data"]):dd.MM.yyyy}", normalFont));
                    doc.Add(new iTextSharp.text.Paragraph($"Контрагент: {info["kontragent"]}", normalFont));
                    doc.Add(new iTextSharp.text.Paragraph("\n"));

                    // Таблица
                    iTextSharp.text.pdf.PdfPTable table = new iTextSharp.text.pdf.PdfPTable(6);
                    table.WidthPercentage = 100;
                    table.SetWidths(new float[] { 1f, 5f, 2f, 2f, 2f, 2f }); // Пропорции колонок

                    // Шапка таблицы
                    string[] headers = { "№", "Материал", "Ед. изм.", "Кол-во", "Цена", "Сумма" };
                    foreach (var h in headers)
                    {
                        iTextSharp.text.pdf.PdfPCell cell = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(h, headerFont));
                        cell.BackgroundColor = new iTextSharp.text.BaseColor(240, 240, 240);
                        cell.HorizontalAlignment = iTextSharp.text.Element.ALIGN_CENTER;
                        cell.Padding = 5f;
                        table.AddCell(cell);
                    }

                    // Данные таблицы
                    int num = 1;
                    foreach (DataRow item in dtItems.Rows)
                    {
                        table.AddCell(new iTextSharp.text.Phrase(num++.ToString(), normalFont));
                        table.AddCell(new iTextSharp.text.Phrase(item["nazvanie"].ToString(), normalFont));
                        table.AddCell(new iTextSharp.text.Phrase(item["EdinIzm"].ToString(), normalFont));
                        table.AddCell(new iTextSharp.text.Phrase(item["count"].ToString(), normalFont));
                        table.AddCell(new iTextSharp.text.Phrase(item["price"].ToString(), normalFont));
                        table.AddCell(new iTextSharp.text.Phrase(item["summa"].ToString(), normalFont));
                    }

                    doc.Add(table);
                    doc.Add(new iTextSharp.text.Paragraph("\n"));

                    // Итого
                    iTextSharp.text.Paragraph total = new iTextSharp.text.Paragraph($"Итого по накладной: {info["total"]} руб.", headerFont);
                    total.Alignment = iTextSharp.text.Element.ALIGN_RIGHT;
                    doc.Add(total);

                    doc.Close();
                }

                MessageBox.Show($"PDF-файл успешно сохранен на рабочий стол:\n{pdfPath}",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                // Опционально: автоматически открыть файл
                System.Diagnostics.Process.Start(pdfPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка генерации PDF: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}