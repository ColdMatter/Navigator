﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Windows.Controls;
using ErrorManager;
using UtilsNS;

namespace MOTMaster2.ExtDevices
{
    /// <summary>
    /// extName, fName, content
    /// </summary>
    public class DDS_factors: List<Tuple<string,string,string>> //
    {
        public DDS_factors()
        {

        }
        public DDS_factors(List<string> ls)
        {
            AsListOfString = ls;
        }

        public int AddFactor(string[] fct)
        {
            switch (fct.Length)
            {
                case 2:
                    this.Add(new Tuple<string, string, string>(fct[0], fct[1], ""));
                    break;
                case 3:
                    this.Add(new Tuple<string, string, string>(fct[0], fct[1], fct[2]));
                    break;
                default: 
                    {
                        ErrorMng.errorMsg("Wrong format factor", 444); return -1;
                    }
            }           
            return this.Count;
        }
        public int IdxFromName(string fName)
        {
            int j = -1;
            for (int i = 0; i < Count; i++)
            {
                if ((this[i].Item2).Equals(fName)) { j = i; break; }
            }
            return j;
        }
        public int IdxFromExtName(string extName)
        {
            int j = -1;
            for (int i = 0; i < Count; i++)
            {
                if ((this[i].Item1).Equals(extName)) { j = i; break; }
            }
            return j;
        }
        public string fNameFromExtName(string extName)
        {
            int j = IdxFromExtName(extName);
            if (j == -1) return "";
            return this[j].Item2;
        }
        public List<string> AsListOfString // without comments !
        {
            get
            {
                var ls = new List<string>();
                foreach (var itm in this)
                {
                    string ss = itm.Item1 + "=" + itm.Item2;
                    if (!itm.Item3.Equals("")) ss += "=" + itm.Item3;
                    ls.Add(ss);
                }
                return ls;
            }
            set
            {
                Clear(); 
                foreach (string ss in value)
                    if(!ss.Equals(""))
                        AddFactor(ss.Split('='));
            }
        }
    }
    public class DDS_units : List<Tuple<string, double, int>> // 0 size -> no size restrictions
    {
        const double freqStep = 232.83064E-3; // [Hz] -> frequency step
        const double usStep = 1.024; // microsec step
        const double nsStep = 0.125; // nanosec step (high-res mode)
        const double degreeSemiTurn = 32767; // 180 [deg]
        const double amplFull_dBm = -2; // full amplitude [dBm]
        const double amplFull_ampl = 16383; // full amplitude [ampl]
        public DDS_units()
        {
            // time
            AddUnit("ms", 1000 / usStep, 0);
            AddUnit("us", 1 / usStep, 0);
            AddUnit("ns", 1 / nsStep, 0);
            // frequency
            AddUnit("Hz", 1 / freqStep, 8);
            AddUnit("kHz", 1E3 / freqStep, 8);
            AddUnit("MHz", 1E6 / freqStep, 8);
            // amplitude [%] 0-100
            AddUnit("ampl", amplFull_ampl / 100, 4);
            // phase degree 0-360
            AddUnit("deg", degreeSemiTurn / 180, 4);
            // non-coeff conversion
            
            AddUnit("dBm", Double.NaN, 4);

        }
        public int AddUnit(string unit, double coeff, int size)
        {
            this.Add(new Tuple<string, double, int>(unit, coeff, size));
            return this.Count;
        }
        public int IdxFromUnit(string unit)
        {
            int j = -1;
            for (int i = 0; i < Count; i++)
            {
                if ((this[i].Item1).Equals(unit)) { j = i; break; }
            }
            return j;
        }

        public string replaceValueByUnit(string unit, double vl)
        {
            int j = IdxFromUnit(unit);
            if (j < 0)
            {
                ErrorMng.errorMsg("Undefined unit -> " + unit, 122); return "";
            }
            double fct = Double.NaN;
            if (Double.IsNaN(this[j].Item2)) 
            {
                switch (unit) // non-coeff units
                {
                     case "dBm":
                        double up = (vl - amplFull_dBm) / 20;
                        fct = Math.Pow(10, up) * (Math.Pow(2, 14) - 1);
                        break;
                }
            }
            else // coeff conversion
            {
                switch (unit) // time is decimal
                {
                    case "ms":                        
                    case "us":
                        return Convert.ToInt32(vl * this[j].Item2).ToString();
                    case "ns":
                        return Convert.ToInt32(vl * this[j].Item2).ToString() + "h";
                }
                fct = vl * this[j].Item2;
            }
            if (Double.IsNaN(fct))
            {
                ErrorMng.errorMsg("Wrong unit -> " + unit, 123); return "";
            }
            string sval = Convert.ToString(Convert.ToInt32(fct), 16);
            if (this[j].Item3 > 0) // if size-restricted
                if (sval.Length > this[j].Item3)
                {
                    ErrorMng.errorMsg("Hexadecimal too long -> " + sval, 121); return "";
                }
            return new String('0', this[j].Item3 - sval.Length) + sval;
        }
    }
    public class DDS_script
    {
        DDS_units units;
        private void SetUnits()
        {
            units = new DDS_units();
        }
        public DDS_script(string[] _template)
        {
            AsList = new List<string>(_template);
            SetUnits();
        }
        public DDS_script(string __filename)
        {
            Open(__filename);
            SetUnits();
        }
        // data core source
        protected void UpadeFromScript()
        {
            _scriptSection = Utils.readStructList(_AsList)["script"];
            var ls = Utils.skimRem(Utils.readStructList(_AsList)["factors"]);
            _factorsSection = new DDS_factors(ls);
        }
        public List<string> _AsList;
        public List<string> AsList 
        {
            get { return _AsList; }
            private set 
            {
                _AsList = new List<string>(value);
                UpadeFromScript();
            } 
        }
        public string[] AsArray
        {
            get { return AsList.ToArray(); }
            set { AsList = new List<string>(value); }
        }
        private List<string> _scriptSection;       
        public List<string> scriptSection
        {
            get { return _scriptSection; }
        }
        public DDS_factors _factorsSection;
        public DDS_factors factorsSection
        {
            get { return _factorsSection; } 
        }
        public List<string> ExtractFactors(bool select, out bool correct)
        {
            correct = true;
            List<string> rslt = new List<string>();
            if (scriptSection.Count == 0) return rslt;

            foreach (string line in scriptSection)
            {
                int j = 0;
                string ss = Utils.skimRem(line);
                if (ss.Length.Equals(0)) continue;               
                 
                while (ss.IndexOf('$', j + 1) > -1)
                {
                    int i = ss.IndexOf('$', j + 1);
                    if (i == -1) continue;
                    j = ss.IndexOf('$', i + 1);
                    if (j == -1)
                    {
                        ErrorMng.errorMsg("Missing closing $ in " + line, 123); correct = false;
                        break;
                    }
                    string fct = ss.Substring(i + 1, j - i - 1);
                    if (rslt.IndexOf(fct) > -1) continue;
                    if (fct.IndexOf("select-") == 0)
                    {
                        if (select) rslt.Add(fct);
                    }
                    else
                    {
                        if (!select) rslt.Add(fct);
                    }
                }
            }
            return rslt;
        }
        public DDS_script clone() { return new DDS_script(AsArray); }
        public void GetFromTextBox(TextBox textBox)
        {
            _AsList.Clear();
            int lineCount = textBox.LineCount;
            for (int line = 0; line < lineCount; line++)
            {
                string ss = textBox.GetLineText(line);
                ss = ss.Replace("\n", "");
                _AsList.Add(ss.Replace("\r", ""));
            }
            UpadeFromScript();
        }
        public void SetToTextBox(TextBox textBox)
        {
            textBox.Text = "";
            foreach (string line in _AsList)
            {
                textBox.Text += line + "\r";
            }
        }
        private string _filename;
        public string filename { get { return _filename; } }
        public bool Open(string fn)
        {
            if (!File.Exists(fn)) return false;
            _filename = fn;
            AsArray = File.ReadAllLines(fn);
            return true;
        }
        public void Save(string fn = "")
        {
            if (!fn.Equals("")) _filename = fn;
            if (filename.Equals(""))
            {
                ErrorMng.errorMsg("Missing filename", 223); return;
            }
            File.WriteAllLines(filename, AsArray);
        }

        public Dictionary<string, string> replacements(FactorsUC ucExtFactors, Dictionary<string, string> OtherFactors) 
        {
            Dictionary<string, string> repl = new Dictionary<string, string>(OtherFactors);
            foreach (var pr in factorsSection) // over ext.names
            {
                string key = Convert.ToString(pr.Item1); double fct = Double.NaN;
                int j = factorsSection.IdxFromExtName(key);
                if (j == -1)
                {
                    ErrorMng.errorMsg("Missing value of factor <" + key + "> ", 118); continue;
                }
                if (!factorsSection[j].Item3.Equals("")) fct = Convert.ToDouble(factorsSection[j].Item3); // factor-constant
                else // factor-variable
                {
                    int k = ucExtFactors.IdxFromName(key, true); // ext.name
                    if (k == -1) continue; // the list of factors is larger the number of factors in script
                    if (!ucExtFactors.Factors[k].Enabled) continue; //the invisible/disabled factors are out
                    fct = ucExtFactors.Factors[k].getReqValue(); // default NaN
                }
                if (Double.IsNaN(fct))
                {
                    ErrorMng.errorMsg("Missing value of factor <" + key + "> ", 119); continue;
                }
                string unit = Utils.betweenStrings(pr.Item2, "[", "]");
                if (unit.Equals(""))
                {
                    ErrorMng.errorMsg("No units in " + pr.Item2, 120); continue;
                }
                // rescale from user units to internal units
                string ss = units.replaceValueByUnit(unit, fct);
                if (ss.Equals("")) continue;
                repl[key] = ss;             
            }
            return repl;
        }
        public List<string> actualScript(Dictionary<string, string> fcts)
        {
            List<string> ls = new List<string>();
            foreach (string line in scriptSection)
            {
                if (line.Length > 0)
                    if (line[0] == '#') continue;
                string ss = Utils.skimRem(line);
                foreach (var fct in fcts)
                {
                    string fctName = '$' + fct.Key + '$';
                    if (line.IndexOf(fctName) > -1) ss = ss.Replace(fctName, fct.Value);
                }
                if (ss.IndexOf('$') > -1)
                {
                    ErrorMng.errorMsg("Missing factor in " + line, 323); continue;
                }
                ls.Add(ss);
            }
            return ls;
        }

        public string actualScriptAsString(Dictionary<string, string> fcts)
        {
            List<string> ls = actualScript(fcts);
            string script = "";
            foreach (string line in ls)
            {
                script += line + "\r";
            }
            return script;
        }
    }
}
