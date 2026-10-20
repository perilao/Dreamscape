using UnityEngine;

public static class LeanTweenRuntimeInitializer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize()
    {
        LeanTween.reset();
    }
}