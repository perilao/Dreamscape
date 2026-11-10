using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    [Tooltip("Nome da cena usada por LoadConfiguredScene(). Ela precisa estar no Build Profiles.")]
    [SerializeField] private string sceneName;

    public void LoadConfiguredScene() => SceneManager.LoadScene(sceneName);

}