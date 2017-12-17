using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Enums;
using System.Xml.Linq;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Controls;

namespace Dodge
{
    class Player : Entity
    {
        public override event EventHandler PositionChanged;

        private const int DEFAULT_LIFE_COUNT = 10;
        private const int DEFAULT_STEP_SIZE = 1;

        private int _stepSize;  //Number of rows/columns for each step
        private Position _movePosition = new Position();

        public Player(int id, Board board, Position position = null, XElement xEntity = null)
            : base(id, board, EntityType.Player, position)
        {
            if (xEntity != null)
            {
                LoadFromXML(xEntity);
            }
            else
            {
                _stepSize = DEFAULT_STEP_SIZE;
                LifeCount = DEFAULT_LIFE_COUNT;
            }
        }

        public int LifeCount { get; set; }

        public void Move(Direction direction = null)
        {
            if (!IsPositioned && Board.GameMode != GameMode.Graphic)
            {
                Board.PositionEntity(this);
            }

            if (!Board.IsAllEntitiesPositioned)
            {
                return;
            }

            SetMovePosition(direction);

            int blockingEntityId;
            if (Board.TryMoveEntity(this, _movePosition, out blockingEntityId) == MoveResult.Clear)
            {
                PositionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void SetMovePosition(Direction direction)
        {
            Position.CopyTo(_movePosition);

            if (direction.Down)
            {
                _movePosition.Row += _stepSize;
            }
            if (direction.Left)
            {
                _movePosition.Col -= _stepSize;
            }
            if (direction.Right)
            {
                _movePosition.Col += _stepSize;
            }
            if (direction.Up)
            {
                _movePosition.Row -= _stepSize;
            }
        }

        private void LoadFromXML(XElement xEntity)
        {
            _stepSize = (int)xEntity.Attribute("StepSize");
            LifeCount = (int)xEntity.Attribute("Life");
            if (xEntity.Attribute("ImageURI") != null)
            {
                Image.Source = new BitmapImage(new Uri(xEntity.Attribute("ImageURI").Value));
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
                    new XAttribute("StepSize", _stepSize)
                )
            );

            string imageURI = ((BitmapImage)Image.Source).UriSource.AbsoluteUri;
            if (imageURI != "")
            {
                xElement.Add(new XAttribute("ImageURI", imageURI));
            }

            return xElement;
        }
    }
}
