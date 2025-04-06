using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player_FSM_Controller : MonoBehaviour
{
    public PlayerState playerState;
    // Call components and variables for setup, so we can use them later
    InputController PlayerInputController;
    [SerializeField]
    private InputActionReference move, jump, dash;
    Rigidbody2D Body2D;
    PhysicsMaterial2D Slip;
    PhysicsMaterial2D Stick;

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

    // Update is called once per frame
    void FixedUpdate()
    {
        // Stand, Run, Jump, Wallcling, and Airborne
        if (playerState == PlayerState.stand || playerState == PlayerState.run || playerState == PlayerState.jump || playerState == PlayerState.wallcling || playerState == PlayerState.airborne)
        {
            // Multiply the direction that the player wants to go by a constant value
            Body2D.velocity = new Vector2(hori_speed, Body2D.velocity.y);
        }
        HandleStates();
    }

    // Method allows calls to other methods when inputs are provided
    private void OnEnable()
    {
        jump.action.performed += StartJump;
        jump.action.canceled += StopJump;
        dash.action.performed += StartDash;
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

    // Called when the jump key/ button is pressed
    private void StartJump(InputAction.CallbackContext obj)
    {
        // If the player is doing a walljump
        if (is_touching_wall && !is_on_ground && walljump_timer == default_walljump_timer)
        {
            // Set the player up to walljump and set the other wall-based states to false
            Body2D.velocity = new Vector2(walk_speed * direction_of_wall * 1.3f, walljump_speed);
            
            playerState = PlayerState.walljump;
        }
        else if ((is_on_ground || (!is_on_ground && jump_count > 0)) && playerState != PlayerState.dash)
        {
            if (!is_on_ground)
            {
                jump_count--;
            }
            
            // Add jump_speed to the player's velocity.y
            Body2D.velocity = new Vector2(Body2D.velocity.x, jump_speed);
            playerState = PlayerState.jump;
        }
    }

    // Called when the jump key/ button is released
    private void StopJump(InputAction.CallbackContext obj)
    {
        // If the player has not reached the apex of their jump
        // or if the player has already double jumped
        if (Mathf.Sign(Body2D.velocity.y) == 1 && (playerState == PlayerState.airborne || playerState == PlayerState.jump))
        {
            // Set the player's velocity.y to 0
            if (is_on_ground)
            {
                playerState = PlayerState.stand;
            }
            else if (jump_count == default_jump_count)
            {
                Body2D.velocity = new Vector2(Body2D.velocity.x, 0f);
                playerState = PlayerState.airborne;
            }
        }

        if (playerState == PlayerState.walljump)
        {
            playerState = PlayerState.walljump;
        }
    }

    // Called when the dash key/ button is pressed
    private void StartDash(InputAction.CallbackContext obj)
    {
        if (playerState != PlayerState.dash && Mathf.Abs(hori_speed) > 0f)
        {
            // If the player is not already dashing, has not already used
            // their dash in the air, and is not already in cooldown
            if (dash_count > 0 && !is_touching_wall && (direction_of_wall == 0f || direction_held != direction_of_wall))
            {
                // Set up the player to be dashing
                Body2D.velocity = new Vector2(Mathf.Sign(move.action.ReadValue<Vector2>().x) * dash_speed, 0f);
                Body2D.gravityScale = 0f;

                playerState = PlayerState.dash;

                // If the player is in the air, decrement dash_count
                if (!is_on_ground)
                {
                    dash_count--;
                }
            }
        }
    }

    private void ReturnToDefaults()
    {
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
        attack1,
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
            // Not implemented yet
            case PlayerState.attack1:
                Attack1();
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
        if (Mathf.Abs(hori_speed) > 0f)
        {
            playerState = PlayerState.run;
        }
    }

    private void Run()
    {
        if (hori_speed == 0f)
        {
            playerState = PlayerState.stand;
        }
    }

    private void Jump()
    {
        if (Mathf.Sign(Body2D.velocity.y) != 1)
        {
            playerState = PlayerState.airborne;
        }
    }

    private void Attack1()
    {

    }

    private void Dash()
    {
        // Count down on dash_timer
        dash_timer -= Time.deltaTime;

        if (dash_timer <= 0f || is_touching_wall)
        {
            dash_timer = default_dash_timer;
            ReturnToDefaults();
        }
    }

    private void Wallcling()
    {
        // Increase the player's friction so they slide slowly/ stick on walls
        Body2D.sharedMaterial = Stick;
        // Save the players held direction into direction_held for later use
        direction_held = Mathf.Sign(move.action.ReadValue<Vector2>().x);
        ReturnToDefaults();
    }

    private void Walljump()
    {
        // Count down on walljump_timer
        walljump_timer -= Time.deltaTime;

        if (walljump_timer <= 0f)
        {
            walljump_timer = default_walljump_timer;
            playerState = PlayerState.airborne;
            // If the direction the player is holding is the same as the direction
            // the player was holding to move into the wall
            //if (Mathf.Sign(move.action.ReadValue<Vector2>().x) == direction_held)
            //{
                // Stop the player from climbing infinitely
            Body2D.velocity = Vector2.zero;
            //}
        }
    }

    private void Wallrun()
    {
        Body2D.velocity = new Vector2(Body2D.velocity.x, wall_speed);
        if (!is_touching_wall && playerState != PlayerState.walljump)
        {
            playerState = PlayerState.airborne;
        }              
    }

    private void Wallslide()
    {
        ReturnToDefaults();
    }

    private void Airborne()
    {
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