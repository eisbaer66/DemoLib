using System;

namespace PurgeDemoCommands
{
    internal class Result
    {
        public string Filename { get; }
        public Warning Warning { get; set; }
        public string NewFilepath { get; set; }

        public Result(string filename)
        {
            if (filename == null) throw new ArgumentNullException(nameof(filename));
            Filename = filename;
        }

    }
}