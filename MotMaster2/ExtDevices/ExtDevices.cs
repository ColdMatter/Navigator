using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MOTMaster2;
using MOTMaster2.SequenceData;

namespace MOTMaster2.ExtDevices
{
    public interface IExtDevice // everything not factor specific 
    {       
        //string dvcName { get; }
        bool GetEnabled(bool ignoreHardware = false); // ready to operate
        bool Talk2Dvc(string fctName, object fctValue);

        bool OptEnabled();
        bool CheckHardware();

        void Init(ref Sequence _sequenceData, ref GeneralOptions _genOptions); // params, opts; call after creating factors
        void Final(); // closing stuff and save state 

        GeneralOptions genOpt { get; set; }
        void UpdateFromOptions(ref GeneralOptions _genOptions);

        bool UpdateDevice(bool ignoreMutable = false); // update all factors and others

    }
    public interface IFactors // everything factor specific 
    {
        bool genOpt_Enabled { get; }
        bool HW_Enabled { get; }
        bool UpdateEnabled(bool _genOpt_Enabled, bool _HW_Enabled);
        Sequence seqData { get; set; }
        void UpdateFromSequence(ref Sequence _sequenceData); 
        bool IsScannable(string prm);
        bool ScanIter(string prm, int grpIdx); // 0 - start; -1 - final
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
                if (dvc.GetEnabled()) dvc.UpdateFromOptions(ref _genOptions);
            }
            return rslt;
        }

        public bool UpdateDevices(bool ignoreMutable = false)
        {
            bool rslt = true;
            foreach (IExtDevice dvc in this.Values)
            {
                if (dvc.GetEnabled()) rslt &= dvc.UpdateDevice(false);
            }
            return rslt;
        }
    }

    public class ExtFactorList : List<IFactors> // list of factor groups (one for each ext.device)
    {
        public ExtFactorList()
        {

        }
        public void UpdateFromSequence(ref Sequence _sequenceData)
        {            
            foreach (IFactors dvc in this)
            {
                if (dvc.genOpt_Enabled && dvc.HW_Enabled) dvc.UpdateFromSequence(ref _sequenceData);
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
    }
        /*
          ExtDeviceDict ed = new ExtDeviceDict();
          MSquare ms = new MSquare("MSquare")
          WindFreak wf = new WindFreak("WindFreak")
          ed.Add("MSquare",ms) dvcStack.Children.Add(ms)
          ed.Add("WindFreak",wf) dvcStack.Children.Add(wf)

         * +events !
        */
    }
