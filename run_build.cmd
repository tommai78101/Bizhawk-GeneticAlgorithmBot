cd BizHawk
if not exist config_bak.ini (
	cp config.ini config_bak.ini
) else (
	cp config_bak.ini config.ini
)
.\EmuHawk.exe --open-ext-tool-dll=GeneticAlgorithmBot