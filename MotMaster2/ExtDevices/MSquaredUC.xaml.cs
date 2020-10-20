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
        }
        protected string _dvcName;
        public string dvcName { get { return _dvcName; } }

        public bool GetEnabled(bool ignoreHardware = false) // ready to operate
        {
            return OptEnabled() && ignoreHardware ? true : CheckHardware();
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
                ErrorMng.Log("Error: the device <" + dvcName + "> is not Enabled (options)!", Brushes.DarkRed.Color);
                return false;
            }
            if (Controller.config.Debug) return true;
            if (!CheckHardware())
            {
                ErrorMng.Log("Error: the device <" + dvcName + "> is not available!", Brushes.Red.Color);
                return false;
            }
            if (!GetEnabled(true)) return false;
            // call hardware
            Dictionary<string,object> curr = Controller.M2PLL.get_status();
            double PLLFreq = Convert.ToDouble(curr["main_synth_freq"]);  // ?!? IS IT THE SAME AS input_frequency ?!?
            double BeatFreqTrim = Convert.ToDouble(curr["beat_frequency_trim"]);
            double ChirpRate = ucExtFactors.Factors[2].getReqValue(0); ucExtFactors.Factors[2].fValue = ChirpRate;
            double ChirpDuration = ucExtFactors.Factors[3].getReqValue(0); ucExtFactors.Factors[3].fValue = ChirpDuration;

            CheckPhaseLock();
            switch (fctName)
            {
                case "PLLFreq":
                    PLLFreq = Convert.ToDouble(fctValue) * 1e6;
                    break;
                case "BeatFreqTrim":
                    BeatFreqTrim = Convert.ToDouble(fctValue);
                    break;
                case "ChirpRate":
                    ChirpRate = Convert.ToDouble(fctValue) * 1e6;
                    break;
                case "ChirpDuration":
                    ChirpDuration = Convert.ToDouble(fctValue);
                    break;
            }
            Controller.M2PLL.configure_lo_profile(true, false, "ecd", PLLFreq, BeatFreqTrim, ChirpRate, ChirpDuration, false);
            CheckPhaseLock();
            return true;
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
                    if (!Controller.M2DCS.Connected)
                    {
                        Controller.M2DCS.Connect(); Controller.M2DCS.StartLink();
                    }
                    if (!Controller.M2PLL.Connected)
                    {
                        Controller.M2PLL.Connect(); Controller.M2PLL.StartLink();
                    }
                }                
                lastCheckHardware = Controller.M2DCS.Connected && Controller.M2PLL.Connected;
            }
            return lastCheckHardware;
        }

        public void Init(ref Sequence _sequenceData, ref GeneralOptions _genOptions) // params, opts; call after creating factors
        {
            ucExtFactors.AddFactor("PLL Freq.[MHz]", "PLLFreq");
            ucExtFactors.AddFactor("Beat Freq.Trim [Hz]", "BeatFreqTrim");
            ucExtFactors.AddFactor("Chirp [MHz/s]", "ChirpRate");
            ucExtFactors.AddFactor("Duration [ms]", "ChirpDuration");
            //ucExtFactors.AddFactor(""); ucExtFactors.AddFactor("");
            factorRow.Height = new GridLength(ucExtFactors.UpdateFactors());
            ucExtFactors.Init(); UpdateFromOptions(ref _genOptions);
            ucExtFactors.UpdateFromSequence(ref _sequenceData); 
            ucExtFactors.OnSend2HW += new FactorsUC.Send2HWHandler(Talk2Dvc);
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
        public bool UpdateDevice(bool ignoreMutable = false)
        {
            return ucExtFactors.UpdateDevice(ignoreMutable) && UpdateOthers(ignoreMutable);
        }

    }
}
