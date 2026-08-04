using System;
using System.Collections.Generic;
using System.Text;

namespace Myra.CompilerTasks
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var game = new MainWindowTestGame())
                game.Run();
        }
    }
}
