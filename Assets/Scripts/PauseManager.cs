using UnityEngine;
public class PauseManager : MonoBehaviour
{
    public GameObject pausePanel;
    public Player player;
    void Update() { if(Input.GetKeyDown(KeyCode.Escape)) TogglePause(); }
    public void TogglePause() { if(player && player.Finished) return; bool paused=!pausePanel.activeSelf; pausePanel.SetActive(paused); Time.timeScale=paused?0:1; }
    public void Resume() { pausePanel.SetActive(false); Time.timeScale=1; }
}
