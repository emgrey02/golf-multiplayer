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
    public GameObject deckGO;
    public Deck deck;

    public List<Card> deckCards;
    public List<CardGolf> drawPile;
    public List<CardGolf> discardPile;
    public List<CardGolf> handCards;
    public eGolfGameState gameState = eGolfGameState.setup;

    public Scoreboard scoreboard;
    public GameObject canvas;

    public CardGolf target;

    public int currentPlayerTurn;

    public static int ROUND_NUM;

    public bool lastTurns;

    private List<GameObject> playerGOs;
    private List<PlayerScript> playerPMs;
    public List<string> playerNames;

    public delegate void OnPositionChange(CardGolf cd, Vector3 newPos);
    OnPositionChange moveCard;

    private const int maxPeaks = 2;
    public int peakCount;

    private bool disableClicks;

    public const byte onDrawEventCode = 1;
    public const byte onDiscardEventCode = 2;
    public const byte onKnockEventCode = 3;

    private Vector3[] localPositions = new Vector3[] { new Vector3(-2f, -6f, 0), new Vector3(2f, -6f, 0), new Vector3(-2f, -10.5f, 0), new Vector3(2f, -10.5f, 0) };
    private Vector3[] oppPositions = new Vector3[] { new Vector3(2, 6, 0), new Vector3(-2, 6, 0), new Vector3(2, 10.5f, 0), new Vector3(-2, 10.5f, 0) };
    private Vector3[] leftPositions = new Vector3[] { new Vector3(-6, 2, 0), new Vector3(-6, -2, 0), new Vector3(-10.5f, 2, 0), new Vector3(-10.5f, -2, 0) };
    private Vector3[] rightPositions = new Vector3[] { new Vector3(6, -2, 0), new Vector3(6, 2, 0), new Vector3(10.5f, -2, 0), new Vector3(10.5f, 2, 0) };

    private Quaternion[] cardRots = new Quaternion[] { Quaternion.identity, Quaternion.Euler(0, 0, 180), Quaternion.Euler(0, 0, -90), Quaternion.Euler(0, 0, 90) };


    private void Awake()
    {
        this.photonView.RPC("UpdateFieldsOnNewRound", RpcTarget.All);
        playerGOs = new List<GameObject>();
        playerPMs = new List<PlayerScript>();
        playerNames = new List<string>();
    }

    private void Start()
    {
        moveCard = MoveThatCard;
        //MasterClient will spawn Players to Network, set proper ownership
        //instantiate deck, shuffle them, create the draw pile, 
        // set player props, deal the cards, and position the cards on the screen
        if (PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            this.photonView.RPC("IncreaseRoundVar", RpcTarget.All);

            if (ROUND_NUM == 1) 
            {
                for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
                {
                    GameObject player = PhotonNetwork.Instantiate("Player", new Vector3(0, 0, 0), Quaternion.identity);
                    PlayerScript PM = player.GetComponent<PlayerScript>();
                    DontDestroyOnLoad(player);

                    playerGOs.Add(player);
                    playerPMs.Add(PM);

                    PM.photonView.TransferOwnership(PhotonNetwork.PlayerList[i]);
                }
            } else 
            {
                for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
                {
                    GameObject player = PhotonView.Find(1000 + (i + 1)).gameObject;
                    PlayerScript PM = player.GetComponent<PlayerScript>();

                    playerGOs.Add(player);
                    playerPMs.Add(PM);

                    //PM.photonView.TransferOwnership(PhotonNetwork.PlayerList[i]);
                }
            }

            CreateDeck();
            SetPlayers();
            DealCards();
            PositionCards();

            //starts first stage of game
            SetGameState("peaking");
            ChangePlayerStates("peaking");
        } else
        {
            //everyone else's camera rotates so their own hand is in front of them
            RotateCamera();
        }
    }

    [PunRPC]
    public void IncreaseRoundVar() 
    {
        ROUND_NUM++;
        scoreboard.RoundNum = ROUND_NUM;
    }

    public void StartNextRound() 
    {
        if (PhotonNetwork.IsMasterClient) 
        {
            LoadNextRound();
        }
    }

    [PunRPC]
    public void UpdateFieldsOnNewRound()
    {
        this.lastTurns = false;
        this.disableClicks = false;
        this.peakCount = 0;
    }


    //RPC
    //syncs the lastTurns field on every client
    [PunRPC]
    public void SetLastTurns(bool v) {
        this.lastTurns = v;
    }

    //calls the coroutine when a card is moved so it can animate
    //move! that! card!
    public void MoveThatCard(CardGolf cd, Vector3 pos) 
    {
        StartCoroutine(MoveCard(cd, pos));
    }

    //this is the coroutine called when a card is moved
    System.Collections.IEnumerator MoveCard(CardGolf cd, Vector3 p) 
    {
        float elapsedTime = 0f;
        float moveTime = .5f;
        Vector3 currentPos = cd.transform.localPosition;

        while (elapsedTime < moveTime) 
        {
            cd.transform.position = Vector3.Lerp(currentPos, p, (elapsedTime / moveTime));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        cd.transform.localPosition = p;
        yield return null;
    }

    public void ResetCustProps() {
        print("resetting custom props");
        Hashtable custProps = new Hashtable();
        custProps.Add("Peak", "not done");

        Hashtable roomCustProps = new Hashtable();
        roomCustProps.Add("currentTurn", 0.ToString());
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomCustProps);

        for (int i = 0; i < playerPMs.Count; i++) {
            PhotonNetwork.PlayerList[i].SetCustomProperties(custProps);
        }
    }

    //this method runs when a player presses the "I'm done peaking" button
    //sets the player's custom prop
    public void SetPeakCustProp()
    {
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
            CheckForEndPeakingState();
        }
    }

    //this is called when the only room custom prop changes - the current turn
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

    public void LoadNextRound()
    {
            PhotonNetwork.LoadLevel("Game");
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
            object PeakProp = PhotonNetwork.PlayerList[i].CustomProperties["Peak"];
            if (PeakProp != null) {
                string peakProp = PeakProp.ToString();
                if (peakProp == "done")
                {
                    count++;
                    playersDonePeaking.Add(playerPMs[i]);
                }
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
            FlipCards(false);
            ChangePlayerStates("waiting");
            SetGameState("playing");
            UpdateDrawPile();
            MoveToDiscard(Draw());
            this.photonView.RPC(nameof(AssignTurn), RpcTarget.All, playerPMs[0].actorNum);
        }
    }

    //RPC
    //assigns the currentPlayerTurn variable on every client
    //then sets a custom prop on this room that tells the current player turn
    [PunRPC]
    public void AssignTurn(int actorNum)
    {
        print("assigning turn");
        currentPlayerTurn = actorNum;

        Hashtable custProps = new Hashtable();
        custProps.Add("currentTurn", actorNum.ToString());
        PhotonNetwork.CurrentRoom.SetCustomProperties(custProps);

        //reset this variable
        disableClicks = false;
    }

    //get and return the actor number of the next player's turn
    private int GetNextTurn()
    {
        int numPlayers = playerPMs.Count;
        if (currentPlayerTurn < numPlayers)
        {
            return currentPlayerTurn + 1;
        } else
        {
            return 1;
        }
    }

    //RPC
    //sets the gameState and syncs it across every client
    //if it's swapping, it also sets the player's state to swapping as well
    [PunRPC]
    public void SyncGameState(string state)
    {
        print("changing game state to " + state);

        if (state == "setup") 
        {
            gameState = eGolfGameState.setup;
        }
        if (state == "peaking") 
        {
            gameState = eGolfGameState.peaking;
        }
        if (state == "playing")
        {
            gameState = eGolfGameState.playing;
        }

        if (state == "swapping")
        {
            gameState = eGolfGameState.swapping;

            if (PhotonNetwork.IsMasterClient) 
            {
                for (int i = 0; i < playerPMs.Count; i++) {
                    if (currentPlayerTurn == playerPMs[i].actorNum) {
                        SetPlayerState(playerPMs[i].photonView, "swapping");
                    }
                }
            }
        }

        if (state == "roundover") {
            gameState = eGolfGameState.roundover;
        }
    }

    //sets the game state
    //calls an RPC
    public void SetGameState(string s) {
        this.photonView.RPC("SyncGameState", RpcTarget.All, s);
    }

    //flips everyone's cards face up or face down
    //calls RPC
    //flip that, that that that that that that
    public void FlipCards(bool fu)
    {
        if (PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            print("flipping hand");
            for (int i = 0; i < playerPMs.Count; i++)
            {
                playerPMs[i].photonView.RPC("FlipHand", RpcTarget.All, fu);
            }
        }
    }

    //set an individual player's state
    //calls RPC
    private void SetPlayerState(PhotonView p, string s)
    {
        p.RPC("SyncPlayerState", RpcTarget.All, s);
    }

    //set all players state to same thing
    //calls RPC
    private void ChangePlayerStates(string pState)
    {
        if (PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            for (int i = 0; i < playerPMs.Count; i++)
            {
                SetPlayerState(playerPMs[i].photonView, pState);
            }
        }
    }

    //only Master Client accesses this
    //instantiates deck prefab, shuffles the deck, and creates the draw pile
    private void CreateDeck()
    {
        if (ROUND_NUM == 1) {
            deckGO = PhotonNetwork.Instantiate("Deck", new Vector3(0, 0, 0), Quaternion.identity);
            DontDestroyOnLoad(deckGO);
        } else {
            deckGO = PhotonView.Find(1003).gameObject;
        }
        deck = GetComponent<Deck>();
        deckCards = deck.GetCards(deckGO);
        Deck.Shuffle(ref deckCards);
        drawPile = ConvertListCardsToListCardGolfs(deckCards);
        UpdateDrawPile();
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

    //only MasterClient calls this
    //go through each player gameObject and set the PlayerScript fields, then sync them 
    private void SetPlayers()
    {
        for (int i = 0; i < playerGOs.Count; i++)
        {
            playerPMs[i].player = PhotonNetwork.PlayerList[i];
            playerPMs[i].playerName = PhotonNetwork.PlayerList[i].NickName;
            playerPMs[i].actorNum = PhotonNetwork.PlayerList[i].ActorNumber;
            playerNames.Add(playerPMs[i].playerName);

            playerPMs[i].photonView.RPC("SyncPlayer", RpcTarget.All, playerPMs[i].playerName, playerPMs[i].player, playerPMs[i].actorNum);
            //RPC in PlayerScript

            if (ROUND_NUM > 1) 
            {
                playerPMs[i].photonView.RPC("ResetPlayerFields", RpcTarget.All);
            }

            //populate scoreboard
            scoreboard.photonView.RPC("SetPlayerNames", RpcTarget.All, i, playerNames[i]);
            scoreboard.photonView.RPC("UpdateScoreBoard", RpcTarget.All, i);
        }

        
    }


    //only MasterClient calls this
    //go through each player gameObject, and depending on which number player it is and how many
    //players are playing, position their hand accordingly on the screen
    private void PositionCards()
    {
        for (int i = 0; i < playerPMs.Count; i++)
        {
            if (i == 0)
            {
                playerPMs[i].PositionHand(localPositions);

            }
            else if ((i == 1 && PhotonNetwork.PlayerList.Length == 2) || i == 2)
            {
                playerPMs[i].PositionHand(oppPositions);
                playerPMs[i].RotateHand(cardRots[1]);
            }
            else if ((i == 1 && PhotonNetwork.PlayerList.Length > 2))
            {
                playerPMs[i].PositionHand(leftPositions);
                playerPMs[i].RotateHand(cardRots[2]);
            }
            else if (i == 3)
            {
                playerPMs[i].PositionHand(rightPositions);
                playerPMs[i].RotateHand(cardRots[3]);
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
    //sync across network
    //calls RPC
    private void DealCards()
    {
        for (int i = 0; i < playerPMs.Count; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                CardGolf card = Draw();
                playerPMs[i].hand[j] = card;
            }
            string[] newStringHand = ConvertHandToStrings(playerPMs[i].hand);
            playerPMs[i].photonView.RPC("SyncHand", RpcTarget.All, newStringHand);
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

    //draws a single card from the drawPile and returns it
    public CardGolf Draw()
    {
        CardGolf cd = drawPile[drawPile.Count - 1]; 
        drawPile.RemoveAt(drawPile.Count - 1);
        return (cd);
    }

    //draws a card from the discard pile and returns it
    public CardGolf DrawFromDiscard()
    {
        CardGolf cd = discardPile[discardPile.Count - 1];
        discardPile.RemoveAt(discardPile.Count - 1);
        return (cd);
    }

    //called when card is clicked
    public void CardClicked(CardGolf cd)
    {
        // the reaction is determined by the state of the clicked card,
        // the game state, and the player state
        switch (cd.state)
        {
            case eGolfCardState.hand:
                //game state is peaking and this card is owned by the player who clicked it
                if (gameState == eGolfGameState.peaking && cd.owner == PhotonNetwork.LocalPlayer.ActorNumber)
                {   
                    //only 2 peaks allowed
                    if (peakCount < maxPeaks)
                    {
                        if (!cd.faceUp)
                        {
                            cd.faceUp = true;
                            peakCount++;
                        }
                    }
                }

                //swapping with a card in our hand
                //discards the card in our hand
                //within that method, the target card is added to our hand
                if (gameState == eGolfGameState.swapping && cd.owner == PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    SendOnDiscardEvent(cd.name);
                    //SetGameState("playing");
                }
                break;

            case eGolfCardState.drawpile:
                //if the current player is me, it's my turn, and I haven't clicked a card yet
                //draws the card from the drawpile
                //disables ability to click on another card this turn
                if (currentPlayerTurn == PhotonNetwork.LocalPlayer.ActorNumber && gameState == eGolfGameState.playing && !disableClicks) {
                    SendOnDrawEvent("drawpile");
                    disableClicks = true;
                }
                break;

            case eGolfCardState.discard:
                //if the current player is me, it's my turn, and I haven't clicked a card yet
                //draws the card from the drawpile
                //disables ability to click on another card this turn
                if (currentPlayerTurn == PhotonNetwork.LocalPlayer.ActorNumber && gameState == eGolfGameState.playing && !disableClicks)
                {
                    SendOnDrawEvent("discardpile");
                    disableClicks = true;
                }
                break;
        }
    }

    //Event that is called when someone clicks on a card from the discard or draw pile
    private void SendOnDrawEvent(string pileName)
    {
        RaiseEventOptions raiseEventOptions = new() { Receivers = ReceiverGroup.MasterClient };
        PhotonNetwork.RaiseEvent(onDrawEventCode, pileName, raiseEventOptions, SendOptions.SendReliable);
        Debug.Log("draw event called");
    }

    //Event that's called when client either presses the discard button after drawing a card
    //from the draw pile, or after swapping the target card with a card in their hand
    public void SendOnDiscardEvent(string cardName)
    {
        RaiseEventOptions raiseEventOptions = new() { Receivers = ReceiverGroup.MasterClient };
        PhotonNetwork.RaiseEvent(onDiscardEventCode, cardName, raiseEventOptions, SendOptions.SendReliable);
        Debug.Log("discard event called");
    }

    //Event that's called when client presses the knock button
    public void SendOnKnockEvent()
    {
        RaiseEventOptions raiseEventOptions = new() { Receivers = ReceiverGroup.MasterClient };
        PhotonNetwork.RaiseEvent(onKnockEventCode, null, raiseEventOptions, SendOptions.SendReliable);
        Debug.Log("knock event called");
    }

    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.NetworkingClient.EventReceived += OnDrawEvent;
        PhotonNetwork.NetworkingClient.EventReceived += OnDiscardEvent;
        PhotonNetwork.NetworkingClient.EventReceived += OnKnockEvent;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.NetworkingClient.EventReceived -= OnDrawEvent;
        PhotonNetwork.NetworkingClient.EventReceived -= OnDiscardEvent;
        PhotonNetwork.NetworkingClient.EventReceived -= OnKnockEvent;
    }

    //handles discards
    private void OnDiscardEvent(EventData photonEvent)
    {
        byte eventCode = photonEvent.Code;
        if (eventCode == onDiscardEventCode)
        {
            string data = photonEvent.CustomData.ToString();

            //if we're discarding a card from our hand (swapping)
            if (data != "")
            {
                CardGolf cd = ConvertStringToCard(data);
                AddTargetToHand(cd);
                MoveToDiscard(cd);
                cd.photonView.RPC("SetOwner", RpcTarget.All, 0);
            } else
            {
                //discarding the target card
                MoveToDiscard(target);
            }
            UpdateDiscardPile();

            int nextTurn = GetNextTurn();
        
            //set new player state and assign next turn
            for (int i = 0; i < playerPMs.Count; i++)
            {
                if (currentPlayerTurn == playerPMs[i].actorNum)
                {   
                    //if someone knocked
                    if (lastTurns) 
                    {   
                        //if last turn is done
                        if (CheckForRoundEnd(nextTurn)) {
                            SetGameState("roundover");
                            ChangePlayerStates("waiting");
                            
                            //so we can spawn button only on Master Client's screen
                            SetPlayerState(playerPMs[0].photonView, "playing");

                            //reset cust props for next round
                            ResetCustProps();

                            //round is over, tally scores
                            PostScores();
                        } else 
                        {
                            SetGameState("playing");
                            SetPlayerState(playerPMs[i].photonView, "waiting");
                            this.photonView.RPC(nameof(AssignTurn), RpcTarget.All, nextTurn);
                        }
                    } else 
                    {
                        SetGameState("playing");
                        SetPlayerState(playerPMs[i].photonView, "waiting");
                        this.photonView.RPC(nameof(AssignTurn), RpcTarget.All, nextTurn);
                    }
                }
            }
        }
    }

    private void PostScores() {
        for (int i = 0; i < playerPMs.Count; i++) {
            scoreboard.photonView.RPC("TallyScores", RpcTarget.All, playerPMs[i].playerName, ConvertHandToStrings(playerPMs[i].hand));
        }
    }

    //returns boolean whether round is over or not
    private bool CheckForRoundEnd(int nt)
    {
        bool roundEnd = false;

        for (int i = 0; i < playerPMs.Count; i++) {
            if (nt == playerPMs[i].actorNum && playerPMs[i].lastTurn) {
                print("round over!");
                roundEnd = true;
            }
        }
        return roundEnd;
    }

    //called when a player knocks
    //calls RPCs
    //syncs lastTurns variable, assigns next turn immediately (knocking is the whole turn)
    private void OnKnockEvent(EventData photonEvent) {
        byte eventCode = photonEvent.Code;
        if (eventCode == onKnockEventCode) 
        {
            int nextTurn = GetNextTurn();
            this.photonView.RPC(nameof(SetLastTurns), RpcTarget.All, true);
            this.photonView.RPC(nameof(AssignTurn), RpcTarget.All, nextTurn);
        }
    }

    //handles drawing a card
    private void OnDrawEvent(EventData photonEvent)
    {
        byte eventCode = photonEvent.Code;
        if (eventCode == onDrawEventCode)
        {
            string data = photonEvent.CustomData.ToString();
            print(data);

            for (int i = 0; i < playerPMs.Count; i++)
            {
                if (playerPMs[i].actorNum == currentPlayerTurn)
                {   
                    //if player drew from draw pile
                    if (data == "drawpile")
                    {
                        SetPlayerState(playerPMs[i].photonView, "deciding");
                        MoveToTarget(Draw());
                        UpdateDrawPile();

                    //if player drew from discard pile
                    //they have to swap
                    } else if (data == "discardpile")
                    {
                        SetPlayerState(playerPMs[i].photonView, "swapping");
                        SetGameState("swapping");
                        MoveToTarget(DrawFromDiscard());
                        UpdateDiscardPile();
                    }
                }
            }
        }
    }

    //Adds a card to the discard pile
    //calls RPCs to sync card state and props
    //updates discard pile at end
    void MoveToDiscard(CardGolf cd)
    {
        discardPile.Add(cd);
        
        cd.photonView.RPC("SetCardState", RpcTarget.All, "discard");
        cd.photonView.RPC("SetCardProps", RpcTarget.All, true, 100 + discardPile.Count, "discardpile");
        UpdateDiscardPile();
    }

    //adds target card to our hand in correct position
    //parameter is the card in our hand that we clicked on to swap with
    public void AddTargetToHand(CardGolf cd)
    {
        print("adding target to hand");
        //get position of card we clicked
        Vector3 cardPos = cd.gameObject.transform.localPosition;

        target.pos = cardPos;
        moveCard(target, cardPos);

        target.photonView.RPC("SetCardProps", RpcTarget.All, false, 0, "hand");
        target.photonView.RPC("SetCardState", RpcTarget.All, "hand");

        for (int i = 0; i < playerPMs.Count; i++) {
            if (playerPMs[i].actorNum == currentPlayerTurn) {
                print("swapping!!!");
                playerPMs[i].photonView.RPC("ReplaceCardFromHandWithTarget", RpcTarget.All, cd.name, target.name);
                playerPMs[i].photonView.RPC("SyncHand", RpcTarget.All, ConvertHandToStrings(playerPMs[i].hand));
            }
        }
    }

    //RPC
    //Sets the card we clicked from the discard/draw pile as the target card on every client
    [PunRPC]
    private void SetTargetCard(string cardName)
    {
        CardGolf targetCard = ConvertStringToCard(cardName);
        target = targetCard;
    }

    //converts the string of the name of a card to the CardGolf card itself
    public CardGolf ConvertStringToCard(string name)
    {
        return GameObject.Find(name).GetComponent<CardGolf>();
    }

    //Moves a card to the target position/rotation
    //calls RPCs
    //syncs card props (only on the current player turn)
    //syncs card state, and target field on every client
    void MoveToTarget(CardGolf cd)
    {
        Quaternion currentRot = Camera.main.transform.rotation;
        
        moveCard(cd, new Vector3(0, 0, 0));
        cd.photonView.transform.rotation = currentRot;

        cd.photonView.RPC("SetCardProps", PhotonNetwork.CurrentRoom.GetPlayer(currentPlayerTurn), true, 300, "target");
        this.photonView.RPC(nameof(SetTargetCard), RpcTarget.All, cd.name);
        cd.photonView.RPC("SetCardState", RpcTarget.All, "target");

    }

    //arranges all the cards of the drawPile
    //sets position
    //calls RPCs
    //card state,props, and owner synced on every client
    void UpdateDrawPile()
    {
        CardGolf cd;

        // go through all the cards of the drawPile
        for (int i = 0; i < drawPile.Count; i++)
        {
            int sortOrder = 10 * i;
            cd = drawPile[i];
            cd.photonView.RPC("SetCardState", RpcTarget.All, "drawpile");
            cd.photonView.transform.localPosition = new Vector3(-1.5f, 0, 0);
            cd.photonView.RPC("SetCardProps", RpcTarget.All, false, sortOrder, "drawpile");
            cd.photonView.RPC("SetOwner", RpcTarget.All, 0);
        }
    }

    //arranges all the cards of the discard pile
    //sets position
    //sets rotation
    //calls RPCs
    //card state, card props, card owner synced on every client
    void UpdateDiscardPile()
    {
        CardGolf cd;

        // go through all cards of discardpile
        for (int i = 0; i < discardPile.Count; i++)
        {
            int sortOrder = 10 * i;
            cd = discardPile[i];
            cd.photonView.RPC("SetCardState", RpcTarget.All, "discardpile");
            moveCard(cd, new Vector3(1.5f, 0, 0));
            cd.photonView.transform.rotation = Quaternion.identity;
            cd.photonView.RPC("SetCardProps", RpcTarget.All, true, sortOrder, "discardpile");
            cd.photonView.RPC("SetOwner", RpcTarget.All, 0);
        }
    }   
}