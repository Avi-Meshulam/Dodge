using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dodge
{
    class Position
    {
        public int Row { get; set; }
        public int Col { get; set; }

        public Position(int row = 0, int col = 0)
        {
            this.Row = row;
            this.Col = col;
        }

        public void SetRandom(Area includedArea, Area excludedArea)
        {
            if(excludedArea.Equals(default(Area)))
            {
                this.Row = Utils.GetRandom(includedArea.startRow, includedArea.endRow);
                this.Col = Utils.GetRandom(includedArea.startCol, includedArea.endCol);
                return;
            }

            int col;
            //do
            //{
                col = Utils.GetRandom(includedArea.startCol, includedArea.endCol);
            //} while (col >= excludedArea.startCol && col <= excludedArea.endCol);

            int row;
            do
            {
                row = Utils.GetRandom(includedArea.startRow, includedArea.endRow);
            } while (row >= excludedArea.startRow && row <= excludedArea.endRow
                && col >= excludedArea.startCol && col <= excludedArea.endCol);

            this.Row = row;
            this.Col = col;
        }

        public void CopyTo(Position position)
        {
            position.Row = this.Row;
            position.Col = this.Col;
        }

        public bool IsEqual(Position position)
        {
            return this.Row == position.Row && this.Col == position.Col;
        }
    }
}
