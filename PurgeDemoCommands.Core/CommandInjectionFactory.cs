using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PurgeDemoCommands.Core.Logging;
using PurgeDemoCommands.Sprache;

namespace PurgeDemoCommands.Core
{
    public interface ICommandInjectionFactory
    {
        ICommandInjection CreateInjection(string filename);
    }

    public class CommandInjectionFactory : ICommandInjectionFactory
    {
        private static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        public string TickMarker { get; set; }
        public IConfilctResolver ConflictResolver { get; set; }
        public IInjectionParser InjectionParser { get; set; }

        public CommandInjectionFactory(IInjectionParser injectionParser)
        {
            InjectionParser = injectionParser ?? throw new ArgumentNullException(nameof(injectionParser));
            TickMarker = "@";
            ConflictResolver = new AbortingConfilctResolver();
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

            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
            int index = fileNameWithoutExtension.IndexOf(TickMarker);
            if (index >= 0)
                return CreateInjectionFromTick(fileNameWithoutExtension, index);

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
                    ITickIterator iterator = t.Dir == TickIterationDirection.Forward ? (ITickIterator) new ForwardTickIterator() : new BackwardTickIterator();
                    return new TickInjection(t.Tick, t.Commands, ConflictResolver, iterator);
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

            Log.InfoFormat("found {TickMarker} in {Filename}. injecting demo_gototick {Tick}", TickMarker, fileNameWithoutExtension, tick);
            return new CommandInjection(new []{new TickInjection(0, new []{ "demo_gototick "+tick }, ConflictResolver, new ForwardTickIterator())});
        }
    }

    internal class CommandInjectionInitializiationException : Exception
    {
        public CommandInjectionInitializiationException()
        {
            
        }

        public CommandInjectionInitializiationException(string msg)
        :base(msg)
        {
            
        }
    }
}