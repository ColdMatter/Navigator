'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
'
' Example program:
'   ContAcqLVDTSamples_IntClk_SCXI1540
'
' Category:
'   AI
'
' Description:
'   This example demonstrates how to make a continuous, hardware-timed
'   acceleration measurement using an SCXI-1540 module.
'
' Instructions for running:
'   1.  Specify the physical channel where you have connected the LVDT.
'   2.  Enter the minimum and maximum distance values, in units based on the
'       units control, that you expect to measure. A smaller range will allow a
'       more accurate measurement.
'   3.  Select the number of samples to acquire.
'   4.  Set the rate of the acquisition.
'   5.  Specify the LVDT settings.
'   6.  If you are using multiple LVDTs and would like to synchronize their
'       excitations, then enable synchronization for all the secondary LVDT
'       channels via the Synchronization Enabled button. You must also connect
'       the excitation output (EX+) of your primary LVDT channel to all the
'       secondary LVDT channel's sync pin (SYNC).
'
' Steps:
'   1.  Create a new analog input task.
'   2.  Create an analog input LVDT channel.
'   3.  Configure the synchronization of the SCXI-1540 module.
'   4.  Set up the timing for the acquisition. In this example we use the DAQ
'       device's internal clock to read samples continuously.
'   5.  Call AnalogMultiChannelReader.BeginReadWaveform to install a callback
'       and begin the asynchronous read operation.
'   6.  Inside the callback, call AnalogMultiChannelReader.EndReadWaveform to
'       retrieve the data from the read operation.  
'   7.  Call AnalogMultiChannelReader.BeginMemoryOptimizedReadWaveform
'   8.  Dispose the Task object to clean-up any resources associated with the
'       task.
'   9.  Handle any DaqExceptions, if they occur.
'
'   Note: This example sets SynchronizeCallback to true. If SynchronizeCallback
'   is set to false, then you must give special consideration to safely dispose
'   the task and to update the UI from the callback. If SynchronizeCallback is
'   set to false, the callback executes on the worker thread and not on the main
'   UI thread. You can only update a UI component on the thread on which it was
'   created. Refer to the How to: Safely Dispose Task When Using Asynchronous
'   Callbacks topic in the NI-DAQmx .NET help for more information.
'
' I/O Connections Overview:
'   Connect your LVDT to the terminals corresponding to the physical channel I/O
'   control value. The excitation lines connect to EX+ and EX- while the analog
'   input lines connect to CH+ and CH-.  For more information on the input and
'   output terminals for your device, open the NI-DAQmx Help, and refer to the
'   NI-DAQmx Device Terminals and Device Considerations books in the table of
'   contents.
'
' Microsoft Windows Vista User Account Control
'   Running certain applications on Microsoft Windows Vista requires
'   administrator privileges, 
'   because the application name contains keywords such as setup, update, or
'   install. To avoid this problem, 
'   you must add an additional manifest to the application that specifies the
'   privileges required to run 
'   the application. Some Measurement Studio NI-DAQmx examples for Visual Studio
'   include these keywords. 
'   Therefore, all examples for Visual Studio are shipped with an additional
'   manifest file that you must 
'   embed in the example executable. The manifest file is named
'   [ExampleName].exe.manifest, where [ExampleName] 
'   is the NI-provided example name. For information on how to embed the manifest
'   file, refer to http://msdn2.microsoft.com/en-us/library/bb756929.aspx.Note: 
'   The manifest file is not provided with examples for Visual Studio .NET 2003.
'
'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

Public Class MainFrom
    Inherits System.Windows.Forms.Form

    Private analogInReader As AnalogMultiChannelReader
    Private excitationSource As AIExcitationSource
    Private sensitivityUnits As AILvdtSensitivityUnits
    Private wireMode As AIACExcitationWireMode

    Private myTask As Task
    Private runningTask As Task
    Private analogCallback As AsyncCallback
    Private data As AnalogWaveform(Of Double)()
    Private dataColumn As DataColumn() = Nothing
    Private dataTable As DataTable = Nothing



#Region " Windows Form Designer generated code "

    Public Sub New()
        MyBase.New()
        Application.EnableVisualStyles()

        'This call is required by the Windows Form Designer.
        InitializeComponent()

        'Add any initialization after the InitializeComponent() call
        stopButton.Enabled = False
        dataTable = New DataTable
        unitsComboBox.SelectedIndex = 0
        sensitivityUnitsComboBox.SelectedIndex = 0
        excitationSourceComboBox.SelectedIndex = 0
        excitationWireModeComboBox.SelectedIndex = 0

        physicalChannelComboBox.Items.AddRange(DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AI, PhysicalChannelAccess.External))
        If (physicalChannelComboBox.Items.Count > 0) Then
            physicalChannelComboBox.SelectedIndex = 0
        End If

    End Sub

    'Form overrides dispose to clean up the component list.
    Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
        If disposing Then
            If Not (components Is Nothing) Then
                components.Dispose()
            End If
            If Not (myTask Is Nothing) Then
                runningTask = Nothing
                myTask.Dispose()
            End If
        End If
        MyBase.Dispose(disposing)
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    Friend WithEvents stopButton As System.Windows.Forms.Button
    Friend WithEvents startButton As System.Windows.Forms.Button
    Friend WithEvents timingParametersGroupBox As System.Windows.Forms.GroupBox
    Friend WithEvents samplesPerChannelNumeric As System.Windows.Forms.NumericUpDown
    Friend WithEvents rateNumeric As System.Windows.Forms.NumericUpDown
    Friend WithEvents samplesLabel As System.Windows.Forms.Label
    Friend WithEvents rateLabel As System.Windows.Forms.Label
    Friend WithEvents acquisitionResultsGroupBox As System.Windows.Forms.GroupBox
    Friend WithEvents resultLabel As System.Windows.Forms.Label
    Friend WithEvents acquisitionDataGrid As System.Windows.Forms.DataGrid
    Friend WithEvents deviceParametersGroupBox As System.Windows.Forms.GroupBox
    Friend WithEvents synchronizationEnabledCheckBox As System.Windows.Forms.CheckBox
    Friend WithEvents lvdtParametersGroupBox As System.Windows.Forms.GroupBox
    Friend WithEvents excitationValueNumeric As System.Windows.Forms.NumericUpDown
    Friend WithEvents sensitivityNumeric As System.Windows.Forms.NumericUpDown
    Friend WithEvents excitationFrequencyNumeric As System.Windows.Forms.NumericUpDown
    Friend WithEvents excitationFrequencyLabel As System.Windows.Forms.Label
    Friend WithEvents excitationSourceComboBox As System.Windows.Forms.ComboBox
    Friend WithEvents excitationSourceLabel As System.Windows.Forms.Label
    Friend WithEvents excitationWireModeComboBox As System.Windows.Forms.ComboBox
    Friend WithEvents sensitivityUnitsComboBox As System.Windows.Forms.ComboBox
    Friend WithEvents excitationValueLabel As System.Windows.Forms.Label
    Friend WithEvents excitationWireModeLabel As System.Windows.Forms.Label
    Friend WithEvents sensitivityUnitsLabel As System.Windows.Forms.Label
    Friend WithEvents sensitivityLabel As System.Windows.Forms.Label
    Friend WithEvents channelParametersGroupBox As System.Windows.Forms.GroupBox
    Friend WithEvents unitsComboBox As System.Windows.Forms.ComboBox
    Friend WithEvents unitsLabel As System.Windows.Forms.Label
    Friend WithEvents maximumLabel As System.Windows.Forms.Label
    Friend WithEvents minimumLabel As System.Windows.Forms.Label
    Friend WithEvents physicalChannelLabel As System.Windows.Forms.Label
    Friend WithEvents minimumValueNumeric As System.Windows.Forms.NumericUpDown
    Friend WithEvents maximumValueNumeric As System.Windows.Forms.NumericUpDown
    Friend WithEvents physicalChannelComboBox As System.Windows.Forms.ComboBox
    Friend WithEvents GroupBox1 As System.Windows.Forms.GroupBox
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents DataGrid1 As System.Windows.Forms.DataGrid
    Friend WithEvents GroupBox2 As System.Windows.Forms.GroupBox
    Friend WithEvents CheckBox1 As System.Windows.Forms.CheckBox
    Friend WithEvents GroupBox3 As System.Windows.Forms.GroupBox
    Friend WithEvents NumericUpDown1 As System.Windows.Forms.NumericUpDown
    Friend WithEvents NumericUpDown2 As System.Windows.Forms.NumericUpDown
    Friend WithEvents NumericUpDown3 As System.Windows.Forms.NumericUpDown
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents ComboBox1 As System.Windows.Forms.ComboBox
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents ComboBox2 As System.Windows.Forms.ComboBox
    Friend WithEvents ComboBox3 As System.Windows.Forms.ComboBox
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents Label5 As System.Windows.Forms.Label
    Friend WithEvents Label6 As System.Windows.Forms.Label
    Friend WithEvents Label7 As System.Windows.Forms.Label
    Friend WithEvents Button1 As System.Windows.Forms.Button
    Friend WithEvents GroupBox4 As System.Windows.Forms.GroupBox
    Friend WithEvents ComboBox4 As System.Windows.Forms.ComboBox
    Friend WithEvents NumericUpDown4 As System.Windows.Forms.NumericUpDown
    Friend WithEvents NumericUpDown5 As System.Windows.Forms.NumericUpDown
    Friend WithEvents ComboBox5 As System.Windows.Forms.ComboBox
    Friend WithEvents Label8 As System.Windows.Forms.Label
    Friend WithEvents Label9 As System.Windows.Forms.Label
    Friend WithEvents Label10 As System.Windows.Forms.Label
    Friend WithEvents Label11 As System.Windows.Forms.Label
    Friend WithEvents GroupBox5 As System.Windows.Forms.GroupBox
    Friend WithEvents NumericUpDown6 As System.Windows.Forms.NumericUpDown
    Friend WithEvents NumericUpDown7 As System.Windows.Forms.NumericUpDown
    Friend WithEvents Label12 As System.Windows.Forms.Label
    Friend WithEvents Label13 As System.Windows.Forms.Label
    <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
        Dim resources As System.Resources.ResourceManager = New System.Resources.ResourceManager(GetType(MainFrom))
        Me.stopButton = New System.Windows.Forms.Button
        Me.startButton = New System.Windows.Forms.Button
        Me.timingParametersGroupBox = New System.Windows.Forms.GroupBox
        Me.samplesPerChannelNumeric = New System.Windows.Forms.NumericUpDown
        Me.rateNumeric = New System.Windows.Forms.NumericUpDown
        Me.samplesLabel = New System.Windows.Forms.Label
        Me.rateLabel = New System.Windows.Forms.Label
        Me.acquisitionResultsGroupBox = New System.Windows.Forms.GroupBox
        Me.resultLabel = New System.Windows.Forms.Label
        Me.acquisitionDataGrid = New System.Windows.Forms.DataGrid
        Me.deviceParametersGroupBox = New System.Windows.Forms.GroupBox
        Me.synchronizationEnabledCheckBox = New System.Windows.Forms.CheckBox
        Me.lvdtParametersGroupBox = New System.Windows.Forms.GroupBox
        Me.excitationValueNumeric = New System.Windows.Forms.NumericUpDown
        Me.sensitivityNumeric = New System.Windows.Forms.NumericUpDown
        Me.excitationFrequencyNumeric = New System.Windows.Forms.NumericUpDown
        Me.excitationFrequencyLabel = New System.Windows.Forms.Label
        Me.excitationSourceComboBox = New System.Windows.Forms.ComboBox
        Me.excitationSourceLabel = New System.Windows.Forms.Label
        Me.excitationWireModeComboBox = New System.Windows.Forms.ComboBox
        Me.sensitivityUnitsComboBox = New System.Windows.Forms.ComboBox
        Me.excitationValueLabel = New System.Windows.Forms.Label
        Me.excitationWireModeLabel = New System.Windows.Forms.Label
        Me.sensitivityUnitsLabel = New System.Windows.Forms.Label
        Me.sensitivityLabel = New System.Windows.Forms.Label
        Me.channelParametersGroupBox = New System.Windows.Forms.GroupBox
        Me.physicalChannelComboBox = New System.Windows.Forms.ComboBox
        Me.minimumValueNumeric = New System.Windows.Forms.NumericUpDown
        Me.maximumValueNumeric = New System.Windows.Forms.NumericUpDown
        Me.unitsComboBox = New System.Windows.Forms.ComboBox
        Me.unitsLabel = New System.Windows.Forms.Label
        Me.maximumLabel = New System.Windows.Forms.Label
        Me.minimumLabel = New System.Windows.Forms.Label
        Me.physicalChannelLabel = New System.Windows.Forms.Label
        Me.GroupBox1 = New System.Windows.Forms.GroupBox
        Me.Label1 = New System.Windows.Forms.Label
        Me.DataGrid1 = New System.Windows.Forms.DataGrid
        Me.GroupBox2 = New System.Windows.Forms.GroupBox
        Me.CheckBox1 = New System.Windows.Forms.CheckBox
        Me.GroupBox3 = New System.Windows.Forms.GroupBox
        Me.NumericUpDown1 = New System.Windows.Forms.NumericUpDown
        Me.NumericUpDown2 = New System.Windows.Forms.NumericUpDown
        Me.NumericUpDown3 = New System.Windows.Forms.NumericUpDown
        Me.Label2 = New System.Windows.Forms.Label
        Me.ComboBox1 = New System.Windows.Forms.ComboBox
        Me.Label3 = New System.Windows.Forms.Label
        Me.ComboBox2 = New System.Windows.Forms.ComboBox
        Me.ComboBox3 = New System.Windows.Forms.ComboBox
        Me.Label4 = New System.Windows.Forms.Label
        Me.Label5 = New System.Windows.Forms.Label
        Me.Label6 = New System.Windows.Forms.Label
        Me.Label7 = New System.Windows.Forms.Label
        Me.Button1 = New System.Windows.Forms.Button
        Me.GroupBox4 = New System.Windows.Forms.GroupBox
        Me.ComboBox4 = New System.Windows.Forms.ComboBox
        Me.NumericUpDown4 = New System.Windows.Forms.NumericUpDown
        Me.NumericUpDown5 = New System.Windows.Forms.NumericUpDown
        Me.ComboBox5 = New System.Windows.Forms.ComboBox
        Me.Label8 = New System.Windows.Forms.Label
        Me.Label9 = New System.Windows.Forms.Label
        Me.Label10 = New System.Windows.Forms.Label
        Me.Label11 = New System.Windows.Forms.Label
        Me.GroupBox5 = New System.Windows.Forms.GroupBox
        Me.NumericUpDown6 = New System.Windows.Forms.NumericUpDown
        Me.NumericUpDown7 = New System.Windows.Forms.NumericUpDown
        Me.Label12 = New System.Windows.Forms.Label
        Me.Label13 = New System.Windows.Forms.Label
        Me.timingParametersGroupBox.SuspendLayout()
        CType(Me.samplesPerChannelNumeric, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.rateNumeric, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.acquisitionResultsGroupBox.SuspendLayout()
        CType(Me.acquisitionDataGrid, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.deviceParametersGroupBox.SuspendLayout()
        Me.lvdtParametersGroupBox.SuspendLayout()
        CType(Me.excitationValueNumeric, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.sensitivityNumeric, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.excitationFrequencyNumeric, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.channelParametersGroupBox.SuspendLayout()
        CType(Me.minimumValueNumeric, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.maximumValueNumeric, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox1.SuspendLayout()
        CType(Me.DataGrid1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox2.SuspendLayout()
        Me.GroupBox3.SuspendLayout()
        CType(Me.NumericUpDown1, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.NumericUpDown2, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.NumericUpDown3, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox4.SuspendLayout()
        CType(Me.NumericUpDown4, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.NumericUpDown5, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox5.SuspendLayout()
        CType(Me.NumericUpDown6, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.NumericUpDown7, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'stopButton
        '
        Me.stopButton.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.stopButton.Location = New System.Drawing.Point(360, 392)
        Me.stopButton.Name = "stopButton"
        Me.stopButton.Size = New System.Drawing.Size(80, 24)
        Me.stopButton.TabIndex = 1
        Me.stopButton.Text = "Stop"
        '
        'startButton
        '
        Me.startButton.Location = New System.Drawing.Point(360, 360)
        Me.startButton.Name = "startButton"
        Me.startButton.Size = New System.Drawing.Size(80, 24)
        Me.startButton.TabIndex = 0
        Me.startButton.Text = "Start"
        '
        'timingParametersGroupBox
        '
        Me.timingParametersGroupBox.Controls.Add(Me.samplesPerChannelNumeric)
        Me.timingParametersGroupBox.Controls.Add(Me.rateNumeric)
        Me.timingParametersGroupBox.Controls.Add(Me.samplesLabel)
        Me.timingParametersGroupBox.Controls.Add(Me.rateLabel)
        Me.timingParametersGroupBox.Location = New System.Drawing.Point(8, 168)
        Me.timingParametersGroupBox.Name = "timingParametersGroupBox"
        Me.timingParametersGroupBox.Size = New System.Drawing.Size(272, 88)
        Me.timingParametersGroupBox.TabIndex = 3
        Me.timingParametersGroupBox.TabStop = False
        Me.timingParametersGroupBox.Text = "Timing Parameters"
        '
        'samplesPerChannelNumeric
        '
        Me.samplesPerChannelNumeric.Location = New System.Drawing.Point(128, 56)
        Me.samplesPerChannelNumeric.Maximum = New Decimal(New Integer() {1000000, 0, 0, 0})
        Me.samplesPerChannelNumeric.Name = "samplesPerChannelNumeric"
        Me.samplesPerChannelNumeric.Size = New System.Drawing.Size(136, 20)
        Me.samplesPerChannelNumeric.TabIndex = 3
        Me.samplesPerChannelNumeric.Value = New Decimal(New Integer() {1000, 0, 0, 0})
        '
        'rateNumeric
        '
        Me.rateNumeric.DecimalPlaces = 2
        Me.rateNumeric.Location = New System.Drawing.Point(128, 24)
        Me.rateNumeric.Maximum = New Decimal(New Integer() {1000000, 0, 0, 0})
        Me.rateNumeric.Name = "rateNumeric"
        Me.rateNumeric.Size = New System.Drawing.Size(136, 20)
        Me.rateNumeric.TabIndex = 1
        Me.rateNumeric.Value = New Decimal(New Integer() {10000, 0, 0, 0})
        '
        'samplesLabel
        '
        Me.samplesLabel.Location = New System.Drawing.Point(16, 58)
        Me.samplesLabel.Name = "samplesLabel"
        Me.samplesLabel.Size = New System.Drawing.Size(120, 16)
        Me.samplesLabel.TabIndex = 2
        Me.samplesLabel.Text = "Samples / Channel:"
        '
        'rateLabel
        '
        Me.rateLabel.Location = New System.Drawing.Point(16, 26)
        Me.rateLabel.Name = "rateLabel"
        Me.rateLabel.Size = New System.Drawing.Size(56, 16)
        Me.rateLabel.TabIndex = 0
        Me.rateLabel.Text = "Rate (Hz):"
        '
        'acquisitionResultsGroupBox
        '
        Me.acquisitionResultsGroupBox.Controls.Add(Me.resultLabel)
        Me.acquisitionResultsGroupBox.Controls.Add(Me.acquisitionDataGrid)
        Me.acquisitionResultsGroupBox.Location = New System.Drawing.Point(288, 56)
        Me.acquisitionResultsGroupBox.Name = "acquisitionResultsGroupBox"
        Me.acquisitionResultsGroupBox.Size = New System.Drawing.Size(232, 264)
        Me.acquisitionResultsGroupBox.TabIndex = 6
        Me.acquisitionResultsGroupBox.TabStop = False
        Me.acquisitionResultsGroupBox.Text = "Acquisition Results"
        '
        'resultLabel
        '
        Me.resultLabel.Location = New System.Drawing.Point(16, 16)
        Me.resultLabel.Name = "resultLabel"
        Me.resultLabel.Size = New System.Drawing.Size(112, 16)
        Me.resultLabel.TabIndex = 0
        Me.resultLabel.Text = "Acquisition Data:"
        '
        'acquisitionDataGrid
        '
        Me.acquisitionDataGrid.AllowSorting = False
        Me.acquisitionDataGrid.DataMember = ""
        Me.acquisitionDataGrid.HeaderForeColor = System.Drawing.SystemColors.ControlText
        Me.acquisitionDataGrid.Location = New System.Drawing.Point(16, 32)
        Me.acquisitionDataGrid.Name = "acquisitionDataGrid"
        Me.acquisitionDataGrid.ParentRowsVisible = False
        Me.acquisitionDataGrid.ReadOnly = True
        Me.acquisitionDataGrid.Size = New System.Drawing.Size(208, 224)
        Me.acquisitionDataGrid.TabIndex = 1
        Me.acquisitionDataGrid.TabStop = False
        '
        'deviceParametersGroupBox
        '
        Me.deviceParametersGroupBox.Controls.Add(Me.synchronizationEnabledCheckBox)
        Me.deviceParametersGroupBox.Location = New System.Drawing.Point(288, 8)
        Me.deviceParametersGroupBox.Name = "deviceParametersGroupBox"
        Me.deviceParametersGroupBox.Size = New System.Drawing.Size(232, 40)
        Me.deviceParametersGroupBox.TabIndex = 5
        Me.deviceParametersGroupBox.TabStop = False
        Me.deviceParametersGroupBox.Text = "Device Parameters"
        '
        'synchronizationEnabledCheckBox
        '
        Me.synchronizationEnabledCheckBox.Location = New System.Drawing.Point(16, 16)
        Me.synchronizationEnabledCheckBox.Name = "synchronizationEnabledCheckBox"
        Me.synchronizationEnabledCheckBox.Size = New System.Drawing.Size(160, 16)
        Me.synchronizationEnabledCheckBox.TabIndex = 0
        Me.synchronizationEnabledCheckBox.Text = "Synchronization Enabled?"
        '
        'lvdtParametersGroupBox
        '
        Me.lvdtParametersGroupBox.Controls.Add(Me.excitationValueNumeric)
        Me.lvdtParametersGroupBox.Controls.Add(Me.sensitivityNumeric)
        Me.lvdtParametersGroupBox.Controls.Add(Me.excitationFrequencyNumeric)
        Me.lvdtParametersGroupBox.Controls.Add(Me.excitationFrequencyLabel)
        Me.lvdtParametersGroupBox.Controls.Add(Me.excitationSourceComboBox)
        Me.lvdtParametersGroupBox.Controls.Add(Me.excitationSourceLabel)
        Me.lvdtParametersGroupBox.Controls.Add(Me.excitationWireModeComboBox)
        Me.lvdtParametersGroupBox.Controls.Add(Me.sensitivityUnitsComboBox)
        Me.lvdtParametersGroupBox.Controls.Add(Me.excitationValueLabel)
        Me.lvdtParametersGroupBox.Controls.Add(Me.excitationWireModeLabel)
        Me.lvdtParametersGroupBox.Controls.Add(Me.sensitivityUnitsLabel)
        Me.lvdtParametersGroupBox.Controls.Add(Me.sensitivityLabel)
        Me.lvdtParametersGroupBox.Location = New System.Drawing.Point(8, 264)
        Me.lvdtParametersGroupBox.Name = "lvdtParametersGroupBox"
        Me.lvdtParametersGroupBox.Size = New System.Drawing.Size(272, 216)
        Me.lvdtParametersGroupBox.TabIndex = 4
        Me.lvdtParametersGroupBox.TabStop = False
        Me.lvdtParametersGroupBox.Text = "LVDT Parameters"
        '
        'excitationValueNumeric
        '
        Me.excitationValueNumeric.Location = New System.Drawing.Point(128, 56)
        Me.excitationValueNumeric.Maximum = New Decimal(New Integer() {1000000, 0, 0, 0})
        Me.excitationValueNumeric.Name = "excitationValueNumeric"
        Me.excitationValueNumeric.Size = New System.Drawing.Size(136, 20)
        Me.excitationValueNumeric.TabIndex = 3
        Me.excitationValueNumeric.Value = New Decimal(New Integer() {1, 0, 0, 0})
        '
        'sensitivityNumeric
        '
        Me.sensitivityNumeric.Location = New System.Drawing.Point(128, 152)
        Me.sensitivityNumeric.Maximum = New Decimal(New Integer() {1000000, 0, 0, 0})
        Me.sensitivityNumeric.Name = "sensitivityNumeric"
        Me.sensitivityNumeric.Size = New System.Drawing.Size(136, 20)
        Me.sensitivityNumeric.TabIndex = 9
        Me.sensitivityNumeric.Value = New Decimal(New Integer() {50, 0, 0, 0})
        '
        'excitationFrequencyNumeric
        '
        Me.excitationFrequencyNumeric.Location = New System.Drawing.Point(128, 120)
        Me.excitationFrequencyNumeric.Maximum = New Decimal(New Integer() {100000, 0, 0, 0})
        Me.excitationFrequencyNumeric.Name = "excitationFrequencyNumeric"
        Me.excitationFrequencyNumeric.Size = New System.Drawing.Size(136, 20)
        Me.excitationFrequencyNumeric.TabIndex = 7
        Me.excitationFrequencyNumeric.Value = New Decimal(New Integer() {2500, 0, 0, 0})
        '
        'excitationFrequencyLabel
        '
        Me.excitationFrequencyLabel.Location = New System.Drawing.Point(16, 122)
        Me.excitationFrequencyLabel.Name = "excitationFrequencyLabel"
        Me.excitationFrequencyLabel.Size = New System.Drawing.Size(120, 16)
        Me.excitationFrequencyLabel.TabIndex = 6
        Me.excitationFrequencyLabel.Text = "Excitation Frequency:"
        '
        'excitationSourceComboBox
        '
        Me.excitationSourceComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.excitationSourceComboBox.Items.AddRange(New Object() {"Internal", "External", "None"})
        Me.excitationSourceComboBox.Location = New System.Drawing.Point(128, 88)
        Me.excitationSourceComboBox.Name = "excitationSourceComboBox"
        Me.excitationSourceComboBox.Size = New System.Drawing.Size(136, 21)
        Me.excitationSourceComboBox.TabIndex = 5
        '
        'excitationSourceLabel
        '
        Me.excitationSourceLabel.Location = New System.Drawing.Point(16, 90)
        Me.excitationSourceLabel.Name = "excitationSourceLabel"
        Me.excitationSourceLabel.Size = New System.Drawing.Size(104, 16)
        Me.excitationSourceLabel.TabIndex = 4
        Me.excitationSourceLabel.Text = "Excitation Source:"
        '
        'excitationWireModeComboBox
        '
        Me.excitationWireModeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.excitationWireModeComboBox.Items.AddRange(New Object() {"4-Wire", "5-Wire"})
        Me.excitationWireModeComboBox.Location = New System.Drawing.Point(128, 24)
        Me.excitationWireModeComboBox.Name = "excitationWireModeComboBox"
        Me.excitationWireModeComboBox.Size = New System.Drawing.Size(136, 21)
        Me.excitationWireModeComboBox.TabIndex = 1
        '
        'sensitivityUnitsComboBox
        '
        Me.sensitivityUnitsComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.sensitivityUnitsComboBox.Items.AddRange(New Object() {"mVolts/Volt/mMeter", "mVolts/Volt/0.001 Inch"})
        Me.sensitivityUnitsComboBox.Location = New System.Drawing.Point(128, 184)
        Me.sensitivityUnitsComboBox.Name = "sensitivityUnitsComboBox"
        Me.sensitivityUnitsComboBox.Size = New System.Drawing.Size(136, 21)
        Me.sensitivityUnitsComboBox.TabIndex = 11
        '
        'excitationValueLabel
        '
        Me.excitationValueLabel.Location = New System.Drawing.Point(16, 58)
        Me.excitationValueLabel.Name = "excitationValueLabel"
        Me.excitationValueLabel.Size = New System.Drawing.Size(96, 16)
        Me.excitationValueLabel.TabIndex = 2
        Me.excitationValueLabel.Text = "Excitation Value:"
        '
        'excitationWireModeLabel
        '
        Me.excitationWireModeLabel.Location = New System.Drawing.Point(16, 26)
        Me.excitationWireModeLabel.Name = "excitationWireModeLabel"
        Me.excitationWireModeLabel.Size = New System.Drawing.Size(128, 16)
        Me.excitationWireModeLabel.TabIndex = 0
        Me.excitationWireModeLabel.Text = "Excitation Wire Mode:"
        '
        'sensitivityUnitsLabel
        '
        Me.sensitivityUnitsLabel.Location = New System.Drawing.Point(16, 186)
        Me.sensitivityUnitsLabel.Name = "sensitivityUnitsLabel"
        Me.sensitivityUnitsLabel.Size = New System.Drawing.Size(88, 16)
        Me.sensitivityUnitsLabel.TabIndex = 10
        Me.sensitivityUnitsLabel.Text = "Sensitivity Units:"
        '
        'sensitivityLabel
        '
        Me.sensitivityLabel.Location = New System.Drawing.Point(16, 154)
        Me.sensitivityLabel.Name = "sensitivityLabel"
        Me.sensitivityLabel.Size = New System.Drawing.Size(72, 16)
        Me.sensitivityLabel.TabIndex = 8
        Me.sensitivityLabel.Text = "Sensitivity:"
        '
        'channelParametersGroupBox
        '
        Me.channelParametersGroupBox.Controls.Add(Me.physicalChannelComboBox)
        Me.channelParametersGroupBox.Controls.Add(Me.minimumValueNumeric)
        Me.channelParametersGroupBox.Controls.Add(Me.maximumValueNumeric)
        Me.channelParametersGroupBox.Controls.Add(Me.unitsComboBox)
        Me.channelParametersGroupBox.Controls.Add(Me.unitsLabel)
        Me.channelParametersGroupBox.Controls.Add(Me.maximumLabel)
        Me.channelParametersGroupBox.Controls.Add(Me.minimumLabel)
        Me.channelParametersGroupBox.Controls.Add(Me.physicalChannelLabel)
        Me.channelParametersGroupBox.Location = New System.Drawing.Point(8, 8)
        Me.channelParametersGroupBox.Name = "channelParametersGroupBox"
        Me.channelParametersGroupBox.Size = New System.Drawing.Size(272, 152)
        Me.channelParametersGroupBox.TabIndex = 2
        Me.channelParametersGroupBox.TabStop = False
        Me.channelParametersGroupBox.Text = "Channel Parameters"
        '
        'physicalChannelComboBox
        '
        Me.physicalChannelComboBox.Location = New System.Drawing.Point(128, 24)
        Me.physicalChannelComboBox.Name = "physicalChannelComboBox"
        Me.physicalChannelComboBox.Size = New System.Drawing.Size(136, 21)
        Me.physicalChannelComboBox.TabIndex = 1
        Me.physicalChannelComboBox.Text = "SC1Mod1/ai0"
        '
        'minimumValueNumeric
        '
        Me.minimumValueNumeric.DecimalPlaces = 2
        Me.minimumValueNumeric.Location = New System.Drawing.Point(128, 56)
        Me.minimumValueNumeric.Maximum = New Decimal(New Integer() {10, 0, 0, 0})
        Me.minimumValueNumeric.Minimum = New Decimal(New Integer() {10, 0, 0, -2147483648})
        Me.minimumValueNumeric.Name = "minimumValueNumeric"
        Me.minimumValueNumeric.Size = New System.Drawing.Size(136, 20)
        Me.minimumValueNumeric.TabIndex = 3
        Me.minimumValueNumeric.Value = New Decimal(New Integer() {10, 0, 0, -2147352576})
        '
        'maximumValueNumeric
        '
        Me.maximumValueNumeric.DecimalPlaces = 2
        Me.maximumValueNumeric.Location = New System.Drawing.Point(128, 88)
        Me.maximumValueNumeric.Maximum = New Decimal(New Integer() {10, 0, 0, 0})
        Me.maximumValueNumeric.Minimum = New Decimal(New Integer() {10, 0, 0, -2147483648})
        Me.maximumValueNumeric.Name = "maximumValueNumeric"
        Me.maximumValueNumeric.Size = New System.Drawing.Size(136, 20)
        Me.maximumValueNumeric.TabIndex = 5
        Me.maximumValueNumeric.Value = New Decimal(New Integer() {10, 0, 0, 131072})
        '
        'unitsComboBox
        '
        Me.unitsComboBox.Items.AddRange(New Object() {"Meters", "Inches", "Custom Scale"})
        Me.unitsComboBox.Location = New System.Drawing.Point(128, 120)
        Me.unitsComboBox.Name = "unitsComboBox"
        Me.unitsComboBox.Size = New System.Drawing.Size(136, 21)
        Me.unitsComboBox.TabIndex = 7
        Me.unitsComboBox.Text = "Meters"
        '
        'unitsLabel
        '
        Me.unitsLabel.Location = New System.Drawing.Point(16, 122)
        Me.unitsLabel.Name = "unitsLabel"
        Me.unitsLabel.Size = New System.Drawing.Size(40, 16)
        Me.unitsLabel.TabIndex = 6
        Me.unitsLabel.Text = "Units:"
        '
        'maximumLabel
        '
        Me.maximumLabel.Location = New System.Drawing.Point(16, 90)
        Me.maximumLabel.Name = "maximumLabel"
        Me.maximumLabel.Size = New System.Drawing.Size(112, 16)
        Me.maximumLabel.TabIndex = 4
        Me.maximumLabel.Text = "Maximum Value:"
        '
        'minimumLabel
        '
        Me.minimumLabel.Location = New System.Drawing.Point(16, 57)
        Me.minimumLabel.Name = "minimumLabel"
        Me.minimumLabel.Size = New System.Drawing.Size(104, 18)
        Me.minimumLabel.TabIndex = 2
        Me.minimumLabel.Text = "Minimum Value:"
        '
        'physicalChannelLabel
        '
        Me.physicalChannelLabel.Location = New System.Drawing.Point(16, 26)
        Me.physicalChannelLabel.Name = "physicalChannelLabel"
        Me.physicalChannelLabel.Size = New System.Drawing.Size(96, 16)
        Me.physicalChannelLabel.TabIndex = 0
        Me.physicalChannelLabel.Text = "Physical Channel:"
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.Label1)
        Me.GroupBox1.Controls.Add(Me.DataGrid1)
        Me.GroupBox1.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.GroupBox1.Location = New System.Drawing.Point(288, 56)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(232, 264)
        Me.GroupBox1.TabIndex = 6
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "Acquisition Results"
        '
        'Label1
        '
        Me.Label1.Location = New System.Drawing.Point(16, 16)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(112, 16)
        Me.Label1.TabIndex = 0
        Me.Label1.Text = "Acquisition Data:"
        '
        'DataGrid1
        '
        Me.DataGrid1.AllowSorting = False
        Me.DataGrid1.DataMember = ""
        Me.DataGrid1.HeaderForeColor = System.Drawing.SystemColors.ControlText
        Me.DataGrid1.Location = New System.Drawing.Point(16, 32)
        Me.DataGrid1.Name = "DataGrid1"
        Me.DataGrid1.ParentRowsVisible = False
        Me.DataGrid1.ReadOnly = True
        Me.DataGrid1.Size = New System.Drawing.Size(208, 224)
        Me.DataGrid1.TabIndex = 1
        Me.DataGrid1.TabStop = False
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.CheckBox1)
        Me.GroupBox2.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.GroupBox2.Location = New System.Drawing.Point(288, 8)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(232, 40)
        Me.GroupBox2.TabIndex = 5
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "Device Parameters"
        '
        'CheckBox1
        '
        Me.CheckBox1.Location = New System.Drawing.Point(16, 16)
        Me.CheckBox1.Name = "CheckBox1"
        Me.CheckBox1.Size = New System.Drawing.Size(160, 16)
        Me.CheckBox1.TabIndex = 0
        Me.CheckBox1.Text = "Synchronization Enabled?"
        '
        'GroupBox3
        '
        Me.GroupBox3.Controls.Add(Me.NumericUpDown1)
        Me.GroupBox3.Controls.Add(Me.NumericUpDown2)
        Me.GroupBox3.Controls.Add(Me.NumericUpDown3)
        Me.GroupBox3.Controls.Add(Me.Label2)
        Me.GroupBox3.Controls.Add(Me.ComboBox1)
        Me.GroupBox3.Controls.Add(Me.Label3)
        Me.GroupBox3.Controls.Add(Me.ComboBox2)
        Me.GroupBox3.Controls.Add(Me.ComboBox3)
        Me.GroupBox3.Controls.Add(Me.Label4)
        Me.GroupBox3.Controls.Add(Me.Label5)
        Me.GroupBox3.Controls.Add(Me.Label6)
        Me.GroupBox3.Controls.Add(Me.Label7)
        Me.GroupBox3.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.GroupBox3.Location = New System.Drawing.Point(8, 264)
        Me.GroupBox3.Name = "GroupBox3"
        Me.GroupBox3.Size = New System.Drawing.Size(272, 216)
        Me.GroupBox3.TabIndex = 4
        Me.GroupBox3.TabStop = False
        Me.GroupBox3.Text = "LVDT Parameters"
        '
        'NumericUpDown1
        '
        Me.NumericUpDown1.Location = New System.Drawing.Point(128, 56)
        Me.NumericUpDown1.Maximum = New Decimal(New Integer() {1000000, 0, 0, 0})
        Me.NumericUpDown1.Name = "NumericUpDown1"
        Me.NumericUpDown1.Size = New System.Drawing.Size(136, 20)
        Me.NumericUpDown1.TabIndex = 3
        Me.NumericUpDown1.Value = New Decimal(New Integer() {1, 0, 0, 0})
        '
        'NumericUpDown2
        '
        Me.NumericUpDown2.Location = New System.Drawing.Point(128, 152)
        Me.NumericUpDown2.Maximum = New Decimal(New Integer() {1000000, 0, 0, 0})
        Me.NumericUpDown2.Name = "NumericUpDown2"
        Me.NumericUpDown2.Size = New System.Drawing.Size(136, 20)
        Me.NumericUpDown2.TabIndex = 9
        Me.NumericUpDown2.Value = New Decimal(New Integer() {50, 0, 0, 0})
        '
        'NumericUpDown3
        '
        Me.NumericUpDown3.Location = New System.Drawing.Point(128, 120)
        Me.NumericUpDown3.Maximum = New Decimal(New Integer() {100000, 0, 0, 0})
        Me.NumericUpDown3.Name = "NumericUpDown3"
        Me.NumericUpDown3.Size = New System.Drawing.Size(136, 20)
        Me.NumericUpDown3.TabIndex = 7
        Me.NumericUpDown3.Value = New Decimal(New Integer() {2500, 0, 0, 0})
        '
        'Label2
        '
        Me.Label2.Location = New System.Drawing.Point(16, 122)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(120, 16)
        Me.Label2.TabIndex = 6
        Me.Label2.Text = "Excitation Frequency:"
        '
        'ComboBox1
        '
        Me.ComboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.ComboBox1.Items.AddRange(New Object() {"Internal", "External", "None"})
        Me.ComboBox1.Location = New System.Drawing.Point(128, 88)
        Me.ComboBox1.Name = "ComboBox1"
        Me.ComboBox1.Size = New System.Drawing.Size(136, 21)
        Me.ComboBox1.TabIndex = 5
        '
        'Label3
        '
        Me.Label3.Location = New System.Drawing.Point(16, 90)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(104, 16)
        Me.Label3.TabIndex = 4
        Me.Label3.Text = "Excitation Source:"
        '
        'ComboBox2
        '
        Me.ComboBox2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.ComboBox2.Items.AddRange(New Object() {"4-Wire", "5-Wire"})
        Me.ComboBox2.Location = New System.Drawing.Point(128, 24)
        Me.ComboBox2.Name = "ComboBox2"
        Me.ComboBox2.Size = New System.Drawing.Size(136, 21)
        Me.ComboBox2.TabIndex = 1
        '
        'ComboBox3
        '
        Me.ComboBox3.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.ComboBox3.Items.AddRange(New Object() {"mVolts/Volt/mMeter", "mVolts/Volt/0.001 Inch"})
        Me.ComboBox3.Location = New System.Drawing.Point(128, 184)
        Me.ComboBox3.Name = "ComboBox3"
        Me.ComboBox3.Size = New System.Drawing.Size(136, 21)
        Me.ComboBox3.TabIndex = 11
        '
        'Label4
        '
        Me.Label4.Location = New System.Drawing.Point(16, 58)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(96, 16)
        Me.Label4.TabIndex = 2
        Me.Label4.Text = "Excitation Value:"
        '
        'Label5
        '
        Me.Label5.Location = New System.Drawing.Point(16, 26)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(128, 16)
        Me.Label5.TabIndex = 0
        Me.Label5.Text = "Excitation Wire Mode:"
        '
        'Label6
        '
        Me.Label6.Location = New System.Drawing.Point(16, 186)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(88, 16)
        Me.Label6.TabIndex = 10
        Me.Label6.Text = "Sensitivity Units:"
        '
        'Label7
        '
        Me.Label7.Location = New System.Drawing.Point(16, 154)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(72, 16)
        Me.Label7.TabIndex = 8
        Me.Label7.Text = "Sensitivity:"
        '
        'Button1
        '
        Me.Button1.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.Button1.Location = New System.Drawing.Point(360, 360)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(80, 24)
        Me.Button1.TabIndex = 0
        Me.Button1.Text = "Start"
        '
        'GroupBox4
        '
        Me.GroupBox4.Controls.Add(Me.ComboBox4)
        Me.GroupBox4.Controls.Add(Me.NumericUpDown4)
        Me.GroupBox4.Controls.Add(Me.NumericUpDown5)
        Me.GroupBox4.Controls.Add(Me.ComboBox5)
        Me.GroupBox4.Controls.Add(Me.Label8)
        Me.GroupBox4.Controls.Add(Me.Label9)
        Me.GroupBox4.Controls.Add(Me.Label10)
        Me.GroupBox4.Controls.Add(Me.Label11)
        Me.GroupBox4.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.GroupBox4.Location = New System.Drawing.Point(8, 8)
        Me.GroupBox4.Name = "GroupBox4"
        Me.GroupBox4.Size = New System.Drawing.Size(272, 152)
        Me.GroupBox4.TabIndex = 2
        Me.GroupBox4.TabStop = False
        Me.GroupBox4.Text = "Channel Parameters"
        '
        'ComboBox4
        '
        Me.ComboBox4.Location = New System.Drawing.Point(128, 24)
        Me.ComboBox4.Name = "ComboBox4"
        Me.ComboBox4.Size = New System.Drawing.Size(136, 21)
        Me.ComboBox4.TabIndex = 1
        Me.ComboBox4.Text = "SC1Mod1/ai0"
        '
        'NumericUpDown4
        '
        Me.NumericUpDown4.DecimalPlaces = 2
        Me.NumericUpDown4.Location = New System.Drawing.Point(128, 56)
        Me.NumericUpDown4.Maximum = New Decimal(New Integer() {10, 0, 0, 0})
        Me.NumericUpDown4.Minimum = New Decimal(New Integer() {10, 0, 0, -2147483648})
        Me.NumericUpDown4.Name = "NumericUpDown4"
        Me.NumericUpDown4.Size = New System.Drawing.Size(136, 20)
        Me.NumericUpDown4.TabIndex = 3
        Me.NumericUpDown4.Value = New Decimal(New Integer() {10, 0, 0, -2147352576})
        '
        'NumericUpDown5
        '
        Me.NumericUpDown5.DecimalPlaces = 2
        Me.NumericUpDown5.Location = New System.Drawing.Point(128, 88)
        Me.NumericUpDown5.Maximum = New Decimal(New Integer() {10, 0, 0, 0})
        Me.NumericUpDown5.Minimum = New Decimal(New Integer() {10, 0, 0, -2147483648})
        Me.NumericUpDown5.Name = "NumericUpDown5"
        Me.NumericUpDown5.Size = New System.Drawing.Size(136, 20)
        Me.NumericUpDown5.TabIndex = 5
        Me.NumericUpDown5.Value = New Decimal(New Integer() {10, 0, 0, 131072})
        '
        'ComboBox5
        '
        Me.ComboBox5.Items.AddRange(New Object() {"Meters", "Inches", "Custom Scale"})
        Me.ComboBox5.Location = New System.Drawing.Point(128, 120)
        Me.ComboBox5.Name = "ComboBox5"
        Me.ComboBox5.Size = New System.Drawing.Size(136, 21)
        Me.ComboBox5.TabIndex = 7
        Me.ComboBox5.Text = "Meters"
        '
        'Label8
        '
        Me.Label8.Location = New System.Drawing.Point(16, 122)
        Me.Label8.Name = "Label8"
        Me.Label8.Size = New System.Drawing.Size(40, 16)
        Me.Label8.TabIndex = 6
        Me.Label8.Text = "Units:"
        '
        'Label9
        '
        Me.Label9.Location = New System.Drawing.Point(16, 90)
        Me.Label9.Name = "Label9"
        Me.Label9.Size = New System.Drawing.Size(112, 16)
        Me.Label9.TabIndex = 4
        Me.Label9.Text = "Maximum Value:"
        '
        'Label10
        '
        Me.Label10.Location = New System.Drawing.Point(16, 57)
        Me.Label10.Name = "Label10"
        Me.Label10.Size = New System.Drawing.Size(104, 18)
        Me.Label10.TabIndex = 2
        Me.Label10.Text = "Minimum Value:"
        '
        'Label11
        '
        Me.Label11.Location = New System.Drawing.Point(16, 26)
        Me.Label11.Name = "Label11"
        Me.Label11.Size = New System.Drawing.Size(96, 16)
        Me.Label11.TabIndex = 0
        Me.Label11.Text = "Physical Channel:"
        '
        'GroupBox5
        '
        Me.GroupBox5.Controls.Add(Me.NumericUpDown6)
        Me.GroupBox5.Controls.Add(Me.NumericUpDown7)
        Me.GroupBox5.Controls.Add(Me.Label12)
        Me.GroupBox5.Controls.Add(Me.Label13)
        Me.GroupBox5.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.GroupBox5.Location = New System.Drawing.Point(8, 168)
        Me.GroupBox5.Name = "GroupBox5"
        Me.GroupBox5.Size = New System.Drawing.Size(272, 88)
        Me.GroupBox5.TabIndex = 3
        Me.GroupBox5.TabStop = False
        Me.GroupBox5.Text = "Timing Parameters"
        '
        'NumericUpDown6
        '
        Me.NumericUpDown6.Location = New System.Drawing.Point(128, 56)
        Me.NumericUpDown6.Maximum = New Decimal(New Integer() {1000000, 0, 0, 0})
        Me.NumericUpDown6.Name = "NumericUpDown6"
        Me.NumericUpDown6.Size = New System.Drawing.Size(136, 20)
        Me.NumericUpDown6.TabIndex = 3
        Me.NumericUpDown6.Value = New Decimal(New Integer() {1000, 0, 0, 0})
        '
        'NumericUpDown7
        '
        Me.NumericUpDown7.DecimalPlaces = 2
        Me.NumericUpDown7.Location = New System.Drawing.Point(128, 24)
        Me.NumericUpDown7.Maximum = New Decimal(New Integer() {1000000, 0, 0, 0})
        Me.NumericUpDown7.Name = "NumericUpDown7"
        Me.NumericUpDown7.Size = New System.Drawing.Size(136, 20)
        Me.NumericUpDown7.TabIndex = 1
        Me.NumericUpDown7.Value = New Decimal(New Integer() {10000, 0, 0, 0})
        '
        'Label12
        '
        Me.Label12.Location = New System.Drawing.Point(16, 58)
        Me.Label12.Name = "Label12"
        Me.Label12.Size = New System.Drawing.Size(120, 16)
        Me.Label12.TabIndex = 2
        Me.Label12.Text = "Samples / Channel:"
        '
        'Label13
        '
        Me.Label13.Location = New System.Drawing.Point(16, 26)
        Me.Label13.Name = "Label13"
        Me.Label13.Size = New System.Drawing.Size(56, 16)
        Me.Label13.TabIndex = 0
        Me.Label13.Text = "Rate (Hz):"
        '
        'MainFrom
        '
        Me.AutoScaleBaseSize = New System.Drawing.Size(5, 13)
        Me.ClientSize = New System.Drawing.Size(530, 488)
        Me.Controls.Add(Me.stopButton)
        Me.Controls.Add(Me.startButton)
        Me.Controls.Add(Me.timingParametersGroupBox)
        Me.Controls.Add(Me.acquisitionResultsGroupBox)
        Me.Controls.Add(Me.deviceParametersGroupBox)
        Me.Controls.Add(Me.lvdtParametersGroupBox)
        Me.Controls.Add(Me.channelParametersGroupBox)
        Me.Controls.Add(Me.GroupBox1)
        Me.Controls.Add(Me.GroupBox2)
        Me.Controls.Add(Me.GroupBox3)
        Me.Controls.Add(Me.Button1)
        Me.Controls.Add(Me.GroupBox4)
        Me.Controls.Add(Me.GroupBox5)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MaximizeBox = False
        Me.Name = "MainFrom"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Continuous Acquisition LVDT Samples - Internal Clock - SCXI1540"
        Me.timingParametersGroupBox.ResumeLayout(False)
        CType(Me.samplesPerChannelNumeric, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.rateNumeric, System.ComponentModel.ISupportInitialize).EndInit()
        Me.acquisitionResultsGroupBox.ResumeLayout(False)
        CType(Me.acquisitionDataGrid, System.ComponentModel.ISupportInitialize).EndInit()
        Me.deviceParametersGroupBox.ResumeLayout(False)
        Me.lvdtParametersGroupBox.ResumeLayout(False)
        CType(Me.excitationValueNumeric, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.sensitivityNumeric, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.excitationFrequencyNumeric, System.ComponentModel.ISupportInitialize).EndInit()
        Me.channelParametersGroupBox.ResumeLayout(False)
        CType(Me.minimumValueNumeric, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.maximumValueNumeric, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox1.ResumeLayout(False)
        CType(Me.DataGrid1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox3.ResumeLayout(False)
        CType(Me.NumericUpDown1, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.NumericUpDown2, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.NumericUpDown3, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox4.ResumeLayout(False)
        CType(Me.NumericUpDown4, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.NumericUpDown5, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox5.ResumeLayout(False)
        CType(Me.NumericUpDown6, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.NumericUpDown7, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub

#End Region

    Private Sub startButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles startButton.Click
        If runningTask Is Nothing Then
            Try
                stopButton.Enabled = True
                startButton.Enabled = False

                ' Get Sensitivity Units
                Select Case sensitivityUnitsComboBox.SelectedItem.ToString()
                    Case "mVolts/Volt/mMeter"
                        sensitivityUnits = AILvdtSensitivityUnits.MillivoltsPerVoltPerMillimeter
                    Case Else
                        sensitivityUnits = AILvdtSensitivityUnits.MillivoltsPerVoltPerMilliinch
                End Select

                ' Get Wire Mode
                Select Case excitationWireModeComboBox.SelectedItem.ToString()
                    Case "4-Wire"
                        wireMode = AIACExcitationWireMode.FourWire
                    Case Else
                        wireMode = AIACExcitationWireMode.FiveWire
                End Select

                ' Get Ex Source
                Select Case excitationSourceComboBox.SelectedItem.ToString()
                    Case "Internal"
                        excitationSource = AIExcitationSource.Internal
                    Case "External"
                        excitationSource = AIExcitationSource.External
                    Case Else
                        excitationSource = AIExcitationSource.None
                End Select

                ' Create a new task
                myTask = New Task()

                ' Create a virtual channel
                Select Case unitsComboBox.SelectedIndex
                    Case 0
                        myTask.AIChannels.CreateLvdtChannel(physicalChannelComboBox.Text, "", _
                            Convert.ToDouble(minimumValueNumeric.Value), Convert.ToDouble(maximumValueNumeric.Value), _
                            Convert.ToDouble(sensitivityNumeric.Value), sensitivityUnits, _
                            excitationSource, Convert.ToDouble(excitationValueNumeric.Value), _
                            Convert.ToDouble(excitationFrequencyNumeric.Value), wireMode, AILvdtUnits.Meters)
                    Case 1
                        myTask.AIChannels.CreateLvdtChannel(physicalChannelComboBox.Text, "", _
                            Convert.ToDouble(minimumValueNumeric.Value), Convert.ToDouble(maximumValueNumeric.Value), _
                            Convert.ToDouble(sensitivityNumeric.Value), sensitivityUnits, _
                            excitationSource, Convert.ToDouble(excitationValueNumeric.Value), _
                            Convert.ToDouble(excitationFrequencyNumeric.Value), wireMode, AILvdtUnits.Inches)
                    Case Else ' Use Custom Scale Units
                        myTask.AIChannels.CreateLvdtChannel(physicalChannelComboBox.Text, "", _
                            Convert.ToDouble(minimumValueNumeric.Value), Convert.ToDouble(maximumValueNumeric.Value), _
                            Convert.ToDouble(sensitivityNumeric.Value), sensitivityUnits, _
                            excitationSource, Convert.ToDouble(excitationValueNumeric.Value), _
                            Convert.ToDouble(excitationFrequencyNumeric.Value), wireMode, _
                            unitsComboBox.Text) ' string entered in combo box that is the custom scale name
                End Select

                ' AI.ACExcit.SyncEnable
                If synchronizationEnabledCheckBox.Checked Then
                    myTask.AIChannels.All.ACExcitationSyncEnable = True
                Else
                    myTask.AIChannels.All.ACExcitationSyncEnable = False
                End If

                ' Configure the timing parameters
                myTask.Timing.ConfigureSampleClock("", Convert.ToDouble(rateNumeric.Value), _
                    SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, 1000)

                ' Verify the Task
                myTask.Control(TaskAction.Verify)

                ' Prepare the table for Data
                InitializeDataTable(myTask.AIChannels, dataTable)
                acquisitionDataGrid.DataSource = dataTable

                runningTask = myTask
                analogInReader = New AnalogMultiChannelReader(myTask.Stream)
                analogCallback = New AsyncCallback(AddressOf AnalogInCallback)

                ' Use SynchronizeCallbacks to specify that the object 
                ' marshals callbacks across threads appropriately.
                analogInReader.SynchronizeCallbacks = True
                analogInReader.BeginReadWaveform(Convert.ToInt32(samplesPerChannelNumeric.Value), _
                    analogCallback, myTask)
            Catch exception As DaqException
                ' Display Errors
                MessageBox.Show(exception.Message)
                runningTask = Nothing
                myTask.Dispose()
                stopButton.Enabled = False
                startButton.Enabled = True
            End Try
        End If
    End Sub

    Private Sub AnalogInCallback(ByVal ar As IAsyncResult)
        Try
            If (Not (runningTask Is Nothing)) AndAlso runningTask Is ar.AsyncState Then
                ' Read the available data from the channels
                data = analogInReader.EndReadWaveform(ar)

                ' Plot your data here
                dataToDataTable(data, dataTable)

                ' Begin next read
                analogInReader.BeginMemoryOptimizedReadWaveform(Convert.ToInt32(samplesPerChannelNumeric.Value), analogCallback, myTask, data)

            End If
        Catch exception As DaqException
            ' Display Errors
            MessageBox.Show(exception.Message)
            runningTask = Nothing
            myTask.Dispose()
            stopButton.Enabled = False
            startButton.Enabled = True
        End Try
    End Sub 'AnalogInCallback

    Private Sub stopButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles stopButton.Click
        ' Dispose of the task
        runningTask = Nothing
        myTask.Dispose()
        stopButton.Enabled = False
        startButton.Enabled = True
    End Sub

    Private Sub dataToDataTable(ByVal sourceArray As AnalogWaveform(Of Double)(), ByRef dataTable As DataTable)
        ' Iterate over channels
        Dim currentLineIndex As Integer = 0
        For Each waveform As AnalogWaveform(Of Double) In sourceArray
            Dim dataCount As Integer = 0
            If waveform.Samples.Count < 10 Then
                dataCount = waveform.Samples.Count
            Else
                dataCount = 10
            End If
            For sample As Integer = 0 To (dataCount - 1)
                dataTable.Rows(sample)(currentLineIndex) = waveform.Samples(sample).Value
            Next
            currentLineIndex += 1
        Next
    End Sub

    Public Sub InitializeDataTable(ByVal channelCollection As AIChannelCollection, ByRef data As DataTable)
        Dim numOfChannels As Integer = channelCollection.Count
        data.Rows.Clear()
        data.Columns.Clear()
        dataColumn = New DataColumn(numOfChannels) {}
        Dim numOfRows As Integer = 10

        Dim currentChannelIndex As Integer
        For currentChannelIndex = 0 To numOfChannels - 1
            dataColumn(currentChannelIndex) = New DataColumn
            dataColumn(currentChannelIndex).DataType = GetType(Double)
            dataColumn(currentChannelIndex).ColumnName = channelCollection(currentChannelIndex).PhysicalName
        Next currentChannelIndex

        data.Columns.AddRange(dataColumn)

        Dim currentDataIndex As Integer
        For currentDataIndex = 0 To numOfRows - 1
            Dim rowArr(numOfChannels - 1) As Object
            data.Rows.Add(rowArr)
        Next currentDataIndex

        Return
    End Sub 'InitializeDataTable
End Class
