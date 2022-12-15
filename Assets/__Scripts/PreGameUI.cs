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
    private PhotonView readyView;
    private bool gameStarted = false;

    private void Awake()
    {
        custProps = new Hashtable();
        custProps.Add("isReady", "false");
        PhotonNetwork.LocalPlayer.SetCustomProperties(custProps);

        numPlayers.text = PhotonNetwork.PlayerList.Length.ToString();

        ListPlayers();
    }

    //updates list according to num player change
    private void Update()
    {
        numPlayers.text = PhotonNetwork.PlayerList.Length.ToString();

        if (num < PhotonNetwork.PlayerList.Length)
        {
            //player joins
            num = PhotonNetwork.PlayerList.Length;
            ListPlayers();
            SetReadyView();
        } else if (num > PhotonNetwork.PlayerList.Length)
        {
            //player disconnects
            num = PhotonNetwork.PlayerList.Length;
            ClearList();
            ListPlayers();
            SetReadyView();
        }

        CheckForGameStart();
    }

    //clears player list
    private void ClearList()
    {
        for (int i = 0; i < playerNames.transform.childCount; i++)
        {
            //playername text
            playerNames.transform.GetChild(i).GetChild(0).GetComponent<TextMeshProUGUI>().text = "";
            //ready text
            playerNames.transform.GetChild(i).GetChild(1).GetComponent<TextMeshProUGUI>().text = "";
        }
    }

    //lists players
    private void ListPlayers()
    {
        for (int i = 0; i < num; i++)
        {
            //playername text
            playerNames.transform.GetChild(i).GetChild(0).GetComponent<TextMeshProUGUI>().text = PhotonNetwork.PlayerList[i].NickName;
            //ready text
            playerNames.transform.GetChild(i).GetChild(1).GetComponent<TextMeshProUGUI>().text = "";
        }
    }

    //sets correct ready GO for each client
    private void SetReadyView()
    {
        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
        {
            if (PhotonNetwork.PlayerList[i].NickName == PhotonNetwork.NickName)
            {
                readyView = PhotonView.Find(i + 1);
            }
        }
    }

    //checks each player's custom props
    //if there are at least 2 players
    //and they all are ready
    //start game
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

    //called when every client pressed the ready button and
    //there are at least 2 ppl
    public void StartGame()
    {
        PhotonNetwork.LoadLevel("Game");
    }

    //called when client presses ready button
    public void ReadyUp()
    {
        custProps["isReady"] = "true";
        PhotonNetwork.LocalPlayer.SetCustomProperties(custProps);
        readyView.RPC("SyncReady", RpcTarget.All);
        CheckForGameStart();
    }

    public void ReturnToLobby()
    {
        PhotonNetwork.Disconnect();
        PhotonNetwork.LoadLevel("Loading");
    }
}
