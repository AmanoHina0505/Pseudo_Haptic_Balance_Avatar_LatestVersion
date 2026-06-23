using UnityEngine;

[DefaultExecutionOrder(10000)]
public class AvatarFootStanceNarrower : MonoBehaviour
{
    [SerializeField, Range(0.5f, 1f)] private float stanceWidthScale = 0.85f;
    [SerializeField] private float maxInwardMetersPerFoot = 0.06f;

    private Animator avatarAnimator;
    private Transform hips;
    private Transform leftFoot;
    private Transform rightFoot;

    void Awake()
    {
        AutoAssignReferences();
    }

    void LateUpdate()
    {
        if (!HasValidFeet())
        {
            AutoAssignReferences();
        }

        if (!HasValidFeet())
        {
            return;
        }

        Vector3 rightAxis = hips.right;
        rightAxis.y = 0f;
        if (rightAxis.sqrMagnitude < 0.0001f)
        {
            rightAxis = avatarAnimator.transform.right;
            rightAxis.y = 0f;
        }

        if (rightAxis.sqrMagnitude < 0.0001f)
        {
            return;
        }

        rightAxis.Normalize();

        Vector3 midpoint = (leftFoot.position + rightFoot.position) * 0.5f;
        NarrowFoot(leftFoot, midpoint, rightAxis);
        NarrowFoot(rightFoot, midpoint, rightAxis);
    }

    private void NarrowFoot(Transform foot, Vector3 midpoint, Vector3 rightAxis)
    {
        Vector3 offset = foot.position - midpoint;
        float sideOffset = Vector3.Dot(offset, rightAxis);
        float targetSideOffset = sideOffset * stanceWidthScale;
        float inward = Mathf.Clamp(sideOffset - targetSideOffset, -maxInwardMetersPerFoot, maxInwardMetersPerFoot);

        foot.position -= rightAxis * inward;
    }

    private bool HasValidFeet()
    {
        return avatarAnimator != null &&
               hips != null &&
               leftFoot != null &&
               rightFoot != null;
    }

    private void AutoAssignReferences()
    {
        if (avatarAnimator == null)
        {
            Animator[] animators = FindObjectsByType<Animator>(FindObjectsSortMode.None);
            foreach (Animator animator in animators)
            {
                if (!animator.isHuman)
                {
                    continue;
                }

                Transform animatorHips = animator.GetBoneTransform(HumanBodyBones.Hips);
                Transform animatorLeftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                Transform animatorRightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
                if (animatorHips != null && animatorLeftFoot != null && animatorRightFoot != null)
                {
                    avatarAnimator = animator;
                    hips = animatorHips;
                    leftFoot = animatorLeftFoot;
                    rightFoot = animatorRightFoot;
                    return;
                }
            }
        }

        if (avatarAnimator != null && avatarAnimator.isHuman)
        {
            hips ??= avatarAnimator.GetBoneTransform(HumanBodyBones.Hips);
            leftFoot ??= avatarAnimator.GetBoneTransform(HumanBodyBones.LeftFoot);
            rightFoot ??= avatarAnimator.GetBoneTransform(HumanBodyBones.RightFoot);
        }
    }
}

internal static class AvatarFootStanceNarrowerBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateNarrowerIfNeeded()
    {
        if (Object.FindAnyObjectByType<AvatarFootStanceNarrower>() != null)
        {
            return;
        }

        GameObject narrowerObject = new GameObject("Avatar Foot Stance Narrower");
        narrowerObject.AddComponent<AvatarFootStanceNarrower>();
    }
}
