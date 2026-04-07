using System.Collections;
using System.Linq;
using System;

using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class PuzzleMenuSceneManager : MonoBehaviour
{

    enum State
    {
        None,
        Menu,
        Setting,
        Solutions,
        Loading,
    }

    /*
     * UI
     */
    public Toggle HardcoreToggle;
    public TextMeshProUGUI TextProgress;
    public TextMeshProUGUI TextCongratulations;
    public Image Preview;
    public TextMeshProUGUI TextLevel;
    string _l10nLevel;

    /*
     * Menu
     */
    public GameObject MenuPanel;
    public Button MenuOpenButton;
    public Button MenuCloseButton;

    public GameObject SettingPanel;
    public Button SettingOpenButton;
    public Button SettingCloseButton;

    public Button ReturnToMenuButton;
    public Button QuitButton;

    public GameObject SolutionsPanel;
    public Button SolutionsCloseButton;

    /*
     * Puzzle layout
     */
    public GameObject PuzzleLayout;
    public Button LevelUpButton;
    public Button LevelDownButton;

    Camera _camera;
    Vector2 _mousePos;
    State _state;

    AssetManager _assetManager;
    AudioManager _audioManager;
    LoadingManager _loadingManager;
    PersistentManager _persistentManager;
    SettingManager _settingManager;
    SteamManager _steamManager;
    SolutionManager _solutionManager;

    // _levels[0] = Level1, ..., _levels[4] = Level5
    GameObject[] _levels;
    int _displayLevel;   // 1–5
    int _currentLevel;   // solved puzzle count (from Progress)

    GameObject _activeCluster;
    Vector3 _activeClusterOriginalPos;
    readonly Vector3 _hoverScale = new Vector3(1.1f, 1.1f, 1f);

    /*
     * Puzzle 1           → Level1 / Cluster0
     * Puzzles  2– 9  (8) → Level2 / Cluster0–7
     * Puzzles 10–17  (8) → Level3 / Cluster0–7
     * Puzzles 18–25  (8) → Level4 / Cluster0–7
     * Puzzles 26–33  (8) → Level5 / Cluster0–7
     */
    static readonly int[] LevelBasePuzzle = { 0, 1, 2, 10, 18, 26 };

    int PuzzleNumber(int displayLevel, int clusterIndex)
        => LevelBasePuzzle[displayLevel] + clusterIndex;

    int ClusterIndex(GameObject cluster)
        => int.Parse(cluster.name.Substring("Cluster".Length));

    // Returns the Cluster-named ancestor of a tile, or null.
    GameObject ClusterOf(GameObject tile)
    {
        if (tile == null) return null;
        var parent = tile.transform.parent;
        if (parent != null && parent.name.StartsWith("Cluster")) return parent.gameObject;
        return null;
    }

    bool IsInCurrentLevel(GameObject cluster)
        => cluster != null && cluster.transform.parent?.gameObject == _levels[_displayLevel - 1];

    // Display level N is accessible if the next puzzle to solve is within it or beyond.
    bool IsDisplayLevelAccessible(int displayLevel)
        => displayLevel >= 1 && displayLevel <= 5
        && _currentLevel >= LevelBasePuzzle[displayLevel] - 1;

    // Which display level contains this puzzle number?
    int DisplayLevelForPuzzle(int puzzleNum)
    {
        for (int lvl = 5; lvl >= 1; lvl--)
            if (puzzleNum >= LevelBasePuzzle[lvl]) return lvl;
        return 1;
    }

    void ChangeState(State to)
    {
        switch (to)
        {
            case State.None:
                SolutionsPanel.SetActive(false);
                MenuPanel.SetActive(false);
                break;
            case State.Solutions:
                SolutionsPanel.SetActive(true);
                break;
            case State.Menu:
                MenuPanel.SetActive(true);
                if (_state == State.Setting)
                {
                    SettingPanel.SetActive(false);
                    _persistentManager.SetBGMVolume(_settingManager.BGMSlider.value);
                    _persistentManager.SetSEVolume(_settingManager.SESlider.value);
                    _persistentManager.SetMouseWheelSensitivity((int)_settingManager.MouseWheelSensitivitySlider.value);
                }
                break;
            case State.Setting:
                SettingPanel.SetActive(true);
                break;
            case State.Loading:
                break;
            default:
                Debug.LogError("Unexpected state: " + to);
                break;
        }
        _state = to;
    }

    void ShowDisplayLevel(int level)
    {
        for (int i = 0; i < _levels.Length; i++)
            _levels[i].SetActive(i == level - 1);
        _displayLevel = level;
        LevelUpButton.interactable = IsDisplayLevelAccessible(_displayLevel + 1);
        LevelDownButton.interactable = _displayLevel > 1;
        ClearHover();
    }

    void Awake()
    {
        _assetManager = GetComponent<AssetManager>();
        _audioManager = GetComponent<AudioManager>();
        _loadingManager = GetComponent<LoadingManager>();
        _persistentManager = GetComponent<PersistentManager>();
        _settingManager = GetComponent<SettingManager>();
        _solutionManager = GetComponent<SolutionManager>();
    }

    void Start()
    {
        _camera = Camera.main;
        _audioManager.SetPlaylist(_assetManager.GetPlaylist(LoadingManager.Scene.PuzzleMenu)).StartBGM();
        _steamManager = GameObject.Find("/SteamManager").GetComponent<SteamManager>();
        _l10nLevel = LocalizationSettings.StringDatabase.GetTableEntry("default", "level").Entry.Value;

        _currentLevel = _persistentManager.LoadProgress(GlobalData.Slot).CurrentLevel;
        HardcoreToggle.isOn = GlobalData.IsHardcoreMode = _persistentManager.IsHardcoreMode(GlobalData.Slot);
        HardcoreToggle.onValueChanged.AddListener(isOn =>
            GlobalData.IsHardcoreMode = _persistentManager.SetHardcoreMode(GlobalData.Slot, isOn));
        TextProgress.text = $"{_currentLevel * 100 / GlobalData.TotalLevel}%";

        MenuOpenButton.onClick.AddListener(() => ChangeState(State.Menu));
        MenuCloseButton.onClick.AddListener(() => ChangeState(State.None));
        SettingOpenButton.onClick.AddListener(() => ChangeState(State.Setting));
        SettingCloseButton.onClick.AddListener(() => ChangeState(State.Menu));
        ReturnToMenuButton.onClick.AddListener(OnReturnToMenuButtonClick);
        SolutionsCloseButton.onClick.AddListener(() => ChangeState(State.None));
        QuitButton.onClick.AddListener(OnPowerOff);
        LevelUpButton.onClick.AddListener(() =>
        {
            _audioManager.PlaySE(_assetManager.SEOK);
            ShowDisplayLevel(_displayLevel + 1);
        });
        LevelDownButton.onClick.AddListener(() =>
        {
            _audioManager.PlaySE(_assetManager.SEOK);
            ShowDisplayLevel(_displayLevel - 1);
        });

        // Collect level GameObjects
        _levels = new GameObject[5];
        for (int i = 0; i < 5; i++)
            _levels[i] = PuzzleLayout.transform.Find("Level" + (i + 1)).gameObject;

        // Apply unlock/dissolve/hide state to each cluster
        for (int lvl = 1; lvl <= 5; lvl++)
        {
            foreach (var cluster in _levels[lvl - 1].Children())
            {
                int puzzleNum = PuzzleNumber(lvl, ClusterIndex(cluster));

                if (_currentLevel >= puzzleNum)
                {
                    // Solved
                    _steamManager.UnlockAchievement(puzzleNum);
                }
                else if (_currentLevel == puzzleNum - 1)
                {
                    // Next puzzle to solve — dissolve animation
                    var mat = new Material(_assetManager.DissolveMaterial);
                    foreach (var r in cluster.GetComponentsInChildren<SpriteRenderer>())
                        r.material = mat;
                    StartCoroutine(DissolveAsync(mat));
                }
                else
                {
                    // Locked
                    cluster.SetActive(false);
                }
            }
        }

        // Preload puzzle frame previews
        for (int p = 1; p <= Math.Min(_currentLevel + 1, GlobalData.TotalLevel); p++)
            StartCoroutine(_assetManager.LoadPuzzleFrameAsync(p, Color.white, _ => { }));

        TextCongratulations.gameObject.SetActive(_currentLevel >= GlobalData.TotalLevel);

        // Start at the display level that contains the next puzzle
        ShowDisplayLevel(DisplayLevelForPuzzle(_currentLevel + 1));
        ChangeState(State.None);
    }

    IEnumerator DissolveAsync(Material material)
    {
        var se = _assetManager.SETileDissolve;
        _audioManager.PlaySE(se);
        float t = 0f;
        while (t < se.length)
        {
            t += Time.deltaTime;
            material.SetFloat("_DissolveRatio", Mathf.Lerp(0f, 1f, t / se.length));
            yield return null;
        }
    }

    // Returns the world-space center of all SpriteRenderers in the cluster.
    static Vector3 ClusterCenter(GameObject cluster)
    {
        var renderers = cluster.GetComponentsInChildren<SpriteRenderer>();
        if (renderers.Length == 0) return cluster.transform.position;
        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds.center;
    }

    const int HoverSortingOrderBoost = 10;

    void ApplyHoverScale(GameObject cluster)
    {
        _activeClusterOriginalPos = cluster.transform.position;
        var center = ClusterCenter(cluster);
        float s = _hoverScale.x;
        cluster.transform.localScale = _hoverScale;
        cluster.transform.position = _activeClusterOriginalPos + (center - _activeClusterOriginalPos) * (1f - s);
        foreach (var r in cluster.GetComponentsInChildren<SpriteRenderer>())
            r.sortingOrder += HoverSortingOrderBoost;
    }

    void ClearHover()
    {
        if (_activeCluster != null)
        {
            _activeCluster.transform.localScale = Vector3.one;
            _activeCluster.transform.position = _activeClusterOriginalPos;
            foreach (var r in _activeCluster.GetComponentsInChildren<SpriteRenderer>())
                r.sortingOrder -= HoverSortingOrderBoost;
            _activeCluster = null;
        }
        Preview.gameObject.SetActive(false);
        TextLevel.text = null;
    }

    void FixedUpdate()
    {
        _mousePos = _camera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        if (_state != State.None) return;

        var tile = XGameObject.AtWorldPoint(_mousePos);
        var cluster = ClusterOf(tile);

        if (cluster == null || !IsInCurrentLevel(cluster))
        {
            if (_activeCluster != null) ClearHover();
            return;
        }

        if (cluster == _activeCluster) return;

        // Switched to a new cluster
        if (_activeCluster != null)
        {
            _activeCluster.transform.localScale = Vector3.one;
            _activeCluster.transform.position = _activeClusterOriginalPos;
            foreach (var r in _activeCluster.GetComponentsInChildren<SpriteRenderer>())
                r.sortingOrder -= HoverSortingOrderBoost;
        }

        _activeCluster = cluster;
        ApplyHoverScale(cluster);

        int puzzleNum = PuzzleNumber(_displayLevel, ClusterIndex(cluster));
        _audioManager.PlaySE(_assetManager.SEOnHoverUI);
        StartCoroutine(_assetManager.LoadPuzzleFrameAsync(puzzleNum, Color.white, sprite =>
        {
            if (_activeCluster != cluster) return;
            Preview.gameObject.SetActive(true);
            Preview.sprite = sprite;
            TextLevel.text = $"{_l10nLevel} {puzzleNum}";
        }));
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        if (!context.performed || _state != State.None) return;

        var tile = XGameObject.AtWorldPoint(_mousePos);
        var cluster = ClusterOf(tile);
        if (cluster == null || !IsInCurrentLevel(cluster)) return;

        int puzzleNum = PuzzleNumber(_displayLevel, ClusterIndex(cluster));
        if (puzzleNum > _currentLevel + 1) return;   // locked

        GlobalData.Level = puzzleNum;
        _audioManager.PlaySE(_assetManager.SEOK);
        if (_solutionManager.Init().HasSolution() && !GlobalData.IsHardcoreMode)
            ChangeState(State.Solutions);
        else
        {
            _solutionManager.OpenNewSolution();
            ChangeState(State.Loading);
        }
    }

    public void OnReturnToMenuButtonClick()
        => StartCoroutine(_loadingManager.LoadAsync(LoadingManager.Scene.Menu));

    public void OnCancel(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        _audioManager.PlaySE(_assetManager.SECancel);
        switch (_state)
        {
            case State.None:
                ChangeState(State.Menu);
                break;
            case State.Solutions:
                _solutionManager.OnCancel();
                if (!SolutionsPanel.activeSelf) ChangeState(State.None);
                break;
            case State.Menu:
                ChangeState(State.None);
                break;
            case State.Setting:
                ChangeState(State.Menu);
                break;
            default:
                break;
        }
    }

    void OnPowerOff()
    {
        _steamManager.Close();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnDebug(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
#if UNITY_EDITOR
        StartCoroutine(_loadingManager.LoadAsync(LoadingManager.Scene.PuzzleMenu, 0.5f, () =>
        {
            int currentLevel = _persistentManager.LoadProgress(GlobalData.Slot).CurrentLevel;
            _persistentManager.SaveProgress(GlobalData.Slot, new Progress(Math.Min(GlobalData.TotalLevel, currentLevel + 1)));
            GlobalData.GameMode = GameMode.Puzzle;
        }));
#endif
    }

}
