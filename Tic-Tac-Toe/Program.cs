using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TicTacToe
{
    enum GridEntry:byte
    {
        Empty,
        PlayerX,
        PlayerO
    }

    sealed class Board
    {
        GridEntry[] m_Values;
        int m_Score ;
        bool m_TurnForPlayerX ;
        public int RecursiveScore
        {
            get;
            private set;
        }
        public bool GameOver
        {
            get;
            private set;
        }

        public Board(GridEntry[] values, bool turnForPlayerX)
        {
            m_TurnForPlayerX = turnForPlayerX;
            m_Values = values;
            ComputeScore();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    GridEntry v = m_Values[i*3 + j];
                    char c = '-';
                    if (v == GridEntry.PlayerX)
                        c = 'X';
                    else if (v == GridEntry.PlayerO)
                        c = 'O';
                    sb.Append(c);
                }
                sb.Append('\n');
            }
            sb.AppendFormat("score={0},ret={1},{2}", m_Score, RecursiveScore, m_TurnForPlayerX);
            return sb.ToString();
        }

        public Board GetChildAtPosition(int x, int y)
        {
            int i = x + y*3;
            GridEntry[] newValues = (GridEntry[])m_Values.Clone();

            if (m_Values[i] != GridEntry.Empty) 
                throw new Exception(string.Format("invalid index [{0},{1}] is taken by {2}",x, y, m_Values[i]));

            newValues[i] = m_TurnForPlayerX?GridEntry.PlayerX:GridEntry.PlayerO;
            return new Board(newValues, !m_TurnForPlayerX);
        }

        public bool IsTerminalNode()
        {
            if (GameOver)
                return true;
            //if all entries are set, then it is a leaf node
            foreach (GridEntry v in m_Values)
            {
                if (v == GridEntry.Empty)
                    return false;
            }
            return true;
        }

        public IEnumerable<Board> GetChildren()
        {
            for (int i = 0; i < m_Values.Length; i++)
            {
                if (m_Values[i] == GridEntry.Empty)
                {
                    GridEntry[] newValues = (GridEntry[])m_Values.Clone();
                    newValues[i] = m_TurnForPlayerX ? GridEntry.PlayerX : GridEntry.PlayerO;
                    yield return new Board(newValues, !m_TurnForPlayerX);
                }
            }
        }

        //http://en.wikipedia.org/wiki/Alpha-beta_pruning
        public int MiniMaxShortVersion(int depth, int alpha, int beta, out Board childWithMax)
        {
            childWithMax = null;
            if (depth == 0 || IsTerminalNode())
            {
                //When it is turn for PlayO, we need to find the minimum score.
                RecursiveScore = m_Score;
                return m_TurnForPlayerX ? m_Score : -m_Score;
            }

            foreach (Board cur in GetChildren())
            {
                Board dummy;
                int score = -cur.MiniMaxShortVersion(depth - 1, -beta, -alpha, out dummy);
                if (alpha < score)
                {
                    alpha = score;
                    childWithMax = cur;
                    if (alpha >= beta)
                    {
                        break;
                    }
                }
            }

            RecursiveScore = alpha;
            return alpha;
        }

        //http://www.ocf.berkeley.edu/~yosenl/extras/alphabeta/alphabeta.html
        public int MiniMax(int depth, bool needMax, int alpha, int beta, out Board childWithMax)
        {
            childWithMax = null;
            System.Diagnostics.Debug.Assert(m_TurnForPlayerX == needMax);
            if (depth == 0 || IsTerminalNode())
            {
                RecursiveScore = m_Score;
                return m_Score;
            }

            foreach (Board cur in GetChildren())
            {
                Board dummy;
                int score = cur.MiniMax(depth - 1, !needMax, alpha, beta, out dummy);
                if (!needMax)
                {
                    if (beta > score)
                    {
                        beta = score;
                        childWithMax = cur;
                        if (alpha >= beta)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    if (alpha < score)
                    {
                        alpha = score;
                        childWithMax = cur;
                        if (alpha >= beta)
                        {
                            break;
                        }
                    }
                }
            }

            RecursiveScore = needMax ? alpha : beta;
            return RecursiveScore;
        }

        public Board FindNextMove(int depth)
        {
            Board ret = null;
            Board ret1 = null;
            MiniMaxShortVersion(depth, int.MinValue + 1, int.MaxValue - 1, out ret1);
            MiniMax(depth, m_TurnForPlayerX, int.MinValue + 1, int.MaxValue - 1, out ret);

            //compare the two versions of MiniMax give the same results
            if (!IsSameBoard(ret, ret1, true))
            {
                Console.WriteLine("ret={0}\n,!= ret1={1},\ncur={2}", ret, ret1, this);
                throw new Exception("Two MinMax functions don't match.");
            }
            return ret;
        }

        int GetScoreForOneLine(GridEntry[] values)
        {
            int countX=0, countO=0;
            foreach (GridEntry v in values)
            {
                if (v == GridEntry.PlayerX)
                    countX++;
                else if (v == GridEntry.PlayerO)
                    countO++;
            }

            if (countO == 3 || countX == 3)
            {
                GameOver = true;
            }

            //The player who has turn should have more advantage.
            //What we should have done
            int advantage = 1;
            if (countO == 0)
            {
                if (m_TurnForPlayerX)
                    advantage = 3;
                return (int)System.Math.Pow(10, countX)*advantage;
            }
            else if (countX == 0)
            {
                if (!m_TurnForPlayerX)
                    advantage = 3;
                return -(int)System.Math.Pow(10, countO) * advantage;
            }
            return 0;
        }

        void ComputeScore()
        {
            int ret = 0;
            int[,] lines = { { 0, 1, 2 }, 
                           { 3, 4, 5 }, 
                           { 6, 7, 8 }, 
                           { 0, 3, 6 }, 
                           { 1, 4, 7 }, 
                           { 2, 5, 8 }, 
                           { 0, 4, 8 }, 
                           { 2, 4, 6 } 
                           };

            for (int i = lines.GetLowerBound(0); i <= lines.GetUpperBound(0); i++)
            {
                ret += GetScoreForOneLine(new GridEntry[] { m_Values[lines[i, 0]], m_Values[lines[i, 1]], m_Values[lines[i, 2]] });
            }
            m_Score = ret;
        }

        public Board TransformBoard(Transform t)
        {
            GridEntry[] values = Enumerable.Repeat(GridEntry.Empty, 9).ToArray();
            for (int i = 0; i < 9; i++)
            {
                Point p = new Point(i % 3, i / 3);
                p = t.ActOn(p);
                int j = p.x + p.y * 3;
                System.Diagnostics.Debug.Assert(values[j] == GridEntry.Empty);
                values[j] = this.m_Values[i];
            }
            return new Board(values, m_TurnForPlayerX);
        }

        static bool IsSameBoard(Board a, Board b, bool compareRecursiveScore)
        {
            if (a == b) 
                return true;
            if (a == null || b == null) 
                return false;
            for (int i = 0; i < a.m_Values.Length; i++)
            {
                if (a.m_Values[i] != b.m_Values[i])
                    return false;
            }

            if (a.m_Score != b.m_Score)
                return false;

            if (compareRecursiveScore && Math.Abs(a.RecursiveScore) != Math.Abs(b.RecursiveScore))
                return false;

            return true;
        }

        public static bool IsSimilarBoard(Board a, Board b)
        {
            if (IsSameBoard(a, b, false))
                return true;

            foreach (Transform t in Transform.s_transforms)
            {
                Board newB = b.TransformBoard(t);
                if(IsSameBoard(a, newB, false))
                {
                    return true;
                }
            }
            return false;
        }
    }

    struct Point
    {
        public int x;
        public int y;
        public Point(int x0, int y0)
        {
            x = x0;
            y = y0;
        }
    }

    class Transform
    {
        const int Size = 3;
        delegate Point TransformFunc(Point p);
        public static Point Rotate90Degree(Point p)
        {
            //012 -> x->y, y->size-x
            //012
            return new Point(Size - p.y -1, p.x);
        }
        public static Point MirrorX(Point p)
        {
            //012 -> 210
            return new Point(Size - p.x -1, p.y);
        }
        public static Point MirrorY(Point p)
        {
            return new Point(p.x, Size - p.y -1);
        }

        List<TransformFunc> actions = new List<TransformFunc>();
        public Point ActOn(Point p)
        {
            foreach (TransformFunc f in actions)
            {
                if(f!=null)
                    p = f(p);
            }

            return p;
        }

        Transform(TransformFunc op, TransformFunc[] ops)
        {
            if(op!=null)
                actions.Add(op);
            if (ops!=null && ops.Length > 0)
                actions.AddRange(ops);
        }
        public static List<Transform> s_transforms = new List<Transform>();
        static Transform()
        {
            for (int i = 0; i < 4; i++)
            {
                TransformFunc[] ops = Enumerable.Repeat<TransformFunc>(Rotate90Degree, i).ToArray();
                s_transforms.Add(new Transform(null, ops));
                s_transforms.Add(new Transform(MirrorX, ops));
                s_transforms.Add(new Transform(MirrorY, ops));
            }
        }
    }

    class TicTacToeGame
    {
        public Board Current
        {
            get;
            private set;
        }
        Board init;

        public TicTacToeGame()
        {
            GridEntry[] values = Enumerable.Repeat(GridEntry.Empty, 9).ToArray();
            init = new Board(values, true);
            Current = init;
        }

        public void ComputerMakeMove(int depth)
        {
            Board next = Current.FindNextMove(depth);
            if(next!=null)
                Current = next;
        }

        public Board GetInitNode()
        {
            return init;
        }

        public void GetNextMoveFromUser()
        {
            if (Current.IsTerminalNode())
                return;

            while (true)
            {
                try
                {
                    Console.WriteLine("Current Node is\n{0}\n Please type in x:[0-2]", Current);
                    int x = int.Parse(Console.ReadLine());
                    Console.WriteLine("Please type in y:[0-2]");
                    int y = int.Parse(Console.ReadLine());
                    Console.WriteLine("x={0},y={1}", x, y);
                    Current = Current.GetChildAtPosition(x, y);
                    Console.WriteLine(Current);
                    return;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            TicTacToeGame game = new TicTacToeGame();
            Console.WriteLine("Winning positions for playerO:");
            List<Board> history = new List<Board>();
            Queue<Board> q = new Queue<Board>();
            q.Enqueue(game.GetInitNode());
            int total = 0;
            while (q.Count > 0 )
            {
                Board b = q.Dequeue();
                Board next = b.FindNextMove(9);
                if (Math.Abs(b.RecursiveScore) >= 200 && next!=null)
                {
                    if (b.RecursiveScore<0&&
                        !next.GameOver && 
                        history.Find(x => Board.IsSimilarBoard(x, b)) == null)
                    {
                        history.Add(b);
                        Console.WriteLine("[{0}] Winner is {1}:\n{2}, next move is:\n{3}", total, b.RecursiveScore < 0 ? "PlayerO" : "PlayerX", b, next);
                        total++;
                    }
                }
                else
                {
                    foreach (Board c in b.GetChildren())
                    {
                        q.Enqueue(c);
                    }
                }
            }

            bool stop = false;
            while (!stop)
            {
                bool userFirst = false;
                game = new TicTacToeGame();
                Console.WriteLine("User play against computer, Do you place the first step?[y/n]");
                if (Console.ReadLine().StartsWith("y", StringComparison.InvariantCultureIgnoreCase))
                {
                    userFirst = true;
                }

                int depth = 8;
                Console.WriteLine("Please select level:[1..8]. 1 is easiet, 8 is hardest");
                int.TryParse(Console.ReadLine(), out depth);

                Console.WriteLine("{0} play first, level={1}", userFirst ? "User" : "Computer", depth);

                while (!game.Current.IsTerminalNode())
                {
                    if (userFirst)
                    {
                        game.GetNextMoveFromUser();
                        game.ComputerMakeMove(depth);
                    }
                    else
                    {
                        game.ComputerMakeMove(depth);
                        game.GetNextMoveFromUser();
                    }
                }
                Console.WriteLine("The final result is \n" + game.Current);
                if (game.Current.RecursiveScore < -200)
                    Console.WriteLine("PlayerO has won.");
                else if (game.Current.RecursiveScore > 200)
                    Console.WriteLine("PlayerX has won.");
                else
                    Console.WriteLine("It is a tie.");

                Console.WriteLine("Try again?[y/n]");
                if (!Console.ReadLine().StartsWith("y", StringComparison.InvariantCultureIgnoreCase))
                {
                    stop = true;
                }
            }

            Console.WriteLine("bye");
        }
    }
}
