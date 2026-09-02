using UnityEngine;
using Obi;
using System.Text;

/// <summary>
/// Attach this script to any target GameObject to diagnose its physics and Obi setup.
/// It will log warnings and errors in its inspector to help you figure out why Bash, Tether, or Tripwire might be failing.
///
/// SCENE SETUP INSTRUCTIONS:
/// 1. Attach this script directly to any GameObject in the scene that acts as a target for a Rope Arrow (e.g., Goblins, Crates, Statues).
/// 2. View the component in the Unity Inspector. The "Diagnostic Report" text area will automatically populate with warnings/errors.
/// 3. While the game is playing, shoot the object with a rope arrow to trigger the Runtime Obi Checks (verifying Dynamic vs Static attachments).
/// 4. You can manually refresh the report by right-clicking the component header and selecting "Run Diagnostics Now".
///
/// WHAT IT CHECKS:
/// - Presence of a Collider (needed for arrows to stick).
/// - Presence and value of ObjectWeight (determines Lightweight vs Heavyweight).
/// - Rigidbody configuration (Lightweight can be kinematic or non-kinematic; Rope system will override. Heavyweight/Anchors should be kinematic or very heavy).
/// - ObiParticleAttachment configuration (Runtime): Lightweight objects MUST be attached with AttachmentType.Dynamic to be pulled by the rope.
/// </summary>
[ExecuteAlways]
public class RopeTargetPhysicsDiagnostic : MonoBehaviour
{
    [Header("Diagnostic Status")]
    public bool isSetupCorrectly = false;

    [TextArea(10, 20)]
    public string diagnosticReport = "";

    void Update()
    {
        // Throttled execution: Update once every 60 frames (~1 sec at 60 FPS) in both Editor and Play Mode
        // to prevent heavy performance impact from FindObjectsOfType and string allocation.
        if (Time.frameCount % 60 != 0) return;

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
            report.AppendLine("❌ ERROR: No Rigidbody found. All rope targets require a Rigidbody to interact properly with the physics engine (e.g. Kinematic for anchors).");
            allGood = false;
        }
        else
        {
            if (mobility == RopeTargetMobility.Lightweight)
            {
                if (rb.isKinematic)
                {
                    report.AppendLine("✅ Rigidbody is KINEMATIC. This is valid for objects with custom movement (e.g. NavMesh). The Rope System will automatically disable Kinematic physics when connected.");
                }
                else
                {
                    report.AppendLine("✅ Rigidbody is non-kinematic. Ready to be pulled.");
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
                    report.AppendLine("✅ Rigidbody setup looks good for an anchor target (Kinematic or high mass).");
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
