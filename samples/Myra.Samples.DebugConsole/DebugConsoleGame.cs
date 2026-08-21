using Myra.Graphics2D.UI;
using Microsoft.Xna.Framework;

namespace Myra.Samples.DebugConsole
{
	public class DebugConsoleGame : Game
	{
		private readonly GraphicsDeviceManager _graphics;
		private GamePanel _gamePanel;
		private Desktop _desktop;
		
		public static DebugConsoleGame Instance { get; private set; }

		public DebugConsoleGame()
		{
			Instance = this;

			_graphics = new GraphicsDeviceManager(this)
			{
				PreferredBackBufferWidth = 1200,
				PreferredBackBufferHeight = 720
			};
			Window.AllowUserResizing = true;
			IsMouseVisible = true;
		}

		protected override void LoadContent()
		{
			base.LoadContent();

			MyraEnvironment.Game = this;

			_gamePanel = new GamePanel();

			_desktop = new Desktop
			{
				Root = _gamePanel
			};

            // Inform Myra that external text input is available
            // So it stops translating Keys to chars
            _desktop.HasExternalTextInput = true;

			// Provide that text input
			Window.TextInput += (s, a) =>
			{
                _desktop.OnChar(a.Character);
			}; 
		}

		protected override void Draw(GameTime gameTime)
		{
			base.Draw(gameTime);

			GraphicsDevice.Clear(Color.Black);
			_desktop.Render();
		}

        protected override void Dispose(bool disposing)
        {
			_graphics.Dispose();
			_desktop.Dispose();
            base.Dispose(disposing);
        }
	}
}