using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;

namespace Myra.Tests
{
	class TestGame : Game
	{
		private readonly GraphicsDeviceManager _graphics;

		public TestGame()
		{
			_graphics = new GraphicsDeviceManager(this)
			{
				PreferredBackBufferWidth = 1200,
				PreferredBackBufferHeight = 800
			};

			((IGraphicsDeviceManager)Services.GetService(typeof(IGraphicsDeviceManager))).CreateDevice();
		}

        protected override void Dispose(bool disposing)
        { 
            _graphics.Dispose();
            base.Dispose(disposing);
        }
    }
}
