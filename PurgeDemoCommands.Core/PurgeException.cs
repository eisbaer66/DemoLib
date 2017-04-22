using System;

namespace PurgeDemoCommands.Core
{
    public class PurgeException : Exception
    {
        public string Filename { get; }

        public PurgeException(string filename, Exception exception)
            :base(string.Format("error while purging file {0}", filename), exception)
        {
            if (filename == null) throw new ArgumentNullException(nameof(filename));
            if (exception == null) throw new ArgumentNullException(nameof(exception));

            Filename = filename;
        }
    }
}