using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NationalInstruments.DAQmx;
using NationalInstruments.Analysis.Math;

using Axel_hub;
using UtilsNS;

namespace MagnetoUtil
{
    public class InOutClass
    {
        public bool simulation = false;
        private NationalInstruments.DAQmx.Task AITask, AOTask;
        private AnalogMultiChannelReader reader; AnalogSingleChannelWriter writer;
        public string[] cols = new string[] { "index", "time", "A_mean", "B_mean", "C_mean", "A_std", "B_std", "C_std"};
        
        public InOutClass(bool _simulation = false) 
        {
            simulation = _simulation;
        }
        public int chnCount { get; private set; }
        private DateTime startTime; public int index; 
        const double minRange = -10; const double maxRange = 10;
        const int sampleRate = 1000; const int samplesPerChannel = 1000;
        public bool Configure(string[] inChns, string outChn)
        {
            chnCount = inChns.Length; index = 0; startTime = DateTime.Now;
            bool rslt = true; if (simulation) return true;
            string[] chnIn = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AI, PhysicalChannelAccess.External);
            string[] chnOut = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AO, PhysicalChannelAccess.External);
            // IN
            try 
            { 
                AITask = new NationalInstruments.DAQmx.Task();
                foreach (string chn in inChns)
                {
                    AITask.AIChannels.CreateVoltageChannel(chn, "",(AITerminalConfiguration)(-1), minRange, maxRange, AIVoltageUnits.Volts);
                }
                // Configure timing specs    
                AITask.Timing.ConfigureSampleClock("", sampleRate, SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples, samplesPerChannel);
                // Verify the task
                AITask.Control(TaskAction.Verify);           
                // Read the data
                reader = new AnalogMultiChannelReader(AITask.Stream);           
            }
            catch (DaqException exception)
            {
                MessageBox.Show("IN> "+exception.Message); rslt = false;   
            }
            // OUT
            try 
            { 
                AOTask = new NationalInstruments.DAQmx.Task();
                AOTask.AOChannels.CreateVoltageChannel(outChn, "aoChannel", minRange, maxRange, AOVoltageUnits.Volts);
                writer = new AnalogSingleChannelWriter(AOTask.Stream);
            }
            catch (DaqException exception)
            {
                MessageBox.Show("OUT> "+exception.Message); rslt = false;
            }

            return rslt;
        }
        public List<double[]> acquire()
        {
            List<double[]> dt = new List<double[]>(); index++; 
            if (simulation)
            {
                for (int i = 0; i < chnCount; i++)
                {
                    dt.Add(new double[samplesPerChannel]);
                    for (int j = 0; j < samplesPerChannel; j++)
                        dt[dt.Count - 1][j] = Utils.NextGaussian(i, 0.2);
                }
                return dt;
            }
            try
            {
                double[,] AIData = reader.ReadMultiSample(samplesPerChannel);
                if (chnCount != AIData.GetLength(0)) throw new Exception("Wrong number of channels");
                int smpCount = AIData.GetLength(1);             
                for (int i = 0; i<chnCount; i++)
                {
                    dt.Add(new double[smpCount]);
                    for (int j = 0; j < smpCount; j++) 
                        dt[dt.Count-1][j] = AIData[i, j];
                }
            }
            catch (DaqException ex)
            {
                MessageBox.Show("IN> " + ex.Message);
            }           
            return dt;
        }
        public Dictionary<string,double> shortStats(List<double[]> dt)
        {
            Dictionary<string, double> sts = new Dictionary<string, double>();
            sts["index"] = index; sts["time"] = DateTime.Now.Subtract(startTime).TotalSeconds;
            for (int i = 0; i<dt.Count; i++)
            {
                sts[(char)(65 + i) + "_mean"] = dt[i].Average();
                sts[(char)(65 + i) + "_std"] = Statistics.StandardDeviation(dt[i]);
            }                 
            return sts;
        }
        private List<DataStack> dts;
        public void longClear()
        {
            dts = new List<DataStack>();
            for(int i = 0; i<3; i++)
            {
                dts.Add(new DataStack(30 * samplesPerChannel));
            }
        }
        public Dictionary<string, double> longStats(List<double[]> dt)
        {
            Dictionary<string, double> sts = new Dictionary<string, double>();
            sts["index"] = Convert.ToInt32(index / 30); sts["time"] = DateTime.Now.Subtract(startTime).TotalMinutes;
            for (int i = 0; i < dt.Count; i++)
            {
                for (int j = 0; j < dt[i].Length; j++)                   
                    dts[i].AddPoint(dt[i][j]);
            }
            if (dts[0].Count > 29) 
            {  
                for (int i = 0; i < dt.Count; i++)
                {
                    sts[(char)(65 + i) + "_mean"] = dts[i].pointYs().Average();
                    sts[(char)(65 + i) + "_std"] = Statistics.StandardDeviation(dts[i].pointYs());
                }
                longClear(); 
                return sts;
            }
            else return null;            
        }

        public void setVoltage(double volt)
        {
            if (simulation) return;
            try
            {
                writer.WriteSingleSample(true, volt); Thread.Sleep(10);              
            }
            catch (DaqException ex)
            {
                MessageBox.Show("OUT> "+ex.Message);
            }
        }
    }
}
