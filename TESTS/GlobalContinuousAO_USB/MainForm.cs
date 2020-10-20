/*******************************************************************************
*
* Example program:
*   GlobalContinuousAO
*
* Description:
*   This example shows how to load a continuous analog output task from the Measurement & 
*   Automation Explorer (MAX) and use it to write generated data to a DAQ device and plot the data.
*
* Instructions for running:
*   1.  Create a continuous analog output NI-DAQmx global task in MAX. For help, refer to 
*       "Creating Tasks and Channels" in the Measurement & Automation Explorer Help. 
*       To access this help, select Start>>All Programs>>National Instruments>>
*       Measurement & Automation. In MAX, select Help>>MAX Help.
*
*       Note: If you prefer, you can import a continuous AO task and a simulated 
*       device into MAX from the GlobalContinuousAO.nce file, which is located in the 
*       example directory. Refer to "Using the Configuration Import Wizard" in the 
*       Measurement & Automation Explorer Help for more information.
*
*   2.  Run the application, select the task from the drop-down list, and click 
*       the Start button.
*
* Steps:
*   1.  Load the task from MAX.
*   2.  Generate data for the task.
*   3.  Write data to the channel in the task
*   4.  Plot the generated data on a waveform graph.
*******************************************************************************/

using NationalInstruments.Analysis.SignalGeneration;
using NationalInstruments;
using NationalInstruments.DAQmx;
using NationalInstruments.UI;
using NationalInstruments.UI.WindowsForms;
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;

namespace NationalInstruments.Examples.GlobalContinuousAO
{
    /// <summary>
    /// Summary description for Form1.
    /// </summary>
    public class MainForm : System.Windows.Forms.Form
    {
        private NationalInstruments.UI.XAxis xAxis1;
        private NationalInstruments.UI.YAxis yAxis1;
        private NationalInstruments.UI.WaveformPlot waveformPlot1;
        private System.Windows.Forms.GroupBox daqmxTaskGroupBox;
        private System.Windows.Forms.Label daqmxTaskLabel;
        private System.Windows.Forms.ComboBox taskComboBox;
        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.Button stopButton;
        private System.Windows.Forms.Label infoLabel;
        private NationalInstruments.UI.WaveformPlot waveformPlot2;
        private NationalInstruments.UI.XAxis xAxis2;
        private NationalInstruments.UI.YAxis yAxis2;
        private NationalInstruments.UI.WindowsForms.Legend channelLegend;
        private Task continuousTask;
        private AnalogSingleChannelWriter writer;
        private NationalInstruments.UI.WindowsForms.WaveformGraph globalContinuousAOWaveformGraph;
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        public MainForm()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();
            
            startButton.Enabled = false;
            stopButton.Enabled = false;

            // Add valid continuous analog output tasks to the combo box
            foreach (string s in DaqSystem.Local.Tasks)
            {
                try
                {
                    using (Task t = DaqSystem.Local.LoadTask(s))
                    {
                        t.Control(TaskAction.Verify);

                        if (t.AOChannels.Count > 0 &&
                            t.Timing.SampleQuantityMode == SampleQuantityMode.ContinuousSamples)
                        {
                            taskComboBox.Items.Add(s);
                        }
                    }
                }
                catch (DaqException)
                {
                    // Ignore invalid tasks
                }
            }
       
            if (taskComboBox.Items.Count > 0)
            {
                taskComboBox.SelectedIndex = 0;
                startButton.Enabled = true;
            }
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose( bool disposing )
        {
            if( disposing )
            {
                if (components != null) 
                {
                    components.Dispose();
                }
            }
            base.Dispose( disposing );
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.xAxis1 = new NationalInstruments.UI.XAxis();
            this.yAxis1 = new NationalInstruments.UI.YAxis();
            this.waveformPlot1 = new NationalInstruments.UI.WaveformPlot();
            this.daqmxTaskGroupBox = new System.Windows.Forms.GroupBox();
            this.daqmxTaskLabel = new System.Windows.Forms.Label();
            this.taskComboBox = new System.Windows.Forms.ComboBox();
            this.startButton = new System.Windows.Forms.Button();
            this.stopButton = new System.Windows.Forms.Button();
            this.infoLabel = new System.Windows.Forms.Label();
            this.globalContinuousAOWaveformGraph = new NationalInstruments.UI.WindowsForms.WaveformGraph();
            this.waveformPlot2 = new NationalInstruments.UI.WaveformPlot();
            this.xAxis2 = new NationalInstruments.UI.XAxis();
            this.yAxis2 = new NationalInstruments.UI.YAxis();
            this.channelLegend = new NationalInstruments.UI.WindowsForms.Legend();
            this.daqmxTaskGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.globalContinuousAOWaveformGraph)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.channelLegend)).BeginInit();
            this.SuspendLayout();
            // 
            // waveformPlot1
            // 
            this.waveformPlot1.XAxis = this.xAxis1;
            this.waveformPlot1.YAxis = this.yAxis1;
            // 
            // daqmxTaskGroupBox
            // 
            this.daqmxTaskGroupBox.Controls.Add(this.daqmxTaskLabel);
            this.daqmxTaskGroupBox.Controls.Add(this.taskComboBox);
            this.daqmxTaskGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.daqmxTaskGroupBox.Location = new System.Drawing.Point(8, 8);
            this.daqmxTaskGroupBox.Name = "daqmxTaskGroupBox";
            this.daqmxTaskGroupBox.Size = new System.Drawing.Size(328, 72);
            this.daqmxTaskGroupBox.TabIndex = 3;
            this.daqmxTaskGroupBox.TabStop = false;
            this.daqmxTaskGroupBox.Text = "Global DAQmx Task";
            // 
            // daqmxTaskLabel
            // 
            this.daqmxTaskLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.daqmxTaskLabel.Location = new System.Drawing.Point(8, 32);
            this.daqmxTaskLabel.Name = "daqmxTaskLabel";
            this.daqmxTaskLabel.Size = new System.Drawing.Size(80, 24);
            this.daqmxTaskLabel.TabIndex = 0;
            this.daqmxTaskLabel.Text = "DAQmx Task:";
            // 
            // taskComboBox
            // 
            this.taskComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.taskComboBox.Location = new System.Drawing.Point(104, 32);
            this.taskComboBox.Name = "taskComboBox";
            this.taskComboBox.Size = new System.Drawing.Size(216, 21);
            this.taskComboBox.TabIndex = 1;
            // 
            // startButton
            // 
            this.startButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.startButton.Location = new System.Drawing.Point(352, 40);
            this.startButton.Name = "startButton";
            this.startButton.TabIndex = 1;
            this.startButton.Text = "Start";
            this.startButton.Click += new System.EventHandler(this.startButton_Click);
            // 
            // stopButton
            // 
            this.stopButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.stopButton.Location = new System.Drawing.Point(437, 40);
            this.stopButton.Name = "stopButton";
            this.stopButton.TabIndex = 2;
            this.stopButton.Text = "Stop";
            this.stopButton.Click += new System.EventHandler(this.stopButton_Click);
            // 
            // infoLabel
            // 
            this.infoLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.infoLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.infoLabel.Location = new System.Drawing.Point(520, 8);
            this.infoLabel.Name = "infoLabel";
            this.infoLabel.Size = new System.Drawing.Size(264, 96);
            this.infoLabel.TabIndex = 5;
            this.infoLabel.Text = resources.GetString("infoLabel.Text");
            // 
            // globalContinuousAOWaveformGraph
            // 
            this.globalContinuousAOWaveformGraph.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.globalContinuousAOWaveformGraph.Location = new System.Drawing.Point(8, 107);
            this.globalContinuousAOWaveformGraph.Name = "globalContinuousAOWaveformGraph";
            this.globalContinuousAOWaveformGraph.Plots.AddRange(new NationalInstruments.UI.WaveformPlot[] {
            this.waveformPlot2});
            this.globalContinuousAOWaveformGraph.Size = new System.Drawing.Size(504, 293);
            this.globalContinuousAOWaveformGraph.TabIndex = 3;
            this.globalContinuousAOWaveformGraph.TabStop = false;
            this.globalContinuousAOWaveformGraph.XAxes.AddRange(new NationalInstruments.UI.XAxis[] {
            this.xAxis2});
            this.globalContinuousAOWaveformGraph.YAxes.AddRange(new NationalInstruments.UI.YAxis[] {
            this.yAxis2});
            // 
            // waveformPlot2
            // 
            this.waveformPlot2.XAxis = this.xAxis2;
            this.waveformPlot2.YAxis = this.yAxis2;
            // 
            // channelLegend
            // 
            this.channelLegend.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.channelLegend.Location = new System.Drawing.Point(520, 107);
            this.channelLegend.Name = "channelLegend";
            this.channelLegend.Size = new System.Drawing.Size(264, 293);
            this.channelLegend.TabIndex = 4;
            this.channelLegend.TabStop = false;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(792, 406);
            this.Controls.Add(this.channelLegend);
            this.Controls.Add(this.globalContinuousAOWaveformGraph);
            this.Controls.Add(this.infoLabel);
            this.Controls.Add(this.daqmxTaskGroupBox);
            this.Controls.Add(this.startButton);
            this.Controls.Add(this.stopButton);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Global Continuous Analog Output";
            this.daqmxTaskGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.globalContinuousAOWaveformGraph)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.channelLegend)).EndInit();
            this.ResumeLayout(false);

        }
        #endregion

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() 
        {
            Application.EnableVisualStyles();
            Application.DoEvents();
            Application.Run(new MainForm());
        }

        private void startButton_Click(object sender, System.EventArgs e)
        {
            startButton.Enabled = false;
            channelLegend.Items.Clear();
            globalContinuousAOWaveformGraph.ClearData();
            globalContinuousAOWaveformGraph.Plots.Clear();

            try
            {
                // Get task from combox box and initialize the analog writer
                string taskName = taskComboBox.SelectedItem.ToString();
                continuousTask = DaqSystem.Local.LoadTask(taskName);
                writer = new AnalogSingleChannelWriter(continuousTask.Stream);
             
                continuousTask.Control(TaskAction.Verify);

                // Generate the sine wave data
                SignalGenerator fgen = null;
                fgen = new SignalGenerator(continuousTask.Timing.SampleClockRate, continuousTask.Timing.SamplesPerChannel, new SineSignal());
                double[] generatedData = fgen.Generate();

                
                // Convert the generated data to an AnalogWaveform
                AnalogWaveform<double> data = AnalogWaveform<double>.FromArray1D(generatedData);

                // Write and plot the generated data
                WaveformPlot plot = new WaveformPlot();
                globalContinuousAOWaveformGraph.Plots.Add(plot);

                writer.WriteWaveform<double>(true, data);
                globalContinuousAOWaveformGraph.PlotWaveform<double>(data);
                channelLegend.Items.Add(new LegendItem(plot, continuousTask.AOChannels[0].VirtualName + ": " + continuousTask.AOChannels[0].PhysicalName));
                
                
                stopButton.Enabled = true;
                taskComboBox.Enabled = false;
            }
            catch (DaqException ex)
            {
                MessageBox.Show(ex.Message);
                continuousTask.Dispose();
                
                startButton.Enabled = true;
            }
        }

        private void stopButton_Click(object sender, System.EventArgs e)
        {
            stopButton.Enabled = false;

            try
            {
                continuousTask.Stop();
            }
            catch (DaqException ex)
            {
                MessageBox.Show(ex.Message);
            }

            continuousTask.Dispose();
            startButton.Enabled = true;
            taskComboBox.Enabled = true;
        }
    }
}
