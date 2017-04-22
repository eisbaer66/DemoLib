using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ganss.Text;
using MoreLinq;
using PurgeDemoCommands.AhoCorasick.Logging;
using PurgeDemoCommands.Core;

namespace PurgeDemoCommands.AhoCorasick
{
    /// <summary>
    /// replaces all commands found by DemoReader in the specified files
    /// preserves all bytes, but the commands get replaced with \0's
    /// </summary>
    internal class AhoCorasickCommand : IPurgeCommand
    {
        private static readonly ILog Log = LogProvider.GetCurrentClassLogger();
        private Ganss.Text.AhoCorasick _ahoCorasick;
        private int _commandCount;
        private readonly IThrottler _throttle;

        public AhoCorasickCommand(IThrottler throttle)
        {
            _throttle = throttle;
        }

        public IList<string> Filenames { get; set; }
        public string NewFilePattern { get; set; }
        public bool Overwrite { get; set; }
        public IFilter Filter { get; set; }

        public void SetCommands(string[] value)
        {
            _commandCount = value.Length;

            _ahoCorasick = new Ganss.Text.AhoCorasick(value);
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
                return new[] {Task.FromResult(result) };

            return Filenames.Select(Purge);
        }

        private Result GuardArguments()
        {
            if (Filenames == null)
                return new Result("unknown") {ErrorText = "no files specified"};
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

                return await ReplaceCommandsWithTempFile(filename);
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

        private async Task<Result> ReplaceCommandsWithTempFile(string filename)
        {
            Result result = new Result(filename);
            
            string newFilename = string.Format(NewFilePattern, Path.GetFileNameWithoutExtension(filename));
            result.NewFilepath = Path.Combine(Path.GetDirectoryName(filename), newFilename);
            string newDirectoryName = Path.GetDirectoryName(result.NewFilepath);

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


                await ReplaceCommandsIn(tempFilename);

                if (overwriting)
                {
                    Log.DebugFormat("deleting {NewFilename} to overwrite", result.NewFilepath);
                    File.Delete(result.NewFilepath);
                }

                Directory.CreateDirectory(newDirectoryName);

                Log.DebugFormat("writing purged content to {NewFilename}", result.NewFilepath);
                File.Copy(tempFilename, result.NewFilepath, true);
            }
            return result;
        }

        private async Task ReplaceCommandsIn(string filename)
        {
            Log.DebugFormat("replacing {CommandCount} commands using {TempFilename}", _commandCount, filename);
            
            string content = File.ReadAllText(filename, Encoding.ASCII);
            var matches = Match(content).ToArray();
            Log.DebugFormat("found {CountOccurrences} occurrences", matches.Length);

            await ReplaceMatches(filename, matches);
            Log.DebugFormat("{CountOccurrences} occurrences in {TempFilename} replaces", matches.Length, filename);
        }

        private static async Task ReplaceMatches(string filename, IEnumerable<WordMatch> matches)
        {
            using (var stream = File.Open(filename, FileMode.Open, FileAccess.ReadWrite))
            {
                foreach (WordMatch match in matches)
                {
                    MoveToPosition(stream, match.Index);

                    MoveToTextStart(stream);

                    var messageTypeMatches = MessageTypeMatches(stream);
                    if (!messageTypeMatches)
                        continue;

                    var expectedLength = await ReadExpectedLength(stream);
                    var bytesTillNull = await FindTextLength(stream, expectedLength);
                    if (bytesTillNull < 0)
                        continue;

                    Log.TraceFormat("replacing {ReplacesByteCount} Bytes for command {ReplacedCommand} at index {ReplacedIndex}", bytesTillNull, match.Word, match.Index);
                    await WriteNulls(stream, bytesTillNull);
                }
            }
        }

        private static async Task<long> ReadExpectedLength(FileStream stream)
        {
            byte[] buffer = new byte[8];
            await stream.ReadAsync(buffer, 0, 8);
            long expectedLength = BitConverter.ToInt64(buffer, 0);
            return expectedLength;
        }

        private static async Task WriteNulls(FileStream stream, long bytesTillNull)
        {
            byte[] array = Enumerable.Range(1, (int) bytesTillNull).Select(i => (byte) 0).ToArray();
            await stream.WriteAsync(array, 0, array.Length);
        }

        private static async Task<long> FindTextLength(FileStream stream, long expectedLength)
        {
            long textStartIndex = stream.Position;
            int length = 0;
            byte b = 1;
            while (b != 0)
            {
                if (length > expectedLength)
                    return -1;

                byte[] buffer = new byte[1];
                await stream.ReadAsync(buffer, 0, 1);
                b = buffer[0];
                length++;
            }
            long bytesTillNull = stream.Position - textStartIndex;
            if (bytesTillNull > int.MaxValue)
                throw new ArgumentOutOfRangeException("bytesTillNull");
            stream.Seek(-bytesTillNull, SeekOrigin.Current);
            return bytesTillNull;
        }

        private static void MoveToTextStart(FileStream stream)
        {
            stream.Seek(-1, SeekOrigin.Current);
            while (char.IsWhiteSpace((char) stream.ReadByte()))
            {
                stream.Seek(-2, SeekOrigin.Current);
            }
        }

        private static void MoveToPosition(FileStream stream, int index)
        {
            long bytesToMove = index - stream.Position;
            stream.Seek(bytesToMove, SeekOrigin.Current);
        }

        private static bool MessageTypeMatches(FileStream stream)
        {
            stream.Seek(-9, SeekOrigin.Current);
            int messageType = stream.ReadByte();
            bool messageTypeMatches = messageType == 4;
            return messageTypeMatches;
        }

        private IOrderedEnumerable<WordMatch> Match(string content)
        {
            return _ahoCorasick
                .Search(content)
                .DistinctBy(m => m.Index)
                .OrderBy(m => m.Index);
        }
    }
}