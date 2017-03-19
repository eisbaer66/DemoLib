using System;

namespace PurgeDemoCommands
{
    [Flags]
    internal enum Warning
    {
        None = 0,
        FileAlreadyExists = 1,
    }
}