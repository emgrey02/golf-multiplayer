using UnityEngine;
using Photon.Pun;
using TMPro;
using Photon.Realtime;

public class CreateAndJoinRooms : MonoBehaviourPunCallbacks
{
    public TMP_InputField createInput;
    public TMP_InputField joinInput;

    private TextMeshProUGUI title;
    private GameObject createStuff;
    private GameObject joinStuff;
    private GameObject nameStuff;

    private const string ppKey = "PlayerName";

    private void Awake()
    {
        //when a master client loads a level, the rest of the clients do too
        PhotonNetwork.AutomaticallySyncScene = true;
        title = GameObject.Find("Title").GetComponent<TextMeshProUGUI>();
        createStuff = GameObject.Find("Create");
        joinStuff = GameObject.Find("Join");
        nameStuff = GameObject.Find("Name");
    }

    private void Start()
    {
        string name = "";
        TMP_InputField input = nameStuff.GetComponent<TMP_InputField>();
        PhotonNetwork.NickName = name;
    }

    public void CreateRoom()
    {
        PhotonNetwork.CreateRoom(createInput.text, new RoomOptions {  MaxPlayers = 4 });
    }

    public void JoinRoom()
    {
        PhotonNetwork.JoinRoom(joinInput.text);
    }

    public void SetPlayerName(string name)
    {
        PhotonNetwork.NickName = name;
    }

    public void StartGame()
    {
        PhotonNetwork.LoadLevel("PreGame");
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("PUN Basics Tutorial/Launcher: OnConnectedToMaster() was called by PUN");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarningFormat("PUN Basics Tutorial/Launcher: OnDisconnected() was called by PUN with reason {0}", cause);
    }

}
