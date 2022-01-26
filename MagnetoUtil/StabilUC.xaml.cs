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
using UtilsNS;

namespace MagnetoUtil
{
    /// <summary>
    /// Interaction logic for StabilUC.xaml
    /// </summary>
    public partial class StabilUC : UserControl
    {
        public StabilUC()
        {
            InitializeComponent();
        }
        private List<double> iStack, dStack;
        public void Init()
        {
            iStack = new List<double>(); dStack = new List<double>();
        }
        public double PID(double sensorA, double sensorC)
        {
            double rslt = Double.NaN;
            Dictionary<string, double> rpt = new Dictionary<string, double>();
            rpt["sensor A"] = sensorA; rpt["sensor C"] = sensorC;
            double sensor = (sensorA + sensorC) / 2;
            rpt["pTerm"] = sensor;
            // integral
            iStack.Add(sensor); while (iStack.Count > ndKIdepth.Value) iStack.RemoveAt(0);
            double iTerm = iStack.Average();
            rpt["iTerm"] = iTerm;
            double iTermSD = 0;
            foreach (double d in iStack)
            {
                iTermSD += (d - iTerm) * (d - iTerm);
            }
            iTermSD = Math.Sqrt(iTermSD / (double)iStack.Count);
            rpt["iTermSD"] = iTermSD;
            // differential
            dStack.Add(sensor); while (dStack.Count > ndKDdepth.Value) dStack.RemoveAt(0);
            double dTerm = 0;
            for (int i = 0; i < dStack.Count - 1; i++)
            {
                dTerm += dStack[i + 1] - dStack[i];
            }
            dTerm /= Math.Max(dStack.Count - 1, 1);
            rpt["dTerm"] = dTerm;

            rslt = -(numKP.Value * sensor + numKI.Value * iTerm + numKD.Value * dTerm);
 
            lbCurrVal.Content = "Value [V] = " + rslt.ToString("G5");
            Utils.dict2ListBox(Utils.dictDouble2String(rpt, "G5"), lbReport);
            return rslt;
        }

    }
}
