using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PreGameUI : MonoBehaviourPunCallbacks
{
    public TextMeshProUGUI numPlayers;
    public GameObject playerNames;

    private Hashtable custProps;
    private int num = 0;
    private PhotonView view;
    private bool gameStarted = false;

    private void Awake()
    {
        custProps = new Hashtable();
        custProps.Add("isReady", "false");
        PhotonNetwork.LocalPlayer.SetCustomProperties(custProps);

        numPlayers.text = PhotonNetwork.PlayerList.Length.ToString();

        ListPlayers();
    }

    private void Update()
    {
        numPlayers.text = PhotonNetwork.PlayerList.Length.ToString();

        if (num < PhotonNetwork.PlayerList.Length)
        {
            //player joins
            num = PhotonNetwork.PlayerList.Length;
            ListPlayers();
            SetView();
        } else if (num > PhotonNetwork.PlayerList.Length)
        {
            //player disconnects
            num = PhotonNetwork.PlayerList.Length;
            ClearList();
            ListPlayers();
            SetView();
        }

        CheckForGameStart();
    }

    private void ClearList()
    {
        for (int i = 0; i < playerNames.transform.childCount; i++)
        {
            playerNames.transform.GetChild(i).GetChild(0).GetComponent<TextMeshProUGUI>().text = "";
            playerNames.transform.GetChild(i).GetChild(1).GetComponent<TextMeshProUGUI>().text = "";
        }
    }

    private void ListPlayers()
    {
        for (int i = 0; i < num; i++)
        {
            playerNames.transform.GetChild(i).GetChild(0).GetComponent<TextMeshProUGUI>().text = PhotonNetwork.PlayerList[i].NickName;
            playerNames.transform.GetChild(i).GetChild(1).GetComponent<TextMeshProUGUI>().text = "";
        }
    }

    private void SetView()
    {
        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
        {
            if (PhotonNetwork.PlayerList[i].NickName == PhotonNetwork.NickName)
            {
                view = PhotonView.Find(i + 1);
            }
        }
    }

    private void CheckForGameStart()
    {
        int count = 0;
        for (int i = 0; i < num; i++)
        {
            if (PhotonNetwork.PlayerList[i].CustomProperties["isReady"].ToString() == "true")
            {
                count++;
            }
        }

        if (count == num && num > 1 && gameStarted == false)
        {
            gameStarted = true;
            StartGame();
        }
    }

    public void StartGame()
    {
        PhotonNetwork.LoadLevel("Game");
    }

    public void ReadyUp()
    {
        custProps["isReady"] = "true";
        PhotonNetwork.LocalPlayer.SetCustomProperties(custProps);
        view.RPC("SyncReady", RpcTarget.All);
        CheckForGameStart();
    }

    public void ReturnToLobby()
    {
        PhotonNetwork.Disconnect();
        PhotonNetwork.LoadLevel("Loading");
    }
}
