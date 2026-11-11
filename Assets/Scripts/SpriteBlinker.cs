using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Faz vários sprites 2D aparecerem e sumirem, um de cada vez (nunca dois ao mesmo tempo),
/// com tempos individuais, fade e randomização. Compatível com Unity 6.
/// </summary>
public class SpriteBlinker : MonoBehaviour
{
    [System.Serializable]
    public class BlinkEntry
    {
        [Tooltip("Nome só para organizar a lista.")]
        public string label;

        [Tooltip("SpriteRenderer que vai aparecer e sumir.")]
        public SpriteRenderer sprite;

        [Tooltip("Quanto tempo este sprite fica totalmente visível.")]
        [Min(0f)] public float visibleTime = 2f;

        [Tooltip("Duração do fade ao aparecer. 0 = aparece de uma vez.")]
        [Min(0f)] public float fadeInDuration = 0.5f;

        [Tooltip("Duração do fade ao sumir. 0 = some de uma vez.")]
        [Min(0f)] public float fadeOutDuration = 0.5f;

        [System.NonSerialized] public float baseAlpha = 1f;
    }

    [Header("Sprites (um de cada vez)")]
    [Tooltip("Cada item é um sprite. Só um fica visível por vez: o próximo só começa quando o anterior já sumiu.")]
    [SerializeField] private List<BlinkEntry> entries = new List<BlinkEntry>();

    [Header("Tempos globais (segundos)")]
    [Tooltip("Tempo com todos os sprites invisíveis entre um sprite e o próximo.")]
    [SerializeField] private float hiddenTime = 2f;

    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Randomização")]
    [Tooltip("Varia os tempos a cada vez (0 = sem variação, 0.3 = até 30% para mais ou para menos).")]
    [Range(0f, 0.9f)]
    [SerializeField] private float randomVariation = 0.2f;

    [Tooltip("Escolhe o próximo sprite aleatoriamente (nunca repete o mesmo em seguida). Desligado = segue a ordem da lista.")]
    [SerializeField] private bool randomOrder = true;

    [Tooltip("Começa depois de um atraso aleatório, para vários SpriteBlinkers não ficarem sincronizados.")]
    [SerializeField] private bool randomizeStart = true;

    [Header("Opções")]
    [Tooltip("Desliga o renderer quando está invisível (economiza processamento).")]
    [SerializeField] private bool disableRendererWhenHidden = true;

    private readonly List<BlinkEntry> valid = new List<BlinkEntry>();

    private void Awake()
    {
        valid.Clear();

        foreach (BlinkEntry e in entries)
        {
            if (e.sprite == null) continue;
            e.baseAlpha = e.sprite.color.a; // respeita o alpha original do sprite
            valid.Add(e);
        }

        if (valid.Count == 0)
        {
            Debug.LogWarning("[SpriteBlinker] Nenhum SpriteRenderer atribuído na lista.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        StartCoroutine(BlinkLoop());
    }

    private IEnumerator BlinkLoop()
    {
        // Todos começam invisíveis.
        foreach (BlinkEntry e in valid)
            SetAlpha(e, 0f);

        if (randomizeStart)
            yield return new WaitForSeconds(Random.value * hiddenTime);

        int last = -1;

        while (true)
        {
            int index = NextIndex(last);
            last = index;
            BlinkEntry e = valid[index];

            // Tudo acontece em sequência, então nunca há dois sprites visíveis ao mesmo tempo.
            yield return Fade(e, 0f, 1f, Vary(e.fadeInDuration));
            yield return new WaitForSeconds(Vary(e.visibleTime));
            yield return Fade(e, 1f, 0f, Vary(e.fadeOutDuration));
            yield return new WaitForSeconds(Vary(hiddenTime));
        }
    }

    private int NextIndex(int last)
    {
        if (valid.Count == 1) return 0;

        if (!randomOrder)
            return (last + 1) % valid.Count;

        int r;
        do { r = Random.Range(0, valid.Count); }
        while (r == last);
        return r;
    }

    // Aplica a variação aleatória a um tempo.
    private float Vary(float time)
    {
        float v = randomVariation;
        return Mathf.Max(0f, time * (1f + Random.Range(-v, v)));
    }

    private IEnumerator Fade(BlinkEntry e, float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            SetAlpha(e, to);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            SetAlpha(e, Mathf.Lerp(from, to, fadeCurve.Evaluate(Mathf.Clamp01(t / duration))));
            yield return null;
        }

        SetAlpha(e, to);
    }

    private void SetAlpha(BlinkEntry e, float a)
    {
        Color c = e.sprite.color;
        c.a = e.baseAlpha * a;
        e.sprite.color = c;

        if (disableRendererWhenHidden)
            e.sprite.enabled = a > 0f;
    }
}