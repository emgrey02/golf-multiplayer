using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PlayerScript : MonoBehaviourPunCallbacks
{
    public CardGolf[] hand;
    public Player player;
    public string playerName;
    public GameObject playerGO;

    public Transform cameraTransform;
    
    public bool myTurn;

    public Transform playerPOS;

    
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
                print("before card owner: " + cardView.Owner);
                cardView.TransferOwnership(player);
                print("new card owner: " + cardView.Owner);
            }
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
