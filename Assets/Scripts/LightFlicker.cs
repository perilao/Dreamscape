using UnityEngine;

[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    public float intensidadeMinima = 0.5f;
    public float intensidadeMaxima = 1.5f;
    public float velocidade = 8f;

    Light luz;
    float deslocamento;

    void Awake()
    {
        luz = GetComponent<Light>();
        deslocamento = Random.value * 100f;
    }

    void Update()
    {
        float ruido = Mathf.PerlinNoise(Time.time * velocidade, deslocamento);
        luz.intensity = Mathf.Lerp(intensidadeMinima, intensidadeMaxima, ruido);
    }
}