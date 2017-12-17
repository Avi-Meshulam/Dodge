using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Enums;
using System.Xml.Linq;
using Windows.UI.Xaml;
using Windows.Storage;
using System.Threading;
using System.Collections.ObjectModel;

namespace Dodge
{
    /// <summary>
    /// Represents a 2D game board, divided into rows and columns, which contains all game's entities
    /// </summary>
    class Board
    {
        public event EventHandler NewEntityCreated;
        public event EventHandler PageSizeChangedEntityCallback;
        public event EventHandler EntityIsHit;
        public event EventHandler EntityIsDead;

        public const int DEFAULT_COLS_COUNT = 100;
        public const int DEFAULT_ROWS_COUNT = 52;
        public const int MIN_FOLLOWING_ENEMIES = 6;
        public const int MAX_FOLLOWING_ENEMIES = 8;

        private const int PLAYER_ID = 1;
        private const int EMPTY_ENTITY_ID = default(int);

        private readonly object _positionGate = new object();

        private Area _playerSafeArea;
        private Position _helperPosition = new Position();
        private int _playerId;
        private int _entitiesPositionedCounter;
        private int _horizontalObstaclesCount;
        private int _verticalObstaclesCount;
        private bool _isLoadedGame;

        // Dictionary key represents entity Id, and can be any unique identifier
        private readonly IDictionary<int, Entity> _entities = new Dictionary<int, Entity>();
        internal readonly IReadOnlyDictionary<int, Entity> Entities;

        public Board(GameMode gameMode = GameMode.Graphic,
            int rowsCount = DEFAULT_ROWS_COUNT, int colsCount = DEFAULT_COLS_COUNT)
        {
            GameMode = gameMode;

            RowsCount = rowsCount;
            ColsCount = colsCount;

            Entities = new ReadOnlyDictionary<int, Entity>(_entities);

            InitPlayerSafeArea();
        }

        public GameMode GameMode { get; private set; }
        public int RowsCount { get; private set; }
        public int ColsCount { get; private set; }
        public int EnemiesCount { get { return _entities.Values.Count(entity => entity.Type == EntityType.Enemy); } }
        public int EnemiesStartCount { get; private set; }
        public int ObstaclesStartCount { get; private set; }
        public bool IsAllEntitiesPositioned { get; private set; }

        private GameLevel _gameLevel;
        public GameLevel GameLevel
        {
            get { return _gameLevel; }
            set
            {
                _gameLevel = value;
                SetEntitiesStepInterval(_gameLevel);
            }
        }

        public Player Player
        {
            get
            {
                if (_entities.ContainsKey(_playerId))
                {
                    return (Player)_entities[_playerId];
                }
                else
                {
                    return null;
                }
            }
        }

        public void StartGame(int enemiesCount, int obstaclesCount)
        {
            if (!_isLoadedGame)
            {
                EnemiesStartCount = enemiesCount;
                ObstaclesStartCount = obstaclesCount;
                CalcObstaclesSpacing(obstaclesCount);
                Enemy.FollowingEnemiesCount = 0;
                CreateEntities();
            }

            StartStopEntities(isStart: true);
        }

        public void PauseGame()
        {
            StartStopEntities(isStart: false);
        }

        public void ResumeGame()
        {
            StartStopEntities(isStart: true);
        }

        public void LoadGame(XElement xEntities)
        {
            CreateEntities(xEntities);
            _isLoadedGame = true;
        }

        private void StartStopEntities(bool isStart = true)
        {
            foreach (var entity in _entities.Values)
            {
                if (entity.Type == EntityType.Enemy)
                {
                    if (isStart)
                    {
                        ((Enemy)entity).Start();
                    }
                    else
                    {
                        ((Enemy)entity).Stop();
                    }
                }
            }
        }

        //Define an area in which enemies will not be positioned at start
        private void InitPlayerSafeArea()
        {
            _playerSafeArea = new Area();
            _playerSafeArea.startRow = RowsCount / 3;
            _playerSafeArea.endRow = RowsCount / 3 * 2;
            _playerSafeArea.startCol = ColsCount / 3;
            _playerSafeArea.endCol = ColsCount / 3 * 2;
        }

        private void CalcObstaclesSpacing(int obstaclesCount)
        {
            double colsToRowsRatio = (double)ColsCount / RowsCount;
            _horizontalObstaclesCount = (int)Math.Round(obstaclesCount / colsToRowsRatio);
            _verticalObstaclesCount = (int)Math.Round((double)obstaclesCount / _horizontalObstaclesCount);
        }

        private void CreateEntities(XElement xEntities = null)
        {
            _entities.Clear();

            if (xEntities != null)
            {
                LoadFromXML(xEntities);
                return;
            }

            CreateEntity(EntityType.Player);

            for (int index = 0; index < EnemiesStartCount; index++)
            {
                CreateEntity(EntityType.Enemy);
            }

            for (int index = 0; index < ObstaclesStartCount; index++)
            {
                CreateEntity(EntityType.Obstacle);
            }

            SetEntitiesStepInterval(GameLevel);
        }

        private Entity CreateEntity(EntityType type, XElement xEntity = null)
        {
            Entity entity = null;
            Position position = null;

            int id;

            if (xEntity == null)
            {
                id = GetNewEntityId(type);
            }
            else
            {
                id = (int)xEntity.Attribute("Id");
                position = new Position((int)xEntity.Attribute("Row"), (int)xEntity.Attribute("Col"));
            }

            switch (type)
            {
                case EntityType.Player:
                    entity = new Player(id, this, position, xEntity);
                    _playerId = entity.Id;
                    break;
                case EntityType.Enemy:
                    entity = new Enemy(id, this, position, xEntity);
                    break;
                case EntityType.Obstacle:
                    entity = new Obstacle(id, this, position, xEntity);
                    break;
                case EntityType.Other:
                    break;
                default:
                    break;
            }

            if (entity != null)
            {
                _entities.Add(entity.Id, entity);
                NewEntityCreated?.Invoke(entity, EventArgs.Empty);
            }

            return entity;
        }

        private int GetNewEntityId(EntityType entityType)
        {
            switch (entityType)
            {
                case EntityType.Player:
                    return PLAYER_ID;
                case EntityType.Enemy:
                    return GetNewEnemyId();
                case EntityType.Obstacle:
                    return GetNewObstacleId();
                case EntityType.Other:
                    break;
                default:
                    break;
            }
            return EMPTY_ENTITY_ID;
        }

        private int GetNewEnemyId()
        {
            int enemyIdStart = PLAYER_ID + 1;

            int id;
            do
            {
                id = Utils.GetRandom(enemyIdStart, enemyIdStart + EnemiesStartCount);
            } while (_entities.ContainsKey(id));
            return id;
        }

        private int GetNewObstacleId()
        {
            int obstacleIdStart = PLAYER_ID + EnemiesStartCount + 1;

            int id;
            do
            {
                id = Utils.GetRandom(obstacleIdStart, obstacleIdStart + ObstaclesStartCount);
            } while (_entities.ContainsKey(id));
            return id;
        }

        private void SetEntitiesStepInterval(GameLevel gameLevel)
        {
            foreach (var entity in _entities.Values)
            {
                if (entity.Type == EntityType.Enemy)
                {
                    switch (_gameLevel)
                    {
                        case GameLevel.Beginner:
                            ((Enemy)entity).StepInterval = Enemy.BEGINNER_STEP_INTERVAL;
                            break;
                        case GameLevel.Intermediate:
                            ((Enemy)entity).StepInterval = Enemy.INTERMEDIATE_STEP_INTERVAL;
                            break;
                        case GameLevel.Expert:
                            ((Enemy)entity).StepInterval = Enemy.EXPERT_STEP_INTERVAL;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public void PositionEntity(Entity entity, bool isLoadedGame = false)
        {
            if (isLoadedGame)
            {
                goto entity_positioned;
            }

            lock (_positionGate)
            {
                int blockingEntityId;

                if (entity.Type == EntityType.Player)
                {
                    //Set player's default start position at center of screen
                    _helperPosition.Row = (RowsCount - entity.RowSpan) / 2;
                    _helperPosition.Col = (ColsCount - entity.ColSpan) / 2;
                    if (TryMoveEntity(entity, _helperPosition, out blockingEntityId, isFirstPositioning: true) != MoveResult.Clear)
                    {
                        throw new ArithmeticException("Error in player positioning");
                    }
                }
                else
                {
                    Area area;

                    if (entity.Type == EntityType.Obstacle)
                    {
                        int positionedObstacles = _entities.Values.Count(e => e.Type == EntityType.Obstacle && e.IsPositioned);
                        int horizontalAreaNumber = positionedObstacles % _horizontalObstaclesCount;
                        int verticalAreaNumber = positionedObstacles / _horizontalObstaclesCount;
                        area.startCol = ColsCount / _horizontalObstaclesCount * horizontalAreaNumber;
                        area.endCol = ColsCount / _horizontalObstaclesCount * (horizontalAreaNumber + 1) - entity.ColSpan;
                        area.startRow = RowsCount / _verticalObstaclesCount * verticalAreaNumber;
                        area.endRow = RowsCount / _verticalObstaclesCount * (verticalAreaNumber + 1) - entity.RowSpan;
                    }
                    else // => entity.Type == EntityType.Enemy
                    {
                        area.startCol = 0;
                        area.endCol = ColsCount;
                        area.startRow = 0;
                        area.endRow = RowsCount;
                    }

                    Area playerSafeArea = _playerSafeArea;
                    playerSafeArea.startCol = _playerSafeArea.startCol - entity.ColSpan;
                    //playerSafeArea.endCol = _playerSafeArea.endCol;
                    playerSafeArea.startRow = _playerSafeArea.startRow - entity.RowSpan;
                    //playerSafeArea.endRow = _playerSafeArea.endRow;

                    do
                    {
                        _helperPosition.SetRandom(area, playerSafeArea);
                    } while (TryMoveEntity(entity, _helperPosition, out blockingEntityId, isFirstPositioning: true) != MoveResult.Clear);

                    if (entity.Type == EntityType.Enemy)
                    {
                        ((Enemy)entity).PositionDone();
                    }
                }
            }

            entity_positioned:

            entity.IsPositioned = true;

            Interlocked.Increment(ref _entitiesPositionedCounter);

            if (_entitiesPositionedCounter == _entities.Count)
            {
                IsAllEntitiesPositioned = true;
            }
        }

        public void MovePlayer(Direction direction)
        {
            if (_entities.ContainsKey(_playerId))
            {
                ((Player)_entities[_playerId]).Move(direction);
            }
        }

        public MoveResult TryMoveEntity(Entity entity, Position newPosition, out int blockingEntityId, bool isFirstPositioning = false)
        {
            MoveResult moveResult = MoveResult.Clear;
            blockingEntityId = EMPTY_ENTITY_ID;

            if (entity.Position.IsEqual(newPosition))
            {
                return MoveResult.Clear;
            }

            moveResult = IsPositionInBoundaries(entity, newPosition);
            if (moveResult != MoveResult.Clear)
            {
                return moveResult;
            }

            moveResult = ScanArea(entity, newPosition, out blockingEntityId, isFirstPositioning);

            if (moveResult != MoveResult.Clear)
            {
                if (isFirstPositioning)
                {
                    return moveResult;
                }

                if (_entities[blockingEntityId].Type.In(EntityType.Player, EntityType.Enemy))
                {
                    EntitiesCollision(entity, _entities[blockingEntityId]);
                }

                return moveResult;
            }

            newPosition.CopyTo(entity.Position);

            return moveResult;
        }

        private MoveResult IsPositionInBoundaries(Entity entity, Position position)
        {
            if ((position.Row < 0 || position.Row + entity.RowSpan > RowsCount) &&
                (position.Col < 0 || position.Col + entity.ColSpan > ColsCount))
            {
                return MoveResult.TotalBlock;
            }

            if (position.Row < 0 || position.Row + entity.RowSpan > RowsCount)
            {
                return MoveResult.RowBlock;
            }

            if (position.Col < 0 || position.Col + entity.ColSpan > ColsCount)
            {
                return MoveResult.ColumnBlock;
            }

            return MoveResult.Clear;
        }

        private MoveResult ScanArea(Entity entity, Position newPosition, out int blockingEntityId, bool isFirstPositioning = false)
        {
            MoveResult moveResult = MoveResult.Clear;
            Position entityPosition = isFirstPositioning ? newPosition : entity.Position;

            blockingEntityId = EMPTY_ENTITY_ID;

            Area scanArea;
            GetScanArea(entity, newPosition, out scanArea, isFirstPositioning);

            foreach (var curEntity in _entities.Values)
            {
                if (curEntity.Id == entity.Id)
                {
                    continue;
                }

                if (scanArea.startCol != scanArea.endCol)
                {
                    if (
                        (
                            (curEntity.Position.Col >= scanArea.startCol &&
                                curEntity.Position.Col <= scanArea.endCol)
                                                            ||
                            (curEntity.Position.Col + curEntity.ColSpan >= scanArea.startCol &&
                                curEntity.Position.Col + curEntity.ColSpan <= scanArea.endCol)
                        )
                                                            &&
                        (
                            (curEntity.Position.Row <= entityPosition.Row &&
                                curEntity.Position.Row + curEntity.RowSpan >= entityPosition.Row)
                                                            ||
                            (entityPosition.Row <= curEntity.Position.Row &&
                                entityPosition.Row + entity.RowSpan >= curEntity.Position.Row)
                        ))
                    {
                        moveResult = MoveResult.ColumnBlock;
                    }
                }

                if (scanArea.startRow != scanArea.endRow)
                {
                    if (
                    (
                        (curEntity.Position.Row >= scanArea.startRow &&
                            curEntity.Position.Row <= scanArea.endRow)
                                                        ||
                        (curEntity.Position.Row + curEntity.RowSpan >= scanArea.startRow &&
                            curEntity.Position.Row + curEntity.RowSpan <= scanArea.endRow)
                    )
                                                        &&
                    (
                        (curEntity.Position.Col <= entityPosition.Col &&
                            curEntity.Position.Col + curEntity.ColSpan >= entityPosition.Col)
                                                        ||
                        (entityPosition.Col <= curEntity.Position.Col &&
                            entityPosition.Col + entity.ColSpan >= curEntity.Position.Col)
                    ))
                    {
                        if (moveResult == MoveResult.ColumnBlock)
                        {
                            moveResult = MoveResult.TotalBlock;
                        }
                        else
                        {
                            moveResult = MoveResult.RowBlock;
                        }
                    }
                }

                if (moveResult != MoveResult.Clear)
                {
                    blockingEntityId = curEntity.Id;
                    break;
                }
            }

            return moveResult;
        }

        private void GetScanArea(Entity entity, Position newPosition, out Area scanArea, bool isFirstPositioning = false)
        {
            scanArea = default(Area);

            if (isFirstPositioning)
            {
                scanArea.startRow = newPosition.Row;
                scanArea.endRow = newPosition.Row + entity.RowSpan;
                scanArea.startCol = newPosition.Col;
                scanArea.endCol = newPosition.Col + entity.ColSpan;
                return;
            }

            GetScanArea(entity.Position.Row, newPosition.Row, entity.RowSpan, out scanArea.startRow, out scanArea.endRow);
            GetScanArea(entity.Position.Col, newPosition.Col, entity.ColSpan, out scanArea.startCol, out scanArea.endCol);
        }

        //The following method controls the extant in which objects can get closer to each other 
        private void GetScanArea(int moveStart, int moveEnd, int span, out int scanStart, out int scanEnd)
        {
            if (moveEnd > moveStart)
            {
                scanStart = moveStart + span;
                scanEnd = moveEnd + span;
            }
            else
            {
                scanStart = moveEnd;
                scanEnd = moveStart;
            }
        }

        private void EntitiesCollision(Entity movingEntity, Entity blockingEntity)
        {
            if (EntityType.Player.In(movingEntity.Type, blockingEntity.Type))
            {
                HitEntity(movingEntity);
                HitEntity(blockingEntity);
            }
            else
            {
                HitEntity(movingEntity);
            }
        }

        private void HitEntity(Entity entity)
        {
            switch (entity.Type)
            {
                case EntityType.Player:
                    if (--((Player)entity).LifeCount <= 0)
                    {
                        KillEntity(entity);
                    }
                    else
                    {
                        EntityIsHit?.Invoke(entity, EventArgs.Empty);
                    }
                    break;
                case EntityType.Enemy:
                    if (--((Enemy)entity).LifeCount <= 0)
                    {
                        KillEntity(entity);
                    }
                    else
                    {
                        EntityIsHit?.Invoke(entity, EventArgs.Empty);
                    }
                    break;
                default:
                    break;
            }
        }

        private void KillEntity(Entity entity)
        {
            _entities.Remove(entity.Id);

            if (entity.Type == EntityType.Enemy)
            {
                ((Enemy)entity).Stop();
            }

            EntityIsDead?.Invoke(entity, EventArgs.Empty);
        }

        public void PageSizeChanged()
        {
            foreach (Entity entity in _entities.Values)
            {
                PageSizeChangedEntityCallback?.Invoke(entity, EventArgs.Empty);
            }
        }

        public XElement GetXML()
        {
            XElement xBoard = new XElement("Board");
            XElement xEntities = new XElement("Entities");
            xBoard.Add(xEntities);

            foreach (Entity entity in _entities.Values)
            {
                xEntities.Add(entity.GetXML());
            }

            return xBoard;
        }

        private void LoadFromXML(XElement xEntities)
        {
            var entities =
                from entity in xEntities.Descendants("Entity")
                select entity;

            foreach (var entity in entities)
            {
                EntityType type = (EntityType)Enum.Parse(typeof(EntityType), entity.Attribute("Type").Value);
                CreateEntity(type, entity);
            }
        }
    }
}
