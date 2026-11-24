using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Abre e fecha um objeto escalonando UM eixo de forma suave (ex.: uma porta/quadrado branco abrindo).
/// A escala que o objeto tem na cena é o estado ABERTO. O estado fechado é a escala mínima definida abaixo.
/// A borda escolhida (Anchor) fica parada, e o objeto cresce a partir dela.
/// Compatível com Unity 6.
/// </summary>
public class ScaleOpener : MonoBehaviour
{
    public enum ScaleAxis { X, Y, Z }

    public enum Anchor
    {
        [Tooltip("A borda do lado negativo do eixo fica parada e o objeto cresce para o lado positivo (no Y: de baixo para cima).")]
        NegativeEdge,
        [Tooltip("O objeto cresce para os dois lados a partir do centro.")]
        Center,
        [Tooltip("A borda do lado positivo do eixo fica parada e o objeto cresce para o lado negativo (no Y: de cima para baixo).")]
        PositiveEdge
    }

    [Header("Eixo")]
    [Tooltip("Eixo LOCAL que vai ser escalonado.")]
    [SerializeField] private ScaleAxis axis = ScaleAxis.X;

    [SerializeField] private Anchor anchor = Anchor.NegativeEdge;

    [Header("Tamanho")]
    [Tooltip("Escala do eixo quando fechado. 0 = some por completo.")]
    [Min(0f)]
    [SerializeField] private float closedScale = 0f;

    [Tooltip("Tamanho do mesh no eixo com escala 1. Cube e Quad do Unity têm 1.")]
    [Min(0.0001f)]
    [SerializeField] private float meshSize = 1f;

    [Header("Animação")]
    [SerializeField] private float duration = 1.5f;
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Começa aberto. Desligado = começa fechado e abre quando você chamar Open().")]
    [SerializeField] private bool startOpen = false;

    [Tooltip("Abre sozinho assim que a cena carrega (bom para testar).")]
    [SerializeField] private bool openOnStart = false;

    [Header("Eventos")]
    public UnityEvent onOpened;
    public UnityEvent onClosed;

    private Vector3 openPosition;
    private Vector3 openLocalScale;
    private Vector3 axisVector;
    private int axisIndex;
    private float openScale;

    private float progress;      // 0 = fechado, 1 = aberto
    private Coroutine routine;

    [SerializeField] private ScaleOpener door;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private SceneChanger changer;

    [Header("Abrir por colisão (opcional)")]
    [Tooltip("Collider da zona. Use um objeto SEPARADO da porta.")]
    [SerializeField] private Collider triggerCollider;

    [Tooltip("Collider do jogador para comparar. Se vazio, usa a tag.")]
    [SerializeField] private Collider playerCollider;

    [Tooltip("Fecha a porta quando o jogador sai da zona.")]
    [SerializeField] private bool closeOnExit = false;

    public bool IsOpen => progress >= 1f;

    private void Awake()
    {
        axisIndex = (int)axis;
        axisVector = axis == ScaleAxis.X ? Vector3.right : axis == ScaleAxis.Y ? Vector3.up : Vector3.forward;

        // O que está na cena é o estado aberto.
        openPosition = transform.localPosition;
        openLocalScale = transform.localScale;
        openScale = Mathf.Abs(openLocalScale[axisIndex]);

        progress = startOpen ? 1f : 0f;
        Apply(progress);

        SetupTrigger();
    }

    private void Start()
    {
        if (openOnStart)
            Open();
    }

    // ---------------- API pública (pode ser chamada por UnityEvent ou outro script) ----------------

    public void Open() => Animate(1f);

    public void Close() => Animate(0f);

    public void Toggle() => Animate(progress >= 0.5f ? 0f : 1f);

    // ---------------- Interno ----------------

    private void Animate(float target)
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(AnimateRoutine(target));
    }

    private IEnumerator AnimateRoutine(float target)
    {
        float start = progress;

        // Se estava no meio do caminho, só gasta o tempo proporcional ao que falta.
        float time = duration * Mathf.Abs(target - start);

        if (time > 0f)
        {
            float t = 0f;
            while (t < time)
            {
                t += Time.deltaTime;
                progress = Mathf.Lerp(start, target, Mathf.Clamp01(t / time));
                Apply(progress);
                yield return null;
            }
        }

        progress = target;
        Apply(progress);
        routine = null;

        if (target >= 1f) onOpened?.Invoke();
        else onClosed?.Invoke();
    }

    private void Apply(float p)
    {
        float s = Mathf.Lerp(closedScale, openScale, curve.Evaluate(p));

        // Escala só no eixo escolhido.
        Vector3 scale = openLocalScale;
        scale[axisIndex] = s;
        transform.localScale = scale;

        // Compensa a posição para a borda escolhida ficar parada.
        float sign = anchor == Anchor.NegativeEdge ? 1f : anchor == Anchor.PositiveEdge ? -1f : 0f;
        Vector3 dir = transform.localRotation * axisVector;
        transform.localPosition = openPosition + dir * ((s - openScale) * meshSize * 0.5f * sign);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        if (changer == null)
        {
            return;
        }
        changer.LoadConfiguredScene();
    }

    private void SetupTrigger()
    {
        if (triggerCollider == null) return;

        triggerCollider.isTrigger = true;

        var relay = triggerCollider.gameObject.GetComponent<TriggerRelay>();
        if (relay == null)
            relay = triggerCollider.gameObject.AddComponent<TriggerRelay>();
        relay.Setup(this);
    }

    private bool IsPlayer(Collider other)
    {
        if (playerCollider != null)
            return other == playerCollider;

        if (string.IsNullOrEmpty(playerTag))
            return true;

        Transform t = other.transform;
        while (t != null)
        {
            if (t.CompareTag(playerTag)) return true;
            t = t.parent;
        }

        return false;
    }

    private void HandleTriggerEnter(Collider other)
    {
        if (IsPlayer(other))
            Open();
    }

    private void HandleTriggerExit(Collider other)
    {
        if (closeOnExit && IsPlayer(other))
            Close();
    }

    private class TriggerRelay : MonoBehaviour
    {
        private ScaleOpener owner;

        public void Setup(ScaleOpener opener) => owner = opener;

        private void OnTriggerEnter(Collider other)
        {
            if (owner != null) owner.HandleTriggerEnter(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (owner != null) owner.HandleTriggerExit(other);
        }
    }
}
