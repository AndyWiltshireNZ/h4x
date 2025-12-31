using UnityEngine;

[CreateAssetMenu( fileName = "XPThresholdsDefinition", menuName = "Definitions/XPThresholdsDefinition" )]
public class XPThresholdsDefinition : ScriptableObject
{
	public int[] Thresholds;
	public int GetXPThresholdForLevel( int level )
	{
		if ( level < 1 )
		{
			return 0;
		}
		else if ( level > Thresholds.Length )
		{
			return Thresholds[ Thresholds.Length - 1 ];
		}
		else
		{
			return Thresholds[ level - 1 ];
		}
	}
}