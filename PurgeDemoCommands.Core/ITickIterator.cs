namespace PurgeDemoCommands.Core
{
    public interface ITickIterator
    {
        int Move(int tick);
    }

    public class ForwardTickIterator : ITickIterator
    {
        public int Move(int tick)
        {
            return ++tick;
        }
    }

    public class BackwardTickIterator : ITickIterator
    {
        public int Move(int tick)
        {
            return --tick;
        }
    }
}