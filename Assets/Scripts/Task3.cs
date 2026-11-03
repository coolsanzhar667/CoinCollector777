using UnityEngine;
public class Task3 : MonoBehaviour
{
    int score=0;
    void Start() { AddScore(10);AddScore(5);AddScore(20);AddScore(100); }
    public void AddScore(int amount) { score+=amount;Debug.Log("Ұпай: "+score); }
}
