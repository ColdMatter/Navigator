/******************************************************************************
*
* Example program:
*   ConAcqThmSmps_IntClk_SCXI1102And1581
*
* Category:
*   AI
*
* Description:
*   This example demonstrates how to acquire temperature data from a thermistor
*   using the DAQ device's internal clock.  This example uses the SCXI 1102
*   module in conjunction with the SCXI 1581 module.
*
* Instructions for running:
*   1.  Select the physical channel corresponding to where your signal is input
*       on the SCXI device.
*   2.  Enter the minimum and maximum temperature values in degrees C that you
*       expect to measure. A smaller range will allow a more accurate
*       measurement.
*   3.  Enter the acquisition rate.Note: The rate should be at least twice as
*       fast as the maximum frequency component of the signal being acquired.
*   4.  Enter the thermistor parameters (A, B, and C).
*   5.  Enter the resistance configuration, the current excitation source, and
*       the excitation value in Amps.  The current has been set to 100uA by
*       default for the SCXI-1581 which provides 100uA excitation.
*
* Steps:
*   1.  Create a task.  Create a AIChannel object using the
*       CreateThermistorIExChannel method.
*   2.  Set the timing parameters using the Timing.ConfigureSampleClock method.
*   3.  Call AnalogMultiChannelReader.BeginReadWaveform to install a callback
*       and begin the asynchronous read operation.
*   4.  Inside the callback, call AnalogMultiChannelReader.EndReadWaveform to
*       retrieve the data from the read operation.  
*   5.  Call AnalogMultiChannelReader.BeginMemoryOptimizedReadWaveform
*   6.  Dispose the Task object to clean-up any resources associated with the
*       task.
*   7.  Handle any DaqExceptions, if they occur.
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

namespace NationalInstruments.Examples.ConAcqThmSmps_IntClk_SCXI1102And1581
{
    /// <summary>
    /// Summary description for MainForm.
    /// </summary>
    public class MainForm : System.Windows.Forms.Form
    {
        private Task myTask;
        private Task runningTask;
        private AnalogWaveform<double>[] data;
        private AnalogMultiChannelReader analogInReader;
        private AsyncCallback myAsyncCallback;
        private DataColumn[] dataColumn = null;
        private DataTable dataTable = null;

        private System.Windows.Forms.GroupBox acquisitionResultsGroupBox;
        private System.Windows.Forms.Label resultLabel;
        private System.Windows.Forms.GroupBox resistanceParametersGroupBox;
        private System.Windows.Forms.Label currentValueLabel;
        private System.Windows.Forms.Label currentExcitationLabel;
        private System.Windows.Forms.Label resistanceConfigLabel;
        private System.Windows.Forms.ComboBox currentExcitationComboBox;
        private System.Windows.Forms.ComboBox resistanceConfigComboBox;
        private System.Windows.Forms.GroupBox thermistorParametersGroupBox;
        private System.Windows.Forms.Button stopButton;
        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.GroupBox timingParametersGroupBox;
        private System.Windows.Forms.Label rateLabel;
        private System.Windows.Forms.GroupBox channelParametersGroupBox;
        private System.Windows.Forms.Label maximumLabel;
        private System.Windows.Forms.Label minimumLabel;
        private System.Windows.Forms.Label physicalChannelLabel;
        private System.Windows.Forms.Label bLabel;
        private System.Windows.Forms.Label aLabel;
        private System.Windows.Forms.Label cLabel;
        private System.Windows.Forms.TextBox bTextBox;
        private System.Windows.Forms.TextBox cTextBox;
        private System.Windows.Forms.DataGrid acquisitionDataGrid;
        private System.Windows.Forms.TextBox aTextBox;
        private System.Windows.Forms.NumericUpDown rateNumeric;
        private System.Windows.Forms.NumericUpDown currentExcitationValueNumeric;
        internal System.Windows.Forms.NumericUpDown minimumValueNumeric;
        internal System.Windows.Forms.NumericUpDown maximumValueNumeric;
        private System.Windows.Forms.ComboBox physicalChannelComboBox;
        private System.ComponentModel.IContainer components = null;

        public MainForm()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();
            resistanceConfigComboBox.SelectedIndex = 2;
            currentExcitationComboBox.SelectedIndex = 2;
            myAsyncCallback = new AsyncCallback(AnalogInCallback);
            dataTable = new DataTable();

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
            this.acquisitionResultsGroupBox = new System.Windows.Forms.GroupBox();
            this.resultLabel = new System.Windows.Forms.Label();
            this.acquisitionDataGrid = new System.Windows.Forms.DataGrid();
            this.resistanceParametersGroupBox = new System.Windows.Forms.GroupBox();
            this.currentExcitationValueNumeric = new System.Windows.Forms.NumericUpDown();
            this.currentExcitationLabel = new System.Windows.Forms.Label();
            this.resistanceConfigLabel = new System.Windows.Forms.Label();
            this.currentExcitationComboBox = new System.Windows.Forms.ComboBox();
            this.resistanceConfigComboBox = new System.Windows.Forms.ComboBox();
            this.currentValueLabel = new System.Windows.Forms.Label();
            this.thermistorParametersGroupBox = new System.Windows.Forms.GroupBox();
            this.cLabel = new System.Windows.Forms.Label();
            this.bTextBox = new System.Windows.Forms.TextBox();
            this.bLabel = new System.Windows.Forms.Label();
            this.aLabel = new System.Windows.Forms.Label();
            this.aTextBox = new System.Windows.Forms.TextBox();
            this.cTextBox = new System.Windows.Forms.TextBox();
            this.stopButton = new System.Windows.Forms.Button();
            this.startButton = new System.Windows.Forms.Button();
            this.timingParametersGroupBox = new System.Windows.Forms.GroupBox();
            this.rateNumeric = new System.Windows.Forms.NumericUpDown();
            this.rateLabel = new System.Windows.Forms.Label();
            this.channelParametersGroupBox = new System.Windows.Forms.GroupBox();
            this.physicalChannelComboBox = new System.Windows.Forms.ComboBox();
            this.minimumValueNumeric = new System.Windows.Forms.NumericUpDown();
            this.maximumValueNumeric = new System.Windows.Forms.NumericUpDown();
            this.maximumLabel = new System.Windows.Forms.Label();
            this.minimumLabel = new System.Windows.Forms.Label();
            this.physicalChannelLabel = new System.Windows.Forms.Label();
            this.acquisitionResultsGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.acquisitionDataGrid)).BeginInit();
            this.resistanceParametersGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.currentExcitationValueNumeric)).BeginInit();
            this.thermistorParametersGroupBox.SuspendLayout();
            this.timingParametersGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.rateNumeric)).BeginInit();
            this.channelParametersGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.minimumValueNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.maximumValueNumeric)).BeginInit();
            this.SuspendLayout();
            // 
            // acquisitionResultsGroupBox
            // 
            this.acquisitionResultsGroupBox.Controls.Add(this.resultLabel);
            this.acquisitionResultsGroupBox.Controls.Add(this.acquisitionDataGrid);
            this.acquisitionResultsGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.acquisitionResultsGroupBox.Location = new System.Drawing.Point(248, 136);
            this.acquisitionResultsGroupBox.Name = "acquisitionResultsGroupBox";
            this.acquisitionResultsGroupBox.Size = new System.Drawing.Size(264, 272);
            this.acquisitionResultsGroupBox.TabIndex = 6;
            this.acquisitionResultsGroupBox.TabStop = false;
            this.acquisitionResultsGroupBox.Text = "Acquisition Results:";
            // 
            // resultLabel
            // 
            this.resultLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.resultLabel.Location = new System.Drawing.Point(16, 24);
            this.resultLabel.Name = "resultLabel";
            this.resultLabel.Size = new System.Drawing.Size(96, 16);
            this.resultLabel.TabIndex = 0;
            this.resultLabel.Text = "Acquisition Data:";
            // 
            // acquisitionDataGrid
            // 
            this.acquisitionDataGrid.AllowSorting = false;
            this.acquisitionDataGrid.DataMember = "";
            this.acquisitionDataGrid.HeaderForeColor = System.Drawing.SystemColors.ControlText;
            this.acquisitionDataGrid.Location = new System.Drawing.Point(16, 40);
            this.acquisitionDataGrid.Name = "acquisitionDataGrid";
            this.acquisitionDataGrid.ParentRowsVisible = false;
            this.acquisitionDataGrid.ReadOnly = true;
            this.acquisitionDataGrid.Size = new System.Drawing.Size(232, 224);
            this.acquisitionDataGrid.TabIndex = 1;
            this.acquisitionDataGrid.TabStop = false;
            // 
            // resistanceParametersGroupBox
            // 
            this.resistanceParametersGroupBox.Controls.Add(this.currentExcitationValueNumeric);
            this.resistanceParametersGroupBox.Controls.Add(this.currentExcitationLabel);
            this.resistanceParametersGroupBox.Controls.Add(this.resistanceConfigLabel);
            this.resistanceParametersGroupBox.Controls.Add(this.currentExcitationComboBox);
            this.resistanceParametersGroupBox.Controls.Add(this.resistanceConfigComboBox);
            this.resistanceParametersGroupBox.Controls.Add(this.currentValueLabel);
            this.resistanceParametersGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.resistanceParametersGroupBox.Location = new System.Drawing.Point(248, 8);
            this.resistanceParametersGroupBox.Name = "resistanceParametersGroupBox";
            this.resistanceParametersGroupBox.Size = new System.Drawing.Size(264, 120);
            this.resistanceParametersGroupBox.TabIndex = 5;
            this.resistanceParametersGroupBox.TabStop = false;
            this.resistanceParametersGroupBox.Text = "Resistance Parameters";
            // 
            // currentExcitationValueNumeric
            // 
            this.currentExcitationValueNumeric.DecimalPlaces = 5;
            this.currentExcitationValueNumeric.Location = new System.Drawing.Point(160, 88);
            this.currentExcitationValueNumeric.Name = "currentExcitationValueNumeric";
            this.currentExcitationValueNumeric.Size = new System.Drawing.Size(88, 20);
            this.currentExcitationValueNumeric.TabIndex = 5;
            this.currentExcitationValueNumeric.Value = new System.Decimal(new int[] {
                                                                                        10,
                                                                                        0,
                                                                                        0,
                                                                                        327680});
            // 
            // currentExcitationLabel
            // 
            this.currentExcitationLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.currentExcitationLabel.Location = new System.Drawing.Point(16, 59);
            this.currentExcitationLabel.Name = "currentExcitationLabel";
            this.currentExcitationLabel.Size = new System.Drawing.Size(136, 14);
            this.currentExcitationLabel.TabIndex = 2;
            this.currentExcitationLabel.Text = "Current Excitation Source:";
            // 
            // resistanceConfigLabel
            // 
            this.resistanceConfigLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.resistanceConfigLabel.Location = new System.Drawing.Point(16, 27);
            this.resistanceConfigLabel.Name = "resistanceConfigLabel";
            this.resistanceConfigLabel.Size = new System.Drawing.Size(136, 14);
            this.resistanceConfigLabel.TabIndex = 0;
            this.resistanceConfigLabel.Text = "Resistance Configuration:";
            // 
            // currentExcitationComboBox
            // 
            this.currentExcitationComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.currentExcitationComboBox.Items.AddRange(new object[] {
                                                                           "None",
                                                                           "Internal",
                                                                           "External"});
            this.currentExcitationComboBox.Location = new System.Drawing.Point(160, 56);
            this.currentExcitationComboBox.Name = "currentExcitationComboBox";
            this.currentExcitationComboBox.Size = new System.Drawing.Size(88, 21);
            this.currentExcitationComboBox.TabIndex = 3;
            // 
            // resistanceConfigComboBox
            // 
            this.resistanceConfigComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.resistanceConfigComboBox.Items.AddRange(new object[] {
                                                                          "2-Wire",
                                                                          "3-Wire",
                                                                          "4-Wire"});
            this.resistanceConfigComboBox.Location = new System.Drawing.Point(160, 24);
            this.resistanceConfigComboBox.Name = "resistanceConfigComboBox";
            this.resistanceConfigComboBox.Size = new System.Drawing.Size(88, 21);
            this.resistanceConfigComboBox.TabIndex = 1;
            // 
            // currentValueLabel
            // 
            this.currentValueLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.currentValueLabel.Location = new System.Drawing.Point(16, 91);
            this.currentValueLabel.Name = "currentValueLabel";
            this.currentValueLabel.Size = new System.Drawing.Size(152, 14);
            this.currentValueLabel.TabIndex = 4;
            this.currentValueLabel.Text = "Current Excitation Value (A):";
            // 
            // thermistorParametersGroupBox
            // 
            this.thermistorParametersGroupBox.Controls.Add(this.cLabel);
            this.thermistorParametersGroupBox.Controls.Add(this.bTextBox);
            this.thermistorParametersGroupBox.Controls.Add(this.bLabel);
            this.thermistorParametersGroupBox.Controls.Add(this.aLabel);
            this.thermistorParametersGroupBox.Controls.Add(this.aTextBox);
            this.thermistorParametersGroupBox.Controls.Add(this.cTextBox);
            this.thermistorParametersGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.thermistorParametersGroupBox.Location = new System.Drawing.Point(8, 216);
            this.thermistorParametersGroupBox.Name = "thermistorParametersGroupBox";
            this.thermistorParametersGroupBox.Size = new System.Drawing.Size(224, 120);
            this.thermistorParametersGroupBox.TabIndex = 4;
            this.thermistorParametersGroupBox.TabStop = false;
            this.thermistorParametersGroupBox.Text = "Thermistor Characteristics";
            // 
            // cLabel
            // 
            this.cLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.cLabel.Location = new System.Drawing.Point(16, 88);
            this.cLabel.Name = "cLabel";
            this.cLabel.Size = new System.Drawing.Size(72, 17);
            this.cLabel.TabIndex = 4;
            this.cLabel.Text = "Parameter C:";
            // 
            // bTextBox
            // 
            this.bTextBox.Location = new System.Drawing.Point(112, 56);
            this.bTextBox.Name = "bTextBox";
            this.bTextBox.Size = new System.Drawing.Size(96, 20);
            this.bTextBox.TabIndex = 3;
            this.bTextBox.Text = "234.3159000E-6";
            // 
            // bLabel
            // 
            this.bLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.bLabel.Location = new System.Drawing.Point(16, 56);
            this.bLabel.Name = "bLabel";
            this.bLabel.Size = new System.Drawing.Size(72, 17);
            this.bLabel.TabIndex = 2;
            this.bLabel.Text = "Parameter B:";
            // 
            // aLabel
            // 
            this.aLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.aLabel.Location = new System.Drawing.Point(16, 26);
            this.aLabel.Name = "aLabel";
            this.aLabel.Size = new System.Drawing.Size(72, 17);
            this.aLabel.TabIndex = 0;
            this.aLabel.Text = "Parameter A:";
            // 
            // aTextBox
            // 
            this.aTextBox.Location = new System.Drawing.Point(112, 24);
            this.aTextBox.Name = "aTextBox";
            this.aTextBox.Size = new System.Drawing.Size(96, 20);
            this.aTextBox.TabIndex = 1;
            this.aTextBox.Text = "1.2953610E-3";
            // 
            // cTextBox
            // 
            this.cTextBox.Location = new System.Drawing.Point(112, 88);
            this.cTextBox.Name = "cTextBox";
            this.cTextBox.Size = new System.Drawing.Size(96, 20);
            this.cTextBox.TabIndex = 5;
            this.cTextBox.Text = "101.8703000E-9";
            // 
            // stopButton
            // 
            this.stopButton.Enabled = false;
            this.stopButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.stopButton.Location = new System.Drawing.Point(128, 384);
            this.stopButton.Name = "stopButton";
            this.stopButton.Size = new System.Drawing.Size(80, 24);
            this.stopButton.TabIndex = 1;
            this.stopButton.Text = "Stop";
            this.stopButton.Click += new System.EventHandler(this.stopButton_Click);
            // 
            // startButton
            // 
            this.startButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.startButton.Location = new System.Drawing.Point(32, 384);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(80, 24);
            this.startButton.TabIndex = 0;
            this.startButton.Text = "Start";
            this.startButton.Click += new System.EventHandler(this.startButton_Click);
            // 
            // timingParametersGroupBox
            // 
            this.timingParametersGroupBox.Controls.Add(this.rateNumeric);
            this.timingParametersGroupBox.Controls.Add(this.rateLabel);
            this.timingParametersGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.timingParametersGroupBox.Location = new System.Drawing.Point(8, 136);
            this.timingParametersGroupBox.Name = "timingParametersGroupBox";
            this.timingParametersGroupBox.Size = new System.Drawing.Size(224, 64);
            this.timingParametersGroupBox.TabIndex = 3;
            this.timingParametersGroupBox.TabStop = false;
            this.timingParametersGroupBox.Text = "Timing Parameters";
            // 
            // rateNumeric
            // 
            this.rateNumeric.DecimalPlaces = 2;
            this.rateNumeric.Location = new System.Drawing.Point(112, 24);
            this.rateNumeric.Maximum = new System.Decimal(new int[] {
                                                                        1000000,
                                                                        0,
                                                                        0,
                                                                        0});
            this.rateNumeric.Minimum = new System.Decimal(new int[] {
                                                                        1,
                                                                        0,
                                                                        0,
                                                                        0});
            this.rateNumeric.Name = "rateNumeric";
            this.rateNumeric.Size = new System.Drawing.Size(96, 20);
            this.rateNumeric.TabIndex = 1;
            this.rateNumeric.Value = new System.Decimal(new int[] {
                                                                      1000,
                                                                      0,
                                                                      0,
                                                                      0});
            // 
            // rateLabel
            // 
            this.rateLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.rateLabel.Location = new System.Drawing.Point(16, 24);
            this.rateLabel.Name = "rateLabel";
            this.rateLabel.Size = new System.Drawing.Size(80, 16);
            this.rateLabel.TabIndex = 0;
            this.rateLabel.Text = "Rate (Hz):";
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
            this.channelParametersGroupBox.Size = new System.Drawing.Size(224, 120);
            this.channelParametersGroupBox.TabIndex = 2;
            this.channelParametersGroupBox.TabStop = false;
            this.channelParametersGroupBox.Text = "Channel Parameters";
            // 
            // physicalChannelComboBox
            // 
            this.physicalChannelComboBox.Location = new System.Drawing.Point(112, 24);
            this.physicalChannelComboBox.Name = "physicalChannelComboBox";
            this.physicalChannelComboBox.Size = new System.Drawing.Size(96, 21);
            this.physicalChannelComboBox.TabIndex = 1;
            this.physicalChannelComboBox.Text = "SC1Mod1/ai0";
            // 
            // minimumValueNumeric
            // 
            this.minimumValueNumeric.DecimalPlaces = 2;
            this.minimumValueNumeric.Location = new System.Drawing.Point(112, 88);
            this.minimumValueNumeric.Maximum = new System.Decimal(new int[] {
                                                                                500,
                                                                                0,
                                                                                0,
                                                                                0});
            this.minimumValueNumeric.Minimum = new System.Decimal(new int[] {
                                                                                500,
                                                                                0,
                                                                                0,
                                                                                -2147483648});
            this.minimumValueNumeric.Name = "minimumValueNumeric";
            this.minimumValueNumeric.Size = new System.Drawing.Size(96, 20);
            this.minimumValueNumeric.TabIndex = 5;
            // 
            // maximumValueNumeric
            // 
            this.maximumValueNumeric.DecimalPlaces = 2;
            this.maximumValueNumeric.Location = new System.Drawing.Point(112, 56);
            this.maximumValueNumeric.Maximum = new System.Decimal(new int[] {
                                                                                500,
                                                                                0,
                                                                                0,
                                                                                0});
            this.maximumValueNumeric.Minimum = new System.Decimal(new int[] {
                                                                                500,
                                                                                0,
                                                                                0,
                                                                                -2147483648});
            this.maximumValueNumeric.Name = "maximumValueNumeric";
            this.maximumValueNumeric.Size = new System.Drawing.Size(96, 20);
            this.maximumValueNumeric.TabIndex = 3;
            this.maximumValueNumeric.Value = new System.Decimal(new int[] {
                                                                              100,
                                                                              0,
                                                                              0,
                                                                              0});
            // 
            // maximumLabel
            // 
            this.maximumLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.maximumLabel.Location = new System.Drawing.Point(16, 59);
            this.maximumLabel.Name = "maximumLabel";
            this.maximumLabel.Size = new System.Drawing.Size(104, 14);
            this.maximumLabel.TabIndex = 2;
            this.maximumLabel.Text = "Maximum (deg C):";
            // 
            // minimumLabel
            // 
            this.minimumLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.minimumLabel.Location = new System.Drawing.Point(16, 91);
            this.minimumLabel.Name = "minimumLabel";
            this.minimumLabel.Size = new System.Drawing.Size(104, 14);
            this.minimumLabel.TabIndex = 4;
            this.minimumLabel.Text = "Minimum (deg C):";
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
            // MainForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(522, 416);
            this.Controls.Add(this.acquisitionResultsGroupBox);
            this.Controls.Add(this.resistanceParametersGroupBox);
            this.Controls.Add(this.thermistorParametersGroupBox);
            this.Controls.Add(this.stopButton);
            this.Controls.Add(this.startButton);
            this.Controls.Add(this.timingParametersGroupBox);
            this.Controls.Add(this.channelParametersGroupBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Continuous Acquisition Thermistor Samples - Internal  Clock - SCXI 1102 And 1581";
            this.acquisitionResultsGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.acquisitionDataGrid)).EndInit();
            this.resistanceParametersGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.currentExcitationValueNumeric)).EndInit();
            this.thermistorParametersGroupBox.ResumeLayout(false);
            this.timingParametersGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.rateNumeric)).EndInit();
            this.channelParametersGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.minimumValueNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.maximumValueNumeric)).EndInit();
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
            stopButton.Enabled = true;

            try
            {
                myTask = new Task();
                AIResistanceConfiguration resistanceConfiguration;
                AIExcitationSource excitationSource;

                switch (resistanceConfigComboBox.SelectedIndex)
                {
                    case 0:
                        resistanceConfiguration = AIResistanceConfiguration.TwoWire;
                        break;
                    case 1:
                        resistanceConfiguration = AIResistanceConfiguration.ThreeWire;
                        break;
                    case 2:
                    default:
                        resistanceConfiguration = AIResistanceConfiguration.FourWire;
                        break;
                }

                switch (currentExcitationComboBox.SelectedIndex)
                {
                    case 0:
                        excitationSource = AIExcitationSource.None;
                        break;
                    case 1:
                        excitationSource = AIExcitationSource.Internal;
                        break;
                    case 2:
                    default:
                        excitationSource = AIExcitationSource.External;
                        break;
                }

                myTask.AIChannels.CreateThermistorIExChannel(physicalChannelComboBox.Text, "",
                    Convert.ToDouble(minimumValueNumeric.Value), Convert.ToDouble(maximumValueNumeric.Value),
                    AITemperatureUnits.DegreesC, resistanceConfiguration, excitationSource,
                    Convert.ToDouble(currentExcitationValueNumeric.Value), Convert.ToDouble(aTextBox.Text),
                    Convert.ToDouble(bTextBox.Text), Convert.ToDouble(cTextBox.Text));

                myTask.Timing.ConfigureSampleClock("", Convert.ToDouble(rateNumeric.Value),
                    SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, 1000);

                myTask.Control(TaskAction.Verify);

                InitializeDataTable(myTask.AIChannels, ref dataTable);
                acquisitionDataGrid.DataSource = dataTable;

                analogInReader = new AnalogMultiChannelReader(myTask.Stream);

                runningTask = myTask;

                // Use SynchronizeCallbacks to specify that the object 
                // marshals callbacks across threads appropriately.
                analogInReader.SynchronizeCallbacks = true;
                analogInReader.BeginReadWaveform(Convert.ToInt32(rateNumeric.Value), myAsyncCallback, myTask);
            }
            catch (DaqException ex)
            {
                MessageBox.Show(ex.Message);
                myTask.Dispose();
                stopButton.Enabled = false;
                startButton.Enabled = true;
            }
        }

        private void stopButton_Click(object sender, System.EventArgs e)
        {
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
                    data = analogInReader.EndReadWaveform(ar);
                    dataToDataTable(data, ref dataTable);
                    analogInReader.BeginMemoryOptimizedReadWaveform(Convert.ToInt32(rateNumeric.Value), myAsyncCallback, myTask, data);
                }
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

        private void InitializeDataTable(AIChannelCollection channelCollection, ref DataTable data)
        {
            if (channelCollection == null)
                return;

            int numOfChannels = channelCollection.Count;
            data.Rows.Clear();
            data.Columns.Clear();
            dataColumn = new DataColumn[numOfChannels];
            int numOfRows = 10;

            for (int currentChannelIndex = 0; currentChannelIndex < numOfChannels; currentChannelIndex++)
            {
                dataColumn[currentChannelIndex] = new DataColumn();
                dataColumn[currentChannelIndex].DataType = typeof(double);
                dataColumn[currentChannelIndex].ColumnName = channelCollection[currentChannelIndex].PhysicalName;
            }

            data.Columns.AddRange(dataColumn);

            for (int currentDataIndex = 0; currentDataIndex < numOfRows; currentDataIndex++)
            {
                object[] rowArr = new object[numOfChannels];
                data.Rows.Add(rowArr);
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
    }
}

