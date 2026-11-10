using System.Collections;
using UnityEngine;

/// <summary>
/// Faz um sprite 2D aparecer e sumir em ciclos (com fade), com tempos configuráveis
/// e randomização, como no FloatingObject. Compatível com Unity 6.
/// </summary>
public class SpriteBlinker : MonoBehaviour
{
    [Header("Modelo")]
    [Tooltip("SpriteRenderer que vai aparecer e sumir. Se vazio, procura no próprio objeto ou nos filhos.")]
    [SerializeField] private SpriteRenderer target;

    [Header("Tempos (segundos)")]
    [Tooltip("Quanto tempo o sprite fica totalmente visível.")]
    [SerializeField] private float visibleTime = 2f;

    [Tooltip("Quanto tempo o sprite fica totalmente invisível.")]
    [SerializeField] private float hiddenTime = 2f;

    [Tooltip("Duração do fade ao aparecer. 0 = aparece de uma vez.")]
    [SerializeField] private float fadeInDuration = 0.5f;

    [Tooltip("Duração do fade ao sumir. 0 = some de uma vez.")]
    [SerializeField] private float fadeOutDuration = 0.5f;

    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Randomização")]
    [Tooltip("Varia os tempos a cada ciclo (0 = sem variação, 0.3 = até 30% para mais ou para menos).")]
    [Range(0f, 0.9f)]
    [SerializeField] private float randomVariation = 0.2f;

    [Tooltip("Começa em um ponto aleatório do ciclo, para vários sprites não piscarem sincronizados.")]
    [SerializeField] private bool randomizeStart = true;

    [Header("Opções")]
    [Tooltip("Começa visível. Se desligado, começa invisível.")]
    [SerializeField] private bool startVisible = false;

    [Tooltip("Desliga o renderer quando está invisível (economiza processamento).")]
    [SerializeField] private bool disableRendererWhenHidden = true;

    private float baseAlpha = 1f;

    private void Awake()
    {
        if (target == null)
            target = GetComponentInChildren<SpriteRenderer>(true);

        if (target == null)
        {
            Debug.LogWarning("[SpriteBlinker] Nenhum SpriteRenderer encontrado.", this);
            enabled = false;
            return;
        }

        baseAlpha = target.color.a; // respeita o alpha original do sprite
    }

    private void OnEnable()
    {
        StartCoroutine(BlinkLoop());
    }

    private IEnumerator BlinkLoop()
    {
        SetAlpha(startVisible ? 1f : 0f);

        // Atraso inicial aleatório para dessincronizar vários sprites.
        if (randomizeStart)
            yield return new WaitForSeconds(Random.value * (visibleTime + hiddenTime));

        bool visible = startVisible;

        while (true)
        {
            if (visible)
            {
                yield return Wait(visibleTime);
                yield return Fade(1f, 0f, Vary(fadeOutDuration));
            }
            else
            {
                yield return Wait(hiddenTime);
                yield return Fade(0f, 1f, Vary(fadeInDuration));
            }

            visible = !visible;
        }
    }

    private WaitForSeconds Wait(float time) => new WaitForSeconds(Vary(time));

    // Aplica a variação aleatória a um tempo.
    private float Vary(float time)
    {
        float v = randomVariation;
        return Mathf.Max(0f, time * (1f + Random.Range(-v, v)));
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (to > 0f)
            target.enabled = true; // liga antes de aparecer

        if (duration <= 0f)
        {
            SetAlpha(to);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Lerp(from, to, fadeCurve.Evaluate(t / duration)));
            yield return null;
        }

        SetAlpha(to);
    }

    private void SetAlpha(float a)
    {
        Color c = target.color;
        c.a = baseAlpha * a;
        target.color = c;

        if (disableRendererWhenHidden)
            target.enabled = a > 0f;
    }
}
