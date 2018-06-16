using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PurgeDemoCommands.Core.Logging;
using PurgeDemoCommands.Sprache;

namespace PurgeDemoCommands.Core.CommandInjections
{
    public interface ICommandInjectionFactory
    {
        ICommandInjection CreateInjection(string filename);
    }

    public class CommandInjectionFactory : ICommandInjectionFactory
    {
        private static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        public string TickMarker { get; set; }
        public bool SearchForOnImplicitGotoTick { get; set; }
        public bool PauseOnImplicitGotoTick { get; set; }
        public IInjectionParser InjectionParser { get; set; }

        public CommandInjectionFactory(IInjectionParser injectionParser)
        {
            InjectionParser = injectionParser ?? throw new ArgumentNullException(nameof(injectionParser));
            TickMarker = "@";
            SearchForOnImplicitGotoTick = true;
            PauseOnImplicitGotoTick = true;
        }

        public ICommandInjection CreateInjection(string filename)
        {
            string injectionFilename = filename + ".inj";
            if (File.Exists(injectionFilename))
            {
                ICommandInjection injectionFromFile = CreateInjectionFromFile(injectionFilename);
                if (injectionFromFile != null)
                    return injectionFromFile;
            }

            if (SearchForOnImplicitGotoTick)
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
                int index = fileNameWithoutExtension.IndexOf(TickMarker);
                if (index >= 0)
                    return CreateInjectionFromTick(fileNameWithoutExtension, index);
            }

            Log.TraceFormat("no injection found. using NullInjection");
            return new CommandInjection(new List<ITickInjection>());
        }

        private ICommandInjection CreateInjectionFromFile(string injectionFilename)
        {
            string text = File.ReadAllText(injectionFilename);

            Result<IEnumerable<TickConfigItem>> result = InjectionParser.ParseFrom(text);
            if (!result.Success)
            {
                Log.ErrorFormat("could not read {InjectionFilename}: {InjectionParserResult}", injectionFilename, result);
                return null;
            }
            IEnumerable<TickConfigItem> items = result.Items;

            var injs = items.Select(t =>
                {
                    return new TickInjection
                    {
                        Tick = t.Tick,
                        Commands = string.Join("; ", t.Commands)
                    };
                })
                .ToList();

            Log.InfoFormat("successfull loaded {TickConfigItemCount} CommandInjections from {InjectionFilename}", injs.Count, injectionFilename);
            return new CommandInjection(injs);
        }

        private ICommandInjection CreateInjectionFromTick(string fileNameWithoutExtension, int index)
        {
            Log.DebugFormat("reading injection from {Filename} at index {Index}", fileNameWithoutExtension, index);

            string tickRaw = fileNameWithoutExtension.Substring(index+1, fileNameWithoutExtension.Length -index-1);

            int tick;
            if (!int.TryParse(tickRaw, out tick))
            {
                Log.ErrorFormat("found {TickMarker} in {Filename} but could not read tick. using NullInjection", TickMarker, fileNameWithoutExtension);
                return new CommandInjection(new List<ITickInjection>());
            }

            string pause = PauseOnImplicitGotoTick ? "1" : "0";
            string command = "demo_gototick " + tick + " 0 " + pause;
            Log.InfoFormat("found {TickMarker} in {Filename}. injecting {Command}", TickMarker, fileNameWithoutExtension, command);
            return new CommandInjection(new[]
            {
                new TickInjection
                {
                    Tick = 0,
                    Commands = command
                }
            });
        }
    }
}