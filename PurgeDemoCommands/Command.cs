using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Ganss.Text;
using MoreLinq;
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
        private AhoCorasick _ahoCorasick;
        private int _commandCount;

        public IList<string> Filenames { get; set; }
        public string NewFilePattern { get; set; }
        public bool Overwrite { get; set; }
        public IFilter Filter { get; set; }

        public void SetCommands(string[] value)
        {
            _commandCount = value.Length;

            _ahoCorasick = new AhoCorasick(value);
        }

        public async Task<IEnumerable<Result>> ExecuteThrottled()
        {
            GuardArguments();

            TransformBlock<string, Result> purges = new TransformBlock<string, Result>(file => Purge(file), new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = Environment.ProcessorCount });
            BufferBlock<Result> buffer = new BufferBlock<Result>();
            purges.LinkTo(buffer);

            foreach (string filename in Filenames)
            {
                purges.Post(filename);
            }

            purges.Complete();
            await purges.Completion;

            IList<Result> results;
            if (buffer.TryReceiveAll(out results))
                return results;

            throw new ExecuteException();
        }

        public IEnumerable<Task<Result>> Execute()
        {
            GuardArguments();

            return Filenames.Select(Purge);
        }

        private void GuardArguments()
        {
            if (Filenames == null)
                throw new ArgumentNullException(nameof(Filenames));
            if (NewFilePattern == null)
                throw new ArgumentNullException(nameof(NewFilePattern));
            if (Filenames.Count == 0)
                throw new ArgumentException("no file specified", nameof(NewFilePattern));
        }

        private async Task<Result> Purge(string filename)
        {
            try
            {
                Log.Information("purging {Filename}", filename);

                return await ReplaceCommandsWithTempFile(filename);
            }
            catch (Exception e)
            {
                throw new PurgeException(filename, e);
            }
        }

        private async Task<Result> ReplaceCommandsWithTempFile(string filename)
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


                await ReplaceCommandsIn(tempFilename);

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

        private async Task ReplaceCommandsIn(string filename)
        {
            Log.Debug("replacing {CommandCount} commands using {TempFilename}", _commandCount, filename);
            
            string content = File.ReadAllText(filename, Encoding.ASCII);
            var matches = Match(content).ToArray();
            Log.Debug("found {CountOccurrences} occurrences", matches.Length);

            await ReplaceMatches(filename, matches);
            Log.Debug("{CountOccurrences} occurrences in {TempFilename} replaces", matches.Length, filename);
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

                    Log.Verbose("replacing {ReplacesByteCount} Bytes for command {ReplacedCommand} at index {ReplacedIndex}", bytesTillNull, match.Word, match.Index);
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