# PurgeDemoCommands 
![Build Status TFS](https://icebaer.visualstudio.com/_apis/public/build/definitions/ac1a498c-b249-4218-bfb0-f73b6a210fd7/6/badge)

purges all console commands from demo files

```
PurgeDemoCommands 0.1.3.0
 
Usage: PurgeDemoCommands.exe awesome.dem other.dem
       PurgeDemoCommands.exe "C:\path to demos\awesome.dem"
       PurgeDemoCommands.exe "C:\path to demos"
       PurgeDemoCommands.exe awesome.dem -n {0}_clean.dem
       PurgeDemoCommands.exe awesome.dem -o -s -u

Options:

  -n, --name                 (Default: purged\{0}.dem) name pattern of 
                             generated file

  -w, --whitelist            path to file containing whitelist for DemoCommands

  -b, --blacklist            path to file containing blacklist for DemoCommands

  -s, --skipTest             (Default: False) skips test if purged file can be 
                             parsed again

  -o, --overwrite            (Default: False) overwrites existing (purged) 
                             files

  -p, --successfullPurges    (Default: False) shows a summary after purgeing

  --help                     Display this help screen.
```
