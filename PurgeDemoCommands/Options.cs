using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CommandLine;
using CommandLine.Text;

namespace PurgeDemoCommands
{
    internal class Options
    {
        [ValueList(typeof(List<string>))]
        public IList<string> Files { get; set; }

        [Option('s', "suffix", DefaultValue = "_purged", HelpText = "suffix of generated file")]
        public string Suffix { get; set; }

        [Option('t', "skipTest", DefaultValue = false, HelpText = "skips test if purged file can be parsed again")]
        public bool SkipTest { get; set; }

        [Option('o', "overwrite", DefaultValue = false, HelpText = "skips test if purged file can be parsed again")]
        public bool Overwrite { get; set; }

        [HelpOption(HelpText = "Display this help screen.")]
        public string GetUsage()
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            var help = new HelpText
            {
                Heading = new HeadingInfo("PurgeDemoCommands", version.ToString()),
                Copyright = new CopyrightInfo("icebear", 2017),
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