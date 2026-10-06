using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BlockCatMenuStatus : MonoBehaviour
{
    [SerializeField] TMP_Text bestLabel, soundLabel;
    [SerializeField] Button soundButton;
    void Start()
    {
        bestLabel.text = "PERSONAL BEST  " + PlayerPrefs.GetInt("BlockBlastBest", 0);
        soundButton.onClick.AddListener(RefreshSound);
        RefreshSound();
    }
    void RefreshSound() => soundLabel.text = PlayerPrefs.GetInt("Muted", 0) == 1 ? "SOUND OFF" : "SOUND ON";
    void OnDestroy() { if (soundButton != null) soundButton.onClick.RemoveListener(RefreshSound); }
}
