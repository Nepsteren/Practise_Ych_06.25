using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Practise_Ych_06._25
{
    /// <summary>
    /// Логика взаимодействия для LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private const string ConnectionString = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=heshing_passwords;Integrated Security=True;TrustServerCertificate=True";        

        public LoginWindow()
        {
            InitializeComponent();
        }

      
       

        

        private void BtnToRegister_Click(object sender, RoutedEventArgs e)
        {
            var registerWindow = new RegistrationWindow();
            registerWindow.Show();
            this.Close();
        }

        private string ComputeHash(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(32);
                return Convert.ToBase64String(hash);
            }
        }
        

        

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtLogin.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                txtError.Text = "Логин и пароль обязательны!";
                return;
            }

            
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    string query = "SELECT PasswordHash, Salt FROM Users WHERE Username = @Username";

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Username", username);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string storedHash = reader["PasswordHash"].ToString();
                                string saltString = reader["Salt"].ToString();
                                byte[] salt = Convert.FromBase64String(saltString);

                                string inputHash = ComputeHash(password, salt);

                                if (inputHash == storedHash)
                                {

                                    string role = ((ComboBoxItem)RoleComboBox.SelectedItem).Content.ToString();
                                    var mainWindow = new MainWindow(role);
                                    mainWindow.Show();
                                    this.Close();
                                }
                            }

                            
                            txtError.Text = "Неверный логин или пароль!";

                            
                        }
                    }

                    
                }
            }
            catch (Exception ex)
            {
                txtError.Text = $"Ошибка входа: {ex.Message}";
            }
        }
    }
}
