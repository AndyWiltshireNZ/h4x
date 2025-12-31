using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CPUCanvasController : MonoBehaviour
{
	private CPUManager cpuManagerParent;
	private XPThresholdsDefinition xpThresholdsData;

	[SerializeField] private CanvasGroup cpuCanvasGroup;
	[SerializeField] private TextMeshProUGUI cpuLevelValueText;
	[SerializeField] private TextMeshProUGUI xpValueText;
	[SerializeField] private Image xpMeterFillImage;

	private int currentCPULevel = 1;
	private int currentXP = 0;
	private int nextXP = 0;

	public void Setup ( CPUManager cpuManager, XPThresholdsDefinition xpThresholdsDefinition )
	{
		cpuManagerParent = cpuManager;
		xpThresholdsData = xpThresholdsDefinition;
		cpuLevelValueText.text = "1";
		xpValueText.text = $"0 / {cpuManagerParent.GetXPThresholdForLevel( 0 )}";
		xpMeterFillImage.fillAmount = 0;
	}

	public void UpdateCPULevelText( int currentCPULevelValue )
	{
		currentCPULevel = currentCPULevelValue;
		cpuLevelValueText.text = currentCPULevelValue.ToString();
	}

	public void UpdateCPUXPText( int currentXPValue )
	{
		currentXP = currentXPValue;
		nextXP = cpuManagerParent.GetXPThresholdForLevel( currentCPULevel );

		string currentXPText = currentXP.ToString();
		string nextXPText = nextXP.ToString();

		xpValueText.text = currentXPText + " / " + nextXPText;

		UpdateXPMeter();
	}

	private void UpdateXPMeter()
	{
		float fillAmount = 0f;
		if (nextXP > 0)
		{
			fillAmount = (float)currentXP / (float)nextXP;
			fillAmount = Mathf.Clamp01(fillAmount);
		}
		xpMeterFillImage.fillAmount = fillAmount;
	}
}
