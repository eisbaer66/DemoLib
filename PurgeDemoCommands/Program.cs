using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Nito.AsyncEx;
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
            if (options.UpdateComandList || !File.Exists(options.CommandList))
            {
                UpdateComandListComand updateComandListComand = new UpdateComandListComand
                {
                    Path = options.CommandList,
                };

                await updateComandListComand.Execute();
            }

            Command command = SetupCommand(options);
            IEnumerable<Result> results = await command.ExecuteThrottled();
            LogResults(results.ToArray(), options);
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

        private static Command SetupCommand(Options options)
        {
            string[] whitelist = GetCommandsFromFile(options.WhitelistPath);
            string[] blacklist = GetCommandsFromFile(options.BlacklistPath);
            string[] commands = GetCommandsFromFile(options.CommandList);
            Command command = new Command
            {
                Filenames = GetFiles(options),
                NewFilePattern = options.NewFilePattern,
                Filter = Filter.From(whitelist, blacklist),
                Overwrite = options.Overwrite,
            };
            command.SetCommands(commands);

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
