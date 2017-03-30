# PurgeDemoCommands 
[![Build Status travis](https://travis-ci.org/eisbaer66/PurgeDemoCommands.svg?branch=purge)](https://travis-ci.org/eisbaer66/PurgeDemoCommands)

![Build Status TFS](https://icebaer.visualstudio.com/_apis/public/build/definitions/ac1a498c-b249-4218-bfb0-f73b6a210fd7/6/badge)

purges all console commands from demo files

```
PurgeDemoCommands 0.1.2.0

Usage: PurgeDemoCommands.exe awesome.dem other.dem
       PurgeDemoCommands.exe "C:\path to demos\awesome.dem"
       PurgeDemoCommands.exe "C:\path to demos"
       PurgeDemoCommands.exe awesome.dem -n {0}_clean.dem
       PurgeDemoCommands.exe awesome.dem -o -s -u

Options:

  -n, --name                 (Default: purged\{0}.dem) name pattern of generated file

  -w, --whitelist            path to file containing whitelist for DemoCommands

  -b, --blacklist            path to file containing blacklist for DemoCommands

  -o, --overwrite            (Default: False) overwrites existing (purged) files

  -s, --successfullPurges    (Default: False) shows a summary after purgeing

  -c, --commandList          (Default: commandlist.txt) path to file containing all DemoCommands

  -u, --updateCommandList    (Default: False) updates list of commands

  --help                     Display this help screen.
```
