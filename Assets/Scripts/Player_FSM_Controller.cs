using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player_FSM_Controller : MonoBehaviour
{
    // Call components and variables for setup, so we can use them later
    // (Many of our Public variables are being set up in the Unity Editor with predetermined variables/prefabs)
    public PlayerState playerState;
    InputController PlayerInputController;
    [SerializeField] private InputActionReference move, jump, dash, attack;
    Rigidbody2D Body2D;
    PhysicsMaterial2D Slip;
    PhysicsMaterial2D Stick;
    public Attack_Controller hori_attack1;
    public Attack_Controller verti_attack1;

    // is_attack_present is public so Attack_Controller can access it- 
    // we will need to add getters/setters later for security purposes
    public bool is_attack_present;
    bool is_on_ground;
    bool is_touching_wall;

    int dash_count;
    int default_dash_count;
    int default_jump_count;
    int jump_count;

    float cooldown_timer;
    float dash_speed;
    float dash_timer;
    float default_dash_timer;
    float dash_cooldown_timer;
    float default_walljump_timer;
    float walljump_timer;
    float default_gravity;
    float direction_x;
    float direction_y;
    float direction_held;
    float direction_of_wall;
    float hori_speed;
    float jump_speed;
    float walk_speed;
    float walljump_speed;
    float wall_speed;

    // Start is called before the first frame update
    void Start()
    {
        //Instantiate our components and variables
        playerState = PlayerState.stand;
        PlayerInputController = new InputController();
        PlayerInputController.Enable();
        Body2D = GetComponent<Rigidbody2D>();
        Slip = Resources.Load<PhysicsMaterial2D>("Materials/Slip");
        Stick = Resources.Load<PhysicsMaterial2D>("Materials/Stick");

        is_attack_present = false;
        is_on_ground = true;

        default_dash_count = 1;
        default_jump_count = 1;
        dash_count = default_dash_count;
        jump_count = default_jump_count;

        dash_speed = 35f;
        dash_cooldown_timer = .2f;
        default_dash_timer = 0.15f;
        default_gravity = 2.5f;
        dash_timer = default_dash_timer;
        default_walljump_timer = 0.2f;
        walljump_timer = default_walljump_timer;
        jump_speed = 12f;
        walk_speed = 8f;
        walljump_speed = 10f;
        wall_speed = 8f;

        Body2D.gravityScale = default_gravity;
    }

    // FixedUpdate is called once per frame
    void FixedUpdate()
    {
        // Allow free movement if state is Stand, Run, Jump, Wallcling, Airborne, or Attack
        if (playerState == PlayerState.stand || playerState == PlayerState.run || playerState == PlayerState.jump || playerState == PlayerState.wallcling || playerState == PlayerState.airborne || playerState == PlayerState.attack)
        {
            // Determine player velocity
            Body2D.velocity = new Vector2(hori_speed, Body2D.velocity.y);
        }
        // Determine player state changes
        HandleStates();
    }

    // Process stick and button presses/releases
    private void OnEnable()
    {
        jump.action.performed += StartJump;
        jump.action.canceled += StopJump;
        dash.action.performed += StartDash;
        attack.action.performed += StartAttack;
    }

    private void OnMove()
    {
        // Make it so that both a controller joystick and keyboard feel the same
        // If the joystick is not pushed far enough, do not accept the input for moving
        if (Mathf.Abs(move.action.ReadValue<Vector2>().x) > Mathf.Epsilon)
        {
            hori_speed = walk_speed * Mathf.Sign(move.action.ReadValue<Vector2>().x);
        }
        else
        {
            hori_speed = 0f;
        }
    }

    // Called when the jump key/button is just pressed
    private void StartJump(InputAction.CallbackContext obj)
    {
        // If the player is able to do a walljump...
        if (is_touching_wall && !is_on_ground && walljump_timer == default_walljump_timer)
        {
            // ... set the player up to walljump and set the other wall-based states to false
            Body2D.velocity = new Vector2(walk_speed * direction_of_wall * 1.3f, walljump_speed);
            
            playerState = PlayerState.walljump;
        }
        // If the player is able to do a standard jump/double jump...
        else if ((is_on_ground || (!is_on_ground && jump_count > 0)) && playerState != PlayerState.dash)
        {
            // ... and the player is in the air...
            if (!is_on_ground)
            {
                // ... decrement our jump counter
                jump_count--;
            }
            
            // ... set the player up to perform a standard jump
            Body2D.velocity = new Vector2(Body2D.velocity.x, jump_speed);
            playerState = PlayerState.jump;
        }
    }

    // Called when the jump key/button is just released
    private void StopJump(InputAction.CallbackContext obj)
    {
        // If the player has not reached the apex of their jump
        // or if the player has already double jumped...
        if (Mathf.Sign(Body2D.velocity.y) == 1 && (playerState == PlayerState.airborne || playerState == PlayerState.jump))
        {
            // ... stop the player from rising
            if (is_on_ground)
            {
                playerState = PlayerState.stand;
            }
            // ... or make the player continue to jump (if the standard jump was a double jump)
            else if (jump_count == default_jump_count)
            {
                Body2D.velocity = new Vector2(Body2D.velocity.x, 0f);
                playerState = PlayerState.airborne;
            }
        }

        // ... ensure the player continues to do their walljump
        if (playerState == PlayerState.walljump)
        {
            playerState = PlayerState.walljump;
        }
    }

    // Called when the dash key/button is pressed
    private void StartDash(InputAction.CallbackContext obj)
    {
        // If the player is not already dashing and the player is holding left or right...
        if (playerState != PlayerState.dash && Mathf.Abs(hori_speed) > 0f)
        {
            // ... and if the player has not already used their dash in the air...
            if (dash_count > 0 && !is_touching_wall && (direction_of_wall == 0f || direction_held != direction_of_wall))
            {
                // ... set up the player to be dashing
                Body2D.velocity = new Vector2(Mathf.Sign(move.action.ReadValue<Vector2>().x) * dash_speed, 0f);
                Body2D.gravityScale = 0f;

                playerState = PlayerState.dash;

                // ... if the player is in the air, decrement our dash counter
                if (!is_on_ground)
                {
                    dash_count--;
                }
            }
        }
    }

    // Called when the attack key/button is pressed
    private void StartAttack(InputAction.CallbackContext obj)
    {
        // If the player is not already attacking and they are in a Default State...
        if (!is_attack_present && (playerState == PlayerState.stand || playerState == PlayerState.run || playerState == PlayerState.jump || playerState == PlayerState.airborne))
        {
            // ... store the direction that the player is holding...
            direction_x = Mathf.Sign(move.action.ReadValue<Vector2>().x);
            direction_y = Mathf.Sign(move.action.ReadValue<Vector2>().y);
            // ... and spawn either a hori or verti attack
            if (Mathf.Abs(move.action.ReadValue<Vector2>().y) > 0.5f)
            {
                SpawnAttack(verti_attack1, true);
            }
            else
            {
                SpawnAttack(hori_attack1, false);
            }

            playerState = PlayerState.attack;
        }
    }

    private void SpawnAttack(Attack_Controller specificAttack, bool is_Y_Pressed)
    {
        // Instantiate a new attack based on the information from specificAttack and is_Y_pressed
        Attack_Controller newAttack = Instantiate(specificAttack);
        newAttack.user = GetComponent<Stats_Controller>();
        newAttack.transform.SetParent(this.gameObject.transform);
        // If the player is holding the stick up or down...
        if (is_Y_Pressed)
        {
            // ... instantiate a vertical attack
            newAttack.transform.position = new Vector3(transform.position.x, transform.position.y + direction_y, transform.position.z);
        }
        else
        {
            // ... or instantiate a horizontal attack if they're not
            newAttack.transform.position = new Vector3(transform.position.x + direction_x, transform.position.y, transform.position.z);
        }
        // Set is_attack_present to true (this will be reset by the Attack_Controller class)
        is_attack_present = true;
    }

    private void ReturnToDefaults()
    {
        // Set the player back to a Default State (standing, running, or airborne)
        walljump_timer = default_walljump_timer;
        Body2D.gravityScale = default_gravity;

        if (!is_on_ground && !is_touching_wall)
        {
            playerState = PlayerState.airborne;
        }
        if (is_on_ground && hori_speed == 0f)
        {
            playerState = PlayerState.stand;
        }
        else if (is_on_ground && Mathf.Abs(hori_speed) > 0f)
        {
            playerState = PlayerState.run;
        }
    }

    public enum PlayerState
    {
        stand,
        run,
        jump,
        attack,
        dash,
        wallcling,
        walljump,
        wallrun,
        wallslide,
        airborne
    }

    private void HandleStates()
    {
        switch (playerState)
        {
            case PlayerState.stand:
                Stand();
                break;
            case PlayerState.run:
                Run();
                break;
            case PlayerState.jump:
                Jump();
                break;
            case PlayerState.attack:
                Attack();
                break;
            case PlayerState.dash:
                Dash();
                break;
            case PlayerState.wallcling:
                Wallcling();
                break;
            case PlayerState.walljump:
                Walljump();
                break;
            case PlayerState.wallrun:
                Wallrun();
                break;
            case PlayerState.wallslide:
                Wallslide();
                break;
            case PlayerState.airborne:
                Airborne();
                break;
        }
    }

    private void Stand()
    {
        // Set up the player to change states
        if (Mathf.Abs(hori_speed) > 0f)
        {
            playerState = PlayerState.run;
        }
    }

    private void Run()
    {
        // Set up the player to change states
        if (hori_speed == 0f)
        {
            playerState = PlayerState.stand;
        }
    }

    private void Jump()
    {
        // Set up the player to change states
        if (Mathf.Sign(Body2D.velocity.y) != 1)
        {
            playerState = PlayerState.airborne;
        }
    }

    private void Attack()
    {
        // Set up the player to change states
        if (!is_attack_present)
        {
            ReturnToDefaults();
        }
    }

    private void Dash()
    {
        // Decrement our dash timer
        dash_timer -= Time.deltaTime;

        // If our dash timer is done with or we start touching a wall...
        if (dash_timer <= 0f || is_touching_wall)
        {
            // ... set up the player to change states
            dash_timer = default_dash_timer;
            ReturnToDefaults();
        }
    }

    private void Wallcling()
    {
        // Increase the player's friction so they slide slowly/stick on walls
        Body2D.sharedMaterial = Stick;
        // Save the players held direction into direction_held for later use
        direction_held = Mathf.Sign(move.action.ReadValue<Vector2>().x);
        // Set up the player to change states
        ReturnToDefaults();
    }

    private void Walljump()
    {
        // Decrement our walljump timer
        walljump_timer -= Time.deltaTime;

        // If our walljump timer is done with...
        if (walljump_timer <= 0f)
        {
            // ... set up the player to change states
            walljump_timer = default_walljump_timer;
            playerState = PlayerState.airborne;
            Body2D.velocity = Vector2.zero;
        }
    }

    private void Wallrun()
    {
        // Set the player's velocity to run up the wall
        Body2D.velocity = new Vector2(Body2D.velocity.x, wall_speed);
        // If we aren't touching a wall and we are not walljumping...
        if (!is_touching_wall && playerState != PlayerState.walljump)
        {
            // ... set up the player to change states
            playerState = PlayerState.airborne;
        }              
    }

    private void Wallslide()
    {
        // Set up the player to change states
        ReturnToDefaults();
    }

    private void Airborne()
    {
        // Set up the player to change states
        ReturnToDefaults();
    }

    // Detects when the player touches floors or walls
    private void OnCollisionStay2D(Collision2D collision)
    {
        // Reset the player's resources if they are standing on flat ground
        if (collision.gameObject.tag == "Floor" && collision.GetContact(0).normal == Vector2.up)
        {
            is_on_ground = true;
            dash_count = default_dash_count;
            jump_count = default_jump_count;
        }
        // If the player is touching a wall and not on the ground
        else if (collision.gameObject.tag == "Wall")
        {
            is_touching_wall = true;
            direction_of_wall = collision.GetContact(0).normal.x;

            if (!is_on_ground && playerState != PlayerState.walljump)
            {
                // If the player's joystick/ d-pad is being held up
                if(move.action.ReadValue<Vector2>().y > .7f)
                {
                    playerState = PlayerState.wallrun;
                }
                // If the player's joystick/ d-pad is being held left or right
                else if (Mathf.Abs(move.action.ReadValue<Vector2>().x) > 0.7f)// && !is_wallrunning)
                {
                    playerState = PlayerState.wallcling;
                }
                // If the player's joystick/ d-pad is being held in neutral
                else
                {
                    // Set the wall-based states back to false
                    direction_held = 0f;
        
                    playerState = PlayerState.wallslide;
                }
            }
        }
    }

    // Detects when the player leaves floors or walls
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Floor")
        {
            is_on_ground = false;
        }
        // Decrease the player's friction so they only slide on walls
        else if ((collision.gameObject.tag == "Wall"))
        {
            // Set the wall-based states back to false
            is_touching_wall = false;
            direction_of_wall = 0f;
            Body2D.sharedMaterial = Slip;
        }
    }
}