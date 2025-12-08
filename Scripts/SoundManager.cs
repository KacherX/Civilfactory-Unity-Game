using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public AudioSource audioSource; // The AudioSource that will play the click sound
    public AudioClip clickSound;   // The click sound clip
    private void Awake()
    {
        PlaySound.audioSource = audioSource;
        PlaySound.clickSound = clickSound;
    }
}
public static class PlaySound
{
    public static AudioSource audioSource; // The AudioSource that will play the click sound
    public static AudioClip clickSound;   // The click sound clip
    public static void PlayButtonClickSound()
    {
        audioSource.PlayOneShot(clickSound);
    }
}
