using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CDRatioBoard : MonoBehaviour
{
    [SerializeField] private CD_Control cdControl;
    [SerializeField] private TMP_Text leftValueText;
    [SerializeField] private TMP_Text rightValueText;
    [SerializeField] private Color selectedColor = new Color(0.2f, 0.8f, 1f);
    [SerializeField] private Color normalColor = Color.white;

    private Button[] leftButtons;
    private Button[] rightButtons;

    void Start()
    {
        leftButtons = transform.Find("LeftColumn").GetComponentsInChildren<Button>();
        rightButtons = transform.Find("RightColumn").GetComponentsInChildren<Button>();
        Refresh();
    }

    public void SetLeftRatio(float ratio)
    {
        cdControl.leftArmCDRatio = ratio;
        Refresh();
    }

    public void SetRightRatio(float ratio)
    {
        cdControl.rightArmCDRatio = ratio;
        Refresh();
    }

    public void Recalibrate()
    {
        cdControl.RequestCalibration();
    }

    void Refresh()
    {
        leftValueText.text = $"Left: {cdControl.leftArmCDRatio:0.0}";
        rightValueText.text = $"Right: {cdControl.rightArmCDRatio:0.0}";

        UpdateColors(leftButtons, cdControl.leftArmCDRatio);
        UpdateColors(rightButtons, cdControl.rightArmCDRatio);
    }

    void UpdateColors(Button[] buttons, float selectedRatio)
    {
        foreach (Button button in buttons)
        {
            float ratio = float.Parse(button.GetComponentInChildren<TMP_Text>().text);
            ColorBlock colors = button.colors;
            colors.normalColor = Mathf.Approximately(ratio, selectedRatio)
                ? selectedColor
                : normalColor;
            button.colors = colors;
        }
    }
}
