using System;
using System.Collections.Generic;
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
using System.Windows.Threading;
using DAQ.HAL;
using DAQ.Environment;
using MOTMaster2;
using MOTMaster2.SequenceData;
using ErrorManager;
using MinimalisticTelnet;
using UtilsNS;

namespace MOTMaster2.ExtDevices
{
    /// <summary>
    /// Interaction logic for MuQuansUC.xaml
    /// </summary>
    public partial class MuQuansUC : UserControl, IExtDevice
    {
        private DDS_script script;
        private TelnetConnection MQcomm;

        public MuQuansUC(string __dvcName, Brush brush)
        {
            InitializeComponent();
            _dvcName = __dvcName; 
            grpBox.Header = dvcName;
            grpBox.BorderBrush = brush;
            ucExtFactors.dvcName = _dvcName; ucExtFactors.groupUpdate = true;
            tiMain.Visibility = Visibility.Collapsed; tiEdit.Visibility = Visibility.Collapsed;            
        }
        protected string _dvcName;

        public string dvcName { get { return _dvcName; } }

        public bool CheckEnabled(bool ignoreHardware = false) // ready to operate
        {
            bool bb = OptEnabled() && (ignoreHardware ? true : CheckHardware());
            ucExtFactors.btnUpdate.IsEnabled = bb;
            return bb;
        }
        
        public bool Talk2Dvc(string fctName, object fctValue)
        {            
            if (!OptEnabled())
            {
                ErrorMng.Log("Error: the device <" + dvcName + "> is not Enabled (options)!", Brushes.DarkRed.Color);
                return false;
            }
            //if (Controller.config.Debug) return true;
            if (!CheckHardware())
            {
                ErrorMng.Log("Error: the device <" + dvcName + "> is not available!", Brushes.Red.Color);
                return false;
            }
            if (!CheckEnabled(true)) return false;
            if (Convert.ToString(fctValue).Equals("_others_")) return true; // no other in here
            ExecScript();
            return true; 
        }
        public bool OptEnabled()
        {
            if (Utils.isNull(genOpt)) return false;
            else return genOpt.ExtDvcEnabled["MSquared"];
        }
        protected bool lastCheckHardware = false;
        public bool CheckHardware()
        {            
            if (Controller.config.Debug) lastCheckHardware = true;
            else // check connection to the device
            {
                if (Utils.isNull(MQcomm))
                {
                    lastCheckHardware = false; return false;
                }               
                lastCheckHardware = MQcomm.IsConnected;
            }
            return lastCheckHardware;
        }
        DispatcherTimer dTimer;
        public void Init(ref Sequence _sequenceData, ref GeneralOptions _genOptions) // params, opts; call after creating factors
        {
            factorRow.Height = new GridLength(ucExtFactors.UpdateFactors()+10);

            ucExtFactors.Init(); UpdateFromOptions(ref _genOptions);
            ucExtFactors.UpdateFromSequence(ref _sequenceData);
            ucExtFactors.OnSend2HW += new FactorsUC.Send2HWHandler(Talk2Dvc);
            ucExtFactors.OnCheckHw += new FactorsUC.CheckHwHandler(CheckHardware);

            script = new DDS_script(Utils.configPath + "MuQuans.tml"); script.units.allowNoUnit = true;
            SetAllFactors(false);
            CheckEnabled();
            Dictionary<string,string> configDict = Utils.readDict(Utils.configPath + dvcName + ".CFG");
            ucExtFactors.factorsState = configDict;

            dTimer = new DispatcherTimer();
            dTimer.Tick += new EventHandler(dTimer_Tick);
            dTimer.Interval = new TimeSpan(0, 0, 0, 1);

            if (Controller.config.Debug) return;

            if (!Utils.isNull(MQcomm))
            {
                MQcomm.tcpSocket.Close();
            }
            MQcomm = new TelnetConnection("192.168.1.125", 23);
            /*string addr;
            if (configDict.ContainsKey("slaveAddress")) addr = configDict["slaveAddress"];
            else addr = "ASRL18::INSTR"; // default
            slaveComm = new MuquansRS232(addr, "slave");
            Environs.Hardware.Instruments.Add("muquansSlave", slaveComm);

            if (configDict.ContainsKey("aomAddress")) addr = configDict["aomAddress"];
            else addr = "ASRL20::INSTR"; // default
            aomComm = new MuquansRS232(addr, "aom");
            Environs.Hardware.Instruments.Add("muquansAOM", aomComm);*/
        }
        public void Final() // closing stuff and save state 
        {
            Dictionary<string, string> configDict = new Dictionary<string, string>(ucExtFactors.factorsState);
            if (!Controller.config.Debug)
            {
                if (!Utils.isNull(MQcomm))
                {
                    MQcomm.tcpSocket.Close();
                }                
            }
            Utils.writeDict(Utils.configPath + dvcName + ".CFG", configDict);
        }
        public GeneralOptions genOpt { get; set; }
        public void UpdateFromOptions(ref GeneralOptions _genOptions)
        {
            genOpt = _genOptions;
            ucExtFactors.UpdateEnabled(genOpt.ExtDvcEnabled["MSquared"], CheckHardware());
        }
        private void SetAllFactors(bool keepContent) // from script
        {
            bool correct; var factorsState = ucExtFactors.factorsState;
            List<string> fcts = script.ExtractFactors(false, out correct);
            SetFactors(fcts);                      
            ucExtFactors.UpdateFromSequence();
            if (keepContent) ucExtFactors.factorsState = factorsState;
        }
        private void SetFactors(List<string> fcts) // list from script section
        {
            ucExtFactors.Factors.Clear();
            DCP_factors ddsFcts = script.factorsSection;
            var ffs = new Dictionary<string, bool>(); // script ones with flags
            foreach (string ss in fcts)
            {
                int j = ddsFcts.IdxFromName(ss);
                if (j == -1)
                {
                    ErrorMng.errorMsg("Missing declaration of factor <" + ss + ">", 143); continue;
                }
                if (ddsFcts[j].Item3.Equals("")) ucExtFactors.AddFactor(ddsFcts[j].Item2, ddsFcts[j].Item1);
            }
            factorRow.Height = new GridLength(ucExtFactors.UpdateFactors());
            ucExtFactors.Init();
        }
        
        public void SequenceEvent(string EventName) // events in the 
        {
            if (!ucExtFactors.chkMutable.IsChecked.Value) return; 
            if (!EventName.Equals("start")) ExecScript();
            /*
            #pragma warning disable 4014
            ExecuteScript(); // I want fire-and-forget here
            #pragma warning restore 4014*/
        }
        private void ExecScript()
        {
            ucExtFactors.UpdateValues();
            dTimer.Start();
        }
        private void dTimer_Tick(object sender, EventArgs e)
        {           
            dTimer.Stop();
            List<string> mscript = script.actualScript(script.replacements(ucExtFactors));            
            foreach (string line in mscript)
            {
                if (line.Length.Equals(0)) continue;

                if (line[0].Equals('@'))
                {
                    if (line.IndexOf("@delay").Equals(0))
                    {
                        string[] sa = line.Split(' ');
                        if (!sa.Length.Equals(2))
                        {
                            ErrorMng.Log("Error: syntax error -> "+line); continue; 
                        }
                        Task.Delay(Convert.ToInt32(sa[1])).Wait();
                        continue;
                    }
                }
                if (!Controller.config.Debug) MQcomm.WriteLine(line);
                ErrorMng.Log("MQ>>" + line);
                if (!Controller.config.Debug) ErrorMng.Log("MQ<<" + MQcomm.Read());
            }
        }

        /*public async Task ExecuteScript() https://www.c-sharpcorner.com/article/async-and-await-in-c-sharp/
        {
            List<string> mscript = script.actualScript(script.replacements(ucExtFactors));
            await Task.Run(() =>
            {                
                foreach (string line in mscript)
                {
                    if (line.Length.Equals(0)) continue;
                    if (line.IndexOf("slave:").Equals(0))
                    {
                        string[] sa = line.Split(':');
                        if (!sa.Length.Equals(2)) continue;
                        // call slave port
                        Talk2Dvc(sa[0], sa[1]);
                    }
                    if (line.IndexOf("aom:").Equals(0))
                    {
                        string[] sa = line.Split(':');
                        if (!sa.Length.Equals(2)) continue;
                        // call aom port
                        Talk2Dvc(sa[0], sa[1]);
                    }
                    if (line[0].Equals("@")) 
                    {
                        if (line.IndexOf("@delay").Equals(0))
                        {
                            string[] sa = line.Split(' ');
                            if (!sa.Length.Equals(2)) continue;
                            Task.Delay(Convert.ToInt32(sa[1])).Wait();
                        }                       
                    }                   
                }
            });           
        }*/

        public bool UpdateDevice(bool ignoreMutable = false)
        {
            return ucExtFactors.UpdateDevice(ignoreMutable);
        }
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            tabControl.SelectedIndex = 0;
        }
        private void btnAccept_Click(object sender, RoutedEventArgs e)
        {
            script.GetFromTextBox(tbTemplate);
            script.Save();            
            SetAllFactors(true);           
            tabControl.SelectedIndex = 0;
        }

        private void imgTripleBars_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ContextMenu cm = this.FindResource("cmTripleBars") as ContextMenu;
            cm.PlacementTarget = sender as Image;
            cm.IsOpen = true;
        }
        private void miCheckHw_Click(object sender, RoutedEventArgs e)
        {
            ucExtFactors.UpdateEnabled(genOpt.ExtDvcEnabled["MSquared"], CheckHardware(), CheckEnabled(false));
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (Utils.isNull(script))
            {
                ErrorMng.errorMsg("No template opened. ", 144); return;
            }
            tabControl.SelectedIndex = 1;
            script.SetToTextBox(tbTemplate);
        }
    }
}
