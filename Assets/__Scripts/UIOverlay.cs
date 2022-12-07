using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIOverlay : MonoBehaviourPunCallbacks
{
    private GameObject overlay;
    private TextMeshProUGUI title;

    private TextMeshProUGUI message;

    public GameObject donePeakingButton;
    public GameObject swapButton;
    public GameObject discardButton;

    private void Awake()
    {
        overlay = gameObject.transform.GetChild(0).gameObject;
        title = overlay.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        message = gameObject.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        message.text = "";

        donePeakingButton = overlay.transform.GetChild(1).gameObject;
        swapButton = overlay.transform.GetChild(2).gameObject;
        discardButton = overlay.transform.GetChild(3).gameObject;

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
        }

        if (playerState == eGolfPlayerState.swapping)
        {
            title.text = "Choose a card from your hand to swap this with.";
        }
    }

    public void ActivateOverlay()
    {
        overlay.SetActive(true);
    }

    public void RemoveOverlay()
    {
        Debug.Log("removing overlay");
        title.text = "";
        overlay.SetActive(false);
        DeactivateButtons();
    }

    private void DeactivateButtons()
    {
        donePeakingButton.SetActive(false);
        swapButton.SetActive(false);
        discardButton.SetActive(false);
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

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
         
    }
}
