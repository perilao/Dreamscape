using UnityEngine;

[CreateAssetMenu( fileName = "CardMaterialDatabase", menuName = "Puzzles/Card Material Database" )]
public class CardMaterialDatabase : ScriptableObject
{
    [SerializeField] private Material redMaterial;
    [SerializeField] private Material orangeMaterial;
    [SerializeField] private Material greenMaterial;
    [SerializeField] private Material violetMaterial;

    public Material GetMaterial(InteractableCard.CardColors color)
    {
        return color switch
        {
            InteractableCard.CardColors.Red => redMaterial,
            InteractableCard.CardColors.Orange => orangeMaterial,
            InteractableCard.CardColors.Green => greenMaterial,
            InteractableCard.CardColors.Violet => violetMaterial,
            _ => null
        };
    }

    public Color GetMaterialColor (InteractableCard.CardColors color)
    {
        Material material = GetMaterial(color);
        
        return material != null ? material.color : Color.white;
    }
}