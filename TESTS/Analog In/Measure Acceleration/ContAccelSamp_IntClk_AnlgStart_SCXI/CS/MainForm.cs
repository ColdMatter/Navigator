/******************************************************************************
*
* Example program:
*   ContAccelSamp_IntClk_AnlgStart_SCXI
*
* Category:
*   AI
*
* Description:
*   This example demonstrates how to make continuous, hardware-timed
*   acceleration measurements using an SCXI-153x module.
*
* Instructions for running:
*   1.  Specify the physical channel where you have connected the accelerometer.
*   2.  Enter the minimum and maximum distance values, in units (based on the
*       units control) that you expect to measure. A smaller range will allow a
*       more accurate measurement.
*   3.  Select the number of samples to acquire.
*   4.  Set the rate of the acquisition.
*   5.  Set the source of the start trigger. By default this is APFI0.  APFI0 is
*       the default Analog Trigger pin for M Series devices.  Please refer to
*       your device documentation for information regarding valid Analog
*       Triggers for your device.
*   6.  Set the slope and level of desired analog edge condition.
*   7.  Enter the sensitivity and the sensitivity units for the accelerometer
*       being used.
*   8.  Specify the cutoff frequency of the filter and the excitation settings.
*   9.  The SCXI-153x can tie the negative terminal to chassis ground by
*       selecting reference single ended(RSE) mode.  Select differential if you
*       do not want the chassis ground to be connected.
*
* Steps:
*   1.  Create a new analog input task.
*   2.  Create an analog input accelerometer channel.
*   3.  Set the rate for the sample clock.  Additionally, define the sample mode
*       to be continuous.
*   4.  Setup the timing for the acquisition.  In this example we use the DAQ
*       device's internal clock to take a continuous number of samples.
*   5.  Create an AnalogMultiChannelReader and associate it with the task by
*       using the task's stream. Call AnalogMultiChannelReader.BeginReadWaveform
*       to install a callback and begin the asynchronous read operation.
*   6.  Inside the callback, call AnalogMultiChannelReader.EndReadWaveform to
*       retrieve the data from the read operation.    In the callback, display
*       the data.
*   7.  Call AnalogMultiChannelReader.BeginMemoryOptimizedReadWaveform
*   8.  When the user presses the stop button, stop the task by calling
*       Task.Stop().  Dispose the Task object to clean-up any resources
*       associated with the task.
*   9.  Handle any DaqExceptions, if they occur.
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
*   Connect your accelerometer to the terminals corresponding to the physical
*   channel control value. Also, make sure the analog trigger terminal matches
*   the trigger source control. For more information on the input and output
*   terminals for your device, open the NI-DAQmx Help, and refer to the NI-DAQmx
*   Device Terminals and Device Considerations books in the table of contents.
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


namespace NationalInstruments.Examples.ContAcqAccelSamp_SCXI
{
    /// <summary>
    /// Summary description for MainForm.
    /// </summary>
    public class MainForm : System.Windows.Forms.Form
    {
        private System.Windows.Forms.GroupBox channelParametersGroupBox;
        private System.Windows.Forms.Label maximumLabel;
        private System.Windows.Forms.Label minimumLabel;
        private System.Windows.Forms.Label physicalChannelLabel;
        private System.Windows.Forms.GroupBox timingParametersGroupBox;
        private System.Windows.Forms.Label samplesLabel;
        private System.Windows.Forms.Label rateLabel;
        private System.Windows.Forms.GroupBox acquisitionResultsGroupBox;
        private System.Windows.Forms.Label resultLabel;
        private System.Windows.Forms.DataGrid acquisitionDataGrid;
        private System.Windows.Forms.Button stopButton;
        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.GroupBox triggerParametersGroupBox;
        private System.Windows.Forms.TextBox triggerSourceTextBox;
        private System.Windows.Forms.Label sensitivityLabel;
        private System.Windows.Forms.Label sensitivityUnitLabel;
        private System.Windows.Forms.ComboBox sensitivityUnitComboBox;

        private AnalogMultiChannelReader analogInReader;
        private AIExcitationSource excitationSource;
        private AIAccelerometerSensitivityUnits sensitivityUnits;
        private AITerminalConfiguration terminalConfiguration;  
        private AnalogEdgeStartTriggerSlope triggerSlope;
        
        private Task myTask;
        private Task runningTask;
        private AsyncCallback analogCallback;
        private AnalogWaveform<double>[] data;
        private DataColumn[] dataColumn = null;
        private DataTable dataTable = null;
        private System.Windows.Forms.NumericUpDown rateNumeric;
        private System.Windows.Forms.NumericUpDown samplesPerChannelNumeric;
        private System.Windows.Forms.NumericUpDown triggerLevelNumeric;
        private System.Windows.Forms.NumericUpDown sensitivityNumeric;
        private System.Windows.Forms.GroupBox accelerometerParametersGroupBox;
        private System.Windows.Forms.Label triggerLevelLabel;
        private System.Windows.Forms.Label triggerSlopeLabel;
        private System.Windows.Forms.Label triggerSourceLabel;
        private System.Windows.Forms.ComboBox triggerSlopeComboBox;
        private System.Windows.Forms.NumericUpDown excitationValueNumeric;
        private System.Windows.Forms.NumericUpDown lowpassCutoffFrequencyNumeric;
        private System.Windows.Forms.ComboBox excitationSourceComboBox;
        private System.Windows.Forms.ComboBox terminalConfigurationComboBox;
        private System.Windows.Forms.Label excitationValueLabel;
        private System.Windows.Forms.Label excitationSourceLabel;
        private System.Windows.Forms.GroupBox deviceParametersGroupBox;
        private System.Windows.Forms.Label lowpassCutoffFrequencyLabel;
        private System.Windows.Forms.Label terminalConfigurationLabel;
        internal System.Windows.Forms.NumericUpDown minimumValueNumeric;
        internal System.Windows.Forms.NumericUpDown maximumValueNumeric;
        private System.Windows.Forms.Label triggerSourceInfo;
        private System.Windows.Forms.Label triggerSourceInfoAsterisk;
        private System.Windows.Forms.ComboBox physicalChannelComboBox;
        private System.Windows.Forms.Label hysteresisLabel;
        private System.Windows.Forms.NumericUpDown hysteresisNumeric;
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
            stopButton.Enabled = false;
            dataTable = new DataTable();
            triggerSlopeComboBox.SelectedIndex = 0;
            sensitivityUnitComboBox.SelectedIndex = 0;
            excitationSourceComboBox.SelectedIndex = 0;
            terminalConfigurationComboBox.SelectedIndex = 2;

            physicalChannelComboBox.Items.AddRange(DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AI, PhysicalChannelAccess.External));
            if (physicalChannelComboBox.Items.Count > 0)
                physicalChannelComboBox.SelectedIndex = 0;
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
                if (myTask != null)
                {
                    runningTask = null;
                    myTask.Dispose();
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
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(MainForm));
            this.triggerParametersGroupBox = new System.Windows.Forms.GroupBox();
            this.triggerSourceInfoAsterisk = new System.Windows.Forms.Label();
            this.triggerLevelNumeric = new System.Windows.Forms.NumericUpDown();
            this.triggerLevelLabel = new System.Windows.Forms.Label();
            this.triggerSlopeLabel = new System.Windows.Forms.Label();
            this.triggerSourceLabel = new System.Windows.Forms.Label();
            this.triggerSourceTextBox = new System.Windows.Forms.TextBox();
            this.triggerSlopeComboBox = new System.Windows.Forms.ComboBox();
            this.triggerSourceInfo = new System.Windows.Forms.Label();
            this.accelerometerParametersGroupBox = new System.Windows.Forms.GroupBox();
            this.sensitivityNumeric = new System.Windows.Forms.NumericUpDown();
            this.sensitivityUnitComboBox = new System.Windows.Forms.ComboBox();
            this.sensitivityUnitLabel = new System.Windows.Forms.Label();
            this.sensitivityLabel = new System.Windows.Forms.Label();
            this.channelParametersGroupBox = new System.Windows.Forms.GroupBox();
            this.physicalChannelComboBox = new System.Windows.Forms.ComboBox();
            this.minimumValueNumeric = new System.Windows.Forms.NumericUpDown();
            this.maximumValueNumeric = new System.Windows.Forms.NumericUpDown();
            this.maximumLabel = new System.Windows.Forms.Label();
            this.minimumLabel = new System.Windows.Forms.Label();
            this.physicalChannelLabel = new System.Windows.Forms.Label();
            this.timingParametersGroupBox = new System.Windows.Forms.GroupBox();
            this.samplesPerChannelNumeric = new System.Windows.Forms.NumericUpDown();
            this.rateNumeric = new System.Windows.Forms.NumericUpDown();
            this.samplesLabel = new System.Windows.Forms.Label();
            this.rateLabel = new System.Windows.Forms.Label();
            this.acquisitionResultsGroupBox = new System.Windows.Forms.GroupBox();
            this.resultLabel = new System.Windows.Forms.Label();
            this.acquisitionDataGrid = new System.Windows.Forms.DataGrid();
            this.stopButton = new System.Windows.Forms.Button();
            this.startButton = new System.Windows.Forms.Button();
            this.excitationValueNumeric = new System.Windows.Forms.NumericUpDown();
            this.lowpassCutoffFrequencyNumeric = new System.Windows.Forms.NumericUpDown();
            this.excitationSourceComboBox = new System.Windows.Forms.ComboBox();
            this.terminalConfigurationComboBox = new System.Windows.Forms.ComboBox();
            this.excitationValueLabel = new System.Windows.Forms.Label();
            this.excitationSourceLabel = new System.Windows.Forms.Label();
            this.terminalConfigurationLabel = new System.Windows.Forms.Label();
            this.lowpassCutoffFrequencyLabel = new System.Windows.Forms.Label();
            this.deviceParametersGroupBox = new System.Windows.Forms.GroupBox();
            this.hysteresisLabel = new System.Windows.Forms.Label();
            this.hysteresisNumeric = new System.Windows.Forms.NumericUpDown();
            this.triggerParametersGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.triggerLevelNumeric)).BeginInit();
            this.accelerometerParametersGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.sensitivityNumeric)).BeginInit();
            this.channelParametersGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.minimumValueNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.maximumValueNumeric)).BeginInit();
            this.timingParametersGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.samplesPerChannelNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.rateNumeric)).BeginInit();
            this.acquisitionResultsGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.acquisitionDataGrid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.excitationValueNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lowpassCutoffFrequencyNumeric)).BeginInit();
            this.deviceParametersGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.hysteresisNumeric)).BeginInit();
            this.SuspendLayout();
            // 
            // triggerParametersGroupBox
            // 
            this.triggerParametersGroupBox.Controls.Add(this.triggerSourceInfoAsterisk);
            this.triggerParametersGroupBox.Controls.Add(this.triggerLevelNumeric);
            this.triggerParametersGroupBox.Controls.Add(this.triggerLevelLabel);
            this.triggerParametersGroupBox.Controls.Add(this.triggerSlopeLabel);
            this.triggerParametersGroupBox.Controls.Add(this.triggerSourceLabel);
            this.triggerParametersGroupBox.Controls.Add(this.triggerSourceTextBox);
            this.triggerParametersGroupBox.Controls.Add(this.triggerSlopeComboBox);
            this.triggerParametersGroupBox.Controls.Add(this.triggerSourceInfo);
            this.triggerParametersGroupBox.Controls.Add(this.hysteresisLabel);
            this.triggerParametersGroupBox.Controls.Add(this.hysteresisNumeric);
            this.triggerParametersGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.triggerParametersGroupBox.Location = new System.Drawing.Point(8, 232);
            this.triggerParametersGroupBox.Name = "triggerParametersGroupBox";
            this.triggerParametersGroupBox.Size = new System.Drawing.Size(232, 224);
            this.triggerParametersGroupBox.TabIndex = 4;
            this.triggerParametersGroupBox.TabStop = false;
            this.triggerParametersGroupBox.Text = "Trigger Parameters";
            // 
            // triggerSourceInfoAsterisk
            // 
            this.triggerSourceInfoAsterisk.Location = new System.Drawing.Point(16, 144);
            this.triggerSourceInfoAsterisk.Name = "triggerSourceInfoAsterisk";
            this.triggerSourceInfoAsterisk.Size = new System.Drawing.Size(8, 23);
            this.triggerSourceInfoAsterisk.TabIndex = 6;
            this.triggerSourceInfoAsterisk.Text = "*";
            // 
            // triggerLevelNumeric
            // 
            this.triggerLevelNumeric.DecimalPlaces = 2;
            this.triggerLevelNumeric.Location = new System.Drawing.Point(120, 88);
            this.triggerLevelNumeric.Maximum = new System.Decimal(new int[] {
                                                                                10,
                                                                                0,
                                                                                0,
                                                                                0});
            this.triggerLevelNumeric.Minimum = new System.Decimal(new int[] {
                                                                                10,
                                                                                0,
                                                                                0,
                                                                                -2147483648});
            this.triggerLevelNumeric.Name = "triggerLevelNumeric";
            this.triggerLevelNumeric.Size = new System.Drawing.Size(96, 20);
            this.triggerLevelNumeric.TabIndex = 5;
            // 
            // triggerLevelLabel
            // 
            this.triggerLevelLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.triggerLevelLabel.Location = new System.Drawing.Point(16, 90);
            this.triggerLevelLabel.Name = "triggerLevelLabel";
            this.triggerLevelLabel.Size = new System.Drawing.Size(96, 16);
            this.triggerLevelLabel.TabIndex = 4;
            this.triggerLevelLabel.Text = "Trigger Level (g):";
            // 
            // triggerSlopeLabel
            // 
            this.triggerSlopeLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.triggerSlopeLabel.Location = new System.Drawing.Point(16, 58);
            this.triggerSlopeLabel.Name = "triggerSlopeLabel";
            this.triggerSlopeLabel.Size = new System.Drawing.Size(88, 16);
            this.triggerSlopeLabel.TabIndex = 2;
            this.triggerSlopeLabel.Text = "Trigger Slope:";
            // 
            // triggerSourceLabel
            // 
            this.triggerSourceLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.triggerSourceLabel.Location = new System.Drawing.Point(16, 26);
            this.triggerSourceLabel.Name = "triggerSourceLabel";
            this.triggerSourceLabel.Size = new System.Drawing.Size(88, 16);
            this.triggerSourceLabel.TabIndex = 0;
            this.triggerSourceLabel.Text = "Trigger Source:*";
            // 
            // triggerSourceTextBox
            // 
            this.triggerSourceTextBox.Location = new System.Drawing.Point(120, 24);
            this.triggerSourceTextBox.Name = "triggerSourceTextBox";
            this.triggerSourceTextBox.Size = new System.Drawing.Size(96, 20);
            this.triggerSourceTextBox.TabIndex = 1;
            this.triggerSourceTextBox.Text = "APFI0";
            // 
            // triggerSlopeComboBox
            // 
            this.triggerSlopeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.triggerSlopeComboBox.Items.AddRange(new object[] {
                                                                      "Rising",
                                                                      "Falling"});
            this.triggerSlopeComboBox.Location = new System.Drawing.Point(120, 56);
            this.triggerSlopeComboBox.Name = "triggerSlopeComboBox";
            this.triggerSlopeComboBox.Size = new System.Drawing.Size(96, 21);
            this.triggerSlopeComboBox.TabIndex = 3;
            // 
            // triggerSourceInfo
            // 
            this.triggerSourceInfo.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.triggerSourceInfo.Location = new System.Drawing.Point(24, 144);
            this.triggerSourceInfo.Name = "triggerSourceInfo";
            this.triggerSourceInfo.Size = new System.Drawing.Size(192, 64);
            this.triggerSourceInfo.TabIndex = 7;
            this.triggerSourceInfo.Text = "APFI0 is the default Analog Trigger pin for M Series devices.  Please refer to you" +
                "r device documentation for information regarding valid Analog Triggers for your " +
                "device.";
            // 
            // accelerometerParametersGroupBox
            // 
            this.accelerometerParametersGroupBox.Controls.Add(this.sensitivityNumeric);
            this.accelerometerParametersGroupBox.Controls.Add(this.sensitivityUnitComboBox);
            this.accelerometerParametersGroupBox.Controls.Add(this.sensitivityUnitLabel);
            this.accelerometerParametersGroupBox.Controls.Add(this.sensitivityLabel);
            this.accelerometerParametersGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.accelerometerParametersGroupBox.Location = new System.Drawing.Point(8, 464);
            this.accelerometerParametersGroupBox.Name = "accelerometerParametersGroupBox";
            this.accelerometerParametersGroupBox.Size = new System.Drawing.Size(232, 88);
            this.accelerometerParametersGroupBox.TabIndex = 5;
            this.accelerometerParametersGroupBox.TabStop = false;
            this.accelerometerParametersGroupBox.Text = "Accelerometer Parameters";
            // 
            // sensitivityNumeric
            // 
            this.sensitivityNumeric.DecimalPlaces = 2;
            this.sensitivityNumeric.Location = new System.Drawing.Point(120, 24);
            this.sensitivityNumeric.Maximum = new System.Decimal(new int[] {
                                                                               1000,
                                                                               0,
                                                                               0,
                                                                               0});
            this.sensitivityNumeric.Name = "sensitivityNumeric";
            this.sensitivityNumeric.Size = new System.Drawing.Size(96, 20);
            this.sensitivityNumeric.TabIndex = 1;
            this.sensitivityNumeric.Value = new System.Decimal(new int[] {
                                                                             50,
                                                                             0,
                                                                             0,
                                                                             0});
            // 
            // sensitivityUnitComboBox
            // 
            this.sensitivityUnitComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.sensitivityUnitComboBox.Items.AddRange(new object[] {
                                                                         "mVolts/G",
                                                                         "Volts/G"});
            this.sensitivityUnitComboBox.Location = new System.Drawing.Point(120, 56);
            this.sensitivityUnitComboBox.Name = "sensitivityUnitComboBox";
            this.sensitivityUnitComboBox.Size = new System.Drawing.Size(96, 21);
            this.sensitivityUnitComboBox.TabIndex = 3;
            // 
            // sensitivityUnitLabel
            // 
            this.sensitivityUnitLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.sensitivityUnitLabel.Location = new System.Drawing.Point(16, 58);
            this.sensitivityUnitLabel.Name = "sensitivityUnitLabel";
            this.sensitivityUnitLabel.Size = new System.Drawing.Size(88, 16);
            this.sensitivityUnitLabel.TabIndex = 2;
            this.sensitivityUnitLabel.Text = "Sensitivity Units:";
            // 
            // sensitivityLabel
            // 
            this.sensitivityLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.sensitivityLabel.Location = new System.Drawing.Point(16, 26);
            this.sensitivityLabel.Name = "sensitivityLabel";
            this.sensitivityLabel.Size = new System.Drawing.Size(88, 16);
            this.sensitivityLabel.TabIndex = 0;
            this.sensitivityLabel.Text = "Sensitivity:";
            // 
            // channelParametersGroupBox
            // 
            this.channelParametersGroupBox.Controls.Add(this.physicalChannelComboBox);
            this.channelParametersGroupBox.Controls.Add(this.minimumValueNumeric);
            this.channelParametersGroupBox.Controls.Add(this.maximumValueNumeric);
            this.channelParametersGroupBox.Controls.Add(this.maximumLabel);
            this.channelParametersGroupBox.Controls.Add(this.minimumLabel);
            this.channelParametersGroupBox.Controls.Add(this.physicalChannelLabel);
            this.channelParametersGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.channelParametersGroupBox.Location = new System.Drawing.Point(8, 8);
            this.channelParametersGroupBox.Name = "channelParametersGroupBox";
            this.channelParametersGroupBox.Size = new System.Drawing.Size(232, 120);
            this.channelParametersGroupBox.TabIndex = 2;
            this.channelParametersGroupBox.TabStop = false;
            this.channelParametersGroupBox.Text = "Channel Parameters";
            // 
            // physicalChannelComboBox
            // 
            this.physicalChannelComboBox.Location = new System.Drawing.Point(120, 24);
            this.physicalChannelComboBox.Name = "physicalChannelComboBox";
            this.physicalChannelComboBox.Size = new System.Drawing.Size(96, 21);
            this.physicalChannelComboBox.TabIndex = 1;
            this.physicalChannelComboBox.Text = "SC1Mod1/ai0";
            // 
            // minimumValueNumeric
            // 
            this.minimumValueNumeric.DecimalPlaces = 2;
            this.minimumValueNumeric.Location = new System.Drawing.Point(120, 56);
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
                                                                              100,
                                                                              0,
                                                                              0,
                                                                              -2147418112});
            // 
            // maximumValueNumeric
            // 
            this.maximumValueNumeric.DecimalPlaces = 2;
            this.maximumValueNumeric.Location = new System.Drawing.Point(120, 88);
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
                                                                              100,
                                                                              0,
                                                                              0,
                                                                              65536});
            // 
            // maximumLabel
            // 
            this.maximumLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.maximumLabel.Location = new System.Drawing.Point(16, 90);
            this.maximumLabel.Name = "maximumLabel";
            this.maximumLabel.Size = new System.Drawing.Size(112, 16);
            this.maximumLabel.TabIndex = 4;
            this.maximumLabel.Text = "Maximum Value (g):";
            // 
            // minimumLabel
            // 
            this.minimumLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.minimumLabel.Location = new System.Drawing.Point(16, 57);
            this.minimumLabel.Name = "minimumLabel";
            this.minimumLabel.Size = new System.Drawing.Size(104, 18);
            this.minimumLabel.TabIndex = 2;
            this.minimumLabel.Text = "Minimum Value (g):";
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
            this.timingParametersGroupBox.Controls.Add(this.samplesPerChannelNumeric);
            this.timingParametersGroupBox.Controls.Add(this.rateNumeric);
            this.timingParametersGroupBox.Controls.Add(this.samplesLabel);
            this.timingParametersGroupBox.Controls.Add(this.rateLabel);
            this.timingParametersGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.timingParametersGroupBox.Location = new System.Drawing.Point(8, 136);
            this.timingParametersGroupBox.Name = "timingParametersGroupBox";
            this.timingParametersGroupBox.Size = new System.Drawing.Size(232, 88);
            this.timingParametersGroupBox.TabIndex = 3;
            this.timingParametersGroupBox.TabStop = false;
            this.timingParametersGroupBox.Text = "Timing Parameters";
            // 
            // samplesPerChannelNumeric
            // 
            this.samplesPerChannelNumeric.Location = new System.Drawing.Point(120, 56);
            this.samplesPerChannelNumeric.Maximum = new System.Decimal(new int[] {
                                                                                     1000000,
                                                                                     0,
                                                                                     0,
                                                                                     0});
            this.samplesPerChannelNumeric.Name = "samplesPerChannelNumeric";
            this.samplesPerChannelNumeric.Size = new System.Drawing.Size(96, 20);
            this.samplesPerChannelNumeric.TabIndex = 3;
            this.samplesPerChannelNumeric.Value = new System.Decimal(new int[] {
                                                                                   1000,
                                                                                   0,
                                                                                   0,
                                                                                   0});
            // 
            // rateNumeric
            // 
            this.rateNumeric.DecimalPlaces = 2;
            this.rateNumeric.Location = new System.Drawing.Point(120, 24);
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
            // samplesLabel
            // 
            this.samplesLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.samplesLabel.Location = new System.Drawing.Point(16, 58);
            this.samplesLabel.Name = "samplesLabel";
            this.samplesLabel.Size = new System.Drawing.Size(104, 16);
            this.samplesLabel.TabIndex = 2;
            this.samplesLabel.Text = "Samples / Channel:";
            // 
            // rateLabel
            // 
            this.rateLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.rateLabel.Location = new System.Drawing.Point(16, 26);
            this.rateLabel.Name = "rateLabel";
            this.rateLabel.Size = new System.Drawing.Size(56, 16);
            this.rateLabel.TabIndex = 0;
            this.rateLabel.Text = "Rate (Hz):";
            // 
            // acquisitionResultsGroupBox
            // 
            this.acquisitionResultsGroupBox.Controls.Add(this.resultLabel);
            this.acquisitionResultsGroupBox.Controls.Add(this.acquisitionDataGrid);
            this.acquisitionResultsGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.acquisitionResultsGroupBox.Location = new System.Drawing.Point(256, 168);
            this.acquisitionResultsGroupBox.Name = "acquisitionResultsGroupBox";
            this.acquisitionResultsGroupBox.Size = new System.Drawing.Size(264, 304);
            this.acquisitionResultsGroupBox.TabIndex = 7;
            this.acquisitionResultsGroupBox.TabStop = false;
            this.acquisitionResultsGroupBox.Text = "Acquisition Results";
            // 
            // resultLabel
            // 
            this.resultLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.resultLabel.Location = new System.Drawing.Point(16, 16);
            this.resultLabel.Name = "resultLabel";
            this.resultLabel.Size = new System.Drawing.Size(112, 16);
            this.resultLabel.TabIndex = 0;
            this.resultLabel.Text = "Acquisition Data:";
            // 
            // acquisitionDataGrid
            // 
            this.acquisitionDataGrid.AllowSorting = false;
            this.acquisitionDataGrid.DataMember = "";
            this.acquisitionDataGrid.HeaderForeColor = System.Drawing.SystemColors.ControlText;
            this.acquisitionDataGrid.Location = new System.Drawing.Point(16, 32);
            this.acquisitionDataGrid.Name = "acquisitionDataGrid";
            this.acquisitionDataGrid.ParentRowsVisible = false;
            this.acquisitionDataGrid.ReadOnly = true;
            this.acquisitionDataGrid.Size = new System.Drawing.Size(240, 256);
            this.acquisitionDataGrid.TabIndex = 1;
            this.acquisitionDataGrid.TabStop = false;
            // 
            // stopButton
            // 
            this.stopButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.stopButton.Location = new System.Drawing.Point(400, 496);
            this.stopButton.Name = "stopButton";
            this.stopButton.Size = new System.Drawing.Size(80, 25);
            this.stopButton.TabIndex = 1;
            this.stopButton.Text = "Stop";
            this.stopButton.Click += new System.EventHandler(this.stopButton_Click);
            // 
            // startButton
            // 
            this.startButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.startButton.Location = new System.Drawing.Point(296, 496);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(80, 25);
            this.startButton.TabIndex = 0;
            this.startButton.Text = "Start";
            this.startButton.Click += new System.EventHandler(this.startButton_Click);
            // 
            // excitationValueNumeric
            // 
            this.excitationValueNumeric.DecimalPlaces = 3;
            this.excitationValueNumeric.Location = new System.Drawing.Point(160, 120);
            this.excitationValueNumeric.Name = "excitationValueNumeric";
            this.excitationValueNumeric.Size = new System.Drawing.Size(96, 20);
            this.excitationValueNumeric.TabIndex = 7;
            this.excitationValueNumeric.Value = new System.Decimal(new int[] {
                                                                                 4,
                                                                                 0,
                                                                                 0,
                                                                                 196608});
            // 
            // lowpassCutoffFrequencyNumeric
            // 
            this.lowpassCutoffFrequencyNumeric.Location = new System.Drawing.Point(160, 24);
            this.lowpassCutoffFrequencyNumeric.Maximum = new System.Decimal(new int[] {
                                                                                          1000000,
                                                                                          0,
                                                                                          0,
                                                                                          0});
            this.lowpassCutoffFrequencyNumeric.Name = "lowpassCutoffFrequencyNumeric";
            this.lowpassCutoffFrequencyNumeric.Size = new System.Drawing.Size(96, 20);
            this.lowpassCutoffFrequencyNumeric.TabIndex = 1;
            this.lowpassCutoffFrequencyNumeric.Value = new System.Decimal(new int[] {
                                                                                        20000,
                                                                                        0,
                                                                                        0,
                                                                                        0});
            // 
            // excitationSourceComboBox
            // 
            this.excitationSourceComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.excitationSourceComboBox.ItemHeight = 13;
            this.excitationSourceComboBox.Items.AddRange(new object[] {
                                                                          "Internal",
                                                                          "External",
                                                                          "None"});
            this.excitationSourceComboBox.Location = new System.Drawing.Point(160, 88);
            this.excitationSourceComboBox.Name = "excitationSourceComboBox";
            this.excitationSourceComboBox.Size = new System.Drawing.Size(96, 21);
            this.excitationSourceComboBox.TabIndex = 5;
            // 
            // terminalConfigurationComboBox
            // 
            this.terminalConfigurationComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.terminalConfigurationComboBox.ItemHeight = 13;
            this.terminalConfigurationComboBox.Items.AddRange(new object[] {
                                                                               "Rse",
                                                                               "Nrse",
                                                                               "Differential"});
            this.terminalConfigurationComboBox.Location = new System.Drawing.Point(160, 56);
            this.terminalConfigurationComboBox.Name = "terminalConfigurationComboBox";
            this.terminalConfigurationComboBox.Size = new System.Drawing.Size(96, 21);
            this.terminalConfigurationComboBox.TabIndex = 3;
            // 
            // excitationValueLabel
            // 
            this.excitationValueLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.excitationValueLabel.Location = new System.Drawing.Point(16, 122);
            this.excitationValueLabel.Name = "excitationValueLabel";
            this.excitationValueLabel.Size = new System.Drawing.Size(128, 16);
            this.excitationValueLabel.TabIndex = 6;
            this.excitationValueLabel.Text = "Excitation Value:";
            // 
            // excitationSourceLabel
            // 
            this.excitationSourceLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.excitationSourceLabel.Location = new System.Drawing.Point(16, 90);
            this.excitationSourceLabel.Name = "excitationSourceLabel";
            this.excitationSourceLabel.Size = new System.Drawing.Size(128, 16);
            this.excitationSourceLabel.TabIndex = 4;
            this.excitationSourceLabel.Text = "Excitation Source:";
            // 
            // terminalConfigurationLabel
            // 
            this.terminalConfigurationLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.terminalConfigurationLabel.Location = new System.Drawing.Point(16, 59);
            this.terminalConfigurationLabel.Name = "terminalConfigurationLabel";
            this.terminalConfigurationLabel.Size = new System.Drawing.Size(136, 14);
            this.terminalConfigurationLabel.TabIndex = 2;
            this.terminalConfigurationLabel.Text = "Terminal Configuration:";
            // 
            // lowpassCutoffFrequencyLabel
            // 
            this.lowpassCutoffFrequencyLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.lowpassCutoffFrequencyLabel.Location = new System.Drawing.Point(16, 26);
            this.lowpassCutoffFrequencyLabel.Name = "lowpassCutoffFrequencyLabel";
            this.lowpassCutoffFrequencyLabel.Size = new System.Drawing.Size(144, 16);
            this.lowpassCutoffFrequencyLabel.TabIndex = 0;
            this.lowpassCutoffFrequencyLabel.Text = "Lowpass Cutoff Frequency:";
            // 
            // deviceParametersGroupBox
            // 
            this.deviceParametersGroupBox.Controls.Add(this.excitationValueNumeric);
            this.deviceParametersGroupBox.Controls.Add(this.lowpassCutoffFrequencyNumeric);
            this.deviceParametersGroupBox.Controls.Add(this.excitationSourceComboBox);
            this.deviceParametersGroupBox.Controls.Add(this.terminalConfigurationComboBox);
            this.deviceParametersGroupBox.Controls.Add(this.excitationValueLabel);
            this.deviceParametersGroupBox.Controls.Add(this.excitationSourceLabel);
            this.deviceParametersGroupBox.Controls.Add(this.terminalConfigurationLabel);
            this.deviceParametersGroupBox.Controls.Add(this.lowpassCutoffFrequencyLabel);
            this.deviceParametersGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.deviceParametersGroupBox.Location = new System.Drawing.Point(256, 8);
            this.deviceParametersGroupBox.Name = "deviceParametersGroupBox";
            this.deviceParametersGroupBox.Size = new System.Drawing.Size(264, 152);
            this.deviceParametersGroupBox.TabIndex = 6;
            this.deviceParametersGroupBox.TabStop = false;
            this.deviceParametersGroupBox.Text = "Device Parameters";
            // 
            // hysteresisLabel
            // 
            this.hysteresisLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.hysteresisLabel.Location = new System.Drawing.Point(16, 120);
            this.hysteresisLabel.Name = "hysteresisLabel";
            this.hysteresisLabel.Size = new System.Drawing.Size(96, 16);
            this.hysteresisLabel.TabIndex = 4;
            this.hysteresisLabel.Text = "Hysteresis (g):";
            // 
            // hysteresisNumeric
            // 
            this.hysteresisNumeric.DecimalPlaces = 2;
            this.hysteresisNumeric.Location = new System.Drawing.Point(120, 120);
            this.hysteresisNumeric.Maximum = new System.Decimal(new int[] {
                                                                              10,
                                                                              0,
                                                                              0,
                                                                              0});
            this.hysteresisNumeric.Minimum = new System.Decimal(new int[] {
                                                                              10,
                                                                              0,
                                                                              0,
                                                                              -2147483648});
            this.hysteresisNumeric.Name = "hysteresisNumeric";
            this.hysteresisNumeric.Size = new System.Drawing.Size(96, 20);
            this.hysteresisNumeric.TabIndex = 5;
            // 
            // MainForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(530, 560);
            this.Controls.Add(this.triggerParametersGroupBox);
            this.Controls.Add(this.stopButton);
            this.Controls.Add(this.startButton);
            this.Controls.Add(this.acquisitionResultsGroupBox);
            this.Controls.Add(this.timingParametersGroupBox);
            this.Controls.Add(this.channelParametersGroupBox);
            this.Controls.Add(this.deviceParametersGroupBox);
            this.Controls.Add(this.accelerometerParametersGroupBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Continuous Acceleration - Internal Clock - Analog Start - SCXI-153x";
            this.triggerParametersGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.triggerLevelNumeric)).EndInit();
            this.accelerometerParametersGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.sensitivityNumeric)).EndInit();
            this.channelParametersGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.minimumValueNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.maximumValueNumeric)).EndInit();
            this.timingParametersGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.samplesPerChannelNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.rateNumeric)).EndInit();
            this.acquisitionResultsGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.acquisitionDataGrid)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.excitationValueNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lowpassCutoffFrequencyNumeric)).EndInit();
            this.deviceParametersGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.hysteresisNumeric)).EndInit();
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
            if (runningTask == null)
            {
                try 
                {
                    stopButton.Enabled = true;
                    startButton.Enabled = false;

                    
                    // Get Slope
                    switch(triggerSlopeComboBox.SelectedItem.ToString())
                    {
                        case "Rising":
                            triggerSlope = AnalogEdgeStartTriggerSlope.Rising;
                            break;
                        case "Falling":
                        default:
                            triggerSlope = AnalogEdgeStartTriggerSlope.Falling;
                            break;
                    }

                    // Get Sensitivity Units
                    switch(sensitivityUnitComboBox.SelectedItem.ToString())
                    {
                        case "mVolts/G": // Units mVolts/G
                            sensitivityUnits = AIAccelerometerSensitivityUnits.MillivoltsPerG;
                            break;
                        case "Volts/G":
                        default: // Units Volts/G
                            sensitivityUnits = AIAccelerometerSensitivityUnits.VoltsPerG;
                            break;
                    }

                    // Get Terminal Configuration
                    switch(terminalConfigurationComboBox.SelectedItem.ToString())
                    {
                        case "Rse":
                            terminalConfiguration = AITerminalConfiguration.Rse;
                            break;
                        case "Nrse":
                            terminalConfiguration = AITerminalConfiguration.Nrse;
                            break;
                        case "Differential":
                        default:
                            terminalConfiguration = AITerminalConfiguration.Differential;
                            break;
                    }
    
                    // Get Ex Source
                    switch(excitationSourceComboBox.SelectedItem.ToString())
                    {
                        case "Internal":
                            excitationSource = AIExcitationSource.Internal;
                            break;
                        case "External":
                            excitationSource = AIExcitationSource.External;
                            break;
                        default:
                            excitationSource = AIExcitationSource.None;
                            break;
                    }
                    
                    // Create a new task
                    myTask = new Task(); 
                    AIChannel aiChannel;

                    // Create a virtual channel
                    aiChannel = myTask.AIChannels.CreateAccelerometerChannel(physicalChannelComboBox.Text,"",
                        terminalConfiguration,Convert.ToDouble(minimumValueNumeric.Value),Convert.ToDouble(maximumValueNumeric.Value),
                        Convert.ToDouble(sensitivityNumeric.Value),sensitivityUnits,excitationSource,
                        Convert.ToDouble(excitationValueNumeric.Value),AIAccelerationUnits.G);

                    // Setup the lowpass cutoff frequency
                    aiChannel.LowpassCutoffFrequency = Convert.ToDouble(lowpassCutoffFrequencyNumeric.Value);

                    // Configure the timing parameters
                    myTask.Timing.ConfigureSampleClock("",Convert.ToDouble(rateNumeric.Value),
                        SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, 1000);

                    // Configure the Analog Trigger
                    myTask.Triggers.StartTrigger.ConfigureAnalogEdgeTrigger(triggerSourceTextBox.Text,triggerSlope,
                        Convert.ToDouble(triggerLevelNumeric.Value));
                                    
                    myTask.Triggers.StartTrigger.AnalogEdge.Hysteresis = Convert.ToDouble(hysteresisNumeric.Value);

                    // Verify the Task
                    myTask.Control(TaskAction.Verify);

                    // Prepare the table for Data
                    InitializeDataTable(myTask.AIChannels,ref dataTable); 
                    acquisitionDataGrid.DataSource=dataTable;   
                    
                    runningTask = myTask;
                    analogInReader = new AnalogMultiChannelReader(myTask.Stream);
                    analogCallback = new AsyncCallback(AnalogInCallback);

                    // Use SynchronizeCallbacks to specify that the object 
                    // marshals callbacks across threads appropriately.
                    analogInReader.SynchronizeCallbacks = true;
                    analogInReader.BeginReadWaveform(Convert.ToInt32(samplesPerChannelNumeric.Value), analogCallback, myTask);
                }
                catch (DaqException exception)
                {
                    // Display Errors
                    MessageBox.Show(exception.Message);
                    runningTask = null;
                    myTask.Dispose();
                    stopButton.Enabled = false;
                    startButton.Enabled = true;
                }           
            }

        }

        private void stopButton_Click(object sender, System.EventArgs e)
        {
            // Dispose of the task
            runningTask = null;
            myTask.Dispose();
            stopButton.Enabled = false;
            startButton.Enabled = true;
        }

        private void AnalogInCallback(IAsyncResult ar)
        {
            try
            {
                if (runningTask != null && runningTask == ar.AsyncState)
                {
                    // Read the available data from the channels
                    data = analogInReader.EndReadWaveform(ar);
                                      
                    // Plot your data here
                    dataToDataTable(data, ref dataTable);

                    analogInReader.BeginMemoryOptimizedReadWaveform(Convert.ToInt32(samplesPerChannelNumeric.Value),
                        analogCallback, myTask, data);
                }
            }
            catch (DaqException exception)
            {   
                // Display Errors
                MessageBox.Show(exception.Message);
                runningTask = null;
                myTask.Dispose();
                stopButton.Enabled = false;
                startButton.Enabled = true;
            }
        }

        private void dataToDataTable(AnalogWaveform<double>[] sourceArray, ref DataTable dataTable)
        {
            // Iterate over channels
            int currentLineIndex = 0;
            foreach (AnalogWaveform<double> waveform in sourceArray)
            {
                for (int sample = 0; sample < waveform.Samples.Count; ++sample)
                {
                    if (sample == 10)
                        break;

                    dataTable.Rows[sample][currentLineIndex] = waveform.Samples[sample].Value;
                }
                currentLineIndex++;
            }
        }

        public void InitializeDataTable(AIChannelCollection channelCollection,ref DataTable data)
        {
            int numOfChannels = channelCollection.Count;
            data.Rows.Clear();
            data.Columns.Clear();
            dataColumn = new DataColumn[numOfChannels];
            int numOfRows = 10;

            for (int currentChannelIndex=0; currentChannelIndex<numOfChannels; currentChannelIndex++)
            {   
                dataColumn[currentChannelIndex] = new DataColumn();
                dataColumn[currentChannelIndex].DataType = typeof(double);
                dataColumn[currentChannelIndex].ColumnName=channelCollection[currentChannelIndex].PhysicalName;
            }

            data.Columns.AddRange(dataColumn); 

            for(int currentDataIndex = 0; currentDataIndex<numOfRows; currentDataIndex++)             
            {
                object[] rowArr = new object[numOfChannels];
                data.Rows.Add(rowArr);              
            }
        }


    }
}
