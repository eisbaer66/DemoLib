using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DemoLib;
using DemoLib.Commands;
using PurgeDemoCommands.Core;
using PurgeDemoCommands.DemoLib.Logging;

namespace PurgeDemoCommands.DemoLib
{
    /// <summary>
    /// replaces all commands found by DemoReader in the specified files
    /// preserves all bytes, but the commands get replaced with \0's
    /// </summary>
    public class PazerCommand : IPurgeCommand
    {
        private static readonly ILog Log = LogProvider.GetCurrentClassLogger();
        private readonly IThrottler _throttle;

        public IList<string> Filenames { get; set; }
        public string NewFilePattern { get; set; }
        public bool SkipTest { get; set; }
        public bool Overwrite { get; set; }
        public IFilter Filter { get; set; }

        public PazerCommand(IThrottler throttle)
        {
            _throttle = throttle;
        }

        public void SetCommands(string[] value)
        {
        }

        public async Task<IEnumerable<Result>> ExecuteThrottled()
        {
            Result result = GuardArguments();
            if (result != null)
                return new[] { result };

            return await _throttle.Throttle(Purge, Filenames);
        }

        public IEnumerable<Task<Result>> Execute()
        {
            Result result = GuardArguments();
            if (result != null)
                return new[] { Task.FromResult(result) };

            return Filenames.Select(Purge);
        }

        private Result GuardArguments()
        {
            if (Filenames == null)
                return new Result("unknown") { ErrorText = "no files specified" };
            if (NewFilePattern == null)
                return new Result("unknown") { ErrorText = "no filepattern for new files specified" };
            if (Filenames.Count == 0)
                return new Result("unknown") { ErrorText = "no files specified" };

            return null;
        }

        private async Task<Result> Purge(string filename)
        {
            try
            {
                Log.InfoFormat("purging {Filename}", filename);

                DemoReader demo = ParseDemo(filename);

                return await ReplaceCommandsWithTempFile(filename, demo);
            }
            catch (Exception e)
            {
                throw new PurgeException(filename, e);
            }
        }

        private async Task<Result> ReplaceCommandsWithTempFile(string filename, DemoReader demo)
        {
            Result result = new Result(filename);
            
            string newFilename = string.Format(NewFilePattern, Path.GetFileNameWithoutExtension(filename));
            result.NewFilepath = Path.Combine(Path.GetDirectoryName(filename), newFilename);

            bool overwriting = false;
            if (File.Exists(result.NewFilepath))
            {
                if (Overwrite)
                    overwriting = true;
                else
                {
                    result.Warning |= Warning.FileAlreadyExists;
                    return result;
                }
            }

            using (TempFileCollection tempFileCollection = new TempFileCollection())
            {
                string tempFilename = tempFileCollection.AddExtension("dem");
                File.Copy(filename, tempFilename);


                await ReplaceCommandsIn(demo, tempFilename);
                if (!SkipTest)
                    EnsureDemoIsReadable(tempFilename);
                
                if (overwriting)
                {
                    Log.DebugFormat("deleting {NewFilename} to overwrite", result.NewFilepath);
                    File.Delete(result.NewFilepath);
                }

                Log.DebugFormat("writing purged content to {NewFilename}", result.NewFilepath);
                string newDirectoryName = Path.GetDirectoryName(result.NewFilepath);
                Directory.CreateDirectory(newDirectoryName);
                File.Copy(tempFilename, result.NewFilepath, true);
            }
            return result;
        }

        private async Task ReplaceCommandsIn(DemoReader demo, string filename)
        {
            var commands = ExtractCommands(demo);
            Log.DebugFormat("replacing {CommandCount} commands using {TempFilename}", commands.Length, filename);

            Regex regex = CreateRegex(commands);
            var matches = Match(filename, regex);
            Log.DebugFormat("found {CountOccurrences} occurrences", matches.Length);

            await ReplaceMatches(filename, matches);
            Log.DebugFormat("{CountOccurrences} occurrences in {TempFilename} replaces", matches.Length, filename);
        }

        private static async Task ReplaceMatches(string filename, IEnumerable<Group> matches)
        {
            using (var stream = File.Open(filename, FileMode.Open, FileAccess.ReadWrite))
            {
                foreach (Group match in matches)
                {
                    long bytesToMove = match.Index - stream.Position;
                    if (bytesToMove < 0)
                        throw new ApplicationException("unexpected index while replacing commands");

                    stream.Seek(bytesToMove, SeekOrigin.Current);
                    byte[] array = Enumerable.Range(1, match.Length).Select(i => (byte) 0).ToArray();
                    await stream.WriteAsync(array, 0, match.Length);
                }
            }
        }

        private static Group[] Match(string filename, Regex regex)
        {
            string content = File.ReadAllText(filename, Encoding.ASCII);
            return regex.Matches(content)
                .Cast<Match>()
                .SelectMany(m => m.Groups.Cast<Group>().Skip(1).Where(g => g.Success))
                .OrderBy(m => m.Index)
                .ToArray();
        }

        private static Regex CreateRegex(string[] commands)
        {
            //  x04[\s\S]{8}(COMMAND)x00   starts with "a byte (value 4)" followed by "8 bytes (length of string)" followed by "the command" followed by "a byte (value 0)"
            string startToken = ((char) 4).ToString();
            IEnumerable<string> regexParts = commands.Select(
                s => string.Format(@"{0}[\s\S]{{8}}({1})", Regex.Escape(startToken), Regex.Escape(s + "\0")));
            return new Regex(string.Join("|", regexParts));
        }

        private string[] ExtractCommands(DemoReader demo)
        {
            return demo.Commands
                .OfType<DemoConsoleCommand>()
                .Select(c => c.Command)
                .Distinct()
                .Where(Filter.Match)
                .ToArray();
        }

        private static DemoReader ParseDemo(string filename)
        {
            Log.DebugFormat("reading demo from {Filename}", filename);

            return Parse(filename);
        }

        private void EnsureDemoIsReadable(string filename)
        {
            Log.DebugFormat("testing demo {Filename}", filename);

            Parse(filename);

            Log.DebugFormat("test successfull for demo {Filename}", filename);
        }

        private static DemoReader Parse(string filename)
        {
            using (var stream = File.Open(filename, FileMode.Open, FileAccess.Read))
            {
                return DemoReader.FromStream(stream);
            }
        }
    }
}