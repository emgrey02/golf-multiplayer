using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ReadyManager : MonoBehaviourPunCallbacks
{
    private TextMeshProUGUI text;

    private void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    [PunRPC]
    void SyncReady(PhotonMessageInfo info)
    {
        Debug.LogFormat("Info: {0} {1} {2}", info.Sender, info.photonView, info.SentServerTime);
        text.text = "READY!";
    }
}
