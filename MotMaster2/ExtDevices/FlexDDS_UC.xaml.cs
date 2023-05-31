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
using Path = System.IO.Path;

namespace MOTMaster2.ExtDevices
{
    public class FlexDDS_HW : RS232Instrument
    {
        public FlexDDS_HW(string address) : base(address)
        {
            
        }
        public bool Connected { get { return connected; } }

        public bool Send2HW(string command, bool keepOpen = false)
        {
            return Write(command, keepOpen);
        }
    }
    /// <summary>
    /// Interaction logic for FlexDDS_UC.xaml
    /// </summary>
    public partial class FlexDDS_UC : UserControl, IExtDevice
    {

        private bool localDebug = false; 
        FlexDDS_HW flexDDS_HW;
        DDS_script script;
        List<GroupBox> SelectFactors;
        public FlexDDS_UC(string __dvcName, SolidColorBrush brush)
        {
            InitializeComponent();
            localDebug = Utils.TheosComputer() && (string)System.Environment.GetEnvironmentVariables()["COMPUTERNAME"] != "DESKTOP-U334RMA";
            _dvcName = __dvcName;
            grpBox.Header = dvcName;
            grpBox.BorderBrush = brush;
            //flexDDS_HW = new FlexDDS_HW("ASRL04::INSTR");
            ucExtFactors.groupUpdate = true;
            tiMain.Visibility = Visibility.Collapsed; tiEdit.Visibility = Visibility.Collapsed; tiTest.Visibility = Visibility.Collapsed;
            SelectFactors = new List<GroupBox>(); SelectFactors.Add(gbTrigger); SelectFactors.Add(gbBoolean);
        }
        protected string _dvcName;
        public string dvcName { get { return _dvcName; } }

        public string dvcPath { get { return Utils.configPath + dvcName + "\\"; } }
        public bool CheckEnabled(bool ignoreHardware = false) // ready to operate
        {
            bool bb = OptEnabled() && (ignoreHardware ? true : CheckHardware());
            bool bc = bb && (cbTemplates.SelectedIndex > -1);            
            ucExtFactors.btnUpdate.IsEnabled = bc; //btnEdit.IsEnabled = bc; btnTest.IsEnabled = bc; 
            return bc;
        }
        public GeneralOptions genOpt { get; set; }
        public bool OptEnabled()
        {
            if (Utils.isNull(genOpt)) return false;
            else return genOpt.ExtDvcEnabled["FlexDDS"];
        }
        public bool TestComm()
        {
            if (Utils.isNull(flexDDS_HW)) return false;
            if (localDebug) return true; // no actual hardware
            string scr = "\r\n";
            return flexDDS_HW.Send2HW(scr);
        }
        public bool RestartDDS()
        {
            string adr = flexDDS_HW.address;
            flexDDS_HW.Disconnect(); flexDDS_HW = null; GC.Collect(); Utils.Sleep(200);
            flexDDS_HW = new FlexDDS_HW(adr); Utils.Sleep(200);
            flexDDS_HW.Connect();
            if (!flexDDS_HW.Connected) return false;
            return TestComm();
        }
        protected bool lastCheckHardware = false;
        public bool CheckHardware()
        {
            if (localDebug) lastCheckHardware = true;
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
            /* if (!fctName.Equals("_block_")) // only block command is allowed
             {
                 ErrorMng.errorMsg("The device <" + dvcName + "> accepts only block commands!", 101); return false;
             }*/
            if (Convert.ToString(fctValue).Equals("_others_")) return true; // ? UpdateOthers ?        
            if (!OptEnabled())
            {
                ErrorMng.errorMsg("The device <" + dvcName + "> is not Enabled (options)!", 102); return false;
            }
            //if (Controller.config.Debug) return true;
            if (!CheckHardware())
            {
                ErrorMng.errorMsg("The device <" + dvcName + "> is not accesible!", 103); return false;
            }
            if (!CheckEnabled(true)) return false;
            
            // generate DDS script
            string scr = script.actualScriptAsString(script.replacements(ucExtFactors, OtherFactors()));
            if (scr.Equals(""))
            {
                ErrorMng.errorMsg("No script available!", 104); return false;
            }
            scr = scr.Replace("\r", "\r\n");
            ucExtFactors.UpdateValues();
            if (localDebug) ErrorMng.Log(scr);
            else            
                if (!flexDDS_HW.Send2HW(scr))
                {
                    ErrorMng.errorMsg("The device <" + dvcName + "> is not accesible!", 103); return false;
                }
            return true;
        }
        public void Init(ref Sequence _sequenceData, ref GeneralOptions _genOptions) // params, opts
        {
            // load config file            
            Dictionary<string, string> cfg = Utils.readDict(Path.Combine(dvcPath,"FlexDDS.CFG"));
            if (!cfg.ContainsKey("Address"))
            {
                ErrorMng.errorMsg("Address is missing from " + Utils.configPath + dvcName + ".CFG -> set to default #12", -436);
                flexDDS_HW = new FlexDDS_HW("ASRL12::INSTR"); 
            }
            else flexDDS_HW = new FlexDDS_HW(cfg["Address"]);
            flexDDS_HW.localDebug = localDebug;

            factorRow.Height = new GridLength(ucExtFactors.UpdateFactors());
            ucExtFactors.Init(); UpdateFromOptions(ref _genOptions);
            ucExtFactors.UpdateFromSequence(ref _sequenceData);
            ucExtFactors.OnSend2HW += new FactorsUC.Send2HWHandler(Talk2Dvc);
            ucExtFactors.OnCheckHw += new FactorsUC.CheckHwHandler(CheckHardware);

            if (!cfg.ContainsKey("LastScript"))
            {
                ErrorMng.errorMsg("LastScript is missing from " + dvcPath + ".CFG", -437); return;
            }
            //cbTemplates_DropDownOpened(null, null);
            readDDSfiles();
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
            if (cbTemplates.Items.Count > 0) cbTemplates.SelectedIndex = Utils.EnsureRange(j, 0, cbTemplates.Items.Count-1);
            loadSelected();
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
            Utils.writeDict(dvcPath + System.IO.Path.ChangeExtension(lastScript, ".ds0"), ucExtFactors.factorsState);
            if (!Utils.isNull(flexDDS_HW))
            {
                cfg["Address"] = flexDDS_HW.address;
                if (flexDDS_HW.Connected) flexDDS_HW.Disconnect();
            }
            Utils.writeDict(dvcPath + "FlexDDS.CFG", cfg);
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
            ucExtFactors.UpdateEnabled(genOpt.ExtDvcEnabled["FlexDDS"], CheckHardware(), CheckEnabled());
        }
        public bool UpdateOthers(bool ignoreMutable = false) // update all non-factors (others) ???
        {
            if (!ignoreMutable && !ucExtFactors.chkMutable.IsChecked.Value) return false;
            bool bb = true;
            return bb;
        }
        public string SequenceEvent(string EventName)
        {
            return "";
        }
        public bool UpdateDevice(bool ignoreMutable = false)
        {
            return ucExtFactors.UpdateDevice(ignoreMutable) && UpdateOthers(ignoreMutable);
        }
        private int lastSelectedIndex = -1;
        private void readDDSfiles()
        {
            string[] files = Directory.GetFiles(dvcPath, "*.dds");
            cbTemplates.Items.Clear();
            foreach (string file in files)
            {
                ComboBoxItem cbi = new ComboBoxItem(); cbi.Content = System.IO.Path.GetFileName(file);
                cbTemplates.Items.Add(cbi);
            }
        }
        private void cbTemplates_DropDownOpened(object sender, EventArgs e)
        {
            if (cbTemplates.SelectedIndex > -1) // save the previous template values
            {
                Utils.writeDict(System.IO.Path.ChangeExtension(script.filename, ".ds0"), ucExtFactors.factorsState);
                lastSelectedIndex = cbTemplates.SelectedIndex;
            }
            readDDSfiles();
        } 
        private void loadSelected()
        {
            if (cbTemplates.SelectedIndex.Equals(-1))
                if (cbTemplates.Items.Count > 0) cbTemplates.SelectedIndex = 0;
            string fn = dvcPath + ((ComboBoxItem)cbTemplates.SelectedItem).Content.ToString();
            if (!File.Exists(fn))
            {
                ErrorMng.errorMsg("No such file: "+fn, 122); return;
            }
            script = new DDS_script(fn);
            SetAllFactors(true);           
            CheckEnabled();
        }
        private void cbTemplates_DropDownClosed(object sender, EventArgs e)
        {
            if (cbTemplates.SelectedIndex.Equals(-1))
            {
                ErrorMng.errorMsg("No template selected.", 121);
                if (lastSelectedIndex > -1)
                {
                    ErrorMng.warningMsg("Trying to recover the last active one."); 
                    cbTemplates.SelectedIndex = lastSelectedIndex;
                }
                else return;
            }
            loadSelected();
        }      
        private void SetAllFactors(bool inclContent) // from script
        {              
            bool correct;
            List<string> fcts = script.ExtractFactors(false, out correct);
            SetFactors(fcts);
            List<string> efcts = script.ExtractFactors(true, out correct);
            SetSelectFactors(efcts);
            if (inclContent) 
                ucExtFactors.factorsState = Utils.readDict(System.IO.Path.ChangeExtension(script.filename, ".ds0"));
            ucExtFactors.UpdateFromSequence();
        }
        private void SetFactors(List<string> fcts) // list from script section
        {
            ucExtFactors.Factors.Clear();
            DCP_factors dcpFcts = script.factorsSection;
            var ffs = new Dictionary<string, bool>(); // script ones with flags
            foreach (string ss in fcts)
            {
                int j = dcpFcts.IdxFromName(ss);
                if (j == -1) 
                {                   
                    ErrorMng.errorMsg("Missing declaration of factor <"+ss+">", 143); continue;
                }                  
                if (dcpFcts[j].Item3.Equals("")) ucExtFactors.AddFactor(dcpFcts[j].Item2, dcpFcts[j].Item1);
            }
            factorRow.Height = new GridLength(ucExtFactors.UpdateFactors());
            ucExtFactors.Init();
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

            btnHelp.IsChecked = false; btnHelp_Click(sender, null);
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
        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            if (btnHelp.IsChecked.Value) tcEdit.SelectedIndex = 1;
            else tcEdit.SelectedIndex = 0;
            if (tbHelp.Text.Length == 0) tbHelp.Text = File.ReadAllText(Utils.configPath + "FlexDDS.hlp");
        }
        private void miCheckHw_Click(object sender, RoutedEventArgs e)
        {
           if (RestartDDS()) Utils.TimedMessageBox("It's fine");
            else Utils.TimedMessageBox("It's broken");
            //ucExtFactors.UpdateEnabled(genOpt.ExtDvcEnabled["FlexDDS"], CheckHardware(), CheckEnabled(false));
        }
        private void miTestHw_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.InitialDirectory = Utils.dataPath;
            dlg.FileName = ""; // Default file name
            dlg.DefaultExt = ".dcp"; // Default file extension
            dlg.Filter = "FlexDDS commands (.dcp)|*.dcp"; // Filter files by extension

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                string ss = File.ReadAllText(dlg.FileName);
                flexDDS_HW.Send2HW(ss); 
            }
        }
        private void miNewTemplate_Click(object sender, RoutedEventArgs e)
        {
            string fn = new InputBox("New template name", "", "").ShowDialog();
            if (fn.Equals("")) return;
            fn = Path.ChangeExtension(fn, ".dds");
            if (File.Exists(Path.Combine(dvcPath,fn))) { Utils.TimedMessageBox("Error: the template with that name already exists.", "", 3000); return; }
            string[] sa = { "[script]", "@Init()", "", "[factors]", "" };
            List<string> ls = new List<string>(sa);
            Utils.writeList(Path.Combine(dvcPath, fn), ls);
            ls.Clear(); ls.Add("# factors data"); string fn0 = Path.ChangeExtension(fn, ".ds0");
            Utils.writeList(Path.Combine(dvcPath, fn0), ls); Utils.Sleep(500);

            cbTemplates_DropDownOpened(null, null);
            int j = -1;
            for (int i = 0; i < cbTemplates.Items.Count; i++)
            {
                string ss = ((ComboBoxItem)cbTemplates.Items[i]).Content.ToString();
                if (ss.Equals(fn)) { j = i; break; }
            }
            if (j == -1)
            {
                ErrorMng.errorMsg(fn + " is missing.", -439); return;
            }
            else cbTemplates.SelectedIndex = j;
            loadSelected();
            btnEdit_Click(null, null);
        }
        private void imgTripleBars_MouseUp_1(object sender, MouseButtonEventArgs e)
        {
            ContextMenu cm = this.FindResource("cmTripleBars") as ContextMenu;
            cm.PlacementTarget = sender as Image;
            cm.IsOpen = true;
        }
    }
}

