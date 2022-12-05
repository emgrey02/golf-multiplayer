using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public enum eGolfPlayerState
{
    peaking,
    waiting,
    playing
}
public class PlayerScript : MonoBehaviourPunCallbacks
{
    public CardGolf[] hand;
    public Player player;
    public string playerName;
    public int actorNum;
    public GameObject playerGO;

    public bool myTurn;

    public UIOverlay overlay;

    public eGolfPlayerState state = eGolfPlayerState.waiting;

    private bool messageSet = false;
    
    private void Awake()
    {
        hand = new CardGolf[4];
        //player = PhotonNetwork.LocalPlayer;
        //actorNum = player.ActorNumber;
        //playerGO = this.gameObject;
        playerName = "";
        overlay = GameObject.Find("Canvas").GetComponent<UIOverlay>();
    }

    [PunRPC]
    public void SyncHand(string[] hand)
    {
        print("PLAYER " + playerName + "'s hand: ");
        for (int i = 0; i < hand.Length; i++)
        {
            this.hand[i] = ConvertStringToCard(hand[i]);
            
            if (PhotonNetwork.LocalPlayer.IsMasterClient)
            {
                PhotonView cardView = this.hand[i].GetComponent<PhotonView>();
                
                cardView.TransferOwnership(player);
            }

            //sync card state
            CardGolf cardScript = this.hand[i].GetComponent<CardGolf>();
            cardScript.state = eGolfCardState.hand;

            //set player state
            //state = eGolfPlayerState.peaking;
        }
        UpdateUI();
    }

    [PunRPC]
    public void SyncPlayer(string name, Player player, int actNum)
    {
        this.playerName = name;
        this.player = player;
        this.actorNum = actNum;
        this.playerGO = this.gameObject;
    }

    [PunRPC]
    public void RotateCards(Quaternion pos)
    {
        for (int i = 0; i < 4; i++)
        {
            this.hand[i].transform.rotation = pos;
        }
    }

    [PunRPC]
    public void SyncHandPOS(Vector3[] pos)
    {
        for (int i = 0; i < 4; i++)
        {
            this.hand[i].transform.position = pos[i];
        }
    }

    [PunRPC]
    public void SyncPlayerState(string pState)
    {
        if (pState == "waiting")
        {
            state = eGolfPlayerState.waiting;
        }

        if (pState == "playing")
        {
            state = eGolfPlayerState.playing;
        }

        if (pState == "peaking")
        {
            state = eGolfPlayerState.peaking;
        }
    }

    [PunRPC]
    public void ResetHand()
    {
        for (int i = 0; i < 4; i++)
        {
            this.hand[i].faceUp = false;
        }
    }

    public CardGolf ConvertStringToCard(string name)
    {
        print(name);
        return GameObject.Find(name).GetComponent<CardGolf>();
    }

    void UpdateUI()
    {
        overlay.SetOverlayText(state);
        overlay.ActivateOverlay();
    }

    [PunRPC]
    public void SetUIMessage(string m)
    {
        overlay.SetMessage(m);
    }

    [PunRPC]
    public void RemoveMessage()
    {
        overlay.RemoveMessage();
    }

    void Update()
    {
        //is game state is peaking
        if (Camera.main.GetComponent<Golf>().gameState == eGolfGameState.peaking) 
        {
            //and player state is waiting (meaning, they are done peaking)
            //and message hasn't been set
            if (state == eGolfPlayerState.waiting && !messageSet)
            {
                if (playerGO.GetPhotonView().IsMine)
                {
                    //activate the UI message element
                    SetUIMessage("waiting for other players...");
                    messageSet = true;
                }
            }

        }

        if (Camera.main.GetComponent<Golf>().gameState == eGolfGameState.playing)
        {
            if (messageSet)
            {
                RemoveMessage();
            }
        }

    }
}
