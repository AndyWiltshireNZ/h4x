using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class CPUCanvasController : MonoBehaviour
{
	private CPUManager cpuManagerParent;

	[SerializeField] private CanvasGroup cpuCanvasGroup;
	[SerializeField] private TextMeshProUGUI cpuLevelValueText;
	[SerializeField] private TextMeshProUGUI xpValueText;
	[SerializeField] private Image xpMeterFillImage;

	private int currentCPULevel = 1;
	private float currentXP = 0;
	private float nextXP = 0;

	public void Setup ( CPUManager cpuManager )
	{
		cpuManagerParent = cpuManager;
		cpuLevelValueText.text = "1";
		UpdateCPUXPText( 0 );
		xpMeterFillImage.fillAmount = 0;
	}

	public void UpdateCPULevelText( int currentCPULevelValue )
	{
		currentCPULevel = currentCPULevelValue;
		cpuLevelValueText.text = currentCPULevelValue.ToString();
	}

	public void UpdateCPUXPText( float currentXPValue )
	{
		currentXP = currentXPValue;
		nextXP = cpuManagerParent.GetXPThresholdForLevel( currentCPULevel );

		string currentXPText = ShortNumberFormatter.FormatShortNumber( currentXP );
		string nextXPText = ShortNumberFormatter.FormatShortNumber( nextXP );

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
