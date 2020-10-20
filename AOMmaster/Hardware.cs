using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NationalInstruments.ModularInstruments.Interop;
using NationalInstruments.DAQmx;
using UtilsNS;

namespace AOMmaster
{
    class Hardware
    {
        private string dvcDIO;
        niHSDIO hsTaskIn, hsTaskOut;

        private string dvcAO;
        public double analogMin { get; private set;}
        public double analogMax { get; private set; }
        private bool hwSet = false;

        public string boolArr2string(bool[] bArray)
        {
            string rslt = "";
            for (int i = bArray.Length-1; i > -1 ; i--) // reverse the order for hsTaskOut.WriteStaticU32 - the least significant is the last
            {
                if (bArray[i])
                    rslt += '1';
                else
                    rslt += '0';
            }
            return rslt;
        }
        private uint BoolArrayToUInt(bool[] bArray)
        {
            string rsltStr = boolArr2string(bArray);
            return System.Convert.ToUInt32(rsltStr, 2);
        }
        public delegate void LogHandler(string txt, bool detail);
        public event LogHandler OnLog;

        protected void LogEvent(string txt, bool detail)
        {
            if (OnLog != null) OnLog(txt+" <hd>", detail);
        }

        public bool configHardware(string _dvcDIO, string _dvcAO, double _analogMin, double _analogMax) // if false look for hardwareSet; if true - do it
        {            
            // Digital In/Out
            dvcDIO = _dvcDIO;
            string ChannelList = "0,1,2,3,4,5,6,7";  
            analogMin = _analogMin; analogMax = _analogMax;          
            if ((dvcDIO == "") || Utils.TheosComputer()) return false;
            hsTaskIn = null; hsTaskOut = null;
            // read in
            hsTaskIn = niHSDIO.InitAcquisitionSession(dvcDIO, false, false, "");
            hsTaskIn.AssignStaticChannels(ChannelList);
            hsTaskIn.ConfigureDataVoltageLogicFamily(ChannelList, niHSDIOConstants._33vLogic);
            // write out
            hsTaskOut = niHSDIO.InitGenerationSession(dvcDIO, false, false, "");
            hsTaskOut.AssignStaticChannels(ChannelList);
            hsTaskOut.ConfigureDataVoltageLogicFamily(ChannelList, niHSDIOConstants._33vLogic);
            // Analog Out
            dvcAO = _dvcAO;            
            hwSet = true;
            return true;
        }
        #region digital hardware
        public bool ReadIn(out bool[] dt)
        {
            if (!hwSet) throw new Exception("Hardware was not configure");
            dt = new bool[32];
            uint dataRead;
            if (!Utils.isNull(hsTaskOut))
            {
                hsTaskIn.ReadStaticU32(out dataRead);
                var bitArray = new BitArray(BitConverter.GetBytes(dataRead));
                bitArray.CopyTo(dt, 0);
            }
            return true;
        }
        public bool WriteSingleOut(int chn, bool Value)
        {
            if (Utils.TheosComputer()) return true;
            bool[] dt = new bool[32]; bool[] mask = new bool[32];
            dt[chn] = Value;
            for (int i = 0; i < 32; i++) // normal order - the least significant is first
            {
                mask[i] = (i == chn);
            }
            bool bb = WriteOut(dt, mask);
            if (bb) LogEvent("Dgt# " + chn.ToString() + " val=" + Value.ToString(), true);
            else LogEvent("Error: DO #" + chn.ToString() + " val=" + Value.ToString(), true);
            return bb;
        }
        public bool WriteOut(bool[] dt, bool[] mask)
        {
            if (!hwSet) throw new Exception("Hardware was not configure");
            if ((dt.Length != 32) || (mask.Length != 32)) return false;
            uint dataOut =  BoolArrayToUInt(dt); uint uMask = BoolArrayToUInt(mask);
            if (!Utils.isNull(hsTaskOut)) hsTaskOut.WriteStaticU32(dataOut, uMask);  
            return true;
        }
        #endregion digital

        #region analog hardware
        public bool AnalogOut(int chn, double voltage)
        {
            if (Utils.TheosComputer()) return true;
            try
            {
                using (NationalInstruments.DAQmx.Task myTask = new NationalInstruments.DAQmx.Task()) // physicalChannel example /Dev2/ao2
                {
                    string physicalChannel = dvcAO + "/ao" + chn.ToString();
                    myTask.AOChannels.CreateVoltageChannel(physicalChannel, "", analogMin, analogMax, AOVoltageUnits.Volts);
                    AnalogSingleChannelWriter writer = new AnalogSingleChannelWriter(myTask.Stream);
                    double volts = Utils.EnsureRange(voltage, analogMin,analogMax);
                    writer.WriteSingleSample(true, volts);
                    LogEvent("Anl# " + chn.ToString() + "; val= " + volts.ToString("G5"), true);
                    return true;
                }
            }
            catch (DaqException ex)
            {
                LogEvent("Error: AO #" + chn.ToString() + ex.Message, true);
                return false;
            }
        }
        #endregion analog
    }
}
