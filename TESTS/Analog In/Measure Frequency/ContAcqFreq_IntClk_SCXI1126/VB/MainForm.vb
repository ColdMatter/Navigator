'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
'
' Example program:
'   ContAcqFreq_IntClk_SCXI1126
'
' Category:
'   AI
'
' Description:
'   This example demonstrates how to acquire frequency data from an SCXI-1126
'   using the DAQ device's internal clock.
'
' Instructions for running:
'   1.  Select the physical channel that corresponds to a channel on your
'       SCXI-1126.
'   2.  Enter the minimum and maximum frequency ranges.Note: For better
'       accuracy, try to match the input range to the expected frequency level
'       of the measured signal.
'   3.  Enter the sample rate for the hardware-timed acquisition. Also set the
'       number of samples to read. This will determine how many samples are read
'       each time the while loop iterates.
'   4.  Enter the level and hysteresis of the triggering window.Note: The
'       triggering window is defined as Threshold-Hysteresis to Threshold Level
'       and must be between -0.5 and 4.48.
'
' Steps:
'   1.  Create a Task object.  Create an AIChannel object using the
'       AIChannelCollection.CreateFrequencyVoltageChannel method.
'   2.  Set the rate for the sample clock using the Timing.ConfigureSampleClock
'       method.  Additionally, define the sample mode to be continuous.
'   3.  Set the value of the cutoff frequency for the low pass filter on the
'       SCXI-1126 module using the AIChannel.LowpassCutoffFrequency property.
'   4.  Call AnalogSingleChannelReader.BeginReadWaveform to install a callback
'       and begin the asynchronous read operation.
'   5.  Inside the callback, call AnalogSingleChannelReader.EndReadWaveform to
'       retrieve the data from the read operation.  
'   6.  Call AnalogSingleChannelReader.BeginMemoryOptimizedReadWaveform
'   7.  Dispose the Task object to clean-up any resources associated with the
'       task.
'   8.  Handle any DaqExceptions, if they occur.
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

Public Class MainForm
    Inherits System.Windows.Forms.Form

    Private myTask As Task
    Private analogInReader As AnalogSingleChannelReader
    Private runningTask As Task
    Private data As AnalogWaveform(Of Double)
    Private analogCallback As AsyncCallback


#Region " Windows Form Designer generated code "

    Public Sub New()
        MyBase.New()
        Application.EnableVisualStyles()

        'This call is required by the Windows Form Designer.
        InitializeComponent()

        'Add any initialization after the InitializeComponent() call
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
    Friend WithEvents sampleRateLabel As System.Windows.Forms.Label
    Friend WithEvents lowPassCutoffLabel As System.Windows.Forms.Label
    Friend WithEvents stopButton As System.Windows.Forms.Button
    Friend WithEvents startButton As System.Windows.Forms.Button
    Friend WithEvents measurementLabel As System.Windows.Forms.Label
    Friend WithEvents measurementListBox As System.Windows.Forms.ListBox
    Friend WithEvents triggerParametersGroupBox As System.Windows.Forms.GroupBox
    Friend WithEvents levelLabel As System.Windows.Forms.Label
    Friend WithEvents rateNumeric As System.Windows.Forms.NumericUpDown
    Friend WithEvents samplesToReadNumeric As System.Windows.Forms.NumericUpDown
    Friend WithEvents levelNumeric As System.Windows.Forms.NumericUpDown
    Friend WithEvents hysteresisNumeric As System.Windows.Forms.NumericUpDown
    Friend WithEvents hysteresisLabel As System.Windows.Forms.Label
    Friend WithEvents timingParametersGroupbox As System.Windows.Forms.GroupBox
    Friend WithEvents samplesToReadLabel As System.Windows.Forms.Label
    Friend WithEvents channelParametersGroupBox As System.Windows.Forms.GroupBox
    Friend WithEvents physicalChannelLabel As System.Windows.Forms.Label
    Friend WithEvents maximumValueLabel As System.Windows.Forms.Label
    Friend WithEvents minimumValueLabel As System.Windows.Forms.Label
    Friend WithEvents lowPassCutoffNumeric As System.Windows.Forms.NumericUpDown
    Friend WithEvents maximumValueNumeric As System.Windows.Forms.NumericUpDown
    Friend WithEvents minimumValueNumeric As System.Windows.Forms.NumericUpDown
    Friend WithEvents physicalChannelComboBox As System.Windows.Forms.ComboBox
    <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
        Dim resources As System.Resources.ResourceManager = New System.Resources.ResourceManager(GetType(MainForm))
        Me.timingParametersGroupbox = New System.Windows.Forms.GroupBox
        Me.samplesToReadNumeric = New System.Windows.Forms.NumericUpDown
        Me.rateNumeric = New System.Windows.Forms.NumericUpDown
        Me.sampleRateLabel = New System.Windows.Forms.Label
        Me.samplesToReadLabel = New System.Windows.Forms.Label
        Me.channelParametersGroupBox = New System.Windows.Forms.GroupBox
        Me.physicalChannelComboBox = New System.Windows.Forms.ComboBox
        Me.maximumValueNumeric = New System.Windows.Forms.NumericUpDown
        Me.minimumValueNumeric = New System.Windows.Forms.NumericUpDown
        Me.lowPassCutoffNumeric = New System.Windows.Forms.NumericUpDown
        Me.lowPassCutoffLabel = New System.Windows.Forms.Label
        Me.physicalChannelLabel = New System.Windows.Forms.Label
        Me.maximumValueLabel = New System.Windows.Forms.Label
        Me.minimumValueLabel = New System.Windows.Forms.Label
        Me.stopButton = New System.Windows.Forms.Button
        Me.startButton = New System.Windows.Forms.Button
        Me.measurementLabel = New System.Windows.Forms.Label
        Me.measurementListBox = New System.Windows.Forms.ListBox
        Me.triggerParametersGroupBox = New System.Windows.Forms.GroupBox
        Me.hysteresisNumeric = New System.Windows.Forms.NumericUpDown
        Me.levelNumeric = New System.Windows.Forms.NumericUpDown
        Me.levelLabel = New System.Windows.Forms.Label
        Me.hysteresisLabel = New System.Windows.Forms.Label
        Me.timingParametersGroupbox.SuspendLayout()
        CType(Me.samplesToReadNumeric, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.rateNumeric, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.channelParametersGroupBox.SuspendLayout()
        CType(Me.maximumValueNumeric, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.minimumValueNumeric, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.lowPassCutoffNumeric, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.triggerParametersGroupBox.SuspendLayout()
        CType(Me.hysteresisNumeric, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.levelNumeric, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'timingParametersGroupbox
        '
        Me.timingParametersGroupbox.Controls.Add(Me.samplesToReadNumeric)
        Me.timingParametersGroupbox.Controls.Add(Me.rateNumeric)
        Me.timingParametersGroupbox.Controls.Add(Me.sampleRateLabel)
        Me.timingParametersGroupbox.Controls.Add(Me.samplesToReadLabel)
        Me.timingParametersGroupbox.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.timingParametersGroupbox.Location = New System.Drawing.Point(8, 176)
        Me.timingParametersGroupbox.Name = "timingParametersGroupbox"
        Me.timingParametersGroupbox.Size = New System.Drawing.Size(240, 88)
        Me.timingParametersGroupbox.TabIndex = 3
        Me.timingParametersGroupbox.TabStop = False
        Me.timingParametersGroupbox.Text = "Timing Parameters"
        '
        'samplesToReadNumeric
        '
        Me.samplesToReadNumeric.Location = New System.Drawing.Point(128, 56)
        Me.samplesToReadNumeric.Maximum = New Decimal(New Integer() {100000, 0, 0, 0})
        Me.samplesToReadNumeric.Name = "samplesToReadNumeric"
        Me.samplesToReadNumeric.Size = New System.Drawing.Size(96, 20)
        Me.samplesToReadNumeric.TabIndex = 3
        Me.samplesToReadNumeric.Value = New Decimal(New Integer() {10000, 0, 0, 0})
        '
        'rateNumeric
        '
        Me.rateNumeric.DecimalPlaces = 2
        Me.rateNumeric.Location = New System.Drawing.Point(128, 24)
        Me.rateNumeric.Maximum = New Decimal(New Integer() {1000000, 0, 0, 0})
        Me.rateNumeric.Name = "rateNumeric"
        Me.rateNumeric.Size = New System.Drawing.Size(96, 20)
        Me.rateNumeric.TabIndex = 1
        Me.rateNumeric.Value = New Decimal(New Integer() {10000, 0, 0, 0})
        '
        'sampleRateLabel
        '
        Me.sampleRateLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.sampleRateLabel.Location = New System.Drawing.Point(16, 24)
        Me.sampleRateLabel.Name = "sampleRateLabel"
        Me.sampleRateLabel.Size = New System.Drawing.Size(100, 16)
        Me.sampleRateLabel.TabIndex = 0
        Me.sampleRateLabel.Text = "Sample Rate (Hz):"
        '
        'samplesToReadLabel
        '
        Me.samplesToReadLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.samplesToReadLabel.Location = New System.Drawing.Point(16, 56)
        Me.samplesToReadLabel.Name = "samplesToReadLabel"
        Me.samplesToReadLabel.Size = New System.Drawing.Size(100, 16)
        Me.samplesToReadLabel.TabIndex = 2
        Me.samplesToReadLabel.Text = "Samples to Read:"
        '
        'channelParametersGroupBox
        '
        Me.channelParametersGroupBox.Controls.Add(Me.physicalChannelComboBox)
        Me.channelParametersGroupBox.Controls.Add(Me.maximumValueNumeric)
        Me.channelParametersGroupBox.Controls.Add(Me.minimumValueNumeric)
        Me.channelParametersGroupBox.Controls.Add(Me.lowPassCutoffNumeric)
        Me.channelParametersGroupBox.Controls.Add(Me.lowPassCutoffLabel)
        Me.channelParametersGroupBox.Controls.Add(Me.physicalChannelLabel)
        Me.channelParametersGroupBox.Controls.Add(Me.maximumValueLabel)
        Me.channelParametersGroupBox.Controls.Add(Me.minimumValueLabel)
        Me.channelParametersGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.channelParametersGroupBox.Location = New System.Drawing.Point(8, 8)
        Me.channelParametersGroupBox.Name = "channelParametersGroupBox"
        Me.channelParametersGroupBox.Size = New System.Drawing.Size(240, 157)
        Me.channelParametersGroupBox.TabIndex = 2
        Me.channelParametersGroupBox.TabStop = False
        Me.channelParametersGroupBox.Text = "Channel Parameters"
        '
        'physicalChannelComboBox
        '
        Me.physicalChannelComboBox.Location = New System.Drawing.Point(128, 24)
        Me.physicalChannelComboBox.Name = "physicalChannelComboBox"
        Me.physicalChannelComboBox.Size = New System.Drawing.Size(96, 21)
        Me.physicalChannelComboBox.TabIndex = 1
        Me.physicalChannelComboBox.Text = "SC1Mod1/ai0"
        '
        'maximumValueNumeric
        '
        Me.maximumValueNumeric.DecimalPlaces = 2
        Me.maximumValueNumeric.Location = New System.Drawing.Point(128, 56)
        Me.maximumValueNumeric.Maximum = New Decimal(New Integer() {1000000, 0, 0, 0})
        Me.maximumValueNumeric.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.maximumValueNumeric.Name = "maximumValueNumeric"
        Me.maximumValueNumeric.Size = New System.Drawing.Size(96, 20)
        Me.maximumValueNumeric.TabIndex = 3
        Me.maximumValueNumeric.Value = New Decimal(New Integer() {1000, 0, 0, 0})
        '
        'minimumValueNumeric
        '
        Me.minimumValueNumeric.DecimalPlaces = 2
        Me.minimumValueNumeric.Location = New System.Drawing.Point(128, 88)
        Me.minimumValueNumeric.Maximum = New Decimal(New Integer() {1000000, 0, 0, 0})
        Me.minimumValueNumeric.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.minimumValueNumeric.Name = "minimumValueNumeric"
        Me.minimumValueNumeric.Size = New System.Drawing.Size(96, 20)
        Me.minimumValueNumeric.TabIndex = 5
        Me.minimumValueNumeric.Value = New Decimal(New Integer() {1, 0, 0, 0})
        '
        'lowPassCutoffNumeric
        '
        Me.lowPassCutoffNumeric.Location = New System.Drawing.Point(128, 120)
        Me.lowPassCutoffNumeric.Name = "lowPassCutoffNumeric"
        Me.lowPassCutoffNumeric.Size = New System.Drawing.Size(96, 20)
        Me.lowPassCutoffNumeric.TabIndex = 7
        Me.lowPassCutoffNumeric.Value = New Decimal(New Integer() {1, 0, 0, 0})
        '
        'lowPassCutoffLabel
        '
        Me.lowPassCutoffLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.lowPassCutoffLabel.Location = New System.Drawing.Point(16, 120)
        Me.lowPassCutoffLabel.Name = "lowPassCutoffLabel"
        Me.lowPassCutoffLabel.Size = New System.Drawing.Size(120, 16)
        Me.lowPassCutoffLabel.TabIndex = 6
        Me.lowPassCutoffLabel.Text = "Cutoff Frequency (Hz):"
        '
        'physicalChannelLabel
        '
        Me.physicalChannelLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.physicalChannelLabel.Location = New System.Drawing.Point(16, 24)
        Me.physicalChannelLabel.Name = "physicalChannelLabel"
        Me.physicalChannelLabel.Size = New System.Drawing.Size(100, 16)
        Me.physicalChannelLabel.TabIndex = 0
        Me.physicalChannelLabel.Text = "Physical Channel:"
        '
        'maximumValueLabel
        '
        Me.maximumValueLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.maximumValueLabel.Location = New System.Drawing.Point(16, 56)
        Me.maximumValueLabel.Name = "maximumValueLabel"
        Me.maximumValueLabel.Size = New System.Drawing.Size(120, 16)
        Me.maximumValueLabel.TabIndex = 2
        Me.maximumValueLabel.Text = "Maximum Value (Hz):"
        '
        'minimumValueLabel
        '
        Me.minimumValueLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.minimumValueLabel.Location = New System.Drawing.Point(16, 88)
        Me.minimumValueLabel.Name = "minimumValueLabel"
        Me.minimumValueLabel.Size = New System.Drawing.Size(112, 16)
        Me.minimumValueLabel.TabIndex = 4
        Me.minimumValueLabel.Text = "Minimum Value (Hz):"
        '
        'stopButton
        '
        Me.stopButton.Enabled = False
        Me.stopButton.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.stopButton.Location = New System.Drawing.Point(280, 224)
        Me.stopButton.Name = "stopButton"
        Me.stopButton.Size = New System.Drawing.Size(71, 29)
        Me.stopButton.TabIndex = 1
        Me.stopButton.Text = "Stop"
        '
        'startButton
        '
        Me.startButton.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.startButton.Location = New System.Drawing.Point(280, 184)
        Me.startButton.Name = "startButton"
        Me.startButton.Size = New System.Drawing.Size(71, 29)
        Me.startButton.TabIndex = 0
        Me.startButton.Text = "Start"
        '
        'measurementLabel
        '
        Me.measurementLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.measurementLabel.Location = New System.Drawing.Point(256, 16)
        Me.measurementLabel.Name = "measurementLabel"
        Me.measurementLabel.Size = New System.Drawing.Size(100, 16)
        Me.measurementLabel.TabIndex = 5
        Me.measurementLabel.Text = "Measurement:"
        '
        'measurementListBox
        '
        Me.measurementListBox.Location = New System.Drawing.Point(256, 32)
        Me.measurementListBox.Name = "measurementListBox"
        Me.measurementListBox.Size = New System.Drawing.Size(120, 134)
        Me.measurementListBox.TabIndex = 6
        Me.measurementListBox.TabStop = False
        '
        'triggerParametersGroupBox
        '
        Me.triggerParametersGroupBox.Controls.Add(Me.hysteresisNumeric)
        Me.triggerParametersGroupBox.Controls.Add(Me.levelNumeric)
        Me.triggerParametersGroupBox.Controls.Add(Me.levelLabel)
        Me.triggerParametersGroupBox.Controls.Add(Me.hysteresisLabel)
        Me.triggerParametersGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.triggerParametersGroupBox.Location = New System.Drawing.Point(8, 280)
        Me.triggerParametersGroupBox.Name = "triggerParametersGroupBox"
        Me.triggerParametersGroupBox.Size = New System.Drawing.Size(240, 88)
        Me.triggerParametersGroupBox.TabIndex = 4
        Me.triggerParametersGroupBox.TabStop = False
        Me.triggerParametersGroupBox.Text = "Trigger Parameters"
        '
        'hysteresisNumeric
        '
        Me.hysteresisNumeric.DecimalPlaces = 2
        Me.hysteresisNumeric.Location = New System.Drawing.Point(128, 56)
        Me.hysteresisNumeric.Name = "hysteresisNumeric"
        Me.hysteresisNumeric.Size = New System.Drawing.Size(96, 20)
        Me.hysteresisNumeric.TabIndex = 3
        Me.hysteresisNumeric.Value = New Decimal(New Integer() {20, 0, 0, 131072})
        '
        'levelNumeric
        '
        Me.levelNumeric.DecimalPlaces = 2
        Me.levelNumeric.Location = New System.Drawing.Point(128, 24)
        Me.levelNumeric.Name = "levelNumeric"
        Me.levelNumeric.Size = New System.Drawing.Size(96, 20)
        Me.levelNumeric.TabIndex = 1
        '
        'levelLabel
        '
        Me.levelLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.levelLabel.Location = New System.Drawing.Point(16, 26)
        Me.levelLabel.Name = "levelLabel"
        Me.levelLabel.Size = New System.Drawing.Size(64, 16)
        Me.levelLabel.TabIndex = 0
        Me.levelLabel.Text = "Level (V):"
        '
        'hysteresisLabel
        '
        Me.hysteresisLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.hysteresisLabel.Location = New System.Drawing.Point(16, 58)
        Me.hysteresisLabel.Name = "hysteresisLabel"
        Me.hysteresisLabel.Size = New System.Drawing.Size(80, 16)
        Me.hysteresisLabel.TabIndex = 2
        Me.hysteresisLabel.Text = "Hysteresis (V):"
        '
        'MainForm
        '
        Me.AutoScaleBaseSize = New System.Drawing.Size(5, 13)
        Me.ClientSize = New System.Drawing.Size(386, 376)
        Me.Controls.Add(Me.timingParametersGroupbox)
        Me.Controls.Add(Me.channelParametersGroupBox)
        Me.Controls.Add(Me.stopButton)
        Me.Controls.Add(Me.startButton)
        Me.Controls.Add(Me.measurementLabel)
        Me.Controls.Add(Me.measurementListBox)
        Me.Controls.Add(Me.triggerParametersGroupBox)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MaximizeBox = False
        Me.Name = "MainForm"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Cont Acq Freq Samples - IntClk - SCXI1126"
        Me.timingParametersGroupbox.ResumeLayout(False)
        CType(Me.samplesToReadNumeric, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.rateNumeric, System.ComponentModel.ISupportInitialize).EndInit()
        Me.channelParametersGroupBox.ResumeLayout(False)
        CType(Me.maximumValueNumeric, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.minimumValueNumeric, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.lowPassCutoffNumeric, System.ComponentModel.ISupportInitialize).EndInit()
        Me.triggerParametersGroupBox.ResumeLayout(False)
        CType(Me.hysteresisNumeric, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.levelNumeric, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub

#End Region


    Private Sub startButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles startButton.Click
        Try
            myTask = New Task()

            Dim myAIChannel As AIChannel

            myAIChannel = myTask.AIChannels.CreateFrequencyVoltageChannel(physicalChannelComboBox.Text, "", _
                Convert.ToDouble(minimumValueNumeric.Value), Convert.ToDouble(maximumValueNumeric.Value), _
                Convert.ToDouble(levelNumeric.Value), Convert.ToDouble(hysteresisNumeric.Value), _
                AIFrequencyUnits.Hertz)

            myTask.Timing.ConfigureSampleClock("", Convert.ToDouble(rateNumeric.Value), _
                    SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, 1000)

            myAIChannel.LowpassCutoffFrequency = Convert.ToDouble(lowPassCutoffNumeric.Value)
            analogCallback = New AsyncCallback(AddressOf AnalogInCallback)

            runningTask = myTask
            analogInReader = New AnalogSingleChannelReader(myTask.Stream)

            ' Use SynchronizeCallbacks to specify that the object 
            ' marshals callbacks across threads appropriately.
            analogInReader.SynchronizeCallbacks = True
            analogInReader.BeginReadWaveform(Convert.ToInt32(samplesToReadNumeric.Value), _
            analogCallback, myTask)

            startButton.Enabled = False
            stopButton.Enabled = True


        Catch exception As DaqException

            MessageBox.Show(exception.Message)
            myTask.Dispose()
            startButton.Enabled = True
            stopButton.Enabled = False
            runningTask = Nothing

        End Try
    End Sub

    Private Sub stopButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles stopButton.Click
        runningTask = Nothing
        myTask.Dispose()
        startButton.Enabled = True
        stopButton.Enabled = False
    End Sub

    Private Sub AnalogInCallback(ByVal ar As IAsyncResult)
        If (Not (runningTask Is Nothing)) AndAlso runningTask Is ar.AsyncState Then
            Try
                Dim iteration, i As Integer
                measurementListBox.BeginUpdate()
                measurementListBox.Items.Clear()

                ' Retrieve data
                data = analogInReader.EndReadWaveform(ar)

                ' Display only the first 10 data points in the listbox. 
                If (data.Samples.Count < 10) Then
                    iteration = data.Samples.Count
                Else
                    iteration = 9
                End If

                For i = 0 To iteration
                    measurementListBox.Items.Add(data.Samples(i).Value)
                Next
                measurementListBox.EndUpdate()

                ' Begin next read
                analogInReader.BeginMemoryOptimizedReadWaveform(Convert.ToInt32(samplesToReadNumeric.Value), analogCallback, _
                      myTask, data)
            Catch exception As DaqException
                MessageBox.Show(exception.Message)
                myTask.Dispose()
                runningTask = Nothing
                startButton.Enabled = True
                stopButton.Enabled = False
            End Try
        End If
    End Sub
End Class
