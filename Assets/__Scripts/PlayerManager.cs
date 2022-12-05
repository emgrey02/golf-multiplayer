using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviourPunCallbacks
{
    public static PlayerManager S;
    public List<PlayerScript> players = new List<PlayerScript> ();
    public int numPlayers = 0;

    private void Awake()
    {
        S = this;
    }
    public void AddPlayer(PlayerScript p)
    {
        players.Add(p);
        numPlayers++;
        //this.photonView.RPC(nameof(SyncPlayerList), RpcTarget.All, p.playerName);
    }

    [PunRPC]
    public void SyncPlayerList(string p)
    {
        if (PhotonNetwork.LocalPlayer.NickName == p)
        {
            players.Add(GameObject.Find("Player(Clone)").GetComponent<PlayerScript>());
        }
    }
}
