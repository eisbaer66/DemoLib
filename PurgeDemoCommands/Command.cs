using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        public void Execute()
        {
            if (Filenames == null)
                throw new ArgumentNullException(nameof(Filenames));
            if (Suffix == null)
                throw new ArgumentNullException(nameof(Suffix));
            if (Filenames.Count == 0)
                throw new ArgumentException("no file specified", nameof(Suffix));

            List<Exception> exceptions = new List<Exception>();
            foreach (string filename in Filenames)
            {
                try
                {
                    Purge(filename);
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);
        }

        private void Purge(string filename)
        {
            DemoReader demo = ParseDemo(filename);

            ReplaceCommandsWithTempFile(filename, demo);
        }

        private void ReplaceCommandsWithTempFile(string filename, DemoReader demo)
        {
            using (TempFileCollection tempFileCollection = new TempFileCollection())
            {
                string tempFilename = tempFileCollection.AddExtension("dem");
                File.Copy(filename, tempFilename);


                ReplaceCommandsIn(demo, tempFilename);
                if (!SkipTest)
                    EnsureDemoIsReadable(tempFilename);


                string newFilename = Path.GetFileNameWithoutExtension(filename) + Suffix + Path.GetExtension(filename);
                string newFilepath = Path.Combine(Path.GetDirectoryName(filename), newFilename);

                if (File.Exists(newFilepath))
                {
                    if (Overwrite)
                    {
                        Log.Debug("deleting {NewFilename} to overwrite", newFilepath);
                        File.Delete(newFilepath);
                    }
                    else
                        throw new FileAlreadyExistsException(newFilepath);
                }
                Log.Debug("writing purged content to {NewFilename}", newFilepath);
                File.Copy(tempFilename, newFilepath, true);
            }
        }

        private static void ReplaceCommandsIn(DemoReader demo, string filename)
        {
            var commands = ExtractCommands(demo);
            Log.Debug("replacing {CommandCount} commands using {TempFilename}", commands.Length, filename);

            Regex regex = CreateRegex(commands);
            var matches = Match(filename, regex);
            Log.Debug("found {CountOccurrences} occurrences", matches.Length);

            ReplaceMatches(filename, matches);
            Log.Debug("{CountOccurrences} occurrences in {TempFilename} replaces", matches.Length, filename);
        }

        private static void ReplaceMatches(string filename, IEnumerable<Group> matches)
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
                    stream.Write(array, 0, match.Length);
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