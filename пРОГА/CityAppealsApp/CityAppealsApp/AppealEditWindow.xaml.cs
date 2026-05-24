using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;

namespace AppealsFinal
{
    public partial class AppealEditWindow : Window
    {
        private string connectionString;
        private int? editAppealId;

        public AppealEditWindow(string connString, int? appealId = null)
        {
            InitializeComponent();
            connectionString = connString;
            editAppealId = appealId;
            LoadComboBoxes();
            if (appealId.HasValue)
            {
                Title = "Редактирование обращения";
                LoadAppealData();
            }
            else
                Title = "Добавление нового обращения";
        }

        private void LoadComboBoxes()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    SqlCommand cmdTypes = new SqlCommand("SELECT TypeID, TypeName FROM AppealTypes", conn);
                    DataTable dtTypes = new DataTable();
                    dtTypes.Load(cmdTypes.ExecuteReader());
                    cmbType.ItemsSource = dtTypes.DefaultView;

                    SqlCommand cmdStatus = new SqlCommand("SELECT StatusID, StatusName FROM AppealStatuses", conn);
                    DataTable dtStatus = new DataTable();
                    dtStatus.Load(cmdStatus.ExecuteReader());
                    cmbStatus.ItemsSource = dtStatus.DefaultView;
                }
                if (cmbType.Items.Count > 0) cmbType.SelectedIndex = 0;
                if (cmbStatus.Items.Count > 0) cmbStatus.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки справочников: " + ex.Message);
            }
        }

        private void LoadAppealData()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("SELECT CitizenID, AppealTypeID, StatusID, Content FROM Appeals WHERE AppealID = @id", conn);
                    cmd.Parameters.AddWithValue("@id", editAppealId.Value);
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        txtCitizenID.Text = reader["CitizenID"].ToString();
                        int typeId = (int)reader["AppealTypeID"];
                        int statusId = (int)reader["StatusID"];
                        txtContent.Text = reader["Content"].ToString();

                        foreach (var item in cmbType.Items)
                        {
                            if ((item as DataRowView)["TypeID"].ToString() == typeId.ToString())
                            {
                                cmbType.SelectedItem = item;
                                break;
                            }
                        }
                        foreach (var item in cmbStatus.Items)
                        {
                            if ((item as DataRowView)["StatusID"].ToString() == statusId.ToString())
                            {
                                cmbStatus.SelectedItem = item;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message);
            }
        }

        private bool ValidateInputs()
        {
            bool isValid = true;
            txtCitizenIDError.Text = "";
            txtTypeError.Text = "";
            txtStatusError.Text = "";
            txtContentError.Text = "";

            if (!int.TryParse(txtCitizenID.Text, out int cid) || cid < 1 || cid > 300)
            {
                txtCitizenIDError.Text = "Введите число от 1 до 300";
                isValid = false;
            }
            if (cmbType.SelectedItem == null)
            {
                txtTypeError.Text = "Выберите категорию";
                isValid = false;
            }
            if (cmbStatus.SelectedItem == null)
            {
                txtStatusError.Text = "Выберите статус";
                isValid = false;
            }
            string content = txtContent.Text.Trim();
            if (string.IsNullOrWhiteSpace(content))
            {
                txtContentError.Text = "Введите содержание";
                isValid = false;
            }
            else if (content.Length < 10)
            {
                txtContentError.Text = "Минимум 10 символов";
                isValid = false;
            }
            return isValid;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs())
                return;
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public int CitizenID => int.Parse(txtCitizenID.Text);
        public int AppealTypeID => (int)((DataRowView)cmbType.SelectedItem)["TypeID"];
        public int StatusID => (int)((DataRowView)cmbStatus.SelectedItem)["StatusID"];
        public string Content => txtContent.Text.Trim();
    }
}