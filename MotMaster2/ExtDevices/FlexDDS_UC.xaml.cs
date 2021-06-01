using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Specialized;
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
    /// <summary>
    /// Interaction logic for FlexDDS_UC.xaml
    /// </summary>
    public partial class FlexDDS_UC : UserControl, IExtDevice
    {
        FlexDDS_HW flexDDS_HW;
        DDS_script script;
        List<GroupBox> SelectFactors;
        public FlexDDS_UC(string __dvcName, SolidColorBrush brush)
        {
            InitializeComponent();
            _dvcName = __dvcName;
            grpBox.Header = dvcName;
            grpBox.BorderBrush = brush;
            flexDDS_HW = new FlexDDS_HW("102.0.45.87");
            ucExtFactors.groupUpdate = true;
            tiMain.Visibility = Visibility.Collapsed; tiEdit.Visibility = Visibility.Collapsed; tiTest.Visibility = Visibility.Collapsed;
            SelectFactors = new List<GroupBox>(); SelectFactors.Add(gbTrigger); SelectFactors.Add(gbBoolean);
        }
        protected string _dvcName;
        public string dvcName { get { return _dvcName; } }
        public bool CheckEnabled(bool ignoreHardware = false) // ready to operate
        {
            bool bb = OptEnabled() && (ignoreHardware ? true : CheckHardware());
            bool bc = bb && (cbTemplates.SelectedIndex > -1);
            btnEdit.IsEnabled = bc; btnTest.IsEnabled = bc; ucExtFactors.btnUpdate.IsEnabled = bc;
            return bc;
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
                ErrorMng.errorMsg("The device <" + dvcName + "> accepts only block commands!", 101); return false;
            }
            if (fctName.Equals("_others_")) return UpdateOthers(Convert.ToBoolean(fctValue)); // recursive           
            if (!OptEnabled())
            {
                ErrorMng.errorMsg("Tthe device <" + dvcName + "> is not Enabled (options)!", 102); return false;
            }
            if (Controller.config.Debug) return true;
            if (!CheckHardware())
            {
                ErrorMng.errorMsg("The device <" + dvcName + "> is not accesable!", 103); return false;
            }
            if (!CheckEnabled(true)) return false;
            
            // generate DDS script
            string scr = script.actualScriptAsString(script.replacements(ucExtFactors, OtherFactors()));
            if (scr.Equals(""))
            {
                ErrorMng.errorMsg("No script available!", 104); return false;
            }
            flexDDS_HW.Send2HW(scr);
            return true;
        }

        public void Init(ref Sequence _sequenceData, ref GeneralOptions _genOptions) // params, opts
        {
            factorRow.Height = new GridLength(ucExtFactors.UpdateFactors());
            ucExtFactors.Init(); UpdateFromOptions(ref _genOptions);
            ucExtFactors.UpdateFromSequence(ref _sequenceData);
            ucExtFactors.OnSend2HW += new FactorsUC.Send2HWHandler(Talk2Dvc);
            ucExtFactors.OnCheckHw += new FactorsUC.CheckHwHandler(CheckHardware);
            // load config file            
            Dictionary<string, string> cfg =  Utils.readDict(Utils.configPath + dvcName + ".CFG");
            if (!cfg.ContainsKey("LastScript"))
            {
                ErrorMng.errorMsg("LastScript is missing from " + Utils.configPath + dvcName + ".CFG", -437); return; 
            }
            cbTemplates_DropDownOpened(null, null);
            int j = -1;
            for (int i = 0; i < cbTemplates.Items.Count; i++)
            {
                string ss = ((ComboBoxItem)cbTemplates.Items[i]).Content.ToString(); 
                if (ss.Equals(cfg["LastScript"])) { j = i; break; }
            }
            if (j == -1)
            {
                ErrorMng.errorMsg(cfg["LastScript"] + " is missing.", -438); return;
            }
            cbTemplates.SelectedIndex = j;
            cbTemplates_DropDownClosed(null, null);
            CheckEnabled();
        }
        public void Final() // closing stuff and save state 
        {
            // save config file
            Dictionary<string, string> cfg = new Dictionary<string, string>();
            cfg["Mutable"] = ucExtFactors.chkMutable.IsChecked.Value.ToString();
            string lastScript;
            if (cbTemplates.SelectedIndex > -1) lastScript = cbTemplates.Text;
            else lastScript = System.IO.Path.GetFileName(script.filename);
            cfg["LastScript"] = lastScript;
            Utils.writeDict(Utils.configPath + dvcName + ".CFG", cfg);
            cfg = ucExtFactors.factorsState;
            if (cfg.ContainsKey("Mutable")) cfg.Remove("Mutable");
            Utils.writeDict(Utils.configPath + System.IO.Path.ChangeExtension(lastScript,".ds0"), cfg);
            // Disconnect
            if (!Utils.isNull(flexDDS_HW))
                if (flexDDS_HW.Connected) flexDDS_HW.Disconnect();
        }
        protected Dictionary<string, string> OtherFactors()
        {
            Dictionary<string, string> repl = new Dictionary<string, string>();
            if (gbTrigger.Visibility == Visibility.Visible) repl[Convert.ToString(gbTrigger.Header)] = cbTrigger.Text;
            if (gbBoolean.Visibility == Visibility.Visible) repl[Convert.ToString(gbBoolean.Header)] = cbBoolean.Text;
            return repl;
        }
        public void UpdateFromOptions(ref GeneralOptions _genOptions)
        {
            genOpt = _genOptions;
            ucExtFactors.UpdateEnabled(genOpt.FlexDDSEnabled, CheckHardware(), CheckEnabled());
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
            if (cbTemplates.SelectedIndex > -1) // save the previous tempalte values
            {
                Utils.writeDict(System.IO.Path.ChangeExtension(script.filename, ".ds0"), ucExtFactors.factorsState);
            }
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
            if (cbTemplates.SelectedIndex.Equals(-1))
            {
                ErrorMng.errorMsg("No template selected.", 121); return;
            }
            string fn = Utils.configPath + ((ComboBoxItem)cbTemplates.SelectedItem).Content.ToString();
            if (!File.Exists(fn))
            {
                ErrorMng.errorMsg("No such file: "+fn, 122); return;
            }
            script = new DDS_script(fn);
            SetAllFactors(true);           
            CheckEnabled();
        }      
        private void SetAllFactors(bool inclContent) // from script
        {              
            bool correct;
            List<string> fcts = script.ExtractFactors(false, out correct);
            SetFactors(fcts);
            List<string> efcts = script.ExtractFactors(true, out correct);
            SetSelectFactors(efcts);
            if (inclContent) ucExtFactors.factorsState = Utils.readDict(System.IO.Path.ChangeExtension(script.filename, ".ds0"));
            ucExtFactors.UpdateFromSequence();
        }
        private void SetFactors(List<string> fcts) // list from script section
        {
            ucExtFactors.Factors.Clear();
            DDS_factors ddsFcts = script.factorsSection;
            var ffs = new Dictionary<string, bool>(); // script ones with flags
            foreach (string ss in fcts)
            {
                int j = ddsFcts.IdxFromExtName(ss);
                if (j == -1) 
                {                   
                    ErrorMng.errorMsg("Missing declaration of factor <"+ss+">", 143); continue;
                }                  
                if (ddsFcts[j].Item3.Equals("")) ucExtFactors.AddFactor(ddsFcts[j].Item2, ddsFcts[j].Item1);
            }
            factorRow.Height = new GridLength(ucExtFactors.UpdateFactors());
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
            btnAccept.IsEnabled = false; btnSaveAccept.IsEnabled = false;
        }
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            tabControl.SelectedIndex = 0;
        }
        private void btnAccept_Click(object sender, RoutedEventArgs e)
        {
            script.GetFromTextBox(tbTemplate);
            if (sender == btnSaveAccept) script.Save();
            SetAllFactors(true);
            tabControl.SelectedIndex = 0;
        }
        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            if (Utils.isNull(script))
            {
                ErrorMng.errorMsg("No template opened. ", 144); return;
            }
            tabControl.SelectedIndex = 2;
            List<string> actualScript = script.actualScript(script.replacements(ucExtFactors,OtherFactors()));
            tbTest.Text = "";
            foreach (string line in actualScript)
            {
                tbTest.Text += line + "\r";
            }
        }
        private void tbTemplate_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnAccept.IsEnabled = true; btnSaveAccept.IsEnabled = true;
        }
        private void imgTripleBars_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ContextMenu cm = this.FindResource("cmTripleBars") as ContextMenu;
            cm.PlacementTarget = sender as Image;
            cm.IsOpen = true;
        }
        private void miCheckHw_Click(object sender, RoutedEventArgs e)
        {
            ucExtFactors.UpdateEnabled(genOpt.FlexDDSEnabled, CheckHardware(), CheckEnabled(false));
        }
        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            if (btnHelp.IsChecked.Value) tcEdit.SelectedIndex = 1;
            else tcEdit.SelectedIndex = 0;
            if (tbHelp.Text.Length == 0) tbHelp.Text = File.ReadAllText(Utils.configPath + "FlexDDS.hlp");
        }
    }
}
