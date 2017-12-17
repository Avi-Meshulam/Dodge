using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Core;
using Windows.System;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.UI.Text;
using Enums;
using System.Xml;
using System.Text;
using Windows.ApplicationModel;
using System.Collections.ObjectModel;
using System.Threading;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Dodge
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const int ENEMIES_COUNT = 10;
        private const int OBSTACLES_COUNT = 4;
        private const int IMAGES_COUNT = ENEMIES_COUNT + OBSTACLES_COUNT + 1;

        private Game _game;
        private Grid _grdBoard;
        private Grid _grdIndicators;
        private TextBlock _tbLife;
        private TextBlock _tbTime;
        private IDictionary<EntityType, IDictionary<string, bool>> _assets = 
            new Dictionary<EntityType, IDictionary<string, bool>>();
        private bool _isCommandBarInitialized;
        private bool _isGamePaused;
        private bool _isGameRunning;
        private bool _isLoadedGame;
        private Direction _playerDirection;
        private DispatcherTimer _keyboardTimer;

        private GameLevel _gameLevel;
        private GameLevel GameLevel
        {
            get { return _gameLevel; }
            set
            {
                _gameLevel = value;
                if (_game != null)
                {
                    _game.Level = _gameLevel;
                }
            }
        }

        public MainPage()
        {
            this.InitializeComponent();
            _gameLevel = GameLevel.Beginner;
            _playerDirection = new Direction();
            InitKeyboardTimer();
        }

        private void InitKeyboardTimer()
        {
            _keyboardTimer = new DispatcherTimer();
            _keyboardTimer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            _keyboardTimer.Tick += _keyboardTimer_Tick;
        }

        private void _keyboardTimer_Tick(object sender, object e)
        {
            if (_game != null)
            {
                _tbTime.Text = string.Format("{0:0#}:{1:0#}:{2:0#}",
                    _game.ElapsedTime.Hours, _game.ElapsedTime.Minutes, _game.ElapsedTime.Seconds);
                _game.Board.MovePlayer(_playerDirection);
            }
        }

        #region Start Game
        private void StartGame()
        {
            if (!_isGamePaused && !_isLoadedGame)
            {
                InitGame();
            }

            SubscribeToKeyboardEvents();

            if (_isGamePaused)
            {
                _game.Resume();
            }
            else
            {
                _game.Start(ENEMIES_COUNT, OBSTACLES_COUNT);
            }

            _grdIndicators.Visibility = Visibility.Visible;
            _isGamePaused = false;
            _isGameRunning = true;
        }

        private void InitGame(XElement xGame = null)
        {
            int rowsCount;
            int colsCount;

            if (xGame != null)
            {
                rowsCount = (int)xGame.Attribute("RowsCount");
                colsCount = (int)xGame.Attribute("ColsCount");

                InitGrid(rowsCount, colsCount);

                if (xGame.Attribute("Level") != null &&
                    Enum.IsDefined(typeof(GameLevel), xGame.Attribute("Level").Value))
                {
                    GameLevel = (GameLevel)Enum.Parse(typeof(GameLevel), xGame.Attribute("Level").Value);

                    var rdBtnLevel = cnvSettings.Children.OfType<RadioButton>().
                        FirstOrDefault((b) => b.Content.ToString() == xGame.Attribute("Level").Value);
                    rdBtnLevel.IsChecked = true;
                }
            }
            else
            {
                InitGrid();

                rowsCount = _grdBoard.RowDefinitions.Count;
                colsCount = _grdBoard.ColumnDefinitions.Count;
            }

            _game = new Game(GameMode.Graphic, rowsCount, colsCount);
            _game.Level = GameLevel;

            SubscribeToBoardEvents();
        }

        //Subscribe to game board's events, in order to update UI
        private void SubscribeToBoardEvents()
        {
            _game.Board.NewEntityCreated += Board_NewEntityCreated;
            _game.Board.PageSizeChangedEntityCallback += Board_PageSizeChangedEntityCallback;
            _game.Board.EntityIsHit += Board_EntityIsHit;
            _game.Board.EntityIsDead += Board_EntityIsDead;
        }

        private void SubscribeToKeyboardEvents()
        {
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            Window.Current.CoreWindow.KeyUp += CoreWindow_KeyUp;
            _keyboardTimer.Start();
        }

        private void InitGrid(int rowsCount = -1, int colsCount = -1)
        {
            if (rowsCount == -1 || colsCount == -1)
            {
                colsCount = Board.DEFAULT_COLS_COUNT;
                int cellSize = (int)Math.Floor(rootLayout.ActualWidth / colsCount);
                rowsCount = (int)Math.Floor(rootLayout.ActualHeight / cellSize);
            }

            CreateMainGrid(rowsCount, colsCount);
            CreateIndicatorsGrid();
        }

        private void CreateMainGrid(int rowsCount, int colsCount)
        {
            if (_grdBoard != null)
            {
                rootLayout.Children.Remove(_grdBoard);
            }

            _grdBoard = new Grid();
            _grdBoard.Width = rootLayout.ActualWidth;
            _grdBoard.Height = rootLayout.ActualHeight;

            Utils.CreateGridCells(_grdBoard, rowsCount, colsCount);

            rootLayout.Children.Add(_grdBoard);
        }

        private void CreateIndicatorsGrid()
        {
            if (_grdIndicators != null)
            {
                rootLayout.Children.Remove(_grdIndicators);
            }

            _grdIndicators = new Grid();
            _grdIndicators.Width = 200;
            _grdIndicators.Height = 50;
            _grdIndicators.Visibility = Visibility.Collapsed;

            rootLayout.Children.Add(_grdIndicators);
            Canvas.SetTop(_grdIndicators, 0);
            Canvas.SetLeft(_grdIndicators, rootLayout.ActualWidth - _grdIndicators.Width);

            Utils.CreateGridCells(_grdIndicators, 1, 2);
            AddIndicatorsGridChildren();
        }

        private void AddIndicatorsGridChildren()
        {
            Image imgLife = new Image();
            imgLife.Source = new BitmapImage(new Uri("ms-appx:///Assets/General/Heart.png"));
            imgLife.HorizontalAlignment = HorizontalAlignment.Center;
            imgLife.VerticalAlignment = VerticalAlignment.Center;
            _grdIndicators.Children.Add(imgLife);
            Grid.SetRow(imgLife, 0);
            Grid.SetColumn(imgLife, 1);

            _tbLife = Utils.CreateTextBlock("tbLife", "10", 25);
            _tbLife.FontWeight = FontWeights.Bold;
            _tbLife.Foreground = new SolidColorBrush(Colors.Blue);
            _tbLife.HorizontalAlignment = HorizontalAlignment.Center;
            _tbLife.VerticalAlignment = VerticalAlignment.Center;
            _grdIndicators.Children.Add(_tbLife);
            Grid.SetRow(_tbLife, 0);
            Grid.SetColumn(_tbLife, 1);

            _tbTime = Utils.CreateTextBlock("tbTime", "50", 25);
            _tbTime.FontWeight = FontWeights.Bold;
            _tbTime.Foreground = new SolidColorBrush(Colors.Red);
            _tbTime.HorizontalAlignment = HorizontalAlignment.Left;
            _tbTime.VerticalAlignment = VerticalAlignment.Center;
            _tbTime.TextAlignment = TextAlignment.Left;
            _grdIndicators.Children.Add(_tbTime);
            Grid.SetRow(_tbTime, 0);
            Grid.SetColumn(_tbTime, 0);
        }
        #endregion Start Game

        #region Pause/Stop Game
        private void PauseGame()
        {
            _game.Pause();
            _isGamePaused = true;
            _isGameRunning = false;
            UnsubscribeFromKeyboardEvents();
        }

        private void StopGame(bool isGameOver = false)
        {
            _game.Stop();
            _isGamePaused = false;
            _isGameRunning = false;
            _isLoadedGame = false;

            UnsubscribeFromBoardEvents();
            UnsubscribeFromKeyboardEvents();
            ResetAssets();

            if (!isGameOver)
            {
                InitGrid();
            }
        }

        private void UnsubscribeFromBoardEvents()
        {
            _game.Board.NewEntityCreated -= Board_NewEntityCreated;
            _game.Board.PageSizeChangedEntityCallback -= Board_PageSizeChangedEntityCallback;
            _game.Board.EntityIsHit -= Board_EntityIsHit;
            _game.Board.EntityIsDead -= Board_EntityIsDead;
        }

        private void UnsubscribeFromKeyboardEvents()
        {
            Window.Current.CoreWindow.KeyDown -= CoreWindow_KeyDown;
            Window.Current.CoreWindow.KeyUp -= CoreWindow_KeyUp;
            _keyboardTimer.Stop();
        }

        private void ResetAssets()
        {
            foreach (var key in _assets.Keys)
            {
                Utils.ResetDictionary(_assets[key]);
            }
        }
        #endregion Pause/Stop Game

        #region Board Events
        private void Board_NewEntityCreated(object sender, EventArgs e)
        {
            Entity entity = (Entity)sender;
            InitEntityImage(entity);
            entity.ImageOpened += Entity_ImageOpened;
            _grdBoard.Children.Add(entity.Image);
        }

        private void InitEntityImage(Entity entity)
        {
            entity.Image.Stretch = Stretch.None;
            entity.Image.HorizontalAlignment = HorizontalAlignment.Stretch;
            entity.Image.VerticalAlignment = VerticalAlignment.Stretch;

            if (!_isLoadedGame)
            {
                string filePath = Utils.GetRandomUnsetDictionaryKey(_assets[entity.Type]);
                _assets[entity.Type][filePath] = true;
                entity.Image.Source = new BitmapImage(new Uri(filePath));
            }
        }

        private void Entity_ImageOpened(object sender, EventArgs e)
        {
            Entity entity = (Entity)sender;

            double rowHeight = _grdBoard.RowDefinitions[0].ActualHeight;
            double colWidth = _grdBoard.ColumnDefinitions[0].ActualWidth;

            entity.RowSpan = (int)Math.Ceiling(entity.Image.ActualHeight / rowHeight);
            entity.ColSpan = (int)Math.Ceiling(entity.Image.ActualWidth / colWidth);

            _game.Board.PositionEntity(entity, _isLoadedGame);

            Grid.SetRow(entity.Image, entity.Position.Row);
            Grid.SetColumn(entity.Image, entity.Position.Col);
            Grid.SetRowSpan(entity.Image, entity.RowSpan);
            Grid.SetColumnSpan(entity.Image, entity.ColSpan);

            entity.ImageOpened -= Entity_ImageOpened;
            entity.PositionChanged += Entity_PositionChanged;
        }

        private void Entity_PositionChanged(object sender, EventArgs e)
        {
            Entity entity = (Entity)sender;
            Grid.SetColumn(entity.Image, entity.Position.Col);
            Grid.SetRow(entity.Image, entity.Position.Row);
        }

        private async void Board_EntityIsHit(object sender, EventArgs e)
        {
            await EntityIsHit((Entity)sender);
        }

        private async Task EntityIsHit(Entity entity)
        {
            if (entity.Type == EntityType.Player)
            {
                if (((Player)entity).LifeCount >= 0)
                {
                    _tbLife.Text = ((Player)entity).LifeCount.ToString();
                }
            }

            //counter must be even, in order to finish HIT effect with visible image
            for (int counter = 0; counter < 4; counter++)
            {
                entity.Image.Visibility = (entity.Image.Visibility == Visibility.Visible) ?
                    Visibility.Collapsed : Visibility.Visible;
                await Task.Delay(100);
            }
        }

        private async void Board_EntityIsDead(object sender, EventArgs e)
        {
            Entity entity = (Entity)sender;

            await EntityIsHit(entity);
            entity.Image.Source = new BitmapImage(new Uri("ms-appx:///Assets/General/Skull.png"));
            await Task.Delay(500);

            _grdBoard.Children.Remove(entity.Image);

            entity.PositionChanged -= Entity_PositionChanged;

            if (entity.Type == EntityType.Player)
            {
                GameOver(win: false);
            }
            else
            {
                if (_game.Board.EnemiesCount <= 1)
                {
                    if (_game.Board.Player != null)
                    {
                        GameOver(win: true);
                    }
                }
            }
        }
        #endregion Board Events

        #region Save Game
        private void ShowSaveGame()
        {
            cnvSaveGame.Visibility = Visibility.Visible;
            txtSaveName.Text = "";
            txtSaveName.Focus(FocusState.Keyboard);
        }

        private void txtSaveGame_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case VirtualKey.Enter:
                    btnSaveOK_Click(null, null);
                    break;
                case VirtualKey.Escape:
                    btnSaveCancel_Click(null, null);
                    break;
                default:
                    break;
            }
        }

        private void txtSaveName_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnSaveOK.IsEnabled = ((TextBox)sender).Text == string.Empty ? false : true;
        }

        private void btnSaveOK_Click(object sender, RoutedEventArgs e)
        {
            SaveGame(txtSaveName.Text + ".xml");
            CommandBar.IsEnabled = true;
            if (!CommandBar.IsOpen)
            {
                CommandBar_Closed(null, null);
            }
        }

        private void btnSaveCancel_Click(object sender, RoutedEventArgs e)
        {
            cnvSaveGame.Visibility = Visibility.Collapsed;
            cnvUser.Visibility = Visibility.Collapsed;
            CommandBar.IsEnabled = true;
            if (!CommandBar.IsOpen)
            {
                CommandBar_Closed(null, null);
            }
        }

        private int _saveCallsCounter;  // Used to avoid duplicate calls, when waiting for asynchronic methods to return

        private async void SaveGame(string fileName)
        {
            bool isFileExists = false;
            if (_saveCallsCounter == 0)
            {
                _saveCallsCounter++;
                isFileExists = await Utils.IsFileExsits(fileName);
            }
            else
            {
                return;
            }

            _saveCallsCounter = 0;

            if (isFileExists)
            {
                var result = await Utils.ShowMessageDialog(
                    string.Format("The name {0} already exists. Would you like to replace existing file?", txtSaveName.Text));
                if (result.Label == "No")
                {
                    txtSaveName.Focus(FocusState.Keyboard);
                    return;
                }
            }

            StorageFolder appLocalFolder = ApplicationData.Current.LocalFolder;
            try
            {
                StorageFile storageFile = await appLocalFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
                using (Stream stream = await storageFile.OpenStreamForWriteAsync())
                {
                    _game.GetXML().Save(stream);
                    stream.Flush();
                }
            }
            catch (Exception ex)
            {
                Utils.ShowMessageBox(ex.Message);
                txtSaveName.Focus(FocusState.Keyboard);
                return;
            }

            cnvSaveGame.Visibility = Visibility.Collapsed;
            cnvUser.Visibility = Visibility.Collapsed;
            Utils.ShowMessageBox("Game has been saved successfully");
        }
        #endregion Save Game

        #region Load Game
        private void ShowLoadGame()
        {
            DisplaySavedGames();
        }

        private async void DisplaySavedGames()
        {
            var files = await ApplicationData.Current.LocalFolder.GetFilesAsync();

            if (files.Count == 0)
            {
                Utils.ShowMessageBox("You haven't saved any game yet");
                btnLoadCancel_Click(null, null);
                return;
            }
            else
            {
                cnvLoadGame.Visibility = Visibility.Visible;
                lvLoadGame.Items.Clear();
            }

            var query =
                from file in files
                select file;

            foreach (var file in files)
            {
                var item = new TextBlock();
                item.Text = file.DisplayName;
                item.FontSize = 20;
                item.FontWeight = FontWeights.Bold;
                item.Margin = new Thickness(5, 0, 0, 0);
                lvLoadGame.Items.Add(item);
            }
        }

        private async void btnLoadOK_Click(object sender, RoutedEventArgs e)
        {
            string fileName = ((TextBlock)lvLoadGame.SelectedItem).Text + ".xml";
            StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(fileName);
            XDocument xDocument = XDocument.Load(file.Path);

            XElement xGame = xDocument.Descendants("Game").First();

            InitGame(xGame);

            _isLoadedGame = true;
            _game.Load(xGame);

            _tbLife.Text = _game.Board.Player.LifeCount.ToString();
            _tbTime.Text = string.Format("{0:0#}:{1:0#}:{2:0#}",
                _game.ElapsedTime.Hours, _game.ElapsedTime.Minutes, _game.ElapsedTime.Seconds);
            _grdIndicators.Visibility = Visibility.Visible;

            cnvLoadGame.Visibility = Visibility.Collapsed;
            cnvUser.Visibility = Visibility.Collapsed;

            Utils.ShowMessageBox("Game has been loaded. Press play to start game...");
            CommandBar.IsEnabled = true;
        }

        private void btnLoadCancel_Click(object sender, RoutedEventArgs e)
        {
            cnvLoadGame.Visibility = Visibility.Collapsed;
            cnvUser.Visibility = Visibility.Collapsed;
            CommandBar.IsEnabled = true;
        }
        #endregion Load Game

        #region Settings
        private void ShowSettings()
        {
            cnvSettings.Visibility = Visibility.Visible;
        }

        private void btnSettingsOK_Click(object sender, RoutedEventArgs e)
        {
            var rdBtnLevel = cnvSettings.Children.OfType<RadioButton>().FirstOrDefault((b) => b.IsChecked == true);

            if (Enum.IsDefined(typeof(GameLevel), rdBtnLevel.Content.ToString()))
            {
                GameLevel = (GameLevel)Enum.Parse(typeof(GameLevel), rdBtnLevel.Content.ToString());
            }

            cnvSettings.Visibility = Visibility.Collapsed;
            cnvUser.Visibility = Visibility.Collapsed;
            CommandBar.IsEnabled = true;
            if (!CommandBar.IsOpen)
            {
                CommandBar_Closed(null, null);
            }
        }
        #endregion Settings

        #region Loading Events
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            CommandBar.IsEnabled = false;
            DisplayAbout(seconds: 5);
            await MapAssets();
            CommandBar.IsEnabled = true;
        }

        private void DisplayAbout(int seconds)
        {
            DispatcherTimer aboutTimer = new DispatcherTimer();
            aboutTimer.Interval = new TimeSpan(0, 0, seconds);
            aboutTimer.Tick += (s, e1) =>
            {
                imgAbout.Visibility = Visibility.Collapsed;
                aboutTimer.Stop();
            };
            aboutTimer.Start();
        }

        private async Task MapAssets()
        {
            List<string> fileTypeFilter = new List<string>();
            fileTypeFilter.Add(".png");
            QueryOptions queryOptions = new QueryOptions(CommonFileQuery.OrderByName, fileTypeFilter);

            var assetsFoldersByType = new Dictionary<EntityType, string>
            {
                { EntityType.Player, "Players" },
                { EntityType.Enemy, "Enemies" },
                { EntityType.Obstacle, "Obstacles" }
            };

            StorageFolder appFolder = Package.Current.InstalledLocation;

            foreach (var assetsFolder in assetsFoldersByType)
            {
                _assets.Add(assetsFolder.Key, new Dictionary<string, bool>());
                var folder = await appFolder.GetFolderAsync($@"Assets\{assetsFolder.Value}");
                var files = await folder.CreateFileQueryWithOptions(queryOptions).GetFilesAsync();
                foreach (StorageFile file in files)
                {
                    _assets[assetsFolder.Key].Add(file.Path, false);
                }
            }
        }

        private void rootLayout_Loaded(object sender, RoutedEventArgs e)
        {
            cnvUser.Width = rootLayout.ActualWidth;
            cnvUser.Height = rootLayout.ActualHeight;
        }

        private void AppBarButton_Loaded(object sender, RoutedEventArgs e)
        {
            AppBarButton appBarButton = (AppBarButton)sender;
            if (appBarButton.TabIndex == 1)
            {
                if (!_isCommandBarInitialized)
                {
                    InitCommandBar((AppBarButton)sender);
                    _isCommandBarInitialized = true;
                }
            }
        }
        #endregion Loading Events

        #region Keyboard Events
        private void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            CoreVirtualKeyStates shift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift);
            bool isShift = shift.HasFlag(CoreVirtualKeyStates.Down);

            switch (args.VirtualKey)
            {
                case VirtualKey.Down:
                    _playerDirection.Down = true;
                    break;
                case VirtualKey.Left:
                    _playerDirection.Left = true;
                    break;
                case VirtualKey.Right:
                    _playerDirection.Right = true;
                    break;
                case VirtualKey.Up:
                    _playerDirection.Up = true;
                    break;
                default:
                    break;
            }
        }

        private void CoreWindow_KeyUp(CoreWindow sender, KeyEventArgs args)
        {
            switch (args.VirtualKey)
            {
                case VirtualKey.Down:
                    _playerDirection.Down = false;
                    break;
                case VirtualKey.Left:
                    _playerDirection.Left = false;
                    break;
                case VirtualKey.Right:
                    _playerDirection.Right = false;
                    break;
                case VirtualKey.Up:
                    _playerDirection.Up = false;
                    break;
                default:
                    break;
            }
        }
        #endregion Keyboard Events

        #region Sizing Events
        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_game != null)
            {
                _game.Board.PageSizeChanged();
            }
        }

        private void cnvLoadGame_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Canvas.SetTop(cnvLoadGame, (rootLayout.ActualHeight - CommandBar.ActualHeight - cnvLoadGame.ActualHeight) / 2);
            Canvas.SetLeft(cnvLoadGame, (rootLayout.ActualWidth - cnvLoadGame.ActualWidth) / 2);
            lvLoadGame.Height = cnvLoadGame.ActualHeight - 2 * btnLoadOK.ActualHeight;
            lvLoadGame.Width = cnvLoadGame.ActualWidth;
        }

        private void cnvSaveGame_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Canvas.SetTop(cnvSaveGame, (rootLayout.ActualHeight - CommandBar.ActualHeight - cnvSaveGame.ActualHeight) / 2);
            Canvas.SetLeft(cnvSaveGame, (rootLayout.ActualWidth - cnvSaveGame.ActualWidth) / 2);
            txtSaveName.Width = cnvSaveGame.ActualWidth;
        }

        private void cnvSettings_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Canvas.SetTop(cnvSettings, (rootLayout.ActualHeight - CommandBar.ActualHeight - cnvSettings.ActualHeight) / 2);
            Canvas.SetLeft(cnvSettings, (rootLayout.ActualWidth - cnvSettings.ActualWidth) / 2);
        }

        private void Board_PageSizeChangedEntityCallback(object sender, EventArgs e)
        {
            Entity entity = (Entity)sender;

            double rowHeight = _grdBoard.RowDefinitions[0].ActualHeight;
            double colWidth = _grdBoard.ColumnDefinitions[0].ActualWidth;

            entity.RowSpan = (int)Math.Ceiling(entity.Image.ActualHeight / rowHeight);
            entity.ColSpan = (int)Math.Ceiling(entity.Image.ActualWidth / colWidth);

            Grid.SetRowSpan(entity.Image, entity.RowSpan);
            Grid.SetColumnSpan(entity.Image, entity.ColSpan);

            entity.Image.UpdateLayout();
        }
        #endregion Sizing Events

        #region GUI Events
        private void imgAbout_ImageOpened(object sender, RoutedEventArgs e)
        {
            Canvas.SetTop(imgAbout, (rootLayout.ActualHeight - imgAbout.ActualHeight) / 2);
            Canvas.SetLeft(imgAbout, (rootLayout.ActualWidth - imgAbout.ActualWidth) / 2);
        }

        private void Page_Tapped(object sender, TappedRoutedEventArgs e)
        {
            imgAbout.Visibility = Visibility.Collapsed;
        }

        private async void TextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            TextBlock textBlock = (TextBlock)sender;

            double rowHeight = _grdBoard.RowDefinitions[0].ActualHeight;
            double colWidth = _grdBoard.ColumnDefinitions[0].ActualWidth;

            int tbRowSpan = (int)Math.Ceiling(textBlock.ActualHeight / rowHeight);
            int tbColSpan = (int)Math.Ceiling(textBlock.ActualWidth / colWidth);
            int tbCol = (_game.Board.ColsCount - tbColSpan) / 2;

            int tbRow;
            switch (textBlock.Name)
            {
                case "tbMessage":
                    tbRow = GetRelativeRow(outOfTenSection: 4, rowSpan: tbRowSpan);
                    break;
                case "tbResult":
                    tbRow = GetRelativeRow(outOfTenSection: 5, rowSpan: tbRowSpan);
                    break;
                case "tbTime":
                    tbRow = GetRelativeRow(outOfTenSection: 6, rowSpan: tbRowSpan);
                    break;
                case "tbLife":
                    tbRow = GetRelativeRow(outOfTenSection: 7, rowSpan: tbRowSpan);
                    break;
                default:
                    tbRow = GetRelativeRow(outOfTenSection: 2, rowSpan: tbRowSpan);
                    break;
            }

            Grid.SetRow(textBlock, tbRow);
            Grid.SetColumn(textBlock, tbCol);
            Grid.SetColumnSpan(textBlock, tbColSpan + 1);

            for (int counter = 1; counter <= tbRowSpan; counter++)
            {
                Grid.SetRowSpan(textBlock, counter);
                await Task.Delay(100);
            }
        }

        private int GetRelativeRow(int outOfTenSection, int rowSpan)
        {
            return (_game.Board.RowsCount / 10 * outOfTenSection) - (rowSpan / 2);
        }
        #endregion GUI Events

        #region Command Bar
        private void InitCommandBar(AppBarButton firstButton)
        {
            double leftMargin = (rootLayout.ActualWidth - (CommandBar.SecondaryCommands.Count() * firstButton.ActualWidth)) / 2;
            firstButton.Margin = new Thickness(leftMargin, 0, 0, 0);

            ResetCommandBar();
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            imgAbout.Visibility = Visibility.Collapsed;

            AppBarButton appBarButton = (AppBarButton)sender;

            if (appBarButton.Label.In("Load", "Save", "Settings"))
            {
                CommandBar.IsEnabled = false;
                cnvUser.Visibility = Visibility.Visible;
            }
            else
            {
                appBarButton.IsEnabled = false;
            }

            switch (appBarButton.Label)
            {
                case "Load":
                    ShowLoadGame();
                    break;
                case "Play":
                    StartGame();
                    break;
                case "Pause":
                    PauseGame();
                    break;
                case "Stop":
                    var result = await Utils.ShowMessageDialog("Are you sure you want to end the game?");
                    if (result.Label == "Yes")
                    {
                        StopGame();
                    }
                    else
                    {
                        appBarButton.IsEnabled = true;
                        return;
                    }
                    break;
                case "Save":
                    ShowSaveGame();
                    break;
                case "Settings":
                    ShowSettings();
                    break;
                default:
                    break;
            }

            AdjustCommandBar(appBarButton.Label);
        }

        private void AdjustCommandBar(string command)
        {
            switch (command)
            {
                case "Load":
                    cmdPlay.IsEnabled = true;
                    break;
                case "Play":
                    cmdPause.IsEnabled = true;
                    cmdStop.IsEnabled = true;
                    cmdSave.IsEnabled = true;
                    cmdLoad.IsEnabled = false;
                    CommandBar.IsOpen = false;
                    break;
                case "Pause":
                    cmdPlay.IsEnabled = true;
                    break;
                case "Stop":
                    ResetCommandBar();
                    break;
                case "Save":
                    cmdSave.IsEnabled = true;
                    break;
                case "Settings":
                    break;
                default:
                    break;
            }
        }

        private void CommandBar_Opened(object sender, object e)
        {
            if (_isGameRunning)
            {
                UnsubscribeFromKeyboardEvents();
                _game.Pause();
            }
        }

        private void CommandBar_Closed(object sender, object e)
        {
            if (_isGameRunning && CommandBar.IsEnabled)
            {
                SubscribeToKeyboardEvents();
                _game.Resume();
            }
        }

        private void ResetCommandBar()
        {
            cmdLoad.IsEnabled = true;
            cmdPlay.IsEnabled = true;
            cmdPause.IsEnabled = false;
            cmdStop.IsEnabled = false;
            cmdSave.IsEnabled = false;
            cmdSettings.IsEnabled = true;
        }
        #endregion Command Bar

        private void GameOver(bool win)
        {
            StopGame(isGameOver: true);

            _grdBoard.Children.Add(Utils.CreateTextBlock("tbMessage",
                "G A M E   O V E R", 70, TextBlock_Loaded));

            _grdBoard.Children.Add(Utils.CreateTextBlock("tbResult",
                win ? "You Won!" : "Unfortunately, You Lost...", 30, TextBlock_Loaded));

            _grdBoard.Children.Add(Utils.CreateTextBlock("tbTime",
                string.Format("Game ended in {0:0#}:{1:0#}:{2:0#}",
                    _game.ElapsedTime.Hours, _game.ElapsedTime.Minutes, _game.ElapsedTime.Seconds),
                25, TextBlock_Loaded));

            if (win)
            {
                _grdBoard.Children.Add(Utils.CreateTextBlock("tbLife",
                    string.Format("You have {0} lifes left", _game.Board.Player.LifeCount), 25, TextBlock_Loaded));
            }

            ResetCommandBar();
            CommandBar.IsOpen = true;
        }
    }
}
