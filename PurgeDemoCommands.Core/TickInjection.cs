namespace PurgeDemoCommands.Core
{
    public interface ITickInjection
    {
        int Tick { get; set; }
        string Commands { get; set; }
    }

    public class TickInjection : ITickInjection
    {
        public int Tick { get; set; }
        public string Commands { get; set; }
    }
}