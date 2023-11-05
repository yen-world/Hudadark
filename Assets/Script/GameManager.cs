using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public Sprite[] signSprites = new Sprite[2];
    // public BuildingType[] buildingSprite = new BuildingType[1];
    public Card[] cards = new Card[0];
    public Building[] buildings = new Building[4];

    public PlayerManager[] players = new PlayerManager[2]; //전체 플레이어 리스트
    public PlayerManager nowPlayer;

    public int turnCount;
    public bool nextTurn = false;

    public bool UIFlag = false; //UI가 활성화 되어있는지 확인하는 플래그

    public GameObject cardsObj;
    public GameObject cardPrefab;

    TurnSignScript theTSI;

    public List<GameObject> buyedTiles = new List<GameObject>();
    public GameObject clickedTile;

    public GameObject[] tpTile = new GameObject[2]; //텔레포트 활성화 시 다음턴에 움질일 위치 정함.

    // Start is called before the first frame update
    void Start()
    {
        theTSI = FindObjectOfType<TurnSignScript>();
    }

    // Update is called once per frame
    void Update()
    {   
        if(nextTurn){
            if(turnCount % 2 == 1){//나머지가 1이면 1플레이어, 0이면 2플레이어
                
                players[0].myTurn = true;
                players[1].myTurn = false;
                theTSI.cursorPos = 1;
                nowPlayer = players[0];
                // CardListUpdate();
            }
            else{
                players[1].myTurn = true;
                players[0].myTurn = false;
                nowPlayer = players[1];
                theTSI.cursorPos = 1;
                // CardListUpdate();
            }
            nextTurn = false;
        }
        
    }

    void CardListUpdate(){
        if(cardsObj.transform.childCount != 0){
            for(int i = 0; i < cardsObj.transform.childCount ; i++){
                Destroy(cardsObj.transform.GetChild(i).gameObject);
            }
        }
        if(nowPlayer.cards.Count > 0){
            for(int i = 0; i < nowPlayer.cards.Count; i++){
                var _card = Instantiate(cardPrefab, new Vector3(0f,0f,0f), Quaternion.identity, cardsObj.transform);//플레이어가 소지한 카드를 생성하고
                _card.transform.localPosition = new Vector3(0f,0f,0f);
                _card.GetComponent<CardManager>().cardInfo = nowPlayer.cards[i]; //현재 플레이어가 가진 카드의 속성을 전부 대입
                _card.GetComponent<SpriteRenderer>().sprite = nowPlayer.cards[i].cardImg; //이미지도 추가로 설정
            }
        }
    }
}
