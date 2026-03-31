using BitGamey;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class SplitscreenActivator : MonoBehaviour
{
    public EasySplitScreen easySplitScreen;
    public PlayerInputManager playerInputManager;

    private int playerCount = 0;
    private void Awake()
    {
        playerCount = 0;
        easySplitScreen = GetComponent<EasySplitScreen>();
        playerInputManager = GetComponent<PlayerInputManager>();
    }

    private void OnEnable()
    {
        playerInputManager.onPlayerJoined += OnPlayerJoined;
    }

    private void OnDisable()
    {
        playerInputManager.onPlayerJoined -= OnPlayerJoined;
    }

    private void OnPlayerJoined(PlayerInput input)
    {
        playerCount++;
        if(playerCount == 1)
        {
            easySplitScreen.player1 = input.gameObject.transform;
        }
        else if (playerCount > 1)
        {
            easySplitScreen.player2 = input.gameObject.transform;

            easySplitScreen.enabled = true;
            this.enabled = false;
        }
    }


}
