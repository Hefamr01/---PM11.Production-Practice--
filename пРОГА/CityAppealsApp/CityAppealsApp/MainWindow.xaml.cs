using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace AppealsFinal
{
    public partial class MainWindow : Window
    {
        // ИЗМЕНИТЕ СТРОКУ ПОДКЛЮЧЕНИЯ ПОД ВАШУ БД
        private string connectionString = @"Server=11WINDOWA\SQLEXPRESS;Database=AdminAppealsDB;Integrated Security=True;";

        private string currentRole = "HeadOfDepartment";
        private int currentPage = 1;
        private int pageSize = 20;
        private int totalPages = 1;
        private string currentFilter = "Все";

        private DateTime? filterDateFrom = null;
        private DateTime? filterDateTo = null;
        private string searchCitizen = "";
        private string searchContent = "";

        public MainWindow()
        {
            InitializeComponent();
            LoadStatusFilter();
            LoadAppeals();
            UpdateUIBasedOnRole();
        }

        private void LoadStatusFilter()
        {
            try
            {
                cmbStatusFilter.Items.Clear();
                cmbStatusFilter.Items.Add("Все");
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("SELECT StatusName FROM AppealStatuses", conn);
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        cmbStatusFilter.Items.Add(reader["StatusName"].ToString());
                    }
                }
            }
            catch
            {
                cmbStatusFilter.Items.Add("Новое");
                cmbStatusFilter.Items.Add("В работе");
                cmbStatusFilter.Items.Add("Выполнено");
                cmbStatusFilter.Items.Add("Отклонено");
                cmbStatusFilter.Items.Add("На доработке");
            }
            cmbStatusFilter.SelectedIndex = 0;
        }

        private void LoadAppeals()
        {
            try
            {
                string filterStatus = currentFilter != "Все" ? currentFilter : null;
                string filterSql = "";
                if (!string.IsNullOrEmpty(filterStatus)) filterSql += " AND s.StatusName = @status";
                if (filterDateFrom.HasValue) filterSql += " AND a.AppealDate >= @dateFrom";
                if (filterDateTo.HasValue) filterSql += " AND a.AppealDate <= @dateTo";
                if (!string.IsNullOrEmpty(searchCitizen)) filterSql += " AND (c.LastName LIKE @searchCitizen OR c.FirstName LIKE @searchCitizen)";
                if (!string.IsNullOrEmpty(searchContent)) filterSql += " AND a.Content LIKE @searchContent";

                string countQuery = $@"
                    SELECT COUNT(*)
                    FROM Appeals a
                    JOIN Citizens c ON a.CitizenID = c.CitizenID
                    JOIN AppealTypes t ON a.AppealTypeID = t.TypeID
                    JOIN AppealStatuses s ON a.StatusID = s.StatusID
                    LEFT JOIN Employees e ON a.EmployeeID = e.EmployeeID
                    WHERE 1=1 {filterSql}";

                int totalRows = 0;
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    SqlCommand countCmd = new SqlCommand(countQuery, conn);
                    if (!string.IsNullOrEmpty(filterStatus)) countCmd.Parameters.AddWithValue("@status", filterStatus);
                    if (filterDateFrom.HasValue) countCmd.Parameters.AddWithValue("@dateFrom", filterDateFrom.Value);
                    if (filterDateTo.HasValue) countCmd.Parameters.AddWithValue("@dateTo", filterDateTo.Value);
                    if (!string.IsNullOrEmpty(searchCitizen)) countCmd.Parameters.AddWithValue("@searchCitizen", $"%{searchCitizen}%");
                    if (!string.IsNullOrEmpty(searchContent)) countCmd.Parameters.AddWithValue("@searchContent", $"%{searchContent}%");
                    totalRows = (int)countCmd.ExecuteScalar();
                }

                totalPages = (int)Math.Ceiling((double)totalRows / pageSize);
                if (totalPages == 0) totalPages = 1;
                if (currentPage > totalPages) currentPage = totalPages;
                if (currentPage < 1) currentPage = 1;

                txtTotalPages.Text = totalPages.ToString();
                txtPageNumber.Text = currentPage.ToString();

                int offset = (currentPage - 1) * pageSize;

                string query = $@"
                    SELECT a.AppealID, 
                           c.LastName + ' ' + c.FirstName + ISNULL(' ' + c.Patronymic, '') AS CitizenFullName,
                           t.TypeName AS AppealType,
                           a.Content,
                           s.StatusName AS Status,
                           a.AppealDate,
                           a.ResponseDeadline,
                           e.FullName AS ExecutorName
                    FROM Appeals a
                    JOIN Citizens c ON a.CitizenID = c.CitizenID
                    JOIN AppealTypes t ON a.AppealTypeID = t.TypeID
                    JOIN AppealStatuses s ON a.StatusID = s.StatusID
                    LEFT JOIN Employees e ON a.EmployeeID = e.EmployeeID
                    WHERE 1=1 {filterSql}
                    ORDER BY a.AppealDate DESC
                    OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(query, conn);
                    if (!string.IsNullOrEmpty(filterStatus)) cmd.Parameters.AddWithValue("@status", filterStatus);
                    if (filterDateFrom.HasValue) cmd.Parameters.AddWithValue("@dateFrom", filterDateFrom.Value);
                    if (filterDateTo.HasValue) cmd.Parameters.AddWithValue("@dateTo", filterDateTo.Value);
                    if (!string.IsNullOrEmpty(searchCitizen)) cmd.Parameters.AddWithValue("@searchCitizen", $"%{searchCitizen}%");
                    if (!string.IsNullOrEmpty(searchContent)) cmd.Parameters.AddWithValue("@searchContent", $"%{searchContent}%");
                    cmd.Parameters.AddWithValue("@offset", offset);
                    cmd.Parameters.AddWithValue("@pageSize", pageSize);
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    adapter.Fill(dt);
                }
                dgAppeals.ItemsSource = dt.DefaultView;
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки: " + ex.Message);
            }
        }

        private void UpdateStatistics()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT s.StatusName, COUNT(a.AppealID) AS Count
                        FROM Appeals a
                        JOIN AppealStatuses s ON a.StatusID = s.StatusID
                        GROUP BY s.StatusName";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        string status = reader["StatusName"].ToString();
                        int count = Convert.ToInt32(reader["Count"]);
                        if (status.Contains("Новое")) txtStatNew.Text = $"📌 Новых: {count}";
                        else if (status.Contains("В работе")) txtStatInWork.Text = $"⚙️ В работе: {count}";
                        else if (status.Contains("Выполнено")) txtStatCompleted.Text = $"✅ Выполнено: {count}";
                        else if (status.Contains("Отклонено")) txtStatRejected.Text = $"❌ Отклонено: {count}";
                    }
                }
            }
            catch { }
        }

        private void UpdateUIBasedOnRole()
        {
            // Если кнопки ещё не созданы – просто выходим
            if (btnAdd == null || btnUpdate == null || btnDelete == null)
                return;

            switch (currentRole)
            {
                case "HeadOfDepartment":
                    btnAdd.IsEnabled = true;
                    btnUpdate.IsEnabled = true;
                    btnDelete.IsEnabled = true;
                    break;
                case "Operator":
                    btnAdd.IsEnabled = true;
                    btnUpdate.IsEnabled = false;
                    btnDelete.IsEnabled = false;
                    break;
                case "Executor":
                    btnAdd.IsEnabled = false;
                    btnUpdate.IsEnabled = true;
                    btnDelete.IsEnabled = false;
                    break;
                default:
                    btnAdd.IsEnabled = false;
                    btnUpdate.IsEnabled = false;
                    btnDelete.IsEnabled = false;
                    break;
            }
        }
        private void CmbRoles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbRoles.SelectedItem is ComboBoxItem roleItem)
            {
                currentRole = roleItem.Content.ToString();
                UpdateUIBasedOnRole();
            }
        }

        private void CmbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbStatusFilter.SelectedItem != null)
            {
                currentFilter = cmbStatusFilter.SelectedItem.ToString();
                currentPage = 1;
                LoadAppeals();
            }
        }

        private void DateFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            filterDateFrom = dpDateFrom.SelectedDate;
            filterDateTo = dpDateTo.SelectedDate;
            currentPage = 1;
            LoadAppeals();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            searchCitizen = txtSearchCitizen.Text;
            searchContent = txtSearchContent.Text;
            currentPage = 1;
            LoadAppeals();
        }

        private void BtnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            txtSearchCitizen.Clear();
            txtSearchContent.Clear();
            dpDateFrom.SelectedDate = null;
            dpDateTo.SelectedDate = null;
            filterDateFrom = null;
            filterDateTo = null;
            searchCitizen = "";
            searchContent = "";
            currentPage = 1;
            LoadAppeals();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => LoadAppeals();

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "CSV файлы (*.csv)|*.csv",
                DefaultExt = "csv",
                FileName = $"Обращения_{DateTime.Now:yyyyMMdd_HHmmss}"
            };
            if (saveDialog.ShowDialog() == true)
            {
                string filePath = saveDialog.FileName;
                var data = dgAppeals.ItemsSource as DataView;
                if (data == null) return;

                using (StreamWriter sw = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
                {
                    sw.WriteLine("ID;Гражданин;Тип;Статус;Дата;Срок;Исполнитель;Содержание");
                    foreach (DataRowView row in data)
                    {
                        sw.WriteLine($"{row["AppealID"]};{row["CitizenFullName"]};{row["AppealType"]};{row["Status"]};{row["AppealDate"]};{row["ResponseDeadline"]};{row["ExecutorName"]};{row["Content"]}");
                    }
                }
                MessageBox.Show($"Экспорт завершён!\nФайл: {filePath}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void FirstPage_Click(object sender, RoutedEventArgs e) { currentPage = 1; LoadAppeals(); }
        private void PrevPage_Click(object sender, RoutedEventArgs e) { if (currentPage > 1) currentPage--; LoadAppeals(); }
        private void NextPage_Click(object sender, RoutedEventArgs e) { if (currentPage < totalPages) currentPage++; LoadAppeals(); }
        private void LastPage_Click(object sender, RoutedEventArgs e) { currentPage = totalPages; LoadAppeals(); }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AppealEditWindow(connectionString);
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        string sql = @"
                            INSERT INTO Appeals (CitizenID, AppealTypeID, StatusID, AppealDate, Content, ResponseDeadline)
                            VALUES (@cid, @tid, @sid, GETDATE(), @content, DATEADD(day, 30, GETDATE()))";
                        SqlCommand cmd = new SqlCommand(sql, conn);
                        cmd.Parameters.AddWithValue("@cid", dialog.CitizenID);
                        cmd.Parameters.AddWithValue("@tid", dialog.AppealTypeID);
                        cmd.Parameters.AddWithValue("@sid", dialog.StatusID);
                        cmd.Parameters.AddWithValue("@content", dialog.Content);
                        cmd.ExecuteNonQuery();
                    }
                    LoadAppeals();
                    MessageBox.Show("Обращение добавлено", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (dgAppeals.SelectedItem == null)
            {
                MessageBox.Show("Выберите обращение", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var row = (DataRowView)dgAppeals.SelectedItem;
            int appealId = Convert.ToInt32(row["AppealID"]);

            var dialog = new AppealEditWindow(connectionString, appealId);
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        string sql = @"
                            UPDATE Appeals 
                            SET CitizenID = @cid, AppealTypeID = @tid, StatusID = @sid, Content = @content
                            WHERE AppealID = @aid";
                        SqlCommand cmd = new SqlCommand(sql, conn);
                        cmd.Parameters.AddWithValue("@cid", dialog.CitizenID);
                        cmd.Parameters.AddWithValue("@tid", dialog.AppealTypeID);
                        cmd.Parameters.AddWithValue("@sid", dialog.StatusID);
                        cmd.Parameters.AddWithValue("@content", dialog.Content);
                        cmd.Parameters.AddWithValue("@aid", appealId);
                        cmd.ExecuteNonQuery();
                    }
                    LoadAppeals();
                    MessageBox.Show("Обращение обновлено", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgAppeals.SelectedItem == null)
            {
                MessageBox.Show("Выберите обращение", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (MessageBox.Show("Удалить выбранное обращение?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var row = (DataRowView)dgAppeals.SelectedItem;
                int appealId = Convert.ToInt32(row["AppealID"]);
                try
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        SqlCommand cmd = new SqlCommand("DELETE FROM Appeals WHERE AppealID = @id", conn);
                        cmd.Parameters.AddWithValue("@id", appealId);
                        cmd.ExecuteNonQuery();
                    }
                    LoadAppeals();
                    MessageBox.Show("Удалено", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnRegisterCitizen_Click(object sender, RoutedEventArgs e)
        {
            var regWindow = new RegisterWindow(connectionString);
            regWindow.Owner = this;
            regWindow.ShowDialog();
        }

        private void DgAppeals_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
    }
}