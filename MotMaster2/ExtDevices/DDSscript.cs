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
        public List<string> AsList { get; private set; }
        public string[] AsArray
        {
            get { return AsList.ToArray(); }
            set { AsList = new List<string>(value); }
        }
        public List<string> scriptSection
        {
            get { return Utils.readStructList(AsList)["script"]; }
        }
        public OrderedDictionary factorsSection
        {
            get
            {
                OrderedDictionary odict = new OrderedDictionary();
                var readDict = Utils.readStructList(AsList);
                List<string> ls = readDict["factors"];
                foreach (string ss in ls)
                {
                    if (ss.Equals("")) continue;
                    if (ss[0].Equals('#') || !ss.Contains("=")) continue;

                    string[] sb = ss.Split('='); // extName = name
                    odict[sb[0]] = Utils.skimRem(sb[1]);
                }
                return odict;
            }
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
            AsList.Clear();
            int lineCount = textBox.LineCount;
            for (int line = 0; line < lineCount; line++)
            {
                string ss = textBox.GetLineText(line);
                ss = ss.Replace("\n", "");
                AsList.Add(ss.Replace("\r", ""));
            }
        }
        public void SetToTextBox(TextBox textBox)
        {
            textBox.Text = "";
            foreach (string line in AsList)
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
            foreach (var pr in factorsSection.Keys) // over ext.names
            {
                string key = Convert.ToString(pr);
                int k = ucExtFactors.IdxFromName(key, true); // ext.name
                if (k == -1) continue; // the list of factors is larger the number of factors in script
                if (!ucExtFactors.Factors[k].Enabled) continue; //the invisible/disabled factors are out
                double fct = ucExtFactors.Factors[k].getReqValue(); // default NaN
                if (Double.IsNaN(fct))
                {
                    ErrorMng.errorMsg("Missing value of factor <" + key + "> ", 119); continue;
                }
                string fnm = ucExtFactors.Factors[k].fName;
                string units = Utils.betweenStrings(fnm, "[", "]");
                if (units.Equals(""))
                {
                    ErrorMng.errorMsg("No units in " + ucExtFactors.Factors[k].fName, 120); continue;
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
