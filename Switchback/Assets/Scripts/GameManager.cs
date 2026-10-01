using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    const string ProgressKey = "switchbackProgress";

    [Header("Levels (in order)")]
    public LevelData[] levels;

    [Header("Sprites (Create > 2D > Sprites > Square / Circle)")]
    public Sprite square;
    public Sprite circle;

    [Header("UI")]
    public TMP_Text titleText;
    public TMP_Text introText;
    public TMP_Text movesText;
    public TMP_Text messageText;
    public Button[] levelButtons;
    public GameObject winPanel;
    public TMP_Text winText;
    public GameObject nextButton;

    public float cellsPerSecond = 16f;

    static readonly Color FloorA = Hex("#1a2131"), FloorB = Hex("#1c2436"), WallColor = Hex("#44516f");
    static readonly Color SwitchColor = Hex("#ffd66b"), WallFill = Hex("#6f4fc2"), WallOutline = Hex("#c9b0ff");
    static readonly Color GoalRing = Hex("#3ee07f"), GoalHole = Hex("#05070c"), StartColor = new Color(0.5f, 0.84f, 1f, 0.3f);

    const float SwitchWallSize = 0.84f, OutlineWidth = 0.08f;
    const int DashesPerSide = 4;

    LevelData level;
    int levelIndex;
    BallState state;
    readonly Stack<BallState> history = new Stack<BallState>();
    readonly List<(GameObject solid, GameObject dotted, bool startsRaised)> switchWalls =
        new List<(GameObject, GameObject, bool)>();
    Transform board, ball;
    bool busy, won;
    Vector2 swipeStart;

    int MovesLeft => level.moveLimit - history.Count;
    int Progress => PlayerPrefs.GetInt(ProgressKey, 0);

    void Start()
    {
        Load(Mathf.Min(Progress, levels.Length - 1));
    }

    public void Load(int index)
    {
        StopAllCoroutines();
        levelIndex = index;
        level = levels[index];
        BuildBoard();
        FitCamera();
        titleText.text = $"Level {index + 1}: {level.levelName}";
        introText.text = level.intro;
        for (int i = 0; i < levelButtons.Length; i++) levelButtons[i].interactable = i <= Progress;
        Restart();
    }

    public void Restart()
    {
        StopAllCoroutines();
        busy = false;
        won = false;
        history.Clear();
        state = new BallState(BoardRules.Find(level.rows, 'A'), false);
        ball.position = ToWorld(state.Pos);
        ApplyWalls(false);
        winPanel.SetActive(false);
        messageText.text = "";
        UpdateHud();
    }

    public void Undo()
    {
        if (busy || history.Count == 0) return;
        state = history.Pop();
        won = false;
        ball.position = ToWorld(state.Pos);
        ApplyWalls(state.Flipped);
        winPanel.SetActive(false);
        messageText.text = "";
        UpdateHud();
    }

    public void Hint()
    {
        if (busy || won) return;
        var solution = BoardRules.Solve(level.rows, state);
        if (solution == null) messageText.text = "No way to B from here: undo or restart.";
        else if (solution.Count > MovesLeft) messageText.text = "Not enough moves left from here: undo a few moves.";
        else messageText.text = $"Try tilting {BoardRules.DirNames[solution[0]]}.";
    }

    public void NextLevel()
    {
        if (levelIndex < levels.Length - 1) Load(levelIndex + 1);
    }

    // 0 up, 1 down, 2 left, 3 right. UI arrow buttons call this with an int argument.
    public void Tilt(int dir)
    {
        if (busy || won) return;
        if (MovesLeft <= 0) { messageText.text = "Out of moves: undo or restart."; return; }
        var path = BoardRules.Roll(level.rows, state, dir, out bool reachedGoal);
        if (path.Count == 0) return;
        history.Push(state);
        state = path[path.Count - 1];
        messageText.text = "";
        UpdateHud();
        StartCoroutine(Animate(path, reachedGoal));
    }

    IEnumerator Animate(List<BallState> path, bool reachedGoal)
    {
        busy = true;
        foreach (var step in path)
        {
            Vector3 target = ToWorld(step.Pos);
            while ((ball.position - target).sqrMagnitude > 0.0001f)
            {
                ball.position = Vector3.MoveTowards(ball.position, target, cellsPerSecond * Time.deltaTime);
                yield return null;
            }
            ApplyWalls(step.Flipped);
        }
        busy = false;
        if (reachedGoal) Finish();
        else if (MovesLeft == 0) messageText.text = "Out of moves: undo or restart.";
    }

    void Finish()
    {
        won = true;
        PlayerPrefs.SetInt(ProgressKey, Mathf.Max(Progress, levelIndex + 1));
        PlayerPrefs.Save();
        for (int i = 0; i < levelButtons.Length; i++) levelButtons[i].interactable = i <= Progress;
        bool last = levelIndex == levels.Length - 1;
        winText.text = last ? "You finished all 5 levels!" : $"Level {levelIndex + 1} complete!";
        nextButton.SetActive(!last);
        winPanel.SetActive(true);
    }

    void Update()
    {
        var k = Keyboard.current;
        if (k != null)
        {
            if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) Tilt(0);
            if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) Tilt(1);
            if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame) Tilt(2);
            if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) Tilt(3);
            if (k.zKey.wasPressedThisFrame) Undo();
            if (k.rKey.wasPressedThisFrame) Restart();
            if (k.hKey.wasPressedThisFrame) Hint();
            if (k.enterKey.wasPressedThisFrame && won) NextLevel();
        }

        var p = Pointer.current;
        if (p == null) return;
        if (p.press.wasPressedThisFrame) swipeStart = p.position.ReadValue();
        if (p.press.wasReleasedThisFrame)
        {
            Vector2 delta = p.position.ReadValue() - swipeStart;
            if (delta.magnitude < 50f) return;
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y)) Tilt(delta.x > 0 ? 3 : 2);
            else Tilt(delta.y > 0 ? 0 : 1);
        }
    }

    void BuildBoard()
    {
        if (board != null) Destroy(board.gameObject);
        board = new GameObject("Board").transform;
        switchWalls.Clear();

        for (int y = 0; y < level.rows.Length; y++)
        {
            for (int x = 0; x < level.rows[y].Length; x++)
            {
                var p = new Vector2Int(x, y);
                char c = level.rows[y][x];
                Make(square, p, (x + y) % 2 == 0 ? FloorB : FloorA, 1f, 0);
                switch (c)
                {
                    case '#': Make(square, p, WallColor, 0.92f, 1); break;
                    case 'A': Make(square, p, StartColor, 0.75f, 1); break;
                    case 'B':
                        Make(circle, p, GoalRing, 0.8f, 1);
                        Make(circle, p, GoalHole, 0.62f, 2);
                        break;
                    case 'o': Make(circle, p, SwitchColor, 0.55f, 1); break;
                    case '1':
                    case '2':
                        MakeSwitchWall(p, c == '1');
                        break;
                }
            }
        }
        ball = Make(circle, BoardRules.Find(level.rows, 'A'), Color.white, 0.6f, 5).transform;
    }

    void ApplyWalls(bool flipped)
    {
        foreach (var (solid, dotted, startsRaised) in switchWalls)
        {
            bool raised = startsRaised != flipped;
            solid.SetActive(raised);
            dotted.SetActive(!raised);
        }
    }

    // Raised: filled block with a solid outline. Lowered: dotted outline only.
    void MakeSwitchWall(Vector2Int p, bool startsRaised)
    {
        var root = new GameObject("SwitchWall").transform;
        root.SetParent(board);
        root.position = ToWorld(p);

        float s = SwitchWallSize, w = OutlineWidth, edge = (s - w) / 2f;

        var solid = new GameObject("Raised").transform;
        solid.SetParent(root, false);
        MakeRect(solid, Vector2.zero, new Vector2(s, s), WallFill, 1);
        MakeRect(solid, new Vector2(0f, edge), new Vector2(s, w), WallOutline, 2);
        MakeRect(solid, new Vector2(0f, -edge), new Vector2(s, w), WallOutline, 2);
        MakeRect(solid, new Vector2(-edge, 0f), new Vector2(w, s), WallOutline, 2);
        MakeRect(solid, new Vector2(edge, 0f), new Vector2(w, s), WallOutline, 2);

        var dotted = new GameObject("Lowered").transform;
        dotted.SetParent(root, false);
        float step = s / DashesPerSide, dash = step / 2f;
        for (int i = 0; i < DashesPerSide; i++)
        {
            float t = -s / 2f + step * (i + 0.5f);
            MakeRect(dotted, new Vector2(t, edge), new Vector2(dash, w), WallOutline, 2);
            MakeRect(dotted, new Vector2(t, -edge), new Vector2(dash, w), WallOutline, 2);
            MakeRect(dotted, new Vector2(-edge, t), new Vector2(w, dash), WallOutline, 2);
            MakeRect(dotted, new Vector2(edge, t), new Vector2(w, dash), WallOutline, 2);
        }

        switchWalls.Add((solid.gameObject, dotted.gameObject, startsRaised));
    }

    void MakeRect(Transform parent, Vector2 localPos, Vector2 size, Color color, int order)
    {
        var go = new GameObject("Rect");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.color = color;
        sr.sortingOrder = order;
    }

    SpriteRenderer Make(Sprite sprite, Vector2Int p, Color color, float scale, int order)
    {
        var go = new GameObject(sprite.name);
        go.transform.SetParent(board);
        go.transform.position = ToWorld(p);
        go.transform.localScale = Vector3.one * scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    void FitCamera()
    {
        var cam = Camera.main;
        float w = level.rows[0].Length, h = level.rows.Length;
        cam.orthographic = true;
        cam.orthographicSize = Mathf.Max(h / 2f, w / 2f / cam.aspect) + 1.5f;
        cam.transform.position = new Vector3((w - 1) / 2f, -(h - 1) / 2f, -10f);
    }

    void UpdateHud()
    {
        movesText.text = $"Moves left: {MovesLeft} / {level.moveLimit}";
    }

    static Vector3 ToWorld(Vector2Int p) => new Vector3(p.x, -p.y, 0f);

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}
