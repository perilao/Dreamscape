using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    private enum Button
    {
        A, 
        B, 
        X, 
        Y
    }

    /// <summary>
    /// Sequência de botões que o jogador deve pressionar para reiniciar a cena.
    /// </summary>
    [SerializeField] private List<Button> sequence = new()
    {
        Button.A,
        Button.B,
        Button.X,
        Button.Y,
        Button.A,
        Button.B
    };

    private int currentIndex = 0;

    /// <summary>
    /// Tempo máximo em segundos permitido entre os inputs do jogador para que a sequência seja considerada válida.
    /// </summary>
    [SerializeField] private float maxInputInterval = 2f;

    private float lastInputTime;
    private bool reloading;

    private void Update()
    {
        if (reloading)
            return;

        if (currentIndex > 0 && Time.unscaledTime - lastInputTime > maxInputInterval)
        {
            currentIndex = 0;
        }

        // Teclado
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.aKey.wasPressedThisFrame)
                ProcessInput(Button.A);

            if (keyboard.bKey.wasPressedThisFrame)
                ProcessInput(Button.B);

            if (keyboard.xKey.wasPressedThisFrame)
                ProcessInput(Button.X);

            if (keyboard.yKey.wasPressedThisFrame)
                ProcessInput(Button.Y);
        }

        var left = UnityEngine.XR.InputDevices.GetDeviceAtXRNode( UnityEngine.XR.XRNode.LeftHand );

        var right = UnityEngine.XR.InputDevices.GetDeviceAtXRNode( UnityEngine.XR.XRNode.RightHand );

        // Leitura dos botões VR
        CheckVRButton(right, UnityEngine.XR.CommonUsages.primaryButton, Button.A);
        CheckVRButton(right, UnityEngine.XR.CommonUsages.secondaryButton, Button.B);

        CheckVRButton(left, UnityEngine.XR.CommonUsages.primaryButton, Button.X);
        CheckVRButton(left, UnityEngine.XR.CommonUsages.secondaryButton, Button.Y);
    }

    private readonly bool[] previousVRStates = new bool[4];

    private void CheckVRButton(UnityEngine.XR.InputDevice device, UnityEngine.XR.InputFeatureUsage<bool> usage, Button button){
        int index = (int)button;

        bool pressed = device.isValid && device.TryGetFeatureValue(usage, out bool value) && value;

        if (pressed && !previousVRStates[index])
            ProcessInput(button);

        previousVRStates[index] = pressed;
    }

    private void ProcessInput(Button button)
    {
        if (reloading)
            return;

        if (button == sequence[currentIndex])
        {
            currentIndex++;
            lastInputTime = Time.unscaledTime;

            if (currentIndex >= sequence.Count)
            {
                reloading = true;
                ReloadScene();
            }
        }
        else
        {
            currentIndex = button == sequence[0] ? 1 : 0;
            lastInputTime = Time.unscaledTime;
        }
    }

    private void ReloadScene()
    {
        SceneManager.LoadScene( SceneManager.GetActiveScene().buildIndex );
    }
}
