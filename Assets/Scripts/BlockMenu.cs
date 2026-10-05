using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class BlockMenu : MonoBehaviour
{
    void Start() => AudioListener.pause = PlayerPrefs.GetInt("Muted", 0) == 1;
    public void PlayGame() => SceneManager.LoadScene("Game");
    public void ToggleSound()
    {
        AudioListener.pause = !AudioListener.pause;
        PlayerPrefs.SetInt("Muted", AudioListener.pause ? 1 : 0); PlayerPrefs.Save();
    }
}
