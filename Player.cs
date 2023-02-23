using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{


    //config
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float jumpSpeed = 5f;
    [SerializeField] float timeToFade = 10f;
    [SerializeField] AudioClip jumpSound;
    [SerializeField] AudioClip fadeSound;
    [SerializeField] AudioClip soulSound;
    [SerializeField] AudioClip fallingSound;
    [SerializeField] AudioClip dieSound;
    [SerializeField] AudioClip reaperSound;
    [SerializeField] AudioClip levelCompleteSound;
    [SerializeField] AudioClip darkCaveMusic;
    [SerializeField] Reaper reaper;

    //state
    public bool isAlive = true;
    public bool isPossessing = false;
    bool isGhost = false;
    GameObject possessedObject;
    int humanLayerIndex;

    //cached component references
    Rigidbody2D myRigidBody;
    Animator myAnimator;
    CapsuleCollider2D myCollider;
    BoxCollider2D feetCollider;
    SpriteRenderer mySpriteRenderer;
    ParticleSystem myParticalSystem;
    AudioSource myAudioSource;
    float startingGravity;

    // Start is called before the first frame update
    void Start()
    {
        myRigidBody = GetComponent<Rigidbody2D>();
        myAnimator = GetComponent<Animator>();
        myCollider = GetComponent<CapsuleCollider2D>();
        feetCollider = GetComponent<BoxCollider2D>();
        myAudioSource = GetComponent<AudioSource>();
        mySpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        myParticalSystem = GetComponentInChildren<ParticleSystem>();
        startingGravity = myRigidBody.gravityScale;
        humanLayerIndex = myAnimator.GetLayerIndex("Human");
        myAnimator.SetLayerWeight(humanLayerIndex, 1);
        if (FindObjectOfType<LevelLoader>().GetCurrentScene() != 0)
        {
            myAnimator.SetLayerWeight(humanLayerIndex, 0);
            isGhost = true;
        }
        if (FindObjectOfType<LevelLoader>().GetCurrentScene() == 4)
        {
            isGhost = false;
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (isAlive && !isPossessing)
        {
            Run();
            Jump();
            Fall();
            FlipSprite();
            if (isGhost) { Fade(); }
        }
        if (!isAlive) {myRigidBody.velocity = new Vector2(0, 0);}
        if (isPossessing) {transform.position = possessedObject.transform.position;}
    }
    private void Run()
    {
        var deltaX = Input.GetAxis("Horizontal") * moveSpeed;
        myRigidBody.velocity = new Vector2(deltaX, myRigidBody.velocity.y);

        bool playerHasHorizontalSpeed = Mathf.Abs(myRigidBody.velocity.x) > Mathf.Epsilon;
        myAnimator.SetBool("isRunning", playerHasHorizontalSpeed);
    }
    private void Jump()
    {
        if (Input.GetButtonDown("Jump") && IsTouchingGround())
        {
            Vector2 jumpVelocityToAdd = new Vector2(0f, jumpSpeed);
            AudioSource.PlayClipAtPoint(jumpSound, gameObject.transform.position, 0.25f);
            myRigidBody.velocity += jumpVelocityToAdd;
            myAnimator.SetBool("isJumping", true);
            StartCoroutine(Land());
        }
        bool playerIsJumping = myRigidBody.velocity.y > Mathf.Epsilon;
        if (playerIsJumping && !IsTouchingGround())
        {
            myAnimator.SetBool("isJumping", true);
        }
        else
        {
            myAnimator.SetBool("isJumping", false);
        }
    }
    private void Fall()
    {
        bool playerIsFalling = myRigidBody.velocity.y < -Mathf.Epsilon;
        if(playerIsFalling && !IsTouchingGround())
        {
            myAnimator.SetBool("isFalling", true);
        }
        else
        {
            myAnimator.SetBool("isFalling", false);
        }  
    }
    IEnumerator Land()
    {
        yield return new WaitForSeconds(0.01f);
        if (IsTouchingGround()) 
        {
            myAnimator.SetBool("isJumping", false);
        }
        else
        {
            StartCoroutine(Land());
        }
    }

    private bool IsTouchingGround()
    {
        if(feetCollider.IsTouchingLayers(LayerMask.GetMask("Foreground")) || feetCollider.IsTouchingLayers(LayerMask.GetMask("Bridge")))
        {
            return true;
        }
        else { return false; }
    }
    private void FlipSprite()
    {
        bool playerHasHorizontalSpeed = Mathf.Abs(myRigidBody.velocity.x) > Mathf.Epsilon;
        if (playerHasHorizontalSpeed)
        {
            transform.localScale = new Vector2(Mathf.Sign(myRigidBody.velocity.x), transform.localScale.y);
        }
    }
    private void Fade()
    {
        float fadeAmount = 1 / timeToFade;
        mySpriteRenderer.color = new Color(1f, 1f, 1f, mySpriteRenderer.color.a-fadeAmount*Time.deltaTime);
        if(mySpriteRenderer.color.a <= 0) { Die(); }
    }

    public void Die()
    {
        isAlive = false;
        myRigidBody.velocity = new Vector2(0, 0);
        AudioSource.PlayClipAtPoint(fadeSound, gameObject.transform.position, 0.25f);
        FindObjectOfType<GameSession>().ShowLoseScreen();
    }

    public void Restart()
    {
        FindObjectOfType<GameSession>().ResetGameSession();
    }

    public void PossessObject(GameObject vessel)
    {
        isPossessing = true;
        possessedObject = vessel;
        myParticalSystem.Play();
        AudioSource.PlayClipAtPoint(soulSound, gameObject.transform.position, 0.25f);
        myRigidBody.velocity = new Vector2(0, 0);
        myRigidBody.bodyType = RigidbodyType2D.Kinematic;
        myCollider.enabled = false;
        mySpriteRenderer.color = new Color(1f, 1f, 1f, 0f);
        transform.position = vessel.transform.position;
    }
    public void LeaveObject()
    {
        isPossessing = false;
        possessedObject = null;
        myParticalSystem.Play();
        AudioSource.PlayClipAtPoint(soulSound, gameObject.transform.position, 0.25f);
        transform.position = transform.position + new Vector3(0f, 1.2f);
        myRigidBody.bodyType = RigidbodyType2D.Dynamic;
        myCollider.enabled = true;
        mySpriteRenderer.color = new Color(1f, 1f, 1f, 1f);
    }

    public void GoGhost()
    {
        StartCoroutine(PlayerDieAnim());
    }

    IEnumerator PlayerDieAnim()
    {
        yield return new WaitForSeconds(0.3f);
        isAlive = false;
        myAnimator.SetTrigger("playerDie");
        StartCoroutine(RiseAgain());
    }

    IEnumerator RiseAgain()
    {
        yield return new WaitForSeconds(3f);
        myParticalSystem.Play();
        AudioSource.PlayClipAtPoint(soulSound, gameObject.transform.position, 0.25f);
        mySpriteRenderer.color = new Color(1f, 1f, 1f, 0f);
        yield return new WaitForSeconds(1f);
        myAnimator.SetLayerWeight(humanLayerIndex, 0);
        myAnimator.Play("ghost_idle");
        mySpriteRenderer.color = new Color(1f, 1f, 1f, 1f);
        isAlive = true;
        Instantiate(reaper);
        yield return new WaitForSeconds(1f);
        FindObjectOfType<GameSession>().ShowWorldUI();
        yield return new WaitForSeconds(10f);
        isGhost = true;
        PlayDarkCaveMusic();
    }

    public IEnumerator PlayFallingSound()
    {
        myAudioSource.clip = fallingSound;
        myAudioSource.Play();
        yield return new WaitForSeconds(3.5f);
        myAudioSource.clip = dieSound;
        myAudioSource.Play();
        myAudioSource.loop = false;
    }

    public void PlayReaperSound()
    {
        AudioSource.PlayClipAtPoint(reaperSound, gameObject.transform.position, 0.25f);
    }
    public void PlayLevelCompleteSound()
    {
        myAudioSource.clip = levelCompleteSound;
        myAudioSource.loop = false;
        myAudioSource.Play();
    }

    void PlayDarkCaveMusic()
    {
        myAudioSource.clip = darkCaveMusic;
        myAudioSource.loop = true;
        myAudioSource.Play();
    }
}
