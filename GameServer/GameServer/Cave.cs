using System;
using System.Collections;
using System.Collections.Generic;

using GameServer;

namespace GameServer
{
    public class Cave
    {
        private byte[] data;

        public CellType[,] grid;
        public static int caveWidth = 100; // 0 < w <= 255
        public static int caveHeight = 80; // 0 < h <= 255

        // cave generation variables
        private float fillRatio = 0.45f;
        private const int NUM_STEPS = 25;
        private const int DEATH_LIM = 3;
        private const int BIRTH_LIM = 4;
        private const int REVIVE_NUM = 3;

        // de-cheesing variables
        private CellType[,] pgrid;
        private ArrayList fill = new ArrayList();
        private int cheeseThresh;

        private List<GridPoint> empty = new List<GridPoint>();

        private Random rng;

        // Use this for initialization
        public Cave()
        {
            data = null;
            rng = new Random();
            fillRatio = (float)(rng.NextDouble() * 0.04 + 0.44);
            cheeseThresh = (int)Math.Floor(caveWidth * caveHeight / 6.0f);
            grid = new CellType[caveWidth, caveHeight];

            // randomize grid
            for (int c = 0; c < caveWidth; c++)
            {
                for (int r = 0; r < caveHeight; r++)
                {
                    if (rng.NextDouble() < fillRatio)
                    {
                        grid[c, r] = CellType.Filled;
                    }
                    else
                    {
                        grid[c, r] = CellType.Empty;
                    }
                }
            }

            // generate cave level
            for (int i = 0; i < NUM_STEPS; i++)
            {
                GenerateCave();
            }

            //fill borders
            for (var r = 0; r < caveHeight; r++)
            {
                grid[0, r] = CellType.Filled;
                grid[caveWidth - 1, r] = CellType.Filled;
            }
            for (var c = 0; c < caveWidth; c++)
            {
                grid[c, 0] = CellType.Filled;
                grid[c, caveHeight - 1] = CellType.Filled;
            }

            // decheese
            pgrid = new CellType[caveWidth, caveHeight];
            pgrid = (CellType[,])grid.Clone();
            DeCheeseCave();

            // Find/Set Slants/Edges
            FindSlants();
            FindEdges();
        }

        public bool Collision(float x, float y)
        {
            int col = (int)x;
            int row = (int)y;

            switch (grid[col, row])
            {
                case CellType.SlantNE:
                    if (x - col + y - row >= 1)
                    {
                        return false;
                    }
                    break;
                case CellType.SlantNW:
                    if (y - row >= x - col)
                    {
                        return false;
                    }
                    break;
                case CellType.SlantSE:
                    if (y - row <= x - col)
                    {
                        return false;
                    }
                    break;
                case CellType.SlantSW:
                    if (x - col + y - row <= 1)
                    {
                        return false;
                    }
                    break;

                case CellType.Empty:
                    return false;

                default:
                    return true;
            }

            return true;
        }

        public CellType Get(int c, int r)
        {
            return grid[c, r];
        }

        public byte[] GetBytes()
        {
            if (data == null)
            {
                data = new byte[caveWidth * caveHeight + 2];

                // first entry is the length of each row
                data[0] = (byte)caveWidth;
                data[1] = (byte)caveHeight;
                int i = 2;

                for (int r = 0; r < caveHeight; r++)
                {
                    for (int c = 0; c < caveWidth; c++)
                    {
                        data[i] = (byte)grid[c, r];
                        i++;
                        //Console.Write(((int)grid[c, r]) + "\t");
                    }
                    //Console.WriteLine();
                }
            }
            return data;
        }

        public GridPoint GetSpawnPoint()
        {
            return empty[rng.Next(empty.Count)];
        }

        #region Generator
        private void GenerateCave()
        {
            CellType[,] newGrid = new CellType[caveWidth, caveHeight];
            for (int r = 0; r < caveHeight; r++)
            {
                for (int c = 0; c < caveWidth; c++)
                {
                    float nbs = CountAliveNeighbors(r, c);
                    if (grid[c, r] == CellType.Filled)
                    {
                        if (nbs < DEATH_LIM)
                        {
                            newGrid[c, r] = CellType.Empty;
                        }
                        else
                        {
                            newGrid[c, r] = CellType.Filled;
                        }
                    }
                    else
                    {
                        if (nbs > BIRTH_LIM)
                        {
                            newGrid[c, r] = CellType.Filled;
                        }
                        else
                        {
                            newGrid[c, r] = CellType.Empty;
                        }
                    }
                }
            }
            for (int r = 0; r < caveHeight; r++)
            {
                for (int c = 0; c < caveWidth; c++)
                {
                    grid[c, r] = newGrid[c, r];
                }
            }
            //grid = newGrid;
            //grid = (Cell[,]) newGrid.Clone();
        }

        private void DeCheeseCave()
        {
            for (int r = 1; r < caveHeight - 1; r++)
            {
                for (int c = 1; c < caveWidth - 1; c++)
                {
                    if (pgrid[c, r] == CellType.Empty)
                    {
                        if (GetCavernSize(r, c) > cheeseThresh)
                        {
                            for (var i = 0; i < fill.Count; i++)
                            {
                                GridPoint p = (GridPoint)fill[i];
                                grid[p.columm, p.row] = CellType.Empty;
                            }
                        }
                    }
                    fill.Clear();
                }
            }
        }

        private int GetCavernSize(int r, int c)
        {
            int i = 0;
            Queue q = new Queue();
            q.Enqueue(new GridPoint(r, c));
            while (q.Count > 0)
            {
                GridPoint p = (GridPoint)q.Dequeue();
                int row = p.row;
                int col = p.columm;
                if (pgrid[col, row] == CellType.Empty)
                {
                    i++;
                    fill.Add(new GridPoint(row, col));
                    grid[col, row] = CellType.Filled;
                    pgrid[col, row] = CellType.Filled;
                    q.Enqueue(new GridPoint(row, col + 1));
                    q.Enqueue(new GridPoint(row - 1, col));
                    q.Enqueue(new GridPoint(row, col - 1));
                    q.Enqueue(new GridPoint(row + 1, col));
                }
            }
            return i;
        }

        private void FindSlants()
        {
            for (int r = 0; r < caveHeight; r++)
            {
                for (int c = 0; c < caveWidth; c++)
                {
                    if (grid[c, r] == CellType.Empty)
                    {
                        SetSlant(r, c);
                    }
                }
            }
        }

        private void FindEdges()
        {
            for (int r = 0; r < caveHeight; r++)
            {
                for (int c = 0; c < caveWidth; c++)
                {
                    CellType cell = grid[c, r];
                    if (cell == CellType.Filled)
                    {
                        if (!SetSlantFilled(r, c))
                        {
                            SetEdge(r, c);
                        }
                    }
                    else if (cell == CellType.Empty)
                    {
                        // TODO: add to empty list
                        empty.Add(new GridPoint(r, c));
                    }
                }
            }
        }

        private bool SetSlant(int r, int c)
        {
            if (CountAliveNeighbors(r, c) >= 2)
            {
                if (c > 0 && r > 0 && grid[c - 1, r] == CellType.Filled && grid[c, r - 1] == CellType.Filled
                        && grid[c, r + 1] != CellType.Filled && grid[c + 1, r] != CellType.Filled)
                {
                    grid[c, r] = CellType.SlantNE;
                    return true;
                }
                else if (c > 0 && r < caveHeight - 1 && grid[c - 1, r] == CellType.Filled && grid[c, r + 1] == CellType.Filled
                        && grid[c, r - 1] != CellType.Filled && grid[c + 1, r] != CellType.Filled)
                {
                    grid[c, r] = CellType.SlantSE;
                    return true;
                }
                else if (c < caveWidth - 1 && r < caveHeight - 1 && grid[c + 1, r] == CellType.Filled && grid[c, r + 1] == CellType.Filled
                        && grid[c, r - 1] != CellType.Filled && grid[c - 1, r] != CellType.Filled)
                {
                    grid[c, r] = CellType.SlantSW;
                    return true;
                }
                else if (c < caveWidth - 1 && r > 0 && grid[c + 1, r] == CellType.Filled && grid[c, r - 1] == CellType.Filled
                        && grid[c, r + 1] != CellType.Filled && grid[c - 1, r] != CellType.Filled)
                {
                    grid[c, r] = CellType.SlantNW;
                    return true;
                }
            }
            return false;
        }

        private bool CellIsFilled(int c, int r)
        {
            int cell = (int)grid[c, r];
            return !((cell >= 2 && cell <= 5) || cell == 0);
        }

        private bool SetSlantFilled(int r, int c)
        {
            if (CountAliveNeighborsInt(r, c) >= 2)
            {
                if (c > 0 && r > 0 && c < caveWidth - 1 && r < caveHeight - 1
                        && CellIsFilled(c - 1, r)
                        && CellIsFilled(c, r - 1)
                        && grid[c, r + 1] == CellType.Empty
                        && grid[c + 1, r] == CellType.Empty)
                {
                    grid[c, r] = CellType.SlantNE;
                    return true;
                }
                else if (c > 0 && r > 0 && c < caveWidth - 1 && r < caveHeight - 1
                        && CellIsFilled(c - 1, r)
                        && CellIsFilled(c, r + 1)
                        && grid[c, r - 1] == CellType.Empty
                        && grid[c + 1, r] == CellType.Empty)
                {
                    grid[c, r] = CellType.SlantSE;
                    return true;
                }
                else if (c > 0 && r > 0 && c < caveWidth - 1 && r < caveHeight - 1
                        && CellIsFilled(c + 1, r)
                        && CellIsFilled(c, r + 1)
                        && grid[c, r - 1] == CellType.Empty
                        && grid[c - 1, r] == CellType.Empty)
                {
                    grid[c, r] = CellType.SlantSW;
                    return true;
                }
                else if (c > 0 && r > 0 && c < caveWidth - 1 && r < caveHeight - 1
                        && CellIsFilled(c + 1, r)
                        && CellIsFilled(c, r - 1)
                        && grid[c, r + 1] == CellType.Empty
                        && grid[c - 1, r] == CellType.Empty)
                {
                    grid[c, r] = CellType.SlantNW;
                    return true;
                }
            }
            return false;
        }

        private bool SetEdge(int r, int c)
        {
            if (c < caveWidth - 1 && grid[c + 1, r] == CellType.Empty)
            {
                grid[c, r] = CellType.EdgeE;
                return true;
            }
            if (r > 0 && grid[c, r - 1] == CellType.Empty)
            {
                grid[c, r] = CellType.EdgeS;
                return true;
            }
            if (c > 0 && grid[c - 1, r] == CellType.Empty)
            {
                grid[c, r] = CellType.EdgeW;
                return true;
            }
            if (r < caveHeight - 1 && grid[c, r + 1] == CellType.Empty)
            {
                grid[c, r] = CellType.EdgeN;
                return true;
            }
            return false;
        }

        private float CountAliveNeighbors(int r, int c)
        {
            float count = 0;
            for (int i = -1; i <= 1; i++)
            {
                for (int k = -1; k <= 1; k++)
                {
                    int nr = r + i;
                    int nc = c + k;
                    float val = 1.0f;
                    if (Math.Abs(i) == 1 && Math.Abs(k) == 1)
                    {
                        val = 0.7f;
                    }
                    if (!(i == 0 && k == 0))
                    {
                        if (nr < 0 || nc < 0 || nr >= caveHeight || nc >= caveWidth)
                        {
                            count += val;
                        }
                        else if (grid[nc, nr] == CellType.Filled)
                        {
                            count += val;
                        }
                    }
                }
            }
            return count;
        }

        private int CountAliveNeighborsInt(int r, int c)
        {
            int count = 0;
            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    int nr = r + i;
                    int nc = c + j;
                    if (!(i == 0 && j == 0))
                    {
                        if (nr < 0 || nc < 0 || nr >= caveHeight || nc >= caveWidth)
                        {
                            count++;
                        }
                        else if (grid[nc, nr] == CellType.Filled)
                        {
                            count++;
                        }
                    }
                }
            }

            return count;
        }
        #endregion

    }
}
