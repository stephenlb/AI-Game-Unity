using UnityEngine;

/// <summary>
/// Helper script to set up the game scene.
/// Attach this to an empty GameObject in your scene to auto-setup.
/// </summary>
public class GameSetup : MonoBehaviour
{
    [Header("Optional: Assign existing objects")]
    public Sprite backgroundImage;
    public AudioClip[] musicTracks;

    void Awake()
    {
        // Create GameManager if it doesn't exist
        if (GameManager.Instance == null)
        {
            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();
        }

        // Create AudioManager if it doesn't exist
        if (AudioManager.Instance == null)
        {
            GameObject audioObj = new GameObject("AudioManager");
            AudioManager audioManager = audioObj.AddComponent<AudioManager>();

            // Assign music if provided
            if (musicTracks != null && musicTracks.Length > 0)
            {
                audioManager.level1Music = musicTracks[0];
                if (musicTracks.Length > 1) audioManager.level2Music = musicTracks[1];
                if (musicTracks.Length > 2) audioManager.level3Music = musicTracks[2];
            }
        }
    }
}
