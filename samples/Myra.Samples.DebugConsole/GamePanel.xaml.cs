using Microsoft.Xna.Framework;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace Myra.Samples.DebugConsole
{
	public partial class GamePanel : Panel
	{
		private Button DebugModalButton { get; set; } = default!;

		private GamePanelViewModel ViewModel { get; set; } = new();


        private void ShowDebugPanel(bool isModal)
		{
			var debugPanel = new DebugPanel
			{
				Opacity = 0.75f,
				Background = new SolidBrush(Color.Blue),
				IsModal = isModal
			};

			ViewModel.ButtonsEnabled = false;
			 
            debugPanel.Removed += (s, a) =>
			{
                ViewModel.ButtonsEnabled = true;
            };

			Desktop.Widgets.Add(debugPanel);
		}

        public void ButtonDebugPanel_Click(object sender, MyraEventArgs e)
		{
			ShowDebugPanel(false);
		}

		public void ButtonModalDebugPanel_Click(object sender, MyraEventArgs e)
		{
			ShowDebugPanel(true);
		}
	}
}