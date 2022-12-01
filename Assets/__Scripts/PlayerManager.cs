using Photon.Pun.Demo.Cockpit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
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
    }
}
