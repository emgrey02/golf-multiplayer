using Photon.Pun;
using TMPro;


public class ReadyManager : MonoBehaviourPunCallbacks
{
    private TextMeshProUGUI text;

    private void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    //PreGameUI calls this on correct ready view according to what
    //player calls it
    //syncs this photonView with all clients
    [PunRPC]
    public void SyncReady(bool ready)
    {
        if (ready)
        {
            text.gameObject.SetActive(true);
            text.text = "READY!";
        }
        else {
            text.text = "";
            text.gameObject.SetActive(false);
        }
    }
}
