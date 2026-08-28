using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.ColorPicker;
using Myra.Graphics2D.UI.File;
using Myra.Graphics2D.UI.Styles;

namespace Myra.Samples.Viewports
{
	public partial class AllWidgets : Panel
	{
		public HorizontalMenu _mainMenu { get; set; }

		public TextBox _textSaveFile { get; set; }  

		public TextBox _textOpenFile { get; set; } 
		  
		public TextBox _textChooseFolder { get; set; }

		public Label _textButtonLabel { get; set; }  

		public HorizontalProgressBar _horizontalProgressBar { get; set; }

        public VerticalProgressBar _verticalProgressBar { get; set; }

        public Label _labelOverGui { get; set; }

		private TreeView _treeView { get; set; }

		private void ShowDebugOptionsModal()
		{
            var debugWindow = new DebugOptionsWindow();
            debugWindow.ShowModal(Desktop);
        }

		private void ShowMessageBox()
		{
            var messageBox = Dialog.CreateMessageBox("AllWidgets", "Myra AllWidgets Sample " + MyraEnvironment.Version);
            messageBox.ShowModal(Desktop);
        }

        public void Init()
		{
			var tree = _treeView;
            Grid.SetColumn(tree, 1);
			Grid.SetRow(tree, 12);
			Grid.SetColumnSpan(tree, 2);

			var node1 = tree.AddSubNode(new Label
			{
				Text = "node1"
			});
			var node2 = node1.AddSubNode(new Label
			{
				Text = "node2"
			});

			var node4 = node2.AddSubNode(new Label
			{
				Text = "node6"
			});
			node4.AddSubNode(new Label
			{
				Text = "node11"
			});
			node4.AddSubNode(new Label
			{
				Text = "node12"
			});
			node4.AddSubNode(new Label
			{
				Text = "node13"
			});
			node4.AddSubNode(new Label
			{
				Text = "node14"
			});
			node4.AddSubNode(new Label
			{
				Text = "node15"
			});
			node4.AddSubNode(new Label
			{
				Text = "node16"
			});
			node4.AddSubNode(new Label
			{
				Text = "node17"
			});
			node4.AddSubNode(new Label
			{
				Text = "node18"
			});

			var node3 = node2.AddSubNode(new CheckButton
			{
				Content = new Label
				{
					Text = "CheckButton node"
				}
			});
			node3.AddSubNode(new Label
			{
				Text = "node4"
			});
			node3.AddSubNode(new CheckButton
			{
				Content = new Label { Text = "CheckButton node2" },
				CheckPosition = CheckPosition.Right,
				CheckContentSpacing = 8
			});

			var imageButtonContent = new HorizontalStackPanel
			{
				Spacing = 4
			};

			imageButtonContent.Widgets.Add(new Image
			{
				Renderable = Stylesheet.Current.Atlas["icon-star"]
			});

			imageButtonContent.Widgets.Add(new Label
			{
				Text = "Button node"
			});
			node3.AddSubNode(new Button
			{
				Content = imageButtonContent
			});
			node3.AddSubNode(new HorizontalSlider());
			node3.AddSubNode(new SpinButton());

			var imageButtonContent2 = new HorizontalStackPanel
			{
				Spacing = 4
			};

			imageButtonContent2.Widgets.Add(new Label
			{
				Text = "ToggleButton node"
			});
			imageButtonContent2.Widgets.Add(new Image
			{
				Renderable = Stylesheet.Current.Atlas["icon-star"]
			});

			node3.AddSubNode(new ToggleButton
			{
				Content = imageButtonContent2
			});
		}

		public void OpenFile()
		{
			var fileDialog = new FileDialog(FileDialogMode.OpenFile);
			fileDialog.ShowModal(Desktop);

			fileDialog.Closed += (s, a) =>
			{
				if (!fileDialog.Result)
				{
					return;
				}

				_textOpenFile.Text = fileDialog.FilePath;
			};
		}

		public void SaveFile()
		{
			var fileDialog = new FileDialog(FileDialogMode.SaveFile);
			fileDialog.ShowModal(Desktop);

			fileDialog.Closed += (s, a) =>
			{
				if (!fileDialog.Result)
				{
					return;
				}

				_textSaveFile.Text = fileDialog.FilePath;
			};
		}

		public void ChooseFolder()
		{
			var fileDialog = new FileDialog(FileDialogMode.ChooseFolder);
			fileDialog.ShowModal(Desktop);

			fileDialog.Closed += (s, a) =>
			{
				if (!fileDialog.Result)
				{
					return;
				}

				_textChooseFolder.Text = fileDialog.FilePath;
			};
		}

		public void ChooseColor()
		{
            var colorWindow = new ColorPickerDialog
            {
                Color = _textButtonLabel.TextColor
            };
            colorWindow.ShowModal(Desktop);

			colorWindow.Closed += (s, a) =>
			{
				if (!colorWindow.Result)
				{
					return;
				}

				_textButtonLabel.TextColor = colorWindow.Color;
			};
		}

		public void Quit()
		{
			AllWidgetsGame.Instance.Exit();
		}
	}
}