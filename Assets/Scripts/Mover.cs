using UnityEngine;
public class Mover : MonoBehaviour
{
    public float speed=5;
    void Update()
    {
        transform.Translate(Input.GetAxis("Horizontal")*speed*Time.deltaTime,0,0);
        var p=transform.position; p.x=Mathf.Clamp(p.x,-10,10); transform.position=p;
        if(Input.GetKeyDown(KeyCode.Space)) Debug.Log("Секіру!");
        if(Input.GetKeyDown(KeyCode.Alpha1)) speed=2;
        if(Input.GetKeyDown(KeyCode.Alpha2)) speed=5;
        if(Input.GetKeyDown(KeyCode.Alpha3)) speed=10;
    }
}
