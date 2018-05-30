using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PurgeDemoCommands.Core.Logging;

namespace PurgeDemoCommands.Core
{
    /// <summary>
    /// replaces all commands found by DemoReader in the specified files
    /// preserves all bytes, but the commands get replaced with \0's
    /// </summary>
    public class PurgeCommand : IPurgeCommand
    {
        private static readonly ILog Log = LogProvider.GetCurrentClassLogger();
        private readonly IThrottler _throttle;

        public IList<string> Filenames { get; set; }
        public string NewFilePattern { get; set; }
        public bool Overwrite { get; set; }
        public IFilter Filter { get; set; }
        public IParser Parser { get; set; }
        public IInjectionCommandCollection StartInjection { get; set; }
        public IEnumerable<ITest> Tests { get; set; }


        public PurgeCommand(IThrottler throttle)
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

                IList<CommandPosition> demo = await Parser.ReadDemo(filename);
                Log.InfoFormat("found {CommandPositionCount} commands in {Filename}", demo.Count, filename);

                return await ReplaceCommandsWithTempFile(filename, demo);
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

        private async Task<Result> ReplaceCommandsWithTempFile(string filename, IList<CommandPosition> positions)
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


                await ReplaceMatches(tempFilename, positions);
                foreach (ITest test in Tests)
                {
                    test.Run(tempFilename);
                }

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

        private async Task ReplaceMatches(string filename, IEnumerable<CommandPosition> positions)
        {
            using (var stream = File.Open(filename, FileMode.Open, FileAccess.ReadWrite))
            {
                foreach (CommandPosition position in positions)
                {
                    MoveToPosition(stream, position.Index);

                    string command = StartInjection.GetCommand(position.NumberOfBytes);

                    if (string.IsNullOrEmpty(command))
                    {
                        Log.TraceFormat("replacing {CommandPosition} with null-bytes", position);
                        await WriteNulls(stream, position.NumberOfBytes);
                    }
                    else
                    {
                        Log.TraceFormat("replacing {CommandPosition} with {InjectedCommand}", position, command);
                        await Write(stream, command, position.NumberOfBytes);
                    }

                }
            }
        }

        private static void MoveToTextStart(FileStream stream)
        {
            stream.Seek(-1, SeekOrigin.Current);
            while (char.IsWhiteSpace((char)stream.ReadByte()))
            {
                stream.Seek(-2, SeekOrigin.Current);
            }
        }

        private static void MoveToPosition(FileStream stream, long index)
        {
            long bytesToMove = index - stream.Position;
            stream.Seek(bytesToMove, SeekOrigin.Current);
        }

        private static async Task WriteNulls(FileStream stream, long bytesTillNull)
        {
            byte[] array = Enumerable.Range(1, (int)bytesTillNull).Select(i => (byte)0).ToArray();
            await stream.WriteAsync(array, 0, array.Length);
        }

        private static async Task Write(FileStream stream, string command, long numberOfBytes)
        {
            byte[] array = Encoding.ASCII.GetBytes(command);
            Debug.Assert(array.Length <= numberOfBytes);

            await stream.WriteAsync(array, 0, array.Length);
            await WriteNulls(stream, numberOfBytes - array.Length);
        }
    }
}