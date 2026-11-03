using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Opt-in integration verification. Never enabled during ordinary play.
public class GameVerification : MonoBehaviour
{
    string folder;
    readonly List<string> results=new List<string>();
    readonly Dictionary<string,float> originals=new Dictionary<string,float>();
    bool persistence;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        string trigger=Path.GetFullPath(Path.Combine(Application.dataPath,"../QA_REQUEST"));
        string persisted=Path.GetFullPath(Path.Combine(Application.dataPath,"../QA_PERSIST_REQUEST"));
        if(!File.Exists(trigger) && !File.Exists(persisted)) return;
        bool second=File.Exists(persisted);
        if(File.Exists(trigger)) File.Delete(trigger);
        var q=new GameObject("Integration verification").AddComponent<GameVerification>();q.persistence=second;DontDestroyOnLoad(q);
    }
    void Start() {folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Screenshots"));Directory.CreateDirectory(folder);Application.logMessageReceived+=Log;if(persistence)StartCoroutine(PersistCheck());else StartCoroutine(Run());}
    IEnumerator PersistCheck()
    {
        results.AddRange(File.ReadAllLines(Path.Combine(folder,"Verification.txt")));
        string file=Path.GetFullPath(Path.Combine(Application.dataPath,"../QA_PERSIST_REQUEST"));
        foreach(string line in File.ReadAllLines(file))
        {
            var parts=line.Split('|');float expected=float.Parse(parts[1],System.Globalization.CultureInfo.InvariantCulture), old=float.Parse(parts[2],System.Globalization.CultureInfo.InvariantCulture);
            Check(Mathf.Abs(PlayerPrefs.GetFloat(parts[0],-100)-expected)<.001f,"Record survives full process restart: "+parts[0]);
            if(old<0)PlayerPrefs.DeleteKey(parts[0]);else PlayerPrefs.SetFloat(parts[0],old);
        }
        PlayerPrefs.Save();File.Delete(file);results.Add("Test-generated records restored; final verification complete.");Write();yield return new WaitForSecondsRealtime(.5f);Application.Quit();
    }
    void Log(string message,string trace,LogType type) {if(type==LogType.Exception || type==LogType.Error) {results.Add("ERROR: "+message);Write();}}
    void Check(bool value,string name) {results.Add((value?"PASS: ":"FAIL: ")+name);Write();}
    void Write() {File.WriteAllLines(Path.Combine(folder,"Verification.txt"),results);}
    IEnumerator Snap(string name) {yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));yield return new WaitForSecondsRealtime(.5f);}
    void Click(string name) {var buttons=FindObjectsByType<Button>(FindObjectsSortMode.None);foreach(var b in buttons)if(b.name==name){b.onClick.Invoke();return;}throw new Exception("Missing button "+name);}
    IEnumerator LoadWait() {yield return null;yield return new WaitForSecondsRealtime(.7f);}
    IEnumerator Run()
    {
        foreach(string name in new[]{"Game","Level2","Level3"}) {string k="BestTime_"+name;originals[k]=PlayerPrefs.GetFloat(k,-1);PlayerPrefs.DeleteKey(k);}
        yield return new WaitForSecondsRealtime(1);
        Click("PlayButton");yield return LoadWait();
        Check(SceneManager.GetActiveScene().name=="Game","Play button loads Game");
        for(int level=1;level<=3;level++)
        {
            var p=FindFirstObjectByType<Player>();p.automated=true;
            yield return new WaitForSeconds(.7f);
            Check(p.IsGrounded,"Level "+level+" player lands on Ground");
            Check(Mathf.Abs(p.healthSlider.fillRect.rect.width-p.healthSlider.GetComponent<RectTransform>().rect.width)<1,"Health fill fits slider bounds "+level);
            var pause=FindFirstObjectByType<PauseManager>();pause.TogglePause();float t=p.Elapsed;
            yield return new WaitForSecondsRealtime(.2f);Check(Time.timeScale==0&&p.Elapsed==t,"Pause freezes timer and physics "+level);pause.Resume();
            if(level==1)yield return Snap("02_Gameplay");
            // Each main platform is reached by normal horizontal velocity and grounded jumps.
            for(int i=0;i<5 && !p.Finished;i++)
            {
                float x=-7+i*4, y=-2.9f+i*(level==3?1.45f:1.35f);
                float timeout=Time.realtimeSinceStartup+9;
                bool launched=false;
                while(Time.realtimeSinceStartup<timeout && !p.Finished)
                {
                    float dx=x-p.transform.position.x;p.drive=Mathf.Abs(dx)>.1f?Mathf.Sign(dx):0;
                    if(p.IsGrounded && !launched) {p.jumpRequest=true;launched=true;}
                    if(launched && p.IsGrounded && p.transform.position.y>y+.4f && Mathf.Abs(dx)<.25f)break;
                    if(launched && p.IsGrounded && p.transform.position.y<y+.3f) {p.jumpRequest=true;}
                    yield return null;
                }
                p.drive=0;Check(p.transform.position.y>y+.35f || p.Finished,"Reach platform "+(i+1)+" in level "+level+" using physics");
                yield return new WaitForSecondsRealtime(.1f);
            }
            // Additional ground/side coins: position directly to isolate actual trigger wiring.
            foreach(var coin in GameObject.FindGameObjectsWithTag("Coin"))
            {
                if(p.Finished)break;
                p.GetComponent<Rigidbody2D>().position=coin.transform.position;p.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;
                yield return new WaitForSecondsRealtime(.15f);
            }
            Check(p.Score==p.totalCoins && p.winPanel.activeSelf && Time.timeScale==0,"Level "+level+" all coins trigger victory");
            string key="BestTime_"+SceneManager.GetActiveScene().name;Check(PlayerPrefs.GetFloat(key,0)>0,"Record stored for "+key);
            yield return Snap(level==1?"03_Victory":"Level"+level+"_Victory");
            if(level<3) {Click("NextButton");yield return LoadWait();Check(SceneManager.GetActiveScene().buildIndex==level+1,"Next button loads level "+(level+1));}
            else Check(Array.Find(p.winPanel.GetComponentsInChildren<Button>(true),b=>b.name=="NextButton")==null,"Final level has no Next button");
        }
        Click("MenuButton");yield return LoadWait();Check(FindFirstObjectByType<MenuManager>().recordText.text.Contains(" с"),"Menu shows level 1 record");yield return Snap("01_Menu");
        Click("PlayButton");yield return LoadWait();
        var player=FindFirstObjectByType<Player>();var spike=GameObject.FindGameObjectsWithTag("Enemy")[0];
        player.GetComponent<Rigidbody2D>().position=(Vector2)spike.transform.position+Vector2.up*.7f;
        yield return new WaitForSeconds(1);Check(player.health<100&&player.healthSlider.value==player.health,"Enemy collision lowers health and slider");
        for(int i=0;i<5;i++){player.TakeDamage(20,true);yield return new WaitForSecondsRealtime(.21f);}
        Check(player.health==0&&player.gameOverPanel.activeSelf&&Time.timeScale==0,"Five hits show Game Over and clamp health");yield return Snap("04_GameOver");
        Click("RestartButton");yield return LoadWait();Check(FindFirstObjectByType<Player>().health==100&&Time.timeScale==1,"Restart resets health and time scale");
        FindFirstObjectByType<MenuManager>().ToMenu();yield return LoadWait();Click("LabButton");yield return LoadWait();Check(FindFirstObjectByType<Mover>()!=null,"Module 2 lab accessible");yield return Snap("05_Module2");
        var persisted=new List<string>();foreach(var item in originals)persisted.Add(item.Key+"|"+PlayerPrefs.GetFloat(item.Key).ToString(System.Globalization.CultureInfo.InvariantCulture)+"|"+item.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        File.WriteAllLines(Path.GetFullPath(Path.Combine(Application.dataPath,"../QA_PERSIST_REQUEST")),persisted);PlayerPrefs.Save();
        results.Add("Platform route uses real movement; extra coins use trigger positioning. Waiting for fresh process persistence check.");Write();
        FindFirstObjectByType<MenuManager>().ToMenu();yield return LoadWait();Application.Quit();
    }
}
