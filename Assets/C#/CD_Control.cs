using UnityEngine;

public class CD_Control : MonoBehaviour
{
    [Header("Avatar Joints")]
    public Transform rightUpperArm;
    public Transform leftUpperArm;

    [Header("Body Tracking")]
    [Tooltip("OVRBody source used to verify that this frame has a fresh tracked pose.")]
    public OVRBody bodyTrackingSource;

    [Header("C/D Ratio Settings (Per Arm)")]
    [Range(0.1f, 2.0f)] public float rightArmCDRatio = 1.0f;
    [Range(0.1f, 2.0f)] public float leftArmCDRatio = 1.0f;

    [Header("Calibration")]
    [Tooltip("Editor fallback: cache the current tracked pose as the neutral pose.")]
    public KeyCode calibrateKey = KeyCode.Space;
    [Tooltip("Quest controller fallback: cache the current tracked pose as the neutral pose.")]
    public OVRInput.RawButton calibrateButton = OVRInput.RawButton.X;

    private Quaternion rUpperBase, lUpperBase;
    private bool isCalibrated = false;
    private bool calibrationRequested = false;

    void Awake()
    {
        if (bodyTrackingSource == null)
        {
            bodyTrackingSource = FindAnyObjectByType<OVRBody>();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(calibrateKey) || OVRInput.GetDown(calibrateButton))
        {
            calibrationRequested = true;
        }
    }

    public void RequestCalibration()
    {
        calibrationRequested = true;
    }

    bool TryCacheNeutralPose()
    {
        if (!HasValidTrackedPose())
        {
            return false;
        }

        rUpperBase = rightUpperArm.localRotation;
        lUpperBase = leftUpperArm.localRotation;
        isCalibrated = true;
        calibrationRequested = false;
        Debug.Log("C/D ratio calibrated from the current tracked pose.");
        return true;
    }

    void LateUpdate()
    {
        if (!HasValidTrackedPose())
        {
            return;
        }

        if (!isCalibrated || calibrationRequested)
        {
            TryCacheNeutralPose();
            return;
        }

        ApplyCD(rightUpperArm, rUpperBase, rightArmCDRatio);
        ApplyCD(leftUpperArm, lUpperBase, leftArmCDRatio);
    }

    bool HasValidTrackedPose()
    {
        return bodyTrackingSource != null &&
               bodyTrackingSource.BodyState.HasValue &&
               bodyTrackingSource.BodyState.Value.Confidence > 0f &&
               rightUpperArm != null &&
               leftUpperArm != null;
    }

    void ApplyCD(Transform joint, Quaternion baseLocal, float cdRatio)
    {
        Quaternion currentLocal = joint.localRotation;
        Quaternion delta = Quaternion.Inverse(baseLocal) * currentLocal;

        Quaternion scaledDelta = Quaternion.SlerpUnclamped(
            Quaternion.identity,
            delta,
            cdRatio
        );

        joint.localRotation = baseLocal * scaledDelta;
    }
}


