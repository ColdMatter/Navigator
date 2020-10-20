'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
'
' Example program:
'   ConAcqThmSmps_IntClk_SCXI1102And1581
'
' Category:
'   AI
'
' Description:
'   This example demonstrates how to acquire temperature data from a thermistor
'   using the DAQ device's internal clock.  This example uses the SCXI 1102
'   module in conjunction with the SCXI 1581 module.
'
' Instructions for running:
'   1.  Select the physical channel corresponding to where your signal is input
'       on the SCXI device.
'   2.  Enter the minimum and maximum temperature values in degrees C that you
'       expect to measure. A smaller range will allow a more accurate
'       measurement.
'   3.  Enter the acquisition rate.Note: The rate should be at least twice as
'       fast as the maximum frequency component of the signal being acquired.
'   4.  Enter the thermistor parameters (A, B, and C).
'   5.  Enter the resistance configuration, the current excitation source, and
'       the excitation value in Amps.  The current has been set to 100uA by
'       default for the SCXI-1581 which provides 100uA excitation.
'
' Steps:
'   1.  Create a task.  Create a AIChannel object using the
'       CreateThermistorIExChannel method.
'   2.  Set the timing parameters using the Timing.ConfigureSampleClock method.
'   3.  Call AnalogMultiChannelReader.BeginReadWaveform to install a callback
'       and begin the asynchronous read operation.
'   4.  Inside the callback, call AnalogMultiChannelReader.EndReadWaveform to
'       retrieve the data from the read operation.  
'   5.  Call AnalogMultiChannelReader.BeginMemoryOptimizedReadWaveform
'   6.  Dispose the Task object to clean-up any resources associated with the
'       task.
'   7.  Handle any DaqExceptions, if they occur.
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
'   Make sure your signal input terminals match the physical channel text box. 
'   For more information on the input and output terminals for your device, open
'   the NI-DAQmx Help, and refer to the NI-DAQmx Device Terminals and Device
'   Considerations books in the table of contents.
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

Imports NationalInstruments.DAQmx

Public Class mainform
    Inherits System.Windows.Forms.Form

    Private myTask As Task

    Private data As AnalogWaveform(Of Double)()
    Private runningTask As Task
    Private analogInReader As AnalogMultiChannelReader
    Private myAsyncCallback As AsyncCallback = New AsyncCallback(AddressOf AnalogInCallback)
    Private dataColumn() As dataColumn = Nothing
    Private dataTable As dataTable = New dataTable

#Region " Windows Form Designer generated code "

    Public Sub New()
        MyBase.New()
        Application.EnableVisualStyles()

        'This call is required by the Windows Form Designer.
        InitializeComponent()

        'Add any initialization after the InitializeComponent() call
        resistanceConfigComboBox.SelectedIndex = 2
        currentExcitationComboBox.SelectedIndex = 2

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
    Friend WithEvents rateNumeric As System.Windows.Forms.NumericUpDown
    Friend WithEvents rateLabel As System.Windows.Forms.Label
    Friend WithEvents acquisitionResultsGroupBox As System.Windows.Forms.GroupBox
    Friend WithEvents resultLabel As System.Windows.Forms.Label
    Friend WithEvents acquisitionDataGrid As System.Windows.Forms.DataGrid
    Friend WithEvents resistanceParametersGroupBox As System.Windows.Forms.GroupBox
    Friend WithEvents currentExcitationValueNumeric As System.Windows.Forms.NumericUpDown
    Friend WithEvents currentExcitationLabel As System.Windows.Forms.Label
    Friend WithEvents resistanceConfigLabel As System.Windows.Forms.Label
    Friend WithEvents currentExcitationComboBox As System.Windows.Forms.ComboBox
    Friend WithEvents resistanceConfigComboBox As System.Windows.Forms.ComboBox
    Friend WithEvents currentValueLabel As System.Windows.Forms.Label
    Friend WithEvents thermistorParametersGroupBox As System.Windows.Forms.GroupBox
    Friend WithEvents cLabel As System.Windows.Forms.Label
    Friend WithEvents bTextBox As System.Windows.Forms.TextBox
    Friend WithEvents bLabel As System.Windows.Forms.Label
    Friend WithEvents aLabel As System.Windows.Forms.Label
    Friend WithEvents aTextBox As System.Windows.Forms.TextBox
    Friend WithEvents cTextBox As System.Windows.Forms.TextBox
    Friend WithEvents channelParametersGroupBox As System.Windows.Forms.GroupBox
    Friend WithEvents maximumLabel As System.Windows.Forms.Label
    Friend WithEvents minimumLabel As System.Windows.Forms.Label
    Friend WithEvents physicalChannelLabel As System.Windows.Forms.Label
    Friend WithEvents minimumValueNumeric As System.Windows.Forms.NumericUpDown
    Friend WithEvents maximumValueNumeric As System.Windows.Forms.NumericUpDown
    Friend WithEvents physicalChannelComboBox As System.Windows.Forms.ComboBox
    <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
        Dim resources As System.Resources.ResourceManager = New System.Resources.ResourceManager(GetType(mainform))
        Me.stopButton = New System.Windows.Forms.Button
        Me.startButton = New System.Windows.Forms.Button
        Me.timingParametersGroupBox = New System.Windows.Forms.GroupBox
        Me.rateNumeric = New System.Windows.Forms.NumericUpDown
        Me.rateLabel = New System.Windows.Forms.Label
        Me.acquisitionResultsGroupBox = New System.Windows.Forms.GroupBox
        Me.resultLabel = New System.Windows.Forms.Label
        Me.acquisitionDataGrid = New System.Windows.Forms.DataGrid
        Me.resistanceParametersGroupBox = New System.Windows.Forms.GroupBox
        Me.currentExcitationValueNumeric = New System.Windows.Forms.NumericUpDown
        Me.currentExcitationLabel = New System.Windows.Forms.Label
        Me.resistanceConfigLabel = New System.Windows.Forms.Label
        Me.currentExcitationComboBox = New System.Windows.Forms.ComboBox
        Me.resistanceConfigComboBox = New System.Windows.Forms.ComboBox
        Me.currentValueLabel = New System.Windows.Forms.Label
        Me.thermistorParametersGroupBox = New System.Windows.Forms.GroupBox
        Me.cLabel = New System.Windows.Forms.Label
        Me.bTextBox = New System.Windows.Forms.TextBox
        Me.bLabel = New System.Windows.Forms.Label
        Me.aLabel = New System.Windows.Forms.Label
        Me.aTextBox = New System.Windows.Forms.TextBox
        Me.cTextBox = New System.Windows.Forms.TextBox
        Me.channelParametersGroupBox = New System.Windows.Forms.GroupBox
        Me.physicalChannelComboBox = New System.Windows.Forms.ComboBox
        Me.minimumValueNumeric = New System.Windows.Forms.NumericUpDown
        Me.maximumValueNumeric = New System.Windows.Forms.NumericUpDown
        Me.maximumLabel = New System.Windows.Forms.Label
        Me.minimumLabel = New System.Windows.Forms.Label
        Me.physicalChannelLabel = New System.Windows.Forms.Label
        Me.timingParametersGroupBox.SuspendLayout()
        CType(Me.rateNumeric, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.acquisitionResultsGroupBox.SuspendLayout()
        CType(Me.acquisitionDataGrid, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.resistanceParametersGroupBox.SuspendLayout()
        CType(Me.currentExcitationValueNumeric, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.thermistorParametersGroupBox.SuspendLayout()
        Me.channelParametersGroupBox.SuspendLayout()
        CType(Me.minimumValueNumeric, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.maximumValueNumeric, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'stopButton
        '
        Me.stopButton.Enabled = False
        Me.stopButton.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.stopButton.Location = New System.Drawing.Point(128, 384)
        Me.stopButton.Name = "stopButton"
        Me.stopButton.Size = New System.Drawing.Size(80, 24)
        Me.stopButton.TabIndex = 1
        Me.stopButton.Text = "Stop"
        '
        'startButton
        '
        Me.startButton.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.startButton.Location = New System.Drawing.Point(32, 384)
        Me.startButton.Name = "startButton"
        Me.startButton.Size = New System.Drawing.Size(80, 24)
        Me.startButton.TabIndex = 0
        Me.startButton.Text = "Start"
        '
        'timingParametersGroupBox
        '
        Me.timingParametersGroupBox.Controls.Add(Me.rateNumeric)
        Me.timingParametersGroupBox.Controls.Add(Me.rateLabel)
        Me.timingParametersGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.timingParametersGroupBox.Location = New System.Drawing.Point(8, 136)
        Me.timingParametersGroupBox.Name = "timingParametersGroupBox"
        Me.timingParametersGroupBox.Size = New System.Drawing.Size(224, 64)
        Me.timingParametersGroupBox.TabIndex = 3
        Me.timingParametersGroupBox.TabStop = False
        Me.timingParametersGroupBox.Text = "Timing Parameters"
        '
        'rateNumeric
        '
        Me.rateNumeric.DecimalPlaces = 2
        Me.rateNumeric.Location = New System.Drawing.Point(112, 24)
        Me.rateNumeric.Maximum = New Decimal(New Integer() {1000000, 0, 0, 0})
        Me.rateNumeric.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.rateNumeric.Name = "rateNumeric"
        Me.rateNumeric.Size = New System.Drawing.Size(96, 20)
        Me.rateNumeric.TabIndex = 1
        Me.rateNumeric.Value = New Decimal(New Integer() {1000, 0, 0, 0})
        '
        'rateLabel
        '
        Me.rateLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.rateLabel.Location = New System.Drawing.Point(16, 24)
        Me.rateLabel.Name = "rateLabel"
        Me.rateLabel.Size = New System.Drawing.Size(80, 16)
        Me.rateLabel.TabIndex = 0
        Me.rateLabel.Text = "Rate (Hz):"
        '
        'acquisitionResultsGroupBox
        '
        Me.acquisitionResultsGroupBox.Controls.Add(Me.resultLabel)
        Me.acquisitionResultsGroupBox.Controls.Add(Me.acquisitionDataGrid)
        Me.acquisitionResultsGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.acquisitionResultsGroupBox.Location = New System.Drawing.Point(248, 136)
        Me.acquisitionResultsGroupBox.Name = "acquisitionResultsGroupBox"
        Me.acquisitionResultsGroupBox.Size = New System.Drawing.Size(264, 272)
        Me.acquisitionResultsGroupBox.TabIndex = 6
        Me.acquisitionResultsGroupBox.TabStop = False
        Me.acquisitionResultsGroupBox.Text = "Acquisition Results:"
        '
        'resultLabel
        '
        Me.resultLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.resultLabel.Location = New System.Drawing.Point(16, 24)
        Me.resultLabel.Name = "resultLabel"
        Me.resultLabel.Size = New System.Drawing.Size(96, 16)
        Me.resultLabel.TabIndex = 0
        Me.resultLabel.Text = "Acquisition Data:"
        '
        'acquisitionDataGrid
        '
        Me.acquisitionDataGrid.AllowSorting = False
        Me.acquisitionDataGrid.DataMember = ""
        Me.acquisitionDataGrid.HeaderForeColor = System.Drawing.SystemColors.ControlText
        Me.acquisitionDataGrid.Location = New System.Drawing.Point(16, 40)
        Me.acquisitionDataGrid.Name = "acquisitionDataGrid"
        Me.acquisitionDataGrid.ParentRowsVisible = False
        Me.acquisitionDataGrid.ReadOnly = True
        Me.acquisitionDataGrid.Size = New System.Drawing.Size(232, 224)
        Me.acquisitionDataGrid.TabIndex = 1
        Me.acquisitionDataGrid.TabStop = False
        '
        'resistanceParametersGroupBox
        '
        Me.resistanceParametersGroupBox.Controls.Add(Me.currentExcitationValueNumeric)
        Me.resistanceParametersGroupBox.Controls.Add(Me.currentExcitationLabel)
        Me.resistanceParametersGroupBox.Controls.Add(Me.resistanceConfigLabel)
        Me.resistanceParametersGroupBox.Controls.Add(Me.currentExcitationComboBox)
        Me.resistanceParametersGroupBox.Controls.Add(Me.resistanceConfigComboBox)
        Me.resistanceParametersGroupBox.Controls.Add(Me.currentValueLabel)
        Me.resistanceParametersGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.resistanceParametersGroupBox.Location = New System.Drawing.Point(248, 8)
        Me.resistanceParametersGroupBox.Name = "resistanceParametersGroupBox"
        Me.resistanceParametersGroupBox.Size = New System.Drawing.Size(264, 120)
        Me.resistanceParametersGroupBox.TabIndex = 5
        Me.resistanceParametersGroupBox.TabStop = False
        Me.resistanceParametersGroupBox.Text = "Resistance Parameters"
        '
        'currentExcitationValueNumeric
        '
        Me.currentExcitationValueNumeric.DecimalPlaces = 5
        Me.currentExcitationValueNumeric.Location = New System.Drawing.Point(160, 88)
        Me.currentExcitationValueNumeric.Name = "currentExcitationValueNumeric"
        Me.currentExcitationValueNumeric.Size = New System.Drawing.Size(88, 20)
        Me.currentExcitationValueNumeric.TabIndex = 5
        Me.currentExcitationValueNumeric.Value = New Decimal(New Integer() {10, 0, 0, 327680})
        '
        'currentExcitationLabel
        '
        Me.currentExcitationLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.currentExcitationLabel.Location = New System.Drawing.Point(16, 59)
        Me.currentExcitationLabel.Name = "currentExcitationLabel"
        Me.currentExcitationLabel.Size = New System.Drawing.Size(136, 14)
        Me.currentExcitationLabel.TabIndex = 2
        Me.currentExcitationLabel.Text = "Current Excitation Source:"
        '
        'resistanceConfigLabel
        '
        Me.resistanceConfigLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.resistanceConfigLabel.Location = New System.Drawing.Point(16, 27)
        Me.resistanceConfigLabel.Name = "resistanceConfigLabel"
        Me.resistanceConfigLabel.Size = New System.Drawing.Size(136, 14)
        Me.resistanceConfigLabel.TabIndex = 0
        Me.resistanceConfigLabel.Text = "Resistance Configuration:"
        '
        'currentExcitationComboBox
        '
        Me.currentExcitationComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.currentExcitationComboBox.Items.AddRange(New Object() {"None", "Internal", "External"})
        Me.currentExcitationComboBox.Location = New System.Drawing.Point(160, 56)
        Me.currentExcitationComboBox.Name = "currentExcitationComboBox"
        Me.currentExcitationComboBox.Size = New System.Drawing.Size(88, 21)
        Me.currentExcitationComboBox.TabIndex = 3
        '
        'resistanceConfigComboBox
        '
        Me.resistanceConfigComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.resistanceConfigComboBox.Items.AddRange(New Object() {"2-Wire", "3-Wire", "4-Wire"})
        Me.resistanceConfigComboBox.Location = New System.Drawing.Point(160, 24)
        Me.resistanceConfigComboBox.Name = "resistanceConfigComboBox"
        Me.resistanceConfigComboBox.Size = New System.Drawing.Size(88, 21)
        Me.resistanceConfigComboBox.TabIndex = 1
        '
        'currentValueLabel
        '
        Me.currentValueLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.currentValueLabel.Location = New System.Drawing.Point(16, 91)
        Me.currentValueLabel.Name = "currentValueLabel"
        Me.currentValueLabel.Size = New System.Drawing.Size(152, 14)
        Me.currentValueLabel.TabIndex = 4
        Me.currentValueLabel.Text = "Current Excitation Value (A):"
        '
        'thermistorParametersGroupBox
        '
        Me.thermistorParametersGroupBox.Controls.Add(Me.cLabel)
        Me.thermistorParametersGroupBox.Controls.Add(Me.bTextBox)
        Me.thermistorParametersGroupBox.Controls.Add(Me.bLabel)
        Me.thermistorParametersGroupBox.Controls.Add(Me.aLabel)
        Me.thermistorParametersGroupBox.Controls.Add(Me.aTextBox)
        Me.thermistorParametersGroupBox.Controls.Add(Me.cTextBox)
        Me.thermistorParametersGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.thermistorParametersGroupBox.Location = New System.Drawing.Point(8, 216)
        Me.thermistorParametersGroupBox.Name = "thermistorParametersGroupBox"
        Me.thermistorParametersGroupBox.Size = New System.Drawing.Size(224, 120)
        Me.thermistorParametersGroupBox.TabIndex = 4
        Me.thermistorParametersGroupBox.TabStop = False
        Me.thermistorParametersGroupBox.Text = "Thermistor Characteristics"
        '
        'cLabel
        '
        Me.cLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.cLabel.Location = New System.Drawing.Point(16, 88)
        Me.cLabel.Name = "cLabel"
        Me.cLabel.Size = New System.Drawing.Size(72, 17)
        Me.cLabel.TabIndex = 4
        Me.cLabel.Text = "Parameter C:"
        '
        'bTextBox
        '
        Me.bTextBox.Location = New System.Drawing.Point(112, 56)
        Me.bTextBox.Name = "bTextBox"
        Me.bTextBox.Size = New System.Drawing.Size(96, 20)
        Me.bTextBox.TabIndex = 3
        Me.bTextBox.Text = "234.3159000E-6"
        '
        'bLabel
        '
        Me.bLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.bLabel.Location = New System.Drawing.Point(16, 56)
        Me.bLabel.Name = "bLabel"
        Me.bLabel.Size = New System.Drawing.Size(72, 17)
        Me.bLabel.TabIndex = 2
        Me.bLabel.Text = "Parameter B:"
        '
        'aLabel
        '
        Me.aLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.aLabel.Location = New System.Drawing.Point(16, 26)
        Me.aLabel.Name = "aLabel"
        Me.aLabel.Size = New System.Drawing.Size(72, 17)
        Me.aLabel.TabIndex = 0
        Me.aLabel.Text = "Parameter A:"
        '
        'aTextBox
        '
        Me.aTextBox.Location = New System.Drawing.Point(112, 24)
        Me.aTextBox.Name = "aTextBox"
        Me.aTextBox.Size = New System.Drawing.Size(96, 20)
        Me.aTextBox.TabIndex = 1
        Me.aTextBox.Text = "1.2953610E-3"
        '
        'cTextBox
        '
        Me.cTextBox.Location = New System.Drawing.Point(112, 88)
        Me.cTextBox.Name = "cTextBox"
        Me.cTextBox.Size = New System.Drawing.Size(96, 20)
        Me.cTextBox.TabIndex = 5
        Me.cTextBox.Text = "101.8703000E-9"
        '
        'channelParametersGroupBox
        '
        Me.channelParametersGroupBox.Controls.Add(Me.physicalChannelComboBox)
        Me.channelParametersGroupBox.Controls.Add(Me.minimumValueNumeric)
        Me.channelParametersGroupBox.Controls.Add(Me.maximumValueNumeric)
        Me.channelParametersGroupBox.Controls.Add(Me.maximumLabel)
        Me.channelParametersGroupBox.Controls.Add(Me.minimumLabel)
        Me.channelParametersGroupBox.Controls.Add(Me.physicalChannelLabel)
        Me.channelParametersGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.channelParametersGroupBox.Location = New System.Drawing.Point(8, 8)
        Me.channelParametersGroupBox.Name = "channelParametersGroupBox"
        Me.channelParametersGroupBox.Size = New System.Drawing.Size(224, 120)
        Me.channelParametersGroupBox.TabIndex = 2
        Me.channelParametersGroupBox.TabStop = False
        Me.channelParametersGroupBox.Text = "Channel Parameters"
        '
        'physicalChannelComboBox
        '
        Me.physicalChannelComboBox.Location = New System.Drawing.Point(112, 24)
        Me.physicalChannelComboBox.Name = "physicalChannelComboBox"
        Me.physicalChannelComboBox.Size = New System.Drawing.Size(96, 21)
        Me.physicalChannelComboBox.TabIndex = 1
        Me.physicalChannelComboBox.Text = "SC1Mod1/ai0"
        '
        'minimumValueNumeric
        '
        Me.minimumValueNumeric.DecimalPlaces = 2
        Me.minimumValueNumeric.Location = New System.Drawing.Point(112, 88)
        Me.minimumValueNumeric.Maximum = New Decimal(New Integer() {500, 0, 0, 0})
        Me.minimumValueNumeric.Minimum = New Decimal(New Integer() {500, 0, 0, -2147483648})
        Me.minimumValueNumeric.Name = "minimumValueNumeric"
        Me.minimumValueNumeric.Size = New System.Drawing.Size(96, 20)
        Me.minimumValueNumeric.TabIndex = 5
        '
        'maximumValueNumeric
        '
        Me.maximumValueNumeric.DecimalPlaces = 2
        Me.maximumValueNumeric.Location = New System.Drawing.Point(112, 56)
        Me.maximumValueNumeric.Maximum = New Decimal(New Integer() {500, 0, 0, 0})
        Me.maximumValueNumeric.Minimum = New Decimal(New Integer() {500, 0, 0, -2147483648})
        Me.maximumValueNumeric.Name = "maximumValueNumeric"
        Me.maximumValueNumeric.Size = New System.Drawing.Size(96, 20)
        Me.maximumValueNumeric.TabIndex = 3
        Me.maximumValueNumeric.Value = New Decimal(New Integer() {100, 0, 0, 0})
        '
        'maximumLabel
        '
        Me.maximumLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.maximumLabel.Location = New System.Drawing.Point(16, 59)
        Me.maximumLabel.Name = "maximumLabel"
        Me.maximumLabel.Size = New System.Drawing.Size(104, 14)
        Me.maximumLabel.TabIndex = 2
        Me.maximumLabel.Text = "Maximum (deg C):"
        '
        'minimumLabel
        '
        Me.minimumLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.minimumLabel.Location = New System.Drawing.Point(16, 91)
        Me.minimumLabel.Name = "minimumLabel"
        Me.minimumLabel.Size = New System.Drawing.Size(104, 14)
        Me.minimumLabel.TabIndex = 4
        Me.minimumLabel.Text = "Minimum (deg C):"
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
        'mainform
        '
        Me.AutoScaleBaseSize = New System.Drawing.Size(5, 13)
        Me.ClientSize = New System.Drawing.Size(522, 416)
        Me.Controls.Add(Me.stopButton)
        Me.Controls.Add(Me.startButton)
        Me.Controls.Add(Me.timingParametersGroupBox)
        Me.Controls.Add(Me.acquisitionResultsGroupBox)
        Me.Controls.Add(Me.resistanceParametersGroupBox)
        Me.Controls.Add(Me.thermistorParametersGroupBox)
        Me.Controls.Add(Me.channelParametersGroupBox)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MaximizeBox = False
        Me.Name = "mainform"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Continuous Acquisition Thermistor Samples - Internal  Clock - SCXI 1102 And 1581"
        Me.timingParametersGroupBox.ResumeLayout(False)
        CType(Me.rateNumeric, System.ComponentModel.ISupportInitialize).EndInit()
        Me.acquisitionResultsGroupBox.ResumeLayout(False)
        CType(Me.acquisitionDataGrid, System.ComponentModel.ISupportInitialize).EndInit()
        Me.resistanceParametersGroupBox.ResumeLayout(False)
        CType(Me.currentExcitationValueNumeric, System.ComponentModel.ISupportInitialize).EndInit()
        Me.thermistorParametersGroupBox.ResumeLayout(False)
        Me.channelParametersGroupBox.ResumeLayout(False)
        CType(Me.minimumValueNumeric, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.maximumValueNumeric, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub

#End Region

    Private Sub startButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles startButton.Click

        startButton.Enabled = False
        stopButton.Enabled = True

        Try

            myTask = New Task()
            Dim resistanceConfiguration As AIResistanceConfiguration
            Dim excitationSource As AIExcitationSource

            Select Case resistanceConfigComboBox.SelectedIndex

                Case 0
                    resistanceConfiguration = AIResistanceConfiguration.TwoWire
                Case 1
                    resistanceConfiguration = AIResistanceConfiguration.ThreeWire
                Case 2
                    resistanceConfiguration = AIResistanceConfiguration.FourWire
                Case Else

            End Select

            Select Case currentExcitationComboBox.SelectedIndex

                Case 0
                    excitationSource = AIExcitationSource.None

                Case 1
                    excitationSource = AIExcitationSource.Internal

                Case 2
                    excitationSource = AIExcitationSource.External
                Case Else
            End Select

            myTask.AIChannels.CreateThermistorIExChannel(physicalChannelComboBox.Text, _
                "", Convert.ToDouble(minimumValueNumeric.Value), Convert.ToDouble(maximumValueNumeric.Value), _
                AITemperatureUnits.DegreesC, resistanceConfiguration, excitationSource, _
                Convert.ToDouble(currentExcitationValueNumeric.Value), Convert.ToDouble(aTextBox.Text), _
                Convert.ToDouble(bTextBox.Text), Convert.ToDouble(cTextBox.Text))

            myTask.Timing.ConfigureSampleClock("", Convert.ToDouble(rateNumeric.Value), _
                SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, 1000)

            myTask.Control(TaskAction.Verify)

            analogInReader = New AnalogMultiChannelReader(myTask.Stream)

            runningTask = myTask

            ' Use SynchronizeCallbacks to specify that the object 
            ' marshals callbacks across threads appropriately.
            analogInReader.SynchronizeCallbacks = True
            analogInReader.BeginReadWaveform(Convert.ToInt32(rateNumeric.Value), myAsyncCallback, myTask)

            InitializeDataTable(myTask.AIChannels, dataTable)
            acquisitionDataGrid.DataSource = dataTable
        Catch exception As DaqException

            MessageBox.Show(exception.Message)
            myTask.Dispose()
            stopButton.Enabled = False
            startButton.Enabled = True
        End Try
    End Sub

    Private Sub stopButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles stopButton.Click
        runningTask = Nothing
        myTask.Dispose()
        stopButton.Enabled = False
        startButton.Enabled = True
    End Sub

    Private Sub AnalogInCallback(ByVal ar As IAsyncResult)
        Try
            If (Not (runningTask Is Nothing)) AndAlso runningTask Is ar.AsyncState Then
                data = analogInReader.EndReadWaveform(ar)
                dataToDataTable(data, dataTable)
                analogInReader.BeginMemoryOptimizedReadWaveform(Convert.ToInt32(rateNumeric.Value), myAsyncCallback, myTask, data)
            End If
        Catch exception As DaqException
            MessageBox.Show(exception.Message)
            myTask.Dispose()
            startButton.Enabled = True
            stopButton.Enabled = False
            runningTask = Nothing
        End Try
    End Sub

    Private Sub InitializeDataTable(ByVal channelCollection As AIChannelCollection, ByRef data As DataTable)
        If (channelCollection Is Nothing) Then
            End
        End If

        Dim numOfChannels As Int16 = channelCollection.Count
        data.Rows.Clear()
        data.Columns.Clear()
        dataColumn = New DataColumn(numOfChannels) {}
        Dim numOfRows As Integer = 10

        For currentChannelIndex As Integer = 0 To (numOfChannels - 1)
            dataColumn(currentChannelIndex) = New DataColumn
            dataColumn(currentChannelIndex).DataType = Type.GetType("System.Double")
            dataColumn(currentChannelIndex).ColumnName = channelCollection(currentChannelIndex).PhysicalName
        Next

        data.Columns.AddRange(dataColumn)

        For currentDataIndex As Integer = 0 To (numOfRows - 1)
            Dim rowArr() As Object = New Object(numOfChannels - 1) {}
            data.Rows.Add(rowArr)
        Next

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

End Class
