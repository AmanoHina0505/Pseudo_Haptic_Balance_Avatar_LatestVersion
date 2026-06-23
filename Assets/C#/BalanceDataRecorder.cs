using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class BalanceDataRecorder : MonoBehaviour
{
    [Header("Tracked Transforms")]
    [SerializeField] private Transform centerEye;
    [SerializeField] private Animator avatarAnimator;
    [SerializeField] private Transform pelvis;
    [SerializeField] private Transform spine;
    [SerializeField] private Transform head;

    [Header("Condition")]
    [SerializeField] private CD_Control cdControl;
    [SerializeField] private OVRBody bodyTrackingSource;

    [Header("Recording")]
    [SerializeField] private float sampleRateHz = 2f;
    [SerializeField] private string filePrefix = "balance_recording";

    [Header("UDP Streaming")]
    [SerializeField] private bool streamUdp = true;
    [SerializeField] private string pcIpAddress = "255.255.255.255";
    [SerializeField] private int pcPort = 9000;

    private bool isRecording;
    private int recordingIndex;
    private float nextSampleTime;
    private StreamWriter writer;
    private string currentPath;
    private UdpClient udpClient;
    private IPEndPoint udpTarget;
    private string pendingEventName = "sample";
    private int conditionIndex;
    private float conditionLeftRatio = 1f;
    private float conditionRightRatio = 1f;

    public bool IsRecording => isRecording;
    public string CurrentPath => currentPath;

    void Awake()
    {
        AutoAssignReferences();
        ConfigureUdp();
    }

    void OnDestroy()
    {
        StopRecording();
        udpClient?.Close();
    }

    void Update()
    {
        if (!isRecording || Time.unscaledTime < nextSampleTime)
        {
            return;
        }

        nextSampleTime = Time.unscaledTime + 1f / Mathf.Max(1f, sampleRateHz);
        WriteSample(ConsumeEventName());
    }

    public void ToggleRecording()
    {
        if (isRecording)
        {
            StopRecording();
        }
        else
        {
            StartRecording();
        }
    }

    public void MarkRatioChanged(float leftRatio, float rightRatio)
    {
        SetCondition(conditionIndex + 1, leftRatio, rightRatio, "ratio_changed");
    }

    public void SetCondition(int index, float leftRatio, float rightRatio, string eventName = "condition_changed")
    {
        conditionIndex = index;
        conditionLeftRatio = leftRatio;
        conditionRightRatio = rightRatio;

        if (isRecording)
        {
            pendingEventName = eventName;
        }
    }

    public void StartRecording()
    {
        if (isRecording)
        {
            return;
        }

        AutoAssignReferences();
        ConfigureUdp();

        recordingIndex++;
        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        currentPath = Path.Combine(Application.persistentDataPath, $"{filePrefix}_{stamp}_trial{recordingIndex}.csv");
        writer = new StreamWriter(currentPath, false, Encoding.UTF8);
        if (conditionIndex == 0)
        {
            conditionIndex = 1;
            conditionLeftRatio = 1f;
            conditionRightRatio = 1f;
        }

        writer.WriteLine("pcTimestamp,task,leftCDRatio,rightCDRatio,bodyConfidence,centerEyeX,centerEyeY,centerEyeZ,trunkLeanDeg,event");
        isRecording = true;
        nextSampleTime = Time.unscaledTime;
        pendingEventName = "recording_started";
        Debug.Log($"Balance recording started: {currentPath}");
    }

    public void StopRecording()
    {
        if (!isRecording)
        {
            return;
        }

        isRecording = false;
        pendingEventName = "sample";
        writer?.Flush();
        writer?.Close();
        writer = null;
        Debug.Log($"Balance recording saved: {currentPath}");
    }

    private void WriteSample(string eventName)
    {
        Vector3 eye = centerEye != null ? centerEye.position : Vector3.zero;
        float confidence = GetBodyConfidence();
        float trunkLean = CalculateTrunkLeanDeg();

        string line = string.Format(
            CultureInfo.InvariantCulture,
            "{0},Task{1},{2:F2},{3:F2},{4:F3},{5:F5},{6:F5},{7:F5},{8:F3},{9}",
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
            conditionIndex,
            conditionLeftRatio,
            conditionRightRatio,
            confidence,
            eye.x,
            eye.y,
            eye.z,
            trunkLean,
            eventName);

        writer?.WriteLine(line);
        SendUdp(line);
    }

    private string ConsumeEventName()
    {
        string eventName = pendingEventName;
        pendingEventName = "sample";
        return eventName;
    }

    private float CalculateTrunkLeanDeg()
    {
        if (pelvis == null || head == null)
        {
            return 0f;
        }

        Vector3 trunkVector = head.position - pelvis.position;
        if (trunkVector.sqrMagnitude < 0.0001f)
        {
            return 0f;
        }

        return Vector3.Angle(trunkVector.normalized, Vector3.up);
    }

    private float GetBodyConfidence()
    {
        if (bodyTrackingSource != null &&
            bodyTrackingSource.BodyState.HasValue)
        {
            return bodyTrackingSource.BodyState.Value.Confidence;
        }

        return -1f;
    }

    private void SendUdp(string line)
    {
        if (!streamUdp || udpClient == null || udpTarget == null)
        {
            return;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(line);
        udpClient.Send(bytes, bytes.Length, udpTarget);
    }

    private void ConfigureUdp()
    {
        if (!streamUdp || string.IsNullOrWhiteSpace(pcIpAddress))
        {
            return;
        }

        try
        {
            udpTarget = new IPEndPoint(IPAddress.Parse(pcIpAddress), pcPort);
            udpClient ??= new UdpClient();
            udpClient.EnableBroadcast = pcIpAddress == "255.255.255.255";
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"UDP recorder setup failed: {ex.Message}");
            udpTarget = null;
        }
    }

    private void AutoAssignReferences()
    {
        cdControl ??= FindAnyObjectByType<CD_Control>();
        bodyTrackingSource ??= FindAnyObjectByType<OVRBody>();

        if (centerEye == null)
        {
            Camera mainCamera = Camera.main;
            centerEye = mainCamera != null ? mainCamera.transform : null;
        }

        if (avatarAnimator == null)
        {
            Animator[] animators = FindObjectsByType<Animator>(FindObjectsSortMode.None);
            foreach (Animator animator in animators)
            {
                if (!animator.isHuman)
                {
                    continue;
                }

                if (animator.GetBoneTransform(HumanBodyBones.Hips) != null &&
                    animator.GetBoneTransform(HumanBodyBones.Head) != null)
                {
                    avatarAnimator = animator;
                    break;
                }
            }
        }

        if (avatarAnimator != null && avatarAnimator.isHuman)
        {
            pelvis ??= avatarAnimator.GetBoneTransform(HumanBodyBones.Hips);
            spine ??= avatarAnimator.GetBoneTransform(HumanBodyBones.Spine);
            if (spine == null)
            {
                spine = avatarAnimator.GetBoneTransform(HumanBodyBones.Chest);
            }
            head ??= avatarAnimator.GetBoneTransform(HumanBodyBones.Head);
        }
    }
}

internal static class BalanceDataRecorderBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateRecorderIfNeeded()
    {
        if (UnityEngine.Object.FindAnyObjectByType<BalanceDataRecorder>() != null)
        {
            return;
        }

        GameObject recorderObject = new GameObject("Balance Data Recorder");
        recorderObject.AddComponent<BalanceDataRecorder>();
    }
}
