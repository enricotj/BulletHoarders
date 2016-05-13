using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public class GridPoint
    {
        public int row;
        public int columm;

        public GridPoint(int row, int column)
        {
            this.row = row;
            this.columm = column;
        }
    }
}
