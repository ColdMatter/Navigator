<?xml version='1.0' encoding='UTF-8'?>
<Project Type="Project" LVVersion="16008000">
	<Item Name="My Computer" Type="My Computer">
		<Property Name="server.app.propertiesEnabled" Type="Bool">true</Property>
		<Property Name="server.control.propertiesEnabled" Type="Bool">true</Property>
		<Property Name="server.tcp.enabled" Type="Bool">false</Property>
		<Property Name="server.tcp.port" Type="Int">0</Property>
		<Property Name="server.tcp.serviceName" Type="Str">My Computer/VI Server</Property>
		<Property Name="server.tcp.serviceName.default" Type="Str">My Computer/VI Server</Property>
		<Property Name="server.vi.callsEnabled" Type="Bool">true</Property>
		<Property Name="server.vi.propertiesEnabled" Type="Bool">true</Property>
		<Property Name="specify.custom.address" Type="Bool">false</Property>
		<Item Name="DDSSequence.vi" Type="VI" URL="../DDSSequence.vi"/>
		<Item Name="Dependencies" Type="Dependencies">
			<Item Name="vi.lib" Type="Folder">
				<Item Name="VISA Configure Serial Port" Type="VI" URL="/&lt;vilib&gt;/Instr/_visa.llb/VISA Configure Serial Port"/>
				<Item Name="VISA Configure Serial Port (Instr).vi" Type="VI" URL="/&lt;vilib&gt;/Instr/_visa.llb/VISA Configure Serial Port (Instr).vi"/>
				<Item Name="VISA Configure Serial Port (Serial Instr).vi" Type="VI" URL="/&lt;vilib&gt;/Instr/_visa.llb/VISA Configure Serial Port (Serial Instr).vi"/>
			</Item>
			<Item Name="DDSSequence_Build.vi" Type="VI" URL="../DDSSequence_Build.vi"/>
			<Item Name="FlexDDS-NG_AD9910_FormatSTP.vi" Type="VI" URL="../FlexDDS-NG_AD9910_FormatSTP.vi"/>
			<Item Name="FlexDDS-NG_DCP_Channels2String.vi" Type="VI" URL="../FlexDDS-NG_DCP_Channels2String.vi"/>
			<Item Name="FlexDDS-NG_DCP_Command.vi" Type="VI" URL="../FlexDDS-NG_DCP_Command.vi"/>
			<Item Name="FlexDDS-NG_DCP_Command_bool.vi" Type="VI" URL="../FlexDDS-NG_DCP_Command_bool.vi"/>
			<Item Name="FlexDDS-NG_DCP_Reset.vi" Type="VI" URL="../FlexDDS-NG_DCP_Reset.vi"/>
			<Item Name="FlexDDS-NG_DCP_StartStop.vi" Type="VI" URL="../FlexDDS-NG_DCP_StartStop.vi"/>
			<Item Name="FlexDDS-NG_DCP_Update.vi" Type="VI" URL="../FlexDDS-NG_DCP_Update.vi"/>
			<Item Name="FlexDDS-NG_DCP_Wait_Event.vi" Type="VI" URL="../FlexDDS-NG_DCP_Wait_Event.vi"/>
			<Item Name="FlexDDS-NG_DCP_WriteCFR2_C.vi" Type="VI" URL="../FlexDDS-NG_DCP_WriteCFR2_C.vi"/>
			<Item Name="FlexDDS-NG_DCP_WriteRegAD9910.vi" Type="VI" URL="../FlexDDS-NG_DCP_WriteRegAD9910.vi"/>
			<Item Name="FlexDDS-NG_DCP_WriteSTP.vi" Type="VI" URL="../FlexDDS-NG_DCP_WriteSTP.vi"/>
			<Item Name="FlexDDS-NG_USB_VISA_Open.vi" Type="VI" URL="../FlexDDS-NG_USB_VISA_Open.vi"/>
		</Item>
		<Item Name="Build Specifications" Type="Build"/>
	</Item>
</Project>
