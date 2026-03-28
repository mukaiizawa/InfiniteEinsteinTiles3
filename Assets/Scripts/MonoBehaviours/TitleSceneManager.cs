using System.Collections;

using TMPro;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(100)]
public class TitleSceneManager : MonoBehaviour
{

    static bool Clicked = false;

    public Button ContinueButton;
    public TextMeshProUGUI VersionText;
    public GameObject DemoLabel;

    AudioManager _audioManager;
    AssetManager _assetManager;
    LoadingManager _loadingManager;
    PersistentManager _persistentManager;

    void Awake()
    {
        Application.targetFrameRate = 60;
        _audioManager = this.gameObject.GetComponent<AudioManager>();
        _assetManager = this.gameObject.GetComponent<AssetManager>();
        _loadingManager = this.gameObject.GetComponent<LoadingManager>();
        _persistentManager = this.gameObject.GetComponent<PersistentManager>();
    }

    void Start()
    {
        var resolution = _persistentManager.GetResolution();
        Screen.fullScreen = _persistentManager.IsFullScreen();
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        _audioManager.SetPlaylist(_assetManager.GetPlaylist(LoadingManager.Scene.Title)).StartBGM();
        ContinueButton.onClick.AddListener(() => StartCoroutine(OnContinue()));
        LocalizationSettings.InitializationOperation.WaitForCompletion();
        StartCoroutine(SetLocaleAsync(_persistentManager.GetLocale()));
        VersionText.text = $"Version {Application.version}";
#if DEMO
        DemoLabel.SetActive(true);
#endif
        var canvasGroup = GameObject.Find("/Canvas").GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        StartCoroutine(PlayTileEntrance(canvasGroup));
    }

    float _entranceDuration = 2.0f;
    float _entranceMaxDelay = 0.3f;

    IEnumerator PlayTileEntrance(CanvasGroup canvasGroup)
    {
        var cam = Camera.main;
        Vector2 center = cam != null
            ? (Vector2)cam.ScreenToWorldPoint(new Vector3(Screen.width / 2f, Screen.height / 2f))
            : Vector2.zero;
        var placedTiles = GameObject.Find("/Board/PlacedTiles");
        if (placedTiles != null)
        {
            foreach (Transform child in placedTiles.transform)
            {
                float delay = Random.Range(0f, _entranceMaxDelay);
                child.gameObject.AddComponent<TileEntrance>().Initialize(center, _entranceDuration, delay);
            }
        }
        yield return new WaitForSeconds(_entranceDuration + _entranceMaxDelay);
        float t = 0f;
        float fadeDuration = 0.5f;
        if (canvasGroup != null)
        {
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                canvasGroup.alpha = Mathf.Clamp01(t / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    float _fadeDulation = 2f;
    IEnumerator OnContinue()
    {
        if (Clicked) yield break;
        Clicked = true;
        _audioManager.PlaySE(_assetManager.SEOK);
        var mask = _loadingManager.Mask;
        mask.alpha = 0f;
        mask.gameObject.SetActive(true);
        float t = 0f;
        while (t < _fadeDulation)
        {
            t += Time.deltaTime;
            mask.alpha = Mathf.Clamp01(t / _fadeDulation);
            yield return null;
        }
        yield return _loadingManager.LoadAsync(LoadingManager.Scene.Menu);
    }

    IEnumerator SetLocaleAsync(string localeCode)
    {
        var locale = LocalizationSettings.AvailableLocales.Locales.Find(locale => locale.Identifier.Code.Equals(localeCode));
        if (locale != null) LocalizationSettings.SelectedLocale = locale;
        yield return null;
    }

}
