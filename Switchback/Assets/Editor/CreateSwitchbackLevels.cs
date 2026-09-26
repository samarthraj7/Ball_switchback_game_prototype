using UnityEditor;
using UnityEngine;
//editor
// Menu: Switchback > Create Level Assets. Writes the five levels from the browser prototype to Assets/Levels.
public static class CreateSwitchbackLevels
{
    [MenuItem("Switchback/Create Level Assets")]
    public static void Create()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Levels")) AssetDatabase.CreateFolder("Assets", "Levels");

        Save(1, "First Flip", 7,
            "Tilt the board with the arrow keys. The ball rolls until it hits a wall. Rolling over the yellow switch flips every purple switch wall: raised ones drop, lowered ones rise.",
            "#########",
            "#...1...#",
            "#.B#....#",
            "#2.....##",
            "###o...A#",
            "#.#..#..#",
            "#...#..##",
            "#########");

        Save(2, "Two Switches", 7,
            "Two switches now. Any switch flips all the switch walls, so think about how many times you cross them.",
            "##########",
            "#.2.....2#",
            "#..o#....#",
            "#..#..1B.#",
            "##...#...#",
            "##.A.o...#",
            "#...#..1.#",
            "##########");

        Save(3, "Back and Forth", 10,
            "You will need to flip the walls more than once. Watch which walls are up before you tilt.",
            "##########",
            "###.o.#..#",
            "#o.1..A..#",
            "#12.###..#",
            "#...#....#",
            "#..2#....#",
            "##1B.##.##",
            "#....2...#",
            "##########");

        Save(4, "Tight Corners", 10,
            "Three switches and only one spare move. Plan the route before you roll.",
            "###########",
            "#...#.1#..#",
            "##.......##",
            "###1......#",
            "#B..22#.#.#",
            "##o..o#A..#",
            "#1.#.....2#",
            "#..#.#..o.#",
            "###########");

        Save(5, "Switchback", 13,
            "The final board: no spare moves. Find the perfect route. Undo gives moves back, so experiment freely.",
            "############",
            "#.#...#..#.#",
            "#..o#...#..#",
            "#....1o1B..#",
            "#2.....##..#",
            "#....2.#...#",
            "#........A2#",
            "#..#o....1##",
            "#..#1...#2##",
            "############");

        AssetDatabase.SaveAssets();
        Debug.Log("Created 5 Switchback levels in Assets/Levels");
    }

    static void Save(int number, string name, int moveLimit, string intro, params string[] rows)
    {
        var level = ScriptableObject.CreateInstance<LevelData>();
        level.levelName = name;
        level.moveLimit = moveLimit;
        level.intro = intro;
        level.rows = rows;
        AssetDatabase.CreateAsset(level, $"Assets/Levels/Level{number}_{name.Replace(" ", "")}.asset");
    }
}
