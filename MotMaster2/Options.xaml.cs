using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UtilsNS;

namespace MOTMaster2
{
    public class Modes
    {
        public string Scan { get; set; }
        public List<string> MultiScan { get; set; }
 
        public void Save()
        {
            string fileJson = JsonConvert.SerializeObject(this);
            File.WriteAllText(Utils.configPath + "Defaults.cfg", fileJson);
        }
    }

    public class GeneralOptions
    {
        public enum SaveOption { save, ask, nosave }

        public SaveOption saveSequence;

        public enum DataLogOption { allData, average}

        public DataLogOption dataLog;
              
        public bool ForceSeqCharge { get; set; }
        public bool SaveSeqB4proc { get; set; }

        public enum AISaveOption { rawData, average, both}
        public AISaveOption aiSaveMode;

        public bool AxelHubLogger { get; set; }
        public bool MatematicaLogger { get; set; }
        public bool BriefData { get; set; }
        public bool ParamIncl { get; set; }
        public int DelayBwnShots { get; set; }

        public bool AIEnabled { get; set; }
        public int AISampleRate { get; set; }
        public int PostTrigSamples { get; set; }
        public double RiseTime { get; set; }
        public Dictionary<string,int> Skim { get; set; }
        
        public bool WindFreakEnabled { get; set; }
        public bool m2Enabled { get; set; }
        public bool FlexDDSEnabled { get; set; }

        public void Save()
        {
            string fileJson = JsonConvert.SerializeObject(this);
            File.WriteAllText(Utils.configPath + "genOptions.cfg", fileJson);
        }

        public bool SaveMultiScanLoop { get; set; }
    }

    /// <summary>
    /// Interaction logic for Options.xaml
    /// </summary>
    public partial class OptionWindow : Window
    {
        public OptionWindow()
        {
            InitializeComponent();
            
            string fileJson = JsonConvert.SerializeObject(DAQ.Environment.Environs.FileSystem);
            var HW = DAQ.Environment.Environs.Hardware;
            hardwareJson = JsonConvert.SerializeObject(HW, Formatting.Indented);
            LoadJsonToTreeView(hardwareTreeView, hardwareJson);
            LoadJsonToTreeView(filesystemTreeView, fileJson);
            // ExtDevices
            if (HW.ExtDevices.ContainsKey("MSquared")) chkM2Enabled.Visibility = Visibility.Visible;
            else chkM2Enabled.Visibility = Visibility.Collapsed;
            if (HW.ExtDevices.ContainsKey("WindFreak")) chkWindFreakEnabled.Visibility = Visibility.Visible;
            else chkWindFreakEnabled.Visibility = Visibility.Collapsed;
            if (HW.ExtDevices.ContainsKey("FlexDDS")) chkFlexDDSEnabled.Visibility = Visibility.Visible;
            else chkFlexDDSEnabled.Visibility = Visibility.Collapsed;          
        }       
        
        string hardwareJson = "";
        void LoadJsonToTreeView(TreeView treeView, string json)
        {
            var token = JToken.Parse(json);

            var children = new List<JToken>();
            if (token != null)
            {
                children.Add(token);
            }

            treeView.ItemsSource = null;
            treeView.Items.Clear();
            treeView.ItemsSource = children;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e) // visual to internal 
        {
            if (rbSaveSeqYes.IsChecked.Value) Controller.genOptions.saveSequence = GeneralOptions.SaveOption.save;
            if (rbSaveSeqAsk.IsChecked.Value) Controller.genOptions.saveSequence = GeneralOptions.SaveOption.ask;
            if (rbSaveSeqNo.IsChecked.Value) Controller.genOptions.saveSequence = GeneralOptions.SaveOption.nosave;

            Controller.genOptions.AxelHubLogger = chkAxelHubLogger.IsChecked.Value;
            Controller.genOptions.MatematicaLogger = chkMatematicaLogger.IsChecked.Value;
            Controller.genOptions.BriefData = chkBriefData.IsChecked.Value;
            Controller.genOptions.ParamIncl = chkParamIncl.IsChecked.Value;

            Controller.genOptions.ForceSeqCharge = chkForceSeqCharge.IsChecked.Value;
            Controller.genOptions.SaveSeqB4proc = chkSaveSeqB4proc.IsChecked.Value;
            Controller.genOptions.DelayBwnShots = numDelayBwnShots.Value;

            Controller.genOptions.AIEnabled = aiEnable.IsChecked.Value;
            // AI
            Controller.genOptions.AISampleRate = numSampleRate.Value;
            Controller.genOptions.PostTrigSamples = numPostTrig.Value;
            Controller.genOptions.RiseTime = 32; // Convert.ToDouble(tbRiseTime.Text);
            Controller.genOptions.Skim["PreSkim2"] = numPreSkim2.Value;
            Controller.genOptions.Skim["PostSkim2"] = numPostSkim2.Value;
            Controller.genOptions.Skim["PreSkimTot"] = numPreSkimTot.Value;
            Controller.genOptions.Skim["PostSkimTot"] = numPostSkimTot.Value;

            if (aiRaw.IsChecked.Value) Controller.genOptions.aiSaveMode = GeneralOptions.AISaveOption.rawData;
            if (aiAverage.IsChecked.Value) Controller.genOptions.aiSaveMode = GeneralOptions.AISaveOption.average;
            if (aiBoth.IsChecked.Value) Controller.genOptions.aiSaveMode = GeneralOptions.AISaveOption.both;
            //Controller.genOptions.qStart = cbQStart.Text;
            //Controller.genOptions.qTime = Convert.ToDouble(tbQTime.Text);

            if (aiEnable.IsChecked.Value) Controller.UpdateAIValues();

            Controller.genOptions.WindFreakEnabled = chkWindFreakEnabled.IsChecked.Value;
            Controller.genOptions.m2Enabled = chkM2Enabled.IsChecked.Value;
            Controller.genOptions.FlexDDSEnabled = chkFlexDDSEnabled.IsChecked.Value;
            Controller.genOptions.Save();
            Close();
        }

        private void tabCtrl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private bool CheckHardwareJson()
        {
            rtbModify.SelectAll();
            try
            {//json consistency
                JContainer.Parse(rtbModify.Selection.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + " (try again)");
                return false;
            }
            return true;
        }

        private void frmOptions_Loaded(object sender, RoutedEventArgs e) // internal to visual
        {
            Title = "Options  v" + Utils.getAppFileVersion;

            rbSaveSeqYes.IsChecked = Controller.genOptions.saveSequence.Equals(GeneralOptions.SaveOption.save);
            rbSaveSeqAsk.IsChecked = Controller.genOptions.saveSequence.Equals(GeneralOptions.SaveOption.ask);
            rbSaveSeqNo.IsChecked = Controller.genOptions.saveSequence.Equals(GeneralOptions.SaveOption.nosave);

            chkAxelHubLogger.IsChecked = Controller.genOptions.AxelHubLogger;
            chkMatematicaLogger.IsChecked = Controller.genOptions.MatematicaLogger;
            chkBriefData.IsChecked = Controller.genOptions.BriefData;
            chkParamIncl.IsChecked = Controller.genOptions.ParamIncl;
            numDelayBwnShots.Value = Controller.genOptions.DelayBwnShots;

            chkForceSeqCharge.IsChecked = Controller.genOptions.ForceSeqCharge; 
            chkSaveSeqB4proc.IsChecked = Controller.genOptions.SaveSeqB4proc; 
            aiEnable.IsChecked = Controller.genOptions.AIEnabled;

            switch (Controller.genOptions.aiSaveMode)
            {
                case GeneralOptions.AISaveOption.rawData:
                    aiRaw.IsChecked = true;
                    break;
                case GeneralOptions.AISaveOption.average:
                    aiAverage.IsChecked = true;
                    break;
                case GeneralOptions.AISaveOption.both:
                    aiBoth.IsChecked = true;
                    break;
                default:
                    break;
            }

            numSampleRate.Value = Controller.genOptions.AISampleRate;
            numPostTrig.Value = Controller.genOptions.PostTrigSamples;

            //Controller.genOptions.RiseTime = Convert.ToDouble(tbRiseTime.Text);
            if (Utils.isNull(Controller.genOptions.Skim)) Controller.genOptions.Skim = new Dictionary<string, int>();
            numPreSkim2.Value = Controller.genOptions.Skim.ContainsKey("PreSkim2") ? Controller.genOptions.Skim["PreSkim2"] : 0;
            numPostSkim2.Value = Controller.genOptions.Skim.ContainsKey("PostSkim2") ? Controller.genOptions.Skim["PostSkim2"] : 0;
            numPreSkimTot.Value = Controller.genOptions.Skim.ContainsKey("PreSkimTot") ? Controller.genOptions.Skim["PreSkimTot"] : 0;
            numPostSkimTot.Value = Controller.genOptions.Skim.ContainsKey("PostSkimTot") ? Controller.genOptions.Skim["PostSkimTot"]: 0;

            chkWindFreakEnabled.IsChecked = Controller.genOptions.WindFreakEnabled;
            chkM2Enabled.IsChecked = Controller.genOptions.m2Enabled;
            chkFlexDDSEnabled.IsChecked = Controller.genOptions.FlexDDSEnabled;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public sealed class MethodToValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var methodName = parameter as string;
            if (value == null || methodName == null)
                return null;
            var methodInfo = value.GetType().GetMethod(methodName, new Type[0]);
            if (methodInfo == null)
                return null;
            return methodInfo.Invoke(value, new object[0]);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException(GetType().Name + " can only be used for one way conversion.");
        }
    }
}

/* Timing 
 * MM2 send the start of inteferometer step as iTime in absolute time ticks and step duration as tTime in time ticks
 * 
 * AH transfers these into measr["time"] relative to startSeqSeries and measr["T"] both in sec   
 * 
 * 
 */