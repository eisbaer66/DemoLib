using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using CommandLine;
using Serilog;

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
                command.Execute();
            }
            catch (AggregateException exception)
            {
                //handle FileAlreadyExistsException
                var fileAlreadyExistsExceptions = exception.InnerExceptions.OfType<FileAlreadyExistsException>();
                var filenames = fileAlreadyExistsExceptions.Select(e => e.Filename).ToArray();
                _logger.Warning("following files were not writen, because they already exist - specify '-o' to overwrite existing files {ExistingFiles}", filenames);

                //log and throw remaining exceptions
                var exceptions = exception.InnerExceptions.Where(e => !(e is FileAlreadyExistsException)).ToArray();
                if (exceptions.Length > 0)
                {
                    Exception aggregateException = new AggregateException(exceptions);
                    _logger.Fatal(aggregateException, "fatal error");
                    throw aggregateException;
                }
            }
            catch (Exception e)
            {
                _logger.Fatal(e, "fatal error");
                throw;
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
                .Enrich.FromLogContext()
                .Enrich.WithThreadId()
                .CreateLogger();

            _logger = Log.Logger.ForContext<Program>();
        }
    }
}
