using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;
using UtilsNS;

namespace AOMmaster
{
    public class ExtraDO
    {
        public int chn { get; set; }
        public bool check { get; set; }
        public string desc { get; set; }
    } 

    /// <summary>
    /// Interaction logic for extraDIO_UC.xaml
    /// </summary>
    public partial class ExtraDIO_UC : UserControl
    {
        private Hardware hardware;
        private List<ExtraDO> data;
        private DataTable dTable;
        private List<CheckBox> checkBoxes;
        private readonly string configFile = Utils.configPath + "digital.cfg";
        public ExtraDIO_UC()
        {
            InitializeComponent();
        }
        public void Init(ref Hardware _hardware)
        {
            hardware = _hardware;
            data = new List<ExtraDO>();
            dTable = new DataTable();

            DataColumn dc = new DataColumn("Chn", typeof(int));
            dc.ReadOnly = true;
            dTable.Columns.Add(dc);
            dTable.Columns.Add(new DataColumn("Check", typeof(bool)));
            dTable.Columns.Add(new DataColumn("Name", typeof(string)));

            //dTable.RowChanged += new DataRowChangeEventHandler(Row_Changed);
            
            loadFile();
            if (Utils.TheosComputer()) btnUpdate_Click(null, null);
            else
            {
                if (hardware.hwSet) btnUpdate_Click(null, null);
                else Utils.TimedMessageBox("Hardware issue: check connections and devices names.");
            }
            data2table();
            
            dataGrid.ItemsSource = dTable.DefaultView;
            checkBoxes = new List<CheckBox>();
            dTimer = new DispatcherTimer();
            dTimer.Tick += new EventHandler(dTimer_Tick);
            dTimer.Interval = new TimeSpan(0, 0, 1);
            dTimer.Start();          
        }

        private void CheckUncheck(object sender, RoutedEventArgs e)
        {
            var chk = (sender as CheckBox);
            int k = Convert.ToInt32(chk.Tag);
            hardware.WriteSingleOut(data[k].chn,  chk.IsChecked.Value);
            Utils.log(richLog, ">set chn #" + data[k].chn.ToString() + " to " + chk.IsChecked.Value.ToString(), Brushes.Navy);
        }
        private DispatcherTimer dTimer;
        private void dTimer_Tick(object sender, EventArgs e)
        {
            if (dataGrid.Columns.Count < 3) return;
            dataGrid.Columns[2].Width = new DataGridLength(115);
            dataGrid.CanUserAddRows = false;
            for (int k = 0; k<data.Count; k++)
            {
                var chk = DataGridHelper.GetCellByIndices(dataGrid, k, 1).FindVisualChild<CheckBox>();
                chk.Tag = k;
                chk.Checked += new RoutedEventHandler(CheckUncheck); chk.Unchecked += new RoutedEventHandler(CheckUncheck);
                checkBoxes.Add(chk);
            }           
            dTimer.Stop();
        }
        public void Finish()
        {
            table2data();
            saveFile();
        }
        private bool checkAccess() // check if all data channels are in ExtraDIO
        {
            bool bb = true; bool bc;
            int[] allowed = hardware.extraDIO();
            foreach (var itm in data)
            {
                bc = false;
                foreach(var extra in allowed)
                {
                    bc |= extra.Equals(itm.chn);
                }
                bb &= bc;
            }
            return bb;
        }
        public void loadFile()
        {            
            string json = System.IO.File.ReadAllText(configFile);
            data = JsonConvert.DeserializeObject<List<ExtraDO>>(json);
            if (!checkAccess()) Utils.TimedMessageBox("Mismatch of channels from config file to channel available to access");
        }
        public void saveFile()
        {
            string json = JsonConvert.SerializeObject(data);
            System.IO.File.WriteAllText(configFile, json);
        }
        public void data2table()
        {
            dTable.Rows.Clear();
            foreach (ExtraDO p in data)
            {
                dTable.Rows.Add(p.chn, p.check, p.desc);
            }            
        }
        public void table2data()
        {
            data.Clear();
            foreach (DataRow row in dTable.Rows)
            {
                data.Add(new ExtraDO()
                {
                    chn = Convert.ToInt32(row[0]),
                    check = Convert.ToBoolean(row[1]),
                    desc = Convert.ToString(row[2])
                });               
            }
        }
        private void Row_Changed(object sender, DataRowChangeEventArgs e)
        {
            if (e.Action.Equals(DataRowAction.Change))
            {
                hardware.WriteSingleOut(Convert.ToInt32(e.Row["chn"]), Convert.ToBoolean(e.Row["check"]));
                Utils.log(richLog, ">set channel #"+ e.Row["chn"] + " to "+ e.Row["check"], Brushes.Navy);
            }           
        }
        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            foreach (var DO in data)
                hardware.WriteSingleOut(DO.chn, DO.check);
            Utils.log(richLog, ">>set all digital channels", Brushes.Maroon);
        }
    }
}
