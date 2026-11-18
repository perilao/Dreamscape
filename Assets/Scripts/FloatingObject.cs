using UnityEngine;

/// <summary>
/// Faz o objeto flutuar suavemente para cima e para baixo (movimento senoidal).
/// Opcionalmente balança levemente, como se estivesse no ar.
/// Compatível com Unity 6.
/// </summary>
[DisallowMultipleComponent]
public class FloatingObject : MonoBehaviour
{
   

    [Header("Flutuação")]
    [Tooltip("Distância máxima que o objeto sobe/desce a partir da posição inicial (em unidades).")]
    [SerializeField] private float amplitude = 0.5f;
    [Tooltip("Multiplicador de velocidade. 1 = normal, 0.5 = metade, 0.25 = bem lento.")]
    [SerializeField] private float speedMultiplier = 0.4f;

    [Tooltip("Quantos ciclos completos (sobe e desce) por segundo.")]
    [SerializeField] private float frequency = 0.4f;

    [Header("Variação")]
    [Tooltip("Dá uma fase aleatória para cada objeto, assim as casas não flutuam todas sincronizadas.")]
    [SerializeField] private bool randomizePhase = true;

    [Tooltip("Varia levemente a velocidade e a altura de cada objeto (0 = sem variação, 0.3 = até 30%).")]
    [Range(0f, 0.5f)]
    [SerializeField] private float randomVariation = 0.2f;

    [Header("Balanço (opcional)")]
    [Tooltip("Inclinação máxima em graus. 0 desativa o balanço.")]
    [SerializeField] private float tiltAmount = 2f;

    [Tooltip("Velocidade do balanço.")]
    [SerializeField] private float tiltSpeed = 0.3f;

    private Vector3 startLocalPosition;
    private Quaternion startLocalRotation;
    private float phase;
    private float finalAmplitude;
    private float finalFrequency;

    private void Awake()
    {
        startLocalPosition = transform.localPosition;
        startLocalRotation = transform.localRotation;

        phase = randomizePhase ? Random.value * Mathf.PI * 2f : 0f;

        float v = randomVariation;
        finalAmplitude = amplitude * (1f + Random.Range(-v, v));
        finalFrequency = frequency * (1f + Random.Range(-v, v));
    }

    private void Update()
    {
        float t = Time.time;

        // Subida e descida suave (seno).
        float offset = Mathf.Sin(t * finalFrequency * speedMultiplier * Mathf.PI * 2f + phase) * finalAmplitude;
        transform.localPosition = startLocalPosition + new Vector3(0f, offset, 0f);

        // Balanço leve, usando a mesma fase para parecer natural.
        if (tiltAmount > 0f)
        {
            float tiltX = Mathf.Sin(t * tiltSpeed * Mathf.PI * 2f + phase) * tiltAmount;
            float tiltZ = Mathf.Cos(t * tiltSpeed * Mathf.PI * 2f + phase * 1.3f) * tiltAmount;
            transform.localRotation = startLocalRotation * Quaternion.Euler(tiltX, 0f, tiltZ);
        }
    }
}
