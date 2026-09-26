using System.Collections.Generic;
using UnityEngine;

public readonly struct BallState
{
    public readonly Vector2Int Pos;
    public readonly bool Flipped;

    public BallState(Vector2Int pos, bool flipped)
    {
        Pos = pos;
        Flipped = flipped;
    }
}

// Same rules as Ball-Prototype/logic.js. Grid coordinates: x to the right, y downward (row index).
public static class BoardRules
{
    public static readonly Vector2Int[] Dirs =
    {
        new Vector2Int(0, -1), // 0 up
        new Vector2Int(0, 1),  // 1 down
        new Vector2Int(-1, 0), // 2 left
        new Vector2Int(1, 0),  // 3 right
    };
    public static readonly string[] DirNames = { "up", "down", "left", "right" };

    public static Vector2Int Find(string[] rows, char c)
    {
        for (int y = 0; y < rows.Length; y++)
        {
            int x = rows[y].IndexOf(c);
            if (x >= 0) return new Vector2Int(x, y);
        }
        return new Vector2Int(-1, -1);
    }

    public static bool IsSolid(string[] rows, bool flipped, Vector2Int p)
    {
        if (p.y < 0 || p.y >= rows.Length || p.x < 0 || p.x >= rows[p.y].Length) return true;
        char c = rows[p.y][p.x];
        return c == '#' || (c == '1' && !flipped) || (c == '2' && flipped);
    }

    // Every cell the ball passes through, in order. Empty if the ball cannot move that way.
    public static List<BallState> Roll(string[] rows, BallState s, int dir, out bool won)
    {
        var path = new List<BallState>();
        won = false;
        Vector2Int pos = s.Pos;
        bool flipped = s.Flipped;
        for (int i = 0; i < 400; i++)
        {
            Vector2Int next = pos + Dirs[dir];
            if (IsSolid(rows, flipped, next)) break;
            pos = next;
            char c = rows[pos.y][pos.x];
            if (c == 'o') flipped = !flipped;
            path.Add(new BallState(pos, flipped));
            if (c == 'B') { won = true; break; }
        }
        return path;
    }

    // Shortest list of directions from s to B (breadth-first search), or null if unreachable.
    public static List<int> Solve(string[] rows, BallState start)
    {
        var seen = new HashSet<(int, int, bool)> { (start.Pos.x, start.Pos.y, start.Flipped) };
        var queue = new Queue<(BallState state, List<int> moves)>();
        queue.Enqueue((start, new List<int>()));
        while (queue.Count > 0)
        {
            var (s, moves) = queue.Dequeue();
            for (int d = 0; d < 4; d++)
            {
                var path = Roll(rows, s, d, out bool won);
                if (path.Count == 0) continue;
                var nextMoves = new List<int>(moves) { d };
                if (won) return nextMoves;
                BallState end = path[path.Count - 1];
                if (seen.Add((end.Pos.x, end.Pos.y, end.Flipped))) queue.Enqueue((end, nextMoves));
            }
        }
        return null;
    }
}
