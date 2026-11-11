using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla a cena atual:
///  1. Toca o áudio inicial quando a cena começa.
///  2. Faz sprites 2D aparecerem e sumirem (com fade) em momentos específicos do áudio.
///     Cada sprite pode ter VÁRIOS intervalos, então ele pode piscar quantas vezes você quiser.
///  3. Quando o áudio acaba, os sprites que ainda estão visíveis somem com fade out.
///  4. O puzzle só aparece (SetActive true + fade in) depois que o áudio acaba.
///     Se um Trigger Collider for atribuído, o jogador também precisa ter atravessado ele.
///
/// Compatível com Unity 6 (URP). Coloque este script em um objeto vazio da cena
/// (ex.: "SceneController") e preencha os campos no Inspector.
/// </summary>
public class SceneController : MonoBehaviour
{
    // ---------------- Dados dos sprites sincronizados com o áudio ----------------

    /// <summary>Um período em que o sprite fica visível.</summary>
    [System.Serializable]
    public class SpriteInterval
    {
        [Tooltip("Segundo do áudio em que o sprite aparece.")]
        [Min(0f)] public float showAtTime = 0f;

        [Tooltip("Segundo do áudio em que o sprite some. -1 = fica até o áudio acabar.")]
        public float hideAtTime = -1f;

        [Tooltip("Duração do fade ao aparecer. Use valores pequenos (0 a 0.1) para um piscar rápido.")]
        [Min(0f)] public float fadeInDuration = 0.1f;

        [Tooltip("Duração do fade ao sumir. Use valores pequenos (0 a 0.1) para um piscar rápido.")]
        [Min(0f)] public float fadeOutDuration = 0.1f;
    }

    [System.Serializable]
    public class SpriteCue
    {
        [Tooltip("Nome só para organizar a lista.")]
        public string label;

        [Tooltip("Sprite 2D que vai aparecer e sumir.")]
        public SpriteRenderer sprite;

        [Tooltip("Períodos em que o sprite fica visível. Adicione vários para ele piscar.")]
        public List<SpriteInterval> intervals = new List<SpriteInterval>();

        // Estado interno (não aparece no Inspector).
        [System.NonSerialized] public float baseAlpha = 1f;
        [System.NonSerialized] public float visibility;
        [System.NonSerialized] public bool targetVisible;
        [System.NonSerialized] public SpriteInterval lastActive;
        [System.NonSerialized] public Coroutine routine;
    }

    [Header("Áudio inicial")]
    [Tooltip("Áudio que toca assim que a cena começa. O puzzle aparece quando ele termina.")]
    [SerializeField] private AudioClip introAudio;

    [Tooltip("Opcional. Se vazio, um AudioSource é criado automaticamente.")]
    [SerializeField] private AudioSource audioSource;

    [Range(0f, 1f)]
    [SerializeField] private float audioVolume = 1f;

    [Tooltip("Atraso (em segundos) antes de tocar o áudio.")]
    [SerializeField] private float audioDelay = 0f;

    [Header("Sprites sincronizados com o áudio")]
    [Tooltip("Cada item controla um sprite e os intervalos em que ele fica visível.")]
    [SerializeField] private List<SpriteCue> spriteCues = new List<SpriteCue>();

    [Header("Detecção do jogador (opcional)")]
    [Tooltip("Se atribuído, o puzzle só aparece quando o áudio acabou E o jogador atravessou este collider. Deixe vazio para ignorar.")]
    [SerializeField] private Collider triggerCollider;

    [Tooltip("Tag do jogador. Deixe vazio para aceitar qualquer objeto.")]
    [SerializeField] private string playerTag = "Player";

    [Header("Puzzle")]
    [Tooltip("GameObject do puzzle. Ele começa desativado e aparece com fade in no fim do áudio.")]
    [SerializeField] private GameObject puzzle;

    [SerializeField] private float fadeInDuration = 1.5f;

    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private bool audioFinished;
    private bool playerTriggered;
    private bool puzzleShown;
    private Coroutine fadeRoutine;

    // ---------------- Ciclo de vida ----------------

    private void Awake()
    {
        if (puzzle != null)
            puzzle.SetActive(false);

        // Todos os sprites começam invisíveis.
        foreach (SpriteCue cue in spriteCues)
        {
            if (cue.sprite == null) continue;
            cue.baseAlpha = cue.sprite.color.a;
            SetCueAlpha(cue, 0f);
        }

        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;

            // Repassa os eventos de trigger do collider para este script.
            var relay = triggerCollider.gameObject.GetComponent<TriggerRelay>();
            if (relay == null)
                relay = triggerCollider.gameObject.AddComponent<TriggerRelay>();
            relay.Setup(this);
        }
    }

    private void Start()
    {
        StartCoroutine(AudioRoutine());
    }

    // ---------------- Áudio + sprites ----------------

    private IEnumerator AudioRoutine()
    {
        // Sem áudio: considera que ele já terminou.
        if (introAudio == null)
        {
            OnAudioFinished();
            yield break;
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        audioSource.clip = introAudio;
        audioSource.volume = audioVolume;
        audioSource.loop = false; // precisa terminar para liberar o puzzle
        audioSource.spatialBlend = 0f; // 2D: o volume não depende da posição

        if (audioDelay > 0f)
            yield return new WaitForSeconds(audioDelay);

        audioSource.Play();
        yield return null; // garante que isPlaying já esteja true

        // Acompanha o tempo do áudio enquanto ele toca.
        while (audioSource.isPlaying || !Application.isFocused)
        {
            UpdateCues(audioSource.time);
            yield return null;
        }

        OnAudioFinished();
    }

    private void UpdateCues(float time)
    {
        foreach (SpriteCue cue in spriteCues)
        {
            if (cue.sprite == null) continue;

            // Procura um intervalo que esteja valendo neste momento do áudio.
            SpriteInterval active = null;
            foreach (SpriteInterval interval in cue.intervals)
            {
                if (time >= interval.showAtTime && (interval.hideAtTime < 0f || time < interval.hideAtTime))
                {
                    active = interval;
                    break;
                }
            }

            bool shouldBeVisible = active != null;

            if (shouldBeVisible != cue.targetVisible)
            {
                cue.targetVisible = shouldBeVisible;

                if (shouldBeVisible)
                {
                    cue.lastActive = active;
                    FadeCue(cue, 1f, active.fadeInDuration);
                }
                else
                {
                    // Usa o fade out do intervalo que acabou de terminar.
                    float duration = cue.lastActive != null ? cue.lastActive.fadeOutDuration : 0.1f;
                    FadeCue(cue, 0f, duration);
                }
            }
            else if (shouldBeVisible)
            {
                cue.lastActive = active; // intervalos colados: guarda o atual
            }
        }
    }

    private void OnAudioFinished()
    {
        audioFinished = true;

        // Quem ainda está visível some com fade out.
        foreach (SpriteCue cue in spriteCues)
        {
            if (cue.sprite == null || !cue.targetVisible) continue;

            cue.targetVisible = false;
            float duration = cue.lastActive != null ? cue.lastActive.fadeOutDuration : 0.5f;
            FadeCue(cue, 0f, duration);
        }

        TryShowPuzzle();
    }

    private void FadeCue(SpriteCue cue, float target, float duration)
    {
        if (cue.routine != null)
            StopCoroutine(cue.routine);

        cue.routine = StartCoroutine(FadeCueRoutine(cue, target, duration));
    }

    private IEnumerator FadeCueRoutine(SpriteCue cue, float target, float duration)
    {
        float from = cue.visibility;

        if (duration <= 0f)
        {
            SetCueAlpha(cue, target);
            cue.routine = null;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            SetCueAlpha(cue, Mathf.Lerp(from, target, fadeCurve.Evaluate(Mathf.Clamp01(t / duration))));
            yield return null;
        }

        SetCueAlpha(cue, target);
        cue.routine = null;
    }

    private static void SetCueAlpha(SpriteCue cue, float visibility)
    {
        cue.visibility = visibility;

        Color c = cue.sprite.color;
        c.a = cue.baseAlpha * visibility;
        cue.sprite.color = c;

        cue.sprite.enabled = visibility > 0f; // desliga o renderer quando invisível
    }

    // ---------------- Trigger ----------------

    private void HandleTriggerEnter(Collider other)
    {
        if (playerTriggered) return;

        // Procura a tag no próprio objeto e nos pais (útil quando o collider está na câmera/mãos do XR Rig).
        if (!string.IsNullOrEmpty(playerTag) && !HasTagInParents(other.transform, playerTag)) return;

        playerTriggered = true;
        TryShowPuzzle();
    }

    private static bool HasTagInParents(Transform t, string tag)
    {
        while (t != null)
        {
            if (t.CompareTag(tag)) return true;
            t = t.parent;
        }
        return false;
    }

    // ---------------- Puzzle ----------------

    private void TryShowPuzzle()
    {
        if (puzzleShown || !audioFinished) return;
        if (triggerCollider != null && !playerTriggered) return;

        puzzleShown = true;
        ShowPuzzle();
    }

    /// <summary>Ativa o puzzle com fade in. Também pode ser chamado por outros scripts ou UnityEvents.</summary>
    public void ShowPuzzle()
    {
        if (puzzle == null)
        {
            Debug.LogWarning("[SceneController] Nenhum puzzle atribuído no Inspector.", this);
            return;
        }

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeInPuzzle());
    }

    private IEnumerator FadeInPuzzle()
    {
        puzzle.SetActive(true);

        // --- Caso UI: usa CanvasGroup ---
        CanvasGroup canvasGroup = puzzle.GetComponent<CanvasGroup>();
        if (canvasGroup == null && puzzle.GetComponentInChildren<Graphic>(true) != null)
            canvasGroup = puzzle.AddComponent<CanvasGroup>();

        // --- Caso 3D: usa o alpha dos materiais ---
        var materialTargets = new List<MaterialAlpha>();
        if (canvasGroup == null)
        {
            foreach (Renderer r in puzzle.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material m in r.materials) // cria instâncias, não altera o asset
                {
                    string prop = m.HasProperty("_BaseColor") ? "_BaseColor"
                                : m.HasProperty("_Color") ? "_Color"
                                : null;
                    if (prop != null)
                        materialTargets.Add(new MaterialAlpha(m, prop));
                }
            }
        }

        // Troca os materiais para Transparent durante o fade.
        foreach (var m in materialTargets) m.Begin();

        SetAlpha(canvasGroup, materialTargets, 0f);

        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            float a = fadeCurve.Evaluate(Mathf.Clamp01(t / fadeInDuration));
            SetAlpha(canvasGroup, materialTargets, a);
            yield return null;
        }

        SetAlpha(canvasGroup, materialTargets, 1f);

        // Restaura os materiais ao estado original (Opaque).
        foreach (var m in materialTargets) m.End();

        fadeRoutine = null;
    }

    private static void SetAlpha(CanvasGroup cg, List<MaterialAlpha> mats, float alpha)
    {
        if (cg != null)
            cg.alpha = alpha;

        for (int i = 0; i < mats.Count; i++)
            mats[i].Set(alpha);
    }

    // ---------------- Tipos auxiliares ----------------

    /// <summary>
    /// Controla o alpha de um material. Em URP Lit opaco, troca temporariamente para
    /// Transparent durante o fade e restaura o original no fim.
    /// </summary>
    private class MaterialAlpha
    {
        private readonly Material material;
        private readonly int propertyId;
        private readonly Color baseColor;

        private readonly bool isUrpLit;
        private readonly bool wasTransparent;
        private readonly float origSurface, origSrc, origDst, origZWrite;
        private readonly int origQueue;
        private readonly string origRenderType;

        public MaterialAlpha(Material material, string property)
        {
            this.material = material;
            propertyId = Shader.PropertyToID(property);
            baseColor = material.GetColor(propertyId);

            isUrpLit = material.HasProperty("_Surface");
            if (isUrpLit)
            {
                origSurface = material.GetFloat("_Surface");
                wasTransparent = origSurface >= 0.5f;
                origSrc = material.GetFloat("_SrcBlend");
                origDst = material.GetFloat("_DstBlend");
                origZWrite = material.GetFloat("_ZWrite");
                origQueue = material.renderQueue;
                origRenderType = material.GetTag("RenderType", false, "Opaque");
            }
        }

        /// <summary>Chamar antes do fade: troca para Transparent.</summary>
        public void Begin()
        {
            if (!isUrpLit || wasTransparent) return;

            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        public void Set(float alpha)
        {
            Color c = baseColor;
            c.a = baseColor.a * alpha;
            material.SetColor(propertyId, c);
        }

        /// <summary>Chamar depois do fade: volta ao material original.</summary>
        public void End()
        {
            Set(1f);
            if (!isUrpLit || wasTransparent) return;

            material.SetFloat("_Surface", origSurface);
            material.SetFloat("_SrcBlend", origSrc);
            material.SetFloat("_DstBlend", origDst);
            material.SetFloat("_ZWrite", origZWrite);
            material.SetOverrideTag("RenderType", origRenderType);
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = origQueue;
        }
    }

    /// <summary>Repassa OnTriggerEnter do collider para o SceneController.</summary>
    private class TriggerRelay : MonoBehaviour
    {
        private SceneController owner;

        public void Setup(SceneController controller) => owner = controller;

        private void OnTriggerEnter(Collider other)
        {
            if (owner != null)
                owner.HandleTriggerEnter(other);
        }
    }
}