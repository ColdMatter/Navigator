/******************************************************************************
*
* Example program:
*   ContAcqFreq_IntClk_SCXI1126
*
* Category:
*   AI
*
* Description:
*   This example demonstrates how to acquire frequency data from an SCXI-1126
*   using the DAQ device's internal clock.
*
* Instructions for running:
*   1.  Select the physical channel that corresponds to a channel on your
*       SCXI-1126.
*   2.  Enter the minimum and maximum frequency ranges.Note: For better
*       accuracy, try to match the input range to the expected frequency level
*       of the measured signal.
*   3.  Enter the sample rate for the hardware-timed acquisition. Also set the
*       number of samples to read. This will determine how many samples are read
*       each time the while loop iterates.
*   4.  Enter the level and hysteresis of the triggering window.Note: The
*       triggering window is defined as Threshold-Hysteresis to Threshold Level
*       and must be between -0.5 and 4.48.
*
* Steps:
*   1.  Create a Task object.  Create an AIChannel object using the
*       AIChannelCollection.CreateFrequencyVoltageChannel method.
*   2.  Set the rate for the sample clock using the Timing.ConfigureSampleClock
*       method.  Additionally, define the sample mode to be continuous.
*   3.  Set the value of the cutoff frequency for the low pass filter on the
*       SCXI-1126 module using the AIChannel.LowpassCutoffFrequency property.
*   4.  Call AnalogSingleChannelReader.BeginReadWaveform to install a callback
*       and begin the asynchronous read operation.
*   5.  Inside the callback, call AnalogSingleChannelReader.EndReadWaveform to
*       retrieve the data from the read operation.  
*   6.  Call AnalogSingleChannelReader.BeginMemoryOptimizedReadWaveform
*   7.  Dispose the Task object to clean-up any resources associated with the
*       task.
*   8.  Handle any DaqExceptions, if they occur.
*
*   Note: This example sets SynchronizeCallback to true. If SynchronizeCallback
*   is set to false, then you must give special consideration to safely dispose
*   the task and to update the UI from the callback. If SynchronizeCallback is
*   set to false, the callback executes on the worker thread and not on the main
*   UI thread. You can only update a UI component on the thread on which it was
*   created. Refer to the How to: Safely Dispose Task When Using Asynchronous
*   Callbacks topic in the NI-DAQmx .NET help for more information.
*
* I/O Connections Overview:
*   Make sure your signal input terminals match the physical channel text box. 
*   For more information on the input and output terminals for your device, open
*   the NI-DAQmx Help, and refer to the NI-DAQmx Device Terminals and Device
*   Considerations books in the table of contents.
*
* Microsoft Windows Vista User Account Control
*   Running certain applications on Microsoft Windows Vista requires
*   administrator privileges, 
*   because the application name contains keywords such as setup, update, or
*   install. To avoid this problem, 
*   you must add an additional manifest to the application that specifies the
*   privileges required to run 
*   the application. Some Measurement Studio NI-DAQmx examples for Visual Studio
*   include these keywords. 
*   Therefore, all examples for Visual Studio are shipped with an additional
*   manifest file that you must 
*   embed in the example executable. The manifest file is named
*   [ExampleName].exe.manifest, where [ExampleName] 
*   is the NI-provided example name. For information on how to embed the manifest
*   file, refer to http://msdn2.microsoft.com/en-us/library/bb756929.aspx.Note: 
*   The manifest file is not provided with examples for Visual Studio .NET 2003.
*
******************************************************************************/

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using NationalInstruments.DAQmx;

namespace NationalInstruments.Examples.ContAcqFreq_IntClk_SCXI1126
{
    /// <summary>
    /// Summary description for MainForm.
    /// </summary>
    public class MainForm : System.Windows.Forms.Form
    {
        private Task myTask;
        private Task runningTask;
        private AnalogSingleChannelReader analogInReader;
        private AnalogWaveform<double> data;
        private AsyncCallback analogCallback;
        internal System.Windows.Forms.GroupBox timingParametersGroupbox;
        internal System.Windows.Forms.NumericUpDown samplesToReadNumeric;
        internal System.Windows.Forms.NumericUpDown rateNumeric;
        internal System.Windows.Forms.Label sampleRateLabel;
        internal System.Windows.Forms.Label samplesToReadLabel;
        internal System.Windows.Forms.GroupBox channelParametersGroupBox;
        internal System.Windows.Forms.NumericUpDown lowPassCutoffNumeric;
        internal System.Windows.Forms.Label lowPassCutoffLabel;
        internal System.Windows.Forms.Label physicalChannelLabel;
        internal System.Windows.Forms.Label maximumValueLabel;
        internal System.Windows.Forms.Label minimumValueLabel;
        internal System.Windows.Forms.Button stopButton;
        internal System.Windows.Forms.Button startButton;
        internal System.Windows.Forms.Label measurementLabel;
        internal System.Windows.Forms.ListBox measurementListBox;
        internal System.Windows.Forms.GroupBox triggerParametersGroupBox;
        internal System.Windows.Forms.NumericUpDown hysteresisNumeric;
        internal System.Windows.Forms.NumericUpDown levelNumeric;
        internal System.Windows.Forms.Label levelLabel;
        internal System.Windows.Forms.Label hysteresisLabel;
        internal System.Windows.Forms.NumericUpDown maximumValueNumeric;
        internal System.Windows.Forms.NumericUpDown minimumValueNumeric;
        private System.Windows.Forms.ComboBox physicalChannelComboBox;
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

            //
            // TODO: Add any constructor code after InitializeComponent call
            //
            physicalChannelComboBox.Items.AddRange(DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AI, PhysicalChannelAccess.External));
            if (physicalChannelComboBox.Items.Count > 0)
                physicalChannelComboBox.SelectedIndex = 0;
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
                if (myTask != null)
                {
                    runningTask = null;
                    myTask.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(MainForm));
            this.timingParametersGroupbox = new System.Windows.Forms.GroupBox();
            this.samplesToReadNumeric = new System.Windows.Forms.NumericUpDown();
            this.rateNumeric = new System.Windows.Forms.NumericUpDown();
            this.sampleRateLabel = new System.Windows.Forms.Label();
            this.samplesToReadLabel = new System.Windows.Forms.Label();
            this.channelParametersGroupBox = new System.Windows.Forms.GroupBox();
            this.physicalChannelComboBox = new System.Windows.Forms.ComboBox();
            this.maximumValueNumeric = new System.Windows.Forms.NumericUpDown();
            this.minimumValueNumeric = new System.Windows.Forms.NumericUpDown();
            this.lowPassCutoffNumeric = new System.Windows.Forms.NumericUpDown();
            this.lowPassCutoffLabel = new System.Windows.Forms.Label();
            this.physicalChannelLabel = new System.Windows.Forms.Label();
            this.maximumValueLabel = new System.Windows.Forms.Label();
            this.minimumValueLabel = new System.Windows.Forms.Label();
            this.stopButton = new System.Windows.Forms.Button();
            this.startButton = new System.Windows.Forms.Button();
            this.measurementLabel = new System.Windows.Forms.Label();
            this.measurementListBox = new System.Windows.Forms.ListBox();
            this.triggerParametersGroupBox = new System.Windows.Forms.GroupBox();
            this.hysteresisNumeric = new System.Windows.Forms.NumericUpDown();
            this.levelNumeric = new System.Windows.Forms.NumericUpDown();
            this.levelLabel = new System.Windows.Forms.Label();
            this.hysteresisLabel = new System.Windows.Forms.Label();
            this.timingParametersGroupbox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.samplesToReadNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.rateNumeric)).BeginInit();
            this.channelParametersGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.maximumValueNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.minimumValueNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lowPassCutoffNumeric)).BeginInit();
            this.triggerParametersGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.hysteresisNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.levelNumeric)).BeginInit();
            this.SuspendLayout();
            // 
            // timingParametersGroupbox
            // 
            this.timingParametersGroupbox.Controls.Add(this.samplesToReadNumeric);
            this.timingParametersGroupbox.Controls.Add(this.rateNumeric);
            this.timingParametersGroupbox.Controls.Add(this.sampleRateLabel);
            this.timingParametersGroupbox.Controls.Add(this.samplesToReadLabel);
            this.timingParametersGroupbox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.timingParametersGroupbox.Location = new System.Drawing.Point(8, 176);
            this.timingParametersGroupbox.Name = "timingParametersGroupbox";
            this.timingParametersGroupbox.Size = new System.Drawing.Size(240, 88);
            this.timingParametersGroupbox.TabIndex = 3;
            this.timingParametersGroupbox.TabStop = false;
            this.timingParametersGroupbox.Text = "Timing Parameters";
            // 
            // samplesToReadNumeric
            // 
            this.samplesToReadNumeric.Location = new System.Drawing.Point(128, 56);
            this.samplesToReadNumeric.Maximum = new System.Decimal(new int[] {
                                                                                 100000,
                                                                                 0,
                                                                                 0,
                                                                                 0});
            this.samplesToReadNumeric.Name = "samplesToReadNumeric";
            this.samplesToReadNumeric.Size = new System.Drawing.Size(96, 20);
            this.samplesToReadNumeric.TabIndex = 3;
            this.samplesToReadNumeric.Value = new System.Decimal(new int[] {
                                                                               10000,
                                                                               0,
                                                                               0,
                                                                               0});
            // 
            // rateNumeric
            // 
            this.rateNumeric.DecimalPlaces = 2;
            this.rateNumeric.Location = new System.Drawing.Point(128, 24);
            this.rateNumeric.Maximum = new System.Decimal(new int[] {
                                                                        1000000,
                                                                        0,
                                                                        0,
                                                                        0});
            this.rateNumeric.Name = "rateNumeric";
            this.rateNumeric.Size = new System.Drawing.Size(96, 20);
            this.rateNumeric.TabIndex = 1;
            this.rateNumeric.Value = new System.Decimal(new int[] {
                                                                      10000,
                                                                      0,
                                                                      0,
                                                                      0});
            // 
            // sampleRateLabel
            // 
            this.sampleRateLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.sampleRateLabel.Location = new System.Drawing.Point(16, 24);
            this.sampleRateLabel.Name = "sampleRateLabel";
            this.sampleRateLabel.Size = new System.Drawing.Size(100, 16);
            this.sampleRateLabel.TabIndex = 0;
            this.sampleRateLabel.Text = "Sample Rate (Hz):";
            // 
            // samplesToReadLabel
            // 
            this.samplesToReadLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.samplesToReadLabel.Location = new System.Drawing.Point(16, 56);
            this.samplesToReadLabel.Name = "samplesToReadLabel";
            this.samplesToReadLabel.Size = new System.Drawing.Size(100, 16);
            this.samplesToReadLabel.TabIndex = 2;
            this.samplesToReadLabel.Text = "Samples to Read:";
            // 
            // channelParametersGroupBox
            // 
            this.channelParametersGroupBox.Controls.Add(this.physicalChannelComboBox);
            this.channelParametersGroupBox.Controls.Add(this.maximumValueNumeric);
            this.channelParametersGroupBox.Controls.Add(this.minimumValueNumeric);
            this.channelParametersGroupBox.Controls.Add(this.lowPassCutoffNumeric);
            this.channelParametersGroupBox.Controls.Add(this.lowPassCutoffLabel);
            this.channelParametersGroupBox.Controls.Add(this.physicalChannelLabel);
            this.channelParametersGroupBox.Controls.Add(this.maximumValueLabel);
            this.channelParametersGroupBox.Controls.Add(this.minimumValueLabel);
            this.channelParametersGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.channelParametersGroupBox.Location = new System.Drawing.Point(8, 8);
            this.channelParametersGroupBox.Name = "channelParametersGroupBox";
            this.channelParametersGroupBox.Size = new System.Drawing.Size(240, 157);
            this.channelParametersGroupBox.TabIndex = 2;
            this.channelParametersGroupBox.TabStop = false;
            this.channelParametersGroupBox.Text = "Channel Parameters";
            // 
            // physicalChannelComboBox
            // 
            this.physicalChannelComboBox.Location = new System.Drawing.Point(128, 24);
            this.physicalChannelComboBox.Name = "physicalChannelComboBox";
            this.physicalChannelComboBox.Size = new System.Drawing.Size(96, 21);
            this.physicalChannelComboBox.TabIndex = 1;
            this.physicalChannelComboBox.Text = "SC1Mod1/ai0";
            // 
            // maximumValueNumeric
            // 
            this.maximumValueNumeric.DecimalPlaces = 2;
            this.maximumValueNumeric.Location = new System.Drawing.Point(128, 56);
            this.maximumValueNumeric.Maximum = new System.Decimal(new int[] {
                                                                                1000000,
                                                                                0,
                                                                                0,
                                                                                0});
            this.maximumValueNumeric.Minimum = new System.Decimal(new int[] {
                                                                                1,
                                                                                0,
                                                                                0,
                                                                                0});
            this.maximumValueNumeric.Name = "maximumValueNumeric";
            this.maximumValueNumeric.Size = new System.Drawing.Size(96, 20);
            this.maximumValueNumeric.TabIndex = 3;
            this.maximumValueNumeric.Value = new System.Decimal(new int[] {
                                                                              1000,
                                                                              0,
                                                                              0,
                                                                              0});
            // 
            // minimumValueNumeric
            // 
            this.minimumValueNumeric.DecimalPlaces = 2;
            this.minimumValueNumeric.Location = new System.Drawing.Point(128, 88);
            this.minimumValueNumeric.Maximum = new System.Decimal(new int[] {
                                                                                1000000,
                                                                                0,
                                                                                0,
                                                                                0});
            this.minimumValueNumeric.Minimum = new System.Decimal(new int[] {
                                                                                1,
                                                                                0,
                                                                                0,
                                                                                0});
            this.minimumValueNumeric.Name = "minimumValueNumeric";
            this.minimumValueNumeric.Size = new System.Drawing.Size(96, 20);
            this.minimumValueNumeric.TabIndex = 5;
            this.minimumValueNumeric.Value = new System.Decimal(new int[] {
                                                                              1,
                                                                              0,
                                                                              0,
                                                                              0});
            // 
            // lowPassCutoffNumeric
            // 
            this.lowPassCutoffNumeric.Location = new System.Drawing.Point(128, 120);
            this.lowPassCutoffNumeric.Name = "lowPassCutoffNumeric";
            this.lowPassCutoffNumeric.Size = new System.Drawing.Size(96, 20);
            this.lowPassCutoffNumeric.TabIndex = 7;
            this.lowPassCutoffNumeric.Value = new System.Decimal(new int[] {
                                                                               1,
                                                                               0,
                                                                               0,
                                                                               0});
            // 
            // lowPassCutoffLabel
            // 
            this.lowPassCutoffLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.lowPassCutoffLabel.Location = new System.Drawing.Point(16, 120);
            this.lowPassCutoffLabel.Name = "lowPassCutoffLabel";
            this.lowPassCutoffLabel.Size = new System.Drawing.Size(120, 16);
            this.lowPassCutoffLabel.TabIndex = 6;
            this.lowPassCutoffLabel.Text = "Cutoff Frequency (Hz):";
            // 
            // physicalChannelLabel
            // 
            this.physicalChannelLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.physicalChannelLabel.Location = new System.Drawing.Point(16, 24);
            this.physicalChannelLabel.Name = "physicalChannelLabel";
            this.physicalChannelLabel.Size = new System.Drawing.Size(100, 16);
            this.physicalChannelLabel.TabIndex = 0;
            this.physicalChannelLabel.Text = "Physical Channel:";
            // 
            // maximumValueLabel
            // 
            this.maximumValueLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.maximumValueLabel.Location = new System.Drawing.Point(16, 56);
            this.maximumValueLabel.Name = "maximumValueLabel";
            this.maximumValueLabel.Size = new System.Drawing.Size(120, 16);
            this.maximumValueLabel.TabIndex = 2;
            this.maximumValueLabel.Text = "Maximum Value (Hz):";
            // 
            // minimumValueLabel
            // 
            this.minimumValueLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.minimumValueLabel.Location = new System.Drawing.Point(16, 88);
            this.minimumValueLabel.Name = "minimumValueLabel";
            this.minimumValueLabel.Size = new System.Drawing.Size(112, 16);
            this.minimumValueLabel.TabIndex = 4;
            this.minimumValueLabel.Text = "Minimum Value (Hz):";
            // 
            // stopButton
            // 
            this.stopButton.Enabled = false;
            this.stopButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.stopButton.Location = new System.Drawing.Point(280, 224);
            this.stopButton.Name = "stopButton";
            this.stopButton.Size = new System.Drawing.Size(71, 29);
            this.stopButton.TabIndex = 1;
            this.stopButton.Text = "Stop";
            this.stopButton.Click += new System.EventHandler(this.stopButton_Click);
            // 
            // startButton
            // 
            this.startButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.startButton.Location = new System.Drawing.Point(280, 184);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(71, 29);
            this.startButton.TabIndex = 0;
            this.startButton.Text = "Start";
            this.startButton.Click += new System.EventHandler(this.startButton_Click);
            // 
            // measurementLabel
            // 
            this.measurementLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.measurementLabel.Location = new System.Drawing.Point(256, 16);
            this.measurementLabel.Name = "measurementLabel";
            this.measurementLabel.Size = new System.Drawing.Size(100, 16);
            this.measurementLabel.TabIndex = 5;
            this.measurementLabel.Text = "Measurement:";
            // 
            // measurementListBox
            // 
            this.measurementListBox.Location = new System.Drawing.Point(256, 32);
            this.measurementListBox.Name = "measurementListBox";
            this.measurementListBox.Size = new System.Drawing.Size(120, 134);
            this.measurementListBox.TabIndex = 6;
            this.measurementListBox.TabStop = false;
            // 
            // triggerParametersGroupBox
            // 
            this.triggerParametersGroupBox.Controls.Add(this.hysteresisNumeric);
            this.triggerParametersGroupBox.Controls.Add(this.levelNumeric);
            this.triggerParametersGroupBox.Controls.Add(this.levelLabel);
            this.triggerParametersGroupBox.Controls.Add(this.hysteresisLabel);
            this.triggerParametersGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.triggerParametersGroupBox.Location = new System.Drawing.Point(8, 280);
            this.triggerParametersGroupBox.Name = "triggerParametersGroupBox";
            this.triggerParametersGroupBox.Size = new System.Drawing.Size(240, 88);
            this.triggerParametersGroupBox.TabIndex = 4;
            this.triggerParametersGroupBox.TabStop = false;
            this.triggerParametersGroupBox.Text = "Trigger Parameters";
            // 
            // hysteresisNumeric
            // 
            this.hysteresisNumeric.DecimalPlaces = 2;
            this.hysteresisNumeric.Location = new System.Drawing.Point(128, 56);
            this.hysteresisNumeric.Name = "hysteresisNumeric";
            this.hysteresisNumeric.Size = new System.Drawing.Size(96, 20);
            this.hysteresisNumeric.TabIndex = 3;
            this.hysteresisNumeric.Value = new System.Decimal(new int[] {
                                                                            20,
                                                                            0,
                                                                            0,
                                                                            131072});
            // 
            // levelNumeric
            // 
            this.levelNumeric.DecimalPlaces = 2;
            this.levelNumeric.Location = new System.Drawing.Point(128, 24);
            this.levelNumeric.Name = "levelNumeric";
            this.levelNumeric.Size = new System.Drawing.Size(96, 20);
            this.levelNumeric.TabIndex = 1;
            // 
            // levelLabel
            // 
            this.levelLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.levelLabel.Location = new System.Drawing.Point(16, 26);
            this.levelLabel.Name = "levelLabel";
            this.levelLabel.Size = new System.Drawing.Size(64, 16);
            this.levelLabel.TabIndex = 0;
            this.levelLabel.Text = "Level (V):";
            // 
            // hysteresisLabel
            // 
            this.hysteresisLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.hysteresisLabel.Location = new System.Drawing.Point(16, 58);
            this.hysteresisLabel.Name = "hysteresisLabel";
            this.hysteresisLabel.Size = new System.Drawing.Size(80, 16);
            this.hysteresisLabel.TabIndex = 2;
            this.hysteresisLabel.Text = "Hysteresis (V):";
            // 
            // MainForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(386, 375);
            this.Controls.Add(this.timingParametersGroupbox);
            this.Controls.Add(this.channelParametersGroupBox);
            this.Controls.Add(this.stopButton);
            this.Controls.Add(this.startButton);
            this.Controls.Add(this.measurementLabel);
            this.Controls.Add(this.measurementListBox);
            this.Controls.Add(this.triggerParametersGroupBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Cont Acq Freq Samples - IntClk - SCXI1126";
            this.timingParametersGroupbox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.samplesToReadNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.rateNumeric)).EndInit();
            this.channelParametersGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.maximumValueNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.minimumValueNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lowPassCutoffNumeric)).EndInit();
            this.triggerParametersGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.hysteresisNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.levelNumeric)).EndInit();
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
            try
            {
                myTask = new Task();

                AIChannel myAIChannel;
                myAIChannel = myTask.AIChannels.CreateFrequencyVoltageChannel(physicalChannelComboBox.Text, "",
                    Convert.ToDouble(minimumValueNumeric.Value), Convert.ToDouble(maximumValueNumeric.Value),
                    Convert.ToDouble(levelNumeric.Value), Convert.ToDouble(hysteresisNumeric.Value),
                    AIFrequencyUnits.Hertz);

                myTask.Timing.ConfigureSampleClock("", Convert.ToDouble(rateNumeric.Value),
                    SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, 1000);

                myAIChannel.LowpassCutoffFrequency = Convert.ToDouble(lowPassCutoffNumeric.Value);
                analogCallback = new AsyncCallback(AnalogInCallback);

                runningTask = myTask;
                analogInReader = new AnalogSingleChannelReader(myTask.Stream);

                // Use SynchronizeCallbacks to specify that the object 
                // marshals callbacks across threads appropriately.
                analogInReader.SynchronizeCallbacks = true;
                analogInReader.BeginReadWaveform(Convert.ToInt32(samplesToReadNumeric.Value),
                    analogCallback, myTask);

                startButton.Enabled = false;
                stopButton.Enabled = true;

            }
            catch (DaqException exception)
            {
                MessageBox.Show(exception.Message);
                myTask.Dispose();
                startButton.Enabled = true;
                stopButton.Enabled = false;
                runningTask = null;
            }

        }
        private void stopButton_Click(object sender, System.EventArgs e)
        {
            runningTask = null;
            myTask.Dispose();
            startButton.Enabled = true;
            stopButton.Enabled = false;

        }

        private void AnalogInCallback(IAsyncResult ar)
        {
            if (runningTask != null && runningTask == ar.AsyncState)
            {
                try
                {
                    measurementListBox.BeginUpdate();
                    measurementListBox.Items.Clear();
                    int iterations;

                    // Retrieve data
                    data = analogInReader.EndReadWaveform(ar);

                    // Display only the first 10 data points in the listbox. 
                    if (data.Samples.Count < 10)
                        iterations = data.Samples.Count;
                    else
                        iterations = 10;

                    for (int i = 0; i < iterations; i++)
                    {
                        measurementListBox.Items.Add(data.Samples[i].Value);
                    }
                    measurementListBox.EndUpdate();

                    // Start next callback
                    analogInReader.BeginMemoryOptimizedReadWaveform(Convert.ToInt32(samplesToReadNumeric.Value),
                        analogCallback, myTask, data);
                }
                catch (DaqException exception)
                {
                    MessageBox.Show(exception.Message);
                    myTask.Dispose();
                    runningTask = null;
                    startButton.Enabled = true;
                    stopButton.Enabled = false;
                }
            }

        }
    }
}
