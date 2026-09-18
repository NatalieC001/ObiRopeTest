using UnityEngine;

public class EnvironmentTag : MonoBehaviour
{
    public enum TagType
    {
        Cover,
        HardCover,
        Chokepoint,
        Pillar,
        Cliff,
        SpiritAnchor,
        ObservationLoop,
        EscapeStart,
        ToppleObject,
        ElementalConduit
    }

    public TagType type;
    public float radius = 1f;

    private void OnEnable()
    {
        EnvironmentTagRegistry registry = FindFirstObjectByType<EnvironmentTagRegistry>();
        if (registry != null)
        {
            registry.RegisterTag(this);
        }
    }

    private void OnDisable()
    {
        EnvironmentTagRegistry registry = FindFirstObjectByType<EnvironmentTagRegistry>();
        if (registry != null)
        {
            registry.UnregisterTag(this);
        }
    }
}
