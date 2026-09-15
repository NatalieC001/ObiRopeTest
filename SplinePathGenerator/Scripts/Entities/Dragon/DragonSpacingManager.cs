using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the dynamic spacing for the segmented dragon based on actual bounding box sizes.
/// Calculates the exact required distance behind the head for any given segment.
/// </summary>
public class DragonSpacingManager : MonoBehaviour
{
    [Tooltip("Extra space added between each segment. Can be negative to overlap them.")]
    public float padding = 0f;

    [System.Serializable]
    public class SegmentData
    {
        public DragonSegment segment;
        public float length;
    }

    // List of currently active segments ordered from head to tail
    [SerializeField] // Display in inspector for debugging
    private List<SegmentData> activeSegmentsData = new List<SegmentData>();

    /// <summary>
    /// Registers a newly spawned segment, calculating and storing its size.
    /// </summary>
    public void RegisterSegment(DragonSegment segment)
    {
        float segmentSize = CalculateSegmentLength(segment);

        activeSegmentsData.Add(new SegmentData
        {
            segment = segment,
            length = segmentSize
        });

        Debug.Log($"<color=cyan>[DragonSpacingManager] Registered {segment.gameObject.name} with length {segmentSize}</color>");
    }

    /// <summary>
    /// Removes a destroyed segment from the spacing list so gaps can be closed.
    /// </summary>
    public void RemoveSegment(DragonSegment segment)
    {
        activeSegmentsData.RemoveAll(x => x.segment == segment);
    }

    /// <summary>
    /// Returns the target physical distance this segment needs to be behind the head.
    /// This is the cumulative sum of the lengths of all pieces in front of it, plus padding.
    /// </summary>
    public float GetTargetDistanceForSegment(DragonSegment targetSegment)
    {
        float cumulativeDistance = 0f;

        for (int i = 0; i < activeSegmentsData.Count; i++)
        {
            var data = activeSegmentsData[i];

            // If we've reached the target segment, we're done summing
            if (data.segment == targetSegment)
            {
                // Note: If we want the target segment to sit directly behind the previous one,
                // the distance is the center-to-center offset.
                // Since length is total bounding box length, we add half the previous piece's length
                // and half of this piece's length, or simply sum the full lengths up to this point
                // if the origins are at the edge. Usually origins are in the center.

                // Assuming origins are exactly in the center of the mesh:
                if (i > 0)
                {
                    cumulativeDistance += (data.length / 2f);
                }
                break;
            }

            // For the first element (Head), we add half its length
            if (i == 0)
            {
                cumulativeDistance += (data.length / 2f);
            }
            else
            {
                // For middle elements, we add their full length
                cumulativeDistance += data.length;
            }

            // Add padding between pieces
            cumulativeDistance += padding;
        }

        return cumulativeDistance;
    }

    /// <summary>
    /// Returns the total physical length of the entire dragon (useful for breadcrumb history limits).
    /// </summary>
    public float GetTotalDragonLength()
    {
        float total = 0f;
        foreach (var data in activeSegmentsData)
        {
            total += data.length + padding;
        }
        return total;
    }

    public void ClearSegments()
    {
        activeSegmentsData.Clear();
    }

    private float CalculateSegmentLength(DragonSegment segment)
    {
        Renderer r = segment.GetComponentInChildren<Renderer>();
        if (r != null)
        {
            // Use local bounding box multiplied by local scale to avoid rotation issues
            MeshFilter mf = segment.GetComponentInChildren<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                return mf.sharedMesh.bounds.size.z * mf.transform.lossyScale.z;
            }
            else
            {
                // Fallback to world bounds if no mesh filter (e.g. SkinnedMeshRenderer)
                // Note: This might be slightly inaccurate if the object is rotated weirdly at spawn
                return r.bounds.size.z;
            }
        }

        Debug.LogWarning($"[DragonSpacingManager] No renderer found on {segment.gameObject.name}, defaulting to 2.0 length");
        return 2.0f; // Default fallback
    }
}
