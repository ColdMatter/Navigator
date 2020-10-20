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
    /// Interaction logic for MSquared.xaml
    /// </summary>
    public partial class MSquared : ucExtDevice
    {
        public override bool OptEnabled()
        {
            if (Utils.isNull(genOpt)) return false; 
            else return genOpt.m2Enabled;
        }

        public override bool CheckHardware()
        {
            if (Controller.config.Debug) lastCheckHardware = true;
            // ping pong with the device
            else lastCheckHardware = false;
            return (bool)lastCheckHardware;
        }

        public override bool Send2Dvc(string fctName, double fctValue) // hardware update
        {
            return true;
        }

        public MSquared(string _dvcName) : base(_dvcName)
        {
            
        }

        public override void Init(ref Sequence _sequenceData, ref GeneralOptions _genOptions) // params, opts
        {           
            Factors.Add("aaa", new Factor("aaa"));
            Factors.Add("bbb", new Factor("bbb"));
            Factors.Add("ccc", new Factor("ccc"));
            UpdateFactors();
            base.Init(ref _sequenceData, ref _genOptions);
        }
    }
}
