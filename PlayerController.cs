using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net.Security;
using JetBrains.Annotations;
using Microsoft.Unity.VisualStudio.Editor;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class PlayerController : MonoBehaviour
{

    [SerializeField] private float defaultSpeed;
    [SerializeField] private float jumpAmount;
    [SerializeField] private float updateSpeedPerSecond;
    [SerializeField] private byte protectDuration = 1;
    [SerializeField] private float doubleJumpAmount;
    [SerializeField] private float heightRespawn;
    [SerializeField] private Sprite doubleJumpImage;
    private static float speed;
    private Rigidbody2D rigid;
    private bool TouchLeftOrRight;
    private bool doubleJumpAvaluable = false;
    private Animator animator;
    private InputActions inputActions;
    public static bool respawnSleepingProtect = false;
    private bool durationSleepingBubble = false;
    private bool died = false;
    private bool protect = false;
    private SpriteRenderer spriteRenderer;

    // Start is called before the first frame update

    private void Start()
    {
        protectDuration *= 10;
        speed = defaultSpeed;
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        InvokeRepeating(nameof(addSpeedPerSecond), 1, 1);

        inputActions.Player.TouchPressed.started += ctx => getTouch(ctx);
    }

    private void getTouch(InputAction.CallbackContext ctx)
    {

        if (inputActions.Player.TouchPosition.ReadValue<Vector2>().x < Screen.width / 2)
        {
            if (respawnSleepingProtect) if (!durationSleepingBubble) { Continue(); }
            Jump();
        }
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }
    private void OnDisable()
    {
        inputActions.Disable();
    }
    private void Awake()
    {
        inputActions = new InputActions();
    }
    private void Update()
    {
        if (transform.position.y < -7 && !died)
        {
            Die();
        }
        //Rớt xuống đất thì không còn double jump nữa
        animator.SetBool("onGround", GroundCheck.onGround);
    }
    private void addSpeedPerSecond()
    {
        if (!respawnSleepingProtect)
            speed += updateSpeedPerSecond;

    }
    private void FixedUpdate()
    {
        if (!respawnSleepingProtect)
        {
            Run();
        }
    }
    private void Run()
    {
        rigid.velocity = new Vector2(speed * Time.fixedDeltaTime, rigid.velocity.y);
    }
    public void Jump()
    {

        //true = doublejumpamount : 2, false = jump amount
        if (GroundCheck.onGround || doubleJumpAvaluable)
        {
            if (GroundCheck.onGround)
            {
                doubleJumpAvaluable = false;
            }
            GroundCheck.onGround = false;
            if (doubleJumpAvaluable) doubleJumpPacticle();
            rigid.velocity = new Vector2(rigid.velocity.x, doubleJumpAvaluable ? doubleJumpAmount : jumpAmount);

            doubleJumpAvaluable = !doubleJumpAvaluable;
        }
    }
    private void doubleJumpPacticle()
    {
        GameObject doubleJumpObject = new GameObject();

        doubleJumpObject.AddComponent<SpriteRenderer>();
        doubleJumpObject.AddComponent<Rigidbody2D>();

        Rigidbody2D rigidDoubleJumpObject = doubleJumpObject.GetComponent<Rigidbody2D>();
        doubleJumpObject.GetComponent<SpriteRenderer>().sprite = doubleJumpImage;

        doubleJumpObject.transform.position = transform.position;
        rigidDoubleJumpObject.velocity = new Vector2(rigid.velocity.x, rigidDoubleJumpObject.velocity.y * -5);

        Destroy(doubleJumpObject, 0.5f);
    }
    private void Continue()
    {
        rigid.simulated = true;
        respawnSleepingProtect = false;
        animator.SetBool("respawnProtect", false);
    }
    private void Die()
    {
        HealthSystem.health -= 1;
        died = true;
        if (CanRespawn())
        {
            if (transform.position.y < -7)
            {
                rigid.simulated = false;
                Invoke(nameof(RespawnSleeping), 0.5f);
            }
            else
            {
                ProtectEnable();
                rigid.AddForce(new Vector2(rigid.velocity.x, 5), ForceMode2D.Impulse);
            }
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    private void ProtectEnable()
    {
        protect = true;
        blink = 0;
        died = false;
        InvokeRepeating(nameof(ProtectEffect), 0.1f, 0.1f);
    }
    private Color colorDefault = Color.white;
    private Color colorChange = Color.red;

    private byte blink;
    private void ProtectEffect()
    {
        blink += 1;
        if (blink <= protectDuration)
        {
            Color spriteColor = spriteRenderer.color;
            if (spriteColor == colorDefault)
            {
                spriteRenderer.color = colorChange;
            }
            else
            {
                spriteRenderer.color = colorDefault;
            }
        }

        else
        {
            disableProtect();
            CancelInvoke(nameof(ProtectEffect));
        }
    }
    private void disableProtect()
    {
        protect = false;
    }
    private void RespawnSleeping()
    {
        respawnSleepingProtect = true;
        durationSleepingBubble = true;
        died = false;
        animator.SetBool("respawnProtect", true);
        InvokeRepeating(nameof(respawnMove), 0.01f, 0.001f);
    }
    private void respawnMove()
    {
        transform.position = new Vector2(transform.position.x, transform.position.y + 0.01f);
        if (transform.position.y > heightRespawn)
        {
            CancelInvoke(nameof(respawnMove));
            GroundCheck.onGround = true;
            DisableSleepingBuble();
        }
    }
    private void DisableSleepingBuble()
    {
        durationSleepingBubble = false;
    }
    private void OnTriggerEnter2D(Collider2D collider)
    {
        GameObject objectBeEntered = collider.gameObject;
        if (!protect)
        {
            if (objectBeEntered.CompareTag("Spite"))
            {
                Die();
            }
            if (objectBeEntered.CompareTag("Slime"))
            {
                if (transform.position.y > objectBeEntered.transform.position.y)
                {
                    objectBeEntered.GetComponent<Animator>().SetBool("died", true);
                    Destroy(objectBeEntered, 1f);
                }
                else Die();
            }
        }
        if (objectBeEntered.CompareTag("Candy"))
        {
            Debug.Log("ADD");
            Destroy(objectBeEntered);
            HealthSystem.miniHealth += 1;
            
        }
    }

    private bool CanRespawn()
    {
        if (HealthSystem.health >= 0)
        {
            return true;
        }
        else return false;
    }
}
