using System;
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
    /// extName, fName (desc [unit]), content
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
        public bool allowNoUnit = false;

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
            // decimal
            AddUnit("dec", Double.NaN, 0);
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
            if (unit.Equals("") && allowNoUnit) return Convert.ToString(vl);
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
            string sval = "";
            try
            {
                sval = Convert.ToString(Convert.ToInt32(fct), 16);
            }   
            catch (OverflowException e)
            {
                ErrorMng.errorMsg("Overflow -> " + e.Message + " for " + fct, 120);
            }    
            if (this[j].Item3 > 0) // if size-restricted
                if (sval.Length > this[j].Item3)
                {
                    ErrorMng.errorMsg("Hexadecimal too long -> " + sval, 121); return "";
                }
            return new String('0', this[j].Item3 - sval.Length) + sval;
        }
    }
    public class MetaDDS
    {
        private Dictionary<string, List<string>> metaCmds;
        private DDS_units units;
        public MetaDDS(ref DDS_units _units)
        {
            units = _units;
            metaCmds = Utils.readStructList(Utils.configPath + "MetaDDS.txt");
        }

        public List<string> meta2Script(string meta, Dictionary<string, string> fcts)
        {
            List<string> ls = new List<string>();
            if (meta.IndexOf('(') == -1)
            {
                ErrorMng.errorMsg("( is missing", 456); return ls;
            }
            // no arguments
            foreach (var cmd in metaCmds)
            {
                if (meta.Equals(cmd.Key))
                {
                    ls.AddRange(cmd.Value);
                    return ls;
                }
            }
            // with arguments
            string[] mtCmd = meta.Split('('); string[] mtArgs = (mtCmd[1].Remove(mtCmd[1].Length-1)).Split(','); // in the script          
            foreach (var cmd in metaCmds)
            {
                string[] mtList = cmd.Key.Split('('); if (mtList[1].Equals(')')) continue; // no arguments
                string[] mtPrms = (mtList[1].Remove(mtList[1].Length - 1)).Split(','); // from MetaDDS file
                if (mtCmd[0].Equals(mtList[0])) // match arg with prm
                {
                    // replace mtPrms with mtArgs from cmd.Value
                    foreach (string line in cmd.Value)
                    {

                    }
                }
            }
            return ls;
        }
    }

    public class DDS_script
    {
        public DDS_units units;
        private MetaDDS metaDDS;
        private void SetUnits()
        {
            units = new DDS_units();
        }
        public DDS_script(string[] _template)
        {
            AsList = new List<string>(_template);
            SetUnits();
            metaDDS = new MetaDDS(ref units); 
        }
        public DDS_script(string __filename)
        {
            Open(__filename);
            SetUnits();
            metaDDS = new MetaDDS(ref units);
        }
        // data core source
        protected void UpdateFromScript()
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
                UpdateFromScript();
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
            List<string> sf = new List<string>(); // factors in the script
            if (scriptSection.Count == 0) return sf;

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
                    if (sf.IndexOf(fct) > -1) continue;
                    if (fct.IndexOf("select-") == 0)
                    {
                        if (select) sf.Add(fct);
                    }
                    else
                    {
                        if (!select) sf.Add(fct);
                    }
                }
            }
            List<string> rslt;
            if (select) rslt = new List<string>(sf);
            else
            {
                rslt = new List<string>();
                foreach (var fct in factorsSection)
                {
                    if (factorsSection.IdxFromExtName(fct.Item3) > -1) // resulting field is a factor
                    {
                        if (rslt.IndexOf(fct.Item3) == -1)                    
                            ErrorMng.errorMsg(fct.Item3 + " is not recognized as a factor.", -134); 
                        continue;                                           
                    }
                    if (sf.IndexOf(fct.Item1) > -1) rslt.Add(fct.Item1);               
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
            UpdateFromScript();
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
        public Dictionary<string, string> replacements(FactorsUC ucExtFactors, Dictionary<string, string> OtherFactors = null) 
        {
            Dictionary<string, string> repl; 
            if (Utils.isNull(OtherFactors)) repl = new Dictionary<string, string>();
            else repl = new Dictionary<string, string>(OtherFactors);
            Dictionary<string, double> vals = new Dictionary<string, double>();
            foreach (var pr in factorsSection) // over ext.names
            {
                string key = Convert.ToString(pr.Item1); double fct = Double.NaN;
                int j = factorsSection.IdxFromExtName(key);
                if (j == -1)
                {
                    ErrorMng.errorMsg("Missing value of factor <" + key + "> ", 118); continue;
                }
                if (!factorsSection[j].Item3.Equals("")) 
                {                   
                    if (!double.TryParse(factorsSection[j].Item3, out fct)) // factor-constant
                        fct = vals[factorsSection[j].Item3];                                                                    
                }
                else // open factor
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
                bool isBrackets = (pr.Item2.IndexOf("[") > 0) && (pr.Item2.IndexOf("]") > 0);
                string unit = "";
                if (isBrackets) unit = Utils.betweenStrings(pr.Item2, "[", "]");                 
                if (!units.allowNoUnit && unit.Equals(""))
                {
                    ErrorMng.errorMsg("No units in " + pr.Item2, 120); continue;
                }
                // rescale from user units to internal units
                vals[key] = fct;
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
                {               
                    if (line[0] == '#') continue;
                    if (line[0] == '@')
                    {
                        ls.AddRange(metaDDS.meta2Script(line.Substring(1), fcts));                   
                        continue;
                    }
                }
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
