﻿using System;
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
    /// Interaction logic for FactorsUC.xaml
    /// </summary>
    public partial class FactorsUC : UserControl, IFactors
    {
        public FactorsUC() //
        {
            InitializeComponent();
            Factors = new List<Factor>();
            BlockMode = false;
        }
        private bool _genOpt_Enabled;
        public bool genOpt_Enabled { get { return _genOpt_Enabled; } }
        private bool _HW_Enabled;
        public bool HW_Enabled { get { return _HW_Enabled; } } 
        public bool UpdateEnabled(bool __genOpt_Enabled, bool __HW_Enabled)
        {
            _genOpt_Enabled = __genOpt_Enabled;
            _HW_Enabled = __HW_Enabled || Controller.config.Debug; 
            string ss = "Device ";
            if (HW_Enabled) ss += "(ON)";
            else ss += "(off)";            
            ss += " Opt.Enabled ";
            if (genOpt_Enabled) ss += "(ON)";
            else ss += "(off)";
            lbStatus.Content = ss;
            if (genOpt_Enabled && HW_Enabled) lbStatus.Foreground = Brushes.DarkGreen;
            else lbStatus.Foreground = Brushes.Maroon;
            return genOpt_Enabled && HW_Enabled;           
        }
        public Sequence seqData { get; set; }
        public void UpdateFromSequence(ref Sequence _sequenceData)
        {
            seqData = _sequenceData;
            foreach (Factor factor in Factors)
            {
                factor.ParamUpdate(Parameters);
            }
        }
        private ObservableDictionary<string,Parameter> Parameters { get { return seqData.Parameters; } }
        public List<Factor> Factors;
        public bool BlockMode; // it does not send individual factors, only the whole device state as one
        public void AddFactor(string fName, string extName = "", bool RequireValue = false)
        {
            Factor fc = new Factor(fName, extName, RequireValue);
            Factors.Add(fc); stackFactors.Children.Add(fc);
        }
        public int IdxFromName(string nm) // ext.name
        {
            int idx = -1; 
            for (int i = 0; i < Factors.Count; i++)
            {
                if (Factors[i].extName.Equals(nm))
                {
                    idx = i; break; 
                }
            }
            return idx;
        }
        public Dictionary<string,string> factorsState // only state of visuals
        {
            get
            {
                Dictionary<string, string> fs = new Dictionary<string, string>();
                foreach (Factor fact in Factors)
                {
                    fs[fact.fName] = fact.Text;
                }
                fs["Mutable"] = chkMutable.IsChecked.Value.ToString();
                return fs;
            }
            set
            {
                foreach (Factor fact in Factors)
                {
                    if (value.ContainsKey(fact.fName)) fact.Text = value[fact.fName];
                }
                if (value.ContainsKey("Mutable")) 
                    chkMutable.IsChecked = Convert.ToBoolean(value["Mutable"]);
            }
        }
        public double Estimate(string param)
        {
            if (Utils.isNull(seqData)) return Double.NaN;
            double d = Double.NaN;
            foreach (KeyValuePair<string, Parameter> prm in Parameters)
            {
                if ((prm.Value.IsLaser) && (prm.Key == param)) d = (double)prm.Value.Value;
            }
            return d;
        }       
        public void Init() // params, opts; only once after creating the factors  
        {
            foreach (Factor factor in Factors)
            {
                factor.OnEstimate += Estimate;
                factor.OnSend2Dvc += Send2HW;
            }
        }
        public void Final()
        {
            Dictionary<string, string> state = new Dictionary<string, string>();
            foreach (Factor factor in Factors)
            {
                state[factor.fName] = factor.Text;
            }
        }
        public double GetHeight()
        {
            double h = 0;
            foreach (Factor factor in Factors)
            {
                if (factor.isVisible) h += Math.Max(factor.ActualHeight, 56.5);
            }
            h += firstRow.Height.Value+lastRow.Height.Value;
            return h;
        }
        public void UpdateFactors() // visual update 
        {            
            stackFactors.Children.Clear();  
            foreach (Factor factor in Factors)
            {
                stackFactors.Children.Add(factor);               
            }
            return;
            //btnUpdate.Margin = new Thickness(0, stackFactors.Height+20, 0, 0);
        }
        public bool UpdateDevice(bool ignoreMutable = false) // before sequence (false) or update button (true)
        {          
            if (!ignoreMutable && !chkMutable.IsChecked.Value) return false;
            bool rslt = genOpt_Enabled && HW_Enabled;
            if (rslt)
            {
                if (BlockMode)
                {
                    Send2HW("_block_", (object)true);
                }               
                foreach (Factor factor in Factors)
                {
                    bool bb = factor.SendValue(BlockMode);
                    if (!bb) ErrorMng.Log("Error: problem with factor <" + factor.fName + ">");
                    rslt &= bb;
                    }
            }
            return rslt;
        }
        public bool IsScannable(string prm) // is scanning param (prm) in Factors; prm = "" resets
        {
            bool bb = false;
            foreach (Factor factor in Factors)
            {
                if (!factor.Enabled) continue;
                bb |= factor.Scanning(prm); 
                if (bb) break;
            }
            return bb && chkMutable.IsChecked.Value;
        }
        public delegate bool Send2HWHandler(string fctName, object fctValue);
        public event Send2HWHandler OnSend2HW;
        protected bool Send2HW(string fctName, object fctValue) // hardware update
        {
            if (OnSend2HW != null) return OnSend2HW(fctName, fctValue); // the real McCoy
            else return false;
        }
        private Factor scanFactor = null; // only ONE scanfactor (it maybe be changed to list)
        public bool ScanIter(string prm, int grpIdx) // return false if no scanning here
            // prm is taken only for grpIdx = 0
        {
            if (grpIdx.Equals(0)) 
            {
                scanFactor = null; 
                foreach (Factor factor in Factors)
                {
                    if(!factor.Enabled) continue;
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
                if (!BlockMode) Send2HW("_others_", (object)false);
                UpdateDevice(false);
                scanFactor.Scanning("");
                scanFactor = null;
                return true;
            }
            // value is taken from parameters
            if (BlockMode) Send2HW("_block_", (object)true);
            if (!scanFactor.SendValue(BlockMode)) ErrorMng.errorMsg("Problem with factor <" + scanFactor.fName + ">",452);      
            return true;
        }
        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (!genOpt_Enabled)
            {
                ErrorMng.Log("Error: the device is not Enable (options)!", Brushes.DarkRed.Color);
                return;
            }
            if (!HW_Enabled)
            {
                ErrorMng.Log("Error: the device is not available!", Brushes.DarkRed.Color);
                return;
            }
            if (!BlockMode) Send2HW("_others_", (object)true);
            UpdateDevice(true);
        }
    }
}