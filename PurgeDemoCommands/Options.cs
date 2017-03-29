using System;
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

        [Option('n', "name", DefaultValue = "{0}_purged.dem", HelpText = "name pattern of generated file")]
        public string NewFilePattern { get; set; }

        [Option('w', "whitelist", HelpText = "path to file containing whitelist for DemoCommands", MutuallyExclusiveSet = "filter")]
        public string WhitelistPath { get; set; }

        [Option('b', "blacklist", HelpText = "path to file containing blacklist for DemoCommands", MutuallyExclusiveSet = "filter")]
        public string BlacklistPath { get; set; }

        [Option('o', "overwrite", DefaultValue = false, HelpText = "overwrites existing (purged) files")]
        public bool Overwrite { get; set; }

        [Option('p', "successfullPurges", DefaultValue = false, HelpText = "shows a summary after purgeing")]
        public bool ShowSummary { get; set; }

        [Option('c', "commandList", DefaultValue = "comandlist.txt", HelpText = "filename of comandlist")]
        public string CommandList { get; set; }

        [Option('u', "updateCommandList", DefaultValue = false, HelpText = "updates list of commands")]
        public bool UpdateComandList { get; set; }

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
            help.AddPreOptionsLine("       PurgeDemoCommands.exe \"C:\\path\\to\\demos\\awesome.dem\"");
            help.AddPreOptionsLine("       PurgeDemoCommands.exe \"C:\\path\\to\\demos\"");
            help.AddPreOptionsLine("       PurgeDemoCommands.exe awesome.dem -s _clean");
            help.AddPreOptionsLine(string.Empty);
            help.AddPreOptionsLine("Options:");
            help.AddOptions(this);
            return help;
        }
    }
}