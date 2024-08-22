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

    bool is_dashing;
    bool is_in_cooldown;
    bool is_on_ground;

    int dash_count;
    int jump_count;

    float cooldown_timer;
    float dash_speed;
    float dash_timer;
    float default_dash_timer;
    float dash_cooldown_timer;
    float default_gravity;
    float hori_speed;
    float jump_speed;

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

        is_dashing = false;
        is_in_cooldown = false;
        is_on_ground = false;

        dash_count = 1;
        jump_count = 1;

        dash_speed = 15f;
        dash_cooldown_timer = .2f;
        default_dash_timer = 0.15f;
        default_gravity = 2f;
        dash_timer = default_dash_timer;
        jump_speed = 10f;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // Set the player's velocity using hori_speed if they are not performing
        // a special move
        if (!is_dashing)
        {
            Body2D.velocity = new Vector2(hori_speed, Body2D.velocity.y);
            
            // If the player is in cooldown, they can only move or jump
            if (is_in_cooldown)
            {
                Debug.Log(cooldown_timer);
                cooldown_timer -= Time.deltaTime;
                if (cooldown_timer < 0)
                {
                    is_in_cooldown = false;
                }
            }
        }
        // Handle the player's dash when is_dashing is true
        else
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
                Body2D.velocity = new Vector2(0f, 0f);
                is_in_cooldown = true;
                cooldown_timer = dash_cooldown_timer;
            }
        }
    }

    // Call the Input System function whenever the player moves left or right
    private void OnMove()
    {
        // Math so that both a controller joystick and keyboard feel the same
        // If the joystick is not pushed far enough, do not accept the input for moving
        if (Mathf.Abs(move.action.ReadValue<Vector2>().x) > 0.99f)
        {
            // Multiply the direction that the player wants to go by a constant value
            hori_speed = 5 * Mathf.Sign(move.action.ReadValue<Vector2>().x);
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
        if ((is_on_ground || jump_count > 0) && !is_dashing)
        {
            // Add jump_speed to the player's velocity.y
            Body2D.velocity = new Vector2(Body2D.velocity.x, jump_speed);

            // If the player double jumped, decrement jump_count
            if (!is_on_ground)
            {
                jump_count--;
            }
        }
    }

    // Called when the jump key/ button is released
    private void StopJump(InputAction.CallbackContext obj)
    {
        // If the player has not reached the apex of their jump
        // or if the player has already double jumped
        if (Mathf.Sign(Body2D.velocity.y) == 1 && jump_count > 0)
        {
            // Set the player's velocity.y to 0
            Body2D.velocity = new Vector2(Body2D.velocity.x, 0f);
        }
    }

    // Called when the dash key/ button is pressed
    private void Dash(InputAction.CallbackContext obj)
    {
        // If the player is not already dashing, has not already used
        // their dash in the air, and is not already in cooldown
        if (!is_dashing && dash_count > 0 && !is_in_cooldown)
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

    // Detects when the player touches floor
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Reset the player's resources
        if (collision.gameObject.tag == "Floor")
        {
            is_on_ground = true;
            dash_count = 1;
            jump_count = 1;
        }
    }
    
    // Detects when the player leaves floor
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Floor")
        {
            is_on_ground = false;
        }
    }
}