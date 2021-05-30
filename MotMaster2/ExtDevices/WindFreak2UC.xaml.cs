using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using MOTMaster2;
using MOTMaster2.SequenceData;
using DAQ.HAL;
using ErrorManager;
using UtilsNS;
using NationalInstruments.Restricted;
using NationalInstruments.Controls;

namespace MOTMaster2.ExtDevices
{
    /// <summary>
    /// Interaction logic for WindFreakUC.xaml
    /// </summary>
    public partial class WindFreak2UC : UserControl, IExtDevice
    {
        Dictionary<char, WindfreakSynth.WindfreakChannel> chns = new Dictionary<char, WindfreakSynth.WindfreakChannel>();
        Dictionary<ArrowButton, NumericTextBoxDouble> phaseShift = new Dictionary<ArrowButton, NumericTextBoxDouble>();
        public WindFreak2UC(string __dvcName, Brush brush)
        {
            InitializeComponent();
            _dvcName = __dvcName;
            grpBox.Header = dvcName;
            grpBox.BorderBrush = brush;
            ucExtFactors.dvcName = _dvcName; ucExtFactors.groupUpdate = false;
        }
        protected string _dvcName;
        public string dvcName { get { return _dvcName; } }
        public bool CheckEnabled(bool ignoreHardware = false) // ready to operate
        {
            bool bb = OptEnabled() && (ignoreHardware ? true : CheckHardware());
            ucExtFactors.btnUpdate.IsEnabled = bb;
            return bb;
        }
        public GeneralOptions genOpt { get; set; }       
        public bool OptEnabled()
        {
            if (Utils.isNull(genOpt)) return false;
            else return genOpt.WindFreakEnabled;
        }
        protected bool lastCheckHardware = false;
        public bool CheckHardware()
        {
            if (Controller.config.Debug) lastCheckHardware = true;
            else // check connection to the device
            {
                if (Utils.isNull(Controller.microSynth) || Utils.isNull(Controller.microSynth2)) lastCheckHardware = false;   
                else
                {
                    if (OptEnabled())
                    {
                        if (!Controller.microSynth.Connected) Controller.microSynth.Connect();
                        if (!Controller.microSynth2.Connected) Controller.microSynth2.Connect();
                    }
                    lastCheckHardware = Controller.microSynth.Connected && Controller.microSynth2.Connected;
                }              
            }                          
            return lastCheckHardware;
        }
        public bool Talk2Dvc(string fctName, object fctValue) // hardware update
        {
            if (fctName.Equals("_others_")) return UpdateOthers(Convert.ToBoolean(fctValue)); // recursive           
            if (!OptEnabled())
            {
                ErrorMng.Log("Error: the device <" + dvcName + "> is not Enabled (options)!", Brushes.DarkRed.Color);
                return false;
            }
            if (Controller.config.Debug) return true;            
            if (!CheckHardware())
            {
                ErrorMng.Log("Error: the device <"+ dvcName+"> is not available!", Brushes.Red.Color);
                return false;
            }
            if (!CheckEnabled(true)) return false;
            string[] fns = fctName.Split(':');
            if (fns.Length == 1) // common commands (non-channel specific)
            {
                return false; // nothing to do
            }
            if (fns.Length != 2) throw new Exception("Syntax problem (-789)");
                    
            WindfreakSynth.WindfreakChannel chn;
            switch (fns[1])
            {
                case "A": chn = Controller.microSynth.ChannelA;
                    break;
                case "B": chn = Controller.microSynth.ChannelB;
                    break;
                case "C": chn = Controller.microSynth2.ChannelA;
                    break;
                case "D": chn = Controller.microSynth2.ChannelB;
                    break;
                default: chn = Controller.microSynth.ChannelA;
                    break;
            }          
            switch (fns[0])
            {
                case "RFPower":
                    chn.RFOn = Convert.ToBoolean(fctValue);
                    break;
                case "amplitude":
                    chn.Amplitude = Convert.ToDouble(fctValue);
                    break;
                case "frequency":
                    chn.Frequency = Convert.ToDouble(fctValue);
                    break;
            }           
            return true;
        }
       public void Init(ref Sequence _sequenceData, ref GeneralOptions _genOptions) // params, opts
       {           
            ucExtFactors.AddFactor("Amplitude[dBm] chn.A", "amplitude:A");
            ucExtFactors.AddFactor("Frequency[MHz] chn.A", "frequency:A");
            ucExtFactors.AddFactor("Amplitude[dBm] chn.B", "amplitude:B");
            ucExtFactors.AddFactor("Frequency[MHz] chn.B", "frequency:B");
            ucExtFactors.AddFactor("Amplitude[dBm] chn.C", "amplitude:C");
            ucExtFactors.AddFactor("Frequency[MHz] chn.C", "frequency:C");
            ucExtFactors.AddFactor("Amplitude[dBm] chn.D", "amplitude:D");
            ucExtFactors.AddFactor("Frequency[MHz] chn.D", "frequency:D");
            factorRow.Height = new GridLength(ucExtFactors.UpdateFactors());
            ucExtFactors.Init(); UpdateFromOptions(ref _genOptions);
            ucExtFactors.UpdateFromSequence(ref _sequenceData);
            ucExtFactors.OnSend2HW += new FactorsUC.Send2HWHandler(Talk2Dvc);
            ucExtFactors.OnCheckHw += new FactorsUC.CheckHwHandler(CheckHardware);
            // load config file
            Dictionary<string, string> cfg = Utils.readDict(Utils.configPath + dvcName + ".CFG");
            ucExtFactors.factorsState = cfg;
            if (cfg.ContainsKey("RFPowerA")) chkRFPowerA.IsChecked = Convert.ToBoolean(cfg["RFPowerA"]);
            if (cfg.ContainsKey("RFPowerB")) chkRFPowerB.IsChecked = Convert.ToBoolean(cfg["RFPowerB"]);
            if (cfg.ContainsKey("RFPowerC")) chkRFPowerC.IsChecked = Convert.ToBoolean(cfg["RFPowerC"]);
            if (cfg.ContainsKey("RFPowerD")) chkRFPowerD.IsChecked = Convert.ToBoolean(cfg["RFPowerD"]);
            
            phaseShift[btnA] = numA; phaseShift[btnB] = numB; phaseShift[btnC] = numC; phaseShift[btnD] = numD;
            if (Utils.isNull(Controller.microSynth)) return;
            chns['A'] = Controller.microSynth.ChannelA; chns['B'] = Controller.microSynth.ChannelB;
            chns['C'] = Controller.microSynth2.ChannelA; chns['D'] = Controller.microSynth2.ChannelB;           
        }
        public void Final() // closing stuff and save state 
        {
            // save config file
            Dictionary<string, string> cfg = ucExtFactors.factorsState;
            cfg["RFPowerA"] = chkRFPowerA.IsChecked.Value.ToString(); cfg["RFPowerB"] = chkRFPowerB.IsChecked.Value.ToString();
            cfg["RFPowerC"] = chkRFPowerC.IsChecked.Value.ToString(); cfg["RFPowerD"] = chkRFPowerD.IsChecked.Value.ToString();

            if (!Controller.config.Debug)
            {
                cfg["dev1"] = Controller.microSynth.address; cfg["dev2"] = Controller.microSynth2.address;
            }
            Utils.writeDict(Utils.configPath + dvcName + ".CFG", cfg);
            // Disconnect
            if (!Utils.isNull(Controller.microSynth))
                if (Controller.microSynth.Connected) Controller.microSynth.Disconnect();
            if (!Utils.isNull(Controller.microSynth2))
                if (Controller.microSynth2.Connected) Controller.microSynth2.Disconnect();
        }      
        public void UpdateFromOptions(ref GeneralOptions _genOptions)
        {
            genOpt = _genOptions;
            ucExtFactors.UpdateEnabled(genOpt.WindFreakEnabled, CheckHardware());
        }
        public bool UpdateOthers(bool ignoreMutable = false) // update all non-factors (others)
        {
            if (!ignoreMutable && !ucExtFactors.chkMutable.IsChecked.Value) return false;
            bool bb = true;
            bb &= Talk2Dvc("RFPower:A", chkRFPowerA.IsChecked.Value); bb &= Talk2Dvc("RFPower:B", chkRFPowerB.IsChecked.Value);
            bb &= Talk2Dvc("RFPower:C", chkRFPowerC.IsChecked.Value); bb &= Talk2Dvc("RFPower:D", chkRFPowerD.IsChecked.Value);
            return bb;
        }
        public bool UpdateDevice(bool ignoreMutable = false)
        {
            bool bb = ucExtFactors.UpdateDevice(ignoreMutable) && UpdateOthers(ignoreMutable);
            Thread.Sleep(500);
            return bb;
        }
        private void btnA_Click(object sender, RoutedEventArgs e)
        {
            double ps = phaseShift[sender as ArrowButton].Value;
            char chnName = Convert.ToChar((sender as ArrowButton).Content);
            if (chns.Count.Equals(0)) Utils.TimedMessageBox("No active devices found");
            chns[chnName].phaseShift(2*ps);
        }

        private void imgTripleBars_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ContextMenu cm = this.FindResource("cmTripleBars") as ContextMenu;
            cm.PlacementTarget = sender as Image;
            cm.IsOpen = true;
        }
        private void miCheckHw_Click(object sender, RoutedEventArgs e)
        {
            ucExtFactors.UpdateEnabled(genOpt.WindFreakEnabled, CheckHardware(), CheckEnabled(false));
        }
    }
}
