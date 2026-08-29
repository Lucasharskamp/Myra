namespace Myra.Samples.DataGrid;

class Program
{
	static void Main(string[] args)
	{
		using (var game = new DataGridGame())
			game.Run();
	}
}
