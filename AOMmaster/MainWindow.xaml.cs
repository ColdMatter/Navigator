using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;
using UtilsNS;

namespace AOMmaster
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            configFile = Utils.configPath + "location.json";
        }
        string configFile;
        Dictionary<string, double> config = null;
        private void frmMain_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(configFile))
            {
                string json = System.IO.File.ReadAllText(configFile);
                config = JsonConvert.DeserializeObject<Dictionary<string, double>>(json);
                Left = config["Left"]; Top = config["Top"];
                Width = config["Width"]; Height = config["Height"];
            }
            else config = new Dictionary<string, double>();
        }
        private void frmMain_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            config["Left"] = Left; config["Top"] = Top;
            config["Width"] = Width; config["Height"] = Height;
            string json = JsonConvert.SerializeObject(config);
            System.IO.File.WriteAllText(configFile, json);

            AOMmasterUC1.Closing();
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.F1)) System.Diagnostics.Process.Start("http://www.axelsuite.com");
        }
    }
}
