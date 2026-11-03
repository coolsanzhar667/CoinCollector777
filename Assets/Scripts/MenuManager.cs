using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
public class MenuManager : MonoBehaviour
{
    public TMP_Text recordText, timerText;
    public Player player;
    void Start()
    {
        Time.timeScale=1;
        if(recordText) { float best=PlayerPrefs.GetFloat("BestTime_Game",0); recordText.text=best>0 ? "Рекорд: "+best.ToString("F1")+" с" : "Рекорд жоқ — алғашқы сапарыңды баста"; }
    }
    void Update() { if(timerText && player) timerText.text=player.Elapsed.ToString("F1")+" с"; }
    public void PlayGame() => Load("Game");
    public void ToMenu() => Load("MainMenu");
    public void OpenLab() => Load("Module2");
    public void RestartGame() => Load(SceneManager.GetActiveScene().name);
    public void NextLevel() { int next=SceneManager.GetActiveScene().buildIndex+1; if(next<=3) { Time.timeScale=1; SceneManager.LoadScene(next); } }
    void Load(string scene) { Time.timeScale=1; SceneManager.LoadScene(scene); }
    public void QuitGame() { Application.Quit(); Debug.Log("Шығу"); }
    public void ToggleSound() { AudioListener.volume=AudioListener.volume>.1f?0:1; }
}
