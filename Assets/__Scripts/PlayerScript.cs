using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System;

public enum eGolfPlayerState
{
    peaking,
    waiting,
    playing,
    deciding,
    swapping
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
        playerName = "";
        overlay = GameObject.Find("Canvas").GetComponent<UIOverlay>();
    }

    [PunRPC]
    public void ReplaceCardFromHandWithTarget(string c, string t) {
        for (int i = 0; i < hand.Length; i++) {
            if (c == hand[i].name) {
                CardGolf handCard = ConvertStringToCard(c);
                int cardIndex = Array.FindIndex(hand, c => c == handCard);
                hand[cardIndex] = ConvertStringToCard(t);
            }
        }
    }

    [PunRPC]
    public void SyncHand(string[] hand)
    {
        print("PLAYER " + playerName + "'s hand: ");
        for (int i = 0; i < hand.Length; i++)
        {
            this.hand[i] = ConvertStringToCard(hand[i]);
            this.hand[i].SetSortingLayerName("Hand");
            
            if (PhotonNetwork.LocalPlayer.IsMasterClient)
            {
                PhotonView cardView = this.hand[i].GetComponent<PhotonView>();
                
                cardView.TransferOwnership(player);
            }

            //sync card state
            CardGolf cardScript = this.hand[i].GetComponent<CardGolf>();
            cardScript.state = eGolfCardState.hand;


        }
        UpdateUI();
    }

    [PunRPC]
    public void UpdateTurnVar()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties["currentTurn"].ToString() == actorNum.ToString())
        {
            myTurn = true;
            state = eGolfPlayerState.playing;
        } else
        {
            myTurn = false;
        }
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

        if (pState == "deciding")
        {
            state = eGolfPlayerState.deciding;
        }

        if (pState == "swapping")
        {
            state = eGolfPlayerState.swapping;
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
        overlay.ActivateOverlay();
        overlay.SetOverlayText(state);
    }

    public void SetUIMessage(string m)
    {
        overlay.SetMessage(m);
    }

   
    public void RemoveMessage()
    {
        overlay.RemoveMessage();
    }

    public string GetCurrentTurnNickName()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties["currentTurn"] != null)
        {
            for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
            {
                string currentActNum = PhotonNetwork.CurrentRoom.CustomProperties["currentTurn"].ToString(); 
                if (PhotonNetwork.PlayerList[i].ActorNumber.ToString() == currentActNum)
                {
                    return PhotonNetwork.PlayerList[i].NickName;
                }
            }
        }
        return null;
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
            //handles beginning peaking message removal
            if (messageSet)
            {
                RemoveMessage();
                messageSet = false;
            }

            //turn signifier
            if (this.photonView.IsMine)
            {
                if (myTurn)
                {
                    SetUIMessage("Your turn!");
                } else
                {
                    SetUIMessage(GetCurrentTurnNickName() + "'s turn...");
                }
            } 

            //add UI when player state switches to deciding
            if (state == eGolfPlayerState.deciding)
            {
                if (this.photonView.IsMine)
                {
                    UpdateUI();
                }
            }

            //remove UI if player is in waiting state
            if (state == eGolfPlayerState.waiting && Camera.main.GetComponent<Golf>().gameState == eGolfGameState.playing)
            {
                if (this.photonView.IsMine)
                {
                    overlay.RemoveOverlay();
                }
            }
        }
        
        if (Camera.main.GetComponent<Golf>().gameState == eGolfGameState.swapping)
        {
            //add UI when player and game state are both swapping
            if (state == eGolfPlayerState.swapping)
            {
                print("player and game in swapping state");
                if (this.photonView.IsMine)
                {
                    UpdateUI();
                }
            }
        }
    }
}
