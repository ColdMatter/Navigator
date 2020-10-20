/******************************************************************************
*
* Example program:
*   ContAcqLVDTSamples_IntClk_SCXI1540
*
* Category:
*   AI
*
* Description:
*   This example demonstrates how to make a continuous, hardware-timed
*   acceleration measurement using an SCXI-1540 module.
*
* Instructions for running:
*   1.  Specify the physical channel where you have connected the LVDT.
*   2.  Enter the minimum and maximum distance values, in units based on the
*       units control, that you expect to measure. A smaller range will allow a
*       more accurate measurement.
*   3.  Select the number of samples to acquire.
*   4.  Set the rate of the acquisition.
*   5.  Specify the LVDT settings.
*   6.  If you are using multiple LVDTs and would like to synchronize their
*       excitations, then enable synchronization for all the secondary LVDT
*       channels via the Synchronization Enabled button. You must also connect
*       the excitation output (EX+) of your primary LVDT channel to all the
*       secondary LVDT channel's sync pin (SYNC).
*
* Steps:
*   1.  Create a new analog input task.
*   2.  Create an analog input LVDT channel.
*   3.  Configure the synchronization of the SCXI-1540 module.
*   4.  Set up the timing for the acquisition. In this example we use the DAQ
*       device's internal clock to read samples continuously.
*   5.  Call AnalogMultiChannelReader.BeginReadWaveform to install a callback
*       and begin the asynchronous read operation.
*   6.  Inside the callback, call AnalogMultiChannelReader.EndReadWaveform to
*       retrieve the data from the read operation.  
*   7.  Call AnalogMultiChannelReader.BeginMemoryOptimizedReadWaveform
*   8.  Dispose the Task object to clean-up any resources associated with the
*       task.
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
*   Connect your LVDT to the terminals corresponding to the physical channel I/O
*   control value. The excitation lines connect to EX+ and EX- while the analog
*   input lines connect to CH+ and CH-.  For more information on the input and
*   output terminals for your device, open the NI-DAQmx Help, and refer to the
*   NI-DAQmx Device Terminals and Device Considerations books in the table of
*   contents.
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

namespace NationalInstruments.Examples.ContAcqLVDTSamples_IntClk_SCXI1540
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
        private System.Windows.Forms.ComboBox unitsComboBox;
        private System.Windows.Forms.Label unitsLabel;
        private System.Windows.Forms.Label sensitivityLabel;

        private AnalogMultiChannelReader analogInReader;
        private AIExcitationSource excitationSource;
        private AILvdtSensitivityUnits sensitivityUnits;
        private AIACExcitationWireMode wireMode;    
        
        private Task myTask;
        private Task runningTask;
        private AsyncCallback analogCallback;
        private AnalogWaveform<double>[] data;
        private DataColumn[] dataColumn = null;
        private DataTable dataTable = null;
        private System.Windows.Forms.NumericUpDown rateNumeric;
        private System.Windows.Forms.NumericUpDown samplesPerChannelNumeric;
        private System.Windows.Forms.NumericUpDown excitationFrequencyNumeric;
        private System.Windows.Forms.NumericUpDown sensitivityNumeric;
        private System.Windows.Forms.NumericUpDown excitationValueNumeric;
        private System.Windows.Forms.GroupBox lvdtParametersGroupBox;
        private System.Windows.Forms.Label excitationFrequencyLabel;
        private System.Windows.Forms.ComboBox excitationSourceComboBox;
        private System.Windows.Forms.Label excitationSourceLabel;
        private System.Windows.Forms.ComboBox excitationWireModeComboBox;
        private System.Windows.Forms.Label excitationValueLabel;
        private System.Windows.Forms.Label excitationWireModeLabel;
        private System.Windows.Forms.GroupBox deviceParametersGroupBox;
        private System.Windows.Forms.ComboBox sensitivityUnitsComboBox;
        private System.Windows.Forms.Label sensitivityUnitsLabel;
        private System.Windows.Forms.CheckBox synchronizationEnabledCheckBox;
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

            //
            // TODO: Add any constructor code after InitializeComponent call
            //
            stopButton.Enabled = false;
            dataTable= new DataTable();
            unitsComboBox.SelectedIndex = 0;
            sensitivityUnitsComboBox.SelectedIndex = 0;
            excitationSourceComboBox.SelectedIndex = 0;
            excitationWireModeComboBox.SelectedIndex = 0;

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
            this.channelParametersGroupBox = new System.Windows.Forms.GroupBox();
            this.physicalChannelComboBox = new System.Windows.Forms.ComboBox();
            this.minimumValueNumeric = new System.Windows.Forms.NumericUpDown();
            this.maximumValueNumeric = new System.Windows.Forms.NumericUpDown();
            this.unitsComboBox = new System.Windows.Forms.ComboBox();
            this.unitsLabel = new System.Windows.Forms.Label();
            this.maximumLabel = new System.Windows.Forms.Label();
            this.minimumLabel = new System.Windows.Forms.Label();
            this.physicalChannelLabel = new System.Windows.Forms.Label();
            this.timingParametersGroupBox = new System.Windows.Forms.GroupBox();
            this.samplesPerChannelNumeric = new System.Windows.Forms.NumericUpDown();
            this.rateNumeric = new System.Windows.Forms.NumericUpDown();
            this.samplesLabel = new System.Windows.Forms.Label();
            this.rateLabel = new System.Windows.Forms.Label();
            this.lvdtParametersGroupBox = new System.Windows.Forms.GroupBox();
            this.excitationValueNumeric = new System.Windows.Forms.NumericUpDown();
            this.sensitivityNumeric = new System.Windows.Forms.NumericUpDown();
            this.excitationFrequencyNumeric = new System.Windows.Forms.NumericUpDown();
            this.excitationFrequencyLabel = new System.Windows.Forms.Label();
            this.excitationSourceComboBox = new System.Windows.Forms.ComboBox();
            this.excitationSourceLabel = new System.Windows.Forms.Label();
            this.excitationWireModeComboBox = new System.Windows.Forms.ComboBox();
            this.sensitivityUnitsComboBox = new System.Windows.Forms.ComboBox();
            this.excitationValueLabel = new System.Windows.Forms.Label();
            this.excitationWireModeLabel = new System.Windows.Forms.Label();
            this.sensitivityUnitsLabel = new System.Windows.Forms.Label();
            this.sensitivityLabel = new System.Windows.Forms.Label();
            this.acquisitionResultsGroupBox = new System.Windows.Forms.GroupBox();
            this.resultLabel = new System.Windows.Forms.Label();
            this.acquisitionDataGrid = new System.Windows.Forms.DataGrid();
            this.deviceParametersGroupBox = new System.Windows.Forms.GroupBox();
            this.synchronizationEnabledCheckBox = new System.Windows.Forms.CheckBox();
            this.stopButton = new System.Windows.Forms.Button();
            this.startButton = new System.Windows.Forms.Button();
            this.channelParametersGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.minimumValueNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.maximumValueNumeric)).BeginInit();
            this.timingParametersGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.samplesPerChannelNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.rateNumeric)).BeginInit();
            this.lvdtParametersGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.excitationValueNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sensitivityNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.excitationFrequencyNumeric)).BeginInit();
            this.acquisitionResultsGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.acquisitionDataGrid)).BeginInit();
            this.deviceParametersGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // channelParametersGroupBox
            // 
            this.channelParametersGroupBox.Controls.Add(this.physicalChannelComboBox);
            this.channelParametersGroupBox.Controls.Add(this.minimumValueNumeric);
            this.channelParametersGroupBox.Controls.Add(this.maximumValueNumeric);
            this.channelParametersGroupBox.Controls.Add(this.unitsComboBox);
            this.channelParametersGroupBox.Controls.Add(this.unitsLabel);
            this.channelParametersGroupBox.Controls.Add(this.maximumLabel);
            this.channelParametersGroupBox.Controls.Add(this.minimumLabel);
            this.channelParametersGroupBox.Controls.Add(this.physicalChannelLabel);
            this.channelParametersGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.channelParametersGroupBox.Location = new System.Drawing.Point(8, 8);
            this.channelParametersGroupBox.Name = "channelParametersGroupBox";
            this.channelParametersGroupBox.Size = new System.Drawing.Size(272, 152);
            this.channelParametersGroupBox.TabIndex = 2;
            this.channelParametersGroupBox.TabStop = false;
            this.channelParametersGroupBox.Text = "Channel Parameters";
            // 
            // physicalChannelComboBox
            // 
            this.physicalChannelComboBox.Location = new System.Drawing.Point(128, 24);
            this.physicalChannelComboBox.Name = "physicalChannelComboBox";
            this.physicalChannelComboBox.Size = new System.Drawing.Size(136, 21);
            this.physicalChannelComboBox.TabIndex = 1;
            this.physicalChannelComboBox.Text = "SC1Mod1/ai0";
            // 
            // minimumValueNumeric
            // 
            this.minimumValueNumeric.DecimalPlaces = 2;
            this.minimumValueNumeric.Location = new System.Drawing.Point(128, 56);
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
            this.minimumValueNumeric.Size = new System.Drawing.Size(136, 20);
            this.minimumValueNumeric.TabIndex = 3;
            this.minimumValueNumeric.Value = new System.Decimal(new int[] {
                                                                              10,
                                                                              0,
                                                                              0,
                                                                              -2147352576});
            // 
            // maximumValueNumeric
            // 
            this.maximumValueNumeric.DecimalPlaces = 2;
            this.maximumValueNumeric.Location = new System.Drawing.Point(128, 88);
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
            this.maximumValueNumeric.Size = new System.Drawing.Size(136, 20);
            this.maximumValueNumeric.TabIndex = 5;
            this.maximumValueNumeric.Value = new System.Decimal(new int[] {
                                                                              10,
                                                                              0,
                                                                              0,
                                                                              131072});
            // 
            // unitsComboBox
            // 
            this.unitsComboBox.Items.AddRange(new object[] {
                                                               "Meters",
                                                               "Inches",
                                                               "Custom Scale"});
            this.unitsComboBox.Location = new System.Drawing.Point(128, 120);
            this.unitsComboBox.Name = "unitsComboBox";
            this.unitsComboBox.Size = new System.Drawing.Size(136, 21);
            this.unitsComboBox.TabIndex = 7;
            this.unitsComboBox.Text = "Meters";
            // 
            // unitsLabel
            // 
            this.unitsLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.unitsLabel.Location = new System.Drawing.Point(16, 122);
            this.unitsLabel.Name = "unitsLabel";
            this.unitsLabel.Size = new System.Drawing.Size(40, 16);
            this.unitsLabel.TabIndex = 6;
            this.unitsLabel.Text = "Units:";
            // 
            // maximumLabel
            // 
            this.maximumLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.maximumLabel.Location = new System.Drawing.Point(16, 90);
            this.maximumLabel.Name = "maximumLabel";
            this.maximumLabel.Size = new System.Drawing.Size(112, 16);
            this.maximumLabel.TabIndex = 4;
            this.maximumLabel.Text = "Maximum Value:";
            // 
            // minimumLabel
            // 
            this.minimumLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.minimumLabel.Location = new System.Drawing.Point(16, 57);
            this.minimumLabel.Name = "minimumLabel";
            this.minimumLabel.Size = new System.Drawing.Size(104, 18);
            this.minimumLabel.TabIndex = 2;
            this.minimumLabel.Text = "Minimum Value:";
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
            this.timingParametersGroupBox.Location = new System.Drawing.Point(8, 168);
            this.timingParametersGroupBox.Name = "timingParametersGroupBox";
            this.timingParametersGroupBox.Size = new System.Drawing.Size(272, 88);
            this.timingParametersGroupBox.TabIndex = 3;
            this.timingParametersGroupBox.TabStop = false;
            this.timingParametersGroupBox.Text = "Timing Parameters";
            // 
            // samplesPerChannelNumeric
            // 
            this.samplesPerChannelNumeric.Location = new System.Drawing.Point(128, 56);
            this.samplesPerChannelNumeric.Maximum = new System.Decimal(new int[] {
                                                                                     1000000,
                                                                                     0,
                                                                                     0,
                                                                                     0});
            this.samplesPerChannelNumeric.Name = "samplesPerChannelNumeric";
            this.samplesPerChannelNumeric.Size = new System.Drawing.Size(136, 20);
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
            this.rateNumeric.Location = new System.Drawing.Point(128, 24);
            this.rateNumeric.Maximum = new System.Decimal(new int[] {
                                                                        1000000,
                                                                        0,
                                                                        0,
                                                                        0});
            this.rateNumeric.Name = "rateNumeric";
            this.rateNumeric.Size = new System.Drawing.Size(136, 20);
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
            this.samplesLabel.Size = new System.Drawing.Size(120, 16);
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
            // lvdtParametersGroupBox
            // 
            this.lvdtParametersGroupBox.Controls.Add(this.excitationValueNumeric);
            this.lvdtParametersGroupBox.Controls.Add(this.sensitivityNumeric);
            this.lvdtParametersGroupBox.Controls.Add(this.excitationFrequencyNumeric);
            this.lvdtParametersGroupBox.Controls.Add(this.excitationFrequencyLabel);
            this.lvdtParametersGroupBox.Controls.Add(this.excitationSourceComboBox);
            this.lvdtParametersGroupBox.Controls.Add(this.excitationSourceLabel);
            this.lvdtParametersGroupBox.Controls.Add(this.excitationWireModeComboBox);
            this.lvdtParametersGroupBox.Controls.Add(this.sensitivityUnitsComboBox);
            this.lvdtParametersGroupBox.Controls.Add(this.excitationValueLabel);
            this.lvdtParametersGroupBox.Controls.Add(this.excitationWireModeLabel);
            this.lvdtParametersGroupBox.Controls.Add(this.sensitivityUnitsLabel);
            this.lvdtParametersGroupBox.Controls.Add(this.sensitivityLabel);
            this.lvdtParametersGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.lvdtParametersGroupBox.Location = new System.Drawing.Point(8, 264);
            this.lvdtParametersGroupBox.Name = "lvdtParametersGroupBox";
            this.lvdtParametersGroupBox.Size = new System.Drawing.Size(272, 216);
            this.lvdtParametersGroupBox.TabIndex = 4;
            this.lvdtParametersGroupBox.TabStop = false;
            this.lvdtParametersGroupBox.Text = "LVDT Parameters";
            // 
            // excitationValueNumeric
            // 
            this.excitationValueNumeric.Location = new System.Drawing.Point(128, 56);
            this.excitationValueNumeric.Maximum = new System.Decimal(new int[] {
                                                                                   1000000,
                                                                                   0,
                                                                                   0,
                                                                                   0});
            this.excitationValueNumeric.Name = "excitationValueNumeric";
            this.excitationValueNumeric.Size = new System.Drawing.Size(136, 20);
            this.excitationValueNumeric.TabIndex = 3;
            this.excitationValueNumeric.Value = new System.Decimal(new int[] {
                                                                                 1,
                                                                                 0,
                                                                                 0,
                                                                                 0});
            // 
            // sensitivityNumeric
            // 
            this.sensitivityNumeric.Location = new System.Drawing.Point(128, 152);
            this.sensitivityNumeric.Maximum = new System.Decimal(new int[] {
                                                                               1000000,
                                                                               0,
                                                                               0,
                                                                               0});
            this.sensitivityNumeric.Name = "sensitivityNumeric";
            this.sensitivityNumeric.Size = new System.Drawing.Size(136, 20);
            this.sensitivityNumeric.TabIndex = 9;
            this.sensitivityNumeric.Value = new System.Decimal(new int[] {
                                                                             50,
                                                                             0,
                                                                             0,
                                                                             0});
            // 
            // excitationFrequencyNumeric
            // 
            this.excitationFrequencyNumeric.Location = new System.Drawing.Point(128, 120);
            this.excitationFrequencyNumeric.Maximum = new System.Decimal(new int[] {
                                                                                       100000,
                                                                                       0,
                                                                                       0,
                                                                                       0});
            this.excitationFrequencyNumeric.Name = "excitationFrequencyNumeric";
            this.excitationFrequencyNumeric.Size = new System.Drawing.Size(136, 20);
            this.excitationFrequencyNumeric.TabIndex = 7;
            this.excitationFrequencyNumeric.Value = new System.Decimal(new int[] {
                                                                                     2500,
                                                                                     0,
                                                                                     0,
                                                                                     0});
            // 
            // excitationFrequencyLabel
            // 
            this.excitationFrequencyLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.excitationFrequencyLabel.Location = new System.Drawing.Point(16, 122);
            this.excitationFrequencyLabel.Name = "excitationFrequencyLabel";
            this.excitationFrequencyLabel.Size = new System.Drawing.Size(120, 16);
            this.excitationFrequencyLabel.TabIndex = 6;
            this.excitationFrequencyLabel.Text = "Excitation Frequency:";
            // 
            // excitationSourceComboBox
            // 
            this.excitationSourceComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.excitationSourceComboBox.Items.AddRange(new object[] {
                                                                          "Internal",
                                                                          "External",
                                                                          "None"});
            this.excitationSourceComboBox.Location = new System.Drawing.Point(128, 88);
            this.excitationSourceComboBox.Name = "excitationSourceComboBox";
            this.excitationSourceComboBox.Size = new System.Drawing.Size(136, 21);
            this.excitationSourceComboBox.TabIndex = 5;
            // 
            // excitationSourceLabel
            // 
            this.excitationSourceLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.excitationSourceLabel.Location = new System.Drawing.Point(16, 90);
            this.excitationSourceLabel.Name = "excitationSourceLabel";
            this.excitationSourceLabel.Size = new System.Drawing.Size(104, 16);
            this.excitationSourceLabel.TabIndex = 4;
            this.excitationSourceLabel.Text = "Excitation Source:";
            // 
            // excitationWireModeComboBox
            // 
            this.excitationWireModeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.excitationWireModeComboBox.Items.AddRange(new object[] {
                                                                            "4-Wire",
                                                                            "5-Wire"});
            this.excitationWireModeComboBox.Location = new System.Drawing.Point(128, 24);
            this.excitationWireModeComboBox.Name = "excitationWireModeComboBox";
            this.excitationWireModeComboBox.Size = new System.Drawing.Size(136, 21);
            this.excitationWireModeComboBox.TabIndex = 1;
            // 
            // sensitivityUnitsComboBox
            // 
            this.sensitivityUnitsComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.sensitivityUnitsComboBox.Items.AddRange(new object[] {
                                                                          "mVolts/Volt/mMeter",
                                                                          "mVolts/Volt/0.001 Inch"});
            this.sensitivityUnitsComboBox.Location = new System.Drawing.Point(128, 184);
            this.sensitivityUnitsComboBox.Name = "sensitivityUnitsComboBox";
            this.sensitivityUnitsComboBox.Size = new System.Drawing.Size(136, 21);
            this.sensitivityUnitsComboBox.TabIndex = 11;
            // 
            // excitationValueLabel
            // 
            this.excitationValueLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.excitationValueLabel.Location = new System.Drawing.Point(16, 58);
            this.excitationValueLabel.Name = "excitationValueLabel";
            this.excitationValueLabel.Size = new System.Drawing.Size(96, 16);
            this.excitationValueLabel.TabIndex = 2;
            this.excitationValueLabel.Text = "Excitation Value:";
            // 
            // excitationWireModeLabel
            // 
            this.excitationWireModeLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.excitationWireModeLabel.Location = new System.Drawing.Point(16, 26);
            this.excitationWireModeLabel.Name = "excitationWireModeLabel";
            this.excitationWireModeLabel.Size = new System.Drawing.Size(128, 16);
            this.excitationWireModeLabel.TabIndex = 0;
            this.excitationWireModeLabel.Text = "Excitation Wire Mode:";
            // 
            // sensitivityUnitsLabel
            // 
            this.sensitivityUnitsLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.sensitivityUnitsLabel.Location = new System.Drawing.Point(16, 186);
            this.sensitivityUnitsLabel.Name = "sensitivityUnitsLabel";
            this.sensitivityUnitsLabel.Size = new System.Drawing.Size(88, 16);
            this.sensitivityUnitsLabel.TabIndex = 10;
            this.sensitivityUnitsLabel.Text = "Sensitivity Units:";
            // 
            // sensitivityLabel
            // 
            this.sensitivityLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.sensitivityLabel.Location = new System.Drawing.Point(16, 154);
            this.sensitivityLabel.Name = "sensitivityLabel";
            this.sensitivityLabel.Size = new System.Drawing.Size(72, 16);
            this.sensitivityLabel.TabIndex = 8;
            this.sensitivityLabel.Text = "Sensitivity:";
            // 
            // acquisitionResultsGroupBox
            // 
            this.acquisitionResultsGroupBox.Controls.Add(this.resultLabel);
            this.acquisitionResultsGroupBox.Controls.Add(this.acquisitionDataGrid);
            this.acquisitionResultsGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.acquisitionResultsGroupBox.Location = new System.Drawing.Point(288, 56);
            this.acquisitionResultsGroupBox.Name = "acquisitionResultsGroupBox";
            this.acquisitionResultsGroupBox.Size = new System.Drawing.Size(232, 264);
            this.acquisitionResultsGroupBox.TabIndex = 6;
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
            this.acquisitionDataGrid.Size = new System.Drawing.Size(208, 224);
            this.acquisitionDataGrid.TabIndex = 1;
            this.acquisitionDataGrid.TabStop = false;
            // 
            // deviceParametersGroupBox
            // 
            this.deviceParametersGroupBox.Controls.Add(this.synchronizationEnabledCheckBox);
            this.deviceParametersGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.deviceParametersGroupBox.Location = new System.Drawing.Point(288, 8);
            this.deviceParametersGroupBox.Name = "deviceParametersGroupBox";
            this.deviceParametersGroupBox.Size = new System.Drawing.Size(232, 40);
            this.deviceParametersGroupBox.TabIndex = 5;
            this.deviceParametersGroupBox.TabStop = false;
            this.deviceParametersGroupBox.Text = "Device Parameters";
            // 
            // synchronizationEnabledCheckBox
            // 
            this.synchronizationEnabledCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.synchronizationEnabledCheckBox.Location = new System.Drawing.Point(16, 16);
            this.synchronizationEnabledCheckBox.Name = "synchronizationEnabledCheckBox";
            this.synchronizationEnabledCheckBox.Size = new System.Drawing.Size(160, 16);
            this.synchronizationEnabledCheckBox.TabIndex = 0;
            this.synchronizationEnabledCheckBox.Text = "Synchronization Enabled?";
            // 
            // stopButton
            // 
            this.stopButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.stopButton.Location = new System.Drawing.Point(360, 392);
            this.stopButton.Name = "stopButton";
            this.stopButton.Size = new System.Drawing.Size(80, 24);
            this.stopButton.TabIndex = 1;
            this.stopButton.Text = "Stop";
            this.stopButton.Click += new System.EventHandler(this.stopButton_Click);
            // 
            // startButton
            // 
            this.startButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.startButton.Location = new System.Drawing.Point(360, 360);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(80, 24);
            this.startButton.TabIndex = 0;
            this.startButton.Text = "Start";
            this.startButton.Click += new System.EventHandler(this.startButton_Click);
            // 
            // MainForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(530, 488);
            this.Controls.Add(this.stopButton);
            this.Controls.Add(this.startButton);
            this.Controls.Add(this.deviceParametersGroupBox);
            this.Controls.Add(this.acquisitionResultsGroupBox);
            this.Controls.Add(this.lvdtParametersGroupBox);
            this.Controls.Add(this.timingParametersGroupBox);
            this.Controls.Add(this.channelParametersGroupBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Continuous Acquisition LVDT Samples - Internal Clock - SCXI1540";
            this.channelParametersGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.minimumValueNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.maximumValueNumeric)).EndInit();
            this.timingParametersGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.samplesPerChannelNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.rateNumeric)).EndInit();
            this.lvdtParametersGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.excitationValueNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sensitivityNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.excitationFrequencyNumeric)).EndInit();
            this.acquisitionResultsGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.acquisitionDataGrid)).EndInit();
            this.deviceParametersGroupBox.ResumeLayout(false);
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
            if(runningTask == null)
            {
                try 
                {
                    stopButton.Enabled = true;
                    startButton.Enabled = false;

                    // Get Sensitivity Units
                    switch(sensitivityUnitsComboBox.SelectedItem.ToString())
                    {
                        case "mVolts/Volt/mMeter":
                            sensitivityUnits = AILvdtSensitivityUnits.MillivoltsPerVoltPerMillimeter;
                            break;
                        case "mVolts/Volt/0.001 Inch":
                        default:
                            sensitivityUnits = AILvdtSensitivityUnits.MillivoltsPerVoltPerMilliinch;
                            break;
                    }

                    // Get Wire Mode
                    switch(excitationWireModeComboBox.SelectedItem.ToString())
                    {
                        case "4-Wire":
                            wireMode = AIACExcitationWireMode.FourWire;
                            break;
                        case "5-Wire":
                        default:
                            wireMode = AIACExcitationWireMode.FiveWire;
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
                                    
                    // Create a virtual channel
                    switch(unitsComboBox.SelectedIndex)
                    {
                        case 0:  // Meters
                            myTask.AIChannels.CreateLvdtChannel(physicalChannelComboBox.Text,"",
                                Convert.ToDouble(minimumValueNumeric.Value),Convert.ToDouble(maximumValueNumeric.Value),
                                Convert.ToDouble(sensitivityNumeric.Value),sensitivityUnits,excitationSource,
                                Convert.ToDouble(excitationValueNumeric.Value),
                                Convert.ToDouble(excitationFrequencyNumeric.Value),wireMode,
                                AILvdtUnits.Meters);
                            break;
                        case 1: // Inches
                            myTask.AIChannels.CreateLvdtChannel(physicalChannelComboBox.Text,"",
                                Convert.ToDouble(minimumValueNumeric.Value),Convert.ToDouble(maximumValueNumeric.Value),
                                Convert.ToDouble(sensitivityNumeric.Value),sensitivityUnits,excitationSource,
                                Convert.ToDouble(excitationValueNumeric.Value),
                                Convert.ToDouble(excitationFrequencyNumeric.Value), wireMode,
                                AILvdtUnits.Inches);
                            break;
                        default:  // If not either of the above then it is a custom scale
                            myTask.AIChannels.CreateLvdtChannel(physicalChannelComboBox.Text,"",
                                Convert.ToDouble(minimumValueNumeric.Value),Convert.ToDouble(maximumValueNumeric.Value),
                                Convert.ToDouble(sensitivityNumeric.Value),sensitivityUnits,excitationSource,
                                Convert.ToDouble(excitationValueNumeric.Value),
                                Convert.ToDouble(excitationFrequencyNumeric.Value), wireMode,
                                unitsComboBox.Text); // string entered in combobox that is the custom scale name
                            break;
                    }

                    // Synchronization Enabled ?
                    if (synchronizationEnabledCheckBox.Checked)
                    {
                        myTask.AIChannels.All.ACExcitationSyncEnable = true;
                    }
                    else
                    {
                        myTask.AIChannels.All.ACExcitationSyncEnable = false;
                    }
                
                    // Configure the timing parameters
                    myTask.Timing.ConfigureSampleClock("", Convert.ToDouble(rateNumeric.Value),
                        SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, 1000);
                    
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

                    // Begin next read
                    analogInReader.BeginMemoryOptimizedReadWaveform(Convert.ToInt32(samplesPerChannelNumeric.Value),
                        analogCallback, myTask, data);
                }
            }
            catch(DaqException exception)
            {   
                // Display Errors
                MessageBox.Show(exception.Message);
                runningTask = null;
                myTask.Dispose();
                stopButton.Enabled = false;
                startButton.Enabled = true;
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
    }
}
