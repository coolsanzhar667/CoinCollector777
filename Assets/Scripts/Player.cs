using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

// Rigidbody movement, ground-only jumping and collision/trigger interactions.
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class Player : MonoBehaviour
{
    public float speed = 5.5f, jumpForce = 11f;
    public int health = 100, totalCoins = 5;
    public TMP_Text scoreText, healthText, resultText;
    public Slider healthSlider;
    public GameObject gameOverPanel, winPanel;
    public AudioClip coinSound, jumpSound, hurtSound;
    public int Score { get; private set; }
    public bool IsGrounded { get; private set; }
    public bool Finished { get; private set; }
    public float Elapsed { get; private set; }
    Rigidbody2D rb;
    SpriteRenderer sr;
    AudioSource audioSource;
    float axis, invincibleUntil;
    bool jump;
    internal bool automated;
    internal float drive;
    internal bool jumpRequest;
    readonly RaycastHit2D[] hits = new RaycastHit2D[8];
    void Awake() { rb=GetComponent<Rigidbody2D>(); sr=GetComponent<SpriteRenderer>(); audioSource=GetComponent<AudioSource>(); }
    void Start() { UpdateHUD(); }
    void Update()
    {
        if (Finished || Time.timeScale == 0) return;
        Elapsed += Time.deltaTime;
        axis=automated?drive:Input.GetAxisRaw("Horizontal");
        if (automated?jumpRequest:Input.GetKeyDown(KeyCode.Space)) jump=true;
        jumpRequest=false;
        if (transform.position.y < -8) TakeDamage(100,true);
        if (axis != 0) sr.flipX=axis<0;
    }
    void FixedUpdate()
    {
        if (Finished) return;
        IsGrounded=false;
        var filter=new ContactFilter2D(); filter.SetLayerMask(1<<6); filter.useTriggers=false;
        int count=rb.Cast(Vector2.down,filter,hits,.1f);
        for(int i=0;i<count;i++) if(hits[i].normal.y>.65f) IsGrounded=true;
        rb.linearVelocity=new Vector2(axis*speed,rb.linearVelocity.y);
        if(jump && IsGrounded) { rb.linearVelocity=new Vector2(rb.linearVelocity.x,jumpForce); audioSource.PlayOneShot(jumpSound,.6f); IsGrounded=false; }
        jump=false;
    }
    void OnCollisionEnter2D(Collision2D collision) { if(collision.gameObject.CompareTag("Enemy")) TakeDamage(20); }
    void OnCollisionStay2D(Collision2D collision) { if(collision.gameObject.CompareTag("Enemy")) TakeDamage(20); }
    void OnTriggerEnter2D(Collider2D other)
    {
        if(Finished || !other.CompareTag("Coin") || !other.enabled) return;
        other.enabled=false;
        Score++; audioSource.PlayOneShot(coinSound); Destroy(other.gameObject); UpdateHUD();
        if(Score>=totalCoins) Win();
    }
    public void TakeDamage(int damage, bool force=false)
    {
        if(Finished || (!force && Time.time<invincibleUntil)) return;
        invincibleUntil=Time.time+.85f;
        health=Mathf.Max(0,health-damage); UpdateHUD(); audioSource.PlayOneShot(hurtSound);
        StartCoroutine(Blink());
        if(health<=0) { Finished=true; gameOverPanel.SetActive(true); Time.timeScale=0; }
    }
    IEnumerator Blink() { sr.color=new Color(1,.3f,.4f); yield return new WaitForSecondsRealtime(.2f); sr.color=Color.white; }
    void UpdateHUD() { scoreText.text="Ұпай: "+Score+" / "+totalCoins; healthText.text="Өмір: "+health; healthSlider.value=health; }
    void Win()
    {
        Finished=true;
        string key="BestTime_"+UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        float old=PlayerPrefs.GetFloat(key,float.MaxValue); float best=Mathf.Min(old,Elapsed);
        PlayerPrefs.SetFloat(key,best); PlayerPrefs.Save();
        resultText.text="Уақыт: "+Elapsed.ToString("F1")+" с     /     Рекорд: "+best.ToString("F1")+" с";
        winPanel.SetActive(true); Time.timeScale=0;
    }
}
