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

    public Image ShimmerImage;
    public float ShimmerDuration = 1.4f;

    AudioManager _audioManager;
    AssetManager _assetManager;
    LoadingManager _loadingManager;
    PersistentManager _persistentManager;

    Material _shimmerMaterial;

    void Awake()
    {
        Application.targetFrameRate = 60;
        _audioManager = this.gameObject.GetComponent<AudioManager>();
        _assetManager = this.gameObject.GetComponent<AssetManager>();
        _loadingManager = this.gameObject.GetComponent<LoadingManager>();
        _persistentManager = this.gameObject.GetComponent<PersistentManager>();
        if (ShimmerImage != null)
        {
            ShimmerImage.gameObject.SetActive(true);
            var shader = Shader.Find("TitleShimmer");
            if (shader != null)
            {
                _shimmerMaterial = new Material(shader);
                ShimmerImage.material = _shimmerMaterial;
            }
            ShimmerImage.enabled = false;
        }
    }

    void OnDestroy()
    {
        if (_shimmerMaterial != null)
            Destroy(_shimmerMaterial);
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
        _audioManager.PlaySE(_assetManager.SETitleEntrance);
        yield return new WaitForSeconds(_entranceDuration + _entranceMaxDelay);
        if (_shimmerMaterial != null) StartCoroutine(PlayShimmer());
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

    IEnumerator PlayShimmer()
    {
        _audioManager.PlaySE(_assetManager.SETitleShimmer);
        ShimmerImage.enabled = true;
        float t = 0f;
        while (t < ShimmerDuration)
        {
            t += Time.deltaTime;
            float ratio = Mathf.Clamp01(t / ShimmerDuration);
            float eased = ratio < 0.5f
                ? 4f * ratio * ratio * ratio
                : 1f - Mathf.Pow(-2f * ratio + 2f, 3f) / 2f;
            _shimmerMaterial.SetFloat("_Progress", eased);
            yield return null;
        }
        _shimmerMaterial.SetFloat("_Progress", 1f);
        ShimmerImage.enabled = false;
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
