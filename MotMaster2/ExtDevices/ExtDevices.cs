using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MOTMaster2;
using MOTMaster2.SequenceData;
using ErrorManager;
using UtilsNS;

namespace MOTMaster2.ExtDevices
{
    public interface IExtDevice // everything not factor specific 
    {       
        string dvcName { get; }
        bool CheckEnabled(bool ignoreHardware = false); // ready to operate
        bool Talk2Dvc(string fctName, object fctValue);

        bool OptEnabled();
        bool CheckHardware();

        void Init(ref Sequence _sequenceData, ref GeneralOptions _genOptions); // params, opts; call after creating factors
        void Final(); // closing stuff and save state 

        GeneralOptions genOpt { get; set; }
        void UpdateFromOptions(ref GeneralOptions _genOptions);
        string SequenceEvent(string EventName);
        bool UpdateDevice(bool ignoreMutable = false); // update all factors and others

    }
    public interface IFactors // everything factor specific 
    {
        string dvcName { get; set; }
        bool genOpt_Enabled { get; }
        bool groupUpdate { get; set; }
        bool HW_Enabled { get; }
        bool UpdateEnabled(bool _genOpt_Enabled, bool _HW_Enabled, bool mainEnabled = true);
        Sequence seqData { get; set; }
        void UpdateFromSequence(ref Sequence _sequenceData); 
        bool IsScannable(string prm);
        bool ScanIter(string prm, int grpIdx); // 0 - start; -1 - final
        bool SetFactor(string factor, string param);
    }
    
    public class ExtDeviceDict : ObservableDictionary<string, IExtDevice>
    {

        public ExtDeviceDict()
        {

        }

        public void Init(ref Sequence _sequenceData, ref GeneralOptions _genOptions)// params, opts
        {
            foreach (IExtDevice dvc in this.Values)
            {
                dvc.Init(ref _sequenceData, ref _genOptions);
            }
        }
        public void Final()
        {
            foreach (IExtDevice dvc in this.Values)
            {
                dvc.Final();
            }
        }

        public bool UpdateFromOptions(ref GeneralOptions _genOptions)
        {
            bool rslt = true;
            foreach (IExtDevice dvc in this.Values)
            {
                dvc.UpdateFromOptions(ref _genOptions); //if (dvc.GetEnabled()) 
                if (dvc.OptEnabled() && !dvc.CheckHardware()) ErrorMng.Log("<" + dvc.dvcName + "> is not operational.", Brushes.Coral.Color);
            }
            return rslt;
        }
        public Dictionary<string,string> SequenceEvent(string EventName)
        {
            Dictionary<string, string> se = new Dictionary<string, string>();

            foreach (IExtDevice dvc in this.Values)
            {
                if (dvc.CheckEnabled())
                {
                    se[dvc.dvcName] = dvc.SequenceEvent(EventName);
                }
            }
            return se;
        }
        /// <summary>
        /// summary of 
        /// </summary>
        /// <param name="SeqEvents"></param>
        /// <returns>true if "cancel" is detected on any dvc</returns>
        public bool DetectCancel(Dictionary<string, string> SeqEvents) 
        {
            bool bb = false;
            foreach (var pair in SeqEvents)
            {
                bb = bb || pair.Value.Equals("cancel");
            }
            return bb;
        }
        public bool UpdateDevices(bool ignoreMutable = false)
        {
            bool rslt = true;
            foreach (IExtDevice dvc in this.Values)
            {
                if (dvc.CheckEnabled()) rslt &= dvc.UpdateDevice(false);
            }
            return rslt;
        }
    }

    public class ExtFactorList : List<IFactors> // list of factor groups (one list for each ext.device)
    {
        public ExtFactorList()
        {

        }
        public void UpdateFromSequence(ref Sequence _sequenceData)
        {            
            foreach (IFactors dvc in this)
            {
                //if (dvc.genOpt_Enabled && dvc.HW_Enabled) 
                    dvc.UpdateFromSequence(ref _sequenceData);
            }
        }
        public bool IsScannable(string prm) // check if prm is in any factor; prm = "" - reset 
        {
            bool rslt = false;
            foreach (IFactors dvc in this)
            {
                if (dvc.genOpt_Enabled && dvc.HW_Enabled) rslt |= dvc.IsScannable(prm);
            }
            return rslt;
        }
        public bool ScanIter(string prm, int grpIdx) // grpIdx 0 - start; -1 - final 
        {
            bool rslt = true;
            foreach (IFactors dvc in this)
            {
                if (dvc.genOpt_Enabled && dvc.HW_Enabled) rslt &= dvc.ScanIter(prm, grpIdx);
            }
            return rslt;
        }
        public bool SetFactor(string dvc, string factor, string param)
        {
            bool rslt = false; 
            foreach (IFactors dvcItr in this)
            {
                if (dvcItr.dvcName.Equals(dvc))
                {
                    rslt = (dvcItr.genOpt_Enabled && dvcItr.HW_Enabled);
                    if (!rslt) Utils.TimedMessageBox("The requested device for scan is not active");
                    else rslt = dvcItr.SetFactor(factor, param);
                    break;
                }                    
            }
            if (!rslt) return false;
            return rslt;
        }
    }
}