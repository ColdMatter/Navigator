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
        string device = "Dev1";
        string precision = "G8";
        public MainWindow()
        {
            InitializeComponent();
            if (Utils.TheosComputer()) tiStabilization.Visibility = Visibility.Visible;
            else tiStabilization.Visibility = Visibility.Collapsed;
            InOut = new InOutClass(true);
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
            loggerS = new DictFileLogger(InOut.cols, "S", timeName);
            loggerS.Enabled = false;           
            loggerS.defaultExt = ".stl";
            loggerS.Enabled = chkShortEnabled.IsChecked.Value;

            loggerL = new DictFileLogger(InOut.cols, "L", timeName);
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
            loggerS = new DictFileLogger(InOut.cols, "N", timeName);
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
        List<double[]> dts; 
        private void dTimer_Tick(object sender, EventArgs e)
        {
            Dictionary<string, double> sts;
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
                        bs = lds[0].Count > numShort.Value; 
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
                        else bl = lds[6].Count > numLong.Value;
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
                    double nextV = scan.sFrom + InOut.index * scan.sBy; 
                    if (nextV > scan.sTo)
                    {
                        state = States.cancelRequest; return;
                    }
                    InOut.setVoltage(nextV); 
                    dts = InOut.acquire();
                    sts = InOut.shortStats(dts);
                    sts["time"] = nextV;
                    addPoint2graph(sts, graphScan);
                    addPoint2graph(sts, graphScanStd);
                    loggerS.dictLog(sts, precision);

                    break;
            }
        }

        void addPoint2graph(Dictionary<string, double> sts, Graph graph)
        {
            if (!sts.ContainsKey("time")) throw new Exception("no time column");            
            for (int i = 0; i< graph.Plots.Count; i++)
            {
                string ss = (string)graph.Plots[i].Label;
                if (sts.ContainsKey(ss))
                {
                    ((DataStack)graph.Data[i]).AddPoint(sts[ss], sts["time"]); 
                }
            }
            graph.Refresh();
        }
    }
}
