using UnityEngine;
using Obi;
using System.Text;

/// <summary>
/// Attach this script to any target GameObject to diagnose its physics and Obi setup.
/// It will log warnings and errors in its inspector to help you figure out why Bash, Tether, or Tripwire might be failing.
/// </summary>
[ExecuteAlways]
public class RopePhysicsDiagnostic : MonoBehaviour
{
    [Header("Diagnostic Status")]
    public bool isSetupCorrectly = false;

    [TextArea(10, 20)]
    public string diagnosticReport = "";

    void Update()
    {
        // Update occasionally in editor so we don't spam, or every frame if requested, but let's just do it cleanly
        if (!Application.isPlaying && Time.frameCount % 60 != 0) return;

        RunDiagnostics();
    }

    [ContextMenu("Run Diagnostics Now")]
    public void RunDiagnostics()
    {
        StringBuilder report = new StringBuilder();
        bool allGood = true;

        report.AppendLine($"--- Diagnostic Report for: {gameObject.name} ---");

        // 1. Check for Collider (required for arrows to hit)
        Collider col = GetComponentInChildren<Collider>();
        if (col == null)
        {
            report.AppendLine("❌ ERROR: No Collider found. Arrows will pass right through this object.");
            allGood = false;
        }
        else
        {
            report.AppendLine("✅ Collider found.");
            if (col.isTrigger)
            {
                report.AppendLine("⚠️ WARNING: Collider is a Trigger. StickingArrow might not register collisions correctly unless it handles triggers.");
            }
        }

        // 2. Check ObjectWeight
        ObjectWeight weight = GetComponent<ObjectWeight>();
        RopeTargetMobility mobility = RopeTargetMobility.Immovable;
        if (weight == null)
        {
            report.AppendLine("⚠️ WARNING: No ObjectWeight component found. The rope system will default this to Immovable.");
        }
        else
        {
            mobility = weight.mobility;
            report.AppendLine($"✅ ObjectWeight found. Mobility is set to: {mobility}");
        }

        // 3. Check Rigidbody based on Mobility
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            if (mobility == RopeTargetMobility.Lightweight)
            {
                report.AppendLine("❌ ERROR: Target is Lightweight but has NO Rigidbody. ObiRope cannot pull an object without a Rigidbody.");
                allGood = false;
            }
            else
            {
                report.AppendLine("✅ No Rigidbody found. This is fine for Immovable targets.");
            }
        }
        else
        {
            if (mobility == RopeTargetMobility.Lightweight)
            {
                if (rb.isKinematic)
                {
                    report.AppendLine("❌ ERROR: Target is Lightweight but Rigidbody is KINEMATIC. The rope cannot physically move it for Bash or Tether. It must be non-kinematic when hit.");
                    allGood = false;
                }
                else
                {
                    report.AppendLine("✅ Rigidbody found and is non-kinematic. Ready to be pulled.");
                }
            }
            else // Heavyweight or Immovable
            {
                if (!rb.isKinematic && rb.mass < 100f)
                {
                    report.AppendLine("⚠️ WARNING: Target is Heavyweight/Immovable but Rigidbody is non-kinematic with a low mass. The rope might accidentally pull this anchor. Consider making it kinematic or heavily increasing mass.");
                }
                else
                {
                    report.AppendLine("✅ Rigidbody setup looks good for an anchor target.");
                }
            }
        }

        // 4. Runtime Obi Checks
        if (Application.isPlaying)
        {
            // Check if there are any ObiParticleAttachments attached to us or if we are the target of one
            ObiParticleAttachment[] attachments = FindObjectsOfType<ObiParticleAttachment>();
            bool isAttached = false;
            foreach (var att in attachments)
            {
                if (att.target != null && (att.target == transform || att.target.IsChildOf(transform)))
                {
                    isAttached = true;
                    if (mobility == RopeTargetMobility.Lightweight && att.attachmentType == ObiParticleAttachment.AttachmentType.Static)
                    {
                        report.AppendLine($"\n❌ CRITICAL OBI ERROR: Object is attached to rope via a STATIC attachment.");
                        report.AppendLine($"ObiRope cannot apply physical pulling forces to Rigidbodies using Static attachments.");
                        report.AppendLine($"The RopeArrowManagerObi7 script must be modified to use AttachmentType.Dynamic for Lightweight objects!");
                        allGood = false;
                    }
                    else if (mobility == RopeTargetMobility.Lightweight && att.attachmentType == ObiParticleAttachment.AttachmentType.Dynamic)
                    {
                        report.AppendLine($"✅ Obi Particle Attachment is Dynamic. Forces will correctly apply to this lightweight object.");
                    }
                }
            }

            if (isAttached)
            {
                report.AppendLine("ℹ️ Object is currently attached to an ObiRope.");
            }
        }

        isSetupCorrectly = allGood;
        diagnosticReport = report.ToString();
    }
}
