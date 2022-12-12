using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;

public class Scoreboard : MonoBehaviourPunCallbacks
{
    public GameObject scoreboard;

    public TextMeshProUGUI p1Name;
    public TextMeshProUGUI p2Name;
    public TextMeshProUGUI p3Name;
    public TextMeshProUGUI p4Name;

    public TextMeshProUGUI p1Total;
    public TextMeshProUGUI p2Total;
    public TextMeshProUGUI p3Total;
    public TextMeshProUGUI p4Total;
    public TextMeshProUGUI roundNum;

    [SerializeField]
    private int _roundNum;
    public static int P1_SCORE;
    public static int P2_SCORE;
    public static int P3_SCORE;
    public static int P4_SCORE;

    [SerializeField]
    public static List<int> SCORES = new List<int> { P1_SCORE, P2_SCORE, P3_SCORE, P4_SCORE };

    private List<TextMeshProUGUI> namesList;
    private List<TextMeshProUGUI> scoresOnBoardList;

    public int RoundNum {
        get 
        {
            return _roundNum;
        }
        set 
        {
            _roundNum = value;
            roundNum.text = "Round " + _roundNum.ToString();
        }
    }

    void Awake()
    {
        namesList = new List<TextMeshProUGUI> { p1Name, p2Name, p3Name, p4Name };
        scoresOnBoardList = new List<TextMeshProUGUI> { p1Total, p2Total, p3Total, p4Total };
    }

    [PunRPC]
    public void SetPlayerNames(int num, string name) 
    {
        namesList[num].text = name;
    }

    [PunRPC]
    private void UpdateScoreBoard(int num)
    {
        scoresOnBoardList[num].text = SCORES[num].ToString(); 
    }

    [PunRPC]
    public void TallyScores(string pName, string[] hand) 
    {
        for (int i = 0; i < namesList.Count; i++) 
        {
            if (pName == namesList[i].text) 
            {
                SCORES[i] += CalculateScore(hand);
                scoresOnBoardList[i].text = SCORES[i].ToString();
            }
        }
    }

    private int CalculateScore(string[] hand) 
    {
        int totalScore = 0;

        for (int i = 0; i < hand.Length; i++) 
        {
            int score;

            //get just the number from the card name
            int.TryParse(hand[i].Substring(1), out score);

            //jack and queens are 10
            if (score == 11 || score == 12) 
            {
                score = 10;
            }

            //kings are 0
            if (score == 13) 
            {
                score = 0;
            }

            totalScore += score;
        }
        return totalScore;
    }

    

}
