using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(HingeJoint))]
public class HingeJointListener : MonoBehaviour
{
    public UnityEvent OnMinLimitReached;
    public UnityEvent OnMaxLimitReached;

    [SerializeField] private float angleBetweenThreshold = 1f;

    public HingeJointState hingeJointState = HingeJointState.None;

    public bool IgnoreNoneState = false;
    public bool DebbugState = false;

    public enum HingeJointState
    {
        Min,
        Max,
        None
    }

    private HingeJoint hinge;

    private void Awake()
    {
        hinge = GetComponent<HingeJoint>();
    }

    private void FixedUpdate()
    {
        if (hinge == null)
            return;

        float angle = hinge.angle;

        if (float.IsNaN(angle) || float.IsInfinity(angle))
            angle = 0f;

        JointLimits limits = hinge.limits;

        float angleWithMinLimit = Mathf.Abs(angle - limits.min);
        float angleWithMaxLimit = Mathf.Abs(angle - limits.max);

        if (DebbugState)
        {
            Debug.Log($"{name} | Angle: {angle} | " + $"Min: {limits.min} | Max: {limits.max}");
        }

        HingeJointState newState = HingeJointState.None;

        if (angleWithMinLimit <= angleBetweenThreshold)
        {
            newState = HingeJointState.Min;
        }
        else if (angleWithMaxLimit <= angleBetweenThreshold)
        {
            newState = HingeJointState.Max;
        }

        // Evita disparar eventos repetidamente.
        if (newState == hingeJointState)
            return;

        if (newState == HingeJointState.None && IgnoreNoneState)
            return;

        hingeJointState = newState;

        switch (newState)
        {
            case HingeJointState.Min:
                OnMinLimitReached?.Invoke();
                break;

            case HingeJointState.Max:
                OnMaxLimitReached?.Invoke();
                break;
        }
    }
}