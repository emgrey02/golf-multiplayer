using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
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

    private Quaternion _handRotation;

    public bool lastTurn = false;

    public bool myTurn;

    public GameObject canvasGO;
    public UIOverlay overlay;

    public eGolfPlayerState state = eGolfPlayerState.waiting;

    public bool messageSet = false;

    public Quaternion handRotation {
        get {
            return this._handRotation;
        }

        set {
            this._handRotation = value;
            this.transform.rotation = value;
        }
    }
    
    private void Awake()
    {
        handRotation = Quaternion.identity;
        hand = new CardGolf[4];
        playerName = "";
        canvasGO = PhotonView.Find(3).gameObject;
        overlay = canvasGO.GetComponent<UIOverlay>();
    }

    //these refs don't hold after the scene is reloaded
    //assigning them afterwards fixes the issue
    [PunRPC]
    public void UpdateOverlayField()
    {
        canvasGO = PhotonView.Find(3).gameObject;
        overlay = canvasGO.GetComponent<UIOverlay>();
    }

    //RPC
    //When swapping, we replace a card from our hand with the target card
    //sent as strings bc we can't send them as gameObjects across the network
    //we put it in the correct index
    [PunRPC]
    public void ReplaceCardFromHandWithTarget(string c, string t) {
        for (int i = 0; i < hand.Length; i++) {
            if (c == hand[i].name) {
                CardGolf handCard = ConvertStringToCard(c);
                int cardIndex = Array.FindIndex(hand, c => c == handCard);
                hand[cardIndex] = ConvertStringToCard(t);
                hand[cardIndex].transform.rotation = handRotation;
            }
        }
    }

    //RPC
    //set this playerscript's hand according to an array of card names (an array of strings)
    //syncs on every client
    [PunRPC]
    public void SyncHand(string[] hand)
    {
        print("PLAYER " + playerName + "'s hand: ");
        for (int i = 0; i < hand.Length; i++)
        {
            this.hand[i] = ConvertStringToCard(hand[i]);
            this.hand[i].SetSortingLayerName("Hand");
            this.hand[i].owner = this.actorNum;

            //sync card state
            CardGolf cardScript = this.hand[i].GetComponent<CardGolf>();
            cardScript.state = eGolfCardState.hand;
        }
    }

    //RPC
    //sets every player's myTurn field according to current player turn
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

    //RPC
    //sync player props on every client
    [PunRPC]
    public void SyncPlayer(string name, Player player, int actNum)
    {
        this.playerName = name;
        this.player = player;
        this.actorNum = actNum;
        this.playerGO = this.gameObject;
    }

   //rotate this player's hand + sends RPC to sync it
    public void RotateHand(Quaternion pos)
    {
        for (int i = 0; i < 4; i++)
        {
            this.hand[i].transform.rotation = pos;
        }
        this.photonView.RPC(nameof(SetHandRotation), RpcTarget.All, pos);
    }

    //RPC
    //syncs player's hand rotation (on every client)
    [PunRPC]
    private void SetHandRotation(Quaternion rot) 
    {
        this.handRotation = rot;
    }

    //position this player's hand
    public void PositionHand(Vector3[] pos)
    {
        for (int i = 0; i < 4; i++)
        {
            this.hand[i].transform.localPosition = pos[i];
        }
    }

    //RPC
    //Set this player's state (called on every client so it's synced)
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
            UpdateUI();
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

    //RPC
    //flips this player's hand face up or face down
    [PunRPC]
    public void FlipHand(bool fu)
    {
        for (int i = 0; i < 4; i++)
        {
            this.hand[i].faceUp = fu;
        }
    }

    //convert the name of a card (string) into the card itself (CardGolf)
    public CardGolf ConvertStringToCard(string name)
    {
        print(name);
        return GameObject.Find(name).GetComponent<CardGolf>();
    }

    //activate overlay and set the text
    void UpdateUI()
    {
        overlay.ActivateOverlay();
        overlay.SetOverlayText(state);
    }

    //set message in upper left corner
    public void SetUIMessage(string m)
    {
        overlay.SetMessage(m);
    }

    //remove text in upper left corner
    public void RemoveMessage()
    {
        overlay.RemoveMessage();
    }

    //get nickname from actor number of current turn
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
            overlay.DeactivateRoundOver();
            //and player state is waiting (meaning, they are done peaking)
            //and message hasn't been set
            if (state == eGolfPlayerState.waiting && !messageSet)
            {
                //only show on THIS player's screen 
                if (this.photonView.IsMine)
                {
                    //activate the UI message element
                    SetUIMessage("waiting for other players...");
                    messageSet = true;
                }
            }
        }

        //give this player option to knock
        //when their player state is playing and it isn't the last turn (no one has knocked yet)
        if (state == eGolfPlayerState.playing && !lastTurn) 
        {
            if (this.photonView.IsMine) {
                overlay.RevealKnockButton();
            }
        }

        //gamestate is playing
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
            if (state == eGolfPlayerState.waiting)
            {
                if (this.photonView.IsMine)
                {
                    overlay.RemoveOverlay();
                    overlay.RemoveKnockButton();
                }
            }
        }
        
        //if gamestate is swapping
        if (Camera.main.GetComponent<Golf>().gameState == eGolfGameState.swapping)
        {
            //add UI when player and game state are both swapping
            if (state == eGolfPlayerState.swapping)
            {
                
                if (this.photonView.IsMine)
                {
                    UpdateUI();
                }
            }
        }
        
        //remove knock button and set lastTurn variable for this PlayerScript
        //when it's this playerscript's turn and it's their last turn bc a player knocked
        if (myTurn && Camera.main.GetComponent<Golf>().lastTurns) 
        {
            overlay.RemoveKnockButton();
            if (!this.lastTurn) 
            {
                this.lastTurn = true;
            }
        } 
        
        //do ui stuff when game state is set to roundover
        if (Camera.main.gameObject.GetComponent<Golf>().gameState == eGolfGameState.roundover) 
        {
            print("game state is roundover");
            overlay.RemoveOverlay();
            overlay.ActivateRoundOver();
            overlay.RemoveMessage();
            Camera.main.gameObject.GetComponent<Golf>().FlipCards(true);

            if (state == eGolfPlayerState.waiting) 
            {   
                if (this.photonView.IsMine) 
                {
                    SetUIMessage("waiting to go to next round...");
                }
            }
            
            
        }
    }
}
