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
using Serilog;

namespace PurgeDemoCommands
{
    /// <summary>
    /// replaces all commands found by DemoReader in the specified files
    /// preserves all bytes, but the commands get replaced with \0's
    /// </summary>
    internal class Command
    {
        private static readonly ILogger Log = Serilog.Log.Logger.ForContext<Command>();

        public IList<string> Filenames { get; set; }
        public string Suffix { get; set; }
        public bool SkipTest { get; set; }
        public bool Overwrite { get; set; }

        public IEnumerable<Task<Result>> Execute()
        {
            if (Filenames == null)
                throw new ArgumentNullException(nameof(Filenames));
            if (Suffix == null)
                throw new ArgumentNullException(nameof(Suffix));
            if (Filenames.Count == 0)
                throw new ArgumentException("no file specified", nameof(Suffix));

            return Filenames.Select(Purge);
        }

        private async Task<Result> Purge(string filename)
        {
            try
            {
                Log.Information("purging {Filename}", filename);

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

            string newFilename = Path.GetFileNameWithoutExtension(filename) + Suffix + Path.GetExtension(filename);
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
                    Log.Debug("deleting {NewFilename} to overwrite", result.NewFilepath);
                    File.Delete(result.NewFilepath);
                }

                Log.Debug("writing purged content to {NewFilename}", result.NewFilepath);
                File.Copy(tempFilename, result.NewFilepath, true);
            }
            return result;
        }

        private static async Task ReplaceCommandsIn(DemoReader demo, string filename)
        {
            var commands = ExtractCommands(demo);
            Log.Debug("replacing {CommandCount} commands using {TempFilename}", commands.Length, filename);

            Regex regex = CreateRegex(commands);
            var matches = Match(filename, regex);
            Log.Debug("found {CountOccurrences} occurrences", matches.Length);

            await ReplaceMatches(filename, matches);
            Log.Debug("{CountOccurrences} occurrences in {TempFilename} replaces", matches.Length, filename);
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

        private static string[] ExtractCommands(DemoReader demo)
        {
            return demo.Commands
                .OfType<DemoConsoleCommand>()
                .Select(c => c.Command)
                .Distinct()
                .ToArray();
        }

        private static DemoReader ParseDemo(string filename)
        {
            Log.Debug("reading demo from {Filename}", filename);

            return Parse(filename);
        }

        private void EnsureDemoIsReadable(string filename)
        {
            Log.Debug("testing demo {Filename}", filename);

            Parse(filename);

            Log.Debug("test successfull for demo {Filename}", filename);
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