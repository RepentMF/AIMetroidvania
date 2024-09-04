using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player_Controller : MonoBehaviour
{
    // Call components and variables for setup, so we can use them later
    InputController PlayerInputController;
    [SerializeField]
    private InputActionReference move, jump, dash;
    Rigidbody2D Body2D;
    PhysicsMaterial2D Slip;
    PhysicsMaterial2D Stick;

    bool is_dashing;
    bool is_in_cooldown;
    bool is_on_ground;
    bool is_touching_wall;
    bool is_wallclinging;
    bool is_walljumping;
    bool is_wallrunning;

    int dash_count;
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
    

    // Method allows calls to other methods when inputs are provided
    private void OnEnable()
    {
        jump.action.performed += StartJump;
        jump.action.canceled += StopJump;
        dash.action.started += Dash;
    }

    // Start is called before the first frame update
    void Start()
    {
        //Instantiate our components and variables
        PlayerInputController = new InputController();
        PlayerInputController.Enable();
        Body2D = GetComponent<Rigidbody2D>();
        Slip = Resources.Load<PhysicsMaterial2D>("Materials/Slip");
        Stick = Resources.Load<PhysicsMaterial2D>("Materials/Stick");

        is_touching_wall = false;
        is_dashing = false;
        is_in_cooldown = false;
        is_on_ground = false;
        is_wallclinging = false;
        is_walljumping = false;
        is_wallrunning = false;

        dash_count = 1;
        jump_count = 1;

        dash_speed = 15f;
        dash_cooldown_timer = .2f;
        default_dash_timer = 0.15f;
        default_gravity = 2f;
        dash_timer = default_dash_timer;
        default_walljump_timer = 0.2f;
        walljump_timer = default_walljump_timer;
        jump_speed = 10f;
        walk_speed = 5f;
        walljump_speed = 7f;
        wall_speed = 5f;
    }

    // Update is called once per frame
    void FixedUpdate()
    {   
        Debug.Log(is_wallclinging + ", " + is_wallrunning + ", " + is_walljumping);

        // Set the player's velocity using hori_speed if they are not performing
        // a special move
        if (!is_dashing && !is_walljumping && !is_wallrunning)
        {
            Body2D.velocity = new Vector2(hori_speed, Body2D.velocity.y);

            // If the player is in cooldown, they can only move or jump
            if (is_in_cooldown)
            {
                cooldown_timer -= Time.deltaTime;
                if (cooldown_timer < 0)
                {
                    is_in_cooldown = false;
                }
            }
        }
        // Handle the player's dash when is_dashing is true
        else if (is_dashing)
        {
            // Count down on dash_timer
            dash_timer -= Time.deltaTime;
            // If dash_timer is complete
            if (dash_timer <= 0f)
            {
                // Re-set up the player and start dash cooldown
                is_dashing = false;
                Body2D.gravityScale = default_gravity;
                dash_timer = default_dash_timer;
                Body2D.velocity = Vector2.zero;
                is_in_cooldown = true;
                cooldown_timer = dash_cooldown_timer;
            }
        }
        // Handle the player's walljump when is_walljumping is true
        else if (is_walljumping)
        {
            // Count down on walljump_timer
            walljump_timer -= Time.deltaTime;
            // If walljump_timer is complete
            if (walljump_timer <= 0f)
            {
                // Re-set up the player's walljump variables
                is_walljumping = false;
                walljump_timer = default_walljump_timer;
                // If the direction the player is holding is the same as the direction
                // the player was holding to move into the wall
                if (Mathf.Sign(move.action.ReadValue<Vector2>().x) == direction_held)
                {
                    // Stop the player from climbing infinitely
                    Body2D.velocity = Vector2.zero;
                }
            }
        }
    }

    // Call the Input System function whenever the player moves left or right
    private void OnMove()
    {
        // Math so that both a controller joystick and keyboard feel the same
        // If the joystick is not pushed far enough, do not accept the input for moving
        if (Mathf.Abs(move.action.ReadValue<Vector2>().x) > 0.97f)
        {
            // Multiply the direction that the player wants to go by a constant value
            hori_speed = walk_speed * Mathf.Sign(move.action.ReadValue<Vector2>().x);
        }
        else
        {
            // If we get no left/ right input, make horizontal speed 0
            hori_speed = 0;
        }
    }

    // Called when the jump key/ button is pressed
    private void StartJump(InputAction.CallbackContext obj)
    {
        // If the player is on the ground and is not dashing
        if ((is_on_ground || jump_count > 0) && !is_dashing && !is_touching_wall)
        {
            // Add jump_speed to the player's velocity.y
            Body2D.velocity = new Vector2(Body2D.velocity.x, jump_speed);

            // If the player double jumped, decrement jump_count
            if (!is_on_ground)
            {
                jump_count--;
            }
        }
        // If the player is touching a wall
        else if (is_touching_wall)
        {
            // Set the player up to walljump and set the other wall-based states to false
            Body2D.velocity = new Vector2(walk_speed * direction_of_wall, walljump_speed);
            is_walljumping = true;
            is_wallclinging = false;
            is_wallrunning = false;
        }
    }

    // Called when the jump key/ button is released
    private void StopJump(InputAction.CallbackContext obj)
    {
        // If the player has not reached the apex of their jump
        // or if the player has already double jumped
        if (!is_walljumping)
        {
            if (Mathf.Sign(Body2D.velocity.y) == 1 && jump_count > 0)
            {
                // Set the player's velocity.y to 0
                Body2D.velocity = new Vector2(Body2D.velocity.x, 0f);
            }
        }
    }

    // Called when the dash key/ button is pressed
    private void Dash(InputAction.CallbackContext obj)
    {
        // If the player is not already dashing, has not already used
        // their dash in the air, and is not already in cooldown
        if (!is_dashing && dash_count > 0 && !is_in_cooldown)
        {
            if (!(is_touching_wall && Mathf.Sign(move.action.ReadValue<Vector2>().x) == direction_held))
            {
                // Set up the player to be dashing
                Body2D.velocity = new Vector2(Mathf.Sign(move.action.ReadValue<Vector2>().x) * dash_speed, 0f);
                Body2D.gravityScale = 0f;
                is_dashing = true;

                // If the player is in the air, decrement dash_count
                if (!is_on_ground)
                {
                    dash_count--;
                }
            }
        }
    }

    // Detects when the player touches floors or walls
    private void OnCollisionStay2D(Collision2D collision)
    {
        // Reset the player's resources if they are standing on flat ground
        if (collision.gameObject.tag == "Floor" && collision.GetContact(0).normal == Vector2.up)
        {
            is_on_ground = true;
            dash_count = 1;
            jump_count = 1;
        }
        // If the player is touching a wall and not on the ground
        else if (collision.gameObject.tag == "Wall" && !is_on_ground)
        {
            is_touching_wall = true;
            direction_of_wall = collision.GetContact(0).normal.x;

            // If the player's joystick/ d-pad is being held up
            if(move.action.ReadValue<Vector2>().y > .7f)
            {
                is_wallrunning = true;
                is_wallclinging = false;
                Body2D.velocity = new Vector2(Body2D.velocity.x, wall_speed);
            }
            // If the player's joystick/ d-pad is being held left or right
            else if (Mathf.Abs(move.action.ReadValue<Vector2>().x) > 0.7f && !is_wallrunning)
            {
                // Increase the player's friction so they slide slowly/ stick on walls
                Body2D.sharedMaterial = Stick;
                is_wallclinging = true;
                is_wallrunning = false;
                // Save the players held direction into direction_held for later use
                direction_held = Mathf.Sign(move.action.ReadValue<Vector2>().x);   
            }
            // If the player's joystick/ d-pad is being held in neutral
            else
            {
                // Set the wall-based states back to false
                is_wallclinging = false;
                is_wallrunning = false;
                direction_held = 0f;
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
            is_wallclinging = false;
            is_wallrunning = false;
            direction_of_wall = 0f;
            Body2D.sharedMaterial = Slip;
        }
    }
}