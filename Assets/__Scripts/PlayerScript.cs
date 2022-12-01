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
    public GameObject playerGO;
    public bool myTurn;

    public eGolfPlayerState state = eGolfPlayerState.waiting;
    
    private void Awake()
    {
        hand = new CardGolf[4];
        player = PhotonNetwork.LocalPlayer;
        playerGO = this.gameObject;
        playerName = "";
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

            CardGolf cardScript = this.hand[i].GetComponent<CardGolf>();
            cardScript.state = eGolfCardState.hand;
        }
    }

    [PunRPC]
    public void SyncName(string name)
    {
        this.playerName = name;
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

    public CardGolf ConvertStringToCard(string name)
    {
        print(name);
        return GameObject.Find(name).GetComponent<CardGolf>();
    }

    void Update()
    {
       
    }
}
