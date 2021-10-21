using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using dotMath;
using UtilsNS;

namespace MOTMaster2.SequenceData
{
    /// <summary>
    /// SW -> key (selector) and N choices where N is the number of arguments after the key
    /// sw(key,arg1, arg2...argN)
    /// </summary>
    public class sw : EqCompiler.CFunction
    {
        /// <summary>
        /// An array of values associated with the function.
        /// </summary>
        ArrayList m_alValues;

        public sw()
        {
        }

        /// <summary>
        /// (class).CreateInstance returns an instance of the CAbs object
        /// with the passed CValue object(s).
        /// </summary>
        /// <param name="alValues">An arraylist of values passed by the compiler.</param>
        /// <returns></returns>
        public override EqCompiler.CFunction CreateInstance(ArrayList alValues)
        {
            sw osw = new sw();
            osw.SetValue(alValues);

            return osw;
        }

        /// <summary>
        /// (class).SetValue() retains the values in the arraylist for the current instance
        /// </summary>
        /// <param name="alValues">Arraylist of CValue objects</param>
        public override void SetValue(ArrayList alValues)
        {
            CheckParms(alValues, 2, 16);
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
            EqCompiler.CValue key = (EqCompiler.CValue)m_alValues[0];
            int ikey = Convert.ToInt32(key.GetValue()) % (m_alValues.Count-1);
            return ((EqCompiler.CValue)m_alValues[ikey+1]).GetValue();
        }

        /// <summary>
        /// GetFunction() returns the function name as it appears syntactically
        /// to the compiler.
        /// </summary>
        /// <returns></returns>
        public override string GetFunction()
        {
            return "sw";
        }
    }

    /// <summary>
    /// SW2 -> key and 2 choices
    /// </summary>
    public class sw2 : EqCompiler.CFunction
    {
        /// <summary>
        /// An array of values associated with the function.
        /// </summary>
        ArrayList m_alValues;

        public sw2()
        {
        }

        public double sw2func(double key, double st0, double st1)
        {
            int ikey = Convert.ToInt32(key);
            switch (key % 2)
            {
                case 0: return st0;
                case 1: return st1;
            }
            return -1;            
        }

        /// <summary>
        /// (class).CreateInstance returns an instance of the CAbs object
        /// with the passed CValue object(s).
        /// </summary>
        /// <param name="alValues">An arraylist of values passed by the compiler.</param>
        /// <returns></returns>
        public override EqCompiler.CFunction CreateInstance(ArrayList alValues)
        {
            sw2 osw2 = new sw2();
            osw2.SetValue(alValues);

            return osw2;
        }

        /// <summary>
        /// (class).SetValue() retains the values in the arraylist for the current instance
        /// </summary>
        /// <param name="alValues">Arraylist of CValue objects</param>
        public override void SetValue(ArrayList alValues)
        {
            CheckParms(alValues, 3,3);
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
            EqCompiler.CValue oValue0 = (EqCompiler.CValue)m_alValues[0];
            EqCompiler.CValue oValue1 = (EqCompiler.CValue)m_alValues[1];
            EqCompiler.CValue oValue2 = (EqCompiler.CValue)m_alValues[2];
            return sw2func(oValue0.GetValue(), oValue1.GetValue(), oValue2.GetValue());
        }

        /// <summary>
        /// GetFunction() returns the function name as it appears syntactically
        /// to the compiler.
        /// </summary>
        /// <returns></returns>
        public override string GetFunction()
        {
            return "sw2";
        }
    }
    /// <summary>
    /// SW3 -> key and 3 choices
    /// </summary>
    public class sw3 : EqCompiler.CFunction
    {
        /// <summary>
        /// An array of values associated with the function.
        /// </summary>
        ArrayList m_alValues;

        public sw3()
        {
        }

        public double sw3func(double key, double st0, double st1, double st2)
        {
            int ikey = Convert.ToInt32(key);
            switch (key % 3)
            {
                case 0: return st0;
                case 1: return st1;
                case 2: return st2;
            }
            return -1;
        }

        /// <summary>
        /// (class).CreateInstance returns an instance of the CAbs object
        /// with the passed CValue object(s).
        /// </summary>
        /// <param name="alValues">An arraylist of values passed by the compiler.</param>
        /// <returns></returns>
        public override EqCompiler.CFunction CreateInstance(ArrayList alValues)
        {
            sw3 osw3 = new sw3();
            osw3.SetValue(alValues);

            return osw3;
        }

        /// <summary>
        /// (class).SetValue() retains the values in the arraylist for the current instance
        /// </summary>
        /// <param name="alValues">Arraylist of CValue objects</param>
        public override void SetValue(ArrayList alValues)
        {
            CheckParms(alValues, 4, 4);
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
            EqCompiler.CValue oValue0 = (EqCompiler.CValue)m_alValues[0];
            EqCompiler.CValue oValue1 = (EqCompiler.CValue)m_alValues[1];
            EqCompiler.CValue oValue2 = (EqCompiler.CValue)m_alValues[2];
            EqCompiler.CValue oValue3 = (EqCompiler.CValue)m_alValues[3];
            return sw3func(oValue0.GetValue(), oValue1.GetValue(), oValue2.GetValue(), oValue3.GetValue());
        }

        /// <summary>
        /// GetFunction() returns the function name as it appears syntactically
        /// to the compiler.
        /// </summary>
        /// <returns></returns>
        public override string GetFunction()
        {
            return "sw3";
        }
    }
    /// <summary>
    /// SW4 -> key and 4 choices
    /// </summary>
    public class sw4 : EqCompiler.CFunction
    {
        /// <summary>
        /// An array of values associated with the function.
        /// </summary>
        ArrayList m_alValues;

        public sw4()
        {
        }

        public double sw4func(double key, double st0, double st1, double st2, double st3)
        {
            int ikey = Convert.ToInt32(key);
            switch (key % 4)
            {
                case 0: return st0;
                case 1: return st1;
                case 2: return st2;
                case 3: return st3;
            }
            return -1;
        }

        /// <summary>
        /// (class).CreateInstance returns an instance of the CAbs object
        /// with the passed CValue object(s).
        /// </summary>
        /// <param name="alValues">An arraylist of values passed by the compiler.</param>
        /// <returns></returns>
        public override EqCompiler.CFunction CreateInstance(ArrayList alValues)
        {
            sw4 osw4 = new sw4();
            osw4.SetValue(alValues);

            return osw4;
        }

        /// <summary>
        /// (class).SetValue() retains the values in the arraylist for the current instance
        /// </summary>
        /// <param name="alValues">Arraylist of CValue objects</param>
        public override void SetValue(ArrayList alValues)
        {
            CheckParms(alValues, 5, 5);
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
            EqCompiler.CValue oValue0 = (EqCompiler.CValue)m_alValues[0];
            EqCompiler.CValue oValue1 = (EqCompiler.CValue)m_alValues[1];
            EqCompiler.CValue oValue2 = (EqCompiler.CValue)m_alValues[2];
            EqCompiler.CValue oValue3 = (EqCompiler.CValue)m_alValues[3];
            EqCompiler.CValue oValue4 = (EqCompiler.CValue)m_alValues[4];
            return sw4func(oValue0.GetValue(), oValue1.GetValue(), oValue2.GetValue(), oValue3.GetValue(), oValue4.GetValue());
        }

        /// <summary>
        /// GetFunction() returns the function name as it appears syntactically
        /// to the compiler.
        /// </summary>
        /// <returns></returns>
        public override string GetFunction()
        {
            return "sw4";
        }
    }

    /// <summary>
    /// SW5 -> key and 5 choices
    /// </summary>
    public class sw5 : EqCompiler.CFunction
    {
        /// <summary>
        /// An array of values associated with the function.
        /// </summary>
        ArrayList m_alValues;

        public sw5()
        {
        }

        public double sw5func(double key, double st0, double st1, double st2, double st3, double st4)
        {
            int ikey = Convert.ToInt32(key);
            switch (key % 5)
            {
                case 0: return st0;
                case 1: return st1;
                case 2: return st2;
                case 3: return st3;
                case 4: return st4;
            }
            return -1;
        }

        /// <summary>
        /// (class).CreateInstance returns an instance of the CAbs object
        /// with the passed CValue object(s).
        /// </summary>
        /// <param name="alValues">An arraylist of values passed by the compiler.</param>
        /// <returns></returns>
        public override EqCompiler.CFunction CreateInstance(ArrayList alValues)
        {
            sw5 osw5 = new sw5();
            osw5.SetValue(alValues);

            return osw5;
        }

        /// <summary>
        /// (class).SetValue() retains the values in the arraylist for the current instance
        /// </summary>
        /// <param name="alValues">Arraylist of CValue objects</param>
        public override void SetValue(ArrayList alValues)
        {
            CheckParms(alValues, 6, 6);
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
            EqCompiler.CValue oValue0 = (EqCompiler.CValue)m_alValues[0];
            EqCompiler.CValue oValue1 = (EqCompiler.CValue)m_alValues[1];
            EqCompiler.CValue oValue2 = (EqCompiler.CValue)m_alValues[2];
            EqCompiler.CValue oValue3 = (EqCompiler.CValue)m_alValues[3];
            EqCompiler.CValue oValue4 = (EqCompiler.CValue)m_alValues[4];
            EqCompiler.CValue oValue5 = (EqCompiler.CValue)m_alValues[5];
            return sw5func(oValue0.GetValue(), oValue1.GetValue(), oValue2.GetValue(), oValue3.GetValue(), oValue4.GetValue(), oValue5.GetValue());
        }

        /// <summary>
        /// GetFunction() returns the function name as it appears syntactically
        /// to the compiler.
        /// </summary>
        /// <returns></returns>
        public override string GetFunction()
        {
            return "sw5";
        }
    }
    /// <summary>
    /// SW6 -> key and 6 choices
    /// </summary>
    public class sw6 : EqCompiler.CFunction
    {
        /// <summary>
        /// An array of values associated with the function.
        /// </summary>
        ArrayList m_alValues;

        public sw6()
        {
        }

        public double sw6func(double key, double st0, double st1, double st2, double st3, double st4, double st5)
        {
            int ikey = Convert.ToInt32(key);
            switch (key % 6)
            {
                case 0: return st0;
                case 1: return st1;
                case 2: return st2;
                case 3: return st3;
                case 4: return st4;
                case 5: return st5;
            }
            return -1;
        }

        /// <summary>
        /// (class).CreateInstance returns an instance of the CAbs object
        /// with the passed CValue object(s).
        /// </summary>
        /// <param name="alValues">An arraylist of values passed by the compiler.</param>
        /// <returns></returns>
        public override EqCompiler.CFunction CreateInstance(ArrayList alValues)
        {
            sw6 osw6 = new sw6();
            osw6.SetValue(alValues);

            return osw6;
        }

        /// <summary>
        /// (class).SetValue() retains the values in the arraylist for the current instance
        /// </summary>
        /// <param name="alValues">Arraylist of CValue objects</param>
        public override void SetValue(ArrayList alValues)
        {
            CheckParms(alValues, 7, 7);
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
            EqCompiler.CValue oValue0 = (EqCompiler.CValue)m_alValues[0];
            EqCompiler.CValue oValue1 = (EqCompiler.CValue)m_alValues[1];
            EqCompiler.CValue oValue2 = (EqCompiler.CValue)m_alValues[2];
            EqCompiler.CValue oValue3 = (EqCompiler.CValue)m_alValues[3];
            EqCompiler.CValue oValue4 = (EqCompiler.CValue)m_alValues[4];
            EqCompiler.CValue oValue5 = (EqCompiler.CValue)m_alValues[5];
            EqCompiler.CValue oValue6 = (EqCompiler.CValue)m_alValues[6];
            return sw6func(oValue0.GetValue(), oValue1.GetValue(), oValue2.GetValue(), oValue3.GetValue(), oValue4.GetValue(), oValue5.GetValue(), oValue6.GetValue());
        }

        /// <summary>
        /// GetFunction() returns the function name as it appears syntactically
        /// to the compiler.
        /// </summary>
        /// <returns></returns>
        public override string GetFunction()
        {
            return "sw6";
        }
    }
    /// <summary>
    /// SW7 -> key and 7 choices
    /// </summary>
    public class sw7 : EqCompiler.CFunction
    {
        /// <summary>
        /// An array of values associated with the function.
        /// </summary>
        ArrayList m_alValues;

        public sw7()
        {
        }

        public double sw7func(double key, double st0, double st1, double st2, double st3, double st4, double st5, double st6)
        {
            int ikey = Convert.ToInt32(key);
            switch (key % 7)
            {
                case 0: return st0;
                case 1: return st1;
                case 2: return st2;
                case 3: return st3;
                case 4: return st4;
                case 5: return st5;
                case 6: return st6;
            }
            return -1;
        }

        /// <summary>
        /// (class).CreateInstance returns an instance of the CAbs object
        /// with the passed CValue object(s).
        /// </summary>
        /// <param name="alValues">An arraylist of values passed by the compiler.</param>
        /// <returns></returns>
        public override EqCompiler.CFunction CreateInstance(ArrayList alValues)
        {
            sw7 osw7 = new sw7();
            osw7.SetValue(alValues);

            return osw7;
        }

        /// <summary>
        /// (class).SetValue() retains the values in the arraylist for the current instance
        /// </summary>
        /// <param name="alValues">Arraylist of CValue objects</param>
        public override void SetValue(ArrayList alValues)
        {
            CheckParms(alValues, 8, 8);
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
            EqCompiler.CValue oValue0 = (EqCompiler.CValue)m_alValues[0];
            EqCompiler.CValue oValue1 = (EqCompiler.CValue)m_alValues[1];
            EqCompiler.CValue oValue2 = (EqCompiler.CValue)m_alValues[2];
            EqCompiler.CValue oValue3 = (EqCompiler.CValue)m_alValues[3];
            EqCompiler.CValue oValue4 = (EqCompiler.CValue)m_alValues[4];
            EqCompiler.CValue oValue5 = (EqCompiler.CValue)m_alValues[5];
            EqCompiler.CValue oValue6 = (EqCompiler.CValue)m_alValues[6];
            EqCompiler.CValue oValue7 = (EqCompiler.CValue)m_alValues[7];
            return sw7func(oValue0.GetValue(), oValue1.GetValue(), oValue2.GetValue(), oValue3.GetValue(), oValue4.GetValue(), oValue5.GetValue(), 
                oValue6.GetValue(), oValue7.GetValue());
        }

        /// <summary>
        /// GetFunction() returns the function name as it appears syntactically
        /// to the compiler.
        /// </summary>
        /// <returns></returns>
        public override string GetFunction()
        {
            return "sw7";
        }
    }
    /// <summary>
    /// SW8 -> key and 8 choices
    /// </summary>
    public class sw8 : EqCompiler.CFunction
    {
        /// <summary>
        /// An array of values associated with the function.
        /// </summary>
        ArrayList m_alValues;

        public sw8()
        {
        }

        public double sw8func(double key, double st0, double st1, double st2, double st3, double st4, double st5, double st6, double st7)
        {
            int ikey = Convert.ToInt32(key);
            switch (key % 8)
            {
                case 0: return st0;
                case 1: return st1;
                case 2: return st2;
                case 3: return st3;
                case 4: return st4;
                case 5: return st5;
                case 6: return st6;
                case 7: return st7;
            }
            return -1;
        }

        /// <summary>
        /// (class).CreateInstance returns an instance of the CAbs object
        /// with the passed CValue object(s).
        /// </summary>
        /// <param name="alValues">An arraylist of values passed by the compiler.</param>
        /// <returns></returns>
        public override EqCompiler.CFunction CreateInstance(ArrayList alValues)
        {
            sw8 osw8 = new sw8();
            osw8.SetValue(alValues);

            return osw8;
        }

        /// <summary>
        /// (class).SetValue() retains the values in the arraylist for the current instance
        /// </summary>
        /// <param name="alValues">Arraylist of CValue objects</param>
        public override void SetValue(ArrayList alValues)
        {
            CheckParms(alValues, 9, 9);
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
            EqCompiler.CValue oValue0 = (EqCompiler.CValue)m_alValues[0];
            EqCompiler.CValue oValue1 = (EqCompiler.CValue)m_alValues[1];
            EqCompiler.CValue oValue2 = (EqCompiler.CValue)m_alValues[2];
            EqCompiler.CValue oValue3 = (EqCompiler.CValue)m_alValues[3];
            EqCompiler.CValue oValue4 = (EqCompiler.CValue)m_alValues[4];
            EqCompiler.CValue oValue5 = (EqCompiler.CValue)m_alValues[5];
            EqCompiler.CValue oValue6 = (EqCompiler.CValue)m_alValues[6];
            EqCompiler.CValue oValue7 = (EqCompiler.CValue)m_alValues[7];
            EqCompiler.CValue oValue8 = (EqCompiler.CValue)m_alValues[8];
            return sw8func(oValue0.GetValue(), oValue1.GetValue(), oValue2.GetValue(), oValue3.GetValue(), oValue4.GetValue(), oValue5.GetValue(), 
                oValue6.GetValue(), oValue7.GetValue(), oValue8.GetValue());
        }

        /// <summary>
        /// GetFunction() returns the function name as it appears syntactically
        /// to the compiler.
        /// </summary>
        /// <returns></returns>
        public override string GetFunction()
        {
            return "sw8";
        }
    }

    class CustomCompiler
    {
        public static void AddFunctions(EqCompiler compiler)
        {
            compiler.AddFunction(new sw2());
            compiler.AddFunction(new sw3());
            compiler.AddFunction(new sw4());
            compiler.AddFunction(new sw5());
            compiler.AddFunction(new sw6());
            compiler.AddFunction(new sw7());
            compiler.AddFunction(new sw8());
        }

    }
}
