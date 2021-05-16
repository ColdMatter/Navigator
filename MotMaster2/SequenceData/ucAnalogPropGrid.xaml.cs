using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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

namespace MOTMaster2.SequenceData
{
    /// <summary>
    /// Templates for WpfProperttGrid
    /// </summary>
    /// 
    public class GetAVItem
    {
        public string Value = "";
        public GetAVItem(List<AnalogArgItem> avList, string Name)
        {
            foreach (AnalogArgItem avi in avList)
            {
                if (avi.Name.Equals(Name)) Value = avi.Value;
            }
        }
    }

    public interface pgArgOut
    {
        List<AnalogArgItem> ArgOut();
    }
    public class pgContinue : pgArgOut
    {
        public pgContinue()
        {

        }
        public List<AnalogArgItem> ArgOut()
        {
            return new List<AnalogArgItem>();
        }

    }
    public class pgSingleValue : pgArgOut
    {         
        [DisplayName("1. Start Time")]
        [Description("Delay from the beginning of the step.\n<number> or <parameter>")]
        public string StartTime { get; set; }

        [DisplayName("2. Value")]
        [Description("Value to be set in volts.\n<number> or <parameter>")]
        public string Value { get; set; }
        

        public pgSingleValue()
        {

        }
        public pgSingleValue(List<AnalogArgItem> avList)
         {
             StartTime = Utils.Convert2StringDef((new GetAVItem(avList, "Start Time")).Value);
             Value = (new GetAVItem(avList, "Value")).Value;            
        }

        public List<AnalogArgItem> ArgOut()
        {           
            return new List<AnalogArgItem> { new AnalogArgItem("Start Time", StartTime), new AnalogArgItem("Value", Value)};
        }
    }
    public class pgLinearRamp : pgArgOut
    {
        [DisplayName("1. Start Time")]
        [Description("Delay from the beginning of the step.\n<number> or <parameter>")]
        public string StartTime { get; set; }

        [DisplayName("2. Duration")]
        [Description("Duration of the ramp.\n<number> or <parameter>")]
        public string Duration { get; set; }

        [DisplayName("3. Final Value")]
        [Description("Final Value to be set in volts.\n<number> or <parameter>")]
        public string FinalValue { get; set; }
        public pgLinearRamp()
        {

        }
        public pgLinearRamp(List<AnalogArgItem> avList)
        {
            StartTime = Utils.Convert2StringDef((new GetAVItem(avList, "Start Time")).Value);
            Duration = Utils.Convert2StringDef((new GetAVItem(avList, "Duration")).Value, "1");
            FinalValue = Utils.Convert2StringDef((new GetAVItem(avList, "Final Value")).Value, "1");
        }
        public List<AnalogArgItem> ArgOut()
        {
            return new List<AnalogArgItem> { new AnalogArgItem("Start Time", StartTime), new AnalogArgItem("Duration", Duration), new AnalogArgItem("Final Value", FinalValue) };
        }

    }
    public class pgPulse : pgArgOut
    {
        [DisplayName("1. Start Time")]
        [Description("Delay from the beginning of the step.\n<number> or <parameter>")]
        public string StartTime { get; set; }

        [DisplayName("2. Duration")]
        [Description("Duration of the pulse.\n<number> or <parameter>")]
        public string Duration { get; set; }

        [DisplayName("3. Value")]
        [Description("Value to be set in volts.\n<number> or <parameter>")]
        public string Value { get; set; }

        [DisplayName("4. Final Value")]
        [Description("Final Value to be set in volts.\n<number> or <parameter>")]
        public string FinalValue { get; set; }
        public pgPulse()
        {

        }
        public pgPulse(List<AnalogArgItem> avList)
        {
            StartTime = Utils.Convert2StringDef((new GetAVItem(avList, "Start Time")).Value);
            Duration = Utils.Convert2StringDef((new GetAVItem(avList, "Duration")).Value, "1");
            Value = Utils.Convert2StringDef((new GetAVItem(avList, "Value")).Value, "1");
            FinalValue = Utils.Convert2StringDef((new GetAVItem(avList, "Final Value")).Value, "1"); 
        }
        public List<AnalogArgItem> ArgOut()
        {
            return new List<AnalogArgItem> { new AnalogArgItem("Start Time", StartTime), new AnalogArgItem("Duration", Duration),  new AnalogArgItem("Value", Value), new AnalogArgItem("Final Value", FinalValue) };
        }

    }
    public class pgFunction : pgArgOut
    {
        [DisplayName("1. Start Time")]
        [Description("Delay from the beginning of the step.\n<number> or <parameter>")]
        public string StartTime { get; set; }

        [DisplayName("2. Duration")]
        [Description("Duration of the pulse.\n<number> or <parameter>")]
        public string Duration { get; set; }

        [DisplayName("3. Function")]
        [Description("Function as expression.")]
        public string Function { get; set; }

        public pgFunction()
        {

        }
        public pgFunction(List<AnalogArgItem> avList)
        {
            StartTime = Utils.Convert2StringDef((new GetAVItem(avList, "Start Time")).Value);
            Duration = Utils.Convert2StringDef((new GetAVItem(avList, "Duration")).Value, "1");
            Function = (new GetAVItem(avList, "Function")).Value;
        }
        public List<AnalogArgItem> ArgOut()
        {
            return new List<AnalogArgItem> { new AnalogArgItem("Start Time", StartTime), new AnalogArgItem("Duration", Duration), new AnalogArgItem("Function", Function) };
        }
    }
    
    public class pgXYPairs : pgArgOut
    {
        public string InterpolationType;
        public DataTable dataTable;
        public pgXYPairs()
        {

        }
        public pgXYPairs(List<AnalogArgItem> avList)
        {            
            InterpolationType = Utils.Convert2StringDef((new GetAVItem(avList, "Interpolation Type")).Value, "Piecewise Linear");

            dataTable = new DataTable();
            DataColumn dc1 = new DataColumn("X-Values", typeof(double));
            DataColumn dc2 = new DataColumn("Y-Values", typeof(string));

            dataTable.Columns.Add(dc1);
            dataTable.Columns.Add(dc2);

            string Xvals = (new GetAVItem(avList, "X Values")).Value; string[] xa = Xvals.Split(',');
            string Yvals = (new GetAVItem(avList, "Y Values")).Value; string[] ya = Yvals.Split(',');
            if (Xvals.Equals("") || Yvals.Equals("")) return;
            if (xa.Length != ya.Length) throw new Exception("Uneven X/Y values lengths");
            for (int i = 0; i<xa.Length; i++)
            {
                string ss = ya[i]; double analogRawValue;
                if ((Double.TryParse(ss, out analogRawValue) || Controller.sequenceData.Parameters.ContainsKey(ss))) dataTable.Rows.Add(Convert.ToDouble(xa[i]), ss);
                else throw new Exception("Wrong Y value -> "+ss);                
            }
        }
        public List<AnalogArgItem> ArgOut()
        {
            string Xvals = ""; string Yvals = "";
            foreach (DataRow row in dataTable.Rows)
            {
                Xvals += row[0].ToString() + ","; Yvals += row[1].ToString() + ",";
            }
            while (Xvals[Xvals.Length - 1].Equals(',')) Xvals = Xvals.Remove(Xvals.Length - 1);
            while (Yvals[Yvals.Length - 1].Equals(',')) Yvals = Yvals.Remove(Yvals.Length - 1);
            return new List<AnalogArgItem> {  new AnalogArgItem("X Values", Xvals), new AnalogArgItem("Y Values", Yvals), new AnalogArgItem("Interpolation Type", InterpolationType) };
        }
    }

    /// <summary>
    /// Interaction logic for ucAnalogPropGrid.xaml
    /// </summary>
    public partial class ucAnalogPropGrid : UserControl
    {       
        public ucAnalogPropGrid()
        {
            InitializeComponent();
            propGrid.PropertySort = PropertySort.Alphabetical;
            propGrid.ToolbarVisible = false;
            propGrid.SelectionTypeLabel.Visibility = Visibility.Collapsed;
            propGrid.HelpVisible = true;
        }
        AnalogChannelSelector analogTypeSelector;
        private pgXYPairs XYPairs;
        public void feedData(SequenceStep selectedStep, string channelName, AnalogChannelSelector analogType)
        {
            lbTitle.Content = string.Format("{0}: {1} with {2}", selectedStep.Name, channelName, analogType.ToString());
            
            List<AnalogArgItem> data = selectedStep.GetAnalogData(channelName, analogType);
            analogTypeSelector = analogType; 
            switch (analogType)
            {
                case AnalogChannelSelector.Continue:
                    propGrid.SelectedObject = new pgContinue();
                    break;
                case AnalogChannelSelector.SingleValue:
                    propGrid.SelectedObject = new pgSingleValue(data);
                    break;
                case AnalogChannelSelector.LinearRamp:
                    propGrid.SelectedObject = new pgLinearRamp(data);
                    break;
                case AnalogChannelSelector.Pulse:
                    propGrid.SelectedObject = new pgPulse(data);
                    break;
                case AnalogChannelSelector.Function:
                    propGrid.SelectedObject = new pgFunction(data);
                    break;
                case AnalogChannelSelector.XYPairs:
                    XYPairs = new pgXYPairs(data);
                    switch (XYPairs.InterpolationType)
                    {
                        case "Piecewise Linear":
                            cbInterpolationType.SelectedIndex = 0;
                            break;
                        case "Step":
                            cbInterpolationType.SelectedIndex = 1;
                            break;
                        default:
                            cbInterpolationType.SelectedIndex = 0;
                            break;
                    }                   
                    dataGrid.ItemsSource = XYPairs.dataTable.DefaultView;
                    break;                
            }
            tcAnalogType.SelectedIndex = (analogType == AnalogChannelSelector.XYPairs) ? 1 : 0;
        }
        public List<AnalogArgItem> getData()
        {
            if (Utils.isNull(propGrid.SelectedObject) && analogTypeSelector != AnalogChannelSelector.XYPairs) return null;
            switch (analogTypeSelector)
            {
                case AnalogChannelSelector.Continue:
                    return ((pgContinue)propGrid.SelectedObject).ArgOut();
                case AnalogChannelSelector.SingleValue:
                    return ((pgSingleValue)propGrid.SelectedObject).ArgOut();
                case AnalogChannelSelector.LinearRamp:
                    return ((pgLinearRamp)propGrid.SelectedObject).ArgOut();
                case AnalogChannelSelector.Pulse:
                    return ((pgPulse)propGrid.SelectedObject).ArgOut();
                case AnalogChannelSelector.Function:
                    return ((pgFunction)propGrid.SelectedObject).ArgOut();
                case AnalogChannelSelector.XYPairs:
                    if (!Utils.isNull(XYPairs)) return XYPairs.ArgOut();
                    else return new List<AnalogArgItem>(); 
                default:
                    return new List<AnalogArgItem>();
            }
        }

        private void cbInterpolationType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!Utils.isNull(XYPairs))
            {
                int j = cbInterpolationType.SelectedIndex;
                if (j > -1) XYPairs.InterpolationType = ((ComboBoxItem)cbInterpolationType.Items[j]).Content.ToString();
            }    
        }
    }
}
/* err:-1) Failed to build sequence:Error building sequence: Failed to add analog data for Channel:ybias3DCoil Step:BSwitch1 IN MOTMaster2 */