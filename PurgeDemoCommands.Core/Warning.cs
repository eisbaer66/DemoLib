using System;

namespace PurgeDemoCommands.Core
{
    [Flags]
    public enum Warning
    {
        None = 0,
        FileAlreadyExists = 1,
    }
}