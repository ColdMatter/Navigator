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
using System.Windows.Threading;
using NationalInstruments.Controls;
using Axel_hub;
using UtilsNS;

namespace MagnetoUtil
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        InOutClass InOut;
        DictFileLogger loggerS, loggerL;
        string device = "Dev7";
        string precision = "G8";
        public MainWindow()
        {
            InitializeComponent();           
            InOut = new InOutClass(Utils.TheosComputer());
            Title = "   Magneto Utility  v" + Utils.getAppFileVersion + (InOut.simulation ? "  (simulation)": "");           
            dTimer = new DispatcherTimer(); dTimer.Interval = new TimeSpan(0, 0, 2);
            dTimer.Tick += new EventHandler(dTimer_Tick);
        }
        DispatcherTimer dTimer;
        List<DataStack> lds;
        enum States { idle, repeat, scan, stabilazing, cancelRequest };
        States state;
        private void bbtnRepeat_Click(object sender, RoutedEventArgs e)
        {
            bbtnRepeat.Value = !bbtnRepeat.Value;
            if (!bbtnRepeat.Value) { state = States.cancelRequest; return; }
            if (!chkShortEnabled.IsChecked.Value && !chkLongEnabled.IsChecked.Value)
            {
                MessageBox.Show("No active logs"); return;
            }
            if (state != States.idle)
            {
                MessageBox.Show("Some other procedure is running -> "+state.ToString());  
                bbtnRepeat.Value = false;  return;
            }
            string timeName = Utils.dataPath + Utils.timeName();
            string[] cols = new string[8]; InOut.cols.CopyTo(cols, 0); cols[1] = "time[sec]";
            loggerS = new DictFileLogger(cols, "S", timeName);
            loggerS.Enabled = false;           
            loggerS.defaultExt = ".stl";
            loggerS.Enabled = chkShortEnabled.IsChecked.Value;

            cols[1] = "time[min]";
            loggerL = new DictFileLogger(cols, "L", timeName);
            loggerL.Enabled = false;
            loggerL.defaultExt = ".ltl";
            loggerL.Enabled = chkLongEnabled.IsChecked.Value;

            lds = new List<DataStack>();
            // short
            for (int i = 0; i<12; i++)
                lds.Add(new DataStack());
            graphShortRepeat.Data[0] = lds[0]; graphShortRepeat.Data[1] = lds[1]; graphShortRepeat.Data[2] = lds[2];
            graphShortRepeatStd.Data[0] = lds[3]; graphShortRepeatStd.Data[1] = lds[4]; graphShortRepeatStd.Data[2] = lds[5];

            graphLongRepeat.Data[0] = lds[6]; graphLongRepeat.Data[1] = lds[7]; graphLongRepeat.Data[2] = lds[8];
            graphLongRepeatStd.Data[0] = lds[9]; graphLongRepeatStd.Data[1] = lds[10]; graphLongRepeatStd.Data[2] = lds[11];

            state = States.repeat; InOut.longClear();
            InOut.Configure(new string[] { device + "/ai0", device + "/ai1", device + "/ai2" }, device + "/ao0");
            dTimer.Start();
        }
        baseMMscan scan;
        private void bbtnScan_Click(object sender, RoutedEventArgs e)
        {
            bbtnScan.Value = !bbtnScan.Value;
            if (!bbtnScan.Value) { state = States.cancelRequest; return; }
            if (state != States.idle)
            {
                MessageBox.Show("Some other procedure is running -> " + state.ToString());
                bbtnScan.Value = false; return;
            }
            scan = new baseMMscan();
            scan.sFrom = numFrom.Value; scan.sTo = numTo.Value; scan.sBy = numBy.Value;
            if ((scan.sFrom > scan.sTo) || (scan.sBy <= 0))
            {
                MessageBox.Show("Incorrect scan parameters"); return;
            }
            axisXscan.Range = new Range<double>(scan.sFrom, scan.sTo); axisXscanStd.Range = new Range<double>(scan.sFrom, scan.sTo);
            string timeName = Utils.dataPath + Utils.timeName();
            string[] cols = new string[8]; InOut.cols.CopyTo(cols, 0); cols[1] = "volts";
            loggerS = new DictFileLogger(cols, "N", timeName);
            loggerS.Enabled = false;
            loggerS.defaultExt = ".snl";
            loggerS.Enabled = chkShortEnabled.IsChecked.Value;

            lds = new List<DataStack>();
            // short
            for (int i = 0; i < 6; i++)
                lds.Add(new DataStack());
            graphScan.Data[0] = lds[0]; graphScan.Data[1] = lds[1]; graphScan.Data[2] = lds[2];
            graphScanStd.Data[0] = lds[3]; graphScanStd.Data[1] = lds[4]; graphScanStd.Data[2] = lds[5];

            state = States.scan;
            InOut.Configure(new string[] { device + "/ai0", device + "/ai1", device + "/ai2" }, device + "/ao0");
            dTimer.Start();
        }
        List<double[]> dts; DataStack dsCorr = new DataStack();
        private void dTimer_Tick(object sender, EventArgs e)
        {
            Dictionary<string, double> sts; double nextV;
            switch (state)
            {
                case States.cancelRequest:
                    dTimer.Stop(); bbtnRepeat.Value = false; bbtnScan.Value = false;
                    state = States.idle; return;
                case States.idle:   
                    throw new Exception("no active procedure mode");
                case States.repeat:
                    dts = InOut.acquire(); 
                    bool bs = !chkShortEnabled.IsChecked.Value; 
                    if (chkShortEnabled.IsChecked.Value)
                    {
                        sts = InOut.shortStats(dts);
                        bs = InOut.index > numShort.Value; 
                        if (!bs)
                        {
                            addPoint2graph(sts, graphShortRepeat);
                            addPoint2graph(sts, graphShortRepeatStd);
                            loggerS.dictLog(sts, precision);
                        }
                    }
                    bool bl = !chkLongEnabled.IsChecked.Value;
                    if (chkLongEnabled.IsChecked.Value)
                    {
                        sts = InOut.longStats(dts);                       
                        if (numLong.Value < 1) bl = false;
                        else bl = InOut.lidx > numLong.Value;
                        if (!Utils.isNull(sts) && !bl)
                        {
                            addPoint2graph(sts, graphLongRepeat);
                            addPoint2graph(sts, graphLongRepeatStd);
                            loggerL.dictLog(sts, precision);                           
                        }                            
                    }
                    if (bs && bl) state = States.cancelRequest;
                    break;
                case States.scan:
                    nextV = scan.sFrom + InOut.index * scan.sBy; 
                    if (nextV > scan.sTo)
                    {
                        state = States.cancelRequest; return;
                    }
                    InOut.setVoltage(nextV); 
                    dts = InOut.acquire();
                    sts = InOut.shortStats(dts);
                    sts["volts"] = nextV;
                    addPoint2graph(sts, graphScan);
                    addPoint2graph(sts, graphScanStd);
                    loggerS.dictLog(sts, precision);
                    break;
                case States.stabilazing:
                    dts = InOut.acquire();
                    sts = InOut.shortStats(dts);
                    addPoint2graph(sts, graphPIDsignal);
                    nextV = stabilUC.PID(sts["A_mean"],sts["C_mean"]);                   
                    dsCorr.AddPoint(nextV, sts["time[sec]"]); graphPIDcorrect.Data[0] = dsCorr;
                    InOut.setVoltage(nextV); sts["volts"] = nextV;
                    loggerS.dictLog(sts, precision);
                    break;
            }
        }
 
        private void bbtnStabil_Click(object sender, RoutedEventArgs e)
        {
            bbtnStabil.Value = !bbtnStabil.Value;
            if (!bbtnStabil.Value) { state = States.cancelRequest; return; }
            if (state != States.idle)
            {
                MessageBox.Show("Some other procedure is running -> " + state.ToString());
                bbtnScan.Value = false; return;
            }
            dsCorr.Clear();
            string timeName = Utils.dataPath + Utils.timeName();
            string[] cols = new string[8]; InOut.cols.CopyTo(cols, 0); cols[1] = "volts";
            loggerS = new DictFileLogger(cols, "B", timeName);
            loggerS.Enabled = false;
            loggerS.defaultExt = ".sbl";
            loggerS.Enabled = chkStabilLog.IsChecked.Value;

            lds = new List<DataStack>();
            // short
            for (int i = 0; i < 3; i++)
                lds.Add(new DataStack());
            graphPIDsignal.Data[0] = lds[0]; graphPIDsignal.Data[1] = lds[1]; graphPIDsignal.Data[2] = lds[2];

            state = States.stabilazing;
            InOut.Configure(new string[] { device + "/ai0", device + "/ai1", device + "/ai2" }, device + "/ao0");
            stabilUC.Init();
            dTimer.Start();
        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!state.Equals(States.idle)) Utils.TimedMessageBox("Do not start another procedure until the current <" + state.ToString() + "> is finished!", "Warning", 3600);
        }

        void addPoint2graph(Dictionary<string, double> sts, Graph graph)
        {
            double x = Double.NaN;
            if (sts.ContainsKey("time[sec]")) x = sts["time[sec]"];
            if (sts.ContainsKey("time[min]")) x = sts["time[min]"];
            if (sts.ContainsKey("volts")) x = sts["volts"];

            if (Double.IsNaN(x)) throw new Exception("no time column");            
            for (int i = 0; i< graph.Plots.Count; i++)
            {
                string ss = (string)graph.Plots[i].Label;
                if (sts.ContainsKey(ss))
                {
                    ((DataStack)graph.Data[i]).AddPoint(sts[ss], x); 
                }
            }
            graph.Refresh();
        }
    }
}
