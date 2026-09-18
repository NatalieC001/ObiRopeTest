using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EnvironmentTagRegistry : MonoBehaviour
{
    private Dictionary<EnvironmentTag.TagType, List<EnvironmentTag>> activeTags = new Dictionary<EnvironmentTag.TagType, List<EnvironmentTag>>();

    public void RegisterTag(EnvironmentTag tag)
    {
        if (!activeTags.ContainsKey(tag.type))
        {
            activeTags[tag.type] = new List<EnvironmentTag>();
        }
        if (!activeTags[tag.type].Contains(tag))
        {
            activeTags[tag.type].Add(tag);
        }
    }

    public void UnregisterTag(EnvironmentTag tag)
    {
        if (activeTags.ContainsKey(tag.type))
        {
            activeTags[tag.type].Remove(tag);
        }
    }

    public List<EnvironmentTag> GetTagsOfType(EnvironmentTag.TagType type)
    {
        if (activeTags.ContainsKey(type))
        {
            return new List<EnvironmentTag>(activeTags[type]);
        }
        return new List<EnvironmentTag>();
    }

    public EnvironmentTag GetNearestTag(Vector3 position, EnvironmentTag.TagType type)
    {
        var tags = GetTagsOfType(type);
        if (tags.Count == 0) return null;

        EnvironmentTag nearest = null;
        float minDistance = float.MaxValue;

        foreach (var tag in tags)
        {
            if (tag == null) continue;
            float dist = Vector3.Distance(position, tag.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = tag;
            }
        }
        return nearest;
    }

    public List<EnvironmentTag> GetTagsWithinRadius(Vector3 position, float radius, EnvironmentTag.TagType? type = null)
    {
        List<EnvironmentTag> result = new List<EnvironmentTag>();

        IEnumerable<EnvironmentTag> source = type.HasValue ? GetTagsOfType(type.Value) : activeTags.Values.SelectMany(x => x);

        foreach (var tag in source)
        {
            if (tag != null && Vector3.Distance(position, tag.transform.position) <= radius)
            {
                result.Add(tag);
            }
        }
        return result;
    }
}
