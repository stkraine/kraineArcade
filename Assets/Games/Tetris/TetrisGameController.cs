using UnityEngine;

/// <summary>
/// Unity integration layer for the Tetris bringup. Drives core game logic and exposes hooks
/// for rendering/input systems. Attach to a GameObject in a Tetris scene.
/// </summary>
public class TetrisGameController : MonoBehaviour
{
    [Header("Config")]
    public int startLevel = 1;
    public int randomSeed = 0;
    public bool useRandomSeed = true;

    [Header("Runtime (read-only)")]
    [SerializeField] private int score;
    [SerializeField] private int level;
    [SerializeField] private int lines;
    [SerializeField] private bool gameOver;

    public TetrisGame Game { get; private set; }

    private float _fallTimer;
    private float _dasTimer;
    private float _arrTimer;
    private int _moveDir;

    private void Awake()
    {
        int? seed = useRandomSeed ? (int?)null : randomSeed;
        Game = new TetrisGame(seed, startLevel);
        SyncDebugState();
        Debug.Log("Tetris bringup initialized (Guideline-aligned core: 10x20, 7-bag, SRS, hold, lock delay).");
    }

    private void Update()
    {
        if (Game == null || Game.IsGameOver)
        {
            SyncDebugState();
            return;
        }

        HandleInput();
        HandleGravity();
        Game.Tick(Time.deltaTime);
        SyncDebugState();
    }

    private void HandleInput()
    {
        // Minimal legacy Input mapping; replace with Input System as project evolves.
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) TryMove(-1);
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) TryMove(1);
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.X)) Game.Rotate(true);
        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.LeftControl)) Game.Rotate(false);
        if (Input.GetKeyDown(KeyCode.Space)) Game.HardDrop();
        if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftShift)) Game.Hold();
        if (Input.GetKey(KeyCode.DownArrow)) Game.SoftDrop();

        // Simple DAS/ARR for held movement
        int dir = 0;
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) dir = -1;
        else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) dir = 1;

        if (dir != _moveDir)
        {
            _moveDir = dir;
            _dasTimer = 0f;
            _arrTimer = 0f;
        }
        else if (dir != 0)
        {
            _dasTimer += Time.deltaTime;
            const float das = 0.167f; // ~10 frames @60fps
            const float arr = 0.033f; // ~2 frames @60fps
            if (_dasTimer >= das)
            {
                _arrTimer += Time.deltaTime;
                while (_arrTimer >= arr)
                {
                    TryMove(dir);
                    _arrTimer -= arr;
                }
            }
        }
    }

    private void TryMove(int dx)
    {
        Game.Move(dx, 0);
    }

    private void HandleGravity()
    {
        int intervalMs = Game.GetFallIntervalMs();
        if (intervalMs <= 0) return; // 20G+ handled as instant in full impl; bringup uses tick/soft drop
        _fallTimer += Time.deltaTime;
        float interval = intervalMs / 1000f;
        while (_fallTimer >= interval)
        {
            if (!Game.Move(0, -1))
            {
                _fallTimer = 0f;
                break;
            }
            _fallTimer -= interval;
        }
    }

    private void SyncDebugState()
    {
        if (Game == null) return;
        score = Game.Score;
        level = Game.Level;
        lines = Game.LinesClearedTotal;
        gameOver = Game.IsGameOver;
    }

    /// <summary>
    /// Hook for UI/menu systems to start or restart a game.
    /// </summary>
    public void Restart()
    {
        Awake();
        _fallTimer = 0f;
    }
}
