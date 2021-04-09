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
            Boards.Add("multiDAQ", "/Dev1"); // NI PXIe-6341
            Boards.Add("analogOut", "/Dev2"); // NI PXIe-6738
            //The HSDIO card cannot be referenced with a leading forward slash like DAQ cards
            Boards.Add("hsDigital", "Dev3"); // NI PXI-6541 
            Boards.Add("analogIn", "/Dev1");
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

            /*  AddDigitalOutputChannel("aom2DTTL", hsdioBoard, 0, 0);  
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
              if (config.UseMuquans) AddDigitalOutputChannel("serialPreTrigger", hsdioBoard, 0, 31);*/

            AddDigitalOutputChannel("digit-0/DDS-Trigger", hsdioBoard, 0, 0);
            AddDigitalOutputChannel("digit-1/PL-TTL", hsdioBoard, 0, 1);
            AddDigitalOutputChannel("digit-2/HG-TTL", hsdioBoard, 0, 2);
            AddDigitalOutputChannel("digit-3/Chirp", hsdioBoard, 0, 3);
            AddDigitalOutputChannel("digit-4/Cooling", hsdioBoard, 0, 4);
            AddDigitalOutputChannel("digit-5/Repump", hsdioBoard, 0, 5);
            AddDigitalOutputChannel("digit-6/DDS-Reset", hsdioBoard, 0, 6);
            AddDigitalOutputChannel("digit-7/MOTCoil", hsdioBoard, 0, 7);
            AddDigitalOutputChannel("digit-8/DDS0-0-AmpSWEnable", hsdioBoard, 0, 8);
            AddDigitalOutputChannel("digit-9/DDS0-0-FreqSWEnable", hsdioBoard, 0, 9);
            AddDigitalOutputChannel("digit-10/DDS0-1-AmpSWEnable", hsdioBoard, 0, 10);
            AddDigitalOutputChannel("digit-11/DDS0-1-FreqSWEnable", hsdioBoard, 0, 11);
            AddDigitalOutputChannel("digit-12/DDS1-0-AmpSWEnable", hsdioBoard, 0, 12);
            AddDigitalOutputChannel("digit-13/DDS1-0-FreqSWEnable", hsdioBoard, 0, 13);
            AddDigitalOutputChannel("digit-14/DDS1-1-AmpSWEnable", hsdioBoard, 0, 14);
            AddDigitalOutputChannel("digit-15/DDS1-1-FreqSWEnable", hsdioBoard, 0, 15);
            AddDigitalOutputChannel("digit-16", hsdioBoard, 0, 16);
            AddDigitalOutputChannel("digit-17", hsdioBoard, 0, 17);
            AddDigitalOutputChannel("digit-18", hsdioBoard, 0, 18);
            AddDigitalOutputChannel("digit-19", hsdioBoard, 0, 19);
            AddDigitalOutputChannel("digit-20", hsdioBoard, 0, 20);
            AddDigitalOutputChannel("digit-21", hsdioBoard, 0, 21);
            AddDigitalOutputChannel("acquisitionTrigger", hsdioBoard, 0, 22);
            AddDigitalOutputChannel("digit-23", hsdioBoard, 0, 23);
            AddDigitalOutputChannel("digit-24", hsdioBoard, 0, 24);
            AddDigitalOutputChannel("digit-25", hsdioBoard, 0, 25);
            AddDigitalOutputChannel("digit-26", hsdioBoard, 0, 26);
            AddDigitalOutputChannel("digit-27", hsdioBoard, 0, 27);
            AddDigitalOutputChannel("digit-28", hsdioBoard, 0, 28);
            AddDigitalOutputChannel("digit-29", hsdioBoard, 0, 29);
            AddDigitalOutputChannel("digit-30", hsdioBoard, 0, 30);
            if (config.UseMuquans) AddDigitalOutputChannel("serialPreTrigger", hsdioBoard, 0, 31);

            //map the analog output channels
            /*   AddAnalogOutputChannel("motCTRL", aoBoard + "/ao2", -10, 10);//
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

               // AddAnalogOutputChannel("analogTest", aoBoard + "/ao24", -10, 10); */
            AddAnalogOutputChannel("AO-0/VCO", aoBoard + "/ao0", -10, 10);
            AddAnalogOutputChannel("AO-1/MasterRes", aoBoard + "/ao1", -10, 10);
            AddAnalogOutputChannel("AO-2/SlaveRes1", aoBoard + "/ao2", -10, 10);
            AddAnalogOutputChannel("AO-3/PhaseShifter", aoBoard + "/ao3", -10, 10);
            AddAnalogOutputChannel("AO-4/BiasCoilCtrl", aoBoard + "/ao4", -10, 10);
            AddAnalogOutputChannel("AO-5/ShimX", aoBoard + "/ao5", -10, 10);
            AddAnalogOutputChannel("AO-6/ShimY", aoBoard + "/ao6", -10, 10);
            AddAnalogOutputChannel("AO-7/ShimZ", aoBoard + "/ao7", -10, 10);
            AddAnalogOutputChannel("AO-8/DDS0-0-Amp", aoBoard + "/ao8", -10, 10);
            AddAnalogOutputChannel("AO-9/DDS0-0-Freq", aoBoard + "/ao9", -10, 10);
            AddAnalogOutputChannel("AO-10/DDS0-0-AmpSWRate", aoBoard + "/ao10", -10, 10);
            AddAnalogOutputChannel("AO-11/DDS0-0-FreqSWRate", aoBoard + "/ao11", -10, 10);
            AddAnalogOutputChannel("AO-12/DDS0-1-Amp", aoBoard + "/ao12", -10, 10);
            AddAnalogOutputChannel("AO-13/DDS0-1-Freq", aoBoard + "/ao13", -10, 10);
            AddAnalogOutputChannel("AO-14/DDS0-1-AmpSWRate", aoBoard + "/ao14", -10, 10);
            AddAnalogOutputChannel("AO-15/DDS0-1-FreqSWRate", aoBoard + "/ao15", -10, 10);
            AddAnalogOutputChannel("AO-16/DDS1-0-Amp", aoBoard + "/ao16", -10, 10);
            AddAnalogOutputChannel("AO-17/DDS1-0-Freq", aoBoard + "/ao17", -10, 10);
            AddAnalogOutputChannel("AO-18/DDS1-0-AmpSWRate", aoBoard + "/ao18", -10, 10);
            AddAnalogOutputChannel("AO-19/DDS1-0-FreqSWRate", aoBoard + "/ao19", -10, 10);
            AddAnalogOutputChannel("AO-20/DDS1-1-Amp", aoBoard + "/ao20", -10, 10);
            AddAnalogOutputChannel("AO-21/DDS1-1-Freq", aoBoard + "/ao21", -10, 10);
            AddAnalogOutputChannel("AO-22/DDS1-1-AmpSWRate", aoBoard + "/ao22", -10, 10);
            AddAnalogOutputChannel("AO-23/DDS1-1-FreqSWRate", aoBoard + "/ao23", -10, 10);
            AddAnalogOutputChannel("AO-24/@", aoBoard + "/ao24", -10, 10);
            AddAnalogOutputChannel("AO-25", aoBoard + "/ao25", -10, 10);
            AddAnalogOutputChannel("AO-26", aoBoard + "/ao26", -10, 10);
            AddAnalogOutputChannel("AO-27", aoBoard + "/ao27", -10, 10);
            AddAnalogOutputChannel("AO-28", aoBoard + "/ao28", -10, 10);
            AddAnalogOutputChannel("AO-29", aoBoard + "/ao29", -10, 10);
            AddAnalogOutputChannel("AO-30", aoBoard + "/ao30", -10, 10);
            AddAnalogOutputChannel("AO-31", aoBoard + "/ao31", -10, 10);

            //map the analog input channels
            AddAnalogInputChannel("accelerome" +
                "" +
                "" +
                "ter", aiBoard + "/ai0", AITerminalConfiguration.Differential, -10, 10);
            AddAnalogInputChannel("photodiode", aiBoard + "/ai1", AITerminalConfiguration.Differential, -10, 10);
            AddAnalogInputChannel("temperature", aiBoard + "/ai2", AITerminalConfiguration.Differential, -10, 10);
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
