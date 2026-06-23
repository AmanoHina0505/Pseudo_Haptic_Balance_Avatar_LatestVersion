using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ExperimentControlPanel : MonoBehaviour
{
    private static readonly Vector2[] Conditions =
    {
        new Vector2(1.0f, 1.0f),
        new Vector2(1.0f, 0.5f),
        new Vector2(1.0f, 1.5f),
        new Vector2(1.0f, 2.0f),
        new Vector2(0.5f, 1.0f),
        new Vector2(0.5f, 0.5f),
        new Vector2(0.5f, 1.5f),
        new Vector2(0.5f, 2.0f),
        new Vector2(1.5f, 0.5f),
        new Vector2(1.5f, 1.0f),
        new Vector2(1.5f, 1.5f),
        new Vector2(1.5f, 2.0f),
        new Vector2(2.0f, 0.5f),
        new Vector2(2.0f, 1.0f),
        new Vector2(2.0f, 1.5f),
        new Vector2(2.0f, 2.0f),
    };

    [SerializeField] private CD_Control cdControl;
    [SerializeField] private BalanceDataRecorder recorder;
    [SerializeField] private AvatarHandCoffeeGrab coffeeCup;
    [SerializeField] private Button advanceButton;
    [SerializeField] private TMP_Text buttonLabel;
    [SerializeField] private TMP_Text statusLabel;

    private int activeConditionIndex = -1;
    private bool experimentStarted;
    private bool experimentComplete;

    void Awake()
    {
        cdControl ??= FindAnyObjectByType<CD_Control>();
        recorder ??= FindAnyObjectByType<BalanceDataRecorder>();
        coffeeCup ??= FindAnyObjectByType<AvatarHandCoffeeGrab>();
        RefreshLabels();
    }

    public void Initialize(Button button, TMP_Text buttonText, TMP_Text statusText)
    {
        advanceButton = button;
        buttonLabel = buttonText;
        statusLabel = statusText;
        RefreshLabels();
    }

    void Start()
    {
        if (advanceButton != null)
        {
            advanceButton.onClick.AddListener(AdvanceExperiment);
        }
    }

    void OnDestroy()
    {
        if (advanceButton != null)
        {
            advanceButton.onClick.RemoveListener(AdvanceExperiment);
        }
    }

    public void AdvanceExperiment()
    {
        if (experimentComplete)
        {
            return;
        }

        cdControl ??= FindAnyObjectByType<CD_Control>();
        recorder ??= FindAnyObjectByType<BalanceDataRecorder>();
        coffeeCup ??= FindAnyObjectByType<AvatarHandCoffeeGrab>();
        coffeeCup?.ResetToOriginalPose();

        if (!experimentStarted)
        {
            experimentStarted = true;
            ApplyCondition(0, "baseline_started");
            if (recorder != null && !recorder.IsRecording)
            {
                recorder.StartRecording();
            }

            RefreshLabels();
            return;
        }

        if (activeConditionIndex >= Conditions.Length - 1)
        {
            recorder?.StopRecording();
            experimentComplete = true;
            RefreshLabels();
            return;
        }

        ApplyCondition(activeConditionIndex + 1, "condition_started");
        RefreshLabels();
    }

    private void ApplyCondition(int index, string eventName)
    {
        Vector2 condition = Conditions[index];
        activeConditionIndex = index;

        if (cdControl != null)
        {
            cdControl.RequestCalibration();
            cdControl.leftArmCDRatio = condition.y;
            cdControl.rightArmCDRatio = condition.x;
        }

        recorder?.SetCondition(index + 1, condition.x, condition.y, eventName);
        Debug.Log($"Experiment condition {index + 1}/{Conditions.Length}: Left C/D={condition.x:0.0}, Right C/D={condition.y:0.0}");
    }

    private void RefreshLabels()
    {
        if (buttonLabel != null)
        {
            buttonLabel.text = experimentComplete
                ? "Experiment Complete"
                : experimentStarted ? "Next" : "Recalibrate Neutral Pose";
        }

        if (advanceButton != null)
        {
            advanceButton.interactable = !experimentComplete;
        }

        if (statusLabel == null)
        {
            return;
        }

        if (experimentComplete)
        {
            statusLabel.text = "Experiment finished";
            return;
        }

        if (!experimentStarted)
        {
            statusLabel.text = "Ready";
            return;
        }

        statusLabel.text = $"Task{activeConditionIndex + 1}";
    }
}

internal static class ExperimentControlPanelBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreatePanelIfNeeded()
    {
        if (Object.FindAnyObjectByType<ExperimentControlPanel>() != null)
        {
            return;
        }

        EnsureEventSystem();

        GameObject root = new GameObject("Experiment Control Panel");
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        GraphicRaycaster raycaster = root.AddComponent<GraphicRaycaster>();
        raycaster.ignoreReversedGraphics = false;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        RectTransform canvasRect = root.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(520f, 220f);
        PlacePanelToAvatarRight(root.transform);
        root.transform.localScale = Vector3.one * 0.002f;
        root.layer = LayerMask.NameToLayer("UI");

        Image panelImage = root.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.09f, 0.1f, 0.92f);

        TMP_Text status = CreateText(root.transform, "Status", new Vector2(0f, 54f), new Vector2(460f, 48f), 26);
        status.text = "Ready";

        Button button = CreateButton(root.transform, out TMP_Text buttonText);
        CreateRayCanvasInteraction(root.transform, canvas, canvasRect);
        SetLayerRecursively(root, root.layer);

        ExperimentControlPanel panel = root.AddComponent<ExperimentControlPanel>();
        panel.Initialize(button, buttonText, status);
    }

    private static void PlacePanelToAvatarRight(Transform panelTransform)
    {
        CD_Control cdControl = Object.FindAnyObjectByType<CD_Control>();
        Transform avatarTransform = cdControl != null ? cdControl.transform : null;
        Camera mainCamera = Camera.main;

        if (avatarTransform == null && mainCamera == null)
        {
            panelTransform.position = new Vector3(0.85f, 1.45f, 2.35f);
            panelTransform.rotation = Quaternion.identity;
            return;
        }

        Vector3 basePosition = avatarTransform != null ? avatarTransform.position : mainCamera.transform.position;
        Vector3 right = avatarTransform != null ? avatarTransform.right : mainCamera.transform.right;
        Vector3 forward = avatarTransform != null ? avatarTransform.forward : mainCamera.transform.forward;

        right.y = 0f;
        forward.y = 0f;

        if (right.sqrMagnitude < 0.001f)
        {
            right = Vector3.right;
        }

        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.forward;
        }

        right.Normalize();
        forward.Normalize();

        panelTransform.position = basePosition + right * 0.85f + forward * 0.25f + Vector3.up * 1.35f;

        Vector3 lookDirection = mainCamera != null
            ? mainCamera.transform.position - panelTransform.position
            : -forward;
        lookDirection.y = 0f;
        if (lookDirection.sqrMagnitude < 0.001f)
        {
            lookDirection = -forward;
        }

        panelTransform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up) * Quaternion.Euler(0f, 180f, 0f);
    }

    private static void CreateRayCanvasInteraction(Transform parent, Canvas canvas, RectTransform canvasRect)
    {
        GameObject interactionObject = new GameObject("ISDK_RayCanvasInteraction");
        interactionObject.transform.SetParent(parent, false);

        RectTransform rect = interactionObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;

        PointableCanvas pointableCanvas = interactionObject.AddComponent<PointableCanvas>();
        pointableCanvas.InjectAllPointableCanvas(canvas);

        BoundsClipper clipper = interactionObject.AddComponent<BoundsClipper>();
        clipper.Size = new Vector3(canvasRect.rect.width, canvasRect.rect.height, 0.01f);

        PlaneSurface planeSurface = interactionObject.AddComponent<PlaneSurface>();
        planeSurface.InjectAllPlaneSurface(PlaneSurface.NormalFacing.Backward, true);

        ClippedPlaneSurface clippedSurface = interactionObject.AddComponent<ClippedPlaneSurface>();
        clippedSurface.InjectAllClippedPlaneSurface(planeSurface, new IBoundsClipper[] { clipper });

        RayInteractable rayInteractable = interactionObject.AddComponent<RayInteractable>();
        rayInteractable.InjectAllRayInteractable(clippedSurface);
        rayInteractable.InjectOptionalSelectSurface(planeSurface);
        rayInteractable.InjectOptionalPointableElement(pointableCanvas);
    }

    private static Button CreateButton(Transform parent, out TMP_Text buttonText)
    {
        GameObject buttonObject = new GameObject("Advance Button");
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(440f, 82f);
        rect.anchoredPosition = new Vector2(0f, -34f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.18f, 0.46f, 0.75f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.24f, 0.56f, 0.9f, 1f);
        colors.pressedColor = new Color(0.12f, 0.34f, 0.62f, 1f);
        colors.disabledColor = new Color(0.25f, 0.25f, 0.25f, 0.8f);
        button.colors = colors;

        buttonText = CreateText(buttonObject.transform, "Label", Vector2.zero, new Vector2(410f, 70f), 28);
        buttonText.text = "Recalibrate Neutral Pose";
        return button;
    }

    private static TMP_Text CreateText(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, int fontSize)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.fontSize = fontSize;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private static void SetLayerRecursively(GameObject gameObject, int layer)
    {
        gameObject.layer = layer;
        foreach (Transform child in gameObject.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

}
