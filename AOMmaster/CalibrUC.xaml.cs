using System;
using System.Collections.Generic;
using System.Windows.Threading;
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
using UtilsNS;
using Microsoft.VisualBasic.Logging;

namespace AOMmaster
{
    /// <summary>
    /// Interaction logic for CalibrUC.xaml
    /// </summary>
    public partial class CalibrUC : UserControl
    {
        private DataTable dt; 
        public CalibrUC()
        {
            InitializeComponent();
            dt = new DataTable();
            
            DataColumn dc1 = new DataColumn("__Volts__", typeof(double));
            DataColumn dc2 = new DataColumn("__Units__", typeof(double));

            dt.Columns.Add(dc1);
            dt.Columns.Add(dc2);

            dgCalibr.ItemsSource = dt.DefaultView;
        }
        public AnalogConfig analogConfig;
        public void Init(AnalogConfig ac) // update from ac
        {
            lbSemi.Content = ac.groupTitle + " : " + ac.title +" ["+ac.minVolt.ToString("G3")+" .. "+ ac.maxVolt.ToString("G3")+"]";
            analogConfig = new AnalogConfig();
            analogConfig.Assign(ac);
            cal2table();
        }
        private void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = ""; // Default file name
            dlg.DefaultExt = ".cal"; // Default file extension
            dlg.InitialDirectory = Utils.basePath + "\\calibration\\";
            dlg.Filter = "Calibration (.cal)|*.cal"; // Filter files by extension

            if (dlg.ShowDialog() == false) return;
            analogConfig.ReadCalibr(dlg.FileName);
            cal2table();
        }
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = ""; // Default file name
            dlg.DefaultExt = ".cal"; // Default file extension
            dlg.InitialDirectory = Utils.basePath + "\\calibration\\";
            dlg.Filter = "Calibration (.cal)|*.cal"; // Filter files by extension

            if (dlg.ShowDialog() == false) return;
            table2cal();
            analogConfig.WriteCalibr(dlg.FileName);
        }
        private void cal2table() // cal -> analogConfig; table -> dt
        {
            dt.Rows.Clear();
            foreach (Point p in analogConfig.calibr)
            {
                dt.Rows.Add(p.X, p.Y);
            }
            tbUnits.Text = analogConfig.calUnit;
        }
        private void table2cal() // cal -> analogConfig; table -> dt
        {           
            analogConfig.calibr.Clear();
            /*dgCalibr.ItemsSource
                dt.DefaultView.Data;
            dt.Up*/
            foreach (DataRow row in dt.Rows)
            {
                double x, y;
                string xStr = Convert.ToString(row[0]); string yStr = Convert.ToString(row[1]);          
                if (!Double.TryParse(xStr, out x)) 
                {
                    Utils.TimedMessageBox("Problem with row:" + xStr+" | "+yStr, "Conversion problem", 2500); continue;
                }               
                if (!Double.TryParse(yStr, out y))
                {
                    Utils.TimedMessageBox("Problem with row:" + xStr + " | " + yStr, "Conversion problem", 2500); continue;
                }
                analogConfig.calibr.Add(new Point(x,y));
            }
            analogConfig.calUnit = tbUnits.Text;
        }
        private void Sort()
        {
            dt.DefaultView.Sort = "__Volts__";
        }
        public void insertValue(double volt)
        {
            dt.Rows.Add(volt,null);
            Sort();
        }
        private void btnDone_Click(object sender, RoutedEventArgs e)
        {
            table2cal();
        }
        private DispatcherTimer dTimer;
        private void dgCalibr_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (Convert.ToString(e.Column.Header) != "__Volts__") return;
            dTimer = new DispatcherTimer();
            dTimer.Tick += new EventHandler(dTimer_Tick);
            dTimer.Interval = new TimeSpan(0, 0, 1);
            dTimer.Start();
        }
        private void dTimer_Tick(object sender, EventArgs e)
        {
            Sort();
            dTimer.Stop();           
        }
    }
}
