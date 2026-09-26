using UnityEngine;

// Map characters: # wall, . floor, A start, B goal, o switch,
// 1 switch wall that starts raised, 2 switch wall that starts lowered.
[CreateAssetMenu(menuName = "Switchback/Level")]
public class LevelData : ScriptableObject
{
    public string levelName;
    public int moveLimit;
    [TextArea(2, 4)] public string intro;
    public string[] rows;
}
