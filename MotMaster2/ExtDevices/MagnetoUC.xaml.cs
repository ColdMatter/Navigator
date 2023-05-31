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
    /// Interaction logic for MagnetoUC.xaml
    /// </summary>
    public partial class MagnetoUC : UserControl, IExtDevice
    {
        public MagnetoUC(string __dvcName, Brush brush)
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
        
        public bool Talk2Dvc(string fctName, object fctValue)
        {
            return true;   
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
            if (!CheckEnabled(true)) return false;
            bool bAll = fctName.Equals("<ALL>");
            /*if ((fctName.Equals("RamanPhase") || bAll) && false)
            {
                double sVal = bAll ? ucExtFactors.Factors[4].getReqValue(0) : Convert.ToDouble(fctValue);
                bool bb = Controller.M2DCS.phaseControl(sVal);
                if (bb) ucExtFactors.Factors[4].fValue = sVal;
                if (fctName.Equals("RamanPhase")) return bb;
            }
            // call hardware
            Dictionary<string, object> curr = Controller.M2PLL.get_status();
            //Utils.writeDict(Utils.configPath + @"M2PLL_get_status", curr);
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
            
            Controller.CheckPhaseLock();
            if (Controller.M2PLL.configure_lo_profile(true, false, "ecd", InputFreq, BeatFreqTrim, ChirpRate, ChirpDuration, false))
            {
                if (Utils.isNull(fctValue))
                {
                    ucExtFactors.Factors[0].fValue = InputFreq / 1.0e6; ucExtFactors.Factors[1].fValue = BeatFreqTrim;
                    ucExtFactors.Factors[2].fValue = ChirpRate / 1.0e6; ucExtFactors.Factors[3].fValue = ChirpDuration;
                    ucExtFactors.UpdateValues(); // ALL
                }
            }
            else ErrorMng.Log("Error: in device <" + dvcName + "> update (out of range value).", Brushes.DarkRed.Color); 
            Controller.CheckPhaseLock();
            */
            return true;
        }
        public bool OptEnabled()
        {
            if (Utils.isNull(genOpt)) return false;
            else return genOpt.ExtDvcEnabled[dvcName]; 
        }
        protected bool lastCheckHardware = false;
        public bool CheckHardware()
        {
            lastCheckHardware = true;
            /*if (Controller.config.Debug) 
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
            }*/
            return lastCheckHardware;
        }

        public void Init(ref Sequence _sequenceData, ref GeneralOptions _genOptions) // params, opts; call after creating factors
        {
            ucExtFactors.AddFactor("X axis [V]", "Xaxis");
            ucExtFactors.AddFactor("Y axis [V]", "Yaxis");
            ucExtFactors.AddFactor("Z axis [V]", "Zaxis");
           
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
            ucExtFactors.UpdateEnabled(genOpt.ExtDvcEnabled[dvcName], CheckHardware());
        }
        public string SequenceEvent(string EventName)
        {
            return "";
        }

        public bool UpdateDevice(bool ignoreMutable = false)
        {
            return ucExtFactors.UpdateDevice(ignoreMutable);
        }

        private void imgTripleBars_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ContextMenu cm = this.FindResource("cmTripleBars") as ContextMenu;
            cm.PlacementTarget = sender as Image;
            cm.IsOpen = true;
        }
        private void miCheckHw_Click(object sender, RoutedEventArgs e)
        {
            ucExtFactors.UpdateEnabled(genOpt.ExtDvcEnabled[dvcName], CheckHardware(), CheckEnabled(false));
        }
    }
}
