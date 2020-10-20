'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
'
' Example program:
'   ContAcqRVDTSamples_IntClk_SCXI1540
'
' Category:
'   AI
'
' Description:
'   This example demonstrates how to make a continuous, hardware-timed
'   acceleration measurement using an SCXI-1540 module.
'
' Instructions for running:
'   1.  Specify the physical channel where you have connected the RVDT.
'   2.  Enter the minimum and maximum distance values, in units (based on the
'       units control) that you expect to measure. A smaller range will allow a
'       more accurate measurement.
'   3.  Select the number of samples to acquire.
'   4.  Set the rate of the acquisition.
'   5.  Specify the RVDT settings.
'   6.  If you are using multiple RVDTs and would like to synchronize their
'       excitations, then enable synchronization for all the secondary RVDT
'       channels via the Synchronization Enabled button. You must also connect
'       the excitation output (EX+) of your primary RVDT channel to all the
'       secondary RVDT channels' sync pins (SYNC).
'
' Steps:
'   1.  Create a new analog input task.
'   2.  Create an analog input RVDT channel.
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
'   Connect your RVDT to the terminals corresponding to the physical channel I/O
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
    Private sensitivityUnits As AIRvdtSensitivityUnits
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

        ' This call is required by the Windows Form Designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call
        dataTable = New DataTable
        unitsComboBox.SelectedIndex = 0
        sensitivityUnitsComboBox.SelectedIndex = 0
        excitationWireModeComboBox.SelectedIndex = 0
        excitationSourceComboBox.SelectedIndex = 0
        stopButton.Enabled = False

        physicalChannelComboBox.Items.AddRange(DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AI, PhysicalChannelAccess.External))
        If (physicalChannelComboBox.Items.Count > 0) Then
            physicalChannelComboBox.SelectedIndex = 0
        End If

    End Sub

    ' Form overrides dispose to clean up the component list.
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

    ' Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    ' NOTE: The following procedure is required by the Windows Form Designer
    ' It can be modified using the Windows Form Designer.  
    ' Do not modify it using the code editor.
    Friend WithEvents deviceParametersGroupBox As System.Windows.Forms.GroupBox
    Friend WithEvents synchronizationEnabledCheckBox As System.Windows.Forms.CheckBox
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
    Friend WithEvents channelParametersGroupBox As System.Windows.Forms.GroupBox
    Friend WithEvents unitsComboBox As System.Windows.Forms.ComboBox
    Friend WithEvents unitsLabel As System.Windows.Forms.Label
    Friend WithEvents maximumLabel As System.Windows.Forms.Label
    Friend WithEvents minimumLabel As System.Windows.Forms.Label
    Friend WithEvents physicalChannelLabel As System.Windows.Forms.Label
    Friend WithEvents rvdtParametersGroupBox As System.Windows.Forms.GroupBox
    Friend WithEvents minimumValueNumeric As System.Windows.Forms.NumericUpDown
    Friend WithEvents maximumValueNumeric As System.Windows.Forms.NumericUpDown
    Friend WithEvents physicalChannelComboBox As System.Windows.Forms.ComboBox
    <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
        Dim resources As System.Resources.ResourceManager = New System.Resources.ResourceManager(GetType(MainFrom))
        Me.deviceParametersGroupBox = New System.Windows.Forms.GroupBox
        Me.synchronizationEnabledCheckBox = New System.Windows.Forms.CheckBox
        Me.rvdtParametersGroupBox = New System.Windows.Forms.GroupBox
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
        Me.channelParametersGroupBox = New System.Windows.Forms.GroupBox
        Me.physicalChannelComboBox = New System.Windows.Forms.ComboBox
        Me.maximumValueNumeric = New System.Windows.Forms.NumericUpDown
        Me.minimumValueNumeric = New System.Windows.Forms.NumericUpDown
        Me.unitsComboBox = New System.Windows.Forms.ComboBox
        Me.unitsLabel = New System.Windows.Forms.Label
        Me.maximumLabel = New System.Windows.Forms.Label
        Me.minimumLabel = New System.Windows.Forms.Label
        Me.physicalChannelLabel = New System.Windows.Forms.Label
        Me.deviceParametersGroupBox.SuspendLayout()
        Me.rvdtParametersGroupBox.SuspendLayout()
        CType(Me.excitationValueNumeric, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.sensitivityNumeric, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.excitationFrequencyNumeric, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.timingParametersGroupBox.SuspendLayout()
        CType(Me.samplesPerChannelNumeric, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.rateNumeric, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.acquisitionResultsGroupBox.SuspendLayout()
        CType(Me.acquisitionDataGrid, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.channelParametersGroupBox.SuspendLayout()
        CType(Me.maximumValueNumeric, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.minimumValueNumeric, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'deviceParametersGroupBox
        '
        Me.deviceParametersGroupBox.Controls.Add(Me.synchronizationEnabledCheckBox)
        Me.deviceParametersGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.deviceParametersGroupBox.Location = New System.Drawing.Point(280, 8)
        Me.deviceParametersGroupBox.Name = "deviceParametersGroupBox"
        Me.deviceParametersGroupBox.Size = New System.Drawing.Size(232, 40)
        Me.deviceParametersGroupBox.TabIndex = 5
        Me.deviceParametersGroupBox.TabStop = False
        Me.deviceParametersGroupBox.Text = "Device Parameters"
        '
        'synchronizationEnabledCheckBox
        '
        Me.synchronizationEnabledCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.synchronizationEnabledCheckBox.Location = New System.Drawing.Point(16, 16)
        Me.synchronizationEnabledCheckBox.Name = "synchronizationEnabledCheckBox"
        Me.synchronizationEnabledCheckBox.Size = New System.Drawing.Size(160, 16)
        Me.synchronizationEnabledCheckBox.TabIndex = 0
        Me.synchronizationEnabledCheckBox.Text = "Synchronization Enabled?"
        '
        'rvdtParametersGroupBox
        '
        Me.rvdtParametersGroupBox.Controls.Add(Me.excitationValueNumeric)
        Me.rvdtParametersGroupBox.Controls.Add(Me.sensitivityNumeric)
        Me.rvdtParametersGroupBox.Controls.Add(Me.excitationFrequencyNumeric)
        Me.rvdtParametersGroupBox.Controls.Add(Me.excitationFrequencyLabel)
        Me.rvdtParametersGroupBox.Controls.Add(Me.excitationSourceComboBox)
        Me.rvdtParametersGroupBox.Controls.Add(Me.excitationSourceLabel)
        Me.rvdtParametersGroupBox.Controls.Add(Me.excitationWireModeComboBox)
        Me.rvdtParametersGroupBox.Controls.Add(Me.sensitivityUnitsComboBox)
        Me.rvdtParametersGroupBox.Controls.Add(Me.excitationValueLabel)
        Me.rvdtParametersGroupBox.Controls.Add(Me.excitationWireModeLabel)
        Me.rvdtParametersGroupBox.Controls.Add(Me.sensitivityUnitsLabel)
        Me.rvdtParametersGroupBox.Controls.Add(Me.sensitivityLabel)
        Me.rvdtParametersGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.rvdtParametersGroupBox.Location = New System.Drawing.Point(8, 264)
        Me.rvdtParametersGroupBox.Name = "rvdtParametersGroupBox"
        Me.rvdtParametersGroupBox.Size = New System.Drawing.Size(264, 216)
        Me.rvdtParametersGroupBox.TabIndex = 4
        Me.rvdtParametersGroupBox.TabStop = False
        Me.rvdtParametersGroupBox.Text = "RVDT Parameters"
        '
        'excitationValueNumeric
        '
        Me.excitationValueNumeric.Location = New System.Drawing.Point(128, 56)
        Me.excitationValueNumeric.Maximum = New Decimal(New Integer() {1000000, 0, 0, 0})
        Me.excitationValueNumeric.Name = "excitationValueNumeric"
        Me.excitationValueNumeric.Size = New System.Drawing.Size(128, 20)
        Me.excitationValueNumeric.TabIndex = 3
        Me.excitationValueNumeric.Value = New Decimal(New Integer() {1, 0, 0, 0})
        '
        'sensitivityNumeric
        '
        Me.sensitivityNumeric.Location = New System.Drawing.Point(128, 152)
        Me.sensitivityNumeric.Maximum = New Decimal(New Integer() {1000000, 0, 0, 0})
        Me.sensitivityNumeric.Name = "sensitivityNumeric"
        Me.sensitivityNumeric.Size = New System.Drawing.Size(128, 20)
        Me.sensitivityNumeric.TabIndex = 9
        Me.sensitivityNumeric.Value = New Decimal(New Integer() {50, 0, 0, 0})
        '
        'excitationFrequencyNumeric
        '
        Me.excitationFrequencyNumeric.Location = New System.Drawing.Point(128, 120)
        Me.excitationFrequencyNumeric.Maximum = New Decimal(New Integer() {100000, 0, 0, 0})
        Me.excitationFrequencyNumeric.Name = "excitationFrequencyNumeric"
        Me.excitationFrequencyNumeric.Size = New System.Drawing.Size(128, 20)
        Me.excitationFrequencyNumeric.TabIndex = 7
        Me.excitationFrequencyNumeric.Value = New Decimal(New Integer() {2500, 0, 0, 0})
        '
        'excitationFrequencyLabel
        '
        Me.excitationFrequencyLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
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
        Me.excitationSourceComboBox.Size = New System.Drawing.Size(128, 21)
        Me.excitationSourceComboBox.TabIndex = 5
        '
        'excitationSourceLabel
        '
        Me.excitationSourceLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
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
        Me.excitationWireModeComboBox.Size = New System.Drawing.Size(128, 21)
        Me.excitationWireModeComboBox.TabIndex = 1
        '
        'sensitivityUnitsComboBox
        '
        Me.sensitivityUnitsComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.sensitivityUnitsComboBox.Items.AddRange(New Object() {"mVolts/Volt/Degree", "mVolts/Volts/Radian"})
        Me.sensitivityUnitsComboBox.Location = New System.Drawing.Point(128, 184)
        Me.sensitivityUnitsComboBox.Name = "sensitivityUnitsComboBox"
        Me.sensitivityUnitsComboBox.Size = New System.Drawing.Size(128, 21)
        Me.sensitivityUnitsComboBox.TabIndex = 11
        '
        'excitationValueLabel
        '
        Me.excitationValueLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.excitationValueLabel.Location = New System.Drawing.Point(16, 58)
        Me.excitationValueLabel.Name = "excitationValueLabel"
        Me.excitationValueLabel.Size = New System.Drawing.Size(96, 16)
        Me.excitationValueLabel.TabIndex = 2
        Me.excitationValueLabel.Text = "Excitation Value:"
        '
        'excitationWireModeLabel
        '
        Me.excitationWireModeLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.excitationWireModeLabel.Location = New System.Drawing.Point(16, 26)
        Me.excitationWireModeLabel.Name = "excitationWireModeLabel"
        Me.excitationWireModeLabel.Size = New System.Drawing.Size(128, 16)
        Me.excitationWireModeLabel.TabIndex = 0
        Me.excitationWireModeLabel.Text = "Excitation Wire Mode:"
        '
        'sensitivityUnitsLabel
        '
        Me.sensitivityUnitsLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.sensitivityUnitsLabel.Location = New System.Drawing.Point(16, 186)
        Me.sensitivityUnitsLabel.Name = "sensitivityUnitsLabel"
        Me.sensitivityUnitsLabel.Size = New System.Drawing.Size(88, 16)
        Me.sensitivityUnitsLabel.TabIndex = 10
        Me.sensitivityUnitsLabel.Text = "Sensitivity Units:"
        '
        'sensitivityLabel
        '
        Me.sensitivityLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.sensitivityLabel.Location = New System.Drawing.Point(16, 154)
        Me.sensitivityLabel.Name = "sensitivityLabel"
        Me.sensitivityLabel.Size = New System.Drawing.Size(72, 16)
        Me.sensitivityLabel.TabIndex = 8
        Me.sensitivityLabel.Text = "Sensitivity:"
        '
        'stopButton
        '
        Me.stopButton.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.stopButton.Location = New System.Drawing.Point(352, 392)
        Me.stopButton.Name = "stopButton"
        Me.stopButton.Size = New System.Drawing.Size(80, 24)
        Me.stopButton.TabIndex = 1
        Me.stopButton.Text = "Stop"
        '
        'startButton
        '
        Me.startButton.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.startButton.Location = New System.Drawing.Point(352, 360)
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
        Me.timingParametersGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.timingParametersGroupBox.Location = New System.Drawing.Point(8, 168)
        Me.timingParametersGroupBox.Name = "timingParametersGroupBox"
        Me.timingParametersGroupBox.Size = New System.Drawing.Size(264, 88)
        Me.timingParametersGroupBox.TabIndex = 3
        Me.timingParametersGroupBox.TabStop = False
        Me.timingParametersGroupBox.Text = "Timing Parameters"
        '
        'samplesPerChannelNumeric
        '
        Me.samplesPerChannelNumeric.Location = New System.Drawing.Point(128, 56)
        Me.samplesPerChannelNumeric.Maximum = New Decimal(New Integer() {1000000, 0, 0, 0})
        Me.samplesPerChannelNumeric.Name = "samplesPerChannelNumeric"
        Me.samplesPerChannelNumeric.Size = New System.Drawing.Size(128, 20)
        Me.samplesPerChannelNumeric.TabIndex = 3
        Me.samplesPerChannelNumeric.Value = New Decimal(New Integer() {1000, 0, 0, 0})
        '
        'rateNumeric
        '
        Me.rateNumeric.DecimalPlaces = 2
        Me.rateNumeric.Location = New System.Drawing.Point(128, 24)
        Me.rateNumeric.Maximum = New Decimal(New Integer() {1000000, 0, 0, 0})
        Me.rateNumeric.Name = "rateNumeric"
        Me.rateNumeric.Size = New System.Drawing.Size(128, 20)
        Me.rateNumeric.TabIndex = 1
        Me.rateNumeric.Value = New Decimal(New Integer() {10000, 0, 0, 0})
        '
        'samplesLabel
        '
        Me.samplesLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.samplesLabel.Location = New System.Drawing.Point(16, 58)
        Me.samplesLabel.Name = "samplesLabel"
        Me.samplesLabel.Size = New System.Drawing.Size(120, 16)
        Me.samplesLabel.TabIndex = 2
        Me.samplesLabel.Text = "Samples / Channel:"
        '
        'rateLabel
        '
        Me.rateLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
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
        Me.acquisitionResultsGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.acquisitionResultsGroupBox.Location = New System.Drawing.Point(280, 56)
        Me.acquisitionResultsGroupBox.Name = "acquisitionResultsGroupBox"
        Me.acquisitionResultsGroupBox.Size = New System.Drawing.Size(232, 264)
        Me.acquisitionResultsGroupBox.TabIndex = 6
        Me.acquisitionResultsGroupBox.TabStop = False
        Me.acquisitionResultsGroupBox.Text = "Acquisition Results"
        '
        'resultLabel
        '
        Me.resultLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
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
        'channelParametersGroupBox
        '
        Me.channelParametersGroupBox.Controls.Add(Me.physicalChannelComboBox)
        Me.channelParametersGroupBox.Controls.Add(Me.maximumValueNumeric)
        Me.channelParametersGroupBox.Controls.Add(Me.minimumValueNumeric)
        Me.channelParametersGroupBox.Controls.Add(Me.unitsComboBox)
        Me.channelParametersGroupBox.Controls.Add(Me.unitsLabel)
        Me.channelParametersGroupBox.Controls.Add(Me.maximumLabel)
        Me.channelParametersGroupBox.Controls.Add(Me.minimumLabel)
        Me.channelParametersGroupBox.Controls.Add(Me.physicalChannelLabel)
        Me.channelParametersGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.channelParametersGroupBox.Location = New System.Drawing.Point(8, 8)
        Me.channelParametersGroupBox.Name = "channelParametersGroupBox"
        Me.channelParametersGroupBox.Size = New System.Drawing.Size(264, 152)
        Me.channelParametersGroupBox.TabIndex = 2
        Me.channelParametersGroupBox.TabStop = False
        Me.channelParametersGroupBox.Text = "Channel Parameters"
        '
        'physicalChannelComboBox
        '
        Me.physicalChannelComboBox.Location = New System.Drawing.Point(128, 24)
        Me.physicalChannelComboBox.Name = "physicalChannelComboBox"
        Me.physicalChannelComboBox.Size = New System.Drawing.Size(128, 21)
        Me.physicalChannelComboBox.TabIndex = 1
        Me.physicalChannelComboBox.Text = "SC1Mod1/ai0"
        '
        'maximumValueNumeric
        '
        Me.maximumValueNumeric.DecimalPlaces = 2
        Me.maximumValueNumeric.Location = New System.Drawing.Point(128, 88)
        Me.maximumValueNumeric.Maximum = New Decimal(New Integer() {500, 0, 0, 0})
        Me.maximumValueNumeric.Minimum = New Decimal(New Integer() {500, 0, 0, -2147483648})
        Me.maximumValueNumeric.Name = "maximumValueNumeric"
        Me.maximumValueNumeric.Size = New System.Drawing.Size(128, 20)
        Me.maximumValueNumeric.TabIndex = 5
        Me.maximumValueNumeric.Value = New Decimal(New Integer() {70, 0, 0, 0})
        '
        'minimumValueNumeric
        '
        Me.minimumValueNumeric.DecimalPlaces = 2
        Me.minimumValueNumeric.Location = New System.Drawing.Point(128, 56)
        Me.minimumValueNumeric.Maximum = New Decimal(New Integer() {500, 0, 0, 0})
        Me.minimumValueNumeric.Minimum = New Decimal(New Integer() {500, 0, 0, -2147483648})
        Me.minimumValueNumeric.Name = "minimumValueNumeric"
        Me.minimumValueNumeric.Size = New System.Drawing.Size(128, 20)
        Me.minimumValueNumeric.TabIndex = 3
        Me.minimumValueNumeric.Value = New Decimal(New Integer() {70, 0, 0, -2147483648})
        '
        'unitsComboBox
        '
        Me.unitsComboBox.Items.AddRange(New Object() {"Degrees", "Radians", "Custom Scale"})
        Me.unitsComboBox.Location = New System.Drawing.Point(128, 120)
        Me.unitsComboBox.Name = "unitsComboBox"
        Me.unitsComboBox.Size = New System.Drawing.Size(128, 21)
        Me.unitsComboBox.TabIndex = 7
        Me.unitsComboBox.Text = "Degrees"
        '
        'unitsLabel
        '
        Me.unitsLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.unitsLabel.Location = New System.Drawing.Point(16, 122)
        Me.unitsLabel.Name = "unitsLabel"
        Me.unitsLabel.Size = New System.Drawing.Size(40, 16)
        Me.unitsLabel.TabIndex = 6
        Me.unitsLabel.Text = "Units:"
        '
        'maximumLabel
        '
        Me.maximumLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.maximumLabel.Location = New System.Drawing.Point(16, 90)
        Me.maximumLabel.Name = "maximumLabel"
        Me.maximumLabel.Size = New System.Drawing.Size(112, 16)
        Me.maximumLabel.TabIndex = 4
        Me.maximumLabel.Text = "Maximum Value:"
        '
        'minimumLabel
        '
        Me.minimumLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.minimumLabel.Location = New System.Drawing.Point(16, 57)
        Me.minimumLabel.Name = "minimumLabel"
        Me.minimumLabel.Size = New System.Drawing.Size(104, 18)
        Me.minimumLabel.TabIndex = 2
        Me.minimumLabel.Text = "Minimum Value:"
        '
        'physicalChannelLabel
        '
        Me.physicalChannelLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.physicalChannelLabel.Location = New System.Drawing.Point(16, 26)
        Me.physicalChannelLabel.Name = "physicalChannelLabel"
        Me.physicalChannelLabel.Size = New System.Drawing.Size(96, 16)
        Me.physicalChannelLabel.TabIndex = 0
        Me.physicalChannelLabel.Text = "Physical Channel:"
        '
        'MainFrom
        '
        Me.AutoScaleBaseSize = New System.Drawing.Size(5, 13)
        Me.ClientSize = New System.Drawing.Size(522, 488)
        Me.Controls.Add(Me.deviceParametersGroupBox)
        Me.Controls.Add(Me.rvdtParametersGroupBox)
        Me.Controls.Add(Me.stopButton)
        Me.Controls.Add(Me.startButton)
        Me.Controls.Add(Me.timingParametersGroupBox)
        Me.Controls.Add(Me.acquisitionResultsGroupBox)
        Me.Controls.Add(Me.channelParametersGroupBox)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MaximizeBox = False
        Me.Name = "MainFrom"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Continuous Acquisition RVDT Samples - Internal Clock - SCXI1540"
        Me.deviceParametersGroupBox.ResumeLayout(False)
        Me.rvdtParametersGroupBox.ResumeLayout(False)
        CType(Me.excitationValueNumeric, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.sensitivityNumeric, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.excitationFrequencyNumeric, System.ComponentModel.ISupportInitialize).EndInit()
        Me.timingParametersGroupBox.ResumeLayout(False)
        CType(Me.samplesPerChannelNumeric, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.rateNumeric, System.ComponentModel.ISupportInitialize).EndInit()
        Me.acquisitionResultsGroupBox.ResumeLayout(False)
        CType(Me.acquisitionDataGrid, System.ComponentModel.ISupportInitialize).EndInit()
        Me.channelParametersGroupBox.ResumeLayout(False)
        CType(Me.maximumValueNumeric, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.minimumValueNumeric, System.ComponentModel.ISupportInitialize).EndInit()
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
                    Case "mVolts/Volt/Degree"
                        sensitivityUnits = AIRvdtSensitivityUnits.MillivoltsPerVoltPerDegree
                    Case Else
                        sensitivityUnits = AIRvdtSensitivityUnits.MillivoltsPerVoltPerRadian
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

                ' Create RVDT Channel based on selected units
                Select Case unitsComboBox.SelectedIndex
                    Case 0 ' Degrees
                        myTask.AIChannels.CreateRvdtChannel(physicalChannelComboBox.Text, "", _
                            Convert.ToDouble(minimumValueNumeric.Value), Convert.ToDouble(maximumValueNumeric.Value), _
                            Convert.ToDouble(sensitivityNumeric.Value), sensitivityUnits, excitationSource, _
                            Convert.ToDouble(excitationValueNumeric.Value), Convert.ToDouble(excitationFrequencyNumeric.Value), _
                            wireMode, AIRvdtUnits.Degrees)
                    Case 1 ' Radians
                        myTask.AIChannels.CreateRvdtChannel(physicalChannelComboBox.Text, "", _
                            Convert.ToDouble(minimumValueNumeric.Value), Convert.ToDouble(maximumValueNumeric.Value), _
                            Convert.ToDouble(sensitivityNumeric.Value), sensitivityUnits, excitationSource, _
                            Convert.ToDouble(excitationValueNumeric.Value), Convert.ToDouble(excitationFrequencyNumeric.Value), _
                            wireMode, AIRvdtUnits.Radians)
                    Case Else ' Use Custom Scale Units
                        myTask.AIChannels.CreateRvdtChannel(physicalChannelComboBox.Text, "", _
                            Convert.ToDouble(minimumValueNumeric.Value), Convert.ToDouble(maximumValueNumeric.Value), _
                            Convert.ToDouble(sensitivityNumeric.Value), sensitivityUnits, excitationSource, _
                            Convert.ToDouble(excitationValueNumeric.Value), Convert.ToDouble(excitationFrequencyNumeric.Value), _
                            wireMode, unitsComboBox.Text) ' custom scale entered in combo box
                End Select

                ' Synchronization Enabled ?
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
                analogInReader.BeginReadWaveform(Convert.ToInt32(samplesPerChannelNumeric.Value), analogCallback, myTask)
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
                analogInReader.BeginMemoryOptimizedReadWaveform(Convert.ToInt32(samplesPerChannelNumeric.Value), _
                    analogCallback, myTask, data)

            End If
        Catch exception As DaqException
            ' Display Errors
            MessageBox.Show(exception.Message)
            runningTask = Nothing
            myTask.Dispose()
            stopButton.Enabled = False
            startButton.Enabled = True
        End Try
    End Sub

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
    End Sub ' InitializeDataTable
End class

    
