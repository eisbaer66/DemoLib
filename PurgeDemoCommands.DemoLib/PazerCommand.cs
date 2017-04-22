using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                Log.ErrorException("error while purging {Filename}", e, filename);
                return new Result(filename)
                {
                    ErrorText = e.Message,
                };
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
            var indices = ExtractIndices(demo);
            Log.DebugFormat("replacing {CommandCount} commands using {TempFilename}", indices.Length, filename);

            await OverrideCommands(filename, indices);
            Log.DebugFormat("{CountOccurrences} occurrences in {TempFilename} replaces", indices.Length, filename);
        }

        private static async Task OverrideCommands(string filename, Tuple<long, long>[] indices)
        {
            using (var stream = File.Open(filename, FileMode.Open, FileAccess.ReadWrite))
            {
                foreach (Tuple<long, long> index in indices)
                {
                    long indexStart = index.Item1 + 4;
                    long indexEnd = index.Item2;

                    long bytesToMove = indexStart - stream.Position;
                    if (bytesToMove < 0)
                        throw new ApplicationException("unexpected index while replacing commands");

                    stream.Seek(bytesToMove, SeekOrigin.Current);
                    long length = indexEnd - indexStart;
                    
                    while (length > 0)
                    {
                        int l = length > int.MaxValue ? int.MaxValue : (int) length;
                        byte[] array = new byte[l];
                        for (int i = 0; i < l; i++)
                        {
                            array[i] = 0;
                        }

                        await stream.WriteAsync(array, 0, l);

                        length -= l;
                    }
                }
            }
        }

        private static Tuple<long, long>[] ExtractIndices(DemoReader demo)
        {
            var indices = demo.Commands
                .OfType<DemoConsoleCommand>()
                .Select(c => new Tuple<long, long>(c.IndexStart, c.IndexEnd))
                .OrderBy(t => t.Item1)
                .ToArray();
            return indices;
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