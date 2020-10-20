using System;
using System.Collections.Generic;
using System.Windows.Threading;
using System.IO;
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
using Newtonsoft.Json;
using Microsoft.VisualBasic;
using UtilsNS;
using System.Data.SqlTypes;

namespace AOMmaster
{
    /// <summary>
    /// Interaction logic for AOMmasterUC.xaml
    /// </summary>
    public partial class AOMmasterUC : UserControl
    {
        Dictionary<string, SemiAxisUC> semis;
        Hardware hardware;
        public AOMmasterUC()
        {
            InitializeComponent();
            semis = new Dictionary<string, SemiAxisUC>()
            {
                ["2DMOT"] = SemiAxisUC0,
                ["Push"] = SemiAxisUC1,
                ["X-positive"] = SemiAxisUC2,
                ["X-negative"] = SemiAxisUC3,
                ["Y-positive"] = SemiAxisUC4,
                ["Y-negative"] = SemiAxisUC5,
                ["Z-positive"] = SemiAxisUC6,
                ["Z-negative"] = SemiAxisUC7
            };
        }
        Dictionary<string, Dictionary<string, string>> semiValues
        {
            get 
            {
                Dictionary<string, Dictionary<string, string>> sv = new Dictionary<string, Dictionary<string, string>>();
                foreach (KeyValuePair<string, SemiAxisUC> semi in semis)
                {
                    sv[semi.Key] = new Dictionary<string, string>()
                        { ["Enabled"] = semi.Value.Enabled.ToString(), 
                        ["VCO-volt"] = semi.Value.VCO_V.ToString("G5"), ["VCA-volt"] = semi.Value.VCA_V.ToString("G5"),
                        ["VCO-unit"] = semi.Value.VCO_U.ToString(), ["VCA-unit"] = semi.Value.VCA_U.ToString()
                    };
                }
                return sv;
            }
            set
            {
                foreach (KeyValuePair<string, SemiAxisUC> semi in semis)
                {
                    semi.Value.Enabled = Convert.ToBoolean(value[semi.Key]["Enabled"]);
                    semi.Value.VCO_U = false; semi.Value.VCA_U = false; // set to volts
                    semi.Value.VCO_V = Convert.ToDouble(value[semi.Key]["VCO-volt"]); semi.Value.VCA_V = Convert.ToDouble(value[semi.Key]["VCA-volt"]);
                    semi.Value.VCO_U = Convert.ToBoolean(value[semi.Key]["VCO-unit"]); semi.Value.VCA_U = Convert.ToBoolean(value[semi.Key]["VCA-unit"]);
                }
            }
        }
        public bool readSemiValues(string fn)
        {
            string ffn = System.IO.Path.ChangeExtension(fn, ".stg");
            if (File.Exists(ffn))
            {
                string json = System.IO.File.ReadAllText(ffn);
                semiValues = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json);
                return true;
            }
            else return false;
        }
        public void writeSemiValues(string fn)
        {
            string json = JsonConvert.SerializeObject(semiValues);
            System.IO.File.WriteAllText(System.IO.Path.ChangeExtension(fn,".stg"), json);
        }
        public void log(string txt, bool detail)
        {
            if (txt.Substring(0, 5) == "Error")
            {
                Utils.log(tbLogger, txt, Brushes.Red.Color); return;
            }
            if (!chkLog.IsChecked.Value) return;
            Color clr = detail ? Brushes.DarkGreen.Color : Brushes.Navy.Color;
            if (detail)
            {
                if (chkDetailed.IsChecked.Value) Utils.log(tbLogger, txt, clr);
            }               
            else Utils.log(tbLogger, txt, clr);
        }
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            tbLogger.Document.Blocks.Clear();
        }
        public void Init()
        {
            hardware = new Hardware();
            hardware.configHardware("Dev3", "Dev2", -10, 10);
            int i = 0; Color clr = Brushes.Black.Color; 
            AnalogConfig vco = new AnalogConfig(); vco.title = "VCO"; vco.calibr = new List<Point>();
            AnalogConfig vca = new AnalogConfig(); vca.title = "VCA"; vca.calibr = new List<Point>();
            foreach (KeyValuePair<string, SemiAxisUC> semi in semis)
            {
                vco.groupTitle = semi.Key; vca.groupTitle = semi.Key;
                vco.chnNumb = 4+i; vca.chnNumb = 20+i; 
                switch (i)
                {
                    case 0:
                        clr = Brushes.Goldenrod.Color;
                        break;
                    case 1:
                        clr = Brushes.OrangeRed.Color;
                        break;
                    case 2:
                    case 3:
                        clr = Brushes.Teal.Color;
                        break;
                    case 4:
                    case 5:
                        clr = Brushes.LimeGreen.Color;
                        break;
                    case 6:
                    case 7:
                        clr = Brushes.Blue.Color;
                        break;
                }
                vco.minVolt = hardware.analogMin; vca.minVolt = hardware.analogMin;
                vco.maxVolt = hardware.analogMax; vca.maxVolt = hardware.analogMax;
                
                semi.Value.analogVCO.OnAnalogChanged += new AnalogUC.AnalogChangedHandler(hardware.AnalogOut);
                semi.Value.analogVCA.OnAnalogChanged += new AnalogUC.AnalogChangedHandler(hardware.AnalogOut);
                semi.Value.OnDigitalChanged += new SemiAxisUC.DigitalChangedHandler(hardware.WriteSingleOut);
                semi.Value.OnLog += new SemiAxisUC.LogHandler(log);
                semi.Value.analogVCO.OnLog += new AnalogUC.LogHandler(log);
                semi.Value.analogVCA.OnLog += new AnalogUC.LogHandler(log);
                semi.Value.analogVCO.OnSelect += new AnalogUC.SelectHandler(setCalibration);
                semi.Value.analogVCA.OnSelect += new AnalogUC.SelectHandler(setCalibration);

                semi.Value.Config(semi.Key, i, vco, vca, clr);
                i++;
            }
            hardware.OnLog += new Hardware.LogHandler(log);
            CalibrUC1.btnDone.Click += new RoutedEventHandler(getCalibration);
            CalibrUC1.btnCancel.Click += new RoutedEventHandler(getCalibration);
            CalibrUC1.btnInsertVal.Click += new RoutedEventHandler(insertValue);
        }
        readonly string lastSettingFile = "default.stg";
        private DispatcherTimer dTimer;
        private void AOMmasterUC0_Loaded(object sender, RoutedEventArgs e)
        {
            Init();
            dTimer = new DispatcherTimer();
            dTimer.Tick += new EventHandler(dTimer_Tick);
            dTimer.Interval = new TimeSpan(0, 0, 1);
            dTimer.Start();
        }
        private void dTimer_Tick(object sender, EventArgs e)
        {
            readSemiValues(Utils.configPath + lastSettingFile);
            dTimer.Stop();
            UpdateSettings();
            log("Default setting loaded", false);
        }
        public void Closing()
        {
            writeSemiValues(Utils.configPath + lastSettingFile);
            foreach (KeyValuePair<string, SemiAxisUC> semi in semis)
            {
                semi.Value.Closing();
            }
        }
        public void UpdateSettings()
        {
            int idx = listSettings.SelectedIndex; 
            string[] files = Directory.GetFiles(Utils.configPath, "*.stg");
            listSettings.Items.Clear();
            foreach (var ffn in files)
            {
                string fn = System.IO.Path.GetFileName(ffn);
                if (fn == "default.stg") continue;
                ListBoxItem lbi = new ListBoxItem();
                lbi.Content = fn;
                lbi.Foreground = Brushes.Navy;
                lbi.FontSize = 14;
                listSettings.Items.Add(lbi);
            }
            if(listSettings.Items.Count > 0)
            {
                if (idx == -1) listSettings.SelectedIndex = 0;
                else listSettings.SelectedIndex = idx;
            }
        }
        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (sender == btnAdd)
            {
                string newSetting = Interaction.InputBox("Name the new setting");
                if (newSetting != "")
                {
                    writeSemiValues(Utils.configPath + newSetting);
                    UpdateSettings();
                    log("> Add new setting file: " + newSetting, false);
                }       
                return;             
            }
            if (listSettings.SelectedIndex == -1) Utils.TimedMessageBox("No setting has been selected");
            ListBoxItem lbi = (listSettings.SelectedItem as ListBoxItem);
            string selSetting = lbi.Content.ToString();
            if (sender == btnUpdate)
            {
                readSemiValues(Utils.configPath + selSetting);
                log("> Update state by " + selSetting, false);
            }
            if (sender == btnRemove)
            {
                if (MessageBox.Show("Do you really ???", "BIG QUESTION", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    File.Delete(Utils.configPath + selSetting);
                log("> Remove setting file: " + selSetting, false);
            }            
            if (sender == btnReplace)
            {
                File.Delete(Utils.configPath + selSetting);
                writeSemiValues(Utils.configPath + selSetting);
                log("> Replace setting file: " + selSetting, false);
            }
            UpdateSettings();
        }
        #region calibration
        private AnalogUC whichAnalog(AnalogConfig ac)
        {
            foreach (KeyValuePair<string, SemiAxisUC> semi in semis)
            {
                if ((ac.groupTitle == semi.Value.analogVCO.analogConfig.groupTitle) && (ac.title == semi.Value.analogVCO.analogConfig.title))
                {
                    return semi.Value.analogVCO;
                }
                if ((ac.groupTitle == semi.Value.analogVCA.analogConfig.groupTitle) && (ac.title == semi.Value.analogVCA.analogConfig.title))
                {
                    return semi.Value.analogVCA;
                }
            }
            return null;
        }
        AnalogConfig selectAC = new AnalogConfig(); // ac buffer
        public void setCalibration(ref AnalogConfig ac)
        {
            AnalogUC analog = whichAnalog(ac);
            if(Utils.isNull(analog))
            {
                Utils.TimedMessageBox("Analog channel not found", "Problem", 2500); return;
            }
            if (tiCalibr.Visibility == Visibility.Visible)
            {
                Utils.TimedMessageBox("Another calibration is in progress", "Problem", 2500);
                analog.Selected = false; return;
            }
            selectAC.Assign(ac);
            tiCalibr.Visibility = Visibility.Visible; tiLog.Visibility = Visibility.Collapsed; tiSetting.Visibility = Visibility.Collapsed;
            tabControl.SelectedIndex = 1;
            CalibrUC1.Init(selectAC);
        }
        private void getCalibration(object sender, RoutedEventArgs e)
        {
            AnalogUC auc = whichAnalog(selectAC);
            auc.Selected = false;
            tiCalibr.Visibility = Visibility.Collapsed; tiLog.Visibility = Visibility.Visible; tiSetting.Visibility = Visibility.Visible;
            tabControl.SelectedIndex = 0;
            if (!(sender is Button)) return;
            if ((sender as Button).Name == "btnCancel") return;
            auc.Config(CalibrUC1.analogConfig, false);
        }

        private void insertValue(object sender, RoutedEventArgs e)
        {
            CalibrUC1.insertValue(whichAnalog(selectAC).target);
        }
        #endregion
    }
}
