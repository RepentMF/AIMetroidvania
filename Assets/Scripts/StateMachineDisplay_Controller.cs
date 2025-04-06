using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StateMachineDisplay_Controller : MonoBehaviour
{
    public Player_FSM_Controller player;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        GetComponent<TMP_Text>().text = player.playerState.ToString();
    }
}
