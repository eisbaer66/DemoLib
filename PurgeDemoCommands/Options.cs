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

        [Option('n', "name", DefaultValue = "purged\\{0}.dem", HelpText = "Defines the name pattern of the generated file ({0} will be replaced by the original filename).")]
        public string NewFilePattern { get; set; }

        [Option('f', "force", DefaultValue = false, HelpText = "Closes the program after finishing (instead of waiting for a 'enter')")]
        public bool Force { get; set; }

        [Option('s', "skipTest", DefaultValue = false, HelpText = "Skips test if purged file can be parsed again.")]
        public bool SkipTest { get; set; }

        [Option('o', "overwrite", DefaultValue = false, HelpText = "Overwrites existing (purged) files.")]
        public bool Overwrite { get; set; }

        [Option('p', "successfullPurges", DefaultValue = false, HelpText = "Shows a summary after purgeing.")]
        public bool ShowSummary { get; set; }

        [Option('g', "skipGotoTick", DefaultValue = false, HelpText = "Skips looking for a demo_gototick in the filename.")]
        public bool SkipSearchForImplicitGotoTick { get; set; }

        [Option('t', "tickmarker", DefaultValue = "@", HelpText = "Defines marker in the filename (used to look for demo_gototick ).")]
        public string TickMarkerInFilename { get; set; }

        [Option('a', "pauseGotoTick", DefaultValue = false, HelpText = "Pauses the demo after injecting demo_gototick from the filename.")]
        public bool PauseOnImplicitGotoTick { get; set; }

        [Option("helpinjection", DefaultValue = false, HelpText = "Display help screen for injections.")]
        public bool HelpInjection { get; set; }

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
            help.AddPreOptionsLine("       PurgeDemoCommands.exe awesome.dem -o -s -f");
            help.AddPreOptionsLine(string.Empty);
            help.AddPreOptionsLine("For additional info on injecting console commands type");
            help.AddPreOptionsLine("    PurgeDemoCommands.exe --helpinjection");
            help.AddPreOptionsLine(string.Empty);
            help.AddPreOptionsLine("Options:");
            help.AddOptions(this);
            return help;
        }
    }
}