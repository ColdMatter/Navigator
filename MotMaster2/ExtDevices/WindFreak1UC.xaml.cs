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
using MOTMaster2;
using MOTMaster2.SequenceData;
using DAQ.HAL;
using ErrorManager;
using UtilsNS;
using NationalInstruments.Restricted;

namespace MOTMaster2.ExtDevices
{
    /// <summary>
    /// Interaction logic for WindFreakUC.xaml
    /// </summary>
    public partial class WindFreak1UC : UserControl, IExtDevice
    {
        public WindFreak1UC(string __dvcName, Brush brush)
        {
            InitializeComponent();
            _dvcName = __dvcName;
            grpBox.Header = dvcName;
            grpBox.BorderBrush = brush;
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
                if (Utils.isNull(Controller.microSynth)) lastCheckHardware = false;   
                else
                {
                    if (OptEnabled() && !Controller.microSynth.Connected) Controller.microSynth.Connect();
                    lastCheckHardware = Controller.microSynth.Connected;
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
            // call hardware
            if (fns.Length == 1) // common commands
            {
                
            }
            else // channel-oriented commands
            {
                WindfreakSynth.WindfreakChannel chn;
                if (fns[1] == "A") chn = Controller.microSynth.ChannelA;
                else chn = Controller.microSynth.ChannelB;
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
            }
            return true;
        }
       public void Init(ref Sequence _sequenceData, ref GeneralOptions _genOptions) // params, opts
       {           
            ucExtFactors.AddFactor("Amplitude[dBm] chn.A", "amplitude:A");
            ucExtFactors.AddFactor("Frequency[MHz] chn.A", "frequency:A");
            ucExtFactors.AddFactor("Amplitude[dBm] chn.B", "amplitude:B");
            ucExtFactors.AddFactor("Frequency[MHz] chn.B", "frequency:B");
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
            
        }
        public void Final() // closing stuff and save state 
        {
            // save config file
            Dictionary<string, string> cfg = ucExtFactors.factorsState;
            cfg["RFPowerA"] = chkRFPowerA.IsChecked.Value.ToString(); cfg["RFPowerB"] = chkRFPowerB.IsChecked.Value.ToString();           
            Utils.writeDict(Utils.configPath + dvcName + ".CFG", cfg);
            // Disconnect
            if (!Utils.isNull(Controller.microSynth))
                if (Controller.microSynth.Connected) Controller.microSynth.Disconnect();
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
            return bb;
        }
        public void SequenceEvent(string EventName)
        {

        }
        public bool UpdateDevice(bool ignoreMutable = false)
        {
            return ucExtFactors.UpdateDevice(ignoreMutable) && UpdateOthers(ignoreMutable);
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
