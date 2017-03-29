using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using Serilog;

namespace PurgeDemoCommands
{
    internal class UpdateComandListComand
    {
        private static readonly ILogger Log = Serilog.Log.Logger.ForContext<UpdateComandListComand>();

        public string Path { get; set; }

        public async Task Execute()
        {
            string url = "https://developer.valvesoftware.com/wiki/List_of_TF2_console_commands_and_variables";
            Log.Information("loading new command list from {CommandListUrl}", url);

            string content = await new HttpClient().GetStringAsync(url);
            XmlDocument document = new XmlDocument();
            document.LoadXml(content);

            var commands = document.SelectNodes("//pre")
                .Cast<XmlNode>()
                .Select(n => n.FirstChild.Value)
                .SelectMany(ExtractCommands);

            Log.Information("writing command list to {CommandListPath}", Path);
            File.WriteAllLines(Path, commands);
        }

        private static IEnumerable<string> ExtractCommands(string n)
        {
            string[] lines = n.Split(new[] {'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                int i = line.IndexOf(" ");
                if (i < 0)
                    yield return line;
                yield return line.Substring(0, i);
            }
        }
    }
}