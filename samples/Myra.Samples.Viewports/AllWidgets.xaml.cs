using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.ColorPicker;
using Myra.Graphics2D.UI.File;
using Myra.Graphics2D.UI.Styles;

namespace Myra.Samples.Viewports
{
	public partial class AllWidgets : Panel
	{
		public HorizontalMenu _mainMenu { get; set; }
		public MenuItem _menuFile { get; set; }

		public MenuItem _menuItemOpenFile { get; set; }
		public MenuItem _menuItemSaveFile { get; set; }

		public MenuItem _menuItemChooseFolder { get; set; }

		public MenuItem _menuItemChooseColor { get; set; }

		public MenuItem _menuItemQuit { get; set; }

		public MenuItem _menuItemCopy { get; set; }
		public MenuItem _menuEdit { get; set; }
        public MenuItem _menuItemPaste { get; set; }

		public MenuItem _menuItemUndo { get; set; }

		public MenuItem _menuItemRedo { get; set; }

		public MenuItem _menuHelp { get; set; }

		public MenuItem _menuItemAbout { get; set; }

		public Grid _gridRight { get; set; }

		public Button _buttonSaveFile { get; set; }
		public Image _imageSaveFile { get; set; }

		public TextBox _textSaveFile { get; set; }

		public Button _buttonOpenFile { get; set; }

		public Image _imageOpenFile { get; set; }

		public TextBox _textOpenFile { get; set; }

		public Button _buttonChooseFolder { get; set; }

		public Image _imageChooseFolder { get; set; }

		public TextBox _textChooseFolder { get; set; }

		public Label _textButtonLabel { get; set; }

		public Button _buttonChooseColor { get; set; }

		public Button _imageButton { get; set; }

		public HorizontalProgressBar _horizontalProgressBar { get; set; }

        public VerticalProgressBar _verticalProgressBar { get; set; }

        public Label _labelOverGui { get; set; }


        public void Init()
		{ 
			_menuItemOpenFile.Image = Stylesheet.Current.Atlas["icon-folder"];
			_menuItemSaveFile.Image = Stylesheet.Current.Atlas["icon-folder-new"];

			_menuItemOpenFile.Selected += (s, a) => OpenFile();
			_menuItemSaveFile.Selected += (s, a) => SaveFile();
			_menuItemChooseColor.Selected += (s, a) => ChooseColor();
			_menuItemChooseFolder.Selected += (s, a) => ChooseFolder();
			_menuItemQuit.Selected += (s, a) => Quit();

			_imageOpenFile.Renderable = Stylesheet.Current.Atlas["icon-star"];
			_buttonOpenFile.Click += (sender, args) => OpenFile();

			_imageSaveFile.Renderable = Stylesheet.Current.Atlas["icon-star"];
			_buttonSaveFile.Click += (sender, args) => SaveFile();

			_imageChooseFolder.Renderable = Stylesheet.Current.Atlas["icon-star"];
			_buttonChooseFolder.Click += (sender, args) => ChooseFolder();

			_buttonChooseColor.Click += (sender, args) => ChooseColor();

			var image = (Image)_imageButton.Content;
			image.Renderable = Stylesheet.Current.Atlas["icon-star-outline"];
			_imageButton.Click += (sender, args) =>
			{
				var debugWindow = new DebugOptionsWindow();
				debugWindow.ShowModal(Desktop);
			};

			_menuItemAbout.Selected += (sender, args) =>
			{
				var messageBox = Dialog.CreateMessageBox("AllWidgets", "Myra AllWidgets Sample " + MyraEnvironment.Version);
				messageBox.ShowModal(Desktop);
			};

			var tree = new TreeView();
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

			_gridRight.Widgets.Add(tree);
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
			var colorWindow = new ColorPickerDialog();
			colorWindow.Color = _textButtonLabel.TextColor;
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