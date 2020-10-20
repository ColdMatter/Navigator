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
using NationalInstruments.Restricted;

namespace MOTMaster2.ExtDevices
{
    /// <summary>
    /// Interaction logic for an external device
    /// </summary>
    public partial class ucExtDevice : UserControl//, IExtDevice
    {
        protected string _dvcName;
        public string dvcName { get { return _dvcName; } }
        public GeneralOptions genOpt { get; set; }
        public bool UpdateFromOptions(ref GeneralOptions _genOptions)
        {
            genOpt = _genOptions;
            return GetEnabled();
        }

        protected bool? lastCheckHardware = null;
        public bool GetEnabled(bool ignoreHardware = false) // 
        {
            string ss = "Device ";
            bool hw = false;
            if (ignoreHardware)
            {
                if (Utils.isNull(hw)) lastCheckHardware = CheckHardware();
                hw = (bool)lastCheckHardware;
            }                
            else hw = CheckHardware();
            if (hw) ss += "(ON)";
            else ss += "(off)";
            bool opt = OptEnabled();
            ss += " Opt.Enabled ";
            if (opt) ss += "(ON)";
            else ss += "(off)";
            lbStatus.Content = ss;
            if (hw && opt) lbStatus.Foreground = Brushes.DarkGreen; 
            else lbStatus.Foreground = Brushes.Maroon;
            return hw && opt;
        }

        public Sequence seqData { get; set; }
        public void UpdateFromSequence(ref Sequence _sequenceData)
        {
            seqData = _sequenceData;
            foreach (Factor factor in Factors.Values)
            {
                factor.ParamUpdate(Parameters);
            }
        }
              
        private ObservableDictionary<string,Parameter> Parameters { get { return seqData.Parameters; } }
        protected ObservableDictionary<string, Factor> Factors;

        public double Estimate(string operand)
        {
            if (Utils.isNull(seqData)) return Double.NaN;
            double d = Double.NaN;
            foreach (KeyValuePair<string, Parameter> prm in Parameters)
            {
                if ((prm.Value.IsLaser) && (prm.Key == operand)) d = (double)prm.Value.Value;
            }
            return d;
        }
        private Factor scanFactor = null;

        public virtual bool OptEnabled()
        {
            return Controller.config.Debug;               
        }

        public virtual bool CheckHardware()
        {
            return Controller.config.Debug;
        }

        public ucExtDevice(string __dvcName)
        {
            InitializeComponent();
            _dvcName = __dvcName;
            //grpBox.Header = _dvcName;
            Factors = new ObservableDictionary<string, Factor>();
        }

        public virtual void Init(ref Sequence _sequenceData, ref GeneralOptions _genOptions) // params, opts; only once after creating the factors  
        {
            UpdateFromOptions(ref _genOptions);
            UpdateFromSequence(ref _sequenceData);
            Dictionary<string, string> state = Utils.readDict(Utils.configPath + dvcName+".CFG");
            foreach (Factor factor in Factors.Values)
            {
                factor.OnEstimate += Estimate;
                factor.OnSend2Dvc += Send2Dvc;
                if (state.ContainsKey(factor.fName)) factor.Text = state[factor.fName];
            }
        }
        public void Final()
        {
            Dictionary<string, string> state = new Dictionary<string, string>();
            foreach (Factor factor in Factors.Values)
            {
                state[factor.fName] = factor.Text;
            }
            Utils.writeDict(Utils.configPath + dvcName + ".CFG",state);
        }

        public void UpdateFactors()
        {            
            stackFactors.Children.Clear();
            foreach (Factor factor in Factors.Values)
            {
                stackFactors.Children.Add(factor);
            }
            //btnUpdate.Margin = new Thickness(0, stackFactors.Height+20, 0, 0);
        }

        public bool UpdateDevice(bool ignoreMutable = false) // before sequence (false) or update button (true)
        {
            if (Utils.isNull(genOpt)) return false;
            if (!ignoreMutable && !chkMutable.IsChecked.Value) return false;

            bool rslt = GetEnabled();
            if (rslt)
                foreach (Factor factor in Factors.Values)
                {
                    rslt &= factor.SendValue();
                }
            return rslt;
        }

        public bool IsScannable(string fct = "")  
        {
            bool bb = false;
            foreach (Factor factor in Factors.Values)
            {
                if (fct == "") bb |= factor.fType == Factor.factorType.ftParam; // at least one param (no name)
                else bb |= factor.Scanning(fct); 
                if (bb) break;
            }
            return bb && chkMutable.IsChecked.Value;
        }

        public virtual bool Send2Dvc(string fctName, double fctValue) // hardware update
        {
            // the real McCoy
            return true;
        }

        public bool ScanIter(string prm, int grpIdx) // return false if no scanning here
            // prm is taken only for grpIdx = 0
        {
            if (grpIdx.Equals(0))
            {
                scanFactor = null; 
                foreach (Factor factor in Factors.Values)
                {
                    if (factor.Scanning(prm))
                    {
                        scanFactor = factor; break; // only one scan factor per device
                    }
                }
                return !Utils.isNull(scanFactor);
            }
            if (Utils.isNull(scanFactor)) return false;
            if (grpIdx.Equals(-1)) 
            {
                UpdateDevice();
                scanFactor.Scanning("");
                scanFactor = null;
                lastCheckHardware = null;
                return true;
            }
            scanFactor.SendValue(); // value is taken from parameters    
            return true;
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            UpdateDevice(true);
        }
    }
}
