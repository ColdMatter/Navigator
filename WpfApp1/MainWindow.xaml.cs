using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
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


namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        DataTable dt = new DataTable();
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
            DataColumn dc1 = new DataColumn("Value1", typeof(double));
            DataColumn dc2 = new DataColumn("Value2", typeof(double));
            DataColumn dc3 = new DataColumn("Value3", typeof(double));

            dt.Columns.Add(dc1);
            dt.Columns.Add(dc2);
            dt.Columns.Add(dc3);

            dataGrid.ItemsSource = dt.DefaultView;

            dt.Rows.Add(1.5, 2.5, 3.5);
            dt.Rows.Add(1.5, 2.5, null);
            dt.Rows.Add(1.5, 2.5, 3.5);

        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            foreach (DataRow row in dt.Rows)
                textBox.Text += row[0].ToString() + " ; " + row[1].ToString() + " ; " + row[2].ToString()+"\n";
        }
    }
}
