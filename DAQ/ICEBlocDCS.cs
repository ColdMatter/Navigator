﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilsNS;

namespace DAQ.HAL
{
    public class PhaseStrobes
    {
        public bool DoubleStrobe = false;
        public int Count = 0;

        public delegate void StrobeHandler(bool doubleStrobe, double strobePos, int counter);
        public event StrobeHandler OnStrobe;

        protected void ProcStrobe(bool doubleStrobe, double strobePos, int counter)
        {
            if (OnStrobe != null) OnStrobe(doubleStrobe, strobePos, counter);
        }

        //two ways to operate: Correction (after init strb1/strb2 in the beginning)
        // or setting Strobe1/Strobe2
        public void Correction(double corr, int counter  = -1)
        {
            if (counter != -1) Count = counter;            
            if (DoubleStrobe)
            {
                if ((Count % 2) == 0) Strobe1 += corr;
                else Strobe2 += corr;
            }
            else Strobe1 += corr;
            Count += 1;
        }
        private double _strobe1;
        public double Strobe1 // even count in case of double strobe
        {
            get { return _strobe1; }
            set 
            { 
                _strobe1 = value;
                ProcStrobe(DoubleStrobe, value, Count);
                Count += 1;
            }
        }
        private double _strobe2;
        public double Strobe2 // even count in case of double strobe
        {
            get { return _strobe2; }
            set
            {
                _strobe2 = value;
                ProcStrobe(DoubleStrobe, value, Count);
                Count += 1;
            }
        }
    }

    public class ICEBlocDCS : ICEBlocRemote
    {
        private static Dictionary<string,string> _timeBlockNames;
        private Dictionary<string, string> _dcsChannelNames;

        private Dictionary<string, Dictionary<string,object>> _timeBlockDict;

        public ICEBlocDCS()
        {
            my_ip_address = "192.168.1.100";
            my_byte_ip_address[3] = 100;
            M2_ip_address = "192.168.1.236";
            M2_ip_port = 1024;
            _timeBlockNames = new Dictionary<string, string>();
            int stepNo = 1;
            foreach (string name in new List<string>(){"Setup",
                "Wait",
                "State Selection 1",
                "Delay",
                "State Selection 2",
                "Delay",
                "Dark 1",
                "Pulse 1",
                "Dark 2",
                "Pulse 2",
                "Dark 3",
                "Pulse 3",
                "Dwell",
                "Measure"
            })
            {
                _timeBlockNames[name] = "time_block_" + stepNo;
                stepNo++;
            }
           
            _dcsChannelNames = new Dictionary<string, string>();

            _dcsChannelNames["Sequence Trigger"] = "dio_5";
            _dcsChannelNames["X-Axis Running"] = "dio_6";
            _dcsChannelNames["Y-Axis Running"] = "dio_7";
            _dcsChannelNames["Z-Axis Running"] = "dio_8";
            _dcsChannelNames["Chirp Control"] = "dio_9";
            _dcsChannelNames["RF Switch Master"] = "dio_18";
            _dcsChannelNames["RF Switch X"] = "dio_19";
            _dcsChannelNames["RF Switch Y"] = "dio_20";
            _dcsChannelNames["RF Switch Z"] = "dio_21";
            _dcsChannelNames["Phase Shift"] = "ano_8";
            _dcsChannelNames["AOM Master"] = "vga_1";
            _dcsChannelNames["AOM X"] = "vga_2";
            _dcsChannelNames["AOM Y"] = "vga_3";
            _dcsChannelNames["AOM Z"] = "vga_4";

            _timeBlockDict = new Dictionary<string, Dictionary<string,object>>();

        }

        public int UpdateSequenceParameters(bool report = false)
        {
            string op = "update_sequence_parameters";
            var prms = _timeBlockDict.ToDictionary(item => item.Key, item => (object) item.Value);
            var rslt = GenericCommand(op, prms, report);
            //Clears the update dictionary after sending the command
            _timeBlockDict.Clear();
            if (report) AdjustReport(ref rslt);
            if (rslt.Count == 0) return 2;
            if (report) return ((int) rslt["report"]);
            else return ((int) rslt["status"]);
        }

        private int FPGARun(string mode, bool report = true)
        {
            string op = "fpga_run";
            int id;
            switch (mode)
            {
                case "Start":
                    id = 1;
                    break;
                case "Stop":
                    id = 0;
                    break;
                case "Load":
                    id = 2;
                    break;
                default:
                    throw new Exception("Incorrect mode specified for FPGA run");
                    break;
            }
            Dictionary<string, object> command = new Dictionary<string, object>();
            command["mode"] = id;
            var rslt = GenericCommand(op, command, report);
            if (report) AdjustReport(ref rslt);
            if (rslt.Count == 0) return 2;
            if (report) return ((int)rslt["report"]);
            else return ((int)rslt["status"]);
        }

        #region TimeBlock Dictionary Builders
        //private void AddToTimeBlockDict(string timeBlock,
        //    Dictionary<string, object> channelDictionary)
        //{
        //    int id;
        //    if (_timeBlockNames.Contains(timeBlock))
        //        id = _timeBlockNames.IndexOf(timeBlock) + 1; //Time block index starts from 1
        //    else throw new Exception("Given timeblock name does not exist in DCS.");
        //    string timeblockID = "time_block_" + id;
        //    _timeBlockDict[timeblockID] = channelDictionary;
        //}

        //private void AddToChannelDict(string param, object value, ref Dictionary<string, object> channelDict)
        //{
        //    if (param == "length" && value is int) channelDict[param] = value;
        //    else if (param == "multiplier" && value is double) channelDict[param] = value;
        //    else if (_dcsChannelNames.ContainsKey(param)) channelDict[_dcsChannelNames[param]] = value;
        //    else
        //        throw new Exception(string.Format("Parameter {0} not found or Value {1} is not the correct type", param,
        //            value));
        //}
        public bool blockControl(int timeBlock, int ano, double value)
        {
            string msg = @"{""message"":{""transmission_id"":[888],""op"":""update_sequence_parameters"",""parameters"":{""time_block_" + timeBlock.ToString() +
                @""":{""ano_" + ano.ToString() + @""":{""phase"":[" + value.ToString("G6") + "]}}}}}";
            Send(msg);
            string rly = Receive();
            Dictionary<string, object> dct = ConvertMessageToDictionary("update_sequence_parameters", rly);
            int i = Convert.ToInt32(dct["status"]);
            return i.Equals(1);
        }

        public bool phaseControl(double phase)
        {
            return blockControl(29, 8, phase) && blockControl(31, 8, phase);
        }

        public string set_dig_param(bool value)
        {
            string sv19 = "0"; string sv20 = "1"; // Y axis
            if (value) { sv19 = "1"; sv20 = "0"; } // X axis
            string msg1 = @" ""dio_20"":{""state"":[" + sv20 + "]}}}}}";
            string msg = @"{""message"":{""transmission_id"":[888],""op"":""update_sequence_parameters"",""parameters"":{""time_block_19"":{""dio_19"":{""state"":[" + sv19 +"]},"+ msg1;

            Send(msg);
            
            string rly = Receive();
            Thread.Sleep(1000);
            return rly;
        }

        public string axisControl(int axis, bool xy)
        {
             string a2 = axis.ToString();

            if (xy)
            {
                if (axis == 0) a2 = "1";
                else a2 = "0";
            }           
            string msg4 = @"{""message"":{""transmission_id"":[889],""op"":""switch_axis"",""parameters"":{""mode"":[" + a2 + "]}}}";
            Send(msg4);
            string rly = Receive();
            return rly;

        }
        
        /*
        public string axisControl(int axis)
        {
            // set_dig_param(19, 19, axis == 0);
            string sv19 = "0"; string sv20 = "1";
            if(axis == 0)
            { sv19 = "1"; sv20 = "0"; }
            string msg1 = @" ""dio_20"":{""state"":[" + sv20 + "]}}}";
            string msg2 = @"""op"":""update_sequence_parameters"",""parameters"":{""time_block_19"":{""dio_19"":{""state"":[" + sv19 +"]},"+ msg1;
            string msg4 = @"{""message"":{""transmission_id"":[889],""op"":""switch_axis"",""parameters"":{""mode"":[" + axis.ToString() + "]},"+ msg2 +"}}";
            //  string msg = @"{""message"":{""transmission_id"":[888],""op"":""update_sequence_parameters"",""parameters"":{""time_block_29"":{""dio_21"":{""status"":[1]}}}}}";

            //"dio_1":{		"state":[1]			} ,

            Send(msg4);
            string rly = Receive();
            return rly;
        }
        */

        private Dictionary<string, object> CreateSubParameterDict(string channel, object value)
        {

            Dictionary<string, object> stateDict = new Dictionary<string, object>();
            if (channel == "length" || channel == "multiplier")
            {
                stateDict[channel] = value;
            }
            else if (!(_dcsChannelNames.ContainsKey(channel) || _dcsChannelNames[channel].Contains("dio") ||
                _dcsChannelNames[channel].Contains("vga") || _dcsChannelNames[channel].Contains("ano")))
                throw new Exception(string.Format("Channel {0} not found in DCS channels", channel));
            
            else if (value is bool)
            {
               
                Dictionary<string, int> subParamDict = new Dictionary<string, int>();
                if ((bool)value) subParamDict["state"] = 1;
                else subParamDict["state"] = 0;
                stateDict[_dcsChannelNames[channel]] = subParamDict;
            }
            else
            {
                double val = Convert.ToDouble(value);
                Dictionary<string, double> subParamDict = new Dictionary<string, double>();
                if (_dcsChannelNames[channel].Contains("ano") && Utils.InRange(val, 0.0 ,7.0))
                    subParamDict["phase"] = val;
                else if (_dcsChannelNames[channel].Contains("vga") && Utils.InRange(val,-9.0,22.0))
                    subParamDict["amplitude"] = val;
                else throw new Exception(string.Format("Value {1} out of range for channel {0}", channel, value));

                stateDict[_dcsChannelNames[channel]] = subParamDict;
            }

            return stateDict;
        }

        private Dictionary<string, object> CreateSubParameterDict(string channel, double amplitude = 0.0,
            double frequency = 0.0)
        {
            Dictionary<string, object> stateDict = new Dictionary<string, object>();
            if (channel == "length" || channel == "multiplier")
            {
                stateDict[channel] = amplitude;
                return stateDict;
            }
            if (!_dcsChannelNames.ContainsKey(channel) || !_dcsChannelNames[channel].Contains("dds"))
                throw new Exception(string.Format("Channel {0} not found in DDS DCS channels", channel));

            if (frequency > 0.0 && (frequency < 1.0 || frequency > 250))
                throw new Exception(string.Format("Frequency for channel {0} is out of 1-250 MHz range", channel));
            if (amplitude > 0.0 && (amplitude < 0.0 || amplitude > 100.0))
                throw new Exception(string.Format("Amplitude for channel {0} is out of 0-100 % range", channel));

            Dictionary<string, double> subParamDict = new Dictionary<string, double>();
            if (frequency > 0.0) subParamDict["frequency"] = frequency;
            if (amplitude > 0.0) subParamDict["amplitude"] = amplitude;

            stateDict[_dcsChannelNames[channel]] = subParamDict;

            return stateDict;
        }
#endregion

        #region Pulse Helper Functions
        /// <summary>
        /// Configures any of the DDS channels for each Raman pulse
        /// </summary>
        /// <param name="RamanDir">X,Y,Z or Master</param>
        /// <param name="pulseNo">-1 = State Select 1, 0 = State Select 2 or 1,2,3 = Interferometer Pulse</param>
        /// <param name="length">time in units of "multiplier" or null if not changing</param>
        /// <param name="amplitude">amplitude in dB or null if not changing</param>
        /// <param name="multiplier">multipler in units of s or 1e-6 if not changing</param>
        /// <param name="phase">phase to set in dark time before pulse if pulseNo = 1,2,3 or null if not changing</param>
        /// <param name="state">set to true or false to turn on/off</param>
        public void ConfigurePulse(string RamanDir, int pulseNo, object length = null, object amplitude = null,
            double multiplier = 1e-6, object phase = null, bool state = true)
        {
            string AOMchannel = "AOM "+RamanDir;
            string RFchannel = "RF Switch "+RamanDir;
            
            //Phase is changed in the step before the pulse
            if (phase != null && pulseNo > 0)
            {
                string darkStep = _timeBlockNames["Dark " + pulseNo];

                Dictionary<string, object> phaseDict = CreateSubParameterDict("Phase Shift", phase);
                if (!_timeBlockDict.ContainsKey(darkStep))
                {
                    _timeBlockDict[darkStep] = new Dictionary<string, object>();
                    _timeBlockDict[darkStep].Add(phaseDict.Keys.First(), phaseDict.Values.First());
                }
            }

            string pulseStep;
            if (pulseNo == -1)
            {
                pulseStep = _timeBlockNames["State Selection 1"];
            }
            else if (pulseNo == 0)
            {
                pulseStep = _timeBlockNames["State Selection 2"];
            }
            else
            {
                pulseStep = _timeBlockNames["Pulse " + pulseNo];
            }
            if (!_timeBlockDict.ContainsKey(pulseStep)) _timeBlockDict[pulseStep] = new Dictionary<string,object>();
            if (amplitude != null)
            {
                Dictionary<string, object> pulseDict = CreateSubParameterDict(AOMchannel, amplitude);
                _timeBlockDict[pulseStep].Add(pulseDict.Keys.First(),pulseDict.Values.First());
            }
            if (length != null && !_timeBlockDict[pulseStep].ContainsKey("length")) _timeBlockDict[pulseStep].Add("length",Convert.ToDouble(length));
            if (multiplier != 1e-6 && !_timeBlockDict[pulseStep].ContainsKey("multiplier")) _timeBlockDict[pulseStep].Add("multiplier", (double)multiplier);

            //Sets the channel on/off as well
            Dictionary<string, object> rfDict = CreateSubParameterDict(RFchannel, state);
            _timeBlockDict[pulseStep].Add(rfDict.Keys.First(),rfDict.Values.First());
        }

        public void ConfigureIntTime(int timeID, double time)
        {
            Utils.EnsureRange(timeID, 1, 3);
           
            string darkStep = _timeBlockNames["Dark " + timeID];

            if (!_timeBlockDict.ContainsKey(darkStep))
            {
                _timeBlockDict[darkStep] = new Dictionary<string, object>();
               
            }
            if (!_timeBlockDict[darkStep].ContainsKey("length"))
            {
                _timeBlockDict[darkStep].Add("length", Convert.ToDouble(time));
                //Assumes this time will always be in ms
                _timeBlockDict[darkStep].Add("multiplier", 1e-3);
            }
            else throw new Exception("Multiple attempts to set Interferometer time at step " + timeID);
        }

#endregion

        public string PrintParametersToConsole()
        {
            string paramString = "";
           foreach (KeyValuePair<string,Dictionary<string,object>> timeBlock in _timeBlockDict)
           {
               paramString+= "Parameters for " + timeBlock.Key+"\n";
               foreach (KeyValuePair<string, object> entry in timeBlock.Value)
               {
                   string v = "";
                   if (entry.Value.GetType() == typeof(Dictionary<string,object>)) {
                       var val = entry.Value as Dictionary<string,object>;
                       v = string.Join(";", val.Select(x => x.Key + "=" + x.Value));
                   }
                   else
                   {
                       v = entry.Value.ToString();
                   }
                   paramString+="\t" + entry.Key + ": " + v+"\n";
               }
           }
           _timeBlockDict.Clear();
            return paramString;
        }

        public void StartFPGA()
        {
            FPGARun("Start", false);
        }
    }

    public class PLLException : Exception { public PLLException(string message) : base(message) { } }
 
}
