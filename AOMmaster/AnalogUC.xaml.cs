using Microsoft.SqlServer.Server;
using NationalInstruments.Controls;
using NationalInstruments.Controls.Rendering;
using NationalInstruments.Analysis.Math;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UtilsNS;

namespace AOMmaster
{
    public struct AnalogConfig
    {
        public string groupTitle;
        public string title;
        public int chnNumb;
        public double minVolt, maxVolt; // hardware limits
        public string calUnit;
        public List<Point> calibr;

        public void Assign(AnalogConfig ac)
        {
            groupTitle = ac.groupTitle; title = ac.title;
            chnNumb = ac.chnNumb;
            minVolt = ac.minVolt; maxVolt = ac.maxVolt;
            calUnit = ac.calUnit;

            calibr = new List<Point>(); 
            foreach (Point p in ac.calibr)
            {
                calibr.Add(new Point(p.X,p.Y));
            }
        }
        public bool isCalibr()
        {
            if (Utils.isNull(calibr)) return false;
            return (calUnit != "") && (calibr.Count > 0);
        }
        public string calibrFile()
        {
            return Utils.basePath + "\\calibration\\" + groupTitle + "_" + title;
        } 
        public bool ReadCalibr(string fn = "") // default call - no arg.
        {
            string ffn; calUnit = "";
            if (fn == "") ffn = calibrFile();
            else ffn = fn;
            ffn = System.IO.Path.ChangeExtension(ffn, ".cal");
            if (!File.Exists(ffn)) return false;           
            if (Utils.isNull(calibr)) calibr = new List<Point>();
            string json = System.IO.File.ReadAllText(ffn);
            JObject j0 = JObject.Parse(json);
            calUnit = j0["units"].ToString();
            IList <JToken> cal = j0["calibration"].Children().ToList();
            calibr.Clear();
            foreach (JToken cl in cal)
            {
                calibr.Add(cl.ToObject<Point>());
            }
            return true;
        }
        public bool WriteCalibr(string fn = "") // default call - no arg.
        {
            string ffn; 
            if (fn == "") ffn = calibrFile();
            else ffn = fn;
            ffn = System.IO.Path.ChangeExtension(ffn, ".cal");
            if (Utils.isNull(calibr)) return false;
            if (calibr.Count == 0) return false;
            Dictionary<string, object> calibrData = new Dictionary<string, object>();
            calibrData["units"] = (object)calUnit; 
            calibrData["calibration"] = calibr;
            string json = JsonConvert.SerializeObject(calibrData);
            System.IO.File.WriteAllText(ffn, json);
            return true;
        }
    }
    /// <summary>
    /// Interaction logic for AnalogUC.xaml
    /// </summary>
    public partial class AnalogUC : UserControl
    {
        public AnalogConfig analogConfig;
        public AnalogUC()
        {
            InitializeComponent();
        }
        private bool _Selected;
        public bool Selected // for calibration
        {
            get { return _Selected; }
            set
            {
                _Selected = value;
                if (value) gridAnalog.Background = new System.Windows.Media.SolidColorBrush(Brushes.LemonChiffon.Color);
                else gridAnalog.Background = null;
                if (_Selected) cbUnits.SelectedIndex = 0;
                else
                {
                    UpdateValue(); checkCalibr();
                }                   
                cbUnits.IsEnabled = !_Selected;
            }
        }
        public delegate void SelectHandler(ref AnalogConfig ac);
        public event SelectHandler OnSelect;
        protected void SelectEvent(ref AnalogConfig ac)
        {
            if (OnSelect != null) OnSelect(ref ac);
        }

        public delegate void LogHandler(string txt, bool detail);
        public event LogHandler OnLog;

        protected void LogEvent(string txt, bool detail)
        {
            if (OnLog != null) OnLog(txt, detail);
        }

        public delegate bool AnalogChangedHandler(int chn, double volt);
        public event AnalogChangedHandler OnAnalogChanged;
        protected bool AnalogChanged(int chn, double volt)
        {
            if (OnAnalogChanged != null) return OnAnalogChanged(chn, volt);
            else return false;
        }
        private bool locked = false;
        public double voltage { get; set; } // in volts
        public double target // acts conditionally of isUnits 
        {
            get { return isUnits ? calV2unit(voltage) : voltage;  }
            set
            {
                voltage = isUnits ? calUnit2V(value) : value;
                // call hw               
                if (IsEnabled)
                {
                    if (AnalogChanged(analogConfig.chnNumb, voltage))
                    {
                        string unt = "";
                        if (isUnits) unt = "[V] / "+ value.ToString("G5")+"["+analogConfig.calUnit+"]";
                        groupBox.Header = analogConfig.title + "-> " + voltage.ToString("G5")+unt;
                    }
                    else UtilsNS.Utils.TimedMessageBox("Hardware analog problem ! (talk to Theo)");
                }
                if (!locked) numValue.Value = value;
            }
        }

        public bool isUnits
        {
            get { return cbUnits.SelectedIndex == 1; }
            set 
            {
                if (cbUnits.Items.Count == 0)
                {
                    cbUnits.Items.Add("V");
                }
                if (cbUnits.Items.Count == 1)
                {
                    cbUnits.SelectedIndex = 0; return;
                }
                if (cbUnits.Items.Count == 2)
                {
                    if (value) cbUnits.SelectedIndex = 1; 
                    else cbUnits.SelectedIndex = 0;
                }
            }
        }
        private Range<double> GetNumRange()
        {
            if (isUnits)
            {
                if (!analogConfig.isCalibr()) throw new Exception("No calibration !!");
                return new Range<double>(analogConfig.calibr[0].Y, analogConfig.calibr[analogConfig.calibr.Count-1].Y);
            }
            else
            {
                return numValue.Range = new Range<double>(analogConfig.minVolt, analogConfig.maxVolt);
            }
        }
        private bool isConfig = false;
        public void Config(AnalogConfig ac, bool fileCalibr = true)
        {
            analogConfig.Assign(ac);
            if (fileCalibr) analogConfig.ReadCalibr();
            groupBox.Header = ac.title + "-> " + voltage.ToString("G5");
            numValue.Range = new Range<double>(ac.minVolt, ac.maxVolt);
            cbUnits.Items.Clear(); cbUnits.Items.Add("V"); cbUnits.SelectedIndex = 0;
            if (!checkCalibr()) return;
            cbUnits.Items.Add(analogConfig.calUnit);           
            isConfig = true;
        }
        public void Closing()
        {
            analogConfig.WriteCalibr();
        }
        public bool UpdateValue()
        {
            numValue_ValueChanged(null, null);
            return true;
        }
        private void numValue_ValueChanged(object sender, NationalInstruments.Controls.ValueChangedEventArgs<double> e)
        {
            locked = true;
            target = numValue.Value;
            locked = false;
        }
        #region calibration
        private void miCalibration_Click(object sender, RoutedEventArgs e)
        {
            if (!Selected) Selected = true;
            if (Selected) SelectEvent(ref analogConfig); 
         }

        private double spline(bool V2unit, double xValue)
        {
            if (Utils.isNull(analogConfig)) return Double.NaN;
            if (!analogConfig.isCalibr()) return Double.NaN;
            int cnt = analogConfig.calibr.Count; int k = 0;
            double[] xData = new double[cnt];
            double[] yData = new double[cnt];
            foreach (Point p in analogConfig.calibr)
            {
                if (V2unit)
                {
                    xData[k] = p.X; yData[k] = p.Y;
                }
                else
                {
                    xData[k] = p.Y; yData[k] = p.X;
                }
                k++;
            }
            double[] secondDerivatives;
            double initialBoundary, finalBoundary;

            // Causes SplineInterpolant method to set the initial boundary condition for a natural spine
            initialBoundary = 1.00E+30;
            // Causes SplineInterpolant method to set the final boundary condition for a natural spine
            finalBoundary = 1.00E+30;

            // Calculate secondDerivatives
            secondDerivatives = CurveFit.SplineInterpolant(xData, yData, initialBoundary, finalBoundary);

            // Calculate spline interpolated value  
            return CurveFit.SplineInterpolation(xData, yData, secondDerivatives, xValue);
        }
        public double calV2unit(double V)
        {
            if (!analogConfig.isCalibr()) return V;
            return spline(true, V); //2 * V;
        }
        public double calUnit2V(double unit)
        {
            if (!analogConfig.isCalibr()) return unit;
            return spline(false, unit); // unit / 2;
        }
        private bool checkCalibr()
        {
            bool bb = analogConfig.isCalibr();
            if (bb) groupBox.BorderBrush = new System.Windows.Media.SolidColorBrush(Brushes.DarkRed.Color);
            else groupBox.BorderBrush = new System.Windows.Media.SolidColorBrush(Brushes.Navy.Color);
            return bb;
        }
        private void cbUnits_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isConfig) return; numValue.Range = GetNumRange();
            if (cbUnits.SelectedIndex == 1) target = calV2unit(voltage);
            else target = voltage;
        }
       #endregion

     }
}
