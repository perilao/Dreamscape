using UnityEngine;

public class BathroomEnterTriggerCollision : MonoBehaviour
{
    [SerializeField] private Puzzle3Manager puzzle3Manager;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && puzzle3Manager != null)
            puzzle3Manager.OnPlayerEnteredRoom();
    }
}
