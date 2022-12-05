using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// an enum defines a variable type with a few prenamed values
public enum eGolfCardState
{
    drawpile,
    hand,
    discard
}

public class CardGolf : Card
{
    [Header("Set Dynamically")]
    // this is how you use the enum eCardState
    public eGolfCardState state = eGolfCardState.drawpile;

    public override void OnMouseUpAsButton()
    {
        // call the cardClicked method on Golf singleton
        Camera.main.GetComponent<Golf>().CardClicked(this);

        // also call the base class (Card.cs) version of this method
        base.OnMouseUpAsButton();
    }

    [PunRPC]
    public void SetCardState(string s)
    {
        if (s == "discard")
        {
            state = eGolfCardState.discard;
        }
        if (s == "drawpile")
        {
            state = eGolfCardState.drawpile;
        }
        if (s == "hand")
        {
            state = eGolfCardState.hand;
        }
    }

    [PunRPC]
    public void SetCardProps(Vector3 pos, bool faceUp)
    {
        this.transform.localPosition = pos;
        this.faceUp = faceUp;
    }
}
