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

        public string FileName { get; set; }
        public string NewFilePattern { get; set; }
        public bool Overwrite { get; set; }
        public IParser Parser { get; set; }
        public ICommandInjection CommandInjection { get; set; }
        public IEnumerable<ITest> Tests { get; set; }

        public async Task<Result> Purge()
        {
            try
            {
                Log.InfoFormat("purging {Filename}", FileName);

                IList<CommandPosition> positions = await Parser.ReadDemo(FileName);
                Log.InfoFormat("found {CommandPositionCount} commands in {Filename}", positions.Count, FileName);
                IList<ReplacementPosition> replacments = PlanInjection(positions);
                return await ReplaceCommandsWithTempFile(replacments);
            }
            catch (Exception e)
            {
                Log.ErrorException("error while purging {Filename}", e, FileName);
                return new Result(FileName)
                {
                    ErrorText = e.Message,
                };
            }
        }

        private IList<ReplacementPosition> PlanInjection(IList<CommandPosition> positions)
        {
            try
            {
                return CommandInjection.PlanReplacements(positions);
            }
            catch (Exception e)
            {
                Log.ErrorException("error while planing injection. using NullInjection", e);

                return new CommandInjection(new List<ITickInjection>()).PlanReplacements(positions);
            }
        }

        private async Task<Result> ReplaceCommandsWithTempFile(IList<ReplacementPosition> positions)
        {
            Result result = new Result(FileName);

            string newFilename = string.Format(NewFilePattern, Path.GetFileNameWithoutExtension(FileName));
            result.NewFilepath = Path.Combine(Path.GetDirectoryName(FileName), newFilename);

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
                File.Copy(FileName, tempFilename);


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

        private async Task ReplaceMatches(string filename, IEnumerable<ReplacementPosition> positions)
        {
            using (var stream = File.Open(filename, FileMode.Open, FileAccess.ReadWrite))
            {
                foreach (ReplacementPosition position in positions)
                {
                    MoveToPosition(stream, position.Index);

                    Log.TraceFormat("replacing {CommandPosition} with {InjectedCommand}", position.Index, Encoding.ASCII.GetString(position.Bytes));
                    await Write(stream, position.Bytes);
                }
            }
        }

        private static void MoveToPosition(FileStream stream, long index)
        {
            long bytesToMove = index - stream.Position;
            stream.Seek(bytesToMove, SeekOrigin.Current);
        }

        private async Task Write(FileStream stream, byte[] bytes)
        {
            await stream.WriteAsync(bytes, 0, bytes.Length);
        }
    }
}