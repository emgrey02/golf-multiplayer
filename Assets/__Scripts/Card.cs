using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Card : MonoBehaviour {

	public string    suit;
	public int       rank;
	public Color     color = Color.black;
	public string    colS = "Black";  // or "Red"
	
	public List<GameObject> decoGOs = new List<GameObject>();
	public List<GameObject> pipGOs = new List<GameObject>();
	
	public GameObject back;  // back of card;
	public CardDefinition def;  // from DeckXML.xml		

	// list of the SpriteRenderer Components of this GameoObject and its children
	public SpriteRenderer[] spriteRenderers;

	public bool faceUp {
		get {
			return (!back.activeSelf);
		}

		set {
			back.SetActive(!value);
		}
	}

	// Use this for initialization
	void Start () {
		SetSortOrder(0); //ensures that the card starts properly depth sorted
	}

	// if spriteRenderers is not yet defined, this function defines it
	public void PopulateSpriteRenderers()
	{
		// if spriteRenderers is null or empty
		if (spriteRenderers == null || spriteRenderers.Length == 0)
		{
			spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
		}
	}

	public void SetSortingLayerName(string tSLN)
	{
		PopulateSpriteRenderers();

		foreach(SpriteRenderer tSR in spriteRenderers)
		{
			tSR.sortingLayerName = tSLN;
		}
	}
	
	// sets the sortingOrder of all SpriteRenderer Components
	public void SetSortOrder(int sOrd)
	{
		PopulateSpriteRenderers();

		// iterate through all the spriteRenderers as tSR
		foreach (SpriteRenderer tSR in spriteRenderers)
		{
			if (tSR.gameObject == this.gameObject)
			{
				// if the gameObject is this.gameObject, it's the background
				tSR.sortingOrder = sOrd;
				continue;
			}
			// each child of this GameObject are named
			// switch based on the names
			switch (tSR.gameObject.name)
			{
				case "back":
					// set it to highest layer to cover other sprites
					tSR.sortingOrder = sOrd + 2;
					break;

				case "face":
				default:
					tSR.sortingOrder = sOrd + 1;
					break;
			}
		}
	}

	virtual public void OnMouseUpAsButton()
	{
		print(name);
	}

} // class Card

[System.Serializable]
public class Decorator{
	public string	type;			// For card pips, tyhpe = "pip"
	public Vector3	loc;			// location of sprite on the card
	public bool		flip = false;	//whether to flip vertically
	public float 	scale = 1.0f;
}

[System.Serializable]
public class CardDefinition{
	public string	face;	//sprite to use for face cart
	public int		rank;	// value from 1-13 (Ace-King)
	public List<Decorator>	
					pips = new List<Decorator>();  // Pips Used
}
