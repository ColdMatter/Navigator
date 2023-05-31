using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace AOMmaster
{
    public struct CustiomSemiSetting
    {
        public string Title;
        public AnalogConfig AO1, AO2;
        public string titleEnabled;
        public int chnEnabled;
    }
    /// <summary>
    /// Interaction logic for SemiAxisUC.xaml
    /// </summary>
    public partial class SemiAxisUC : UserControl
    {
        private int chnEnabled;
        private string title;
        public bool Custom { get; set; } // degroup enabled from analog
        public bool locked = false; // lock out visual 
        public SemiAxisUC()
        {
            InitializeComponent();
        }
        public delegate void LogHandler(string txt, bool detail);
        public event LogHandler OnLog;

        protected void LogEvent(string txt, bool detail)
        {
            if (OnLog != null) OnLog(txt, detail);
        }

        public delegate bool DigitalChangedHandler(int chn, bool Value);
        public event DigitalChangedHandler OnDigitalChanged;
        protected bool DigitalChanged(int chn, bool Value)
        {
            if (OnDigitalChanged != null) return OnDigitalChanged(chn, Value);
            else return false;
        }
        bool _Enabled;
        public bool Enabled 
        {
            get { return _Enabled; } 
            set 
            { 
                _Enabled = value;
                if (IsEnabled)
                {
                    if (DigitalChanged(chnEnabled, value))
                    {
                        if (!Custom)
                        {                       
                            if (value)
                            {
                                groupBox.Header = title + " (Enabled)";
                                analogVCO.UpdateValue(); analogVCA.UpdateValue();
                            }
                            else groupBox.Header = title + " (Disabled)";
                        }
                    }
                    else UtilsNS.Utils.TimedMessageBox("Hardware digital error ! (talk to Theo)");
                }
                if (!locked) chkEnabled.IsChecked = value;
             } 
        }
        public double VCO_V // voltage
        {
            get { return analogVCO.voltage; } 
            set 
            { 
                if (locked) return;
                analogVCO.target = value; 
            } 
        }
        public bool VCO_U //unit
        {
            get { return analogVCO.isUnits; }
            set { analogVCO.isUnits = value; }
        }
        public double VCA_V // voltage
        {
            get { return analogVCA.voltage; }
            set 
            {
                if (locked) return;
                analogVCA.target = value;
            }
        }
        public bool VCA_U //unit
        {
            get { return analogVCA.isUnits; }
            set { analogVCA.isUnits = value; }
        }

        private void chkEnabled_Checked(object sender, RoutedEventArgs e)
        {
            locked = true;
            Enabled = chkEnabled.IsChecked.Value;
            locked = false;
        }
        public void Config(string _title, int _chnEnabled, AnalogConfig vco, AnalogConfig vca, Color? clr = null)
        {
            groupBox.Header = _title; title = _title;
            chnEnabled = _chnEnabled;
            vco.groupTitle = title; vca.groupTitle = title;
            analogVCO.Config(vco); analogVCA.Config(vca);
            Color ForeColor = clr.GetValueOrDefault(Brushes.Black.Color);
            groupBox.BorderBrush = new System.Windows.Media.SolidColorBrush(ForeColor);
        }
        public void Config(string _title, string _titleEnabled, int _chnEnabled, AnalogConfig vco, AnalogConfig vca, Color? clr = null)
        {
            Config(_title, _chnEnabled, vco, vca, clr);
            if (_titleEnabled.Equals("")) chkEnabled.Visibility = Visibility.Collapsed;
            else chkEnabled.Content = _titleEnabled;
            if (vco.title.Equals("")) analogVCO.Visibility = Visibility.Collapsed;
            if (vca.title.Equals("")) analogVCA.Visibility = Visibility.Collapsed;
        }
        public void Closing()
        {
            analogVCO.Closing(); analogVCA.Closing();
        }
    }
}
