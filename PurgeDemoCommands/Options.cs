﻿using System;
using System.Collections.Generic;
using System.Reflection;
using CommandLine;
using CommandLine.Text;

namespace PurgeDemoCommands
{
    internal class Options
    {
        [ValueList(typeof(List<string>))]
        public IList<string> Files { get; set; }

        [Option('n', "name", DefaultValue = "purged\\{0}.dem", HelpText = "name pattern of generated file")]
        public string NewFilePattern { get; set; }

        [Option('w', "whitelist", HelpText = "path to file containing whitelist for DemoCommands", MutuallyExclusiveSet = "filter")]
        public string WhitelistPath { get; set; }

        [Option('b', "blacklist", HelpText = "path to file containing blacklist for DemoCommands", MutuallyExclusiveSet = "filter")]
        public string BlacklistPath { get; set; }

        [Option('s', "skipTest", DefaultValue = false, HelpText = "skips test if purged file can be parsed again")]
        public bool SkipTest { get; set; }

        [Option('o', "overwrite", DefaultValue = false, HelpText = "overwrites existing (purged) files")]
        public bool Overwrite { get; set; }

        [Option('p', "successfullPurges", DefaultValue = false, HelpText = "shows a summary after purgeing")]
        public bool ShowSummary { get; set; }

        [Option('g', "skipGotoTick", DefaultValue = false, HelpText = "skips looking for a demo_gototick in the filename")]
        public bool SkipSearchForImplicitGotoTick { get; set; }

        [Option('t', "tickmarker", DefaultValue = "@", HelpText = "marker used to look for demo_gototick in the filename")]
        public string TickMarkerInFilename { get; set; }

        [Option('a', "pauseGotoTick", DefaultValue = false, HelpText = "pauses the demo after injecting demo_gototick from the filename")]
        public bool PauseOnImplicitGotoTick { get; set; }

        [HelpOption(HelpText = "Display this help screen.")]
        public string GetUsage()
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            var help = new HelpText
            {
                Heading = new HeadingInfo("PurgeDemoCommands", version.ToString()),
                AdditionalNewLineAfterOption = true,
                AddDashesToOption = true
            };
            help.AddPreOptionsLine(" ");
            help.AddPreOptionsLine("Usage: PurgeDemoCommands.exe awesome.dem other.dem");
            help.AddPreOptionsLine("       PurgeDemoCommands.exe \"C:\\path to demos\\awesome.dem\"");
            help.AddPreOptionsLine("       PurgeDemoCommands.exe \"C:\\path to demos\"");
            help.AddPreOptionsLine("       PurgeDemoCommands.exe awesome.dem -n {0}_clean.dem");
            help.AddPreOptionsLine("       PurgeDemoCommands.exe awesome.dem -o -s -u");
            help.AddPreOptionsLine(string.Empty);
            help.AddPreOptionsLine("Options:");
            help.AddOptions(this);
            return help;
        }
    }
}