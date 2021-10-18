using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.ComponentModel;
using NationalInstruments.Analysis.Math;
using Newtonsoft.Json;
using dotMath;
using UtilsNS;

namespace MOTMaster2.SequenceData
{
    //A parameter class which adds more functionality to the parameter dictionary used in a MOTMasterScript
    public class Parameter : TypeConverter,INotifyPropertyChanged 
    {
        public string Name { get; set; }
        private object _value;
        public object Value 
        {
            get 
            { 
                if (IsScannable()) return _value;
                else return CompileParameter(Description);
            }
            set {_value = value;}
        }
        public string Description { get; set; }
        public bool IsLaser { get; set; }
        public bool Read_Only { get; set; }
        //Flags if the variable is used to modify a sequence
        public bool SequenceVariable { get; set; }
        
        public bool IsScannable() 
        {
            //if (Read_Only) return false; 
            if (Description == "" || Description == null) return true;
            return !Description[0].Equals('=');
        }

        public string Types {
            get 
            { 
                string tp = "";
                if (IsLaser) tp += "<laser>";
                if (Read_Only) tp += "<readonly>";
                if (SequenceVariable) tp += "<seq>";
                return tp;  
            } 
        }      

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { PropertyChanged += value; }
            remove { PropertyChanged -= value; }
        }

        protected virtual event PropertyChangedEventHandler PropertyChanged;
        public Parameter()
        {
            //TODO Check this doesn't cause problems if the parameter needs to be a double
            Name = "";
            Value = 0.0;
            Description = "";
            IsLaser = false;
            SequenceVariable = true;
            Read_Only = false;
        }
        public Parameter(string name, string description, object value, bool isLaser = false, bool sequenceVar = true, bool readOnly = false)
        {
            Name = name;
            Value = value;
            Description = description;
            IsLaser = isLaser;
            SequenceVariable = sequenceVar;
            Read_Only = readOnly;
        }
        public Parameter Copy()
        {
            Parameter newParam = new Parameter(this.Name, this.Description, this.Value, this.IsLaser, this.SequenceVariable, this.Read_Only);
            return newParam;
        }

        //Equality is only defined if two parameters have the same name. This is to make it easier for overriding them when loading a new sequence
        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(Parameter))
            {
                Parameter param = obj as Parameter;
                if (param.Name == this.Name && param.Value.ToString() == this.Value.ToString() && param.SequenceVariable == this.SequenceVariable)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return base.Equals(obj);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value is int || value is double || value is string) return new Parameter("", "", value);
            else return null;
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType) 
        {
            Parameter vl = (Parameter) value;
            if (destinationType == typeof(double)) return (double)vl.Value;
            if (destinationType == typeof(int)) return (int)vl.Value;
            else return null;
        }

        public double CompileParameter(string function)
        {
            if (Utils.isNull(Controller.sequenceData)) return Double.NaN;
            if (Utils.isNull(Controller.sequenceData.Parameters)) return Double.NaN;
            string func = function.TrimStart('=');
            EqCompiler compiler = new EqCompiler(func, true);
            CustomCompiler.AddFunctions(compiler);   
            compiler.Compile();

            //Checks all variables to use values in parameter dictionary
            foreach (string variable in compiler.GetVariableList())
            {
                //if (Controller.sequenceData.Parameters[variable].Read_Only) continue;
                if (Controller.sequenceData.Parameters.Keys.Contains(variable))
                {
                    compiler.SetVariable(variable, Convert.ToDouble(Controller.sequenceData.Parameters[variable].Value));
                }
                else throw new Exception(string.Format("Variable {0} not found in parameters.", variable));
                if(!Controller.sequenceData.Parameters[variable].IsScannable())
                    throw new Exception(string.Format("Variable {0} is derivative (non-scannable) - not allowed!", variable));
            }
            return compiler.Calculate();
        }
    }
    #region CustomizeCompiler
    public class calV2Deg : EqCompiler.CFunction
    {
        /// <summary>
        /// An array of values associated with the function.
        /// </summary>
        ArrayList m_alValues;
        double[] vData, degData;

        public calV2Deg()
        {
        vData = new double[131];
            for (int i = 0; i < 131; i++)
            {
                vData[i] = (double)i / 10.0;
            }                
            degData = new double[] // n=131         
            { 0.000, 9.228, 18.458, 26.806, 35.596, 44.166, 52.514, 60.424, 68.334, 75.806, 83.056, 89.648, 96.240, 102.172, 107.666, 112.940, 117.774, 122.828, 127.222, 131.396, // 20
            135.572, 139.746, 143.482, 147.220, 150.952, 154.468, 158.204, 161.500, 165.234, 168.750, 172.266, 176.440, 179.516, 182.812, 186.108, 189.624, 192.920, 195.996, 199.512,  
            203.248, 206.762, 210.280, 214.014, 217.750, 221.264, 225.220, 228.956, 232.910, 236.646, 240.820, 244.556, 248.292, 252.466, 256.202, 260.156, 264.112, 267.846, 271.582,
            275.538, 279.052, 282.568, 286.304, 290.260, 293.774, 297.070, 300.586, 304.102, 307.398, 310.474, 313.990, 317.286, 320.362, 323.438, 326.514, 329.590, 332.886, 335.742,
            338.818, 341.456, 344.312, 347.388, 350.244, 353.100, 355.738, 358.374, 361.230, 364.086, 366.944, 369.580, 372.216, 375.074, 377.710, 380.346, 383.422, 385.620, 388.038,
            390.454, 393.090, 395.728, 398.144, 400.782, 403.198, 405.616, 408.032, 410.450, 412.866, 414.842, 417.260, 419.238, 421.656, 423.852, 425.830, 427.808, 429.566, 431.542, // 5x19
            433.300, 435.058, 436.596, 438.134, 439.672, 441.210, 442.530, 443.848, 445.166, 446.264, 447.364, 448.242, 449.560, 450.220, 450.878, 451.758 }; // 16
        }

        public double calibrV2Deg(double V)
        {
            if (!Utils.InRange(V, vData[0], vData[vData.Length - 1])) throw new Exception("Out of range voltage -> " + V.ToString("G6"));

            double[] secondDerivatives;
            double initialBoundary, finalBoundary;
            Random rnd = new Random();

            // Causes SplineInterpolant method to set the initial boundary condition for a natural spine
            initialBoundary = 1.00E+30;

            // Causes SplineInterpolant method to set the final boundary condition for a natural spine
            finalBoundary = 1.00E+30;           

            // Calculate secondDerivatives
            secondDerivatives = CurveFit.SplineInterpolant(vData, degData, initialBoundary, finalBoundary);

            // Calculate spline interpolated value  
            return CurveFit.SplineInterpolation(vData, degData, secondDerivatives, V);
        }

        /// <summary>
        /// (class).CreateInstance returns an instance of the CAbs object
        /// with the passed CValue object(s).
        /// </summary>
        /// <param name="alValues">An arraylist of values passed by the compiler.</param>
        /// <returns></returns>
        public override EqCompiler.CFunction CreateInstance(ArrayList alValues)
        {
            calV2Deg oV2Deg = new calV2Deg();
            oV2Deg.SetValue(alValues);

            return oV2Deg;
        }

        /// <summary>
        /// (class).SetValue() retains the values in the arraylist for the current instance
        /// </summary>
        /// <param name="alValues">Arraylist of CValue objects</param>
        public override void SetValue(ArrayList alValues)
        {
            CheckParms(alValues, 1);
            m_alValues = alValues;
        }

        /// <summary>
        ///  GetValue() is called by the compiler when the user requests the
        ///  function to be evaluated.
        /// </summary>
        /// <returns>
        /// a double value with absolute value applied to the
        /// child parameter
        /// </returns>
        public override double GetValue()
        {
            EqCompiler.CValue oValue = (EqCompiler.CValue)m_alValues[0];
            return calibrV2Deg(oValue.GetValue());
        }

        /// <summary>
        /// GetFunction() returns the function name as it appears syntactically
        /// to the compiler.
        /// </summary>
        /// <returns></returns>
        public override string GetFunction()
        {
            return "calV2Deg";
        }
    }

    public class calDeg2V : EqCompiler.CFunction
    {
        /// <summary>
        /// An array of values associated with the function.
        /// </summary>
        ArrayList m_alValues;
        double[] vData, degData;

        public calDeg2V()
        {
            vData = new double[131];
            for (int i = 0; i < 131; i++)
            {
                vData[i] = (double)i / 10.0;
            }                
            degData = new double[] // n=131         
            { 0.000, 9.228, 18.458, 26.806, 35.596, 44.166, 52.514, 60.424, 68.334, 75.806, 83.056, 89.648, 96.240, 102.172, 107.666, 112.940, 117.774, 122.828, 127.222, 131.396, // 20
            135.572, 139.746, 143.482, 147.220, 150.952, 154.468, 158.204, 161.500, 165.234, 168.750, 172.266, 176.440, 179.516, 182.812, 186.108, 189.624, 192.920, 195.996, 199.512,  
            203.248, 206.762, 210.280, 214.014, 217.750, 221.264, 225.220, 228.956, 232.910, 236.646, 240.820, 244.556, 248.292, 252.466, 256.202, 260.156, 264.112, 267.846, 271.582,
            275.538, 279.052, 282.568, 286.304, 290.260, 293.774, 297.070, 300.586, 304.102, 307.398, 310.474, 313.990, 317.286, 320.362, 323.438, 326.514, 329.590, 332.886, 335.742,
            338.818, 341.456, 344.312, 347.388, 350.244, 353.100, 355.738, 358.374, 361.230, 364.086, 366.944, 369.580, 372.216, 375.074, 377.710, 380.346, 383.422, 385.620, 388.038,
            390.454, 393.090, 395.728, 398.144, 400.782, 403.198, 405.616, 408.032, 410.450, 412.866, 414.842, 417.260, 419.238, 421.656, 423.852, 425.830, 427.808, 429.566, 431.542, // 5x19
            433.300, 435.058, 436.596, 438.134, 439.672, 441.210, 442.530, 443.848, 445.166, 446.264, 447.364, 448.242, 449.560, 450.220, 450.878, 451.758 }; // 16
        }
        public double calibrDeg2V(double deg)
        {
            if (!Utils.InRange(deg, degData[0], degData[degData.Length - 1])) throw new Exception("Out of range phase -> " + deg.ToString("G6"));
            double[] secondDerivatives;
            double initialBoundary, finalBoundary;
            Random rnd = new Random();

            // Causes SplineInterpolant method to set the initial boundary condition for a natural spine
            initialBoundary = 1.00E+30;

            // Causes SplineInterpolant method to set the final boundary condition for a natural spine
            finalBoundary = 1.00E+30;

            // Calculate secondDerivatives
            secondDerivatives = CurveFit.SplineInterpolant(degData, vData, initialBoundary, finalBoundary);

            // Calculate spline interpolated value  
            return CurveFit.SplineInterpolation(degData, vData, secondDerivatives, deg);
        }

        /// <summary>
        /// (class).CreateInstance returns an instance of the CAbs object
        /// with the passed CValue object(s).
        /// </summary>
        /// <param name="alValues">An arraylist of values passed by the compiler.</param>
        /// <returns></returns>
        public override EqCompiler.CFunction CreateInstance(ArrayList alValues)
        {
            calDeg2V oDeg2V = new calDeg2V();
            oDeg2V.SetValue(alValues);

            return oDeg2V;
        }

        /// <summary>
        /// (class)s.SetValue() retains the values in the arraylist for the current instance
        /// </summary>
        /// <param name="alValues">Arraylist of CValue objects</param>
        public override void SetValue(ArrayList alValues)
        {
            CheckParms(alValues, 1);
            m_alValues = alValues;
        }

        /// <summary>
        ///  GetValue() is called by the compiler when the user requests the
        ///  function to be evaluated.
        /// </summary>
        /// <returns>
        /// a double value with absolute value applied to the
        /// child parameter
        /// </returns>
        public override double GetValue()
        {
            EqCompiler.CValue oValue = (EqCompiler.CValue)m_alValues[0];
            return calibrDeg2V(oValue.GetValue());
        }

        /// <summary>
        /// GetFunction() returns the function name as it appears syntactically
        /// to the compiler.
        /// </summary>
        /// <returns></returns>
        public override string GetFunction()
        {
            return "calDeg2V";
        }
    }

    public class CustomizeCompiler
    {
        public static void AddFunctions(EqCompiler compiler)
        {
            compiler.AddFunction(new calV2Deg());
            compiler.AddFunction(new calDeg2V());
        }
    }
    #endregion CustomizeCompiler
}
