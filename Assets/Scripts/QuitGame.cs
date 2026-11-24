using System.Collections;
using UnityEngine;

public class QuitGame : MonoBehaviour
{
    [Tooltip("Espera alguns segundos antes de fechar (opcional).")]
    [SerializeField] private float delay = 0f;

    public void Quit() => StartCoroutine(QuitRoutine());

    private IEnumerator QuitRoutine()
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}