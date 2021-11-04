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
using ErrorManager;
using UtilsNS;

namespace MOTMaster2.ExtDevices
{
    /// <summary>
    /// Interaction logic for MSquaredUC.xaml
    /// </summary>
    public partial class MSquaredUC : UserControl, IExtDevice
    {
        public MSquaredUC(string __dvcName, Brush brush)
        {
            InitializeComponent();
            _dvcName = __dvcName; 
            grpBox.Header = dvcName;
            grpBox.BorderBrush = brush;
            ucExtFactors.dvcName = _dvcName; ucExtFactors.groupUpdate = true;
        }
        protected string _dvcName;
        public string dvcName { get { return _dvcName; } }

        public bool CheckEnabled(bool ignoreHardware = false) // ready to operate
        {
            bool bb = OptEnabled() && (ignoreHardware ? true : CheckHardware());
            ucExtFactors.btnUpdate.IsEnabled = bb;
            return bb;
        }
        
        private bool CheckPhaseLock()
        {
            if (!Controller.config.Debug)
            {
                DAQ.HAL.ICEBlocPLL.Lock_Status lockStatus = new DAQ.HAL.ICEBlocPLL.Lock_Status();
                bool locked = Controller.M2PLL.main_lock_status(out lockStatus);
                //if (!locked) ErrorMng.errorMsg("PLL lock is not engaged - currently " + lockStatus.ToString(),10,false);
                return locked;
            }
            else return true;
        }

        public bool Talk2Dvc(string fctName, object fctValue)
        {
            if (fctName.Equals("_others_")) return UpdateOthers(Convert.ToBoolean(fctValue)); // recursive           
            if (!OptEnabled())
            {
                ErrorMng.errorMsg("the device <" + dvcName + "> is not Enabled (options)!", 118);
                return false;
            }
            if (Controller.config.Debug)
            {
                //return true; // no factors visual update with values when debug; maybe later ???
            }
            if (!CheckHardware())
            {
                ErrorMng.errorMsg("the device <" + dvcName + "> is not available!", 119);
                return false;
            }
            if (!CheckEnabled(true)) return false;
            bool bAll = fctName.Equals("<ALL>");
            if ((fctName.Equals("RamanPhase") || bAll) && false) // temporary disabled
            {
                double sVal = bAll ? ucExtFactors.Factors[4].getReqValue(0) : Convert.ToDouble(fctValue);
                bool bb = Controller.M2DCS.phaseControl(sVal);
                if (bb) ucExtFactors.Factors[4].fValue = sVal;
                if (fctName.Equals("RamanPhase")) return bb;
            }
            // call hardware
            Dictionary<string, object> curr;
            if (Controller.config.Debug)
            {
                Dictionary<string, string> curr0 = Utils.readDict(Utils.configPath + @"M2PLL_get_status");
                curr = new Dictionary<string, object>();
                foreach (var item in curr0)
                    curr[item.Key] = (object)item.Value;
            }
            else curr = Controller.M2PLL.get_status(); //Utils.writeDict(Utils.configPath + @"M2PLL_get_status", curr);
            
            double InputFreq = 6834.68;
            if (ucExtFactors.Factors[0].fType == Factor.factorType.ftNone)
            {
                if (curr.ContainsKey("beat_freq")) InputFreq = Convert.ToDouble(curr["beat_freq"])/1e6;
            }
            else InputFreq = ucExtFactors.Factors[0].getReqValue(0) * 1.0e6;

            double BeatFreqTrim = (ucExtFactors.Factors[1].fType == Factor.factorType.ftNone) ? 0 : ucExtFactors.Factors[1].getReqValue(0);
            double ChirpRate = (ucExtFactors.Factors[2].fType == Factor.factorType.ftNone) ? 0 : ucExtFactors.Factors[2].getReqValue(0) * 1.0e6; 
            double ChirpDuration = (ucExtFactors.Factors[3].fType == Factor.factorType.ftNone) ? 0 : ucExtFactors.Factors[3].getReqValue(0); //ucExtFactors.Factors[3].fValue = ChirpDuration;

            switch (fctName)
            {
                case "InputFreq":
                    if (!Utils.isNull(fctValue)) InputFreq = Convert.ToDouble(fctValue) * 1.0e6;
                    break;
                case "BeatFreqTrim":
                    BeatFreqTrim = Convert.ToDouble(fctValue);
                    break;
                case "ChirpRate":
                    ChirpRate = Convert.ToDouble(fctValue) * 1.0e6;
                    break;
                case "ChirpDuration":
                    ChirpDuration = Convert.ToDouble(fctValue);
                    break;
            }          
            bool bc = Controller.CheckPhaseLock();
            bc &= (!Controller.config.Debug) ? Controller.M2PLL.configure_lo_profile(true, false, "ecd", InputFreq, BeatFreqTrim, ChirpRate, ChirpDuration, false) : true;
            if (bc)
            {
                if (Utils.isNull(fctValue) || ((string)fctValue == "<ALL>"))
                {
                    ucExtFactors.Factors[0].fValue = InputFreq / 1.0e6; ucExtFactors.Factors[1].fValue = BeatFreqTrim;
                    ucExtFactors.Factors[2].fValue = ChirpRate / 1.0e6; ucExtFactors.Factors[3].fValue = ChirpDuration;
                    ucExtFactors.UpdateValues(); // ALL
                }
            }
            else ErrorMng.errorMsg("Error: in device <" + dvcName + "> update (out of range value).", 122);
            bc &= Controller.CheckPhaseLock();
            
            return bc;
        }
        public bool OptEnabled()
        {
            if (Utils.isNull(genOpt)) return false;
            else return genOpt.m2Enabled;
        }
        protected bool lastCheckHardware = false;
        public bool CheckHardware()
        {
            if (Controller.config.Debug) lastCheckHardware = true;
            else // check connection to the device
            {
                if (Utils.isNull(Controller.M2DCS) || Utils.isNull(Controller.M2PLL))
                {
                    lastCheckHardware = false; return false;
                }
                if (OptEnabled())
                {
                    if (!Controller.M2PLL.Connected)
                    {
                        Controller.M2PLL.Connect();
                        if (Controller.M2PLL.Connected) Controller.M2PLL.StartLink();
                    }
                    if (!Controller.M2DCS.Connected)
                    {
                        Controller.M2DCS.Connect(); 
                        if (Controller.M2DCS.Connected) Controller.M2DCS.StartLink();
                    }
                }                
                lastCheckHardware = Controller.M2DCS.Connected && Controller.M2PLL.Connected;
            }
            return lastCheckHardware;
        }

        public void Init(ref Sequence _sequenceData, ref GeneralOptions _genOptions) // params, opts; call after creating factors
        {
            ucExtFactors.AddFactor("Input Freq.[MHz]", "InputFreq");
            ucExtFactors.AddFactor("Beat Freq.Trim [Hz]", "BeatFreqTrim");
            ucExtFactors.AddFactor("Chirp [MHz/s]", "ChirpRate");
            ucExtFactors.AddFactor("Chirp Duration [s]", "ChirpDuration");
            //ucExtFactors.AddFactor("Raman Phase [rad]", "RamanPhase");
            //ucExtFactors.AddFactor(""); ucExtFactors.AddFactor("");
            factorRow.Height = new GridLength(ucExtFactors.UpdateFactors()+10);
            ucExtFactors.Init(); UpdateFromOptions(ref _genOptions);
            ucExtFactors.UpdateFromSequence(ref _sequenceData); 
            ucExtFactors.OnSend2HW += new FactorsUC.Send2HWHandler(Talk2Dvc);
            ucExtFactors.OnCheckHw += new FactorsUC.CheckHwHandler(CheckHardware);
            ucExtFactors.factorsState = Utils.readDict(Utils.configPath + dvcName + ".CFG");
        }
        public void Final() // closing stuff and save state 
        {
            Utils.writeDict(Utils.configPath + dvcName + ".CFG", ucExtFactors.factorsState);
            // Disconnect
            //if (!Utils.isNull(Controller.microSynth))
            //    if (Controller.microSynth.Connected) Controller.microSynth.Disconnect();

        }
        public GeneralOptions genOpt { get; set; }
        public void UpdateFromOptions(ref GeneralOptions _genOptions)
        {
            genOpt = _genOptions;
            ucExtFactors.UpdateEnabled(genOpt.m2Enabled, CheckHardware());
        }
        public bool UpdateOthers(bool ignoreMutable = false) // update all non-factors (others)
        {
            if (!ignoreMutable && !ucExtFactors.chkMutable.IsChecked.Value) return false; 
            // Talk2Dvc(...
            return true;
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
            ucExtFactors.UpdateEnabled(genOpt.m2Enabled, CheckHardware(), CheckEnabled(false));
        }
    }
}
