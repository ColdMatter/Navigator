using DAQ;
using DAQ.Analog;
using DAQ.Environment;
using DAQ.HAL;
using NationalInstruments.DAQmx;
using Microsoft.CSharp;
using MOTMaster2.SequenceData;
using Newtonsoft.Json;
using System;
//using IMAQ;

using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Threading;
using System.Windows;
//using DataStructures;
using System.Runtime.Serialization.Formatters.Binary;
using UtilsNS;
using ErrorManager;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace MOTMaster2
{
    /// <summary>
    /// Here's MOTMaster's controller.
    /// 
    /// Gets a MOTMasterScript (a script contaning a series of commands like "addEdge" for both digital and analog)
    /// from user (either remotely or via UI), compiles it, builds a pattern and sends it
    /// to hardware.
    /// </summary>
    public class Controller : MarshalByRefObject
    {

        #region Class members

        private static string
            motMasterPath = (string)Environs.FileSystem.Paths["MOTMasterEXEPath"] + "MOTMaster2.exe";
        private static string
            daqPath = (string)Environs.FileSystem.Paths["daqDLLPath"];
        private static string
            scriptListPath = (string)Environs.FileSystem.Paths["scriptListPath"];
        private static string
            motMasterDataPath = (string)Environs.FileSystem.Paths["DataPath"];
        private static string
            saveToDirectory = (string)Environs.FileSystem.Paths["MOTMasterDataPath"];
        private static string
            cameraAttributesPath = (string)Environs.FileSystem.Paths["CameraAttributesPath"];
        private static string
            hardwareClassPath = (string)Environs.FileSystem.Paths["HardwareClassPath"];

        private static string defaultScriptPath = scriptListPath + "\\defaultScript.sm2";

        private static string tempScriptPath = scriptListPath + "\\tempScript.sm2";

        private static string digitalPGBoard = (string)Environs.Hardware.Boards["multiDAQ"];

        public static MMConfig config = (MMConfig)Environs.Hardware.GetInfo("MotMasterConfiguration");

        private Thread runThread;
        private static Exception runThreadException = null;

        public enum RunningState { stopped, running };
        private static RunningState _runningStatus = RunningState.stopped;
        public static RunningState status
        {
            get { return _runningStatus; }
            set
            {
                if ((value != _runningStatus) && !Utils.isNull(Application.Current))
                 Application.Current.Dispatcher.BeginInvoke(
                  DispatcherPriority.Background,
                  new Action(() =>
                  {
                      RunStatusEvent(value == RunningState.running);
                  }));
                _runningStatus = value;
            }
        }

        //public List<string> analogChannels;
        public List<string> digitalChannels;
        public static MOTMasterScript script;
        public static GeneralOptions genOptions;
        public static Sequence sequenceData;
        public MOTMasterSequence sequence;
        public static ExperimentData ExpData { get; set; }
        public static FileLogger dataLogger;
        public static FileLogger paramLogger;

        private static NationalInstruments.DAQmx.Task clockTask;
        private static DigitalSingleChannelReader myDigitalReader;
        public static double acquireTime { get; private set; } 

        public static bool SendDataRemotely { get; set; }
        private bool _AutoLogging;
        public bool AutoLogging
        {
            get { return _AutoLogging; }
            set
            {
                if (value) StartLogging();
                else StopLogging();
                _AutoLogging = value;
            }
        }

        public static event DataEventHandler MotMasterDataEvent;
        public delegate void DataEventHandler(object sender, DataEventArgs d);

        private static DAQMxPatternGenerator pg;
        private static HSDIOPatternGenerator hs;
        private static DAQMxPatternGenerator PCIpg;
        private static DAQMxAnalogPatternGenerator apg;
        private static MMAIWrapper aip;

        private static bool _StaticSequence;
        public static bool StaticSequence { get { return genOptions.ForceSeqCharge ? false : _StaticSequence; } set { _StaticSequence = value; } }
        private bool hardwareError = false;
        private static CameraControllable camera = null;
       // private static TranslationStageControllable tstage = null;
        private static ExperimentReportable experimentReporter = null;

        public static WindfreakSynth microSynth, microSynth2;
        //public string ExperimentRunTag { get; set; }
        public static MMscan ScanParam { get; set; }
        
        public static int numInterations { get; set; }
        private static MuquansController muquans = null;
        public static ICEBlocDCS M2DCS;
        public static ICEBlocPLL M2PLL;
        public PhaseStrobes phaseStrobes;
        public static Dictionary<string, object> DCSParams;
        MMDataIOHelper ioHelper;
        static SequenceBuilder builder;

        DataStructures.SequenceData ciceroSequence;
        DataStructures.SettingsData ciceroSettings;

        public delegate void b4AcquireHandler(long ticks, out double sTime);
        public static event b4AcquireHandler Onb4Acquire;

        protected static void b4AcquireEvent(long ticks, out double sTime)
        {
            sTime = -1;
            if (Onb4Acquire != null) Onb4Acquire(ticks, out sTime);
        }

        public delegate void RunStatusHandler(bool running);
        public static event RunStatusHandler OnRunStatus;

        protected static void RunStatusEvent(bool running)
        {
            if (OnRunStatus != null) OnRunStatus(running);
        }

        public delegate void ChnChangeHandler(int chn);
        public static event ChnChangeHandler OnChnChange;

        protected static void ChnChangeEvent(int chn)
        {
            if (OnChnChange != null) OnChnChange(chn);
        }

        #endregion

        #region Initialisation

        // without this method, any remote connections to this object will time out after
        // five minutes of inactivity.
        // It just overrides the lifetime lease system completely.
        public override Object InitializeLifetimeService()
        {
            return null;
        }
        public void StartApplication()
        {
            LoadEnvironment();
        
            LoadDefaultSequence();

            //TODO Analog input config should be moved to GeneralOptions
            if (ExpData == null) { ExpData = new ExperimentData(); Controller.UpdateAIValues(); }

            CheckHardware(config.Debug);

            phaseStrobes = new PhaseStrobes();
            ioHelper = new MMDataIOHelper(motMasterDataPath,
                    (string)Environs.Hardware.GetInfo("Element"));
        }
        //TODO Set config flags based on if hardware exists
        private void CheckHardware(bool debug)
        {
            if (!config.HSDIOCard) pg = new DAQMxPatternGenerator((string)Environs.Hardware.Boards["digital"]);
            else hs = new HSDIOPatternGenerator((string)Environs.Hardware.Boards["hsDigital"]);
            apg = new DAQMxAnalogPatternGenerator();
            PCIpg = new DAQMxPatternGenerator((string)Environs.Hardware.Boards["multiDAQPCI"]);
            aip = new MMAIWrapper((string)Environs.Hardware.Boards["analogIn"]);

            
            digitalChannels = Environs.Hardware.DigitalOutputChannels.Keys.Cast<string>().ToList();

            if (config.CameraUsed) camera = (CameraControllable)Activator.GetObject(typeof(CameraControllable),
                "tcp://localhost:1172/controller.rem");

           // if (config.TranslationStageUsed) tstage = (TranslationStageControllable)Activator.GetObject(typeof(CameraControllable),
           //     "tcp://localhost:1172/controller.rem");

            if (config.ReporterUsed) experimentReporter = (ExperimentReportable)Activator.GetObject(typeof(ExperimentReportable),
                "tcp://localhost:1172/controller.rem");

            if (config.UseMuquans) { muquans = new MuquansController(); }
            if (!config.Debug) 
            { 
                microSynth = (WindfreakSynth)Environs.Hardware.Instruments["microwaveSynth"];
                microSynth2 = (WindfreakSynth)Environs.Hardware.Instruments["microwaveSynth2"];
            }
            if (config.UseMSquared)
            {
                CheckMSquaredHardware();
            }
        }

        private void CheckMSquaredHardware()
        {
            //if (genOptions.m2Comm == GeneralOptions.M2CommOption.off) return;
            if (Environs.Hardware.Instruments.ContainsKey("MSquaredDCS")) M2DCS = (ICEBlocDCS)Environs.Hardware.Instruments["MSquaredDCS"];
            else throw new Exception("Cannot find DCS ICE-BLOC");
            if (Environs.Hardware.Instruments.ContainsKey("MSquaredPLL")) M2PLL = (ICEBlocPLL)Environs.Hardware.Instruments["MSquaredPLL"];
            else throw new Exception("Cannot find PLL ICE-BLOC");

            return;
            
            try
            {
                
                if (!config.Debug)
                {
                    M2DCS.Connect();
                    M2PLL.Connect();

                    M2PLL.StartLink();
                    M2DCS.StartLink();
                    //SetMSquaredParameters();
                }
            }
            catch
            {
                //Set to popup to avoid Exception called when it can't write to a Log
                if (genOptions.ExtDvcEnabled["MSquared"])
                    ErrorMng.warningMsg("Could not set MSquared Parameters", -1, true);
            }
        }

        #endregion

        #region Hardware control methods

        private void run(MOTMasterSequence sequence)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            try
            {
                if (config.UseMuquans)
                {
                    muquans.StartOutput(); Console.WriteLine("Started muquans at {0}ms", watch.ElapsedMilliseconds);
                }

                apg.OutputPatternAndWait(sequence.AnalogPattern.Pattern);
                Console.WriteLine("Started apg at {0}ms", watch.ElapsedMilliseconds);
                if (Controller.genOptions.AIEnabled) aip.StartTask();
                if (!config.HSDIOCard) pg.OutputPattern(sequence.DigitalPattern.Pattern, true);
                else
                {
                    int[] loopTimes = ((DAQ.Pattern.HSDIOPatternBuilder)sequence.DigitalPattern).LoopTimes;
                    hs.OutputPattern(sequence.DigitalPattern.Pattern, loopTimes);
                    Console.WriteLine("Started hs at {0}ms", watch.ElapsedMilliseconds);
                }
            }
            catch
            {
                releaseHardware();
                runThreadException = new Exception("Failed to start output patterns. Releasing hardware");
                Console.WriteLine("Failed to start output patterns. Releasing hardware");
            }
        }
        private void ContinueLoop()
        {          
            //Just need to restart the cards

            apg.StartPattern();
            if (Controller.genOptions.AIEnabled) aip.StartTask();
            if (config.HSDIOCard)
            {
                hs.StartPattern();
            }
            else
            {
                throw new NotImplementedException("DAQmx digital cards not currently supported");
            }
            if (Controller.genOptions.AIEnabled) aip.ReadAnalogDataFromBuffer();
        }

        private static void initializeHardware(MOTMasterSequence sequence)
        {
            if (!config.HSDIOCard) pg.Configure(config.DigitalPatternClockFrequency, StaticSequence, true, true, sequence.DigitalPattern.Pattern.Length, true, false);
            else hs.Configure(config.DigitalPatternClockFrequency, StaticSequence, true, false);
            if (config.UseMuquans) { muquans.Configure(StaticSequence); }
            apg.Configure(sequence.AnalogPattern, config.AnalogPatternClockFrequency, StaticSequence);
            // Create the task.
            clockTask = new NationalInstruments.DAQmx.Task();
            DigitalOutputChannel aqcTr = ((DigitalOutputChannel)Environs.Hardware.DigitalOutputChannels["acquisitionTrigger"]); // e.g. Dev3/port0/line23 
            string triggerLoc = aqcTr.Device + "/port0/line" + aqcTr.line.ToString();
            clockTask.Dispose();

            if (Controller.genOptions.AIEnabled)
            {
                sequence.AIConfiguration.DynamicLength = sequenceData.DynamicLength();
                aip.Configure(sequence.AIConfiguration, StaticSequence);
                aip.AnalogDataReceived += OnAnalogDataReceived;
                acquireTime = Double.NaN;

                try
                {
                  /*  // Create the task.
                    clockTask = new NationalInstruments.DAQmx.Task();
                    DigitalOutputChannel aqcTr = ((DigitalOutputChannel)Environs.Hardware.DigitalOutputChannels["acquisitionTrigger"]); // e.g. Dev3/port0/line23 
                    string triggerLoc = aqcTr.Device + "/port0/line" + aqcTr.line.ToString();
                    clockTask.Dispose();
                    // Create channel
                           clockTask.DOChannels.CreateChannel(triggerLoc, "", ChannelLineGrouping.OneChannelForEachLine);
                           // Configure digital change detection timing
                           clockTask.Timing.ConfigureChangeDetection(triggerLoc, "", SampleQuantityMode.ContinuousSamples, 1000);

                           // Add the digital change detection event handler
                           // Use SynchronizeCallbacks to specify that the object 
                           // marshals callbacks across threads appropriately.
                           clockTask.SynchronizeCallbacks = true;

                           clockTask.DigitalChangeDetection += new DigitalChangeDetectionEventHandler(clockTask_DigitalChangeDetection);

                           // Create the reader
                           myDigitalReader = new DigitalSingleChannelReader(clockTask.Stream);

                           // Start the task
                           clockTask.Start();*/
                }
                catch (DaqException exception)
                {
                    clockTask.Dispose();
                    MessageBox.Show(exception.Message);
                }
            }
        }

        private static void clockTask_DigitalChangeDetection(object sender, DigitalChangeDetectionEventArgs e)
        {
            try
            {
                bool triggerLine = myDigitalReader.ReadSingleSampleSingleLine();
                double aTime = -1;
                //b4AcquireEvent(DateTime.Now.Ticks, out aTime); 
                acquireTime = (aTime > 0) ? aTime : Double.NaN;
            }
            catch (DaqException ex)
            {
                clockTask.Dispose();

                MessageBox.Show(ex.Message);
            }
        }
        private void releaseHardware()
        {
            try
            {
                //if (StaticSequence) pauseHardware();
            if (!config.HSDIOCard) pg.StopPattern();
            else hs.StopPattern();
            apg.StopPattern();
            if (Controller.genOptions.AIEnabled) { aip.StopPattern(); }
                if (config.UseMuquans) { muquans.StopOutput(); }//microSynth.Disconnect(); }
        }
            catch (Exception e)
            {
                ErrorMng.warningMsg("Error when releasing hardware: " + e.Message, -3, false);
            }
        }

        private void pauseHardware()
        {
            apg.PauseLoop();
            if (Controller.genOptions.AIEnabled) aip.PauseLoop();
            if (config.HSDIOCard) hs.PauseLoop();
            else throw new NotImplementedException("DAQmx digital cards not currently supported");
        }
        //private void releaseHardwareLoop()
        //{
        //    if (!config.HSDIOCard) pg.StopPattern();
        //    else hs.AbortRunning();
        //    apg.AbortRunning();
        //    if (Controller.genOptions.AIEnable) aip.AbortRunning();
        //    if (config.UseMuquans) muquans.StopOutput();
        //}
        private void clearDigitalPattern(MOTMasterSequence sequence)
        {
            sequence.DigitalPattern.Clear(); //No clearing required for analog (I think).
        }
        private void releaseHardwareAndClearDigitalPattern(MOTMasterSequence sequence)
        {
            clearDigitalPattern(sequence);
            releaseHardware();
        }
        private void ClearPatterns()
        {
            if (Utils.isNull(sequence)) return;
            if (Utils.isNull(sequence.AnalogPattern)) return;
            if (Utils.isNull(sequence.DigitalPattern)) return;
            sequence.AnalogPattern.Clear();
            sequence.DigitalPattern.Clear();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        //private static long[] hwInterferometerInterval = new long[2];

        public static Tuple<long, long> InterferometerInterval() // from..to [ticks] relative to beginning of seq
        {
            Tuple<long, long> rslt = new Tuple<long, long>(-1, -1);
            if (ExpData.startSeqTime < 0) return rslt; // no reference point
            //if (Utils.isNull(ExpData.InterferometerStepName)) return rslt;
            //if (ExpData.InterferometerStepName.Equals("")) return rslt;
            long curr = ExpData.startSeqTime; long first = -1; long second = -1;
            foreach (SequenceStep step in sequenceData.Steps)
            {               
                if (step.Description.Contains("Interferometer"))
                {
                    if (first < 0)
                    {
                        first = curr; // first occurence
                        second = first;
                    }
                    double d = step.evalDuration(true); long l = Utils.sec2tick(d);
                    second += l; 
                }
                if (step.Enabled) curr += Utils.sec2tick(step.evalDuration(true));
            }
            //if (hwInterferometerInterval[0] > -1) first = hwInterferometerInterval[0];
            //if (hwInterferometerInterval[1] > -1) second = hwInterferometerInterval[1];
            rslt = new Tuple<long, long>(first, second);
            //hwInterferometerInterval[0] = -1; hwInterferometerInterval[1] = -1;
            return rslt;
        }
        protected static void OnAnalogDataReceived(object sender, EventArgs e)
        {
            var rawData = config.Debug ? ExpData.GenerateFakeData() : aip.GetAnalogData();
            MMexec[] finalData = ConvertDataXYToAxelHub(rawData);
            ExperimentData.lastData = Controller.genOptions.AIEnabled ? finalData[0].prms : null;
            if (!Controller.genOptions.AIEnabled) return;           
            if (ExpData.grpMME.cmd.Equals("repeat") && SendDataRemotely)
            {
                if (Convert.ToInt32(ExpData.grpMME.prms["cycles"]) == (Convert.ToInt32(finalData[0].prms["runID"]) + 1))
                {
                    finalData[0].prms["last"] = 1;
                }
            }
            if (ExpData.grpMME.cmd.Equals("scan") && SendDataRemotely)
            {
                MMscan mms = new MMscan();
                mms.FromDictionary(ExpData.grpMME.prms);
                int k = (int)((mms.sTo - mms.sFrom) / mms.sBy);
                if (k == (Convert.ToInt32(finalData[0].prms["runID"])))
                {
                    finalData[0].prms["last"] = 1;
                }
            }
            foreach (MMexec mme in finalData)
            {
                if (SendDataRemotely && (ExpData.startSeqTime > 0))
                {
                    var tm = InterferometerInterval(); Tuple<int, int> ei;
                    if (ExpData.AnalogSegments.ContainsKey("ExtraInterferometer"))
                    {
                        ei = ExpData.AnalogSegments["ExtraInterferometer"]; // pre (before) and post (after) skimming in numPnt; if not they are there then 0
                        mme.prms["bTime"] = ei.Item1.ToString(); mme.prms["aTime"] = ei.Item2.ToString();
                        ExpData.AnalogSegments.Remove("ExtraInterferometer");
                    }
                    mme.prms["iTime"] = tm.Item1.ToString(); mme.prms["tTime"] = (tm.Item2 - tm.Item1).ToString(); // start in ticks; length in ticks
                    mme.prms["samplingRate"] = genOptions.AISampleRate.ToString(); 
                }
                    
                if (!Utils.isNull(ScanParam)) 
                   if (ScanParam.randomized) mme.prms["scan.prm"] = ScanParam.Value;
                string dataJson = JsonConvert.SerializeObject(mme, Formatting.Indented);
                if (!Utils.isNull(dataLogger)) dataLogger.log("{\"MMExec\":" + dataJson + "},");
                if (SendDataRemotely)
                {
                    if (MotMasterDataEvent != null) MotMasterDataEvent(sender, new DataEventArgs(dataJson));
                }
                dataJson = null;
            }
            finalData = null;
           // if (Controller.genOptions.AIEnable && !config.Debug) aip.ClearBuffer();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        internal void StopRunning(bool force = false)
        {
            if (!config.Debug)
            {
                WaitForRunToFinish();
                while (IsRunning() && !StaticSequence)
                {
                    WaitForRunToFinish();
                    if (!hardwareError) releaseHardware();
                }
                try { if (force || StaticSequence) releaseHardware(); }
                catch { }
                if (config.UseMuquans)muquans.DisposeAll();               
            }
            StaticSequence = false; //Set this here in case we want to scan after
            status = RunningState.stopped;
            if (force) ClearPatterns();
        }

        internal bool CheckForRunErrors()
        {
            if (runThreadException == null) return false;
            else
            {
                status = RunningState.stopped;
                Exception ex = runThreadException;
                runThreadException = null;
                hardwareError = true;
                throw ex;
            }
        }
        #endregion

        #region RUN RUN RUN (public & remotable stuff)

        /// <summary>
        /// This is the guts of MOTMaster.
        /// 
        /// - MOTMaster initializes the hardware, faffs a little to prepare the patterns in the 
        /// builders (e.g. calls "BuildPattern"), and sends the pattern to Hardware.
        /// 
        /// -Note that the analog stuff needs a trigger to start!!!! Make sure one of your digital lines is reserved 
        /// for triggering the analog pattern.
        /// 
        /// - Once the experiment is finished, MM releases the hardware.
        /// 
        /// - MOTMaster also saves the data to a .zip. This includes: the original MOTMasterScript (.cs), a text file
        /// with the parameters in it (IF DIFFERENT FROM THE VALUES IN .cs, THE PARAMETERS IN THE TEXT FILE ARE THE
        /// CORRECT VALUES!), another text file with the camera attributes, yet another file (entitled hardware report)
        ///  which contains the values set by the Hardware controller at the start of the run, and a .png file(s) containing the final image(s).
        /// 
        /// -There are 2 ways of using "Run". Run(null) uses the parameters given in the script (.cs file).
        ///  Run(Dictionary<>) compiles the .cs file but then replaces values in the dictionary. This is to allow
        ///  the user to inject values after compilation but before sending to hardware. By doing this,
        ///  the user can scan parameters using a python script, for example.
        ///  If you call Run(), MOTMaster immediately checks to see if you're running a fresh script 
        ///  or whether you're re-running an old one. In the former case Run(null) is called. In the latter,
        ///  MOTMaster will fetch the dictionary used in the old experiment and use it as the
        ///  argument for Run(Dictionary<>).        ///  
        /// 

        /// </summary>

        private bool saveEnable = true;


        public void SaveToggle(System.Boolean value)
        {
            //saveEnable = value;
            //controllerWindow.SetSaveCheckBox(value);
        }

        private static void axisControl(int chn, bool xy)
        {
            int chn0 = chn;
            if (sequenceData.Parameters.ContainsKey("swapAxes"))
                if (Convert.ToDouble(sequenceData.Parameters["swapAxes"].Value) > 0.5) 
                {
                    if (chn == 0) chn0 = 1;
                    else chn0 = 0;
                }
            M2DCS.axisControl(chn0,xy);
            if (xy) ChnChangeEvent(chn0);
            else ChnChangeEvent(2); 
            
         /*   double PLLFreq = (double)sequenceData.Parameters["PLLFreq"].Value;
            double ChirpRate = (double)sequenceData.Parameters["ChirpRate"].Value;
            double ChirpDuration = (double)sequenceData.Parameters["ChirpDuration"].Value;
         
            if (xy) 
            {
                switch (chn) 
                {              
                    case 0: 
                        if (sequenceData.Parameters.ContainsKey("PLLFreqX")) PLLFreq = (double)sequenceData.Parameters["PLLFreqX"].Value;
                        if (sequenceData.Parameters.ContainsKey("ChirpRateX")) ChirpRate = (double)sequenceData.Parameters["ChirpRateX"].Value;               
                        break;
                    case 1:
                        if (sequenceData.Parameters.ContainsKey("PLLFreqY")) PLLFreq = (double)sequenceData.Parameters["PLLFreqY"].Value;
                        if (sequenceData.Parameters.ContainsKey("ChirpRateY")) ChirpRate = (double)sequenceData.Parameters["ChirpRateY"].Value;
                        break;
                }
            }
            
            M2PLL.configure_PLL_profile(PLLFreq * 1e6, ChirpRate * 1e6, ChirpDuration);*/
        }

        private static int _BatchNumber;
        public static int BatchNumber 
        { 
            get { return _BatchNumber; } 
            set 
            { 
                _BatchNumber = value;
                if (sequenceData.Parameters.ContainsKey("runID")) sequenceData.Parameters["runID"].Value = (double)value;
                if (sequenceData.Parameters.ContainsKey("aChn"))
                {
                    if (sequenceData.Parameters.ContainsKey("swapAxes"))
                    {
                        sequenceData.Parameters["aChn"].Value = (double)actChannel(value, Convert.ToDouble(sequenceData.Parameters["swapAxes"].Value) > 0.5);
                    }
                    else
                    {
                        if (actChannel(value) == 0) sequenceData.Parameters["aChn"].Value = 1.0;
                        else sequenceData.Parameters["aChn"].Value = 0.0;
                    }
                }

                if (!config.Debug && config.UseMSquared && genOptions.ExtDvcEnabled["MSquared"])
                {
                    if (Math.Abs(ExpData.axis).Equals(2)) axisControl(actChannel(value), true);
                    else
                    {
                        if (value.Equals(0)) axisControl(ExpData.axis, false); // at start only
                    }
                }
            } 
        }

        public void IncrementBatchNumber()
        {
            BatchNumber++;
        }
        
        private string scriptPath = "";
        public void SetScriptPath(String path)
        {
            scriptPath = path;
            //controllerWindow.WriteToScriptPath(path);
        }
        /*
        private bool replicaRun = false;
        public void SetReplicaRunBool(System.Boolean value)
        {
            replicaRun = value;
        }
        private string dictionaryPath = "";
        public void SetDictionaryPath(String path)
        {
            dictionaryPath = path;
        }
        */
        public bool IsRunning()
        {
            return status == RunningState.running;
            /*
            if (status == RunningState.running && !config.Debug)
            {
                Console.WriteLine("Thread Running");
                return true;
            }
            else
                return false;
             * */
        }
        public void RunStart(Dictionary<string, object> paramDict, int myBatchNumber = 0)
        {
            //runThread = new Thread(delegate()
            //{
            //    try
            //    {
            //        this.Run(paramDict);
            //    }
            //    catch (ThreadAbortException) { }
            //    catch (Exception e)
            //    {
            //        status = RunningState.stopped;
            //        throw e;
                    
            //    }

            //});
            runThread = new Thread(new ParameterizedThreadStart(this.Run));
            runThread.Name = "MOTMaster Controller";
            runThread.Priority = ThreadPriority.Highest;
            status = RunningState.running;

            runThread.Start(paramDict);
            //Console.WriteLine("Thread Starting");
        }
        public void WaitForRunToFinish()
        {
            if (runThread != null) { runThread.Join(); }
            if (IsRunning()) hardwareError = CheckForRunErrors();
            // Console.WriteLine("Thread Waiting");
        }
        /*
        public void Run()
        {
            status = RunningState.running;
            Run(replicaRun ? ioHelper.LoadDictionary(dictionaryPath) : null);
        }

        public void Run(Dictionary<String, Object> dict)
        {
            Run(dict, batchNumber);
        }
        */
        public void Run(object dict)
        {
            Run((Dictionary<string, object>)dict);
        }
        
        public void Run(Dictionary<String, Object> dict)
        {
            Stopwatch watch = new Stopwatch();

            //sequence = BuildMMSequence(dict);
            if (sequence == null)
            {
                //Exception has been thrown. Will be passed when CheckForRunErrors is called.
                status = RunningState.stopped;
                return;
            }
            if (BatchNumber == 0)
            {
                if (StaticSequence) hardwareError = !InitialiseHardwareAndPattern(sequence);
                InitialiseData(this);
            }
           // if (hardwareError && !config.Debug) ErrorMng.errorMsg(runThreadException.Message, -5);
            PrepareNonDAQHardware();

            if (!StaticSequence)
            {
                hardwareError = InitialiseHardwareAndPattern(sequence);
            }

            if (config.CameraUsed) waitUntilCameraIsReadyForAcquisition();
            Utils.Trace("shot");
            watch.Start();
            ExpData.startSeqTime = DateTime.Now.Ticks; // move to 277 or 299 ??
            //TODO Try WaitForRunToFinish here and nowhere else

            if (!config.Debug)
            {
                if (BatchNumber == 0 || !StaticSequence) runPattern(sequence);
                else if (status == RunningState.running) ContinueLoop();
                else return;
            }

            watch.Stop();

            if (saveEnable)
            {
                AcquireDataFromHardware();
            }

            if (config.CameraUsed) finishCameraControl();
           // if (config.TranslationStageUsed) disarmAndReturnTranslationStage();
            //if (config.UseMuquans && !config.Debug) microSynth.ChannelA.RFOn = false;
            if (Controller.genOptions.AIEnabled || config.Debug) OnAnalogDataReceived(this, new DataEventArgs(BatchNumber));
            if (StaticSequence && !config.Debug) pauseHardware();

            status = RunningState.stopped;
            //Dereferences the MMScan object
            //ScanParam = null;
        }

        private static bool InitialiseHardwareAndPattern(MOTMasterSequence sequence)
        {
            if (config.UseMMScripts) buildPattern(sequence, (int)script.Parameters["PatternLength"]);
            else buildPattern(sequence, (int)builder.Parameters["PatternLength"]);
            try
            {
                if (!config.Debug) initializeHardware(sequence);

            }
            catch (Exception e)
            {
                ErrorMng.errorMsg("Could not initialise hardware:" + e.Message, -2, true);
                return false;
            }
            return true;
        }

        public void BuildMMSequence(Dictionary<String, Object> dict, string scriptPath = null)
        {           
            if (config.UseMMScripts || sequenceData == null)
            {
                script = prepareScript(scriptPath, dict);
                sequence = getSequenceFromScript(script);
            }
            else
            {
                if (Controller.genOptions.AIEnabled || config.Debug)
                {
                    if (!sequenceData.Steps.Any(t=>t.GetDigitalData("acquisitionTrigger")))
                    {
                        Controller.genOptions.AIEnabled = false;
                        ErrorMng.warningMsg("acquisitionTrigger is not enabled. Setting AIEnable to false."); return;
                    }
                    else
                    {
                        CreateAcquisitionTimeSegments();
                    }
                }
                if (!StaticSequence || BatchNumber == 0) 
                    sequence = getSequenceFromSequenceData(dict);
                //aip.UpdateSamplesCount(sequence.AIConfiguration); // new?
                if (sequence == null) { throw runThreadException; }
            }
        }
            
        /// <summary>
        /// Prepares the hardware that is not controlled using DAQmx voltage patterns. Typically, these are experiment specific.
        /// </summary>
        private static void PrepareNonDAQHardware()
        {
                    if (config.CameraUsed) prepareCameraControl();

                  //  if (config.TranslationStageUsed) armTranslationStageForTimedMotion(script);

                    if (config.CameraUsed) GrabImage((int)script.Parameters["NumberOfFrames"]);
                   
        }
        /// <summary>
        /// Initialises the objects used to store data from the run !
        /// </summary>
        private static void InitialiseData(object sender)
        {
            MMexec mme = InitialCommand(ScanParam);
            string initJson = JsonConvert.SerializeObject(mme, Formatting.Indented);
            if (!Utils.isNull(paramLogger))
            paramLogger.log("{\"MMExec\":" + initJson + "},");
            var jm = ExpData.jumboMode();
            if (jm == ExperimentData.JumboModes.repeat)
            {

            }
            if (SendDataRemotely && ((jm == ExperimentData.JumboModes.none))) 
            {
                MotMasterDataEvent(sender, new DataEventArgs(initJson));
                ExpData.grpMME = mme.Clone();
            }
        }

        [Obsolete("This method encapsulates the old-style data acquisition and will be removed in the future", false)]
        private void AcquireDataFromHardware()
        {
            if (config.CameraUsed)
            {

                waitUntilCameraAquisitionIsDone();

                try
                {
                    checkDataArrived();
                }
                catch (DataNotArrivedFromHardwareControllerException)
                {
                    ErrorMng.warningMsg("No Data Arrived from Hardware Controller", -10, true);
                }

                Dictionary<String, Object> report = new Dictionary<string, object>();
                if (config.ReporterUsed)
                {
                    report = GetExperimentReport();
                    //TODO Change save method
                                
                }
                save(script, scriptPath, imageData, report, BatchNumber);
            }
            else
            {
                Dictionary<String, Object> report = new Dictionary<string, object>();
                if (config.ReporterUsed)
                {
                    report = GetExperimentReport();
                               
                }
                if (config.UseMMScripts)
                    save(builder, motMasterDataPath, report, ExpData.ExperimentName, BatchNumber);
            }
        }

        #endregion

        #region private stuff

        private void updateSaveDirectory(string newDirectory)
        {
            saveToDirectory = newDirectory;
            if (!Directory.Exists(newDirectory))
            {
                Directory.CreateDirectory(saveToDirectory);
            }
        }

        //TODO Change the way everything is saved
        private void save(MOTMasterScript script, string pathToPattern, byte[,] imageData, Dictionary<String, Object> report, double[,] aiData, int batchNumber)
        {
            ioHelper.StoreRun(motMasterDataPath, batchNumber, pathToPattern, hardwareClassPath,
                script.Parameters, report, cameraAttributesPath, imageData, config.ExternalFilePattern);
        }
        private void save(MOTMasterScript script, string pathToPattern, byte[][,] imageData, Dictionary<String, Object> report, double[,] aiData, int batchNumber)
        {
            ioHelper.StoreRun(motMasterDataPath, batchNumber, pathToPattern, hardwareClassPath,
                script.Parameters, report, cameraAttributesPath, imageData, config.ExternalFilePattern);
        }
        private void save(MOTMasterScript script, string pathToPattern, Dictionary<String, Object> report, int batchNumber)
        {
            ioHelper.StoreRun(motMasterDataPath, batchNumber, pathToPattern, hardwareClassPath,
                script.Parameters, report, config.ExternalFilePattern);
        }
        private void save(MOTMasterScript script, string pathToPattern, byte[][,] imageData, Dictionary<String, Object> report, int batchNumber)
        {
            ioHelper.StoreRun(motMasterDataPath, batchNumber, pathToPattern, hardwareClassPath,
                script.Parameters, report, cameraAttributesPath, imageData, config.ExternalFilePattern);
        }
        private void save(SequenceBuilder builder, string saveDirectory, Dictionary<string, object> report, string element, int batchNumber)
        {
            ioHelper.StoreRun(builder, saveDirectory, report, element, batchNumber);
        }

        
        private void runPattern(MOTMasterSequence sequence)
        {
            run(sequence);
            if (Controller.genOptions.AIEnabled) 
                aip.ReadAnalogDataFromBuffer();
            if (!StaticSequence) { releaseHardware(); status = RunningState.stopped; }
            //else pauseHardware();
        }

        private void debugRun(MOTMasterSequence sequence)
        {
            int[] loopTimes = ((DAQ.Pattern.HSDIOPatternBuilder)sequence.DigitalPattern).LoopTimes;
            hs.BuildScriptForDebug(sequence.DigitalPattern.Pattern, loopTimes);
        }
        public static MOTMasterScript prepareScript(string pathToPattern, Dictionary<String, Object> dict)
        {
            MOTMasterScript script;
            CompilerResults results = compileFromFile(pathToPattern);
            if (results != null)
            {

                script = loadScriptFromDLL(results);
                if (dict != null)
                {
                    script.EditDictionary(dict);

                }
                return script;

            }
            return null;
        }

        private static void buildPattern(MOTMasterSequence sequence, int patternLength)
        {
            sequence.DigitalPattern.BuildPattern(patternLength);
            sequence.AnalogPattern.BuildPattern();
            if (config.UseMuquans) muquans.BuildCommands(sequence.MuquansPattern.commands);
        }

        #endregion

        #region Compiler & Loading DLLs

        /// <summary>
        ///   /// - Once the user has selected a particular implementation of MOTMasterScript, 
        /// MOTMaster will compile it. Note: the dll is currently stored in a temp folder somewhere. 
        /// Its pathToPattern can be found in the CompilerResults.PathToAssembly). 
        /// This newly formed dll contain methods named GetDigitalPattern and GetAnalogPattern. 
        /// 
        /// - These are called by the script's "GetSequence". GetSequence always returns a 
        /// "MOTMasterSequence", which comprises a PatternBuilder32 and an AnalogPatternBuilder.
        /// </summary>

        private static CompilerResults compileFromFile(string scriptPath)
        {
            CompilerParameters options = new CompilerParameters();

            options.ReferencedAssemblies.Add(motMasterPath);
            options.ReferencedAssemblies.Add(daqPath);
            options.ReferencedAssemblies.Add("System.Core.dll");

            TempFileCollection tempFiles = new TempFileCollection();
            tempFiles.KeepFiles = true;
            CompilerResults results = new CompilerResults(tempFiles);
            options.GenerateExecutable = false;                         //Creates .dll instead of .exe.
            CodeDomProvider codeProvider = new CSharpCodeProvider();
            options.TempFiles = tempFiles;
            //options.GenerateInMemory = false; // may not be necessary...haven't tried this in a while
            //options.OutputAssembly = string.Format(@"{0}\{1}", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "mydll.dll");
            try
            {
                results = codeProvider.CompileAssemblyFromFile(options, scriptPath);
                if (results.Errors.Count > 0)
                {
                    MessageBox.Show("Error in MOTMaster Script Compilation");
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return null;
            }
            //controllerWindow.WriteToScriptPath(results.PathToAssembly);
            return results;
        }

        private static MOTMasterScript loadScriptFromDLL(CompilerResults results)
        {
            object loadedInstance = new object();
            try
            {
                Assembly patternAssembly = Assembly.LoadFrom(results.PathToAssembly);
                foreach (Type type in patternAssembly.GetTypes())
                {
                    if (type.IsClass == true)
                    {
                        loadedInstance = Activator.CreateInstance(type);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + e.InnerException.Message, "Error in loading script DLL");
                return null;
            }

            return (MOTMasterScript)loadedInstance;
        }

        private static MOTMasterSequence getSequenceFromScript(MOTMasterScript script)
        {
            MOTMasterSequence sequence = script.GetSequence(config.HSDIOCard, config.UseMuquans);

            return sequence;
        }

        private static MOTMasterSequence getSequenceFromSequenceData(Dictionary<string, object> paramDict)
        {
            
            builder = new SequenceBuilder(sequenceData);
            if (paramDict != null) builder.EditDictionary(paramDict);
            try { builder.BuildSequence(); }
            catch (Exception e)
            {
                runThreadException = new Exception("Error building sequence: \n" + e.Message);
             return null;
            }
            MOTMasterSequence sequence = builder.GetSequence(config.HSDIOCard, config.UseMuquans);
            return sequence;
        }
        public void BuildMOTMasterSequence(ObservableCollection<SequenceStep> steps)
        {
            builder = new SequenceBuilder(sequenceData);

            builder.BuildSequence();


            sequence = builder.GetSequence(config.HSDIOCard, config.UseMuquans);

            if (sequenceData == null)
            {
                sequenceData = new Sequence();
                sequenceData.Steps = steps;
                sequenceData.CreateParameterList(script.Parameters);
            }
            if (config.Debug) MessageBox.Show("Successfully Built Sequence.");
        }
        #endregion

        #region CameraControl

        /// <summary>
        /// - Camera control is run through the hardware controller. All MOTMaster knows 
        /// about it a function called "GrabImage(string cameraSettings)". If the camera attributes are 
        /// set so that it needs a trigger, MOTMaster will have to deliver that too.
        /// It'll expect a byte[,] or byte[][,] (if there are several images) as a return value.
        /// 
        /// -At the moment MOTMaster won't run without a camera nor with 
        /// more than one. In the long term, we might 
        /// want to fix this.
        /// </summary>
        /// 
        static int nof;
        public static void GrabImage(int numberOfFrames)
        {
            nof = numberOfFrames;
            Thread LLEThread = new Thread(new ThreadStart(grabImage));
            LLEThread.Start();

        }

        static bool imagesRecieved = false;
        /*private byte[,] imageData;
        private void grabImage()
        {
            imagesRecieved = false;
            imageData = (byte[,])camera.GrabSingleImage(cameraAttributesPath);
            imagesRecieved = true;
        }*/
        private static byte[][,] imageData;
        private static void grabImage()
        {
            imagesRecieved = false;
            imageData = camera.GrabMultipleImages(cameraAttributesPath, nof);
            imagesRecieved = true;
        }
        public class DataNotArrivedFromHardwareControllerException : Exception { };
        private bool waitUntilCameraAquisitionIsDone()
        {
            while (!imagesRecieved)
            { Thread.Sleep(10); }
            return true;
        }
        private bool waitUntilCameraIsReadyForAcquisition()
        {
            try
            {
            while (!camera.IsReadyForAcquisition())
            { Thread.Sleep(10); }
            }
            catch (System.Net.Sockets.SocketException e)
            {
                MessageBox.Show("CameraControllable not found. \n Is there a hardware controller running? \n \n" + e.Message, "Remoting Error");
            }
            return true;
        }
        private static void prepareCameraControl()
        {
            camera.PrepareRemoteCameraControl();
        }
        private void finishCameraControl()
        {
            try
            {
            camera.FinishRemoteCameraControl();
        }
            catch (System.Net.Sockets.SocketException e)
            {
                MessageBox.Show("CameraControllable not found. \n Is there a hardware controller running? \n \n" + e.Message, "Remoting Error");
            }
        }
        private void checkDataArrived()
        {
            if (imageData == null)
            {
                MessageBox.Show("No data. Something's Wrong.");
                throw new DataNotArrivedFromHardwareControllerException();
            }
        }
        #endregion

        #region Getting an Experiment Report
        /// <summary>
        /// This is the mechanism for saving experimental parameters which MM doesn't control, but that the hardware controller can monitor
        /// (e.g. oven temperature, vacuum chamber pressure etc).
        /// </summary>

        public Dictionary<String, Object> GetExperimentReport()
        {
            return experimentReporter.GetExperimentReport();
        }

        #endregion

        #region Translation stage
    /*    private static void armTranslationStageForTimedMotion(MOTMasterScript script)
        {
            tstage.TSConnect();
            Thread.Sleep(50);
            tstage.TSInitialize((double)script.Parameters["TSAcceleration"], (double)script.Parameters["TSDeceleration"],
                (double)script.Parameters["TSDistance"], (double)script.Parameters["TSVelocity"]);
            Thread.Sleep(50);
            tstage.TSOn();
            Thread.Sleep(50);
            tstage.TSAutoTriggerDisable();
            Thread.Sleep(50);
            tstage.TSGo();
        }
        private void disarmAndReturnTranslationStage()
        {
            tstage.TSAutoTriggerEnable();
            Thread.Sleep(50);
            tstage.TSReturn(); // This is the hard coded return of the translation stage at the end of running a MM script
            Thread.Sleep(50);
            tstage.TSDisconnect();
        }*/
        #endregion

        #region Re-Running a script (intended for reloading old scripts)

        /// <summary>
        /// This section is meant to be for the situation when you want to re-run exactly the same pattern
        /// you ran sometime in the past.
        /// armReplicaRun prompts you for a zip file which contains the run you want to replicate. It unzipps the
        /// file into a folder of the same name, picks out the dictionary and the script.
        /// These then get loaded in the usual way through Run().
        /// disposeReplicaRun does some clean up after the experiment is finished.
        /// </summary>
        /*
        public void RunReplica()
        {
            armReplicaRun();
            Run();
            disposeReplicaRun();
        }

        private void armReplicaRun()
        {
            string zipPath = ioHelper.SelectSavedScriptPathDialog();
            string outputFolderPath = Path.GetDirectoryName(zipPath) + "\\" +
                Path.GetFileNameWithoutExtension(zipPath) + "\\";

            ioHelper.UnzipFolder(zipPath);
            SetScriptPath(outputFolderPath +
                Path.GetFileNameWithoutExtension(zipPath) + ".cs");

            SetDictionaryPath(outputFolderPath +
                Path.GetFileNameWithoutExtension(zipPath) + "_parameters.txt");

            SetReplicaRunBool(true);

        }

        private void disposeReplicaRun()
        {
            SetReplicaRunBool(false);
            ioHelper.DisposeReplicaScript(Path.GetDirectoryName(scriptPath));
        }
        */
        #endregion

        #region Remotable Stuff from python
        /*
        public void RemoteRun(string scriptName, Dictionary<String, Object> parameters, bool save)
        {
            scriptPath = scriptName;
            saveEnable = save;
            status = RunningState.running;
            Run(parameters);
        }

        public void CloseIt()
        {
            //controllerWindow.Close();
        }

        public void SetSaveDirectory(string saveDirectory)
        {
            updateSaveDirectory(saveDirectory);
        }

        public string getSaveDirectory()
        {
            return saveToDirectory;
        }
        */
        #endregion

        #region Environment Loading
        public void LoadEnvironment(bool daqClassLoad = false)
        {

            if (File.Exists(Utils.configPath + "genOptions.cfg"))
            {
                string fileJson = File.ReadAllText(Utils.configPath + "genOptions.cfg");
                Controller.genOptions = JsonConvert.DeserializeObject<GeneralOptions>(fileJson);
     
            }
            else
                Controller.genOptions = new GeneralOptions();

            if (daqClassLoad)
            {
                if (File.Exists((Utils.configPath + "filesystem.json")))
                {
                    string fileSystemJson = File.ReadAllText(Utils.configPath + "filesystem.json");
                    DAQ.Environment.Environs.FileSystem = JsonConvert.DeserializeObject<DAQ.Environment.FileSystem>(fileSystemJson);
                }

                if (File.Exists((Utils.configPath + "hardware.json")))
                {
                    string hardwareJson = File.ReadAllText(Utils.configPath + "hardware.json");
                    DAQ.Environment.Environs.Hardware = JsonConvert.DeserializeObject<DAQ.HAL.NavigatorHardware>(hardwareJson);
                }

                if (File.Exists((Utils.configPath + "config.json")))
                {
                    string configJson = File.ReadAllText(Utils.configPath + "config.json");
                    config = JsonConvert.DeserializeObject<MMConfig>(configJson);
                }
            }
            
        }

        public void SaveEnvironment()
        {
            string fileJson = JsonConvert.SerializeObject(DAQ.Environment.Environs.FileSystem, Formatting.Indented);
            string hardwareJson = JsonConvert.SerializeObject(DAQ.Environment.Environs.Hardware, Formatting.Indented);
            string configJson = JsonConvert.SerializeObject(config, Formatting.Indented);
            string optionsJson = JsonConvert.SerializeObject(Controller.genOptions, Formatting.Indented);

            File.WriteAllText(Utils.configPath + "filesystem.json", fileJson);
            File.WriteAllText(Utils.configPath + "hardware.json", hardwareJson);
            File.WriteAllText(Utils.configPath + "config.json", configJson);
            File.WriteAllText(Utils.configPath + "genOptions.cfg", optionsJson);
        }

        public static void LoadDefaultSequence()
        {
            if (File.Exists(defaultScriptPath)) LoadSequenceFromPath(defaultScriptPath);
            else sequenceData = new Sequence();

            if (!sequenceData.Parameters.ContainsKey("ElapsedTime")) sequenceData.Parameters["ElapsedTime"] = new Parameter("ElapsedTime", "", 0.0, true, false);
            //if (Environs.Hardware.ContainsInfo("channelMap")) RenameOldChannels((Dictionary<string, string>)Environs.Hardware.GetInfo("channelMap"));
        }

        public static void LoadSequenceFromPath(string path)
        {
            sequenceData = null;
            string sequenceJson = File.ReadAllText(path);
            sequenceData = JsonConvert.DeserializeObject<Sequence>(sequenceJson);
            //RenameOldChannels((Dictionary<string, string>)Environs.Hardware.GetInfo("channelMap"));
            //script.Parameters = sequenceData.CreateParameterDictionary();
        }

        private static void RenameOldChannels(Dictionary<string,string> channelMap)
        {
            foreach (SequenceStep step in sequenceData.Steps)
            {
                foreach (KeyValuePair<string,string> entry in channelMap)
                {
                    if (step.DigitalValueTypes.ContainsKey(entry.Key))
                    {
                        step.DigitalValueTypes[entry.Value] = step.DigitalValueTypes[entry.Key];
                        step.DigitalValueTypes.Remove(entry.Key);
                        Console.WriteLine(string.Format("Renamed channel {0} to {1}", entry.Key, entry.Value));

                    }
                    else if (step.AnalogValueTypes.ContainsKey(entry.Key))
                    {
                        step.AnalogValueTypes[entry.Value] = step.AnalogValueTypes[entry.Key];
                        step.AnalogValueTypes.Remove(entry.Key);
                        Console.WriteLine(string.Format("Renamed channel {0} to {1}", entry.Key, entry.Value));
                    }
                }
            }
        }
        public static void SaveSequenceAsDefault()
        {
            SaveSequenceToPath(defaultScriptPath);
        }

        /*public static void SaveSequenceToPath(string path, List<SequenceStep> steps)
        {
           
            if (sequenceData == null)
            {
                sequenceData = new Sequence();
                sequenceData.CreateParameterList(script.Parameters);
                sequenceData.Steps = steps;
            }
            string sequenceJson = JsonConvert.SerializeObject(sequenceData, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            File.WriteAllText(path, sequenceJson);
        }
        */
        public static void SaveSequenceToPath(string path)
        {
            string sequenceJson = JsonConvert.SerializeObject(sequenceData, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            File.WriteAllText(path, sequenceJson);
        }
        #endregion

        #region Cicero Sequence Loading

        internal void LoadCiceroSequenceFromPath(string filename)
        {
            ciceroSequence = (DataStructures.SequenceData)DataStructures.Common.loadBinaryObjectFromFile(filename);

        }

        internal void LoadCiceroSettingsFromPath(string filename)
        {
            ciceroSettings = (DataStructures.SettingsData)DataStructures.Common.loadBinaryObjectFromFile(filename);
        }

        internal void ConvertCiceroSequence()
        {

            CiceroConverter ciceroConverter = new CiceroConverter();

            ciceroConverter.SetSettingsData(ciceroSettings);
            ciceroConverter.InitMMSequence(sequenceData);

            if (ciceroConverter.CheckValidHardwareChannels() && ciceroConverter.CanConvertFrom(ciceroSequence.GetType())) sequenceData = (Sequence)ciceroConverter.ConvertFrom(ciceroSequence);
        }

        #endregion

        #region Saving and Processing Experiment Data
        public void StartLogging()
        {
            string fileTag = motMasterDataPath + "/" + ExpData.ExperimentName;
            if (!genOptions.BriefData)
            {
                dataLogger = new FileLogger("", fileTag + ".dta");
                paramLogger = new FileLogger("", fileTag + ".prm");
            }          
        }
 
        public void StopLogging()
        {
            //Finishes writing the JSONs. Removes the last comma since Mathematica has issues with it
            if (!Utils.isNull(dataLogger))
            {
                //dataLogger.DropLastChar();
                //paramLogger.DropLastChar();
                dataLogger.log("]\n}");
                dataLogger.Enabled = false;
            }
            if (!Utils.isNull(paramLogger))
            {
                paramLogger.log("]\n}");
                paramLogger.Enabled = false;
            }       
        }

        /// <summary>
        /// Creates the time segments required for the ExpData class. Assumes that there is a digital channel named acquisitionTrigger 
        /// and this is set high during the acquistion time. Any step that should not be saved during this time should be labelled using "DNS"
        /// </summary>
        public static void CreateAcquisitionTimeSegments()
        {
            if (!Environs.Hardware.DigitalOutputChannels.ContainsKey("acquisitionTrigger")) throw new WarningException("No channel named acquisitionTrigger found in Hardware");
            Dictionary<string, Tuple<int, int>> analogSegments = new Dictionary<string, Tuple<int, int>>();
            int sampleRate = ExpData.SampleRate;
            int sampleStartTime = ExpData.PostTrigSamples;
            List<string> ignoredSegments = new List<string>();
            ignoredSegments = sequenceData.Steps.Where(t => (t.Description.Contains("DNS") && t.GetDigitalData("acquisitionTrigger"))).Select(t => t.Name).ToList();
            ignoredSegments.Add("ExtraInterferometer");
            ExpData.IgnoredSegments = ignoredSegments;
            if (ignoredSegments.Count == 0) throw new WarningException("Suspisious situation - No gaps in acquisition section");  

            /*IEnumerable<string> interferometerStepNames=sequenceData.Steps.Where(t => (t.Description.Contains("Interferometer") && t.GetDigitalData("acquisitionTrigger"))).Select(t => t.Name);
            if (interferometerStepNames.Count() > 0) ExpData.InterferometerStepName = interferometerStepNames.First();
            else */           
            ExpData.InterferometerStepName = null;
            int cur = 0; SequenceStep step; double duration; int sampleDuration = 0; 
            int interStart = -1; int beforeTime = 0; int afterTime = 0; int subSection = 0; // begining -> 0; presample -> 1; actual -> 2; postsample -> 3

            for (int i = cur; i <sequenceData.Steps.Count; i++)
            {
                step = sequenceData.Steps[i];
                if (step.Description.Contains("Interferometer"))
                {
                    ExpData.InterferometerStepName = step.Name; cur = i; interStart = sampleStartTime; break; 
                }
            }

            if (interStart.Equals(-1)) throw new Exception("No interferometer section !");

            for (int i = cur; i < sequenceData.Steps.Count; i++)
            {
                step = sequenceData.Steps[i];
                if (!step.GetDigitalData("acquisitionTrigger")) continue;  //   || step.Description.Contains("DNS")
                if (step.Description.Contains("Interferometer"))
                {
                    duration = step.evalDuration(true);
                    sampleDuration = Convert.ToInt32(duration * sampleRate);
                    sampleStartTime += sampleDuration;
                    if (step.Description.Contains("Interferometer+") && subSection < 2)
                    {
                        beforeTime += sampleDuration; subSection = 1;
                    }
                    else subSection = 2;
                    if (step.Description.Contains("Interferometer+") && (subSection > 1))
                    {
                        afterTime += sampleDuration;
                    }
                }
                else
                {
                    cur = i;
                    if (interStart < sampleStartTime) analogSegments["Interferometer"] = Tuple.Create<int, int>(interStart, sampleStartTime);
                    if ((beforeTime > 0) && (afterTime > 0)) analogSegments["ExtraInterferometer"] = Tuple.Create<int, int>(beforeTime, afterTime);
                    break;
                }
            }
            for (int i = cur; i < sequenceData.Steps.Count; i++) // photodiode
            {
                step = sequenceData.Steps[i];
                if (!step.GetDigitalData("acquisitionTrigger")) continue;
                duration = step.evalDuration(true);
                sampleDuration = Convert.ToInt32(duration * sampleRate);
                string name = step.Name;
                Tuple<int, int> segmentTimes = Tuple.Create<int, int>(sampleStartTime, sampleStartTime + sampleDuration);
                analogSegments[name] = segmentTimes;
                sampleStartTime += sampleDuration;
            }
            ExpData.AnalogSegments = analogSegments;
            ExpData.NSamples = sampleStartTime;
        }

        public static MMexec ConvertDataToAxelHub(double[,] aiData, int chn = -1)
        {
            MMexec axelCommand = new MMexec(); int axis = chn;
            axelCommand.sender = "MOTMaster";
            switch (chn)
            {
                case -1: axelCommand.cmd = "shotData"; // obsolete
                    axis = 0;
                    break;
                case 0: axelCommand.cmd = "shot.X";
                    break;
                case 1: axelCommand.cmd = "shot.Y";
                    break;
            }

            Dictionary<string, double[]> segData = ExpData.SegmentShot(aiData, axis);
            if (!Utils.isNull(segData))
                foreach (KeyValuePair<string, double[]> item in segData) 
                        axelCommand.prms[item.Key] = Utils.formatDouble(item.Value,Constants.LogDataFormat);
            axelCommand.prms["runID"] = BatchNumber;
            axelCommand.prms["samplingRate"] = ExpData.SampleRate;
            axelCommand.prms["groupID"] = ExpData.ExperimentName;
            return axelCommand;
        }

        public static int actChannel(int batch = -1, bool swappAxes = false)
        {
            int b = swappAxes ? batch + 1 : batch;
            if (batch.Equals(-1)) b = BatchNumber;
            int chn = -1;
            switch (Math.Abs(ExpData.axis))
            {
                case 0:
                    if (swappAxes) chn = 1;
                    else chn = ExpData.axis;
                    break;
                case 1:
                    if (swappAxes) chn = 0;
                    else chn = ExpData.axis;
                    break;
                case 2:
                    chn = (b % 2).Equals(0) ? 0 : 1;  // X : Y axis                    

                    break;
            }
            return chn;
        }
        public static MMexec[] ConvertDataXYToAxelHub(double[,] aiData)
        {
            int d = aiData.GetLength(0); MMexec[] axelCommand = null;
            if (config.DoubleAxes)
            {
                if (!d.Equals(4)) throw new Exception("Wrong number of analog input channels in the buffer!");
            }
            else
            {
                if (!d.Equals(2)) throw new Exception("Wrong number of analog input channels in the buffer!");
            }
            axelCommand = new MMexec[1];
            int chn = actChannel();
            if (chn > -1) axelCommand[0] = ConvertDataToAxelHub(aiData, chn);
            return axelCommand;
        }
        #endregion

        public static MMexec InitialCommand(MMscan scan)
        {
            MMexec axelCommand = new MMexec();
            axelCommand.sender = "MOTMaster";
 
            axelCommand.mmexec = ExpData.Description;
            axelCommand.prms["params"] = sequenceData.CreateParameterDictionary(); // all params with values
            axelCommand.prms["scanPrms"] = sequenceData.ScannableParams(); // list of scanables params
            axelCommand.prms["steps"] = sequenceData.CreateStepsDictionary(true);
            axelCommand.prms["sampleRate"] = ExpData.SampleRate;
            axelCommand.prms["runID"] = BatchNumber;
            axelCommand.prms["groupID"] = ExpData.ExperimentName;
            if (scan != null)
            {
                MMscan s2 = (MMscan)scan;
                s2.ToDictionary(ref axelCommand.prms);
               // axelCommand.prms["scanParam"] = scanParam;
                axelCommand.cmd = "scan";
            }
            else
            {
                axelCommand.cmd = "repeat";
                axelCommand.prms["cycles"] = numInterations;
            }
            return axelCommand;
        }

        #region MSquared Control - Maybe move elsewhere?

        public static bool CheckPhaseLock()
        {
            if (!config.Debug)
            {
                DAQ.HAL.ICEBlocPLL.Lock_Status lockStatus = new DAQ.HAL.ICEBlocPLL.Lock_Status();
                bool locked = M2PLL.main_lock_status(out lockStatus);
                //if (!locked) ErrorMng.errorMsg("PLL lock is not engaged - currently " + lockStatus.ToString(),10,false);
                return locked;
            }
            else return true;
        }
        #endregion

        internal static void SetParameter(string key, object p)
        {
            if (sequenceData.Parameters.ContainsKey(key)) sequenceData.Parameters[key].Value = p;
            else sequenceData.Parameters[key] = new Parameter(key, "", p, true, false);
        }

        internal static void SaveTempSequence(bool? saveSeq = null, MMscan scan = null)
        {
            bool SaveSeq = false;
            if (Utils.isNull(saveSeq))
            {
                SaveSeq = Controller.genOptions.SaveSeqB4proc || Utils.keyStatus("Shift");
            }
            else SaveSeq = (bool)saveSeq;
            if (SaveSeq)
            {
                string fn = Utils.timeName();
                if (!Utils.isNull(scan))
                    if (!fn.Equals("---") && !scan.groupID.Equals("")) fn = scan.groupID;
                string ffn = scriptListPath + "\\" + fn;
                SaveSequenceToPath(ffn + ".sm2");
                if (!Utils.isNull(scan))
                {
                    File.WriteAllText(ffn + ".scn", JsonConvert.SerializeObject(scan, Formatting.Indented));
                }
            }
        }
              
        internal static void UpdateAIValues()
        {
            ExpData.PostTrigSamples = Controller.genOptions.PostTrigSamples;
            ExpData.SampleRate = Controller.genOptions.AISampleRate;
            ExpData.RiseTime = Controller.genOptions.RiseTime;
            ExpData.SkimEdges = new Dictionary<string, int>(Controller.genOptions.Skim);
        }
    }

    public class DataEventArgs : EventArgs
    {
        public object Data { get; set; }
        public DataEventArgs(object data)
            : base()
        {
            Data = data;
        }
    }
}
