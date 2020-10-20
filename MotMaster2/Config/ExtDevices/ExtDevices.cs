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
    public interface IExtDevice
    {
        string dvcName { get; }
        bool GetEnabled(bool ignoreHardware = false); // ready to operate

        bool OptEnabled();
        bool CheckHardware();

        void Init(ref Sequence _sequenceData, ref GeneralOptions _genOptions); // params, opts; call after creating factors
        void Final(); // closing stuff and save state 

        GeneralOptions genOpt { get; set; }
        bool UpdateFromOptions(ref GeneralOptions _genOptions);
        Sequence seqData { get; set; }
        void UpdateFromSequence(ref Sequence _sequenceData); 

        bool UpdateDevice(bool ignoreMutable = false); // update all valid factors

        bool IsScannable(string fct = "");
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
                if (dvc.GetEnabled()) rslt &= dvc.UpdateFromOptions(ref _genOptions);
            }
            return rslt;
        }
        public void UpdateFromSequence(ref Sequence _sequenceData)
        {            
            foreach (IExtDevice dvc in this.Values)
            {
                if (dvc.GetEnabled()) dvc.UpdateFromSequence(ref _sequenceData);
            }
        }

        public void DataChangedEvent(string what, ref object data)
        {
            switch (what)
            {
                case ("options"):
                    GeneralOptions _genOptions = (GeneralOptions)data;
                    foreach (IExtDevice dvc in this.Values) dvc.UpdateFromOptions(ref _genOptions);
                    break;
                case ("sequence"):
                    Sequence _sequenceData = (Sequence)data;
                    foreach (IExtDevice dvc in this.Values) dvc.UpdateFromSequence(ref _sequenceData);
                    break;
            }
        }

         public bool UpdateDevices()
        {
            bool rslt = true;
            foreach (IExtDevice dvc in this.Values)
            {
                if (dvc.GetEnabled()) rslt &= dvc.UpdateDevice();
            }
            return rslt;
        }

        public bool ScanIter(string prm, int grpIdx) // grpIdx 0 - start; -1 - final 
        {
            bool rslt = true;
            foreach (IExtDevice dvc in this.Values)
            {
                if (dvc.GetEnabled()) rslt &= dvc.ScanIter(prm, grpIdx);
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
