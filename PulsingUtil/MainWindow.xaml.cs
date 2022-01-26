using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NationalInstruments.DAQmx;
using UtilsNS;


namespace PulsingUtil
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        NationalInstruments.DAQmx.Task myTask = null;
        AnalogSingleChannelWriter writer = null;
        public MainWindow()
        {
            InitializeComponent();
            Title = "   Pulsing Utility  v" + Utils.getAppFileVersion;
            string[] chnArray = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AO, PhysicalChannelAccess.External);
            foreach (string chn in chnArray)
            {
                int i = cbPhysicalChannel.Items.Add(new ComboBoxItem());
                ((ComboBoxItem)cbPhysicalChannel.Items[i]).Content = chn;
            }           
            if (cbPhysicalChannel.Items.Count > 0)
                cbPhysicalChannel.SelectedIndex = 0;
        }
        private void SetVoltage(double volt)
        {
            try
            {
                if (Utils.isNull(myTask))
                {
                    myTask = new NationalInstruments.DAQmx.Task();
                    myTask.AOChannels.CreateVoltageChannel(cbPhysicalChannel.Text, "aoChannel", -10, 10, AOVoltageUnits.Volts);
                    writer = new AnalogSingleChannelWriter(myTask.Stream);
                }
                writer.WriteSingleSample(true, volt);
            }
            catch (DaqException ex)
            {
                MessageBox.Show(ex.Message);
            }           
        }

        public void Pulse(double volt, double duration) // [V], [ms]
        {
            SetVoltage(0);
            SetVoltage(volt); Thread.Sleep(Convert.ToInt32(duration));
            SetVoltage(0);
        }
        private void bbSetVoltage_Click(object sender, RoutedEventArgs e)
        {
            SetVoltage(numVoltage.Value);
        }

        private void bbSinglePulse_Click(object sender, RoutedEventArgs e)
        {
            Pulse(numVoltage.Value, numDuration.Value);
        }

        private void bbGenerator_Click(object sender, RoutedEventArgs e)
        {
            bbGenerator.Value = !bbGenerator.Value;
            if (!bbGenerator.Value) return;
            if (numDuration.Value > numPeriod.Value) throw new Exception("Error: period is less than duration!");
            while (bbGenerator.Value)
            {
                Pulse(numVoltage.Value, numDuration.Value); Thread.Sleep(Convert.ToInt32(numPeriod.Value - numDuration.Value));
                Utils.DoEvents();
            }
        }

        private void cbPhysicalChannel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            myTask = null; writer = null;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bbGenerator.Value = false; Thread.Sleep(Convert.ToInt32(numPeriod.Value)); Utils.DoEvents();
        }
    }
}
