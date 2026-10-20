using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class InteractableDoor : MonoBehaviour
{
    [SerializeField] private Vector3 closedRotation = Vector3.zero;
    [SerializeField] private Vector3 openedRotation = new(0f, 0f, 25f);

    [SerializeField] private float openDuration = 1f;
    [SerializeField] private float closeDuration = 0.3f;

    public Vector3 ClosedRotation => closedRotation;
    public Vector3 OpenedRotation => openedRotation;
    public float OpenDuration => openDuration;
    public float CloseDuration => closeDuration;

    private Rigidbody rigidbody;

    private bool originalKinematicState;

    public XROffsetGrabInteractable XRInteractable { get; private set; }

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();

        XRInteractable = GetComponent<XROffsetGrabInteractable>();

        originalKinematicState = rigidbody.isKinematic;

        if (!rigidbody.isKinematic)
        {
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
        }

        rigidbody.isKinematic = true;

        transform.localRotation = Quaternion.Euler(closedRotation);

        Physics.SyncTransforms();
    }

    void OnDestroy()
    {
        LeanTween.cancel(gameObject);
    }

    public void SetDoorColliders(bool enabled)
    {
        foreach (Collider collider in XRInteractable.colliders)
        {
            if (collider != null)
                collider.enabled = enabled;
        }
    }

    public void OpenDoor()
    {
        print("Abrindo porta");
        AnimateDoor(openedRotation, openDuration, LeanTweenType.easeOutCubic);
    }

    public void CloseDoor()
    {
        print("Fechando porta");
        AnimateDoor(closedRotation, closeDuration, LeanTweenType.linear);
    }

    private void AnimateDoor(Vector3 targetRotation, float duration, LeanTweenType easeType)
    {
        ForceRelease();
        
        LeanTween.cancel(gameObject);

        rigidbody.isKinematic = true;

        LeanTween.rotateLocal(gameObject, targetRotation, duration).setEase(easeType)
            .setOnComplete(() =>
            {
                transform.localRotation = Quaternion.Euler(targetRotation);

                Physics.SyncTransforms();

                rigidbody.isKinematic = originalKinematicState;
            });
    }

    public void SetDoorRotationImmediate(Vector3 rotation)
    {
        ForceRelease();

        LeanTween.cancel(gameObject);

        rigidbody.isKinematic = true;

        transform.localRotation = Quaternion.Euler(rotation);

        Physics.SyncTransforms();

        rigidbody.isKinematic = originalKinematicState;
    }

    private void ForceRelease()
    {
        if (!XRInteractable.isSelected)
            return;

        XRInteractionManager interactionManager = XRInteractable.interactionManager;

        foreach (IXRSelectInteractor interactor in XRInteractable.interactorsSelecting.ToArray())
        {
            interactionManager.SelectExit(interactor, XRInteractable);
        }
    }
}
