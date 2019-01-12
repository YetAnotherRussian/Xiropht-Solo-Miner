# Xiropht-Solo-Miner
Official Xiropht Solo Miner, this is the first stable version.

Windows:

-> This miner require Netframework 4.6 and the Xiropht Connector All library: https://github.com/XIROPHT/Xiropht-Connector-All

Linux:

-> This miner require Mono for be executed or compiled into a binary file follow those instructions:

- apt-get install mono-complete

- mono Xiropht-Solo-Miner.exe

-> You can also make your own linux binary for don't have to install mono:

- mkbundle Xiropht-Solo-Miner.exe -o Xiropht-Solo-Miner Xiropht-Connector-All.dll NCalc.dll Antlr3.Runtime.dll --deps -z --static

- ./Xiropht-Solo-Miner

The config.ini file of the miner is initialized after the first running.

This release as been compiled into Debug Mode, because we are currently in private test.

Hashrate test:

-> Raspberry PI 3 (On Raspberian OS): 1554C/s | 903H/s | Accurate Rate 58,11% Cache Enabled.

-> Ryzen 7 2700x No OC (On Windows 10): 4015C/s | 2590 H/s | Accurate Rate 64,51% Cache Enabled.

-> Celeron G3930 (On Windows 10): 2150C/s | 1200H/s | Accurate Rate 55,81% Cache Enabled.

For more informations about how work the Xiropht Mining System, please check out the Wiki: https://github.com/XIROPHT/Xiropht-Solo-Miner/wiki
