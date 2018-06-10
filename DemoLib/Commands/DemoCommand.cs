namespace DemoLib.Commands
{
    public class DemoCommand
	{
		public DemoCommandType Type { get; protected set; } = DemoCommandType.dem_invalid;
	    public long IndexStart { get; set; } = 0;
	    public long IndexEnd { get; set; } = 0;
	}
}
