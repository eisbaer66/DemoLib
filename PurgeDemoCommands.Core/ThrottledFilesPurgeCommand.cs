using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PurgeDemoCommands.Core.CommandInjections;
using PurgeDemoCommands.Core.Logging;

namespace PurgeDemoCommands.Core
{
    /// <summary>
    /// replaces all commands found by DemoReader in the specified files
    /// preserves all bytes, but the commands get replaced with \0's
    /// </summary>
    public class ThrottledFilesPurgeCommand : IThrottledPurgeCommand
    {
        private static readonly ILog Log = LogProvider.GetCurrentClassLogger();
        private readonly IThrottler _throttle;

        public IList<string> Filenames { get; set; }
        public string NewFilePattern { get; set; }
        public bool Overwrite { get; set; }
        public IFilter Filter { get; set; }
        public IParser Parser { get; set; }
        public ICommandInjectionFactory CommandInjectionFactory { get; set; }
        public IEnumerable<ITest> Tests { get; set; }


        public ThrottledFilesPurgeCommand(IThrottler throttle)
        {
            _throttle = throttle;
        }

        public void SetCommands(string[] value)
        {
        }

        public async Task<IEnumerable<Result>> Execute()
        {
            Result result = GuardArguments();
            if (result != null)
                return new[] { result };

            return await _throttle.Throttle(Purge, Filenames);
        }

        private async Task<Result> Purge(string filename)
        {
            try
            { 
                ICommandInjection injection = CommandInjectionFactory.CreateInjection(filename);
                PurgeCommand command = new PurgeCommand
                {
                    FileName = filename,
                    CommandInjection = injection,
                    NewFilePattern = NewFilePattern,
                    Overwrite = Overwrite,
                    Parser = Parser,
                    Tests = Tests,
                };

                return await command.Purge();
            }
            catch (Exception e)
            {
                Log.ErrorException("error while purging {Filename}", e, filename);
                return new Result(filename)
                {
                    ErrorText = e.Message,
                };
            }
        }

        private Result GuardArguments()
        {
            if (Filenames == null)
                return new Result("unknown") { ErrorText = "no files specified" };
            if (NewFilePattern == null)
                return new Result("unknown") { ErrorText = "no filepattern for new files specified" };
            if (Filenames.Count == 0)
                return new Result("unknown") { ErrorText = "no files specified" };
            if (CommandInjectionFactory == null)
                return new Result("unknown") { ErrorText = "no CommandInjectionFactory specified" };

            return null;
        }
    }
}