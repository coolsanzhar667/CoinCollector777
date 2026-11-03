using UnityEngine;
public class Task2 : MonoBehaviour
{
    // Example names; replace with actual classmates if required.
    public string[] names={"Айдар","Мадина","Ерлан","Аружан","Данияр"};
    void Start() { foreach(string person in names) Debug.Log("Сәлем, "+person+"!"); }
}
