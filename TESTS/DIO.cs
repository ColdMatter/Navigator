static void Main(string[] args)
{

/* start of main */
string card_name = "PXI-6552";

bool id_query_status = false;

bool reset_instrument_status = false;

string output_channels = "0,1,2,3,10,11,12";

string input_channels = "13";

uint misc_counter=0;

uint write_data=0x00000000;

uint mask_value=0XFFFFFFFF;

niHSDIO sig_gen = niHSDIO.InitGenerationSession(card_name,id_query_status,reset_instrument_status,"");

/* create a generation session to the Digital IO card */

sig_gen.AssignStaticChannels(output_channels);

sig_gen.ConfigureDataVoltageLogicFamily(output_channels,InstrumentDriverInterop.Ivi.niHSDIOConstants._33vLogic);

misc_counter = 0;

while(misc_counter <= 10000)

{

sig_gen.WriteStaticU32(0x00000000,mask_value);

sig_gen.WriteStaticU32(0xFFFFFFFF,mask_value);

Console.Write(misc_counter+"\n");

misc_counter++;

}

} /* end of main */

