using System;
using System.Linq;
using System.Collections.Generic;

using NationalInstruments.DAQmx;

using DAQ.Pattern;

namespace DAQ.HAL
{
    /// <summary>
    /// This is the specific hardware for the Navigator experiment. Currently, the channels used must be specified here. At a later date, the physical channels may be defined inside a settings file for the hardware controller.
    /// </summary>
    public class PlexalHardware : DAQ.HAL.Hardware
    {
        public MMConfig config { get; set; }
        public PlexalHardware()
        {
            //add information for MMConfig
            config = new MMConfig(false, false, false, Environment.Environs.Debug);
            config.HSDIOCard = true;
            config.UseAI = true;
            config.DigitalPatternClockFrequency = 20000000;
            config.UseMMScripts = false;
            config.UseMSquared = false;
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
            Instruments.Add("muquansSlave", new MuquansRS232("ASRL18::INSTR", "slave"));//
            Instruments.Add("muquansAOM", new MuquansRS232("ASRL20::INSTR", "aom"));//
            Instruments.Add("microwaveSynth", new WindfreakSynth("ASRL12::INSTR"));

            Instruments.Add("MSquaredDCS", new ICEBlocDCS());
            Instruments.Add("MSquaredPLL", new ICEBlocPLL());
            //Instruments.Add("microwaveSynth", new Gigatronics7100Synth("GPIB1::19::INSTR"));


            //map the digital channels
            //complex names (name/showAs) 
            //for analog, digital and counter channels 

            AddDigitalOutputChannel("aom2DTTL", hsdioBoard, 0, 0);  
            AddDigitalOutputChannel("aomPushTTL", hsdioBoard, 0, 1); 
            AddDigitalOutputChannel("aomXPTTL", hsdioBoard, 0, 2);
            AddDigitalOutputChannel("aomXMTTL", hsdioBoard, 0, 3);
            AddDigitalOutputChannel("aomYPTTL", hsdioBoard, 0, 4); 
            AddDigitalOutputChannel("aomYMTTL", hsdioBoard, 0, 5);
            AddDigitalOutputChannel("aomZPTTL", hsdioBoard, 0, 6); 
            AddDigitalOutputChannel("aomZMTTL", hsdioBoard, 0, 7);
            AddDigitalOutputChannel("M2TTL", hsdioBoard, 0, 20);//
            AddDigitalOutputChannel("LQTTL/DetTTL", hsdioBoard, 0, 21);
            AddDigitalOutputChannel("acquisitionTrigger", hsdioBoard, 0, 22);
            AddDigitalOutputChannel("MWTTL", hsdioBoard, 0, 24);
            AddDigitalOutputChannel("FreeDO1", hsdioBoard, 0, 25);//
            AddDigitalOutputChannel("FreeDO2", hsdioBoard, 0, 26);//
            if (config.UseMuquans) AddDigitalOutputChannel("serialPreTrigger", hsdioBoard, 0, 31);

            //map the analog output channels
            AddAnalogOutputChannel("motCTRL", aoBoard + "/ao2", -10, 10);//
            AddAnalogOutputChannel("mphiCTRL", aoBoard + "/ao3", -10, 10);//
            AddAnalogOutputChannel("aom2DFreq", aoBoard + "/ao4", -10, 10);
            AddAnalogOutputChannel("aomPushFreq", aoBoard + "/ao5", -10, 10);
            AddAnalogOutputChannel("aomXPFreq", aoBoard + "/ao6", -10, 10);
            AddAnalogOutputChannel("aomXMFreq", aoBoard + "/ao7", -10, 10);
            AddAnalogOutputChannel("aomYPFreq", aoBoard + "/ao8", -10, 10);
            AddAnalogOutputChannel("aomYMFreq", aoBoard + "/ao9", -10, 10);
            AddAnalogOutputChannel("aomZPFreq", aoBoard + "/ao10", -10, 10);
            AddAnalogOutputChannel("aomZMFreq", aoBoard + "/ao11", -10, 10);
            AddAnalogOutputChannel("xbias2DCoil", aoBoard + "/ao13", -10, 10);
            AddAnalogOutputChannel("ybias2DCoil", aoBoard + "/ao14", -10, 10);
            AddAnalogOutputChannel("2DMOTCoil", aoBoard + "/ao19", 0, 10);
            AddAnalogOutputChannel("xbias3DCoil", aoBoard + "/ao15", -10, 10);
            AddAnalogOutputChannel("ybias3DCoil", aoBoard + "/ao16", -10, 10);
            AddAnalogOutputChannel("zbias3DCoil", aoBoard + "/ao17", -10, 10);
            AddAnalogOutputChannel("3DMOTCoil/spareCoil", aoBoard + "/ao12", -10, 10);
            AddAnalogOutputChannel("zmaomFreq/3DMOTCoil", aoBoard + "/ao18", -10, 10);
            AddAnalogOutputChannel("aom2DAtt", aoBoard + "/ao20", -10, 10);
            AddAnalogOutputChannel("aomPushAtt", aoBoard + "/ao21", -10, 10);
            AddAnalogOutputChannel("aomXPAtt", aoBoard + "/ao22", -10, 10);
            AddAnalogOutputChannel("aomXMAtt", aoBoard + "/ao23", -10, 10);
            AddAnalogOutputChannel("aomYPAtt", aoBoard + "/ao24", -10, 10);
            AddAnalogOutputChannel("aomYMAtt", aoBoard + "/ao25", -10, 10);
            AddAnalogOutputChannel("aomZPAtt", aoBoard + "/ao26", -10, 10);
            AddAnalogOutputChannel("aomZMAtt", aoBoard + "/ao27", -10, 10); 
            AddAnalogOutputChannel("DetAttn/rPhaseV", aoBoard + "/ao1", -10, 10);
            AddAnalogOutputChannel("DetFreq", aoBoard + "/ao0", -10, 10);

            // AddAnalogOutputChannel("analogTest", aoBoard + "/ao24", -10, 10);

            //map the analog input channels
            AddAnalogInputChannel("accelerometer", aiBoard + "/ai0", AITerminalConfiguration.Pseudodifferential, -10, 10);
            AddAnalogInputChannel("photodiode", aiBoard + "/ai1", AITerminalConfiguration.Pseudodifferential, -10, 10);
            AddAnalogInputChannel("fibrePD", aiBoard + "/ai3", AITerminalConfiguration.Differential);
            //AddAnalogInputChannel("forwardRamanPD", multiBoard + "/ai0", AITerminalConfiguration.Differential);
            //AddAnalogInputChannel("backwardRamanPD", multiBoard + "/ai1", AITerminalConfiguration.Differential);
            //AddAnalogInputChannel("motPD", multiBoard + "/ai2", AITerminalConfiguration.Differential);
            //AddAnalogInputChannel("slave0Error", multiBoard + "/ai3", AITerminalConfiguration.Differential);
            //AddAnalogInputChannel("slave1Error", multiBoard + "/ai4", AITerminalConfiguration.Differential);
            //AddAnalogInputChannel("slave2Error", multiBoard + "/ai5", AITerminalConfiguration.Differential);

            //AddCounterChannel("Counter", multiBoard + "/ctr0"); Muquans ONLY !!!

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
