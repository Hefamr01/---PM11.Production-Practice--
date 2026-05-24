using System;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Windows;
using BCrypt.Net;

namespace AppealsFinal
{
    public partial class RegisterWindow : Window
    {
        private string connectionString;

        public RegisterWindow(string connString)
        {
            InitializeComponent();
            connectionString = connString;
        }

        private bool ValidateInputs()
        {
            bool isValid = true;
            txtLastNameError.Text = "";
            txtFirstNameError.Text = "";
            txtAddressError.Text = "";
            txtEmailError.Text = "";
            txtPasswordError.Text = "";

            if (string.IsNullOrWhiteSpace(txtLastName.Text))
            {
                txtLastNameError.Text = "Введите фамилию";
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(txtFirstName.Text))
            {
                txtFirstNameError.Text = "Введите имя";
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(txtAddress.Text))
            {
                txtAddressError.Text = "Введите адрес";
                isValid = false;
            }

            string email = txtEmail.Text.Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                txtEmailError.Text = "Введите email";
                isValid = false;
            }
            else if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                txtEmailError.Text = "Некорректный email";
                isValid = false;
            }
            else
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Citizens WHERE Email = @email", conn);
                    cmd.Parameters.AddWithValue("@email", email);
                    int count = (int)cmd.ExecuteScalar();
                    if (count > 0)
                    {
                        txtEmailError.Text = "Email уже зарегистрирован";
                        isValid = false;
                    }
                }
            }

            string password = txtPassword.Password;
            if (string.IsNullOrWhiteSpace(password))
            {
                txtPasswordError.Text = "Введите пароль";
                isValid = false;
            }
            else if (password.Length < 6)
            {
                txtPasswordError.Text = "Пароль не менее 6 символов";
                isValid = false;
            }
            return isValid;
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs())
                return;

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(txtPassword.Password);

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"
                        INSERT INTO Citizens (LastName, FirstName, Patronymic, Address, Phone, Email, PasswordHash)
                        VALUES (@ln, @fn, @patr, @addr, @phone, @email, @pwd)";
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@ln", txtLastName.Text.Trim());
                    cmd.Parameters.AddWithValue("@fn", txtFirstName.Text.Trim());
                    cmd.Parameters.AddWithValue("@patr", string.IsNullOrWhiteSpace(txtPatronymic.Text) ? (object)DBNull.Value : txtPatronymic.Text.Trim());
                    cmd.Parameters.AddWithValue("@addr", txtAddress.Text.Trim());
                    cmd.Parameters.AddWithValue("@phone", string.IsNullOrWhiteSpace(txtPhone.Text) ? (object)DBNull.Value : txtPhone.Text.Trim());
                    cmd.Parameters.AddWithValue("@email", txtEmail.Text.Trim());
                    cmd.Parameters.AddWithValue("@pwd", hashedPassword);
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Регистрация успешна!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}