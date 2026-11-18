using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Transição de cena com fade (escurece, carrega a cena de forma assíncrona e clareia).
/// Não precisa de setup: o Canvas e a imagem de fade são criados automaticamente.
/// Compatível com Unity 6.
///
/// Uso em qualquer script:
///     SceneTransition.LoadScene("NomeDaCena");
///     SceneTransition.LoadScene(2);
///     SceneTransition.ReloadScene();
///
/// Uso em botão da UI (OnClick): arraste um objeto com este componente e chame
/// LoadSceneByName(string) ou LoadSceneByIndex(int).
/// </summary>
[DisallowMultipleComponent]
public class SceneTransition : MonoBehaviour
{
    [Header("Fade")]
    [SerializeField] private Color fadeColor = Color.black;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Carregamento")]
    [Tooltip("Tempo mínimo com a tela escura, útil para evitar piscadas em cenas que carregam rápido.")]
    [SerializeField] private float minimumBlackTime = 0.1f;

    [Tooltip("Se ligado, a cena atual também clareia ao iniciar o jogo.")]
    [SerializeField] private bool fadeInOnStart = true;

    private static SceneTransition instance;

    private CanvasGroup canvasGroup;
    private bool isTransitioning;

    private Canvas canvas;

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => AssignCamera();

    private void AssignCamera()
    {
        Camera cam = Camera.main;
        if (cam != null && canvas != null)
            canvas.worldCamera = cam;
    }

    /// <summary>Instância única. Se não existir na cena, é criada automaticamente.</summary>
    public static SceneTransition Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<SceneTransition>();
                if (instance == null)
                {
                    var go = new GameObject("SceneTransition");
                    instance = go.AddComponent<SceneTransition>();
                }
            }
            return instance;
        }
    }

    public static bool IsTransitioning => instance != null && instance.isTransitioning;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
    }

    private void Start()
    {
        if (fadeInOnStart)
        {
            canvasGroup.alpha = 1f;
            StartCoroutine(Fade(1f, 0f, fadeInDuration));
        }
    }

    // ---------- API estática ----------

    public static void LoadScene(string sceneName) => Instance.StartTransition(sceneName, -1);

    public static void LoadScene(int buildIndex) => Instance.StartTransition(null, buildIndex);

    public static void ReloadScene() => Instance.StartTransition(null, SceneManager.GetActiveScene().buildIndex);

    public static void LoadNextScene()
    {
        int next = (SceneManager.GetActiveScene().buildIndex + 1) % SceneManager.sceneCountInBuildSettings;
        Instance.StartTransition(null, next);
    }

    // ---------- Métodos para botões da UI (OnClick) ----------

    public void LoadSceneByName(string sceneName) => StartTransition(sceneName, -1);

    public void LoadSceneByIndex(int buildIndex) => StartTransition(null, buildIndex);

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------- Internos ----------

    private void StartTransition(string sceneName, int buildIndex)
    {
        if (isTransitioning) return; // ignora cliques repetidos
        AssignCamera();
        StartCoroutine(TransitionRoutine(sceneName, buildIndex));
    }

    private IEnumerator TransitionRoutine(string sceneName, int buildIndex)
    {
        isTransitioning = true;
        canvasGroup.blocksRaycasts = true;

        // 1. Escurece a tela
        yield return Fade(canvasGroup.alpha, 1f, fadeOutDuration);

        // 2. Carrega a cena em segundo plano
        AsyncOperation op = sceneName != null
            ? SceneManager.LoadSceneAsync(sceneName)
            : SceneManager.LoadSceneAsync(buildIndex);

        if (op == null)
        {
            Debug.LogError($"[SceneTransition] Não foi possível carregar a cena '{sceneName ?? buildIndex.ToString()}'. " +
                           "Confira se ela está em File > Build Profiles > Scene List.");
            yield return Fade(1f, 0f, fadeInDuration);
            canvasGroup.blocksRaycasts = false;
            isTransitioning = false;
            yield break;
        }

        op.allowSceneActivation = false;

        float timer = 0f;
        while (op.progress < 0.9f || timer < minimumBlackTime)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        op.allowSceneActivation = true;
        while (!op.isDone)
            yield return null;

        // 3. Clareia a tela na nova cena
        yield return Fade(1f, 0f, fadeInDuration);

        canvasGroup.blocksRaycasts = false;
        isTransitioning = false;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // funciona mesmo com Time.timeScale = 0
            canvasGroup.alpha = Mathf.Lerp(from, to, fadeCurve.Evaluate(t / duration));
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    private void BuildUI()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.planeDistance = 0.5f;
        canvas.sortingOrder = 9999;
        AssignCamera();

        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;

        var imgObj = new GameObject("Fade");
        imgObj.transform.SetParent(transform, false);

        var img = imgObj.AddComponent<Image>();
        img.color = fadeColor;
        img.raycastTarget = true;

        var rect = img.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
