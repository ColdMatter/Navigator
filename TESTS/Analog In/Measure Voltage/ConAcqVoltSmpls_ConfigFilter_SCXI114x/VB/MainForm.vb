'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
'
' Example program:
'   ConAcqVoltSmpls_ConfigFilter_SCXI114x
'
' Category:
'   AI
'
' Description:
'   This example demonstrates how to acquire and filter an analog signal using
'   the SCXI-114x.
'
' Instructions for running:
'   1.  Select the physical channel corresponding to a channel on your SCXI-114x
'       module.
'   2.  Enter the minimum and maximum voltage values.Note: For better accuracy,
'       try to match the input range to the expected voltage level of the
'       measured signal.
'   3.  Enter the sample rate for the hardware-timed acquisition. Also set the
'       number of samples to read.
'   4.  Enter the desired cutoff frequency.
'
' Steps:
'   1.  Create a new Task. Create a AIChannel object by calling the
'       CreateVoltageChannel method.
'   2.  Set the rate for the sample clock by using the
'       Timing.ConfigureSampleClock method. Additionally, define the sample mode
'       to be continuous.
'   3.  Enable the filter and set the value of the cutoff frequency by using the
'       AIChannel.LowpassEnable and AIChannel.LowpassCutoffFrequency properties.
'       The driver will choose the cutoff frequency that is closest to the
'       desired cutoff frequency.
'   4.  Call AnalogMultiChannelReader.BeginReadWaveform to install a callback
'       and begin the asynchronous read operation.
'   5.  Inside the callback, call AnalogMultiChannelReader.EndReadWaveform to
'       retrieve the data from the read operation.  
'   6.  Call AnalogMultiChannelReader.BeginMemoryOptimizedReadWaveform
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
'   Make sure your signal output terminal matches the text in physical channel
'   control. For more information on the input and output terminals for your
'   device, open the NI-DAQmx Help, and refer to the NI-DAQmx Device Terminals
'   and Device Considerations books in the table of contents.
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

#Region " Windows Form Designer generated code "

    Public Sub New()
        MyBase.New()
        Application.EnableVisualStyles()

        'This call is required by the Windows Form Designer.
        InitializeComponent()

        asyncCB = New AsyncCallback(AddressOf AnalogInCallback)

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
    Friend WithEvents filterParametersGroupBox As System.Windows.Forms.GroupBox
    Friend WithEvents actualCutoffFrequencyTextBox As System.Windows.Forms.TextBox
    Friend WithEvents desiredCutoffFrequencyNumeric As System.Windows.Forms.NumericUpDown
    Friend WithEvents actualCutoffFrequencyLabel As System.Windows.Forms.Label
    Friend WithEvents desiredCutoffFrequencyLabel As System.Windows.Forms.Label
    Friend WithEvents stopButton As System.Windows.Forms.Button
    Friend WithEvents startButton As System.Windows.Forms.Button
    Friend WithEvents dataGroupBox As System.Windows.Forms.GroupBox
    Friend WithEvents amplitudeListBox As System.Windows.Forms.ListBox
    Friend WithEvents amplitudeLabel As System.Windows.Forms.Label
    Friend WithEvents channelParametersGroupBox As System.Windows.Forms.GroupBox
    Friend WithEvents maximumValueLabel As System.Windows.Forms.Label
    Friend WithEvents minimumValueLabel As System.Windows.Forms.Label
    Friend WithEvents physicalChannelLabel As System.Windows.Forms.Label
    Friend WithEvents timingParametersGroupBox As System.Windows.Forms.GroupBox
    Friend WithEvents samplesNumeric As System.Windows.Forms.NumericUpDown
    Friend WithEvents rateNumeric As System.Windows.Forms.NumericUpDown
    Friend WithEvents samplesToReadLabel As System.Windows.Forms.Label
    Friend WithEvents rateLabel As System.Windows.Forms.Label
    Friend WithEvents minimumValueNumeric As System.Windows.Forms.NumericUpDown
    Friend WithEvents maximumValueNumeric As System.Windows.Forms.NumericUpDown
    Friend WithEvents physicalChannelComboBox As System.Windows.Forms.ComboBox
    <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
        Dim resources As System.Resources.ResourceManager = New System.Resources.ResourceManager(GetType(MainForm))
        Me.filterParametersGroupBox = New System.Windows.Forms.GroupBox
        Me.actualCutoffFrequencyTextBox = New System.Windows.Forms.TextBox
        Me.desiredCutoffFrequencyNumeric = New System.Windows.Forms.NumericUpDown
        Me.actualCutoffFrequencyLabel = New System.Windows.Forms.Label
        Me.desiredCutoffFrequencyLabel = New System.Windows.Forms.Label
        Me.stopButton = New System.Windows.Forms.Button
        Me.startButton = New System.Windows.Forms.Button
        Me.dataGroupBox = New System.Windows.Forms.GroupBox
        Me.amplitudeListBox = New System.Windows.Forms.ListBox
        Me.amplitudeLabel = New System.Windows.Forms.Label
        Me.channelParametersGroupBox = New System.Windows.Forms.GroupBox
        Me.physicalChannelComboBox = New System.Windows.Forms.ComboBox
        Me.minimumValueNumeric = New System.Windows.Forms.NumericUpDown
        Me.maximumValueNumeric = New System.Windows.Forms.NumericUpDown
        Me.maximumValueLabel = New System.Windows.Forms.Label
        Me.minimumValueLabel = New System.Windows.Forms.Label
        Me.physicalChannelLabel = New System.Windows.Forms.Label
        Me.timingParametersGroupBox = New System.Windows.Forms.GroupBox
        Me.samplesNumeric = New System.Windows.Forms.NumericUpDown
        Me.rateNumeric = New System.Windows.Forms.NumericUpDown
        Me.samplesToReadLabel = New System.Windows.Forms.Label
        Me.rateLabel = New System.Windows.Forms.Label
        Me.filterParametersGroupBox.SuspendLayout()
        CType(Me.desiredCutoffFrequencyNumeric, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.dataGroupBox.SuspendLayout()
        Me.channelParametersGroupBox.SuspendLayout()
        CType(Me.minimumValueNumeric, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.maximumValueNumeric, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.timingParametersGroupBox.SuspendLayout()
        CType(Me.samplesNumeric, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.rateNumeric, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'filterParametersGroupBox
        '
        Me.filterParametersGroupBox.Controls.Add(Me.actualCutoffFrequencyTextBox)
        Me.filterParametersGroupBox.Controls.Add(Me.desiredCutoffFrequencyNumeric)
        Me.filterParametersGroupBox.Controls.Add(Me.actualCutoffFrequencyLabel)
        Me.filterParametersGroupBox.Controls.Add(Me.desiredCutoffFrequencyLabel)
        Me.filterParametersGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.filterParametersGroupBox.Location = New System.Drawing.Point(9, 244)
        Me.filterParametersGroupBox.Name = "filterParametersGroupBox"
        Me.filterParametersGroupBox.Size = New System.Drawing.Size(256, 92)
        Me.filterParametersGroupBox.TabIndex = 4
        Me.filterParametersGroupBox.TabStop = False
        Me.filterParametersGroupBox.Text = "Trigger Parameters"
        '
        'actualCutoffFrequencyTextBox
        '
        Me.actualCutoffFrequencyTextBox.Location = New System.Drawing.Point(144, 56)
        Me.actualCutoffFrequencyTextBox.Name = "actualCutoffFrequencyTextBox"
        Me.actualCutoffFrequencyTextBox.ReadOnly = True
        Me.actualCutoffFrequencyTextBox.Size = New System.Drawing.Size(96, 20)
        Me.actualCutoffFrequencyTextBox.TabIndex = 3
        Me.actualCutoffFrequencyTextBox.TabStop = False
        Me.actualCutoffFrequencyTextBox.Text = "1.00"
        '
        'desiredCutoffFrequencyNumeric
        '
        Me.desiredCutoffFrequencyNumeric.DecimalPlaces = 2
        Me.desiredCutoffFrequencyNumeric.Location = New System.Drawing.Point(144, 24)
        Me.desiredCutoffFrequencyNumeric.Maximum = New Decimal(New Integer() {10000, 0, 0, 0})
        Me.desiredCutoffFrequencyNumeric.Name = "desiredCutoffFrequencyNumeric"
        Me.desiredCutoffFrequencyNumeric.Size = New System.Drawing.Size(96, 20)
        Me.desiredCutoffFrequencyNumeric.TabIndex = 1
        Me.desiredCutoffFrequencyNumeric.Value = New Decimal(New Integer() {1, 0, 0, 0})
        '
        'actualCutoffFrequencyLabel
        '
        Me.actualCutoffFrequencyLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.actualCutoffFrequencyLabel.Location = New System.Drawing.Point(16, 56)
        Me.actualCutoffFrequencyLabel.Name = "actualCutoffFrequencyLabel"
        Me.actualCutoffFrequencyLabel.Size = New System.Drawing.Size(136, 16)
        Me.actualCutoffFrequencyLabel.TabIndex = 2
        Me.actualCutoffFrequencyLabel.Text = "Actual Cutoff Frequency:"
        '
        'desiredCutoffFrequencyLabel
        '
        Me.desiredCutoffFrequencyLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.desiredCutoffFrequencyLabel.Location = New System.Drawing.Point(16, 26)
        Me.desiredCutoffFrequencyLabel.Name = "desiredCutoffFrequencyLabel"
        Me.desiredCutoffFrequencyLabel.Size = New System.Drawing.Size(144, 16)
        Me.desiredCutoffFrequencyLabel.TabIndex = 0
        Me.desiredCutoffFrequencyLabel.Text = "Desired Cutoff Frequency:"
        '
        'stopButton
        '
        Me.stopButton.Enabled = False
        Me.stopButton.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.stopButton.Location = New System.Drawing.Point(313, 304)
        Me.stopButton.Name = "stopButton"
        Me.stopButton.Size = New System.Drawing.Size(80, 32)
        Me.stopButton.TabIndex = 1
        Me.stopButton.Text = "Stop"
        '
        'startButton
        '
        Me.startButton.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.startButton.Location = New System.Drawing.Point(313, 264)
        Me.startButton.Name = "startButton"
        Me.startButton.Size = New System.Drawing.Size(80, 32)
        Me.startButton.TabIndex = 0
        Me.startButton.Text = "Start"
        '
        'dataGroupBox
        '
        Me.dataGroupBox.Controls.Add(Me.amplitudeListBox)
        Me.dataGroupBox.Controls.Add(Me.amplitudeLabel)
        Me.dataGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.dataGroupBox.Location = New System.Drawing.Point(281, 12)
        Me.dataGroupBox.Name = "dataGroupBox"
        Me.dataGroupBox.Size = New System.Drawing.Size(144, 240)
        Me.dataGroupBox.TabIndex = 5
        Me.dataGroupBox.TabStop = False
        Me.dataGroupBox.Text = "Data"
        '
        'amplitudeListBox
        '
        Me.amplitudeListBox.Location = New System.Drawing.Point(8, 32)
        Me.amplitudeListBox.Name = "amplitudeListBox"
        Me.amplitudeListBox.RightToLeft = System.Windows.Forms.RightToLeft.No
        Me.amplitudeListBox.Size = New System.Drawing.Size(128, 199)
        Me.amplitudeListBox.TabIndex = 1
        Me.amplitudeListBox.TabStop = False
        '
        'amplitudeLabel
        '
        Me.amplitudeLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.amplitudeLabel.Location = New System.Drawing.Point(8, 16)
        Me.amplitudeLabel.Name = "amplitudeLabel"
        Me.amplitudeLabel.Size = New System.Drawing.Size(72, 16)
        Me.amplitudeLabel.TabIndex = 0
        Me.amplitudeLabel.Text = "Amplitude:"
        '
        'channelParametersGroupBox
        '
        Me.channelParametersGroupBox.Controls.Add(Me.physicalChannelComboBox)
        Me.channelParametersGroupBox.Controls.Add(Me.minimumValueNumeric)
        Me.channelParametersGroupBox.Controls.Add(Me.maximumValueNumeric)
        Me.channelParametersGroupBox.Controls.Add(Me.maximumValueLabel)
        Me.channelParametersGroupBox.Controls.Add(Me.minimumValueLabel)
        Me.channelParametersGroupBox.Controls.Add(Me.physicalChannelLabel)
        Me.channelParametersGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.channelParametersGroupBox.Location = New System.Drawing.Point(9, 12)
        Me.channelParametersGroupBox.Name = "channelParametersGroupBox"
        Me.channelParametersGroupBox.Size = New System.Drawing.Size(256, 120)
        Me.channelParametersGroupBox.TabIndex = 2
        Me.channelParametersGroupBox.TabStop = False
        Me.channelParametersGroupBox.Text = "Channel Parameters"
        '
        'physicalChannelComboBox
        '
        Me.physicalChannelComboBox.Location = New System.Drawing.Point(144, 24)
        Me.physicalChannelComboBox.Name = "physicalChannelComboBox"
        Me.physicalChannelComboBox.Size = New System.Drawing.Size(96, 21)
        Me.physicalChannelComboBox.TabIndex = 1
        Me.physicalChannelComboBox.Text = "SC1Mod1/ai0"
        '
        'minimumValueNumeric
        '
        Me.minimumValueNumeric.DecimalPlaces = 2
        Me.minimumValueNumeric.Location = New System.Drawing.Point(144, 56)
        Me.minimumValueNumeric.Maximum = New Decimal(New Integer() {10, 0, 0, 0})
        Me.minimumValueNumeric.Minimum = New Decimal(New Integer() {10, 0, 0, -2147483648})
        Me.minimumValueNumeric.Name = "minimumValueNumeric"
        Me.minimumValueNumeric.Size = New System.Drawing.Size(96, 20)
        Me.minimumValueNumeric.TabIndex = 3
        Me.minimumValueNumeric.Value = New Decimal(New Integer() {50, 0, 0, -2147418112})
        '
        'maximumValueNumeric
        '
        Me.maximumValueNumeric.DecimalPlaces = 2
        Me.maximumValueNumeric.Location = New System.Drawing.Point(144, 88)
        Me.maximumValueNumeric.Maximum = New Decimal(New Integer() {10, 0, 0, 0})
        Me.maximumValueNumeric.Minimum = New Decimal(New Integer() {10, 0, 0, -2147483648})
        Me.maximumValueNumeric.Name = "maximumValueNumeric"
        Me.maximumValueNumeric.Size = New System.Drawing.Size(96, 20)
        Me.maximumValueNumeric.TabIndex = 5
        Me.maximumValueNumeric.Value = New Decimal(New Integer() {50, 0, 0, 65536})
        '
        'maximumValueLabel
        '
        Me.maximumValueLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.maximumValueLabel.Location = New System.Drawing.Point(16, 90)
        Me.maximumValueLabel.Name = "maximumValueLabel"
        Me.maximumValueLabel.Size = New System.Drawing.Size(112, 16)
        Me.maximumValueLabel.TabIndex = 4
        Me.maximumValueLabel.Text = "Maximum Value (V):"
        '
        'minimumValueLabel
        '
        Me.minimumValueLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.minimumValueLabel.Location = New System.Drawing.Point(16, 58)
        Me.minimumValueLabel.Name = "minimumValueLabel"
        Me.minimumValueLabel.Size = New System.Drawing.Size(104, 16)
        Me.minimumValueLabel.TabIndex = 2
        Me.minimumValueLabel.Text = "Minimum Value (V):"
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
        'timingParametersGroupBox
        '
        Me.timingParametersGroupBox.Controls.Add(Me.samplesNumeric)
        Me.timingParametersGroupBox.Controls.Add(Me.rateNumeric)
        Me.timingParametersGroupBox.Controls.Add(Me.samplesToReadLabel)
        Me.timingParametersGroupBox.Controls.Add(Me.rateLabel)
        Me.timingParametersGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.timingParametersGroupBox.Location = New System.Drawing.Point(9, 148)
        Me.timingParametersGroupBox.Name = "timingParametersGroupBox"
        Me.timingParametersGroupBox.Size = New System.Drawing.Size(256, 80)
        Me.timingParametersGroupBox.TabIndex = 3
        Me.timingParametersGroupBox.TabStop = False
        Me.timingParametersGroupBox.Text = "Timing Parameters"
        '
        'samplesNumeric
        '
        Me.samplesNumeric.Location = New System.Drawing.Point(144, 48)
        Me.samplesNumeric.Maximum = New Decimal(New Integer() {1000000, 0, 0, 0})
        Me.samplesNumeric.Name = "samplesNumeric"
        Me.samplesNumeric.Size = New System.Drawing.Size(96, 20)
        Me.samplesNumeric.TabIndex = 3
        Me.samplesNumeric.Value = New Decimal(New Integer() {1000, 0, 0, 0})
        '
        'rateNumeric
        '
        Me.rateNumeric.DecimalPlaces = 2
        Me.rateNumeric.Location = New System.Drawing.Point(144, 16)
        Me.rateNumeric.Maximum = New Decimal(New Integer() {1000000, 0, 0, 0})
        Me.rateNumeric.Name = "rateNumeric"
        Me.rateNumeric.Size = New System.Drawing.Size(96, 20)
        Me.rateNumeric.TabIndex = 1
        Me.rateNumeric.Value = New Decimal(New Integer() {10000, 0, 0, 0})
        '
        'samplesToReadLabel
        '
        Me.samplesToReadLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.samplesToReadLabel.Location = New System.Drawing.Point(14, 50)
        Me.samplesToReadLabel.Name = "samplesToReadLabel"
        Me.samplesToReadLabel.Size = New System.Drawing.Size(98, 16)
        Me.samplesToReadLabel.TabIndex = 2
        Me.samplesToReadLabel.Text = "Samples to Read:"
        '
        'rateLabel
        '
        Me.rateLabel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.rateLabel.Location = New System.Drawing.Point(16, 18)
        Me.rateLabel.Name = "rateLabel"
        Me.rateLabel.Size = New System.Drawing.Size(56, 16)
        Me.rateLabel.TabIndex = 0
        Me.rateLabel.Text = "Rate:"
        '
        'MainForm
        '
        Me.AutoScaleBaseSize = New System.Drawing.Size(5, 13)
        Me.ClientSize = New System.Drawing.Size(434, 344)
        Me.Controls.Add(Me.filterParametersGroupBox)
        Me.Controls.Add(Me.stopButton)
        Me.Controls.Add(Me.startButton)
        Me.Controls.Add(Me.dataGroupBox)
        Me.Controls.Add(Me.channelParametersGroupBox)
        Me.Controls.Add(Me.timingParametersGroupBox)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MaximizeBox = False
        Me.Name = "MainForm"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Continuous Acqusition - Configurable Filter - SCXI114x"
        Me.filterParametersGroupBox.ResumeLayout(False)
        CType(Me.desiredCutoffFrequencyNumeric, System.ComponentModel.ISupportInitialize).EndInit()
        Me.dataGroupBox.ResumeLayout(False)
        Me.channelParametersGroupBox.ResumeLayout(False)
        CType(Me.minimumValueNumeric, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.maximumValueNumeric, System.ComponentModel.ISupportInitialize).EndInit()
        Me.timingParametersGroupBox.ResumeLayout(False)
        CType(Me.samplesNumeric, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.rateNumeric, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub

#End Region

    Private myTask As Task
    Private runningTask As Task
    Private numberOfSamples As Int32 = 0
    Private data As AnalogWaveform(Of Double)

    Private analogInReader As AnalogSingleChannelReader
    Private asyncCB As AsyncCallback


    Private Sub startButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles startButton.Click

        amplitudeListBox.Items.Clear()
        numberOfSamples = Convert.ToInt32(samplesNumeric.Value)
        Try
            'Create a new task
            myTask = New Task()

            Dim myChannel As AIChannel = myTask.AIChannels.CreateVoltageChannel(physicalChannelComboBox.Text, _
                "", CType(-1, AITerminalConfiguration), Convert.ToDouble(minimumValueNumeric.Value), _
                Convert.ToDouble(maximumValueNumeric.Value), AIVoltageUnits.Volts)

            myTask.Timing.ConfigureSampleClock("", Convert.ToDouble(rateNumeric.Value), _
                SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, 1000)

            myChannel.LowpassEnable = True
            myChannel.LowpassCutoffFrequency = Convert.ToDouble(desiredCutoffFrequencyNumeric.Value)
            myTask.Control(TaskAction.Verify)
            actualCutoffFrequencyTextBox.Text = myChannel.LowpassCutoffFrequency.ToString("f2")

            runningTask = myTask
            analogInReader = New AnalogSingleChannelReader(myTask.Stream)

            ' Use SynchronizeCallbacks to specify that the object 
            ' marshals callbacks across threads appropriately.
            analogInReader.SynchronizeCallbacks = True
            analogInReader.BeginReadWaveform(numberOfSamples, asyncCB, myTask)

            startButton.Enabled = False
            stopButton.Enabled = True

        Catch ex As DaqException
            MessageBox.Show(ex.Message)
            myTask.Dispose()
            runningTask = Nothing
        End Try
    End Sub

    Private Sub AnalogInCallback(ByVal ar As IAsyncResult)
        Try
            If (Not (runningTask Is Nothing)) AndAlso runningTask Is ar.AsyncState Then
                Dim iteration, i As Integer

                data = analogInReader.EndReadWaveform(ar)

                ' Display only the first 15 data points in the listbox. 
                If (data.Samples.Count < 15) Then
                    iteration = data.Samples.Count
                Else
                    iteration = 15
                End If

                amplitudeListBox.BeginUpdate()
                amplitudeListBox.Items.Clear()

                For i = 0 To iteration
                    amplitudeListBox.Items.Add(data.Samples(i).Value)
                Next
                amplitudeListBox.EndUpdate()

                analogInReader.BeginMemoryOptimizedReadWaveform(numberOfSamples, asyncCB, myTask, data)

            End If
        Catch exception As DaqException
            MessageBox.Show(exception.Message)
            stopButton_Click(Nothing, Nothing)
        End Try

    End Sub

    Private Sub stopButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles stopButton.Click
        myTask.Dispose()
        runningTask = Nothing
        startButton.Enabled = True
        stopButton.Enabled = False
    End Sub
End Class
