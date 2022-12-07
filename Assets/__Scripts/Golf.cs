using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;
using Photon.Realtime;

public enum eGolfGameState
{
    setup,
    peaking,
    playing,
    swapping,
    roundover,
    gameover
}

public class Golf : MonoBehaviourPunCallbacks
{
    public TextAsset deckXML;

    public GameObject deckGO;
    public Deck deck;

    public List<Card> deckCards;
    public List<CardGolf> drawPile;
    public List<CardGolf> discardPile;
    public eGolfGameState gameState = eGolfGameState.setup;

    public GameObject canvas;

    public CardGolf target;

    public int currentPlayerTurn;

    private List<GameObject> playerGOs;
    private List<PlayerScript> playerPMs;
    private List<PhotonView> playerViews;

    private const int maxPeaks = 2;
    private int peakCount = 0;

    private bool disableClicks = false;

    public const byte onDrawEventCode = 1;
    public const byte onDiscardEventCode = 2;
    public const byte onSwapEventCode = 3;

    private Vector3[] localPositions = new Vector3[] { new Vector3(-2f, -6f, 0), new Vector3(2f, -6f, 0), new Vector3(-2f, -10.5f, 0), new Vector3(2f, -10.5f, 0) };
    private Vector3[] oppPositions = new Vector3[] { new Vector3(2, 6, 0), new Vector3(-2, 6, 0), new Vector3(2, 10.5f, 0), new Vector3(-2, 10.5f, 0) };
    private Vector3[] leftPositions = new Vector3[] { new Vector3(-6, 2, 0), new Vector3(-6, -2, 0), new Vector3(-10.5f, 2, 0), new Vector3(-10.5f, -2, 0) };
    private Vector3[] rightPositions = new Vector3[] { new Vector3(6, -2, 0), new Vector3(6, 2, 0), new Vector3(10.5f, -2, 0), new Vector3(10.5f, 2, 0) };


    private void Awake()
    {
        playerGOs = new List<GameObject>();
        playerPMs = new List<PlayerScript>();
        playerViews = new List<PhotonView>();

        canvas = GameObject.Find("Canvas");
    }

    private void Start()
    {
        //MasterClient will spawn Players to Network, set proper ownership, update PlayerManager
        //...and then instantiate deck, shuffle them, create the draw pile, 
        // create the players, deal the cards, and position the cards on the screen
        if (PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
            {
                GameObject player = PhotonNetwork.Instantiate("Player", new Vector3(0, 0, 0), Quaternion.identity);
                PlayerScript PM = player.GetComponent<PlayerScript>();
                PhotonView playerView = player.GetComponent<PhotonView>();

                playerGOs.Add(player);
                playerPMs.Add(PM);
                playerViews.Add(playerView);

                PlayerManager.S.AddPlayer(PM);
                playerView.TransferOwnership(PhotonNetwork.PlayerList[i]);
            }

            CreateDeck();
            SetPlayers();
            DealCards();
            PositionCards();
        } else
        {
            //everyone else's camera rotates so their own hand is in front of them
            RotateCamera();
        }

        //set first game state - peaking
        gameState = eGolfGameState.peaking;


    }

    //this method runs when a player presses the "I'm done peaking" button
    //sets the player's custom prop
    public void SetPeakCustProp()
    {
        Debug.Log("button pressed.");
        Hashtable custProps = new Hashtable();
        custProps.Add("Peak", "done");
        PhotonNetwork.LocalPlayer.SetCustomProperties(custProps);
    }

    //this is called when the custom prop change is registered
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        base.OnPlayerPropertiesUpdate(targetPlayer, changedProps);
        if (PhotonNetwork.LocalPlayer.IsMasterClient && (gameState == eGolfGameState.peaking))
        {
            Debug.Log("master client checking end peaking state");
            CheckForEndPeakingState();
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        base.OnRoomPropertiesUpdate(propertiesThatChanged);
        if (PhotonNetwork.LocalPlayer.IsMasterClient && gameState == eGolfGameState.playing)
        {
            Debug.Log("updating turn vars");
            for (int i = 0; i < playerPMs.Count; i++)
            {
                playerPMs[i].photonView.RPC("UpdateTurnVar", RpcTarget.All);
            }
        }
    }

    //only master client uses this method
    private void CheckForEndPeakingState()
    {
        //time to check player custom props and get a list of
        //players who are done peaking
        List<PlayerScript> playersDonePeaking = new List<PlayerScript>();
        int count = 0;
        for (int i = 0; i < playerGOs.Count; i++)
        {
            if (PhotonNetwork.PlayerList[i].CustomProperties["Peak"] != null)
            {
                count++;
                playersDonePeaking.Add(playerPMs[i]);
            }
        }

        //set player state to "waiting" if they are done peaking but there are still
        //others peaking
        if (count < playerPMs.Count)
        {
            Debug.Log("set player state to waiting");
            for (int j = 0; j < playersDonePeaking.Count; j++)
            {
                SetPlayerState(playersDonePeaking[j].photonView, "waiting");
            }
        }

        //if everyone is done peaking, then flip all the cards over and
        //change the game state
        if (count == playerPMs.Count)
        {
            print("all players have peaked. Changing player state");
            FlipCards();
            ChangePlayerStates("waiting");
            Camera.main.gameObject.GetPhotonView().RPC(nameof(SyncGameState), RpcTarget.All, "playing");
            MoveDrawPile();
            MoveToDiscard(Draw());
            Camera.main.gameObject.GetPhotonView().RPC(nameof(AssignTurn), RpcTarget.All, playerPMs[0].actorNum);
        }
    }

    [PunRPC]
    public void AssignTurn(int actorNum)
    {
        print("assigning next turn");
        currentPlayerTurn = actorNum;

        Hashtable custProps = new Hashtable();
        custProps.Add("currentTurn", actorNum.ToString());
        PhotonNetwork.CurrentRoom.SetCustomProperties(custProps);

        disableClicks = false;
        
        if (PhotonNetwork.LocalPlayer.IsMasterClient && currentPlayerTurn != PhotonNetwork.LocalPlayer.ActorNumber)
        {
            for (int i = 0; i < drawPile.Count; i++)
            {
                drawPile[i].photonView.TransferOwnership(PhotonNetwork.CurrentRoom.GetPlayer(actorNum));
            }
            for (int i = 0; i < discardPile.Count; i++)
            {
                discardPile[i].photonView.TransferOwnership(PhotonNetwork.CurrentRoom.GetPlayer(actorNum));
            }
        } else
        {
            for (int i = 0; i < drawPile.Count; i++)
            {
                drawPile[i].photonView.TransferOwnership(PhotonNetwork.CurrentRoom.GetPlayer(actorNum));
            }
            for (int i = 0; i < discardPile.Count; i++)
            {
                discardPile[i].photonView.TransferOwnership(PhotonNetwork.CurrentRoom.GetPlayer(actorNum));
            }
        }
    }

    private int GetNextTurn()
    {
        int numPlayers = playerPMs.Count;
        Debug.Log(currentPlayerTurn + "vs" + numPlayers);
        if (currentPlayerTurn < numPlayers)
        {
            return currentPlayerTurn + 1;
        } else
        {
            return 1;
        }
    }

    public void MoveDrawPile()
    {
        for (int i = 0; i < drawPile.Count; i++)
        {
            drawPile[i].photonView.RPC("SetCardProps", RpcTarget.All, new Vector3(-1.5f, 0, 0), false, 0, "drawpile");
        }
    }

    [PunRPC]
    public void SyncGameState(string state)
    {
        if (state == "playing")
        {
            gameState = eGolfGameState.playing;
        }

        if (state == "swapping")
        {
            gameState = eGolfGameState.swapping;
        }
    }

    //flip that, that that that that that that
    private void FlipCards()
    {
        if (PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            print("flipping hand");
            for (int i = 0; i < playerViews.Count; i++)
            {
                playerViews[i].RPC("ResetHand", RpcTarget.All);
            }
        }
    }

    //set an individual player's state
    private void SetPlayerState(PhotonView p, string s)
    {
        p.RPC("SyncPlayerState", RpcTarget.All, s);
    }

    //set all players state to same thing
    private void ChangePlayerStates(string pState)
    {
        if (PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            print("changing player states to " + pState);
            for (int i = 0; i < playerViews.Count; i++)
            {
                playerViews[i].RPC("SyncPlayerState", RpcTarget.All, pState);
            }
        }
    }

    //only Master Client accesses this
    //instantiates deck prefab, shuffles the deck, and creates the draw pile
    private void CreateDeck()
    {
        deckGO = PhotonNetwork.InstantiateRoomObject("Deck", new Vector3(0, 0, 0), Quaternion.identity);
        deck = deckGO.GetComponent<Deck>();
        deckCards = deck.GetCards(deckGO);
        Deck.Shuffle(ref deckCards);
        drawPile = ConvertListCardsToListCardGolfs(deckCards);
    }

    //every client other than master client calls this
    //rotates camera so that each client's hand is at the bottom of the screen
    private void RotateCamera()
    {
        for (int i = 1; i < PhotonNetwork.PlayerList.Length; i++)
        {
            if (((i == 1 && PhotonNetwork.PlayerList.Length == 2) || i == 2) && (PhotonNetwork.LocalPlayer == PhotonNetwork.PlayerList[i]))
            {
                Camera.main.transform.Rotate(0, 0, -180);
                canvas.transform.Rotate(0, 0, -180);
            }
            else if ((i == 1 && PhotonNetwork.PlayerList.Length > 2) && (PhotonNetwork.LocalPlayer == PhotonNetwork.PlayerList[i]))
            {
                Camera.main.transform.Rotate(0, 0, -90);
                canvas.transform.Rotate(0, 0, -90);
            }
            else if (i == 3 && PhotonNetwork.LocalPlayer == PhotonNetwork.PlayerList[i])
            {
                Camera.main.transform.Rotate(0, 0, 90);
                canvas.transform.Rotate(0, 0, 90);
            }
        }
    }

    //only MasterClient accesses this method
    //go through each player gameObject and set the PlayerScript fields, then sync them to other clients
    private void SetPlayers()
    {
        for (int i = 0; i < playerGOs.Count; i++)
        {
            playerPMs[i].player = PhotonNetwork.PlayerList[i];
            playerPMs[i].playerName = PhotonNetwork.PlayerList[i].NickName;
            playerPMs[i].actorNum = PhotonNetwork.PlayerList[i].ActorNumber;

            playerViews[i].RPC("SyncPlayer", RpcTarget.All, playerPMs[i].playerName, playerPMs[i].player, playerPMs[i].actorNum);
            //RPC in PlayerScript
        }
        ChangePlayerStates("peaking");
    }

    //only MasterClient accesses this
    //go through each player gameObject, and depending on which number player it is and how many
    //players are playing, position their hand accordingly on the screen
    private void PositionCards()
    {
        for (int i = 0; i < playerGOs.Count; i++)
        {
            if (i == 0)
            {
                for (int j = 0; j < 4; j++)
                {
                    playerViews[i].RPC("SyncHandPOS", RpcTarget.All, localPositions);
                }
            } else if ((i == 1 && PhotonNetwork.PlayerList.Length == 2) || i == 2)
            {
                for (int j = 0; j < 4; j++)
                {
                    playerViews[i].RPC("SyncHandPOS", RpcTarget.All, oppPositions);
                    playerViews[i].RPC("RotateCards", RpcTarget.All, Quaternion.Euler(0, 0, 180));
                }
            } else if ((i == 1 && PhotonNetwork.PlayerList.Length > 2))
            {
                for (int j = 0; j < 4; j++)
                {
                    playerViews[i].RPC("SyncHandPOS", RpcTarget.All, leftPositions);
                    playerViews[i].RPC("RotateCards", RpcTarget.All, Quaternion.Euler(0, 0, -90));
                }
            } else if (i == 3)
            {
                for (int j = 0; j < 4; j++)
                {
                    playerViews[i].RPC("SyncHandPOS", RpcTarget.All, rightPositions);
                    playerViews[i].RPC("RotateCards", RpcTarget.All, Quaternion.Euler(0, 0, 90));
                }
            }
        }
    }

    //Converts CardGolf[] to string[]
    //in order to sync each PlayerScript's hand accross the network, we need to send the hand in 
    //the appropriate type. We can't send an array of gameObjects, so we will change them into an
    //array of their string names, and then convert them back to cards afterwards
    private string[] ConvertHandToStrings(CardGolf[] hand)
    {
        string[] stringHand = new string[4];
        for (int i = 0; i < hand.Length; i++)
        {
            stringHand[i] = hand[i].GetComponent<CardGolf>().suit + hand[i].GetComponent<CardGolf>().rank;
        }
        return stringHand;
    }

    //only the MasterClient accesses this
    //draw 4 cards and add them to each player's hand
    //sync hands accross network
    private void DealCards()
    {
        for (int i = 0; i < playerGOs.Count; i++)
        {
            CardGolf[] newHand = new CardGolf[4];
            for (int j = 0; j < 4; j++)
            {
                CardGolf card = Draw();
                
                newHand[j] = card;
            }
            string[] newStringHand = ConvertHandToStrings(newHand);
            playerViews[i].RPC("SyncHand", RpcTarget.All, newStringHand);
        }
    }

    //we use this at the beginning when putting cards into the deck
    List<CardGolf> ConvertListCardsToListCardGolfs(List<Card> lCD)
    {
        List<CardGolf> lCP = new List<CardGolf>();
        CardGolf tCP;
        foreach (Card tCD in lCD)
        {
            tCP = tCD as CardGolf;
            lCP.Add(tCP);
        }
        return (lCP);
    }

    // the draw function will pull a single card from the drawPile and return it
    public CardGolf Draw()
    {
        CardGolf cd = drawPile[drawPile.Count - 1]; 
        drawPile.RemoveAt(drawPile.Count - 1);
        return (cd);
    }

    public CardGolf DrawFromDiscard()
    {
        CardGolf cd = discardPile[discardPile.Count - 1];
        discardPile.RemoveAt(discardPile.Count - 1);
        return (cd);
    }

    public void CardClicked(CardGolf cd)
    {
        // the reaction is determined by the state of the clicked card,
        // the game state, and the player state
        switch (cd.state)
        {
            case eGolfCardState.hand:
                if (cd.GetComponent<PhotonView>().IsMine && gameState == eGolfGameState.peaking)
                {
                    if (peakCount < maxPeaks)
                    {
                        if (cd.faceUp)
                        {
                            cd.faceUp = false;
                        } else
                        {
                            cd.faceUp = true;
                            peakCount++;
                        }
                    }
                }

                if (cd.GetComponent<PhotonView>().IsMine && gameState == eGolfGameState.swapping)
                {
                    SendOnDiscardEvent(cd.name);
                    SyncGameState("playing");
                }
                break;

            case eGolfCardState.drawpile:
                if (cd.GetComponent<PhotonView>().IsMine && gameState == eGolfGameState.playing && !disableClicks)
                {
                    SendOnDrawEvent("drawpile");
                    disableClicks = true;
                }
                break;

            case eGolfCardState.discard:
                if (cd.GetComponent<PhotonView>().IsMine && gameState == eGolfGameState.playing && !disableClicks)
                {
                    SendOnDrawEvent("discardpile");
                    disableClicks = true;
                }
                break;
        }
    }

    private void SendOnDrawEvent(string pileName)
    {
        RaiseEventOptions raiseEventOptions = new() { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(onDrawEventCode, pileName, raiseEventOptions, SendOptions.SendReliable);
        Debug.Log("raise event called");
    }

    public void SendOnDiscardEvent(string cardName)
    {
        RaiseEventOptions raiseEventOptions = new() { Receivers = ReceiverGroup.MasterClient };
        PhotonNetwork.RaiseEvent(onDiscardEventCode, cardName, raiseEventOptions, SendOptions.SendReliable);
        Debug.Log("raise event called");
    }

    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.NetworkingClient.EventReceived += OnDrawEvent;
        PhotonNetwork.NetworkingClient.EventReceived += OnDiscardEvent;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.NetworkingClient.EventReceived -= OnDrawEvent;
        PhotonNetwork.NetworkingClient.EventReceived -= OnDiscardEvent;
    }

    private void OnDiscardEvent(EventData photonEvent)
    {
        int nextTurn = GetNextTurn();
        byte eventCode = photonEvent.Code;
        if (eventCode == onDiscardEventCode && PhotonNetwork.IsMasterClient)
        {
            string data = photonEvent.CustomData.ToString();
            if (data != "hi")
            {
                MoveToDiscard(ConvertStringToCard(data));
            } else
            {
                MoveToDiscard(target);
            }
            UpdateDiscardPile();

            for (int i = 0; i < playerPMs.Count; i++)
            {
                if (currentPlayerTurn == playerPMs[i].actorNum)
                {
                    SetPlayerState(playerPMs[i].photonView, "waiting");
                    Camera.main.gameObject.GetPhotonView().RPC(nameof(AssignTurn), RpcTarget.All, nextTurn);
                }
            }
        }
    }

    private void OnDrawEvent(EventData photonEvent)
    {
        byte eventCode = photonEvent.Code;
        if (eventCode == onDrawEventCode && PhotonNetwork.IsMasterClient)
        {
            string data = photonEvent.CustomData.ToString();
            print(data);

            for (int i = 0; i < playerPMs.Count; i++)
            {
                if (playerPMs[i].actorNum == currentPlayerTurn)
                {
                    if (data == "drawpile")
                    {
                        SetPlayerState(playerPMs[i].photonView, "deciding");
                        MoveToTarget(Draw());
                        UpdateDrawPile();
                    } else if (data == "discardpile")
                    {
                        SetPlayerState(playerPMs[i].photonView, "swapping");
                        SyncGameState("swapping");
                        MoveToTarget(DrawFromDiscard());
                        UpdateDiscardPile();
                    }
                }
            }
        }
    }

    void MoveToDiscard(CardGolf cd)
    {
        discardPile.Add(cd);
        
        //set state of card to discard
        cd.gameObject.GetPhotonView().RPC("SetCardState", RpcTarget.All, "discard");

        int sortOrder = 100 + discardPile.Count;

        // position this card on the discardPile
        cd.photonView.RPC("SetCardProps", RpcTarget.All, new Vector3(1.5f, 0, 0), true, sortOrder, "discardpile");
    }

    public void SwapTarget()
    {
        if (PhotonNetwork.LocalPlayer.IsMasterClient)
        {

        }
    }

    [PunRPC]
    private void SetTargetCard(string cardName)
    {
        CardGolf targetCard = ConvertStringToCard(cardName);
        target = targetCard;
    }

    public CardGolf ConvertStringToCard(string name)
    {
        return GameObject.Find(name).GetComponent<CardGolf>();
    }

    void MoveToTarget(CardGolf cd)
    {
        cd.photonView.RPC("SetCardProps", PhotonNetwork.CurrentRoom.GetPlayer(currentPlayerTurn), new Vector3(0, 0, 0), true, 300, "target");
        this.photonView.RPC(nameof(SetTargetCard), RpcTarget.All, cd.name);
        cd.photonView.RPC("SetCardState", RpcTarget.All, "target");
    }

    // arranges all the cards of the drawPile to show how many are left
    void UpdateDrawPile()
    {
        print("updating draw pile");
        CardGolf cd;
        // go through all the cards of the drawPile
        for (int i = 0; i < drawPile.Count; i++)
        {
            int sortOrder = 10 * i;
            print("sortOrder: " + sortOrder);
            cd = drawPile[i];
            cd.photonView.RPC("SetCardState", RpcTarget.All, "drawpile");
            cd.photonView.RPC("SetCardProps", RpcTarget.All, new Vector3(-1.5f, 0, 0), false, sortOrder, "drawpile");
        }
    }

    void UpdateDiscardPile()
    {
        print("updating discard pile");
        CardGolf cd;
        // go through all cards of discardpile
        for (int i = 0; i < discardPile.Count; i++)
        {
            int sortOrder = 10 * i;
            cd = discardPile[i];
            cd.photonView.RPC("SetCardState", RpcTarget.All, "discardpile");
            cd.photonView.RPC("SetCardProps", RpcTarget.All, new Vector3(1.5f, 0, 0), true, sortOrder, "discardpile");
        }
    }

    private void Update()
    {
        
    }

   
}