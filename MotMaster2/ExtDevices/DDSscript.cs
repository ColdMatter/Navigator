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
    public class DDS_script
    {
        public DDS_script(string[] _template)
        {
            AsList = new List<string>(_template);
        }
        public DDS_script(string __filename)
        {
            Open(__filename);
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
        const double freqStep = 232.83064E-3; // [Hz] -> frequency step
        const double degreeStep = 5.4933317E-3; // [deg]
        const int degreeOffset = 1073676288;
        const double amplFull = -2; // full amplitude [dBm]
        const double usStep = 1.024; // microsec step
        const double nsStep = 0.125; // nanosec step (high-res mode)

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
                string units = Utils.betweenStrings(pr.Item2, "[", "]");
                if (units.Equals(""))
                {
                    ErrorMng.errorMsg("No units in " + pr.Item2, 120); continue;
                }
                // rescale from user units to internal units                                              
                if ((units == "us") || (units == "ns"))  // time is decimal
                {
                    switch (units)
                    {
                        case "us":
                            fct = fct / usStep;
                            repl[key] = ((int)Math.Round(fct)).ToString();
                            break;
                        case "ns":
                            fct = fct / nsStep;
                            repl[key] = ((int)Math.Round(fct)).ToString() + "h";
                            break;
                    }
                    continue;
                }
                switch (units) // rescale to internal units
                {
                    case "Hz":
                        fct = fct / freqStep;
                        break;
                    case "kHz":
                        fct = 1E3 * fct / freqStep;
                        break;
                    case "MHz":
                        fct = 1E6 * fct / freqStep;
                        break;
                    case "deg":
                        fct = degreeOffset + fct / degreeStep;
                        break;
                    case "dBm":
                        double up = (fct - amplFull) / 20;
                        fct = Math.Pow(10, up) * (Math.Pow(2, 14) - 1);
                        break;
                }
                string sval = Convert.ToString(Convert.ToInt32(fct), 16);
                if (sval.Length > 8)
                {
                    ErrorMng.errorMsg("Hexadecimal too long -> " + sval, 121); continue;
                }
                string tval = new String('0', 8 - sval.Length);
                repl[key] = tval + sval; // all values must be of length 8
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
