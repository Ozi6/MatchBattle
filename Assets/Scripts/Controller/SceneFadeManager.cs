using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneFadeManager : MonoBehaviour
{
    public static SceneFadeManager Instance;

    [Header("Fade Settings")]
    public float fadeInDuration = 1f;
    public float fadeOutDuration = 1f;
    public Color fadeColor = Color.black;

    private Image fadeImage;
    private Canvas fadeCanvas;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupFadeUI();
        }
        else
            Destroy(gameObject);
    }

    void SetupFadeUI()
    {
        GameObject fadeCanvasGO = new GameObject("FadeCanvas");
        fadeCanvasGO.transform.SetParent(transform);
        fadeCanvas = fadeCanvasGO.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 9999;
        CanvasScaler scaler = fadeCanvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        fadeCanvasGO.AddComponent<GraphicRaycaster>();
        GameObject fadeImageGO = new GameObject("FadeImage");
        fadeImageGO.transform.SetParent(fadeCanvasGO.transform, false);
        fadeImage = fadeImageGO.AddComponent<Image>();
        fadeImage.color = fadeColor;
        RectTransform rectTransform = fadeImage.rectTransform;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        SetFadeAlpha(0f);
        fadeCanvas.gameObject.SetActive(false);
    }

    public void LoadSceneWithFade(string sceneName)
    {
        StartCoroutine(FadeTransition(sceneName));
    }

    public void LoadSceneWithFade(int sceneIndex)
    {
        StartCoroutine(FadeTransition(sceneIndex));
    }

    IEnumerator FadeTransition(string sceneName)
    {
        yield return StartCoroutine(FadeOut());
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
            yield return null;
        yield return StartCoroutine(FadeIn());
    }

    IEnumerator FadeTransition(int sceneIndex)
    {
        yield return StartCoroutine(FadeOut());
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneIndex);
        while (!asyncLoad.isDone)
            yield return null;
        yield return StartCoroutine(FadeIn());
    }

    public IEnumerator FadeOut()
    {
        fadeCanvas.gameObject.SetActive(true);
        float elapsedTime = 0f;
        Color startColor = fadeColor;
        startColor.a = 0f;
        Color endColor = fadeColor;
        endColor.a = 1f;
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float normalizedTime = elapsedTime / fadeOutDuration;
            Color currentColor = Color.Lerp(startColor, endColor, normalizedTime);
            SetFadeAlpha(currentColor.a);
            yield return null;
        }
        SetFadeAlpha(1f);
    }

    public IEnumerator FadeIn()
    {
        fadeCanvas.gameObject.SetActive(true);
        float elapsedTime = 0f;
        Color startColor = fadeColor;
        startColor.a = 1f;
        Color endColor = fadeColor;
        endColor.a = 0f;
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float normalizedTime = elapsedTime / fadeInDuration;
            Color currentColor = Color.Lerp(startColor, endColor, normalizedTime);
            SetFadeAlpha(currentColor.a);
            yield return null;
        }
        SetFadeAlpha(0f);
        fadeCanvas.gameObject.SetActive(false);
    }

    void SetFadeAlpha(float alpha)
    {
        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = alpha;
            fadeImage.color = color;
        }
    }

    public void FadeInOnSceneStart()
    {
        StartCoroutine(FadeIn());
    }
}