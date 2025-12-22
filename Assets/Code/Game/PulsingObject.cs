using UnityEngine;

public class PulsingObject : MonoBehaviour
{
	[SerializeField] private int _band;

	[SerializeField] private bool scaleObject = false;
	[SerializeField] private float _startScale, _scaleMultiplier;

	[SerializeField] private bool scaleGlow = false;
	[SerializeField] private float _startGlow, _glowMultiplier;
	[SerializeField] [ColorUsage(true, true)] private Color _glowColor = Color.white;

	[SerializeField] private bool scaleLightIntensity = false;
	[SerializeField] private float _startLightIntensity, _lightIntensityMultiplier;

	void Update()
	{
		if ( scaleObject == true )
		{
			transform.localScale = new Vector3(
				( PulsingAudio._freqBand[ _band ] * _scaleMultiplier ) + _startScale,
				( PulsingAudio._freqBand[ _band ] * _scaleMultiplier ) + _startScale,
				( PulsingAudio._freqBand[ _band ] * _scaleMultiplier ) + _startScale
				);
		}

		if ( scaleGlow == true )
		{
			Material mat = GetComponent<Renderer>().material;
			mat.SetColor( "_EmissionColor", _glowColor * ( ( PulsingAudio._freqBand[ _band ] * _glowMultiplier ) + _startGlow ) );
		}

		if ( scaleLightIntensity == true )
		{
			Light light = GetComponent<Light>();
			light.intensity = ( PulsingAudio._freqBand[ _band ] * _lightIntensityMultiplier ) + _startLightIntensity;
		}
	}
}