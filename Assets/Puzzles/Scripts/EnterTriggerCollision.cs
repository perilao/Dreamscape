using UnityEngine;
using UnityEngine.Events;

public class EnterTriggerCollision : MonoBehaviour
{
    public UnityEvent OnPlayerEnteredRoom;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            OnPlayerEnteredRoom?.Invoke();
    }
}
