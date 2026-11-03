using UnityEngine;
// Separate from the final game's Player, preserving the lesson's negative health example.
public class HealthLesson : MonoBehaviour
{
    public int health=100;
    void Start() { TakeDamage(30);TakeDamage(50);TakeDamage(40); }
    public void TakeDamage(int damage) { health-=damage;Debug.Log("Өмір: "+health);if(health<=0) Debug.Log("Ойын аяқталды!"); }
}
