using UnityEngine;

[DefaultExecutionOrder(100)]
public class AvatarHandCoffeeGrab : MonoBehaviour
{
    [SerializeField] private float grabRadius = 0.28f;
    [SerializeField] private float controllerGripThreshold = 0.45f;

    private Rigidbody cupRigidbody;
    private Collider[] cupColliders;
    private CD_Control cdControl;
    private Transform leftHand;
    private Transform rightHand;
    private OVRHand leftOvrHand;
    private OVRHand rightOvrHand;

    private Transform heldHand;
    private Vector3 localPositionOffset;
    private Quaternion localRotationOffset;
    private Transform originalParent;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private bool originalUseGravity;
    private bool originalIsKinematic;
    private bool capturedRigidbodyState;

    public bool IsHeld => heldHand != null;

    void Awake()
    {
        cupRigidbody = GetComponent<Rigidbody>();
        cupColliders = GetComponentsInChildren<Collider>();
        originalParent = transform.parent;
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
        CaptureRigidbodyState();
        AutoAssignAvatarHands();
        AutoAssignTrackedHands();
    }

    public void ResetToOriginalPose()
    {
        heldHand = null;

        if (transform.parent != originalParent)
        {
            transform.SetParent(originalParent, false);
        }

        transform.SetLocalPositionAndRotation(originalLocalPosition, originalLocalRotation);

        if (cupRigidbody != null)
        {
            if (capturedRigidbodyState)
            {
                cupRigidbody.useGravity = originalUseGravity;
                cupRigidbody.isKinematic = originalIsKinematic;
            }

            cupRigidbody.linearVelocity = Vector3.zero;
            cupRigidbody.angularVelocity = Vector3.zero;
            cupRigidbody.Sleep();
        }
    }

    void LateUpdate()
    {
        if (leftHand == null || rightHand == null)
        {
            AutoAssignAvatarHands();
        }

        if (leftOvrHand == null || rightOvrHand == null)
        {
            AutoAssignTrackedHands();
        }

        if (heldHand != null)
        {
            bool holdingLeft = heldHand == leftHand && IsLeftGrabActive();
            bool holdingRight = heldHand == rightHand && IsRightGrabActive();
            if (!holdingLeft && !holdingRight)
            {
                Release();
                return;
            }

            MoveToHeldHand();
            return;
        }

        Transform targetHand = GetBestAvatarGrabHand();
        if (targetHand != null)
        {
            Grab(targetHand);
        }
    }

    private Transform GetBestAvatarGrabHand()
    {
        bool leftActive = IsLeftGrabActive();
        bool rightActive = IsRightGrabActive();

        float leftDistance = leftActive && leftHand != null
            ? GetDistanceToCup(leftHand.position)
            : float.PositiveInfinity;
        float rightDistance = rightActive && rightHand != null
            ? GetDistanceToCup(rightHand.position)
            : float.PositiveInfinity;

        if (leftDistance > grabRadius && rightDistance > grabRadius)
        {
            return null;
        }

        return leftDistance <= rightDistance ? leftHand : rightHand;
    }

    private float GetDistanceToCup(Vector3 point)
    {
        if (cupColliders == null || cupColliders.Length == 0)
        {
            cupColliders = GetComponentsInChildren<Collider>();
        }

        float bestDistance = Vector3.Distance(transform.position, point);
        foreach (Collider collider in cupColliders)
        {
            if (collider == null || !collider.enabled)
            {
                continue;
            }

            Vector3 closest = collider.ClosestPoint(point);
            bestDistance = Mathf.Min(bestDistance, Vector3.Distance(point, closest));
        }

        return bestDistance;
    }

    private void Grab(Transform hand)
    {
        heldHand = hand;
        localPositionOffset = Quaternion.Inverse(hand.rotation) * (transform.position - hand.position);
        localRotationOffset = Quaternion.Inverse(hand.rotation) * transform.rotation;

        if (cupRigidbody != null)
        {
            CaptureRigidbodyState();
            cupRigidbody.useGravity = false;
            cupRigidbody.isKinematic = true;
            cupRigidbody.linearVelocity = Vector3.zero;
            cupRigidbody.angularVelocity = Vector3.zero;
        }
    }

    private void Release()
    {
        if (cupRigidbody != null)
        {
            cupRigidbody.useGravity = originalUseGravity;
            cupRigidbody.isKinematic = originalIsKinematic;
        }

        heldHand = null;
    }

    private void MoveToHeldHand()
    {
        transform.SetPositionAndRotation(
            heldHand.position + heldHand.rotation * localPositionOffset,
            heldHand.rotation * localRotationOffset);
    }

    private void CaptureRigidbodyState()
    {
        if (cupRigidbody == null)
        {
            return;
        }

        originalUseGravity = cupRigidbody.useGravity;
        originalIsKinematic = cupRigidbody.isKinematic;
        capturedRigidbodyState = true;
    }

    private bool IsLeftGrabActive()
    {
        return OVRInput.Get(OVRInput.RawAxis1D.LHandTrigger) >= controllerGripThreshold ||
               IsHandPinching(leftOvrHand);
    }

    private bool IsRightGrabActive()
    {
        return OVRInput.Get(OVRInput.RawAxis1D.RHandTrigger) >= controllerGripThreshold ||
               IsHandPinching(rightOvrHand);
    }

    private static bool IsHandPinching(OVRHand hand)
    {
        return hand != null && hand.GetFingerIsPinching(OVRHand.HandFinger.Index);
    }

    private void AutoAssignAvatarHands()
    {
        cdControl = FindAnyObjectByType<CD_Control>();
        if (cdControl == null)
        {
            AutoAssignAnyHumanoidAnimatorHands();
            return;
        }

        Animator avatarAnimator = cdControl.GetComponent<Animator>();
        if (avatarAnimator == null || !avatarAnimator.isHuman)
        {
            avatarAnimator = cdControl.GetComponentInParent<Animator>();
        }

        if (avatarAnimator == null || !avatarAnimator.isHuman)
        {
            avatarAnimator = cdControl.GetComponentInChildren<Animator>();
        }

        if (avatarAnimator != null && avatarAnimator.isHuman)
        {
            leftHand = avatarAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
            rightHand = avatarAnimator.GetBoneTransform(HumanBodyBones.RightHand);
        }

        if (leftHand == null)
        {
            leftHand = FindHandBelow(cdControl.leftUpperArm, "left");
        }

        if (rightHand == null)
        {
            rightHand = FindHandBelow(cdControl.rightUpperArm, "right");
        }

        if (leftHand == null || rightHand == null)
        {
            AutoAssignAnyHumanoidAnimatorHands();
        }
    }

    private void AutoAssignAnyHumanoidAnimatorHands()
    {
        Animator[] animators = FindObjectsByType<Animator>(FindObjectsSortMode.None);
        foreach (Animator animator in animators)
        {
            if (animator == null || !animator.isHuman)
            {
                continue;
            }

            if (leftHand == null)
            {
                leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            }

            if (rightHand == null)
            {
                rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            }

            if (leftHand != null && rightHand != null)
            {
                return;
            }
        }
    }

    private static Transform FindHandBelow(Transform root, string side)
    {
        if (root == null)
        {
            return null;
        }

        Transform best = null;
        string sideLower = side.ToLowerInvariant();
        foreach (Transform child in root.GetComponentsInChildren<Transform>())
        {
            string name = child.name.ToLowerInvariant();
            if (name.Contains(sideLower) && name.Contains("hand"))
            {
                return child;
            }

            if (best == null && name.Contains("hand"))
            {
                best = child;
            }
        }

        return best;
    }

    private void AutoAssignTrackedHands()
    {
        OVRHand[] hands = FindObjectsByType<OVRHand>(FindObjectsSortMode.None);
        foreach (OVRHand hand in hands)
        {
            string handName = hand.name.ToLowerInvariant();
            string parentName = hand.transform.parent != null
                ? hand.transform.parent.name.ToLowerInvariant()
                : string.Empty;

            if (leftOvrHand == null && (handName.Contains("left") || parentName.Contains("left")))
            {
                leftOvrHand = hand;
            }
            else if (rightOvrHand == null && (handName.Contains("right") || parentName.Contains("right")))
            {
                rightOvrHand = hand;
            }
        }
    }
}
