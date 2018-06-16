# PurgeDemoCommands 0.2.1.0 #
![Build Status TFS](https://icebaer.visualstudio.com/_apis/public/build/definitions/ac1a498c-b249-4218-bfb0-f73b6a210fd7/6/badge)

Purges all console commands from demos. Optionally inserts new commands.
## Usage ##
``` 
PurgeDemoCommands.exe awesome.dem other.dem
PurgeDemoCommands.exe "C:\path to demos\awesome.dem"
PurgeDemoCommands.exe "C:\path to demos"
PurgeDemoCommands.exe awesome.dem -n {0}_clean.dem
PurgeDemoCommands.exe awesome.dem --skipTest
PurgeDemoCommands.exe awesome.dem -o -s -f
```
## Options ##

short name | name | default | description
-------------|--------|---------|-------------
n |name | purged\{0}.dem | Defines the name pattern of the generated file ({0} will be replaced by the original filename).
f | force | False | Closes the program after finishing (instead of waiting for a 'enter')
s | skipTest | False | Skips test if purged file can be parsed again.
o | overwrite | False | Overwrites existing (purged) files.
p | successfullPurges | False | Shows a summary after purgeing.
g | skipGotoTick | False | Skips looking for a demo_gototick in the filename.
t | tickmarker | @ | Defines marker in the filename (used to look for demo_gototick ).
a | pauseGotoTick | False | Pauses the demo after injecting demo_gototick from the filename.
&nbsp; | helpinjection | False | Display help screen for injections.
h | help | False | Display this help screen.

## Console command injection ##
### Option 1 ###
If you simply want to skip to a certain tick at the beginning of the demo, rename your demo so it ends in '@<span></span>tick' (e.i. awesome@<span></span>200.dem will skip to tick 200).
using option --skipGotoTick skips this
using option --tickmarker defines which text will be searched in the filename (Default: '@')
using option --pauseGotoTick skips to the detected tick and pauses the demo (instead of starting immediately

### Option 2 ###
If you want to insert advanced commands at specific ticks, create a textfile next to the demo-file with the same name appending '.inj' (awesome.dem.inj).
This file should contain the ticks and commands that should be injected

The following example will
1. go to tick 200
2. pause the demo and slow down the playback to half speed at tick 500
```
0 demo_gototick 200
500 demo_pause; demo_timescale 0.5
```
