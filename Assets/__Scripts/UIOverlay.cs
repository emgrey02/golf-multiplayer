using Photon.Pun;
using TMPro;
using UnityEngine;

public class UIOverlay : MonoBehaviourPunCallbacks
{
    private GameObject overlay;
    private TextMeshProUGUI title;

    private TextMeshProUGUI message;
    private TextMeshProUGUI knockMessage;

    private GameObject donePeakingButton;
    private GameObject swapButton;
    private GameObject discardButton;
    private GameObject knockButton;
    private GameObject RoundOver;
    private GameObject NextRoundButton;
    private GameObject Scoreboard;
    private GameObject GameOverPanel;

    private void Awake()
    {
        Scoreboard = gameObject.transform.GetChild(0).gameObject;

        overlay = gameObject.transform.GetChild(1).gameObject;

        title = overlay.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        donePeakingButton = overlay.transform.GetChild(1).gameObject;
        swapButton = overlay.transform.GetChild(2).gameObject;
        discardButton = overlay.transform.GetChild(3).gameObject;

        message = gameObject.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        message.text = "";
        knockButton = gameObject.transform.GetChild(3).gameObject;
        RoundOver = gameObject.transform.GetChild(4).gameObject;
        NextRoundButton = gameObject.transform.GetChild(5).gameObject;
        GameOverPanel = gameObject.transform.GetChild(6).gameObject;

        knockMessage = gameObject.transform.GetChild(7).GetComponent<TextMeshProUGUI>();

        DeactivateButtons();
        overlay.SetActive(false);
        message.gameObject.SetActive(false);
    }

    public void SetOverlayText(eGolfPlayerState playerState)
    {
        if (playerState == eGolfPlayerState.peaking)
        { 
            title.text = "Peak at two cards in your hand. Memorize them!";
            donePeakingButton.SetActive(true);
        }

        if (playerState == eGolfPlayerState.deciding)
        {
            title.text = "Discard, or swap with a card in your hand.";
            swapButton.SetActive(true);
            discardButton.SetActive(true);
            knockButton.SetActive(false);
        }

        if (playerState == eGolfPlayerState.swapping)
        {
            title.text = "Choose a card from your hand to swap this with.";
            swapButton.SetActive(false);
            discardButton.SetActive(false);
            knockButton.SetActive(false);
        }
    }

    public void SetKnockMessage(string name)
    {
        knockMessage.gameObject.SetActive(true);
        knockMessage.text = name + " knocked!!";
    }

    public void RemoveKnockMessage()
    {
        knockMessage.text = "";
        knockMessage.gameObject.SetActive(false);
    }

    public void TriggerGameOverDisplay()
    {
        GameOverPanel.SetActive(true);
        Scoreboard.transform.localPosition = new Vector3(0, 0, 0);
    }

    public void ActivateOverlay()
    {
        overlay.SetActive(true);
    }

    public void RemoveOverlay()
    {
        title.text = "";
        overlay.SetActive(false);
        DeactivateButtons();
    }

    private void DeactivateButtons()
    {
        donePeakingButton.SetActive(false);
        swapButton.SetActive(false);
        discardButton.SetActive(false);
        knockButton.SetActive(false);
    }

    public void SetMessage(string m)
    {
        message.gameObject.SetActive(true);
        message.text = m;
    }

    public void RemoveMessage()
    {
        message.gameObject.SetActive(false);
    }

    public void RevealKnockButton() 
    {
        knockButton.SetActive(true);
    }

    public void RemoveKnockButton()
    {
        knockButton.SetActive(false);
    }

    public void ActivateRoundOver()
    {
        RoundOver.SetActive(true);

        if (PhotonNetwork.IsMasterClient) {
            NextRoundButton.SetActive(true);
        }
    }

    public void DeactivateRoundOver()
    {
        RoundOver.SetActive(false);
        NextRoundButton.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
