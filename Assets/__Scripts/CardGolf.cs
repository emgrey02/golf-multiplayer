using Photon.Pun;
using UnityEngine;

// an enum defines a variable type with a few prenamed values
public enum eGolfCardState
{
    drawpile,
    hand,
    target,
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

    public int owner;

    [PunRPC]
    public void SetOwner(int actorNum) {
        this.owner = actorNum;
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
        if (s == "target")
        {
            state = eGolfCardState.target;
        }
    }

    [PunRPC]
    public void SetCardProps(bool faceUp, int sortOrder, string sortName)
    {
        this.faceUp = faceUp;
        this.SetSortOrder(sortOrder);
        this.SetSortingLayerName(sortName);
    }
}
