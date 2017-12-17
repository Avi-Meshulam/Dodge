using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Enums;
using System.Diagnostics;
using System.Xml.Linq;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Controls;

namespace Dodge
{
    enum BlockMode
    {
        None,
        Column,
        Row,
        Total
    }

    class Enemy : Entity
    {
        public override event EventHandler PositionChanged;

        internal static int FollowingEnemiesCount;

        public const int BEGINNER_STEP_INTERVAL = 100;
        public const int INTERMEDIATE_STEP_INTERVAL = 20;
        public const int EXPERT_STEP_INTERVAL = 4;

        private const int DEFAULT_LIFE_COUNT = 3;
        private const int DEFAULT_STEP_SIZE = 1;
        private const int DEFAULT_STEP_QUANTUM = 10;
        private const int MAX_DIVERSIONS = 3;

        private DispatcherTimer _dispatcherTimer;
        private readonly Position _movePosition = new Position();
        private readonly Direction _moveDirection = new Direction();
        private MovePattern _movePattern;
        private MovePace _movePace;
        private Position _circleCenter;
        private PlayerTarget _playerTarget;
        private int _stepSize;   //Number of rows/columns for each step
        private int _minStepInterval;
        private int _maxStepInterval;
        private int _stepQuantum;
        private int _stepQuantumCounter;
        private int _diversionCounter;
        private int _moveRadius;
        private bool _isClockwise;
        private bool _isQuantumActive;

        private BlockMode _lastFollowingPlayerBlockMode;
        private BlockMode _followingPlayerBlockMode;
        private BlockMode FollowingPlayerBlockMode
        {
            get { return _followingPlayerBlockMode; }
            set
            {
                _lastFollowingPlayerBlockMode = _followingPlayerBlockMode;
                _followingPlayerBlockMode = value;
            }
        }

        public Enemy(int id, Board board, Position position = null, XElement xEntity = null)
            : base(id, board, EntityType.Enemy, position)
        {
            InitTimer();

            if (xEntity != null)
            {
                LoadFromXML(xEntity);
            }
            else
            {
                InitMovePattern();
                InitMovePace();
                InitMoveDirection();

                _stepSize = DEFAULT_STEP_SIZE;
                _playerTarget = PlayerTarget.Top;
                LifeCount = DEFAULT_LIFE_COUNT;
                CurrentStepInterval = StepInterval = BEGINNER_STEP_INTERVAL;
            }
        }

        public int LifeCount { get; set; }

        //Time in milliseconds between steps
        private int _stepInterval;
        public int StepInterval
        {
            get { return _stepInterval; }
            set
            {
                if (_stepInterval != value)
                {
                    _stepInterval = value;
                    _dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, _stepInterval);

                    if (_movePace.In(MovePace.Accelerated, MovePace.Decelerated))
                    {
                        SetChangingStepIntervalParams();
                    }
                }
            }
        }

        public void Start()
        {
            _dispatcherTimer.Start();
        }

        public void Stop()
        {
            _dispatcherTimer.Stop();
        }

        internal void PositionDone()
        {
            if (_movePattern == MovePattern.Circular)
            {
                InitMoveCircle();
            }
        }

        private void InitTimer()
        {
            _dispatcherTimer = new DispatcherTimer();
            //_dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, StepInterval);
            _dispatcherTimer.Tick += DispatcherTimer_Tick;
        }

        private int _currentStepInterval;
        private int CurrentStepInterval
        {
            get { return _currentStepInterval; }
            set
            {
                _currentStepInterval = value;
                if (_dispatcherTimer != null)
                {
                    _dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, _currentStepInterval);
                }
            }
        }

        private void SetChangingStepIntervalParams()
        {
            if (_movePace == MovePace.Accelerated)
            {
                _currentStepInterval = _maxStepInterval = _stepInterval;
                _minStepInterval = _stepInterval / 4;
            }
            else if (_movePace == MovePace.Decelerated)
            {
                _currentStepInterval = _minStepInterval = _stepInterval;
                _maxStepInterval = _stepInterval * 2;
            }
        }

        private void InitMovePattern()
        {
            int maxMovePatternValue = (int)Enum.GetValues(typeof(MovePattern)).Cast<MovePattern>().Max();

            bool done = false;
            do
            {
                int movePatternValue = Utils.GetRandom(1, maxMovePatternValue + 1);
                if (Enum.IsDefined(typeof(MovePattern), movePatternValue))
                {
                    if (movePatternValue == (int)MovePattern.FollowingPlayer)
                    {
                        if (Enemy.FollowingEnemiesCount >= Board.MAX_FOLLOWING_ENEMIES)
                        {
                            continue;
                        }
                        else
                        {
                            Enemy.FollowingEnemiesCount++;
                        }
                    }
                    else
                    {
                        if (Enemy.FollowingEnemiesCount < Board.MIN_FOLLOWING_ENEMIES &&
                            Board.EnemiesStartCount - Board.EnemiesCount <= Board.MIN_FOLLOWING_ENEMIES)
                        {
                            movePatternValue = (int)MovePattern.FollowingPlayer;
                            Enemy.FollowingEnemiesCount++;
                        }
                    }

                    _movePattern = (MovePattern)movePatternValue;
                    done = true;
                }
                else
                {
                    continue;
                }
            } while (!done);
        }

        private void InitMovePace()
        {
            if (_movePattern == MovePattern.FollowingPlayer)
            {
                _movePace = MovePace.Constant;
                return;
            }

            int maxMovePaceValue = (int)Enum.GetValues(typeof(MovePace)).Cast<MovePace>().Max();
            int movePaceValue = Utils.GetRandom(maxMovePaceValue + 1);
            if (Enum.IsDefined(typeof(MovePace), movePaceValue))
            {
                _movePace = (MovePace)movePaceValue;
                if (_movePace == MovePace.Quantum)
                {
                    _stepQuantum = Utils.GetRandom(DEFAULT_STEP_QUANTUM / 2, DEFAULT_STEP_QUANTUM + 1);
                }
            }
        }

        private void InitMoveDirection()
        {
            switch (_movePattern)
            {
                case MovePattern.Straight:
                    _moveDirection.SetRandomStraightDirection();
                    break;
                case MovePattern.Diagonal:
                    _moveDirection.SetRandomDiagonalDirection();
                    break;
                default:
                    break;
            }
        }

        private void InitMoveCircle()
        {
            if (_circleCenter != null)
            {
                return;
            }

            _isClockwise = true;

            int rowSpan = (Position.Row > Board.RowsCount / 2) ? 0 : RowSpan;
            int colSpan = (Position.Col > Board.ColsCount / 2) ? 0 : ColSpan;

            _moveRadius = Math.Min(
                Math.Max(Position.Row / 2, (Board.RowsCount - 1 - Position.Row - rowSpan) / 2),
                Math.Max(Position.Col / 2, (Board.ColsCount - 1 - Position.Col - colSpan) / 2)
            );

            _circleCenter = new Position();

            _circleCenter.Row = (Position.Row + rowSpan + (_moveRadius * 2) <= Board.RowsCount) ?
                Position.Row + _moveRadius : Position.Row - _moveRadius;
            _circleCenter.Col = (Position.Col + colSpan + (_moveRadius * 2) <= Board.ColsCount) ?
                Position.Col + _moveRadius : Position.Col - _moveRadius;
        }

        private void DispatcherTimer_Tick(object sender, object e)
        {
            if (!IsPositioned && Board.GameMode != GameMode.Graphic)
            {
                Board.PositionEntity(this);
            }

            if (!Board.IsAllEntitiesPositioned)
            {
                return;
            }

            switch (_movePace)
            {
                case MovePace.Constant:
                    Move();
                    break;
                case MovePace.Quantum:
                    DoQuantumMove();
                    break;
                case MovePace.Accelerated:
                    DoAcceleratedMove();
                    break;
                case MovePace.Decelerated:
                    DoDeceleratedMove();
                    break;
                default:
                    break;
            }
        }

        private void Move()
        {
            SetMovePosition(_movePattern);

            int blockingEntityId;
            MoveResult moveResult = Board.TryMoveEntity(this, _movePosition, out blockingEntityId);
            if (moveResult == MoveResult.Clear)
            {
                PositionChanged?.Invoke(this, EventArgs.Empty);

                if (_followingPlayerBlockMode != BlockMode.None)
                {
                    if (_followingPlayerBlockMode == BlockMode.Total)
                    {
                        if (_lastFollowingPlayerBlockMode == BlockMode.Row)
                        {
                            setFollowingPlayerParamsByMoveResult(MoveResult.ColumnBlock);
                        }
                        else
                        {
                            setFollowingPlayerParamsByMoveResult(MoveResult.RowBlock);
                        }
                    }
                    else
                    {
                        // Check if block has opened
                        SetMovePosition(MovePattern.FollowingPlayer);
                        if (MoveResult.Clear == Board.TryMoveEntity(this, _movePosition, out blockingEntityId))
                        {
                            PositionChanged?.Invoke(this, EventArgs.Empty);
                            FollowingPlayerBlockMode = BlockMode.None;
                            _movePattern = MovePattern.FollowingPlayer;
                        }
                    }
                }
                return;
            }

            // => moveResult != MoveResult.Clear

            if (_movePattern == MovePattern.FollowingPlayer || _followingPlayerBlockMode != BlockMode.None)
            {
                if (_lastFollowingPlayerBlockMode != BlockMode.None)
                    ChangePlayerTarget();
                setFollowingPlayerParamsByMoveResult(moveResult);
                return;
            }

            if (_movePattern == MovePattern.Circular)
            {
                _isClockwise = !_isClockwise;
                return;
            }

            _moveDirection.Divert(moveResult);

            // Limit amount of recursions to avoid stack overflow
            if (_diversionCounter > MAX_DIVERSIONS)
            {
                return;
            }

            _diversionCounter++;

            Move();

            if (_diversionCounter > 0)
            {
                _diversionCounter--;
            }
        }

        private void setFollowingPlayerParamsByMoveResult(MoveResult moveResult)
        {
            _lastFollowingPlayerBlockMode = _followingPlayerBlockMode;

            SetMovePosition(MovePattern.FollowingPlayer);

            switch (moveResult)
            {
                case MoveResult.RowBlock:
                    FollowingPlayerBlockMode = BlockMode.Row;
                    _movePattern = MovePattern.Straight;
                    _moveDirection.setHorizontalDirection(Position.Col, _movePosition.Col);
                    break;
                case MoveResult.ColumnBlock:
                    FollowingPlayerBlockMode = BlockMode.Column;
                    _movePattern = MovePattern.Straight;
                    _moveDirection.setVerticalDirection(Position.Row, _movePosition.Row);
                    break;
                case MoveResult.TotalBlock:
                    FollowingPlayerBlockMode = BlockMode.Total;
                    _movePattern = MovePattern.Diagonal;
                    _moveDirection.setHorizontalDirection(Position.Col, _movePosition.Col, isDiagonal: true);
                    _moveDirection.setVerticalDirection(Position.Row, _movePosition.Row, isDiagonal: true);
                    _moveDirection.Divert(moveResult);
                    break;
                default:
                    break;
            }
        }

        private void DoQuantumMove()
        {
            if (_isQuantumActive)
            {
                Move();
                if (--_stepQuantumCounter <= 0)
                {
                    _isQuantumActive = false;
                }
            }
            else
            {
                if (_stepQuantumCounter % _stepQuantum == 0 && _stepQuantumCounter != 0)
                {
                    _stepQuantumCounter *= 2;
                    _isQuantumActive = true;
                }
                else
                {
                    _stepQuantumCounter++;
                }
            }
        }

        private void DoAcceleratedMove()
        {
            int newVal = (int)Math.Round(CurrentStepInterval * 0.8);
            if (newVal >= _minStepInterval)
            {
                CurrentStepInterval = newVal;
            }
            else
            {
                CurrentStepInterval = StepInterval;
            }
            Move();
        }

        private void DoDeceleratedMove()
        {
            int newVal = (int)Math.Round(CurrentStepInterval * 1.12);
            if (newVal <= _maxStepInterval)
            {
                CurrentStepInterval = newVal;
            }
            else
            {
                CurrentStepInterval = StepInterval;
            }
            Move();
        }

        private void SetMovePosition(MovePattern movePattern, Direction moveDirection = null)
        {
            switch (movePattern)
            {
                case MovePattern.FollowingPlayer:
                    SetFollowingPlayerMovePosition();
                    break;
                case MovePattern.Circular:
                    SetCircularMovePosition();
                    break;
                default:
                    SetInDirectionMovePosition();
                    break;
            }
        }

        private void SetCircularMovePosition()
        {
            if (_circleCenter == null)
            {
                Position.CopyTo(_movePosition);
                return;
            }

            //Set row and col relative to circle's center
            int col = Position.Col - _circleCenter.Col;
            double row = _circleCenter.Row - Position.Row;

            if (_moveRadius == 0)
            {
                _moveRadius = (int)Math.Ceiling(
                    Math.Sqrt(
                        Math.Pow(Math.Abs(col), 2) +
                        Math.Pow(Math.Abs(row), 2)
                    )
                );
            }

            bool isRowPositive;
            if (row > 0 && Math.Abs(col) < _moveRadius)
            {
                col += _isClockwise ? 1 : -1;
                isRowPositive = true;
            }
            else
            {
                if (row < 0 && Math.Abs(col) < _moveRadius)
                {
                    col += _isClockwise ? -1 : 1;
                    isRowPositive = false;
                }
                else
                {
                    if (col > 0)
                    {
                        col -= 1;
                        isRowPositive = !_isClockwise;
                    }
                    else
                    {
                        col += 1;
                        isRowPositive = _isClockwise;
                    }
                }
            }

            // Calculate row according to Circle formula: X^2 + Y^2 = R^2 (X: Col, Y: Row)
            row = Math.Sqrt(Math.Pow(_moveRadius, 2) - Math.Pow(col, 2));

            if (!isRowPositive)
            {
                row = -row;
            }

            _movePosition.Col = _circleCenter.Col + col;
            _movePosition.Row = _circleCenter.Row - (int)Math.Round(row);
        }

        private void SetInDirectionMovePosition()
        {
            Position.CopyTo(_movePosition);

            if (_moveDirection.Down)
            {
                _movePosition.Row += _stepSize;
            }
            if (_moveDirection.Left)
            {
                _movePosition.Col -= _stepSize;
            }
            if (_moveDirection.Right)
            {
                _movePosition.Col += _stepSize;
            }
            if (_moveDirection.Up)
            {
                _movePosition.Row -= _stepSize;
            }
        }

        private void SetFollowingPlayerMovePosition()
        {
            // No Player => Stay in place
            if (Board.Player == null)
            {
                Position.CopyTo(_movePosition);
                return;
            }

            switch (_playerTarget)
            {
                case PlayerTarget.Top:
                    _movePosition.Row = GetCoordinateTowardsTarget(this.Position.Row, Board.Player.Position.Row - this.RowSpan);
                    _movePosition.Col = GetCoordinateTowardsTarget(this.Position.Col, Board.Player.Position.Col);
                    break;
                case PlayerTarget.Left:
                    _movePosition.Row = GetCoordinateTowardsTarget(this.Position.Row, Board.Player.Position.Row);
                    _movePosition.Col = GetCoordinateTowardsTarget(this.Position.Col, Board.Player.Position.Col - this.ColSpan);
                    break;
                case PlayerTarget.Bottom:
                    _movePosition.Row = GetCoordinateTowardsTarget(this.Position.Row, Board.Player.Position.Row + Board.Player.RowSpan);
                    _movePosition.Col = GetCoordinateTowardsTarget(this.Position.Col, Board.Player.Position.Col);
                    break;
                case PlayerTarget.Right:
                    _movePosition.Row = GetCoordinateTowardsTarget(this.Position.Row, Board.Player.Position.Row);
                    _movePosition.Col = GetCoordinateTowardsTarget(this.Position.Col, Board.Player.Position.Col + Board.Player.ColSpan);
                    break;
                default:
                    break;
            }
        }

        private int GetCoordinateTowardsTarget(int source, int target)
        {
            if (target != source)
            {
                int step = Math.Min(Math.Abs(target - source), _stepSize);

                if (target > source)
                {
                    return source + step;
                }
                else
                {
                    return source - step;
                }
            }
            return source;
        }

        private void ChangePlayerTarget()
        {
            if (Board.Player == null)
                return;

            switch (_playerTarget)
            {
                case PlayerTarget.Top:
                    _playerTarget = PlayerTarget.Right;
                    break;
                case PlayerTarget.Right:
                    _playerTarget = PlayerTarget.Bottom;
                    break;
                case PlayerTarget.Bottom:
                    _playerTarget = PlayerTarget.Left;
                    break;
                case PlayerTarget.Left:
                    _playerTarget = PlayerTarget.Top;
                    break;
                default:
                    break;
            }
        }

        public override XElement GetXML()
        {
            XElement xElement = (
                new XElement("Entity",
                    new XAttribute("Id", Id),
                    new XAttribute("Type", Type),
                    new XAttribute("Row", Position.Row),
                    new XAttribute("Col", Position.Col),
                    new XAttribute("Life", LifeCount),
                    new XAttribute("StepSize", _stepSize),
                    new XAttribute("StepInterval", StepInterval),
                    new XAttribute("MovePattern", _movePattern),
                    new XAttribute("MovePace", _movePace)
                )
            );

            switch (_movePattern)
            {
                case MovePattern.FollowingPlayer:
                    xElement.Add(new XAttribute("PlayerTarget", _playerTarget));
                    break;
                case MovePattern.Straight:
                case MovePattern.Diagonal:
                    xElement.Add(
                        new XElement("MoveDirection",
                            new XAttribute("Up", _moveDirection.Up),
                            new XAttribute("Left", _moveDirection.Left),
                            new XAttribute("Right", _moveDirection.Right),
                            new XAttribute("Down", _moveDirection.Down)
                        )
                    );
                    break;
                case MovePattern.Circular:
                    xElement.Add(
                        new XAttribute("CenterRow", _circleCenter.Row),
                        new XAttribute("CenterCol", _circleCenter.Col),
                        new XAttribute("_isClockwise", _isClockwise)
                    );
                    break;
                default:
                    break;
            }

            if (_movePace == MovePace.Quantum)
            {
                xElement.Add(
                    new XAttribute("StepQuantum", _stepQuantum),
                    new XAttribute("StepQuantumCounter", _stepQuantumCounter)
                );
            }

            string imageURI = ((BitmapImage)Image.Source).UriSource.AbsoluteUri;
            if (imageURI != "")
            {
                xElement.Add(new XAttribute("ImageURI", imageURI));
            }

            return xElement;
        }

        private void LoadFromXML(XElement xEntity)
        {
            LifeCount = (int)xEntity.Attribute("Life");
            _stepSize = (int)xEntity.Attribute("StepSize");
            _movePattern = xEntity.Attribute("MovePattern") != null ?
                (MovePattern)Enum.Parse(typeof(MovePattern), xEntity.Attribute("MovePattern").Value) : _movePattern;
            int centerRow = xEntity.Attribute("CenterRow") != null ?
                int.Parse(xEntity.Attribute("CenterRow").Value) : -1;
            int centerCol = xEntity.Attribute("CenterCol") != null ?
                int.Parse(xEntity.Attribute("CenterCol").Value) : -1;
            if (centerRow != -1 && centerCol != -1)
            {
                _circleCenter = new Position(centerRow, centerCol);
            }
            _isClockwise = xEntity.Attribute("_isClockwise") != null ?
                bool.Parse(xEntity.Attribute("_isClockwise").Value) : _isClockwise;
            _movePace = xEntity.Attribute("MovePace") != null ?
                (MovePace)Enum.Parse(typeof(MovePace), xEntity.Attribute("MovePace").Value) : _movePace;
            StepInterval = xEntity.Attribute("StepInterval") != null ?
                int.Parse(xEntity.Attribute("StepInterval").Value) : StepInterval;
            _playerTarget = xEntity.Attribute("PlayerTarget") != null ?
                (PlayerTarget)Enum.Parse(typeof(PlayerTarget), xEntity.Attribute("PlayerTarget").Value) : _playerTarget;
            _stepQuantum = xEntity.Attribute("StepQuantum") != null ?
                int.Parse(xEntity.Attribute("StepQuantum").Value) : _stepQuantum;
            _stepQuantumCounter = xEntity.Attribute("StepQuantumCounter") != null ?
                int.Parse(xEntity.Attribute("StepQuantumCounter").Value) : _stepQuantumCounter;
            XElement xDirection = xEntity.Element("MoveDirection");
            if (xDirection != null)
            {
                _moveDirection.Up = (bool)xDirection.Attribute("Up");
                _moveDirection.Down = (bool)xDirection.Attribute("Down");
                _moveDirection.Left = (bool)xDirection.Attribute("Left");
                _moveDirection.Right = (bool)xDirection.Attribute("Right");
            }
            if (xEntity.Attribute("ImageURI") != null)
            {
                Image.Source = new BitmapImage(new Uri(xEntity.Attribute("ImageURI").Value));
            }
        }
    }
}
