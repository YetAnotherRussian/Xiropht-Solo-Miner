# Xiropht-Solo-Miner
<h2>Official Xiropht Solo Miner.</h2>

**In production, we suggest to compile in Release Mode for disable log files and debug mode.**

<h2>Windows:</h2>

-> This miner require Netframework 4.6.1 minimum and the Xiropht Connector All library: https://github.com/XIROPHT/Xiropht-Connector-All

<h2>Linux:</h2>

-> This miner require Mono for be executed or compiled into a binary file follow those instructions:

~~~text
- apt-get install mono-complete

- mono Xiropht-Solo-Miner.exe
~~~

-> You can also make your own linux binary for don't have to install mono:

~~~text
- mkbundle Xiropht-Solo-Miner.exe -o Xiropht-Solo-Miner Xiropht-Connector-All.dll --deps -z --static

- ./Xiropht-Solo-Miner
~~~

The config.ini file of the miner is initialized after the first running.

This release as been compiled into Debug Mode, because we are currently in private test.

Hashrate test [Updated 13/02/2019]:

-> Raspberry PI 3 (On Raspberian OS): 1238C/s | 1157H/s | Accurate Rate 93.46%

-> Ryzen 7 2700x No OC (On Windows 10): 4521 C/s | 4187 H/s | Accurate Rate 92,61%

-> Celeron G3930 (On Windows 10): 2150C/s | 1890H/s | Accurate Rate 91,25%

<h3>Help:</h3>

For more informations about how work the Xiropht Mining System, please check out the Wiki: https://github.com/XIROPHT/Xiropht-Solo-Miner/wiki

**Developers:**

- Xiropht (Sam Segura)
