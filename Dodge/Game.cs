using Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Windows.Storage;
using Windows.UI.Xaml;

namespace Dodge
{
    class Game
    {
        private const int DEFAULT_ENEMIES_COUNT = 10;
        private const int DEFAULT_OBSTACLES_COUNT = 4;

        private DispatcherTimer _gameTimer;

        public Game(GameMode gameMode = GameMode.Graphic, 
            int rowsCount = Board.DEFAULT_ROWS_COUNT, int colsCount = Board.DEFAULT_COLS_COUNT)
        {
            InitTimer();
            Board = new Board(gameMode, rowsCount, colsCount);
        }

        private void InitTimer()
        {
            _gameTimer = new DispatcherTimer();
            _gameTimer.Interval = new TimeSpan(0, 0, 1);
            _gameTimer.Tick += _gameTimer_Tick;
        }

        private void _gameTimer_Tick(object sender, object e)
        {
            ElapsedTime = ElapsedTime.Add(TimeSpan.FromSeconds(1.0));
        }

        public Board Board { get; private set; }
        public TimeSpan ElapsedTime { get; private set; }
        public GameMode Mode { get; set; }

        private GameLevel _level;
        public GameLevel Level {
            get { return _level; }
            set
            {
                _level = value;
                Board.GameLevel = _level;
            }
        }

        public void Start(int enemiesCount = DEFAULT_ENEMIES_COUNT,
            int obstaclesCount = DEFAULT_OBSTACLES_COUNT)
        {
            Board.StartGame(enemiesCount, obstaclesCount);
            _gameTimer.Start();
        }

        public void Resume()
        {
            Board.ResumeGame();
            _gameTimer.Start();
        }

        public void Load(XElement xGame)
        {
            int rowsCount = (int)xGame.Attribute("RowsCount");
            int colsCount = (int)xGame.Attribute("ColsCount");

            if (xGame.Attribute("Level") != null &&
                Enum.IsDefined(typeof(GameLevel), xGame.Attribute("Level").Value))
            {
                Level = (GameLevel)Enum.Parse(typeof(GameLevel), xGame.Attribute("Level").Value);
            }

            int elapsedTime = (int)xGame.Attribute("ElapsedTime");
            ElapsedTime = new TimeSpan(0, 0, elapsedTime);

            XElement xEntities = xGame.Descendants("Entities").First();
            Board.LoadGame(xEntities);
        }

        public void Pause()
        {
            Board.PauseGame();
            _gameTimer.Stop();
        }

        public void Stop()
        {
            Pause();
        }

        public XElement GetXML()
        {
            XElement xGame =
                new XElement("Game",
                    new XAttribute("Level", Level),
                    new XAttribute("ElapsedTime", ElapsedTime.Seconds),
                    new XAttribute("RowsCount", Board.RowsCount),
                    new XAttribute("ColsCount", Board.ColsCount)
                );

            xGame.Add(Board.GetXML());

            return xGame;
        }
    }
}
