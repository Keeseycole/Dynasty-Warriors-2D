using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class FactionColorSwapper : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock propertyBlock;

    [System.Serializable]
    public struct TeamColors10
    {
        public Color target1; public Color target2; public Color target3; public Color target4; public Color target5;
        public Color target6; public Color target7; public Color target8; public Color target9; public Color target10;
    }

    [Header("Dynamic 10-Slot Team Color Palettes")]
    public TeamColors10 playerSideColors; // Configure your 10 player team colors in the Inspector
    public TeamColors10 enemySideColors;  // Configure your 10 enemy team colors in the Inspector

    // Pre-hash property name IDs to completely avoid performance-heavy string lookups at runtime
    private static readonly int[] TargetIDs = new int[]
    {
        Shader.PropertyToID("_TargetColor1"),  Shader.PropertyToID("_TargetColor2"),
        Shader.PropertyToID("_TargetColor3"),  Shader.PropertyToID("_TargetColor4"),
        Shader.PropertyToID("_TargetColor5"),  Shader.PropertyToID("_TargetColor6"),
        Shader.PropertyToID("_TargetColor7"),  Shader.PropertyToID("_TargetColor8"),
        Shader.PropertyToID("_TargetColor9"),  Shader.PropertyToID("_TargetColor10")
    };

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        propertyBlock = new MaterialPropertyBlock();
    }

    void Start()
    {
        ApplyFactionPalette();
    }

    public void ApplyFactionPalette()
    {
        MusouUnit baseUnit = GetComponent<MusouUnit>() ?? GetComponentInParent<MusouUnit>();
        if (baseUnit == null) return;

        spriteRenderer.GetPropertyBlock(propertyBlock);

        // Pull the correct data array depending on our squad faction affiliation
        TeamColors10 chosenPalette = (baseUnit.unitTeam == MusouUnit.Team.PlayerSide) ? playerSideColors : enemySideColors;

        // Efficiently pass all 10 color configurations directly into the shader memory registers
        propertyBlock.SetColor(TargetIDs[0], chosenPalette.target1);
        propertyBlock.SetColor(TargetIDs[1], chosenPalette.target2);
        propertyBlock.SetColor(TargetIDs[2], chosenPalette.target3);
        propertyBlock.SetColor(TargetIDs[3], chosenPalette.target4);
        propertyBlock.SetColor(TargetIDs[4], chosenPalette.target5);
        propertyBlock.SetColor(TargetIDs[5], chosenPalette.target6);
        propertyBlock.SetColor(TargetIDs[6], chosenPalette.target7);
        propertyBlock.SetColor(TargetIDs[7], chosenPalette.target8);
        propertyBlock.SetColor(TargetIDs[8], chosenPalette.target9);
        propertyBlock.SetColor(TargetIDs[9], chosenPalette.target10);

        spriteRenderer.SetPropertyBlock(propertyBlock);
    }
}