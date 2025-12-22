using MoreMountains.Tools;
using System.Collections;
using UnityEngine;

public class PulsingAudio : MonoBehaviour
{
	[SerializeField] private float acquireInterval = 0.1f;
	[SerializeField] private float acquireTimeout = 10f;

	// smoothing speed (higher = faster follow)
	[Tooltip("Higher values make the bands follow faster. Use ~5-20 for responsive smoothing.")]
	[SerializeField] private float smoothSpeed = 8f;

	private AudioSource audioSourceMusic;
	public static float[] _samples = new float[512];
	public static float[] _freqBand = new float[8];

	private bool isAcquiring = false;

	// track the currently monitored transient source so we can cancel monitoring when a new one appears
	private AudioSource monitoredTransientSource;
	private Coroutine monitorCoroutine;

	private void OnEnable()
	{
		// observe when MMSoundManager plays sounds so we can detect new music sources
		MMSoundManagerSoundPlayEvent.Register(OnAnySoundPlay);
	}

	private void OnDisable()
	{
		MMSoundManagerSoundPlayEvent.Unregister(OnAnySoundPlay);
		StopMonitoringTransient();
	}

	private void Start()
	{
		if (audioSourceMusic == null && !isAcquiring)
		{
			StartCoroutine(AcquireMusicSourceCoroutine());
		}
	}

	private IEnumerator AcquireMusicSourceCoroutine()
	{
		isAcquiring = true;

		float elapsed = 0f;
		while (audioSourceMusic == null && elapsed < acquireTimeout)
		{
			if (MMSoundManager.HasInstance)
			{
				var sounds = MMSoundManager.Instance.GetSoundsPlaying(MMSoundManager.MMSoundManagerTracks.Music);
				if (sounds != null && sounds.Count > 0)
				{
					// pick the first music AudioSource (or adjust policy to pick the newest)
					audioSourceMusic = sounds[0].Source;
					break;
				}
			}

			yield return new WaitForSeconds(acquireInterval);
			elapsed += acquireInterval;
		}
				
		isAcquiring = false;
		yield break;
	}

	private void Update()
	{
		// if we lost the source at runtime try to re-acquire once
		if (audioSourceMusic == null && !isAcquiring)
		{
			StartCoroutine(AcquireMusicSourceCoroutine());
		}

		if (audioSourceMusic == null) return;
		GetSpectrumData();
		MakeFrequencyBands();
	}

	void GetSpectrumData()
	{
		audioSourceMusic.GetSpectrumData(_samples, 0, FFTWindow.Blackman);
	}

	void MakeFrequencyBands()
	{
		int count = 0;

		for (int i = 0; i < 8; i++)
		{
			float average = 0f;
			int sampleCount = (int)Mathf.Pow(2, i) * 2;

			if (i == 7)
			{
				sampleCount += 2;
			}

			// guard: if sampleCount <= 0, skip
			if (sampleCount <= 0)
			{
				_freqBand[i] = 0f;
				continue;
			}

			for (int j = 0; j < sampleCount; j++)
			{
				average += _samples[count] * (count + 1);
				count++;
			}

			average /= sampleCount;

			// raw value for this band
			float rawBandValue = average * 10f;

			// smoothing: compute lerp alpha frame-rate independently
			// alpha = 1 - exp(-speed * dt)
			float alpha = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);

			_freqBand[i] = Mathf.Lerp(_freqBand[i], rawBandValue, alpha);
		}
	}

	// Handler for MMSoundManagerSoundPlayEvent.
	// Signature must match the delegate: AudioSource Delegate(AudioClip clip, MMSoundManagerPlayOptions options)
	// We return null because we are only observing.
	private AudioSource OnAnySoundPlay(AudioClip clip, MMSoundManagerPlayOptions options)
	{
		// Only care about music track plays
		if (options.MmSoundManagerTrack == MMSoundManager.MMSoundManagerTracks.Music)
		{
			// Defer detection one frame to let MMSoundManager register the new AudioSource in its internal list
			StartCoroutine(HandleNewMusicSourceNextFrame());
		}

		return null;
	}

	private IEnumerator HandleNewMusicSourceNextFrame()
	{
		// wait a frame to let MMSoundManager finish adding the source
		yield return null;

		if (!MMSoundManager.HasInstance) yield break;

		var sounds = MMSoundManager.Instance.GetSoundsPlaying(MMSoundManager.MMSoundManagerTracks.Music);
		if (sounds == null || sounds.Count == 0) yield break;

		// pick the newest (last) music source
		AudioSource newSource = sounds[sounds.Count - 1].Source;
		if (newSource == null) yield break;

		// if the new source is already our current source, nothing to do
		if (newSource == audioSourceMusic) yield break;

		// store previous and switch to transient/new source
		AudioSource previous = audioSourceMusic;
		audioSourceMusic = newSource;

		// start monitoring the transient source to restore previous when it stops
		StartMonitoringTransient(newSource, previous);
	}

	private void StartMonitoringTransient(AudioSource transient, AudioSource previous)
	{
		StopMonitoringTransient();
		monitoredTransientSource = transient;
		monitorCoroutine = StartCoroutine(MonitorTransientSourceCoroutine(transient, previous));
	}

	private void StopMonitoringTransient()
	{
		if (monitorCoroutine != null)
		{
			StopCoroutine(monitorCoroutine);
			monitorCoroutine = null;
		}
		monitoredTransientSource = null;
	}

	private IEnumerator MonitorTransientSourceCoroutine(AudioSource transient, AudioSource previous)
	{
		if (transient == null)
		{
			// if no transient, try to restore previous immediately
			if (previous != null && previous.isPlaying)
			{
				audioSourceMusic = previous;
			}
			else
			{
				// fallback: try re-acquire
				StartCoroutine(AcquireMusicSourceCoroutine());
			}
			yield break;
		}

		// wait while transient is playing
		while (transient != null && transient.isPlaying)
		{
			yield return null;
		}

		// transient finished - restore previous if still valid, otherwise try to re-acquire
		if (previous != null && previous.isPlaying)
		{
			audioSourceMusic = previous;
		}
		else
		{
			// try to pick an available music source
			if (MMSoundManager.HasInstance)
			{
				var sounds = MMSoundManager.Instance.GetSoundsPlaying(MMSoundManager.MMSoundManagerTracks.Music);
				if (sounds != null && sounds.Count > 0)
				{
					audioSourceMusic = sounds[0].Source;
				}
				else
				{
					// fallback re-acquire coroutine
					StartCoroutine(AcquireMusicSourceCoroutine());
				}
			}
			else
			{
				StartCoroutine(AcquireMusicSourceCoroutine());
			}
		}

		monitorCoroutine = null;
		monitoredTransientSource = null;
	}
}