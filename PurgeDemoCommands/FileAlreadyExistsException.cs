using System;

namespace PurgeDemoCommands
{
    internal class FileAlreadyExistsException : Exception
    {
        public string Filename { get; }

        public FileAlreadyExistsException(string filename)
            :base(string.Format("file '{0}' already exists", filename))
        {
            Filename = filename;
        }
    }
}