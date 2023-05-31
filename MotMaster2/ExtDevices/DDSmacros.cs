using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ErrorManager;
using UtilsNS;

namespace MOTMaster2.ExtDevices
{
    public class DDSmacros
    {    
        private Dictionary<string, List<string>> extCmds;
        private Dictionary<string, List<string>> intCmds;
        private DDS_units units;
        public DDSmacros(ref DDS_units _units)
        {
            units = _units;
            extCmds = Utils.readStructList(Utils.configPath + "MetaDDS.txt");
            intCmds = new Dictionary<string, List<string>>();

            // chn->0/1/2 (2-both); mode - 1->Ampl; 2->Freq; 3->Phase[rad]; 4->Phase[deg]
            intCmds["setRamp"] = new List<string>() { "chn", "mode", "rate", "duration", "range" };
            // rate - in AD9910 units; duration - in sec; range - by mode units
            intCmds["setRamp2"] = new List<string>() { "chn", "mode", "rateUp", "durationUp", "rangeUp", "rateDown", "durationDown", "rangeDown" };
            // rateUp/Down - in AD9910 units; durationUp/Down - [sec]; rangeUp/Down - by mode units

            intCmds["rampFreq"] = new List<string>() { "chn", "smooth", "freqStart", "durationUp", "freqMiddle", "durationDown" };
            // smooth - 1-10; duration* [sec]; freq* [MHz]
            intCmds["rampAmpl"] = new List<string>() { "chn", "smooth", "amplStart", "durationUp", "amplMiddle", "durationDown" };
            // smooth - 1-10; duration* [sec]; ampl* [ampl]
            intCmds["rampdBm"] = new List<string>() { "chn", "smooth", "amplStart", "durationUp", "amplMiddle", "durationDown" };
            // smooth - 1-10; duration* [sec]; ampl* [dBm]
            intCmds["rampPhase"] = new List<string>() { "chn", "smooth", "phaseStart", "durationUp", "phaseMiddle" , "durationDown" };
            // smooth - 1-10; duration* [sec]; phase* [deg]
        }
        private Dictionary<string, double> intAgrs;
        public List<string> meta2Script(string meta)
        {
            List<string> ls = new List<string>();
            if (meta.IndexOf('(') == -1)
            {
                ErrorMng.errorMsg("( is missing", 456); return ls;
            }
            // external meta command, no arguments
            foreach (var icmd in extCmds)
            {
                if (meta.Equals(icmd.Key))
                {
                    ls.AddRange(icmd.Value);
                    return ls;
                }
            }
            // internal meta commands 
            string[] mtCmd = meta.Split('('); // in the script
            if (mtCmd.Length != 2)
            {
                ErrorMng.errorMsg("Syntax error in meta command -> " + mtCmd, 129); return ls;
            }
            string cmd = mtCmd[0];
            if (!intCmds.ContainsKey(cmd))
            {
                ErrorMng.errorMsg("Undefined meta command -> " + mtCmd[0], 122); return ls;
            }
            string[] mtArgs = (mtCmd[1].Remove(mtCmd[1].Length - 1)).Split(',');
            if (intCmds[cmd].Count != mtArgs.Length)
            {
                ErrorMng.errorMsg("Wrong number of arguments in meta command -> " + mtCmd[0], 132); return ls;
            }
            int k = 0; intAgrs = new Dictionary<string, double>();
            foreach (var arg in mtArgs)
            {
                double d;
                if (Double.TryParse(arg, out d))
                {
                    string argName = intCmds[cmd][k];
                    intAgrs[argName] = d;
                }
                else
                {
                    ErrorMng.errorMsg("Syntax error in meta command -> " + mtCmd[0], 122); return ls;
                }
                k++;
            }

            MethodInfo mi = this.GetType().GetMethod(cmd);

            object rslt = mi.Invoke(this, null);
            if (!Utils.isNull(rslt))
            {
                List<string> lt = rslt as List<string>;
                ls.AddRange(lt);
            }
            return ls;
        }

        private int calcRampStep(int mode, int rate, double duration, double range)
        {
            double K = Double.NaN;
            switch (mode)
            {
                case 1: // amplitude [ampl] 
                    K = 20 / Math.Pow(2, 32);
                    break;
                case 2: // frequency [Hz]
                    K = 1E9 / Math.Pow(2, 32);
                    break;
                case 3: // phase [rad]
                    K = Math.PI / Math.Pow(2, 31);
                    break;
                case 4: // phase [deg]
                    K = 45 / Math.Pow(2, 29);
                    break;
                default:
                    ErrorMng.errorMsg("Wrong mode in meta command ", 142);
                    return -1;
            }
            double Rs = rate / SysClk; // rate [s]
            int NS = Convert.ToInt32(duration / Rs); // number of steps
            double phys_step = range / NS; // physical step
            return Convert.ToInt32(phys_step / K); // convert to register step
        }

        public object setRamp()
        {
            List<string> ls = new List<string>();
            if (Utils.isNull(intAgrs)) return ls;
            if (intAgrs.Count != 5) return ls;

            string sChn = "";
            if (Utils.InRange(Convert.ToInt32(intAgrs["chn"]), 0, 1)) sChn = Convert.ToInt32(intAgrs["chn"]).ToString();
            // rate
            int rate = Convert.ToInt32(intAgrs["rate"]);
            ls.Add("dcp " + sChn + " spi:DRR = 0x" + DCPutils.dbl2hex(rate, 4) + DCPutils.dbl2hex(rate, 4));
            // step
            int step = calcRampStep(Convert.ToInt32(intAgrs["mode"]), rate, intAgrs["duration"], intAgrs["range"]);
            if (step < 0)
            {
                ErrorMng.errorMsg("Unable to calculate step in meta command <rampStep>", 142);
                return ls;
            }
            ls.Add("dcp " + sChn + " spi:DRSS=0x" + DCPutils.dbl2hex(step, 8) + DCPutils.dbl2hex(step, 8));
            return ls;
        }
        public object setRamp2()
        {
            List<string> ls = new List<string>();
            if (Utils.isNull(intAgrs)) return ls;
            if (intAgrs.Count != 8) return ls;

            string sChn = "";
            if (Utils.InRange(Convert.ToInt32(intAgrs["chn"]), 0, 1)) sChn = Convert.ToInt32(intAgrs["chn"]).ToString();
            // rate
            int rateDown = Convert.ToInt32(intAgrs["rateDown"]); int rateUp = Convert.ToInt32(intAgrs["rateUp"]);
            ls.Add("dcp " + sChn + " spi:DRR=0x" + DCPutils.dbl2hex(rateDown, 4) + DCPutils.dbl2hex(rateUp, 4));
            // step
            int stepDown = calcRampStep(Convert.ToInt32(intAgrs["mode"]), Convert.ToInt32(intAgrs["rateDown"]), intAgrs["durationDown"], intAgrs["rangeDown"]);
            int stepUp = calcRampStep(Convert.ToInt32(intAgrs["mode"]), Convert.ToInt32(intAgrs["rateUp"]), intAgrs["durationUp"], intAgrs["rangeUp"]);
            if (stepUp < 0 || stepDown < 0)
            {
                ErrorMng.errorMsg("Unable to calculate step in meta command <rampStep2>", 142);
                return ls;
            }
            ls.Add("dcp " + sChn + " spi:DRSS=0x" + DCPutils.dbl2hex(stepDown, 8) + DCPutils.dbl2hex(stepUp, 8));
            return ls;
        }
        private int rateFromSmooth()
        {
            double smth = intAgrs["smooth"]; int nSteps = 1;
            if (smth >= 0) // exponential - typically from 1 to 4
            {
                smth = Utils.EnsureRange(smth, 0, 10);
                nSteps = Convert.ToInt32(1E4*Math.Exp(smth));
            }
            else // linear
            {
                // number of steps
                smth = Utils.EnsureRange(Math.Abs(smth), 0, 10);
                nSteps = Convert.ToInt32(smth * 20e6 + 4000); // steps per sec 
            }
            double stepSize = 1 / (double)nSteps; // [sec]
            return Convert.ToInt32(stepSize*SysClk); 
        } 

        private double SysClk = 1E9/4; // [Hz]
        // intCmds["rampFreq"] = new List<string>() { "chn", "smooth", "freqStart", "durationUp", "freqMiddle" , "durationDown" };
        // smooth - 0-10; nSteps/s = smooth * 20e6 + 4000; duration [sec]; freq* [MHz]
        public object rampFreq()
        {
            List<string> ls = new List<string>();
            if (Utils.isNull(intAgrs)) return ls;
            if (intAgrs.Count != 6) return ls;
            // channel
            string sChn = "";
            if (Utils.InRange(Convert.ToInt32(intAgrs["chn"]), 0, 1)) sChn = Convert.ToInt32(intAgrs["chn"]).ToString() + " ";

            // frequences
            double freqStart = Convert.ToInt32(intAgrs["freqStart"]); // in internal units
            double freqMiddle = Convert.ToInt32(intAgrs["freqMiddle"]); 
            ls.Add("dcp " + sChn + "spi:DRL=0x" + units.replaceValueByUnit("MHz", freqMiddle) + units.replaceValueByUnit("MHz",freqStart));

            // rate
            int rate = rateFromSmooth();
            ls.Add("dcp " + sChn + "spi:DRR=0x" + DCPutils.dbl2hex(rate, 4) + DCPutils.dbl2hex(rate, 4));

            // step
            double freqRange = Math.Abs(freqMiddle - freqStart) * 1E6; // freq change [Hz]
            int stepDown = calcRampStep(2, rate, intAgrs["durationDown"], freqRange); 
            int stepUp = calcRampStep(2, rate, intAgrs["durationUp"], freqRange); 
            if (stepUp < 0 || stepDown < 0)
            {
                ErrorMng.errorMsg("Unable to calculate step in macro-command <rampFreq>", 143);
                return ls;
            }
            ls.Add("dcp " + sChn + "spi:DRSS=0x" + DCPutils.dbl2hex(stepDown, 8) + DCPutils.dbl2hex(stepUp, 8));
            //ls.Add("dcp " + sChn + "spi:CFR2=0x80080");
            return ls;
        }
        // intCmds["rampAmpl"] = new List<string>() { "chn", "smooth", "amplStart", "durationUp", "amplMiddle" , "durationDown" };
        // smooth - 1-10; duration [sec]; ampl* [ampl]
        public object rampAmpl()
        {
            List<string> ls = new List<string>();
            if (Utils.isNull(intAgrs)) return ls;
            if (intAgrs.Count != 6) return ls;
            // channel
            string sChn = "";
            if (Utils.InRange(Convert.ToInt32(intAgrs["chn"]), 0, 1)) sChn = Convert.ToInt32(intAgrs["chn"]).ToString() + " ";
            // amplitudes            
            double coeff = 16383 / 100.0;
            double amplStart = Utils.EnsureRange(intAgrs["amplStart"], 0,100); double amplMiddle = Utils.EnsureRange(intAgrs["amplMiddle"], 0, 100);
            ls.Add("dcp " + sChn + "spi:DRL=0x" + DCPutils.dbl2hex(amplMiddle * coeff, 4)+ "0000" + DCPutils.dbl2hex(amplStart * coeff, 4) + "0000");
            // rate
            int rate = rateFromSmooth();
            ls.Add("dcp " + sChn + "spi:DRR=0x" + DCPutils.dbl2hex(rate, 4) + DCPutils.dbl2hex(rate, 4));
            // step
            int amplRange = Convert.ToInt32(Math.Abs(amplMiddle - amplStart));
            int stepDown = calcRampStep(1, rate, intAgrs["durationDown"], amplRange);
            int stepUp = calcRampStep(1, rate, intAgrs["durationUp"], amplRange);
            if (stepUp < 0 || stepDown < 0)
            {
                ErrorMng.errorMsg("Unable to calculate step in meta command <rampStep2>", 142);
                return ls;
            }
            ls.Add("dcp " + sChn + "spi:DRSS=0x" + DCPutils.dbl2hex(stepDown, 8) + DCPutils.dbl2hex(stepUp, 8));
            //ls.Add("dcp " + sChn + " spi:CFR2=0x01280080");
            return ls;
        }

        // intCmds["rampdBm"] = new List<string>() { "chn", "smooth", "amplStart", "durationUp", "amplMiddle" , "durationDown" };
        // smooth - 1-10; duration [sec]; ampl* [dBm]
        public object rampdBm()
        {
            double dBm(double val)
            {
                return ((Math.Pow(10, (val - 2) / 20))* 16383)*4;
            }
            List<string> ls = new List<string>();
            if (Utils.isNull(intAgrs)) return ls;
            if (intAgrs.Count != 6) return ls;
            // channel
            string sChn = "";
            if (Utils.InRange(Convert.ToInt32(intAgrs["chn"]), 0, 1)) sChn = Convert.ToInt32(intAgrs["chn"]).ToString() + " ";
            // amplitudes
            double amplStart = intAgrs["amplStart"]; double amplMiddle = intAgrs["amplMiddle"];
            ls.Add("dcp " + sChn + "spi:DRL=0x" + DCPutils.dbl2hex(dBm(amplMiddle), 4) + "0000" + DCPutils.dbl2hex(dBm(amplStart), 4) + "0000");
            // rate
            int rate = rateFromSmooth();
            ls.Add("dcp " + sChn + "spi:DRR=0x" + DCPutils.dbl2hex(rate, 4) + DCPutils.dbl2hex(rate, 4));
            // step
            int amplRange = Convert.ToInt32(Math.Abs(amplMiddle - amplStart));
            int stepDown = calcRampStep(1, rate, intAgrs["durationDown"], amplRange);
            int stepUp = calcRampStep(1, rate, intAgrs["durationUp"], amplRange);
            if (stepUp < 0 || stepDown < 0)
            {
                ErrorMng.errorMsg("Unable to calculate step in meta command <rampStep2>", 142);
                return ls;
            }
            ls.Add("dcp " + sChn + "spi:DRSS=0x" + DCPutils.dbl2hex(stepDown, 8) + DCPutils.dbl2hex(stepUp, 8));
            //ls.Add("dcp " + sChn + " spi:CFR2=0x01280080");
            return ls;
        }
        // intCmds["rampPhase"] = new List<string>() { "chn", "smooth", "phaseStart", "durationUp", "phaseMiddle", "durationDown" };
        // smooth - 1-10; duration [sec]; phase* [deg]
        public object rampPhase()
        {
            List<string> ls = new List<string>();
            if (Utils.isNull(intAgrs)) return ls;
            if (intAgrs.Count != 6) return ls;

            string sChn = "";
            if (Utils.InRange(Convert.ToInt32(intAgrs["chn"]), 0, 1)) sChn = Convert.ToInt32(intAgrs["chn"]).ToString() + " ";
            // phases
            long halfTurn = (long)2147483647; // 180 deg from example p22
            double coeff = halfTurn / 180.0;
            double phaseStart = Convert.ToInt32(intAgrs["phaseStart"]); double phaseMiddle = Convert.ToInt32(intAgrs["phaseMiddle"]);
            ls.Add("dcp " + sChn + "spi:DRL = 0x" + DCPutils.dbl2hex(phaseMiddle * coeff, 8) + DCPutils.dbl2hex(phaseStart * coeff, 8));
            // rate
            int rate = rateFromSmooth();
            ls.Add("dcp " + sChn + "spi:DRR=0x" + DCPutils.dbl2hex(rate, 4) + DCPutils.dbl2hex(rate, 4));
            // step
            int phaseRange = Convert.ToInt32(Math.Abs(phaseMiddle - phaseStart));
            int stepDown = calcRampStep(4, rate, intAgrs["durationDown"], phaseRange);
            int stepUp = calcRampStep(4, rate, intAgrs["durationUp"], phaseRange);
            if (stepUp < 0 || stepDown < 0)
            {
                ErrorMng.errorMsg("Unable to calculate step in meta command <rampStep2>", 142);
                return ls;
            }
            ls.Add("dcp " + sChn + "spi:DRSS=0x" + DCPutils.dbl2hex(stepDown, 8) + DCPutils.dbl2hex(stepUp, 8));
            //ls.Add("dcp " + sChn + " spi:CFR2 = 0x1180080");
            return ls;
        }
    }

}
