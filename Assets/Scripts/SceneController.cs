using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla a cena atual:
///  1. Toca o áudio inicial quando a cena começa.
///  2. Detecta quando o jogador atravessa um collider (trigger).
///  3. Ativa o puzzle (SetActive true) com um fade in gradual.
///
/// Compatível com Unity 6 (URP). Coloque este script em um objeto vazio da cena
/// (ex.: "SceneController") e preencha os campos no Inspector.
/// </summary>
public class SceneController : MonoBehaviour
{
    [Header("Áudio inicial")]
    [Tooltip("Áudio que toca assim que a cena começa.")]
    [SerializeField] private AudioClip introAudio;

    [Tooltip("Opcional. Se vazio, um AudioSource é criado automaticamente.")]
    [SerializeField] private AudioSource audioSource;

    [Range(0f, 1f)]
    [SerializeField] private float audioVolume = 1f;

    [Tooltip("Atraso (em segundos) antes de tocar o áudio.")]
    [SerializeField] private float audioDelay = 0f;

    [SerializeField] private bool loopAudio = false;

    [Header("Detecção do jogador")]
    [Tooltip("Collider que o jogador vai atravessar. Ele será configurado como Trigger automaticamente.")]
    [SerializeField] private Collider triggerCollider;

    [Tooltip("Tag do jogador. Deixe vazio para aceitar qualquer objeto.")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("Se ligado, o puzzle só é ativado na primeira vez que o jogador passar.")]
    [SerializeField] private bool triggerOnlyOnce = true;

    [Header("Puzzle")]
    [Tooltip("GameObject do puzzle. Ele começa desativado e aparece com fade in.")]
    [SerializeField] private GameObject puzzle;

    [SerializeField] private float fadeInDuration = 1.5f;

    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private bool alreadyTriggered;
    private Coroutine fadeRoutine;

    // ---------------- Ciclo de vida ----------------

    private void Awake()
    {
        if (puzzle != null)
            puzzle.SetActive(false);

        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;

            // Repassa os eventos de trigger do collider para este script.
            var relay = triggerCollider.gameObject.GetComponent<TriggerRelay>();
            if (relay == null)
                relay = triggerCollider.gameObject.AddComponent<TriggerRelay>();
            relay.Setup(this);
        }
        else
        {
            Debug.LogWarning("[SceneController] Nenhum Trigger Collider atribuído no Inspector.", this);
        }
    }

    private void Start()
    {
        PlayIntroAudio();
    }

    // ---------------- Áudio ----------------

    private void PlayIntroAudio()
    {
        if (introAudio == null) return;

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        audioSource.clip = introAudio;
        audioSource.volume = audioVolume;
        audioSource.loop = loopAudio;
        audioSource.spatialBlend = 0f; // 2D: o volume não depende da posição

        if (audioDelay > 0f)
            audioSource.PlayDelayed(audioDelay);
        else
            audioSource.Play();
    }

    // ---------------- Trigger ----------------

    private void HandleTriggerEnter(Collider other)
    {
        if (triggerOnlyOnce && alreadyTriggered) return;

        // Procura a tag no próprio objeto e nos pais (útil quando o collider está na câmera/mãos do XR Rig).
        if (!string.IsNullOrEmpty(playerTag) && !HasTagInParents(other.transform, playerTag)) return;

        alreadyTriggered = true;
        ShowPuzzle();
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
