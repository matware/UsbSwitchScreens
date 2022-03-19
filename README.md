Do you want to automatically switch video input sources based on when a USB keyboard is connected, well then this is the (very specific) tool for you.
It was borne out of wanting to have your display follow your keyboard when using something like a USB sharing switch.
To use this tool, compile and run it. It will launch a tray app. Right click, select Show, then fiddle with the appsettings.json file to specify the monitor name, and input ids
Then, run it on the other computers that are connected to the same monitors, specify the inputs for that computer, then when the USB keyboard is switched, the monitor inputs will switch.
You can also have it switch off the monitors on shutdown (if you have a crappy monitor that doesn’t sleep properly and chews power the your PC off.
Other things of note:
* It does fun things with VESA commands that I haven’t seen done much before.
* It’s a console application that can listen to windows messages, which I haven’t seen done much before.
* It handles USB device connection events in C#, which I haven’t seen done much before (and is a fair PITA)
