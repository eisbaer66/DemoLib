using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using Nito.AsyncEx;
using PurgeDemoCommands.Core;
using PurgeDemoCommands.Core.CommandInjections;
using PurgeDemoCommands.DemoLib;
using PurgeDemoCommands.Sprache;
using Serilog;
using Serilog.Formatting.Json;
using Result = PurgeDemoCommands.Core.Result;

namespace PurgeDemoCommands
{
    class Program
    {
        private static ILogger _logger;

        static void Main(string[] args)
        {
            SetupLogging();
            _logger.Verbose("started with parameters {Args}", args);

            Options options = ParseOptions(args);
            if (options == null)
                return;

            try
            {
                AsyncContext.Run(() => MainAsync(options));
            }
            catch (Exception e)
            {
                _logger.Fatal(e, "fatal error");
                throw;
            }
        }

        private static async void MainAsync(Options options)
        {
            IThrottledPurgeCommand command = SetupCommand(options);
            IEnumerable<Result> results = await command.Execute();
            LogResults(results.ToArray(), options);

            if (!options.Force)
            {
                await Console.Out.WriteLineAsync("press 'enter' to exit");
                await Console.In.ReadLineAsync();
            }
        }

        private static void LogResults(ICollection<Result> results, Options options)
        {
            var resultsErrors = results.Where(r => !string.IsNullOrEmpty(r.ErrorText)).ToArray();
            var existingFiles = results.Where(r => r.Warning.HasFlag(Warning.FileAlreadyExists)).Select(e => e.NewFilepath).ToArray();
            var resultsWithOtherWarnings = results.Where(r => r.Warning.HasFlag(~Warning.FileAlreadyExists)).ToArray();
            var successfullResults = results.Where(r => !r.Warning.HasFlag(~Warning.None) && string.IsNullOrEmpty(r.ErrorText)).ToArray();

            Console.WriteLine();
            _logger.Information(
                "finished parsing {ResultCount} files with \r\n\t{ErrorCount} errors, \r\n\t{WaringCount} warnings and \r\n\t{SuccessCount} successes",
                results.Count,
                resultsErrors.Length,
                existingFiles.Length + resultsWithOtherWarnings.Length,
                successfullResults.Length);

            foreach (Result result in resultsErrors)
            {
                _logger.Error("error purging demo {Filename}: {ErrorText}", result.Filename, result.ErrorText);
            }

            if (existingFiles.Length > 0)
                _logger.Warning("following files were not written, because they already exist - specify '-o' to overwrite existing files {ExistingFiles}", existingFiles);

            foreach (Result result in resultsWithOtherWarnings)
            {
                _logger.Warning("{Warning} for demo {Filename} -> {NewFilename}", result.Warning, result.Filename, result.NewFilepath);
            }

            if (!options.ShowSummary)
                return;

            foreach (Result result in successfullResults)
            {
                _logger.Information("purged demo {Filename} -> {NewFilename}", result.Filename, result.NewFilepath);
            }
        }

        private static IThrottledPurgeCommand SetupCommand(Options options)
        {
            IList<ITest> tests = GetTests(options.SkipTest);

            ThrottledFilesPurgeCommand command = new ThrottledFilesPurgeCommand(new CommandHelper())
            {
                Parser = new PazerParser(),
                Filenames = GetFiles(options),
                NewFilePattern = options.NewFilePattern,
                Overwrite = options.Overwrite,
                CommandInjectionFactory = new CommandInjectionFactory(new InjectionParser())
                {
                    TickMarker = options.TickMarkerInFilename,
                    SearchForOnImplicitGotoTick = !options.SkipSearchForImplicitGotoTick,
                    PauseOnImplicitGotoTick = options.PauseOnImplicitGotoTick,
                },
                Tests = tests,
            };

            return command;
        }

        private static List<string> GetFiles(Options options)
        {
            return options.Files
                .SelectMany(f => Directory.Exists(f) ? Directory.GetFiles(f, "*.dem") : new[] {f})
                .Where(f => f.EndsWith(".dem"))
                .Where(f => !f.EndsWith(options.NewFilePattern + Path.GetExtension(f)))
                .ToList();
        }

        private static string[] GetCommandsFromFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            if (!File.Exists(path))
                return new string[0];

            return File.ReadAllLines(path);
        }

        private static IList<ITest> GetTests(bool skipTest)
        {
            if (skipTest)
                return new List<ITest>();
            return new List<ITest> { new IsParsableByPazer() };
        }

        private static Options ParseOptions(string[] args)
        {
            Options options = new Options();

            Parser parser = new Parser(s =>
            {
                s.MutuallyExclusive = true;
            });
            
            if (!parser.ParseArguments(args, options))
            {
                if (args.Contains("-h") || args.Contains("--help"))
                {
                    _logger.Debug("showing help");
                    Console.Out.WriteLine(options.GetUsage());
                   return null;
                }

                _logger.Fatal("could not parse parameters");
                Console.Error.WriteLine(options.GetUsage());
                Environment.Exit(1);
            }

            if (options.HelpInjection)
            {
                _logger.Debug("showing injection help");

                Console.Out.WriteLine("Option 1:");
                Console.Out.WriteLine("if you simply want to skip to a certain tick at the beginning of the demo,");
                Console.Out.WriteLine("rename you demo so it ends in '@tick' (e.i. awesome@200.dem will skip to tick 200).");
                Console.Out.WriteLine("using option --skipGotoTick skips this");
                Console.Out.WriteLine("using option --tickmarker defines which text will be searched in the filename (Default: '@')");
                Console.Out.WriteLine("using option --pauseGotoTick skips to the detected tick and pauses the demo (instead of starting immediately)");
                Console.Out.WriteLine("Option 2:");
                Console.Out.WriteLine("if you want to insert advanced commands at specific ticks,");
                Console.Out.WriteLine("create a textfile next to the demo-file with the same name appending '.inj' (awesome.dem.inj).");
                Console.Out.WriteLine("this file should contain the ticks and commands that should be injected");
                Console.Out.WriteLine("the following example will ");
                Console.Out.WriteLine("1. go to tick 200");
                Console.Out.WriteLine("2. pause the demo and slow down the playback to half speed at tick 500");
                Console.Out.WriteLine("awesome.dem.inj:");
                Console.Out.WriteLine("0 demo_gototick 200");
                Console.Out.WriteLine("500 demo_pause; demo_timescale 0.5");
                
                return null;
            }

            if (options.Files.Count == 0)
            {
                _logger.Fatal("no files specified in parameters");
                Console.Error.WriteLine(options.GetUsage());
                Environment.Exit(2);
            }

            if (string.IsNullOrEmpty(options.NewFilePattern))
            {
                _logger.Fatal("name pattern has to be specified");
                Console.Error.WriteLine(options.GetUsage());
                Environment.Exit(3);
            }
            return options;
        }

        private static void SetupLogging()
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.AppSettings()
                .MinimumLevel.Verbose()
                .WriteTo.RollingFile(new JsonFormatter(), Environment.ExpandEnvironmentVariables("%TEMP%\\icebear\\Tf2PurgeDemo\\log-{Date}.json"))
                .Enrich.FromLogContext()
                .Enrich.WithThreadId()
                .CreateLogger();

            _logger = Log.Logger.ForContext<Program>();
        }
    }
}
