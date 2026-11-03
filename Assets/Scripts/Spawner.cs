using UnityEngine;
// Optional module 3 task. The playable scenes use hand-placed prefab instances.
public class Spawner : MonoBehaviour
{
    public GameObject coinPrefab;
    public int count=5;
    void Start() { for(int i=0;i<count;i++) Instantiate(coinPrefab,transform.position+new Vector3(i*2f,0,0),Quaternion.identity); }
}
