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
    [Header("Set Dynamically: CardProspector")]
    // this is how you use the enum eCardState
    public eGolfCardState state = eGolfCardState.drawpile;

    public override void OnMouseUpAsButton()
    {
        // call the cardClicked method on Golf singleton
        Camera.main.GetComponent<Golf>().CardClicked(this);

        // also call the base class (Card.cs) version of this method
        base.OnMouseUpAsButton();

    }
}
