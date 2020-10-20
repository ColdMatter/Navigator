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

using MOTMaster2.SequenceData;

namespace MOTMaster2.ExtDevices
{
    /// <summary>
    /// Interaction logic for Factor.xaml
    /// </summary>
    public partial class Factor : UserControl
    {
        public Factor(string _fName, string _extName = "")
        {
            InitializeComponent();
            if (_fName.Equals("")) throw new Exception("No name Factor!");
            fName = _fName;
            if (_extName.Equals("")) extName = fName;
            else extName = _extName;
            VisUpdate();
        }

        public enum factorType
        {
            ftNone, ftValue, ftParam
        }

        public string fName { get; private set; } // visual name
        public string extName { get; private set; } // name in the hardware

        public delegate double EstimateHandler(string operand); // get the value of param
        public event EstimateHandler OnEstimate;
        protected double Estimate(string operand)
        {
            if (OnEstimate != null) return OnEstimate(operand);
            else return Double.NaN;
        }
        public bool isEstimateAttached()
        {
            EstimateHandler[] handlers = (EstimateHandler[])OnEstimate.GetInvocationList();
            return handlers.Length > 0;
        }

        public double getReqValue() // required value
        {
            switch (fType)
            {
                case factorType.ftNone:
                    return Double.NaN;
                case factorType.ftValue:
                    return Convert.ToDouble(cbFactor.Text);
                case factorType.ftParam:
                    return Estimate(cbFactor.Text);
                default: return Double.NaN;
            }           
        }
        private double _fValue = Double.NaN;
        public double fValue // the value set in device
        {
            get { return _fValue; }
            set { _fValue = value; VisUpdate(); }
        }

        public factorType fType 
        { 
            get
            { 
                double d; 
                factorType rslt = factorType.ftNone;
                // if is valid double
                if (Double.TryParse(cbFactor.Text, out d)) rslt = factorType.ftValue;
                // if is in the list
                else 
                    if ((cbFactor.Items.IndexOf(cbFactor.Text) > -1) && (!cbFactor.Text.Equals("- - -"))) rslt = factorType.ftParam;
                return rslt;
            } 
        }

        /*public void Init(string _fName, string _extName)
        {
            fName = _fName; extName = _extName;
            VisUpdate();
        }*/
        public delegate bool Send2DvcHandler(string fctName, double fctValue); // send the value of param
        public event Send2DvcHandler OnSend2Dvc;
        protected bool Send2Dvc(string fctName, double fctValue)
        {
            if (OnSend2Dvc != null) return OnSend2Dvc(fctName, fctValue);
            else return false;
        }

        public void VisUpdate() // 
        {
            if (Double.IsNaN(fValue)) lbFactor.Content = fName;
            else lbFactor.Content = fName + " -> " + fValue.ToString("G5");
        }

        public string Text
        {
            get { return cbFactor.Text; }
            set { cbFactor.Text = value; }
        }

        public bool SendValue()
        {
            double d; bool b;
            switch (fType)
            {
                case factorType.ftNone:
                    fValue = Double.NaN; return true;                   
                case factorType.ftValue:
                case factorType.ftParam:
                    d = getReqValue(); b = Send2Dvc(extName,d);
                    if (b) fValue = d;
                    return b;
                default: return false;
            }
        }

        public void ParamUpdate(ObservableDictionary<string, Parameter> Parameters) // when params list set or change
        {
            string txt = cbFactor.Text; factorType kind = fType; 
            cbFactor.Items.Clear(); cbFactor.Items.Add("- - -");
            foreach (KeyValuePair<string, Parameter> prm in Parameters)
            {
                if (prm.Value.IsLaser) cbFactor.Items.Add(prm.Key);
            } 
            switch (kind)
            {
                case factorType.ftNone: cbFactor.Text = "- - -";
                    break;
                case factorType.ftValue: cbFactor.Text = txt;
                    break;
                case factorType.ftParam:
                    if (fType == factorType.ftParam) cbFactor.Text = txt;
                    else cbFactor.Text = "- - -";
                    break;
            }
        }

        public bool Scanning(string scanPrm) // before scan check the factor; after scan restore with scanPrm = ""
        {
            bool sc = false;
            if (fType == factorType.ftParam) sc = (cbFactor.Items.IndexOf(scanPrm) > -1) && (!cbFactor.Text.Equals("- - -"));
            if (sc) gridCanvas.Background = Brushes.LightYellow;
            else gridCanvas.Background = Brushes.Transparent;
            return sc;
        }
    }
}
