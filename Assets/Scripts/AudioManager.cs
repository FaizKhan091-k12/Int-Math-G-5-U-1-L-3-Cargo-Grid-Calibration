using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private AudioClip correctClip;
    [SerializeField] private AudioClip wrongClip;
    [SerializeField] private AudioClip clickClip;

    [Header("Mute Settings")]
    [SerializeField] private bool isMuted = false;
    [SerializeField] private Image muteButtonImage;
    [SerializeField] private Sprite muteSprite;
    [SerializeField] private Sprite unmuteSprite;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(instance.gameObject);
        }
        instance = this;

        // Ensure clips exist
        if (clickClip == null || (clickClip != null && clickClip.name.ToLower().Contains("dial")))
        {
            clickClip = CreateSoftCyberClick();
        }

        if (correctClip == null)
        {
            correctClip = CreateCyberCorrectChime();
        }

        if (wrongClip == null)
        {
            wrongClip = CreateCyberGlitchBuzz();
        }
    }

    public void Initialize(AudioSource music, AudioSource sfx, AudioClip bgm, AudioClip correct, AudioClip wrong, Image muteImg = null, Sprite muteSp = null, Sprite unmuteSp = null)
    {
        musicSource = music;
        sfxSource = sfx;
        bgmClip = bgm;
        if (correct != null) correctClip = correct;
        if (wrong != null) wrongClip = wrong;
        if (clickClip == null) clickClip = CreateSoftCyberClick();
        if (muteImg != null) muteButtonImage = muteImg;
        if (muteSp != null) muteSprite = muteSp;
        if (unmuteSp != null) unmuteSprite = unmuteSp;

        PlayBGM();
        UpdateMuteVisuals();
    }

    private AudioClip CreateSoftCyberClick()
    {
        int sampleRate = 44100;
        int samples = (int)(sampleRate * 0.035f); // 35ms warm low-frequency haptic tap
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float freq = Mathf.Lerp(450f, 150f, t);
            float envelope = Mathf.Pow(1f - t, 3.5f);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * i / sampleRate) * envelope * 0.25f;
        }
        AudioClip clip = AudioClip.Create("CyberClick", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip CreateCyberCorrectChime()
    {
        int sampleRate = 44100;
        int samples = (int)(sampleRate * 0.6f); // 600ms triumphant chord
        float[] data = new float[samples];
        float[] freqs = new float[] { 523.25f, 659.25f, 783.99f, 1046.50f, 1318.51f }; // C5, E5, G5, C6, E6
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float envelope = Mathf.Pow(1f - t, 1.6f);
            float sampleSum = 0f;
            for (int f = 0; f < freqs.Length; f++)
            {
                sampleSum += Mathf.Sin(2f * Mathf.PI * freqs[f] * i / sampleRate);
            }
            data[i] = (sampleSum / freqs.Length) * envelope * 0.5f;
        }
        AudioClip clip = AudioClip.Create("CyberCorrectChime", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip CreateCyberGlitchBuzz()
    {
        int sampleRate = 44100;
        int samples = (int)(sampleRate * 0.3f);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float freq = Mathf.Lerp(240f, 90f, t);
            float envelope = Mathf.Pow(1f - t, 2.0f);
            float tone = Mathf.Sin(2f * Mathf.PI * freq * i / sampleRate);
            float noise = (Random.value * 2f - 1f) * 0.3f;
            data[i] = Mathf.Clamp(tone + noise, -1f, 1f) * envelope * 0.4f;
        }
        AudioClip clip = AudioClip.Create("CyberGlitchBuzz", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private void Start()
    {
        PlayBGM();
        UpdateMuteVisuals();
    }

    public void PlayBGM()
    {
        if (musicSource != null && bgmClip != null)
        {
            musicSource.clip = bgmClip;
            musicSource.loop = true;
            musicSource.volume = isMuted ? 0f : 0.4f;
            if (!musicSource.isPlaying)
            {
                musicSource.Play();
            }
        }
    }

    public void PlayCorrect()
    {
        if (isMuted) return;
        if (sfxSource != null && correctClip != null)
        {
            sfxSource.pitch = 1.0f;
            sfxSource.PlayOneShot(correctClip, 0.95f);
        }
    }

    public void PlayWrong()
    {
        if (isMuted) return;
        if (sfxSource != null && wrongClip != null)
        {
            sfxSource.pitch = 0.9f;
            sfxSource.PlayOneShot(wrongClip, 0.95f);
            sfxSource.pitch = 1.0f;
        }
    }

    public void PlayClick()
    {
        if (isMuted) return;
        if (sfxSource != null && clickClip != null)
        {
            sfxSource.pitch = Random.Range(0.95f, 1.05f);
            sfxSource.PlayOneShot(clickClip, 0.7f);
            sfxSource.pitch = 1.0f;
        }
    }

    public void PlayHover()
    {
        // Silenced per user request: removes dial chirp when hovering over buttons
    }

    public void PlayScoreTick()
    {
        // Silenced per user request: removes dial-tone clicks during score rollup
    }

    public void ToggleMute()
    {
        isMuted = !isMuted;
        if (musicSource != null)
        {
            musicSource.volume = isMuted ? 0f : 0.4f;
        }
        if (sfxSource != null)
        {
            sfxSource.volume = isMuted ? 0f : 1f;
        }
        UpdateMuteVisuals();
    }

    private void UpdateMuteVisuals()
    {
        if (muteButtonImage != null)
        {
            muteButtonImage.sprite = isMuted ? muteSprite : unmuteSprite;
        }
    }

    public bool IsMuted => isMuted;
}
