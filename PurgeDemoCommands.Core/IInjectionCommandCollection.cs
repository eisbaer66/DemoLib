namespace PurgeDemoCommands.Core
{
    public interface IInjectionCommandCollection
    {
        string GetCommand(long numberOfBytes);
    }
}