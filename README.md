# PurgeDemoCommands
purges all console commands from demo files

```
PurgeDemoCommands 0.1.0.39147

Usage: PurgeDemoCommands.exe awesome.dem other.dem
       PurgeDemoCommands.exe "C:\path\to\demos\awesome.dem"
       PurgeDemoCommands.exe "C:\path\to\demos"
       PurgeDemoCommands.exe awesome.dem -s _clean
       PurgeDemoCommands.exe awesome.dem -o

Options:

  -s, --suffix               (Default: _purged) suffix of generated file

  -t, --skipTest             (Default: False) skips test if purged file can be parsed again

  -o, --overwrite            (Default: False) overwrites existing (purged) files

  -p, --successfullPurges    (Default: False) shows a summary after purgeing

  --help                     Display this help screen.
```
