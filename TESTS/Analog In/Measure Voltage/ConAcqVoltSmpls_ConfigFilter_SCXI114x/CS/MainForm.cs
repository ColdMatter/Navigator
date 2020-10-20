/******************************************************************************
*
* Example program:
*   ConAcqVoltSmpls_ConfigFilter_SCXI114x
*
* Category:
*   AI
*
* Description:
*   This example demonstrates how to acquire and filter an analog signal using
*   the SCXI-114x.
*
* Instructions for running:
*   1.  Select the physical channel corresponding to a channel on your SCXI-114x
*       module.
*   2.  Enter the minimum and maximum voltage values.Note: For better accuracy,
*       try to match the input range to the expected voltage level of the
*       measured signal.
*   3.  Enter the sample rate for the hardware-timed acquisition. Also set the
*       number of samples to read.
*   4.  Enter the desired cutoff frequency.
*
* Steps:
*   1.  Create a new Task. Create a AIChannel object by calling the
*       CreateVoltageChannel method.
*   2.  Set the rate for the sample clock by using the
*       Timing.ConfigureSampleClock method. Additionally, define the sample mode
*       to be continuous.
*   3.  Enable the filter and set the value of the cutoff frequency by using the
*       AIChannel.LowpassEnable and AIChannel.LowpassCutoffFrequency properties.
*       The driver will choose the cutoff frequency that is closest to the
*       desired cutoff frequency.
*   4.  Call AnalogMultiChannelReader.BeginReadWaveform to install a callback
*       and begin the asynchronous read operation.
*   5.  Inside the callback, call AnalogMultiChannelReader.EndReadWaveform to
*       retrieve the data from the read operation.  
*   6.  Call AnalogMultiChannelReader.BeginMemoryOptimizedReadWaveform
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
*   Make sure your signal output terminal matches the text in physical channel
*   control. For more information on the input and output terminals for your
*   device, open the NI-DAQmx Help, and refer to the NI-DAQmx Device Terminals
*   and Device Considerations books in the table of contents.
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

namespace NationalInstruments.Examples.ConAcqVoltSmpls_ConfigFilter_SCXI114x
{
    /// <summary>
    /// Summary description for MainForm.
    /// </summary>
    public class MainForm : System.Windows.Forms.Form
    {
        private System.Windows.Forms.GroupBox channelParametersGroupBox;
        private System.Windows.Forms.Label maximumValueLabel;
        private System.Windows.Forms.Label minimumValueLabel;
        private System.Windows.Forms.Label physicalChannelLabel;
        private System.Windows.Forms.Label rateLabel;
        private System.Windows.Forms.Label samplesToReadLabel;
        private System.Windows.Forms.Label desiredCutoffFrequencyLabel;
        internal System.Windows.Forms.GroupBox dataGroupBox;
        internal System.Windows.Forms.Button stopButton;
        internal System.Windows.Forms.Button startButton;
        private System.Windows.Forms.Label actualCutoffFrequencyLabel;
        internal System.Windows.Forms.Label amplitudeLabel;
        internal System.Windows.Forms.ListBox amplitudeListBox;

        private AnalogSingleChannelReader analogInReader;
        private Task myTask;
        private int numberOfSamples;
        private Task runningTask;
        private AnalogWaveform<double> data;
        private AsyncCallback myAsyncCallback;
        private System.Windows.Forms.GroupBox filterParametersGroupBox;
        private System.Windows.Forms.NumericUpDown desiredCutoffFrequencyNumeric;
        private System.Windows.Forms.NumericUpDown rateNumeric;
        private System.Windows.Forms.NumericUpDown samplesNumeric;
        private System.Windows.Forms.TextBox actualCutoffFrequencyTextBox;
        private System.Windows.Forms.GroupBox timingParametersGroupBox;
        internal System.Windows.Forms.NumericUpDown minimumValueNumeric;
        internal System.Windows.Forms.NumericUpDown maximumValueNumeric;
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
            myAsyncCallback = new AsyncCallback(AnalogInCallback);

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
            this.channelParametersGroupBox = new System.Windows.Forms.GroupBox();
            this.physicalChannelComboBox = new System.Windows.Forms.ComboBox();
            this.minimumValueNumeric = new System.Windows.Forms.NumericUpDown();
            this.maximumValueNumeric = new System.Windows.Forms.NumericUpDown();
            this.maximumValueLabel = new System.Windows.Forms.Label();
            this.minimumValueLabel = new System.Windows.Forms.Label();
            this.physicalChannelLabel = new System.Windows.Forms.Label();
            this.timingParametersGroupBox = new System.Windows.Forms.GroupBox();
            this.samplesNumeric = new System.Windows.Forms.NumericUpDown();
            this.rateNumeric = new System.Windows.Forms.NumericUpDown();
            this.samplesToReadLabel = new System.Windows.Forms.Label();
            this.rateLabel = new System.Windows.Forms.Label();
            this.filterParametersGroupBox = new System.Windows.Forms.GroupBox();
            this.actualCutoffFrequencyTextBox = new System.Windows.Forms.TextBox();
            this.desiredCutoffFrequencyNumeric = new System.Windows.Forms.NumericUpDown();
            this.actualCutoffFrequencyLabel = new System.Windows.Forms.Label();
            this.desiredCutoffFrequencyLabel = new System.Windows.Forms.Label();
            this.dataGroupBox = new System.Windows.Forms.GroupBox();
            this.amplitudeListBox = new System.Windows.Forms.ListBox();
            this.amplitudeLabel = new System.Windows.Forms.Label();
            this.stopButton = new System.Windows.Forms.Button();
            this.startButton = new System.Windows.Forms.Button();
            this.channelParametersGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.minimumValueNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.maximumValueNumeric)).BeginInit();
            this.timingParametersGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.samplesNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.rateNumeric)).BeginInit();
            this.filterParametersGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.desiredCutoffFrequencyNumeric)).BeginInit();
            this.dataGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // channelParametersGroupBox
            // 
            this.channelParametersGroupBox.Controls.Add(this.physicalChannelComboBox);
            this.channelParametersGroupBox.Controls.Add(this.minimumValueNumeric);
            this.channelParametersGroupBox.Controls.Add(this.maximumValueNumeric);
            this.channelParametersGroupBox.Controls.Add(this.maximumValueLabel);
            this.channelParametersGroupBox.Controls.Add(this.minimumValueLabel);
            this.channelParametersGroupBox.Controls.Add(this.physicalChannelLabel);
            this.channelParametersGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.channelParametersGroupBox.Location = new System.Drawing.Point(8, 8);
            this.channelParametersGroupBox.Name = "channelParametersGroupBox";
            this.channelParametersGroupBox.Size = new System.Drawing.Size(256, 120);
            this.channelParametersGroupBox.TabIndex = 2;
            this.channelParametersGroupBox.TabStop = false;
            this.channelParametersGroupBox.Text = "Channel Parameters";
            // 
            // physicalChannelComboBox
            // 
            this.physicalChannelComboBox.Location = new System.Drawing.Point(144, 24);
            this.physicalChannelComboBox.Name = "physicalChannelComboBox";
            this.physicalChannelComboBox.Size = new System.Drawing.Size(96, 21);
            this.physicalChannelComboBox.TabIndex = 1;
            this.physicalChannelComboBox.Text = "SC1Mod1/ai0";
            // 
            // minimumValueNumeric
            // 
            this.minimumValueNumeric.DecimalPlaces = 2;
            this.minimumValueNumeric.Location = new System.Drawing.Point(144, 56);
            this.minimumValueNumeric.Maximum = new System.Decimal(new int[] {
                                                                                10,
                                                                                0,
                                                                                0,
                                                                                0});
            this.minimumValueNumeric.Minimum = new System.Decimal(new int[] {
                                                                                10,
                                                                                0,
                                                                                0,
                                                                                -2147483648});
            this.minimumValueNumeric.Name = "minimumValueNumeric";
            this.minimumValueNumeric.Size = new System.Drawing.Size(96, 20);
            this.minimumValueNumeric.TabIndex = 3;
            this.minimumValueNumeric.Value = new System.Decimal(new int[] {
                                                                              50,
                                                                              0,
                                                                              0,
                                                                              -2147418112});
            // 
            // maximumValueNumeric
            // 
            this.maximumValueNumeric.DecimalPlaces = 2;
            this.maximumValueNumeric.Location = new System.Drawing.Point(144, 88);
            this.maximumValueNumeric.Maximum = new System.Decimal(new int[] {
                                                                                10,
                                                                                0,
                                                                                0,
                                                                                0});
            this.maximumValueNumeric.Minimum = new System.Decimal(new int[] {
                                                                                10,
                                                                                0,
                                                                                0,
                                                                                -2147483648});
            this.maximumValueNumeric.Name = "maximumValueNumeric";
            this.maximumValueNumeric.Size = new System.Drawing.Size(96, 20);
            this.maximumValueNumeric.TabIndex = 5;
            this.maximumValueNumeric.Value = new System.Decimal(new int[] {
                                                                              50,
                                                                              0,
                                                                              0,
                                                                              65536});
            // 
            // maximumValueLabel
            // 
            this.maximumValueLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.maximumValueLabel.Location = new System.Drawing.Point(16, 90);
            this.maximumValueLabel.Name = "maximumValueLabel";
            this.maximumValueLabel.Size = new System.Drawing.Size(112, 16);
            this.maximumValueLabel.TabIndex = 4;
            this.maximumValueLabel.Text = "Maximum Value (V):";
            // 
            // minimumValueLabel
            // 
            this.minimumValueLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.minimumValueLabel.Location = new System.Drawing.Point(16, 58);
            this.minimumValueLabel.Name = "minimumValueLabel";
            this.minimumValueLabel.Size = new System.Drawing.Size(104, 16);
            this.minimumValueLabel.TabIndex = 2;
            this.minimumValueLabel.Text = "Minimum Value (V):";
            // 
            // physicalChannelLabel
            // 
            this.physicalChannelLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.physicalChannelLabel.Location = new System.Drawing.Point(16, 26);
            this.physicalChannelLabel.Name = "physicalChannelLabel";
            this.physicalChannelLabel.Size = new System.Drawing.Size(96, 16);
            this.physicalChannelLabel.TabIndex = 0;
            this.physicalChannelLabel.Text = "Physical Channel:";
            // 
            // timingParametersGroupBox
            // 
            this.timingParametersGroupBox.Controls.Add(this.samplesNumeric);
            this.timingParametersGroupBox.Controls.Add(this.rateNumeric);
            this.timingParametersGroupBox.Controls.Add(this.samplesToReadLabel);
            this.timingParametersGroupBox.Controls.Add(this.rateLabel);
            this.timingParametersGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.timingParametersGroupBox.Location = new System.Drawing.Point(8, 144);
            this.timingParametersGroupBox.Name = "timingParametersGroupBox";
            this.timingParametersGroupBox.Size = new System.Drawing.Size(256, 80);
            this.timingParametersGroupBox.TabIndex = 3;
            this.timingParametersGroupBox.TabStop = false;
            this.timingParametersGroupBox.Text = "Timing Parameters";
            // 
            // samplesNumeric
            // 
            this.samplesNumeric.Location = new System.Drawing.Point(144, 48);
            this.samplesNumeric.Maximum = new System.Decimal(new int[] {
                                                                           1000000,
                                                                           0,
                                                                           0,
                                                                           0});
            this.samplesNumeric.Name = "samplesNumeric";
            this.samplesNumeric.Size = new System.Drawing.Size(96, 20);
            this.samplesNumeric.TabIndex = 3;
            this.samplesNumeric.Value = new System.Decimal(new int[] {
                                                                         1000,
                                                                         0,
                                                                         0,
                                                                         0});
            // 
            // rateNumeric
            // 
            this.rateNumeric.DecimalPlaces = 2;
            this.rateNumeric.Location = new System.Drawing.Point(144, 16);
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
            // samplesToReadLabel
            // 
            this.samplesToReadLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.samplesToReadLabel.Location = new System.Drawing.Point(14, 50);
            this.samplesToReadLabel.Name = "samplesToReadLabel";
            this.samplesToReadLabel.Size = new System.Drawing.Size(98, 16);
            this.samplesToReadLabel.TabIndex = 2;
            this.samplesToReadLabel.Text = "Samples to Read:";
            // 
            // rateLabel
            // 
            this.rateLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.rateLabel.Location = new System.Drawing.Point(16, 18);
            this.rateLabel.Name = "rateLabel";
            this.rateLabel.Size = new System.Drawing.Size(56, 16);
            this.rateLabel.TabIndex = 0;
            this.rateLabel.Text = "Rate:";
            // 
            // filterParametersGroupBox
            // 
            this.filterParametersGroupBox.Controls.Add(this.actualCutoffFrequencyTextBox);
            this.filterParametersGroupBox.Controls.Add(this.desiredCutoffFrequencyNumeric);
            this.filterParametersGroupBox.Controls.Add(this.actualCutoffFrequencyLabel);
            this.filterParametersGroupBox.Controls.Add(this.desiredCutoffFrequencyLabel);
            this.filterParametersGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.filterParametersGroupBox.Location = new System.Drawing.Point(8, 240);
            this.filterParametersGroupBox.Name = "filterParametersGroupBox";
            this.filterParametersGroupBox.Size = new System.Drawing.Size(256, 88);
            this.filterParametersGroupBox.TabIndex = 4;
            this.filterParametersGroupBox.TabStop = false;
            this.filterParametersGroupBox.Text = "Trigger Parameters";
            // 
            // actualCutoffFrequencyTextBox
            // 
            this.actualCutoffFrequencyTextBox.Location = new System.Drawing.Point(144, 56);
            this.actualCutoffFrequencyTextBox.Name = "actualCutoffFrequencyTextBox";
            this.actualCutoffFrequencyTextBox.ReadOnly = true;
            this.actualCutoffFrequencyTextBox.Size = new System.Drawing.Size(96, 20);
            this.actualCutoffFrequencyTextBox.TabIndex = 3;
            this.actualCutoffFrequencyTextBox.TabStop = false;
            this.actualCutoffFrequencyTextBox.Text = "1.00";
            // 
            // desiredCutoffFrequencyNumeric
            // 
            this.desiredCutoffFrequencyNumeric.DecimalPlaces = 2;
            this.desiredCutoffFrequencyNumeric.Location = new System.Drawing.Point(144, 24);
            this.desiredCutoffFrequencyNumeric.Maximum = new System.Decimal(new int[] {
                                                                                          10000,
                                                                                          0,
                                                                                          0,
                                                                                          0});
            this.desiredCutoffFrequencyNumeric.Name = "desiredCutoffFrequencyNumeric";
            this.desiredCutoffFrequencyNumeric.Size = new System.Drawing.Size(96, 20);
            this.desiredCutoffFrequencyNumeric.TabIndex = 1;
            this.desiredCutoffFrequencyNumeric.Value = new System.Decimal(new int[] {
                                                                                        1,
                                                                                        0,
                                                                                        0,
                                                                                        0});
            // 
            // actualCutoffFrequencyLabel
            // 
            this.actualCutoffFrequencyLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.actualCutoffFrequencyLabel.Location = new System.Drawing.Point(16, 56);
            this.actualCutoffFrequencyLabel.Name = "actualCutoffFrequencyLabel";
            this.actualCutoffFrequencyLabel.Size = new System.Drawing.Size(136, 16);
            this.actualCutoffFrequencyLabel.TabIndex = 2;
            this.actualCutoffFrequencyLabel.Text = "Actual Cutoff Frequency:";
            // 
            // desiredCutoffFrequencyLabel
            // 
            this.desiredCutoffFrequencyLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.desiredCutoffFrequencyLabel.Location = new System.Drawing.Point(16, 26);
            this.desiredCutoffFrequencyLabel.Name = "desiredCutoffFrequencyLabel";
            this.desiredCutoffFrequencyLabel.Size = new System.Drawing.Size(144, 16);
            this.desiredCutoffFrequencyLabel.TabIndex = 0;
            this.desiredCutoffFrequencyLabel.Text = "Desired Cutoff Frequency:";
            // 
            // dataGroupBox
            // 
            this.dataGroupBox.Controls.Add(this.amplitudeListBox);
            this.dataGroupBox.Controls.Add(this.amplitudeLabel);
            this.dataGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.dataGroupBox.Location = new System.Drawing.Point(280, 8);
            this.dataGroupBox.Name = "dataGroupBox";
            this.dataGroupBox.Size = new System.Drawing.Size(144, 240);
            this.dataGroupBox.TabIndex = 5;
            this.dataGroupBox.TabStop = false;
            this.dataGroupBox.Text = "Data";
            // 
            // amplitudeListBox
            // 
            this.amplitudeListBox.Location = new System.Drawing.Point(8, 32);
            this.amplitudeListBox.Name = "amplitudeListBox";
            this.amplitudeListBox.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.amplitudeListBox.Size = new System.Drawing.Size(128, 199);
            this.amplitudeListBox.TabIndex = 1;
            this.amplitudeListBox.TabStop = false;
            // 
            // amplitudeLabel
            // 
            this.amplitudeLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.amplitudeLabel.Location = new System.Drawing.Point(8, 16);
            this.amplitudeLabel.Name = "amplitudeLabel";
            this.amplitudeLabel.Size = new System.Drawing.Size(72, 16);
            this.amplitudeLabel.TabIndex = 0;
            this.amplitudeLabel.Text = "Amplitude:";
            // 
            // stopButton
            // 
            this.stopButton.Enabled = false;
            this.stopButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.stopButton.Location = new System.Drawing.Point(312, 296);
            this.stopButton.Name = "stopButton";
            this.stopButton.Size = new System.Drawing.Size(80, 32);
            this.stopButton.TabIndex = 1;
            this.stopButton.Text = "Stop";
            this.stopButton.Click += new System.EventHandler(this.stopButton_Click);
            // 
            // startButton
            // 
            this.startButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.startButton.Location = new System.Drawing.Point(312, 256);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(80, 32);
            this.startButton.TabIndex = 0;
            this.startButton.Text = "Start";
            this.startButton.Click += new System.EventHandler(this.startButton_Click);
            // 
            // MainForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(434, 336);
            this.Controls.Add(this.stopButton);
            this.Controls.Add(this.startButton);
            this.Controls.Add(this.dataGroupBox);
            this.Controls.Add(this.channelParametersGroupBox);
            this.Controls.Add(this.filterParametersGroupBox);
            this.Controls.Add(this.timingParametersGroupBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Continuous Acqusition - Configurable Filter - SCXI114x";
            this.channelParametersGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.minimumValueNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.maximumValueNumeric)).EndInit();
            this.timingParametersGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.samplesNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.rateNumeric)).EndInit();
            this.filterParametersGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.desiredCutoffFrequencyNumeric)).EndInit();
            this.dataGroupBox.ResumeLayout(false);
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
            amplitudeListBox.Items.Clear();
            numberOfSamples = Convert.ToInt32(samplesNumeric.Value);
            try
            {
                myTask = new Task();

                AIChannel myChannel = myTask.AIChannels.CreateVoltageChannel(physicalChannelComboBox.Text, "",
                (AITerminalConfiguration)(-1), Convert.ToDouble(minimumValueNumeric.Value),
                Convert.ToDouble(maximumValueNumeric.Value), AIVoltageUnits.Volts);

                myTask.Timing.ConfigureSampleClock("", Convert.ToDouble(rateNumeric.Value),
                    SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, 1000);

                myChannel.LowpassEnable = true;
                myChannel.LowpassCutoffFrequency = Convert.ToDouble(desiredCutoffFrequencyNumeric.Value);
                myTask.Control(TaskAction.Verify);
                actualCutoffFrequencyTextBox.Text = myChannel.LowpassCutoffFrequency.ToString("f2");

                runningTask = myTask;

                analogInReader = new AnalogSingleChannelReader(myTask.Stream);

                // Use SynchronizeCallbacks to specify that the object 
                // marshals callbacks across threads appropriately.
                analogInReader.SynchronizeCallbacks = true;
                analogInReader.BeginReadWaveform(numberOfSamples, myAsyncCallback, myTask);    

                startButton.Enabled = false;
                stopButton.Enabled = true;
            }
            catch (DaqException ex)
            {
                MessageBox.Show(ex.Message);
                myTask.Dispose();
                runningTask = null;
            }
        }

        private void stopButton_Click(object sender, System.EventArgs e)
        {
            myTask.Dispose();
            runningTask = null;
            startButton.Enabled = true;
            stopButton.Enabled = false;
        }

        private void AnalogInCallback(IAsyncResult ar)
        {
            try
            {
                if (runningTask != null && runningTask == ar.AsyncState)
                {
                    int iteration;

                    data = analogInReader.EndReadWaveform(ar);

                    // Display only the first 15 data points in the listbox. 
                    if (data.Samples.Count < 15)
                        iteration = data.Samples.Count;
                    else
                        iteration = 15;

                    amplitudeListBox.BeginUpdate();
                    amplitudeListBox.Items.Clear();

                    for (int i = 0; i < iteration; i++)
                    {
                        amplitudeListBox.Items.Add(data.Samples[i].Value);
                    }
                    amplitudeListBox.EndUpdate();

                    analogInReader.BeginMemoryOptimizedReadWaveform(numberOfSamples, myAsyncCallback, myTask, data);
                }
            }
            catch (DaqException exception)
            {
                MessageBox.Show(exception.Message);
                stopButton_Click(null, null);
            }

        }
    }
}
