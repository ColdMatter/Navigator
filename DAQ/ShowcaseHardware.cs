using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Media;
using NationalInstruments.DAQmx;
using DAQ.Pattern;

using UtilsNS;

namespace DAQ.HAL
{
    /// <summary>
    /// This is the specific hardware for the Navigator experiment. Currently, the channels used must be specified here. At a later date, the physical channels may be defined inside a settings file for the hardware controller.
    /// </summary>
    public class ShowcaseHardware : DAQ.HAL.Hardware
    {       
        public ShowcaseHardware()
        {
            //add information for MMConfig
            config = new MMConfig(false, false, false, Environment.Environs.Debug);
            config.DoubleAxes = true;
            config.HSDIOCard = true;
            config.UseAI = true;
            config.DigitalPatternClockFrequency = 20000000;
            config.UseMMScripts = false;
            config.UseMSquared = true;
            config.UseMuquans = false;
            Info.Add("MotMasterConfiguration", config);
            //add the boards - perhaps these values can be derived from a settings file
            Boards.Add("multiDAQ", "/Dev1");
            Boards.Add("analogOut", "/Dev2");
            //The HSDIO card cannot be referenced with a leading forward slash like DAQ cards
            Boards.Add("hsDigital", "Dev3");
            Boards.Add("analogIn", "/Dev4");
            string multiBoard = (string)Boards["multiDAQ"];
            string aoBoard = (string)Boards["analogOut"];
            string hsdioBoard = (string)Boards["hsDigital"];
            string aiBoard = (string)Boards["analogIn"];
            //Collect each type of board into a list - this is useful if we need to loop over each
            List<string> aoBoards = new List<string>();
            List<string> aiBoards = new List<string>();
            List<string> doBoards = new List<string>();
            aoBoards.Add(multiBoard);
            aiBoards.Add(aiBoard);
            aiBoards.Add(multiBoard);
            doBoards.Add(hsdioBoard);


            //A list of trigger lines for each card
            Info.Add("sampleClockLine", (string)Boards["hsDigital"] + "/PXI_Trig0");
            Info.Add("analogInTrigger0", (string)Boards["multiDAQ"] + "/PXI_Trig1");
            Info.Add("AOPatternTrigger", (string)Boards["analogOut"] + "/PXI_Trig1");
            //Info.Add("analogInClock", (string)Boards["analogOut"] + "/ao/SampleClock");
            Info.Add("analogInTrigger1", (string)Boards["multiDAQ"] + "/PXI_Trig2");
            Info.Add("HSTrigger", "PXI_Trig1");

            //Add identifiers for each card

            Info.Add("analogOutBoards", aoBoards);
            Info.Add("analogInBoards", aiBoards);
            Info.Add("digitalBoards", doBoards);
            Info.Add("AIAcquireTrigger", "pfi0");

            //Add other instruments such as serial channels
            ExtDevices["WindFreak"] = "2";
            Dictionary<string, string> wDict = Utils.readDict(Utils.configPath + "WindFreak.CFG");
            Instruments.Add("microwaveSynth", new WindfreakSynth(wDict.ContainsKey("dev1") ? wDict["dev1"] : "ASRL12::INSTR"));
            Instruments.Add("microwaveSynth2", new WindfreakSynth(wDict.ContainsKey("dev2") ? wDict["dev2"] : "ASRL04::INSTR"));

            ExtDevices["MSquared"] = "1";
            Instruments.Add("MSquaredDCS", new ICEBlocDCS());
            Instruments.Add("MSquaredPLL", new ICEBlocPLL());
          
            // map of all analog, digital and counter channels 
            // complex names (name/showAs): 
            //      'name' is the channel name in sequence file
            //      'showAs' is the name in the table; 
            //      if 'showAs' is equal to "@" the channel is hidden in the table; 
            //      make sure that all hidden channel are NOT connected, because of the lack of control over them
            //      if 'showAs' is missing, 'name' is assumed for showAs
            // the channels will appears in the order of creation
            // the valid color names: http://axelsuite.com/images/001-allcolors.png 

            //map the analog output channels
            AddAnalogOutputChannel("motCTRL", aoBoard + "/ao2", -10, 10); // absent
            AddAnalogOutputChannel("mphiCTRL", aoBoard + "/ao3", -10, 10); // absent
            AddAnalogOutputChannel("aom2DFreq", aoBoard + "/ao4", -10, 10, Brushes.Green);
            AddAnalogOutputChannel("aomPushFreq", aoBoard + "/ao5", -10, 10, Brushes.Green);
            AddAnalogOutputChannel("aomXPFreq", aoBoard + "/ao6", -10, 10, Brushes.DarkSlateBlue);
            AddAnalogOutputChannel("aomXMFreq", aoBoard + "/ao7", -10, 10, Brushes.DarkSlateBlue);
            AddAnalogOutputChannel("aomYPFreq", aoBoard + "/ao8", -10, 10, Brushes.DarkSlateBlue);
            AddAnalogOutputChannel("aomYMFreq", aoBoard + "/ao9", -10, 10, Brushes.DarkSlateBlue);
            AddAnalogOutputChannel("aomZPFreq", aoBoard + "/ao10", -10, 10, Brushes.DarkSlateBlue);
            AddAnalogOutputChannel("aomZMFreq", aoBoard + "/ao11", -10, 10, Brushes.DarkSlateBlue);
            AddAnalogOutputChannel("DetFreq", aoBoard + "/ao0", -10, 10, Brushes.DarkSlateBlue);
            AddAnalogOutputChannel("xbias2DCoil", aoBoard + "/ao13", -10, 10, Brushes.Green);
            AddAnalogOutputChannel("ybias2DCoil", aoBoard + "/ao14", -10, 10, Brushes.Green);
            AddAnalogOutputChannel("2DMOTCoil", aoBoard + "/ao19", 0, 10, Brushes.Green);
            AddAnalogOutputChannel("xbias3DCoil", aoBoard + "/ao15", -10, 10, Brushes.DarkSlateBlue);
            AddAnalogOutputChannel("ybias3DCoil", aoBoard + "/ao16", -10, 10, Brushes.DarkSlateBlue);
            AddAnalogOutputChannel("zbias3DCoil", aoBoard + "/ao17", -10, 10, Brushes.DarkSlateBlue);
            AddAnalogOutputChannel("zmaomFreq/3DMOTCoil", aoBoard + "/ao18", -10, 10, Brushes.DarkSlateBlue);
            AddAnalogOutputChannel("aom2DAtt", aoBoard + "/ao20", -10, 10, Brushes.Green);
            AddAnalogOutputChannel("aomPushAtt", aoBoard + "/ao21", -10, 10, Brushes.Green);
            AddAnalogOutputChannel("aomXPAtt", aoBoard + "/ao22", -10, 10, Brushes.DarkSlateBlue);
            AddAnalogOutputChannel("aomXMAtt", aoBoard + "/ao23", -10, 10, Brushes.DarkSlateBlue);
            AddAnalogOutputChannel("aomYPAtt", aoBoard + "/ao24", -10, 10, Brushes.DarkSlateBlue);
            AddAnalogOutputChannel("aomYMAtt", aoBoard + "/ao25", -10, 10, Brushes.DarkSlateBlue);
            AddAnalogOutputChannel("aomZPAtt", aoBoard + "/ao26", -10, 10, Brushes.DarkSlateBlue);
            AddAnalogOutputChannel("aomZMAtt", aoBoard + "/ao27", -10, 10, Brushes.DarkSlateBlue);
            AddAnalogOutputChannel("DetAttn", aoBoard + "/ao1", -10, 10, Brushes.DarkSlateBlue);
            AddAnalogOutputChannel("chirpFreq", aoBoard + "/ao28", -10, 10);
            AddAnalogOutputChannel("3DMOTCoil/spareCoil", aoBoard + "/ao12", -10, 10);
            //map the analog input channels
            AddAnalogInputChannel("photodiode", aiBoard + "/ai0", AITerminalConfiguration.Pseudodifferential, -10, 10);
            AddAnalogInputChannel("photodiode2", aiBoard + "/ai1", AITerminalConfiguration.Pseudodifferential, -10, 10);
            AddAnalogInputChannel("accelerometer", aiBoard + "/ai2", AITerminalConfiguration.Pseudodifferential, -10, 10);
            AddAnalogInputChannel("accelerometer2", aiBoard + "/ai3", AITerminalConfiguration.Pseudodifferential, -10, 10);

            //map the digital output channels
            AddDigitalOutputChannel("acquisitionTrigger", hsdioBoard, 0, 22, Brushes.Crimson);
            AddDigitalOutputChannel("M2TTL", hsdioBoard, 0, 20, Brushes.Crimson);
            AddDigitalOutputChannel("aom2DTTL", hsdioBoard, 0, 0, Brushes.DarkBlue);
            AddDigitalOutputChannel("aomPushTTL", hsdioBoard, 0, 1, Brushes.DarkBlue);
            AddDigitalOutputChannel("aomXPTTL", hsdioBoard, 0, 2, Brushes.Crimson);
            AddDigitalOutputChannel("aomXMTTL", hsdioBoard, 0, 3, Brushes.Crimson);
            AddDigitalOutputChannel("aomYPTTL", hsdioBoard, 0, 4, Brushes.Crimson);
            AddDigitalOutputChannel("aomYMTTL", hsdioBoard, 0, 5, Brushes.Crimson);
            AddDigitalOutputChannel("aomZPTTL", hsdioBoard, 0, 6, Brushes.Crimson);
            AddDigitalOutputChannel("aomZMTTL", hsdioBoard, 0, 7, Brushes.Crimson);
            AddDigitalOutputChannel("FreeDO2/aomDetTTL", hsdioBoard, 0, 26, Brushes.Crimson);
            AddDigitalOutputChannel("LQTTL/DetTTL", hsdioBoard, 0, 21, Brushes.DarkBlue);
            AddDigitalOutputChannel("MWTTL", hsdioBoard, 0, 24, Brushes.DarkBlue);
            AddDigitalOutputChannel("FreeDO1", hsdioBoard, 0, 25, Brushes.DarkBlue);//
            if (config.UseMuquans) AddDigitalOutputChannel("serialPreTrigger", hsdioBoard, 0, 31);

            AddCounterChannel("Counter", multiBoard + "/ctr0");

            //Adds a Channel map to convert channel names from old sequences
            Dictionary<string, string> channelMap = new Dictionary<string, string>(); // channelMap["old_name"] = "new_name";
            channelMap["cameraTTL"] = "mainMicrowaveTTL";
            channelMap["Analog Trigger"] = "fm1MicrowaveTTL";
            //  channelMap["ramanTTL"] = "lcTTL";
            //  channelMap["ramanDDSTrig"] = "msquaredTTL";
            //  channelMap["shutter"] = "fp1MicrowaveTTL";
            channelMap["pushaomTTL"] = "f0MicrowaveTTL";

            Info.Add("channelMap", channelMap);
        }
    }
}
