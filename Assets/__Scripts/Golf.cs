using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;


public class Golf : MonoBehaviourPunCallbacks
{
    public TextAsset deckXML;

    public GameObject deckGO;
    public Deck deck;
    public List<Card> deckCards;
    public List<CardGolf> drawPile;
    public List<CardGolf> discardPile;

    private List<GameObject> playerGOs;
    private List<PlayerScript> playerPMs;
    private List<PhotonView> playerViews;

    private Vector3[] localPositions = new Vector3[] { new Vector3(-2f, -6f, 0), new Vector3(2f, -6f, 0), new Vector3(-2f, -10.5f, 0), new Vector3(2f, -10.5f, 0) };
    private Vector3[] oppPositions = new Vector3[] { new Vector3(2, 6, 0), new Vector3(-2, 6, 0), new Vector3(2, 10.5f, 0), new Vector3(-2, 10.5f, 0) };
    private Vector3[] leftPositions = new Vector3[] { new Vector3(-6, 2, 0), new Vector3(-6, -2, 0), new Vector3(-10.5f, 2, 0), new Vector3(-10.5f, -2, 0) };
    private Vector3[] rightPositions = new Vector3[] { new Vector3(6, -2, 0), new Vector3(6, 2, 0), new Vector3(10.5f, -2, 0), new Vector3(10.5f, 2, 0) };


    private void Awake()
    {
        playerGOs = new List<GameObject>();
        playerPMs = new List<PlayerScript>();
        playerViews = new List<PhotonView>();
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
            RotateCamera();
        }
    }

    private void CreateDeck()
    {
        deckGO = PhotonNetwork.InstantiateRoomObject("Deck", new Vector3(0, 0, 0), Quaternion.identity);
        deck = deckGO.GetComponent<Deck>();
        deckCards = deck.GetCards(deckGO);
        Deck.Shuffle(ref deckCards);
        drawPile = ConvertListCardsToListCardProspectors(deckCards);
    }

    private void RotateCamera()
    {
        for (int i = 1; i < PhotonNetwork.PlayerList.Length; i++)
        {
            if (((i == 1 && PhotonNetwork.PlayerList.Length == 2) || i == 2) && (PhotonNetwork.LocalPlayer == PhotonNetwork.PlayerList[i]))
            {
                Camera.main.transform.Rotate(0, 0, -180);
            }
            else if ((i == 1 && PhotonNetwork.PlayerList.Length > 2) && (PhotonNetwork.LocalPlayer == PhotonNetwork.PlayerList[i]))
            {
                Camera.main.transform.Rotate(0, 0, -90);
            }
            else if (i == 3 && PhotonNetwork.LocalPlayer == PhotonNetwork.PlayerList[i])
            {
                Camera.main.transform.Rotate(0, 0, 90);
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
            playerViews[i].RPC("SyncName", RpcTarget.Others, playerPMs[i].playerName);
            //RPC in PlayerScript
        }
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
                card.state = eGolfCardState.hand;
                newHand[j] = card;
            }
            string[] newStringHand = ConvertHandToStrings(newHand);
            playerViews[i].RPC("SyncHand", RpcTarget.All, newStringHand);
        }
        
    }

    //we use this at the beginning when putting cards into the deck
    List<CardGolf> ConvertListCardsToListCardProspectors(List<Card> lCD)
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
    CardGolf Draw()
    {
        CardGolf cd = drawPile[0]; 
        drawPile.RemoveAt(0);
        return (cd);
    }

    public void CardClicked(CardGolf cd)
    {
        // the reaction is determined by the state of the clicked card
        switch (cd.state)
        {
            case eGolfCardState.hand:
                if (cd.GetComponent<PhotonView>().IsMine)
                {
                    if (cd.faceUp)
                    {
                        cd.faceUp = false;
                    } else
                    {
                        cd.faceUp = true;
                    }
                }
                break;

            case eGolfCardState.drawpile:
                //MoveToDiscard(target);
                //MoveToTarget(Draw());
                //UpdateDrawPile();
                break;

            case eGolfCardState.discard:
                
                break;
        }
    }
}