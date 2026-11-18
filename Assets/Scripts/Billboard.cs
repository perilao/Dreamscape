using UnityEngine;

/// <summary>
/// Faz o objeto sempre "olhar" para a câmera (billboard).
/// Compatível com Unity 6 (Built-in, URP e HDRP).
/// Adicione este componente ao objeto (sprite, quad, TextMeshPro 3D, ícone de vida etc.).
/// </summary>
[DisallowMultipleComponent]
public class Billboard : MonoBehaviour
{
    public enum BillboardMode
    {
        /// <summary>Copia a rotação da câmera. Sem distorção; ideal para sprites, ícones e textos.</summary>
        CameraAligned,

        /// <summary>Aponta para a posição da câmera em todos os eixos.</summary>
        LookAtCamera,

        /// <summary>Gira apenas no eixo Y (árvores, personagens 2D em mundo 3D que ficam em pé).</summary>
        YAxisOnly
    }

    [Header("Configuração")]
    [Tooltip("Câmera alvo. Se vazia, usa a Camera.main.")]
    [SerializeField] private Camera targetCamera;

    [SerializeField] private BillboardMode mode = BillboardMode.CameraAligned;

    [Tooltip("Marque se o objeto aparece espelhado/de costas (ex.: Quad ou SpriteRenderer em alguns casos).")]
    [SerializeField] private bool invertFacing = false;

    [Header("Opcional")]
    [Tooltip("Rotação extra aplicada depois do billboard (em graus).")]
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;

    [Tooltip("Executa também no modo Edit (para ver o resultado na Scene/Game sem dar Play).")]
    [SerializeField] private bool runInEditMode = false;

    private Transform camTransform;

    private void OnEnable()
    {
        CacheCamera();
    }

    private void CacheCamera()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        camTransform = targetCamera != null ? targetCamera.transform : null;
    }

    // LateUpdate garante que a câmera já se moveu neste frame (evita tremedeira).
    private void LateUpdate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && !runInEditMode)
            return;
#endif
        if (camTransform == null)
        {
            CacheCamera();
            if (camTransform == null) return;
        }

        Quaternion rot;

        switch (mode)
        {
            case BillboardMode.LookAtCamera:
            {
                Vector3 dir = camTransform.position - transform.position;
                if (dir.sqrMagnitude < 0.0001f) return;
                if (!invertFacing) dir = -dir;
                rot = Quaternion.LookRotation(dir, camTransform.up);
                break;
            }

            case BillboardMode.YAxisOnly:
            {
                Vector3 dir = camTransform.position - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.0001f) return;
                if (!invertFacing) dir = -dir;
                rot = Quaternion.LookRotation(dir, Vector3.up);
                break;
            }

            default: // CameraAligned
            {
                rot = camTransform.rotation;
                if (invertFacing)
                    rot *= Quaternion.Euler(0f, 180f, 0f);
                break;
            }
        }

        transform.rotation = rot * Quaternion.Euler(rotationOffset);
    }

    /// <summary>Permite trocar a câmera via código (split-screen, câmera de cutscene etc.).</summary>
    public void SetCamera(Camera cam)
    {
        targetCamera = cam;
        camTransform = cam != null ? cam.transform : null;
    }


}
