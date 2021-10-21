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
    public class PlexalHardware : DAQ.HAL.Hardware
    {
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
            //Instruments.Add("muquansSlave", new MuquansRS232("ASRL18::INSTR", "slave"));//
            //Instruments.Add("muquansAOM", new MuquansRS232("ASRL20::INSTR", "aom"));//

            ExtDevices["WindFreak"] = "1";
            Instruments.Add("microwaveSynth", new WindfreakSynth("ASRL12::INSTR"));

            ExtDevices["FlexDDS"] = "1";

            //Instruments.Add("MSquaredDCS", new ICEBlocDCS());
            //Instruments.Add("MSquaredPLL", new ICEBlocPLL());
            //Instruments.Add("microwaveSynth", new Gigatronics7100Synth("GPIB1::19::INSTR"));

            // map of all analog, digital and counter channels 
            // complex names (name/showAs): 
            //      'name' is the channel name in sequence file
            //      'showAs' is the name in the table; 
            //      if 'showAs' is equal to "@" the channel is hidden in the table; 
            //      make sure that all hidden channel are NOT connected, because of the lack of control over them
            //      if 'showAs' is missing, 'name' is assumed for showAs
            // the channels will appears in the order of creation
            // the valid color names: http://axelsuite.com/images/001-allcolors.png 

            // map the analog output channels
            AddAnalogOutputChannel("AO-0/VCO", aoBoard + "/ao0", -10, 10, Brushes.Blue);
            AddAnalogOutputChannel("AO-1/MasterRes", aoBoard + "/ao1", -10, 10, Brushes.Green);
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

            // map the analog input channels
            AddAnalogInputChannel("accelerometer", aiBoard + "/ai0", AITerminalConfiguration.Differential, -10, 10);
            AddAnalogInputChannel("photodiode", aiBoard + "/ai1", AITerminalConfiguration.Differential, -10, 10);
            AddAnalogInputChannel("temperature", aiBoard + "/ai2", AITerminalConfiguration.Differential, -10, 10);

            // map the digital channels
            AddDigitalOutputChannel("digit-0/DDS-Trigger", hsdioBoard, 0, 0, Brushes.Blue);
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

            ExtDevices["WindFreak"] = "1";
            Dictionary<string, string> wDict = Utils.readDict(Utils.configPath + "WindFreak.CFG");

        }
    }
}
