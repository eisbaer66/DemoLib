using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using CommandLine;
using Serilog;
using Serilog.Formatting.Json;

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

            try
            {
                Command command = SetupCommand(options);
                Task<Result>[] tasks = command.Execute().ToArray();
                Task.WaitAll(tasks);

                LogResults(tasks.Select(t => t.Result).ToArray(), options);
            }
            catch (Exception e)
            {
                _logger.Fatal(e, "fatal error");
                throw;
            }
        }

        private static void LogResults(ICollection<Result> results, Options options)
        {
            var existingFiles = results.Where(r => r.Warning.HasFlag(Warning.FileAlreadyExists)).Select(e => e.NewFilepath).ToArray();
            if (existingFiles.Length > 0)
                _logger.Warning("following files were not written, because they already exist - specify '-o' to overwrite existing files {ExistingFiles}", existingFiles);

            var resultsWithOtherWarnings = results.Where(r => r.Warning.HasFlag(~Warning.FileAlreadyExists)).ToArray();
            foreach (Result result in resultsWithOtherWarnings)
            {
                _logger.Warning("{Warning} for demo {Filename} -> {NewFilename}", result.Warning, result.Filename, result.NewFilepath);
            }

            if (!options.ShowSummary)
                return;

            var successfullResults = results.Where(r => !r.Warning.HasFlag(~Warning.None)).ToArray();
            foreach (Result result in successfullResults)
            {
                _logger.Information("purged demo {Filename} -> {NewFilename}", result.Filename, result.NewFilepath);
            }
        }

        private static Command SetupCommand(Options options)
        {
            var filenames = options.Files
                .SelectMany(f => Directory.Exists(f) ? Directory.GetFiles(f) : new[] {f})
                .Where(f => !f.EndsWith(options.Suffix + Path.GetExtension(f)))
                .ToList();
            Command command = new Command
            {
                Filenames = filenames,
                Suffix = options.Suffix,
                SkipTest = options.SkipTest,
                Overwrite = options.Overwrite,
            };
            return command;
        }

        private static Options ParseOptions(string[] args)
        {
            Options options = new Options();

            if (!Parser.Default.ParseArguments(args, options))
            {
                _logger.Fatal("could not parse parameters");
                Console.Error.WriteLine(options.GetUsage());
                Environment.Exit(1);
            }

            if (options.Files.Count == 0)
            {
                _logger.Fatal("no files specified in parameters");
                Console.Error.WriteLine(options.GetUsage());
                Environment.Exit(2);
            }

            if (string.IsNullOrEmpty(options.Suffix))
            {
                _logger.Fatal("suffix has to be specified");
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
