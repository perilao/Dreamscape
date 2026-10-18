using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(CardGrabInteractable))]
public class InteractableCard : PuzzleObject
{
    public CardGrabInteractable GrabInteractable => grabInteractable;
    [SerializeField] protected CardColors defaultCardColor;
    public CardColors DefaultCardColor => defaultCardColor;
    public CardColors CurrentCardColor { get; protected set; }

    [SerializeField] protected CardGrabInteractable grabInteractable;

    [SerializeField] protected MeshRenderer meshRenderer;

    [SerializeField] protected bool isColorChangeable = true;

    [Header("Materiais")]
    [SerializeField] protected CardMaterialDatabase CardMaterialDatabase;

    public enum CardColors
    {
        Red,
        Orange,
        Green,
        Violet
    }

    protected MaterialPropertyBlock propertyBlock;

    protected static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    void Start()
    {
        SwitchMaterial(defaultCardColor);

        CurrentCardColor = defaultCardColor;
    }

    protected void SwitchMaterial(CardColors color)
    {
        Material targetMaterial = CardMaterialDatabase.GetMaterial(color);

        if (targetMaterial == null)
        {
            Debug.LogWarning($"Material não definido para a cor {color}.", this);
            return;
        }

        meshRenderer.sharedMaterial = targetMaterial;
    }

    public void SwitchColor(CardColors newColor, bool isForceUpdate = false)
    {
        if (CurrentCardColor == newColor && !isForceUpdate)
            return;

        CurrentCardColor = newColor;
        UpdateColorMaterial();
    }

    private void UpdateColorMaterial()
    {
        if (meshRenderer == null || CardMaterialDatabase == null)
            return;

        propertyBlock ??= new MaterialPropertyBlock();

        Color targetColor = CardMaterialDatabase.GetMaterialColor(CurrentCardColor);

        meshRenderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetColor(BaseColorId, targetColor);

        meshRenderer.SetPropertyBlock(propertyBlock);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        ConnectOnValidate();
    }

    protected void ConnectOnValidate()
    {
        // Evita acumular chamadas enquanto o Inspector é atualizado.
        UnityEditor.EditorApplication.delayCall -= ApplyDefaultMaterial;
        UnityEditor.EditorApplication.delayCall += ApplyDefaultMaterial;
    }

    protected void ApplyDefaultMaterial()
    {
        // O objeto pode ter sido destruído antes da execução.
        if (this == null ||
            meshRenderer == null ||
            CardMaterialDatabase == null)
        {
            return;
        }

        SwitchMaterial(defaultCardColor);
    }
#endif
}
