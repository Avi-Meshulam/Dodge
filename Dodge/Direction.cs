using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Enums;

namespace Dodge
{
    class Direction
    {
        private bool _isPreviousUp;
        private bool _isPreviousLeft;

        public bool Up { get; set; }
        public bool Down { get; set; }
        public bool Left { get; set; }
        public bool Right { get; set; }

        public Direction() { }

        public Direction(bool up, bool down, bool left, bool right)
        {
            Up = up;
            Down = down;
            Left = left;
            Right = right;
        }

        public void SetRandomStraightDirection()
        {
            var rnd = Utils.GetRandom(4);
            switch (rnd)
            {
                case 0:
                    Up = true;
                    break;
                case 1:
                    Down = true;
                    break;
                case 2:
                    Left = true;
                    break;
                case 3:
                    Right = true;
                    break;
                default:
                    break;
            }
        }

        public void SetRandomDiagonalDirection()
        {
            Up = Utils.GetRandom(2) == 0 ? false : true;
            Down = !Up;

            Left = Utils.GetRandom(2) == 0 ? false : true;
            Right = !Left;
        }

        public void Divert(MoveResult moveResult)
        {
            if (Up || Down)
            {
                if (Right || Left)
                {
                    switch (moveResult)
                    {
                        case MoveResult.RowBlock:
                            Up = !Up;
                            Down = !Down;
                            break;
                        case MoveResult.ColumnBlock:
                            Right = !Right;
                            Left = !Left;
                            break;
                        case MoveResult.TotalBlock:
                            Up = !Up;
                            Down = !Down;
                            Right = !Right;
                            Left = !Left;
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    Up = !Up;
                    Down = !Down;
                }
            }
            else
            {
                Right = !Right;
                Left = !Left;
            }
        }

        public void Reset()
        {
            Up = Right = Down = Left = false;
        }

        public void setVerticalDirection(int origin, int target, bool isDiagonal = false)
        {
            if(!isDiagonal)
                Reset();

            if (origin > target)
                Up = true;
            else if (origin < target)
                Up = false;
            else
                Up = _isPreviousUp ? false : true;

            Down = !Up;
            _isPreviousUp = Up;
        }

        public void setHorizontalDirection(int origin, int target, bool isDiagonal = false)
        {
            if (!isDiagonal)
                Reset();

            if (origin > target)
                Left = true;
            else if (origin < target)
                Left = false;
            else
                Left = _isPreviousLeft ? false : true;

            Right = !Left;
            _isPreviousLeft = Left;
        }
    }
}
