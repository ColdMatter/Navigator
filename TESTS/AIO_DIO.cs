/*
C# Walkthrough: Modifying a DAQ Device in a System Definition File

You can use any .NET-compatible programming language to access the NI VeriStand System Definition .NET API and programmatically configure DAQ devices in a system definition file.

The example code in this topic, written in C#, uses the System Definition API to programmatically configure a DAQ device in a system definition.
++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++*/

public static void Main(string[] args)
{

// Locate the system definition file, ExampleSDF_ModifyDAQ.nivssdf, relative to the current directory.
string exampleSDFPath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName + "\\ExampleSDF_ModifyDAQ.nivssdf";



// Open an existing system definition file from disk.
SystemDefinition exampleSDF = newSystemDefinition(exampleSDFPath);



// Find the first target in the system definition file by traversing the following nodes in the system definition file, in descending order: root-level item, Targets section, all targets.
Target firstTarget = exampleSDF.Root.GetTargets().GetTargetList()[0];



// Find the first DAQ device in the DAQ section by traversing the following nodes in the system definition file, in descending order: first Target, Hardware section, first Chassis, DAQ section.
DAQDevice firstDAQDevice = firstTarget.GetHardware().GetChassisList()[0].GetDAQ().GetDeviceList()[0];

// Add new analog inputs DAQAnalogInputs daqAnalogInputsSection; // Instantiate class for Analog Inputs section.
DAQAnalogInput newAnalogInputChannel; // Instantiate class for analog input channel.
firstDAQDevice.GetAnalogInputSection().RemoveNode(); // Remove any existing analog inputs by removing the existing Analog Input section.
firstDAQDevice.CreateAnalogInputs(out daqAnalogInputsSection); // Create a new Analog Inputs section under the first DAQ device.
newAnalogInputChannel = newDAQAnalogInput("AI0", 0, DAQMeasurementType.AnalogInputVoltage, 0); // Create a new analog input named AI0.
daqAnalogInputsSection.AddAnalogInput(newAnalogInputChannel); // Add the new analog input channel to the Analog Input section.



// Add new analog outputs.
DAQAnalogOutputs daqAnalogOutputsSection; // Instantiate class for Analog Outputs section.
DAQAnalogOutput newAnalogOutputChannel; // Instantiate class for analog output channel.
firstDAQDevice.GetAnalogOutputSection().RemoveNode(); // Remove any existing analog outputs by removing the existing Analog Outputs section.
firstDAQDevice.CreateAnalogOutputs(out daqAnalogOutputsSection); // Create a new Analog Outputs section under the first DAQ device.
newAnalogOutputChannel = newDAQAnalogOutput("AO0", 0, DAQMeasurementType.AnalogOutputVoltage, 0); // Create new analog output channel named AO0.
daqAnalogOutputsSection.AddAnalogOutput(newAnalogOutputChannel); // Add the new analag output channel to the Analog Output section.



// Add new digital inputs
DAQDigitalInputs daqDigitalInputSection; // Instantiate class for Digital Input section.
DAQDigitalInput newDigitalInputChannel; // Instantiate class for digital input channel.
DAQDIOPort newDigitalInputPort; // Instantiate class for DAQ DIO port.
firstDAQDevice.GetDigitalInputSection().RemoveNode(); // Remove any existing digital inputs by removing the existing Digital Input section.
firstDAQDevice.CreateDigitalInputs(out daqDigitalInputSection); // Create a new Digital Input section under the first DAQ device.
newDigitalInputChannel = newDAQDigitalInput("DI0", 0, 0, true, DAQMeasurementType.DigitalInput); // Create a new digital input channel named DI0.
newDigitalInputPort = newDAQDIOPort(0, true); // Create a new DAQ DIO port.
newDigitalInputPort.AddDigitalInput(newDigitalInputChannel); // Add the new digital input channel to the DAQ DIO port.
daqDigitalInputSection.AddDIOPort(newDigitalInputPort); // Add the DAQ DIO port to the new Digital Input section.



// Add new digital outputs.
DAQDigitalOutputs daqDigitalOutputSection; // Instantiate class for Digital Ouput section.
DAQDigitalOutput newDigitalOutputChannel; // Instantiate class for digital output channel.
DAQDIOPort newDigitalOutputPort; // Instantiate class for DAQ DIO Port.
firstDAQDevice.GetDigitalOutputSection().RemoveNode(); // Remove any existing digital outputs by removing the existing Digital Outputs section.
firstDAQDevice.CreateDigitalOutputs(out daqDigitalOutputSection); // Create a new Digital Outputs section under the first DAQ device.
newDigitalOutputChannel = newDAQDigitalOutput("DO0", 0, 0, true, DAQMeasurementType.DigitalOutput); // Create a new digital output channel named DO0.
newDigitalOutputPort = newDAQDIOPort(0, false); // Create a new DAQ DIO port.
newDigitalOutputPort.AddDigitalOutput(newDigitalOutputChannel); // Add the digital output channel to the new DAQ DIO Port.
daqDigitalOutputSection.AddDIOPort(newDigitalOutputPort); // Add the DAQ DIO port to the new Digital Output section


// Create aliases for the new DAQ channels
exampleSDF.Root.GetAliases().AddNewAliasByReference("Analog_Input0", string.Empty, newAnalogInputChannel);
exampleSDF.Root.GetAliases().AddNewAliasByReference("Analog_Output0", string.Empty, newAnalogOutputChannel);
exampleSDF.Root.GetAliases().AddNewAliasByReference("Digital_Input0", string.Empty, newDigitalInputChannel);
exampleSDF.Root.GetAliases().AddNewAliasByReference("Digital_Output0", string.Empty, newDigitalOutputChannel);



// Create channel mappings such that AI0 maps to AO0 and DI0 maps to DO0. Note that the method AddChannelMappings requires arrays as parameters. This allows you to simultaneously create multiple channel mappings.
string[] daqChannelPathSources = new string[] { newAnalogInputChannel.NodePath, newDigitalInputChannel.NodePath };
string[] daqChannelPathDestinations = new string[] { newAnalogOutputChannel.NodePath, newDigitalOutputChannel.NodePath };
exampleSDF.Root.AddChannelMappings(daqChannelPathSources, daqChannelPathDestinations);



// Remove any aliases that have null references.
for (int i = 0; i <= exampleSDF.Root.GetAliases().GetAliasesList().Length - 1; i++)
{

if (exampleSDF.Root.GetAliases().GetAliasesList()[i].LinkedChannel == null)
{

    exampleSDF.Root.GetAliases().GetAliasesList()[i].RemoveNode();

}

}



// Remove any channel mappings that do not have a source.
string[] sdfChannelPathSources; // Declare arrays for the sources and destinations of all channel mappings in the system definition.
string[] sdfChannelPathDestinations;



exampleSDF.Root.GetChannelMappings(out sdfChannelPathSources, out sdfChannelPathDestinations); // Get all of the channel mappings.
List<string> channelMappingsToRemove = newList<string>(); // Initialize an empty List to store the channel mappings to remove.



// For any mappings that have an empty source, store the corresponding Destination Path.
for (int i = 0; i <= sdfChannelPathSources.Length - 1; i++)
{

if (sdfChannelPathSources[i] == string.Empty)
{

     channelMappingsToRemove.Add(sdfChannelPathDestinations[i]);

}

}



string[] channelMappingsToRemoveArray = channelMappingsToRemove.ToArray(); // Convert the List of mappings to delete to an array.
exampleSDF.Root.DeleteChannelMappings(channelMappingsToRemoveArray); // Delete the broken mappings using the DeleteChanelMappings method.



// Save the system definition file again and handle any errors.
string error;
bool saveError = exampleSDF.SaveSystemDefinitionFile(exampleSDFPath, out error);



if (saveError == true)
{

Console.WriteLine("\n\nSystem Definition File saved successfully");

}

else
{

Console.WriteLine("\n\nThere was an error saving the System Definition" + error);

}

}