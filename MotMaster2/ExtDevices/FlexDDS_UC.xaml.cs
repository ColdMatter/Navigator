using System;
using System.IO;
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
using DAQ.HAL;
using ErrorManager;
using UtilsNS;
using System.Security.Policy;

namespace MOTMaster2.ExtDevices
{
    public class FlexDDS_HW : RS232Instrument
    {
        public FlexDDS_HW(string address) : base(address)
        {
            
        }
        public bool Connected { get { return connected; } }

        public void Send2HW(string command, bool keepOpen = true)
        {
            Write(command, keepOpen);
        }
    }

    public class DDS_script
    {        
        public DDS_script(string[] _template)
        {
            AsList = new List<string>(_template);
        }
        public DDS_script(string __filename)
        {
            Open(__filename);
        }

        public List<string> AsList { get; private set; }
        public string[] AsArray 
        { 
            get { return AsList.ToArray(); } 
            set { AsList = new List<string>(value); }
        }
        public List<string> ExtractFactors(bool select, out bool correct) 
        {
            correct = true;
            List<string> rslt = new List<string>();
            if (AsList.Count == 0) return rslt;
            
            foreach (string line in AsList)
            {
                int j = 0;
                if (line.Length < 2) continue;
                while (line.IndexOf('$', j + 1) > -1)
                {
                    int i = line.IndexOf('$',j + 1);
                    if (i == -1) continue;
                    j = line.IndexOf('$', i + 1);
                    if (j == -1)
                    {
                        ErrorMng.errorMsg("Missing closing $ in " + line, 123); correct = false;
                        break;
                    }
                    string fct = line.Substring(i + 1, j - i - 1);
                    if (fct.IndexOf("select-") == 0)
                    {
                        if (select) rslt.Add(fct);
                    }
                    else
                    {
                        if (!select) rslt.Add(fct);
                    }
                }
             }           
            return rslt;
        }
        public DDS_script clone() { return new DDS_script(AsArray); }
        public void GetFromTextBox(TextBox textBox)
        {
            AsList.Clear();
            int lineCount = textBox.LineCount;
            for (int line = 0; line < lineCount; line++)
            {
                string ss = textBox.GetLineText(line);
                AsList.Add(ss.Replace("\r",""));   
            }                                       
        }
        public void SetToTextBox(TextBox textBox)
        {
            textBox.Text = "";
            foreach (string line in AsList)
            {
                textBox.Text += line+"\r";
            }
        }
        private string _filename;
        public string filename { get { return _filename; } }
        public bool Open(string fn)
        {
            if (!File.Exists(fn)) return false;
            _filename = fn;
            AsArray = File.ReadAllLines(fn);
            return true;
        }
        public void Save(string fn = "")
        {
            if (!fn.Equals("")) _filename = fn;
            if (filename.Equals(""))
            {
                ErrorMng.errorMsg("Missing filename", 223); return;
            }
            File.WriteAllLines(filename, AsArray);
        }

        public List<string> actualScript(Dictionary<string,string> fcts)
        {
            List<string> ls = new List<string>();
            foreach (string line in AsList)
            {
                string ss = line;
                foreach (var fct in fcts)
                {
                    string fctName = '$' + fct.Key + '$';
                    if (line.IndexOf(fctName) > -1) ss = ss.Replace(fctName, fct.Value);
                }
                if (ss.IndexOf('$') > -1)
                {
                    ErrorMng.errorMsg("Missing factor in " + line, 323); continue;
                }
                ls.Add(ss);
            }
            return ls;
        }

        public string actualScriptAsString(Dictionary<string, string> fcts)
        {
            List<string> ls = actualScript(fcts);
            string script = "";
            foreach (string line in ls)
            {
                script += line + "\r";
            }
            return script;
        }
    }
    /// <summary>
    /// Interaction logic for FlexDDS_UC.xaml
    /// </summary>
    public partial class FlexDDS_UC : UserControl, IExtDevice
    {
        FlexDDS_HW flexDDS_HW;
        DDS_script script;
        List<GroupBox> SelectFactors;
        public FlexDDS_UC(string __dvcName, Brush brush)
        {
            InitializeComponent();
            _dvcName = __dvcName;
            grpBox.Header = dvcName;
            grpBox.BorderBrush = brush;
            flexDDS_HW = new FlexDDS_HW("102.0.45.87");
            ucExtFactors.BlockMode = true;
            tiMain.Visibility = Visibility.Collapsed; tiEdit.Visibility = Visibility.Collapsed; tiTest.Visibility = Visibility.Collapsed;
            SelectFactors = new List<GroupBox>(); SelectFactors.Add(gbTrigger); SelectFactors.Add(gbBoolean);
        }
        protected string _dvcName;
        public string dvcName { get { return _dvcName; } }
        Dictionary<string, string> seqFactors; // ext.name, name
        public bool GetEnabled(bool ignoreHardware = false) // ready to operate
        {
            return OptEnabled() && ignoreHardware ? true : CheckHardware();
        }
        public GeneralOptions genOpt { get; set; }
        public bool OptEnabled()
        {
            if (Utils.isNull(genOpt)) return false;
            else return genOpt.FlexDDSEnabled;
        }
        protected bool lastCheckHardware = false;
        public bool CheckHardware()
        {
            if (Controller.config.Debug) lastCheckHardware = true;
            else // check connection to the device
            {
                if (Utils.isNull(flexDDS_HW)) lastCheckHardware = false;
                else
                {
                    if (OptEnabled() && !flexDDS_HW.Connected) flexDDS_HW.Connect();
                    lastCheckHardware = flexDDS_HW.Connected;
                }
            }
            return lastCheckHardware;
        }
        public bool Talk2Dvc(string fctName, object fctValue) // hardware update
        {
            if (!fctName.Equals("_block_")) // only block command is allowed
            {
                ErrorMng.Log("Error: the device <" + dvcName + "> accepts only block commands!", Brushes.Red.Color);
                return false;
            }
            if (fctName.Equals("_others_")) return UpdateOthers(Convert.ToBoolean(fctValue)); // recursive           
            if (!OptEnabled())
            {
                ErrorMng.Log("Error: the device <" + dvcName + "> is not Enabled (options)!", Brushes.Red.Color);
                return false;
            }
            if (Controller.config.Debug) return true;
            if (!CheckHardware())
            {
                ErrorMng.Log("Error: the device <" + dvcName + "> is not available!", Brushes.Red.Color);
                return false;
            }
            if (!GetEnabled(true)) return false;
            
            // generate DDS script
            string scr = script.actualScriptAsString(replacements());
            if (scr.Equals(""))
            {
                ErrorMng.Log("Error: no script available!", Brushes.Red.Color); return false;
            }
            flexDDS_HW.Send2HW(scr);
            return true;
        }
        public void Init(ref Sequence _sequenceData, ref GeneralOptions _genOptions) // params, opts
        {
            seqFactors = Utils.readDict(Utils.configPath + "seq.factors.FDDS");
            foreach (var pr in seqFactors)
            {
                ucExtFactors.AddFactor(pr.Value, pr.Key, true);
            }
            factorRow.Height = new GridLength(ucExtFactors.GetHeight());
            ucExtFactors.Init(); UpdateFromOptions(ref _genOptions);
            ucExtFactors.UpdateFromSequence(ref _sequenceData);
            ucExtFactors.OnSend2HW += new FactorsUC.Send2HWHandler(Talk2Dvc);
            // load config file
            Dictionary<string, string> cfg = Utils.readDict(Utils.configPath + dvcName + ".CFG");
            ucExtFactors.factorsState = cfg;
        }
        public void Final() // closing stuff and save state 
        {
            // save config file
            Dictionary<string, string> cfg = ucExtFactors.factorsState;
            Utils.writeDict(Utils.configPath + dvcName + ".CFG", cfg);
            // Disconnect
            if (!Utils.isNull(flexDDS_HW))
                if (flexDDS_HW.Connected) flexDDS_HW.Disconnect();
        }
        const double freqStep = 232.83064E-3; // [Hz] -> frequency step
        const double degreeStep = 5.4933317E-3; // [deg]
        const int degreeOffset = 1073676288;
        const double amplFull = -2; // full amplitude [dBm]
        const double usStep = 1.024; // microsec step
        const double nsStep = 0.125; // nanosec step (high-res mode)
        
        public Dictionary<string,string> replacements() // 
        {
            Dictionary<string, string> repl = new Dictionary<string, string>();
            foreach (var pr in seqFactors)
            {
                int k = ucExtFactors.IdxFromName(pr.Key);
                if (k == -1) continue;
                if (!ucExtFactors.Factors[k].Enabled) continue;
                double fct = ucExtFactors.Factors[k].getReqValue(0); // default 0 ???
                string fnm = ucExtFactors.Factors[k].fName;
                int i = fnm.IndexOf('['); int j = fnm.IndexOf(']', i + 1);
                if (!((i > -1) && (j > -1))) 
                {
                    	ErrorMng.Log("Error: no units for "+ucExtFactors.Factors[k].fName, Brushes.Red.Color); continue;
            	    }
                // rescale from user units to internal units                               
                string units = fnm.Substring(i + 1, j - i - 1);
                if ((units == "us") || (units == "ns"))  // time is decimal
                {
	               	switch (units) 
	                {
	                	case "us": fct = fct / usStep;
	                		repl[pr.Key] = ((int)Math.Round(fct)).ToString();		
	                		break;
	                	case "ns": fct = fct /nsStep;
	                		repl[pr.Key] = ((int)Math.Round(fct)).ToString()+"h";		
	                		break;
	 				} 
	 				j = (int)Math.Round(fct);  
	 				continue;             
                }
                switch (units) // rescale to internal units
                {
                    case "Hz": fct = fct / freqStep;
                        break;
                    case "kHz": fct = 1E3 * fct / freqStep;
                        break;
                    case "MHz": fct = 1E6 * fct / freqStep;
                        break;
                    case "deg": fct = degreeOffset + fct / degreeStep;
                        break; 
                    case "dBm": double up = (fct - amplFull)/20;
                    	fct = Math.Pow(10, up) * (Math.Pow(2,14)-1);
                        break;                                                  
                }                
                j = (int)Math.Round(fct);
                string sval = Convert.ToString(j, 16);
                if (sval.Length > 8)
                {
                    ErrorMng.Log("Error: Hexadecimal too long -> "+sval, Brushes.Red.Color); continue;
                }
                string tval = new String('0', 8 - sval.Length);
                repl[pr.Key] = tval + sval;
            }
            if (gbTrigger.Visibility == Visibility.Visible) repl[Convert.ToString(gbTrigger.Header)] = cbTrigger.Text;
            if (gbBoolean.Visibility == Visibility.Visible) repl[Convert.ToString(gbBoolean.Header)] = cbBoolean.Text;
            return repl;
        }
        public void UpdateFromOptions(ref GeneralOptions _genOptions)
        {
            genOpt = _genOptions;
            ucExtFactors.UpdateEnabled(genOpt.FlexDDSEnabled, CheckHardware());
        }
        public bool UpdateOthers(bool ignoreMutable = false) // update all non-factors (others)
        {
            if (!ignoreMutable && !ucExtFactors.chkMutable.IsChecked.Value) return false;
            bool bb = true;
            return bb;
        }
        public bool UpdateDevice(bool ignoreMutable = false)
        {
            return ucExtFactors.UpdateDevice(ignoreMutable) && UpdateOthers(ignoreMutable);
        }
        private void cbTemplates_DropDownOpened(object sender, EventArgs e)
        {
            string[] files = Directory.GetFiles(Utils.configPath, "*.dds");
            cbTemplates.Items.Clear();
            foreach (string file in files)
            {
                ComboBoxItem cbi = new ComboBoxItem(); cbi.Content = System.IO.Path.GetFileName(file);
                cbTemplates.Items.Add(cbi);
            }
        }
        private void cbTemplates_DropDownClosed(object sender, EventArgs e)
        {
            string fn = Utils.configPath + cbTemplates.Text;
            if (!File.Exists(fn))
            {
                ErrorMng.errorMsg("No such file: "+fn, 122); return;
            }
            script = new DDS_script(fn);
            SetAllFactors();
        }
        private void SetAllFactors()
        {              
            bool correct;
            List<string> fcts = script.ExtractFactors(false, out correct);
            SetFactors(fcts);
            List<string> efcts = script.ExtractFactors(true, out correct);
            SetSelectFactors(efcts);
        }

        private void SetFactors(List<string> fcts)
        {
            foreach (Factor fct in ucExtFactors.Factors)
                fct.Enabled = false;
            foreach (string fct in fcts)
            {
                if (!seqFactors.ContainsKey(fct))
                {
                    ErrorMng.errorMsg("No such factor: " + fct, 124); continue;
                }
                int i = ucExtFactors.IdxFromName(fct);
                if (i.Equals(-1))
                {
                    ErrorMng.errorMsg("Cannot find factor: " + fct, 125); continue;
                }
                ucExtFactors.Factors[i].Enabled = true;
            }
            factorRow.Height = new GridLength(ucExtFactors.GetHeight());
        }
        private void SetSelectFactors(List<string> fcts)
        {
            foreach (GroupBox gb in SelectFactors)
            {
                if (fcts.IndexOf(Convert.ToString(gb.Header)) > -1) gb.Visibility = Visibility.Visible;
                else gb.Visibility = Visibility.Collapsed;
            }
        }
        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (Utils.isNull(script))
            {
                ErrorMng.errorMsg("No template opened. ", 144); return;
            }
            tabControl.SelectedIndex = 1;
            script.SetToTextBox(tbTemplate);
        }
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            tabControl.SelectedIndex = 0;
        }
        private void btnAccept_Click(object sender, RoutedEventArgs e)
        {
            script.GetFromTextBox(tbTemplate);
            if(sender == btnSaveAccept)
            {
                script.Save();
            }
            SetAllFactors();
            tabControl.SelectedIndex = 0;
        }
        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            if (Utils.isNull(script))
            {
                ErrorMng.errorMsg("No template opened. ", 144); return;
            }
            tabControl.SelectedIndex = 2;
            List<string> actualScript = script.actualScript(replacements());
            tbTest.Text = "";
            foreach (string line in actualScript)
            {
                tbTest.Text += line + "\r";
            }
        }
     }
}
