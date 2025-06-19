using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.Win32;
using System.IO;
using OfficeOpenXml;
using System.ComponentModel;

namespace Practise_Ych_06._25
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private LeasingCompany_2Entities _context = new LeasingCompany_2Entities();



        private string role;

        private readonly string _role;

        public MainWindow()
        {
            InitializeComponent();
            LoadData();
        }
        public MainWindow(string role)
        {
            InitializeComponent();
            _role = role;
            LoadData();
            AdjustTabsForRole();
        }

        public void AdjustTabsForRole()
        {
            switch (_role)
            {
                case "Admin":
                    
                    break;

                case "Employee":
                    HideTab("Финансовая аналитика");
                    HideTab("Резервное копирование");
                    break;

                case "Finance":
                    HideTab("Активные контракты");
                    HideTab("Создать контракт");
                    break;
            }
        }

        public void HideTab(string header)
        {
            foreach (var item in MainTabControl.Items)
            {
                if (item is TabItem tab && tab.Header.ToString() == header)
                {
                    tab.Visibility = Visibility.Collapsed;
                    break;
                }
            }
        }




        private void LoadData()
        {
            try
            {
                
                var activeContracts = _context.vw_ActiveContractsWithDetails.ToList();
                ActiveContractsGrid.ItemsSource = activeContracts;

                
                ClientComboBox.ItemsSource = _context.Clients.ToList();

               
                ProductComboBox.ItemsSource = _context.Products
                    .Where(p => p.status == "Available")
                    .ToList();

                
                EmployeeComboBox.ItemsSource = _context.Employees
                    .Select(e => new {
                        e.employee_id,
                        FullName = e.first_name + " " + e.last_name
                    })
                    .ToList();

                
                ProductAnalyticsGrid.ItemsSource = _context.vw_ProductFinancialAnalytics.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateContractButton_Click(object sender, RoutedEventArgs e)
        {
            
            if (ClientComboBox.SelectedItem == null || ProductComboBox.SelectedItem == null ||
                EmployeeComboBox.SelectedItem == null || StartDatePicker.SelectedDate == null ||
                EndDatePicker.SelectedDate == null || string.IsNullOrWhiteSpace(MonthlyPaymentTextBox.Text))
            {
                MessageBox.Show("Заполните все поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            
            if (!decimal.TryParse(MonthlyPaymentTextBox.Text, out decimal monthlyPayment) || monthlyPayment <= 0)
            {
                MessageBox.Show("Введите корректную сумму платежа", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            
            if (StartDatePicker.SelectedDate >= EndDatePicker.SelectedDate)
            {
                MessageBox.Show("Дата окончания должна быть позже даты начала", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                
                dynamic client = ClientComboBox.SelectedItem;
                dynamic product = ProductComboBox.SelectedItem;
                dynamic employee = EmployeeComboBox.SelectedItem;

                int clientId = client.client_id;
                int productId = product.product_id;
                int employeeId = employee.employee_id;
                DateTime startDate = StartDatePicker.SelectedDate.Value;
                DateTime endDate = EndDatePicker.SelectedDate.Value;

                
                var resultParam = new SqlParameter("@result", SqlDbType.Int) { Direction = ParameterDirection.Output };
                var messageParam = new SqlParameter("@message", SqlDbType.NVarChar, 255) { Direction = ParameterDirection.Output };

                _context.Database.ExecuteSqlCommand(
                    "EXEC sp_CreateLeasingContract @client_id, @product_id, @start_date, @end_date, " +
                    "@monthly_payment, @sales_rep_id, @result OUTPUT, @message OUTPUT",
                    new SqlParameter("@client_id", clientId),
                    new SqlParameter("@product_id", productId),
                    new SqlParameter("@start_date", startDate),
                    new SqlParameter("@end_date", endDate),
                    new SqlParameter("@monthly_payment", monthlyPayment),
                    new SqlParameter("@sales_rep_id", employeeId),
                    resultParam,
                    messageParam
                );

                int result = (int)resultParam.Value;
                string message = messageParam.Value.ToString();

                ContractResultText.Text = message;

                if (result == 1)
                {
                    
                    LoadData();
                    ClearForm();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании контракта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearForm()
        {
            ClientComboBox.SelectedItem = null;
            ProductComboBox.SelectedItem = null;
            EmployeeComboBox.SelectedItem = null;
            StartDatePicker.SelectedDate = null;
            EndDatePicker.SelectedDate = null;
            MonthlyPaymentTextBox.Text = string.Empty;
        }

        private void CalculateRevenueButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(MonthsTextBox.Text, out int months) && months > 0)
            {
                try
                {
                    
                    var revenueData = _context.Database.SqlQuery<RevenueProjection>(
                        "SELECT * FROM fn_CalculateProjectedRevenue(@months)",
                        new SqlParameter("@months", months)
                    ).ToList();

                    RevenueGrid.ItemsSource = revenueData;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при расчете дохода: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Введите корректное количество месяцев", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _context?.Dispose();
        }


        private void BackupDatabase_Click(object sender, EventArgs e)
        {           

            var saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Backup Files (*.bak)|*.bak";
            if (saveDialog.ShowDialog() != true) return;

            string backupPath = saveDialog.FileName;

            using (var connection = new SqlConnection(@"Data Source=DESKTOP-VUBCREJ\SQLEXPRESS;Initial Catalog=LeasingCompany;Integrated Security=True"))
            {
                connection.Open();
                var command = new SqlCommand(
                    $"BACKUP DATABASE [LeasingCompany] TO DISK = '{backupPath}' WITH FORMAT, INIT",
                    connection
                );
                command.ExecuteNonQuery();
                MessageBox.Show("Резервная копия была создана");
            }
        }
        
        private void RestoreDatabase_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog();
            openDialog.Filter = "Backup Files (*.bak)|*.bak";
            if (openDialog.ShowDialog() != true) return;

            
            string backupPath = openDialog.FileName;

            using (var connection = new SqlConnection(
                @"Data Source=DESKTOP-VUBCREJ\SQLEXPRESS;Initial Catalog=master;Integrated Security=True"))
            {
                try
                {
                    connection.Open();

                    
                    var killConnections = new SqlCommand(@"
                ALTER DATABASE [LeasingCompany] SET SINGLE_USER WITH ROLLBACK IMMEDIATE",
                        connection);
                    killConnections.ExecuteNonQuery();

                    
                    var restoreCommand = new SqlCommand($@"
                RESTORE DATABASE [LeasingCompany] FROM DISK = '{backupPath}' WITH REPLACE",
                        connection);
                    restoreCommand.ExecuteNonQuery();

                    
                    var multiUser = new SqlCommand(@"
                ALTER DATABASE [LeasingCompany] SET MULTI_USER",
                        connection);
                    multiUser.ExecuteNonQuery();

                    MessageBox.Show("Данные восстановлены!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}");
                }
            }
        }

        private void ExportToCsv_Click(object sender, RoutedEventArgs e)
        {
            

            var saveDialog = new SaveFileDialog
            {
                Filter = "CSV файлы (*.csv)|*.csv",
                FileName = "ActiveContracts.csv"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    using (var writer = new StreamWriter(saveDialog.FileName, false, Encoding.UTF8))
                    {
                        var contracts = ActiveContractsGrid.ItemsSource.Cast<dynamic>();

                        
                        writer.WriteLine("ID контракта,Клиент,Продукт,Категория,Ежемесячный платеж,Осталось месяцев");

                        foreach (var contract in contracts)
                        {
                            writer.WriteLine($"{contract.contract_id},{contract.client_name},{contract.product_name}," +
                                $"{contract.category},{contract.monthly_payment},{contract.months_remaining}");
                        }

                        MessageBox.Show("Данные успешно экспортированы в CSV");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка экспорта: {ex.Message}");
                }
            }
        }

        

       private void ExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx"
            };
            if (saveDialog.ShowDialog() != true) return;

            var contracts = ActiveContractsGrid.ItemsSource.Cast<dynamic>().ToList();

            
            OfficeOpenXml.ExcelPackage.License.SetNonCommercialPersonal("Nikita");

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Контракты");

               
                worksheet.Cells[1, 1].Value = "ID контракта";
                worksheet.Cells[1, 2].Value = "Клиент";
                worksheet.Cells[1, 3].Value = "Продукт";
                worksheet.Cells[1, 4].Value = "Категория";
                worksheet.Cells[1, 5].Value = "Ежемесячный платеж";
                worksheet.Cells[1, 6].Value = "Осталось месяцев";

                
                for (int i = 0; i < contracts.Count; i++)
                {
                    var contract = contracts[i];
                    worksheet.Cells[i + 2, 1].Value = contract.contract_id;
                    worksheet.Cells[i + 2, 2].Value = contract.client_name;
                    worksheet.Cells[i + 2, 3].Value = contract.product_name;
                    worksheet.Cells[i + 2, 4].Value = contract.category;
                    worksheet.Cells[i + 2, 5].Value = contract.monthly_payment;
                    worksheet.Cells[i + 2, 6].Value = contract.months_remaining;
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                File.WriteAllBytes(saveDialog.FileName, package.GetAsByteArray());
                MessageBox.Show("Данные экспортированы в Excel.");
            }
        }

        private void ImportFromExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OfficeOpenXml.ExcelPackage.License.SetNonCommercialPersonal("Nikita");

                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Excel файлы (*.xlsx)|*.xlsx"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var contracts = new List<ContractViewModel>();

                    using (var package = new ExcelPackage(new FileInfo(openFileDialog.FileName)))
                    {
                        var worksheet = package.Workbook.Worksheets.First();
                        int rowCount = worksheet.Dimension.Rows;

                        for (int row = 2; row <= rowCount; row++)
                        {
                            contracts.Add(new ContractViewModel
                            {
                                contract_id = int.Parse(worksheet.Cells[row, 1].Text),
                                client_name = worksheet.Cells[row, 2].Text,
                                product_name = worksheet.Cells[row, 3].Text,
                                category = worksheet.Cells[row, 4].Text,
                                monthly_payment = decimal.Parse(worksheet.Cells[row, 5].Text),
                                months_remaining = int.Parse(worksheet.Cells[row, 6].Text)
                            });
                        }
                    }

                    ActiveContractsGrid.ItemsSource = contracts;
                    MessageBox.Show("Импорт из Excel завершён!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка импорта: {ex.Message}");
            }
        }

        private void ImportFromCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "CSV файлы (*.csv)|*.csv"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var lines = File.ReadAllLines(openFileDialog.FileName, Encoding.UTF8);
                    var contracts = new List<ContractViewModel>();

                    for (int i = 1; i < lines.Length; i++) 
                    {
                        var values = lines[i].Split(',');

                        contracts.Add(new ContractViewModel
                        {
                            contract_id = int.Parse(values[0]),
                            client_name = values[1],
                            product_name = values[2],
                            category = values[3],
                            monthly_payment = decimal.Parse(values[4]),
                            months_remaining = int.Parse(values[5])
                        });
                    }

                    ActiveContractsGrid.ItemsSource = contracts;
                    MessageBox.Show("Импорт из CSV завершён!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка импорта: {ex.Message}");
            }
        }





    }

    public class RevenueProjection
    {
        public int client_id { get; set; }
        public string client_name { get; set; }
        public decimal projected_revenue { get; set; }
    }
}
