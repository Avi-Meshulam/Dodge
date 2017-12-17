using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Enums
{
    enum EntityType
    {
        Player = 1,
        Enemy,
        Obstacle,
        Other
    }

    enum MovePattern
    {
        //None = 0,
        FollowingPlayer,
        Straight,
        Diagonal,
        Circular
    }

    enum MovePace
    {
        Constant = 0,
        Quantum,
        Accelerated,
        Decelerated
    }

    enum MoveResult
    {
        Clear = 0,
        RowBlock,
        ColumnBlock,
        TotalBlock
    }

    enum PlayerTarget
    {
        Top = 0,
        Left,
        Bottom,
        Right
    }

    enum GameLevel
    {
        Beginner = 0,
        Intermediate,
        Expert
    }

    enum GameMode
    {
        Console,
        Graphic,
        Other
    }
}
