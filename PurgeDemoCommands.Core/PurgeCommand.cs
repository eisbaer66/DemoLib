using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PurgeDemoCommands.Core.DemoEditActions;
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

                CommandPositions positions = await Parser.ReadDemo(FileName);
                Log.InfoFormat("found {CommandPositionCount} commands in {Filename}", positions.Count, FileName);
                IList<IDemoEditAction> replacments = CommandInjection.PlanReplacements(positions).ToList(); ;
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

        private async Task<Result> ReplaceCommandsWithTempFile(IList<IDemoEditAction> positions)
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

                Log.InfoFormat("writing purged content to {NewFilename}", result.NewFilepath);
                string newDirectoryName = Path.GetDirectoryName(result.NewFilepath);
                Directory.CreateDirectory(newDirectoryName);
                File.Copy(tempFilename, result.NewFilepath, true);
            }
            return result;
        }

        private async Task ReplaceMatches(string filename, IEnumerable<IDemoEditAction> positions)
        {
            using (var readStream = File.OpenRead(FileName))
            using (var writeStream = File.OpenWrite(filename))
            {
                foreach (IDemoEditAction position in positions)
                {
                    await position.Execute(readStream, writeStream);
                }
            }
        }
    }
}