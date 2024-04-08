#region using
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BackEnd;
using BackEnd.Tcp;
using UnityEngine.SceneManagement;
using System.Text;
using UnityEngine.UI;

#endregion
public class EventManager : MonoBehaviour
{
    CardManager theCardManager;
    DiceSystem theDice;
    MatchInGameRoomInfo _roomInfo; //인게임에서 방 정보를 전달하기위해 선언해둔 변수

    public static EventManager Instance = null;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = FindObjectOfType(typeof(EventManager)) as EventManager;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    private void Start()
    {
        theDice = FindObjectOfType<DiceSystem>();
    }

    void Update()
    {
        // 대기방을 떠나면서 실행되는 핸들러
        Backend.Match.OnMatchMakingRoomLeave = (MatchMakingGamerInfoInRoomEventArgs args) =>
        {

        };

        //매칭신청(인게임서버접속 시작)
        Backend.Match.OnMatchMakingResponse = (MatchMakingResponseEventArgs args) =>
        {
            // 유저가 매칭을 신청, 취소 했을 때 그리고 매칭이 성사되었을 때 호출되는 이벤트
            switch (args.ErrInfo)
            {
                case ErrorCode.Success: //매칭이 성사되었을 떄 여기서 인게임 서버 접속시도

                    //true인 경우, OnSessionJoinInServer 호출.
                    if (Backend.Match.JoinGameServer(args.RoomInfo.m_inGameServerEndPoint.m_address,
                    args.RoomInfo.m_inGameServerEndPoint.m_port,
                    false, out ErrorInfo errorInfo) == false)
                    {

                    }
                    break;
            }
        };

        //인게임서버에 접속 성공했을 떄 호출되는 이벤트
        Backend.Match.OnSessionJoinInServer += (args) =>
        {
            if (args.ErrInfo == ErrorInfo.Success)
            {
                //OnMatchMakingResponse에서 전달받은 RoomToken을 여기로 전달.
                Backend.Match.JoinGameRoom(this._roomInfo.m_inGameRoomToken);
            }
        };

        //유저가 입장 시 호출
        Backend.Match.OnMatchInGameAccess = (MatchInGameSessionEventArgs args) =>
        {
            if (args.ErrInfo == ErrorCode.Success)
            {
                AudioManager.Instance.Stop("Title_Sound");
                SceneManager.LoadScene("MainScene");
            }
        };

        //게임시작 이벤트 브로드캐스팅 준비 완료
        Backend.Match.OnMatchInGameStart = () =>
        {
            UIManager.Instance.SetUI();
        };

        Backend.Match.OnMatchRelay = (MatchRelayEventArgs args) =>
        {
            byte[] data = args.BinaryUserData;
            ParsingData pData = JsonUtility.FromJson<ParsingData>(Encoding.Default.GetString(data));
            switch (pData.type)
            {
                case ParsingType.TurnCardSet:
                    TurnCardSet tsData = JsonUtility.FromJson<TurnCardSet>(pData.data);
                    if (tsData.randomNum == 0)
                    {
                        GameManager.Instance.turnCards[0].GetComponent<ButtonManager>().turnNum = 1;
                        GameManager.Instance.turnCards[1].GetComponent<ButtonManager>().turnNum = 0;
                    }
                    else
                    {
                        GameManager.Instance.turnCards[0].GetComponent<ButtonManager>().turnNum = 0;
                        GameManager.Instance.turnCards[1].GetComponent<ButtonManager>().turnNum = 1;
                    }
                    break;

                case ParsingType.Turn:
                    TurnCard tData = JsonUtility.FromJson<TurnCard>(pData.data);
                    GameManager.Instance.playerCount.Add(1);
                    GameManager.Instance.turnCards[tData.turncardIdx].SetActive(false);
                    if (GameManager.Instance.playerCount.Count > 1)
                    {
                        GameManager.Instance.turnCardParent.SetActive(false);
                    }
                    break;

                case ParsingType.Dice:
                    if (theDice == null) theDice = FindObjectOfType<DiceSystem>();

                    StartCoroutine(theDice.RollDiceCoroutine());
                    DiceData dData = JsonUtility.FromJson<DiceData>(pData.data);
                    GameManager.Instance.diceNum = dData.diceNum;
                    theDice.diceFlag = true;
                    break;

                case ParsingType.NextTurn:
                    GameManager.Instance.NextTurnFunc();
                    GameManager.Instance.UIFlag = false;
                    break;

                case ParsingType.GroundBuy:
                    if (GameManager.Instance.myCharactor.myTurn)
                    {
                        GameManager.Instance.myCharactor.groundCount += 1;
                        GameManager.Instance.myCharactor.playerMoney -= 50;
                        GameManager.Instance.nowPlayer.nowTile.price = 50;
                    }
                    else
                    {
                        GameManager.Instance.myCharactor.againstPlayer.nowTile.ownPlayer
                            = GameManager.Instance.myCharactor.againstPlayer.playerId;
                        GameManager.Instance.myCharactor.againstPlayer.groundCount += 1;
                        GameManager.Instance.myCharactor.againstPlayer.playerMoney -= 50;
                        GameManager.Instance.nowPlayer.nowTile.price = 50;
                    }
                    GameManager.Instance.SetFloatingText(GameManager.Instance.nowPlayer, 50, false);
                    break;

                case ParsingType.BuildingBuy:
                    if (GameManager.Instance.myCharactor.myTurn)
                    {
                        GameManager.Instance.NextTurnFunc();
                        GameManager.Instance.UIFlag = false;
                    }
                    else
                    {
                        BuildingData bdata = JsonUtility.FromJson<BuildingData>(pData.data);

                        GameManager.Instance.myCharactor.againstPlayer.nowTile.building =
                            GameManager.Instance.buildings[bdata.buildingNum];

                        GameManager.Instance.myCharactor.againstPlayer.buildingCount += 1;
                        GameManager.Instance.myCharactor.againstPlayer.playerMoney -= 50;
                        GameManager.Instance.myCharactor.againstPlayer.nowTile.price =
                        GameManager.Instance.buildings[bdata.buildingNum].toll;
                        GameManager.Instance.SetFloatingText(GameManager.Instance.nowPlayer, 50, false);
                        GameManager.Instance.NextTurnFunc();
                        GameManager.Instance.UIFlag = false;
                    }
                    break;

                case ParsingType.Teleport:
                    TeleportData tpData = JsonUtility.FromJson<TeleportData>(pData.data);

                    GameManager.Instance.nowPlayer.tpFlag = tpData.tpFlag;
                    GameManager.Instance.nowPlayer.tpTile = GameObject.Find(tpData.tpTileNum);

                    GameManager.Instance.seletedTile = null;

                    GameManager.Instance.NextTurnFunc();
                    GameManager.Instance.UIFlag = false;
                    break;

                case ParsingType.Card:
                    CardData cardData = JsonUtility.FromJson<CardData>(pData.data);
                    GameManager.Instance.nowPlayer.cards.Add(cardData.card);
                    break;


                case ParsingType.TileSelect:
                    TileSelectData tileSelectData = JsonUtility.FromJson<TileSelectData>(pData.data);
                    GameManager.Instance.seletedTile = GameObject.Find(tileSelectData.tilename);
                    break;

                case ParsingType.Extortion:
                    ExtortionData extortionData = JsonUtility.FromJson<ExtortionData>(pData.data);
                    Color tileColor = GameManager.Instance.seletedTile.GetComponent<Tile>().signImg.GetComponent<SpriteRenderer>().color;
                    StartCoroutine(ExtortionAlphaCoroutine(tileColor, extortionData.playerId));
                    break;

                case ParsingType.CardClick:
                    CardClickData cData = JsonUtility.FromJson<CardClickData>(pData.data);
                    switch (cData.cardNum)
                    {
                        case 1:
                            GameManager.Instance.nowPlayer.highSpeedFlag = true;
                            break;

                        case 2:
                            GameManager.Instance.nowPlayer.invisibleFlag = true;
                            break;

                        case 3:
                            GameManager.Instance.nowPlayer.biggerFlag = true;
                            break;

                        case 4:
                            GameManager.Instance.nowPlayer.lowerDiceFlag = true;
                            break;

                        case 5:
                            GameManager.Instance.nowPlayer.higherDiceFlag = true;
                            break;

                        case 7:
                            GameManager.Instance.nowPlayer.laserFlag = true;
                            break;
                    }
                    break;

                case ParsingType.CardListAdd:
                    CardData cardData1 = JsonUtility.FromJson<CardData>(pData.data);

                    var _card = Instantiate(GameManager.Instance.nowPlayer.cardPrefab,
                        Vector3.zero, Quaternion.identity, GameManager.Instance.nowPlayer.cardParent);
                    _card.transform.localPosition = new Vector3(0f, 0f, 0f);

                    GameManager.Instance.nowPlayer.cards.Add(cardData1.card);
                    break;

                case ParsingType.CardDestory:
                    CardDestroyData destroyData = JsonUtility.FromJson<CardDestroyData>(pData.data);
                    Destroy(destroyData.destoryCard);
                    Destroy(GameManager.Instance.nowPlayer.cardParent.GetChild(0).gameObject);
                    GameManager.Instance.nowPlayer.cards.Remove(GameManager.Instance.nowPlayer.cards.Find(card => card.cardCode == destroyData.cardCode));
                    break;

                case ParsingType.InvisibleThief:
                    GameManager.Instance.invisibleCardNum = UnityEngine.Random.Range(0,
                        GameManager.Instance.nowPlayer.againstPlayer.cards.Count);
                    break;

                case ParsingType.ExemptionFlag:
                    StartCoroutine(ExemptionCoroutine());
                    break;

                case ParsingType.ExemptionFlagSet:
                    GameManager.Instance.nowPlayer.exemptionFlag = true;
                    break;

                case ParsingType.Visit:
                    VisitData visitData = JsonUtility.FromJson<VisitData>(pData.data);
                    switch (visitData.caseNum)
                    {
                        case 0:
                            GameManager.Instance.nowPlayer.playerMoney += visitData.money;
                            GameManager.Instance.SetFloatingText(GameManager.Instance.nowPlayer, visitData.money, true);
                            GameManager.Instance.NextTurnFunc();
                            break;

                        case 1:
                            StartCoroutine(TempleCoroutine());
                            break;
                    }
                    break;

                case ParsingType.ArriveTile:
                    StartCoroutine(ArriveCoroutine(pData));
                    break;

                case ParsingType.Olympic:
                    StartCoroutine(GameManager.Instance.OlympicMethod(GameManager.Instance.nowPlayer.playerId, GameManager.Instance.nowPlayer.VirtualCamera));
                    GameManager.Instance.NextTurnFunc();
                    break;

                case ParsingType.Laser:
                    LaserData laserData = JsonUtility.FromJson<LaserData>(pData.data);
                    GameManager.Instance.seletedTile = GameObject.Find(laserData.laserTileNum);
                    theCardManager = GameObject.Find("CardManager").GetComponent<CardManager>();
                    StartCoroutine(theCardManager.LaserCoroutine());
                    break;
            }
        };

        //게임 종료(정상적: 게임에서 게임오버 함수 호출, 비정상적 : 플레이어가 나감)
        Backend.Match.OnMatchResult = (MatchResultEventArgs args) =>
        {
            GameManager.Instance.gameOverUI.SetActive(true);
        };

        //게임 중, 플레이어가 연결 끊김.
        Backend.Match.OnSessionOffline = (MatchInGameSessionEventArgs args) =>
        {
            if (args.ErrInfo == ErrorCode.NetworkOffline)
            {
                //연결 끊긴 방에서 나가기 위한 UI 출력
                UIManager.Instance.SetErrorUI();
            }
        };
    }


    //건물강탈 코루틴
    IEnumerator ExtortionAlphaCoroutine(Color tileColor, int playerId)
    {
        AudioManager.Instance.Play("Extortion_Sound");

        while (tileColor.a > 0f)
        {
            tileColor.a -= 0.02f;
            GameManager.Instance.seletedTile.GetComponent<Tile>().signImg.GetComponent<SpriteRenderer>().color = tileColor;
            yield return new WaitForSeconds(0.02f);
        }

        GameManager.Instance.seletedTile.GetComponent<Tile>().ownPlayer = playerId;

        while (tileColor.a < 1f)
        {
            tileColor.a += 0.02f;
            GameManager.Instance.seletedTile.GetComponent<Tile>().signImg.GetComponent<SpriteRenderer>().color = tileColor;
            yield return new WaitForSeconds(0.02f);
        }

        GameManager.Instance.seletedTile = null;

        GameManager.Instance.NextTurnFunc();
        GameManager.Instance.UIFlag = false;
    }

    //양계장 코루틴
    IEnumerator ArriveCoroutine(ParsingData pData)
    {
        ArriveTileData arriveTileData = JsonUtility.FromJson<ArriveTileData>(pData.data);
        int totalMoney = 0;

        for (int i = 0; i < TileManager.Instance.tiles.Length; i++)
        {
            if (TileManager.Instance.tiles[i].ownPlayer == arriveTileData.playerId && TileManager.Instance.tiles[i].building.type == 0) totalMoney += 100;
        }
        GameManager.Instance.nowPlayer.playerMoney += totalMoney;

        yield return new WaitForSeconds(0.5f);

        if (totalMoney > 0)
        {
            GameManager.Instance.SetFloatingText(GameManager.Instance.nowPlayer, totalMoney, true);
        }

        GameManager.Instance.NextTurnFunc();
        GameManager.Instance.UIFlag = false;
    }

    //재단 코루틴
    IEnumerator TempleCoroutine()
    {
        AudioManager.Instance.Play("Olympics_Sound");
        GameManager.Instance.nowPlayer.nowTile.price *= 2;
        GameManager.Instance.nowPlayer.nowTile.transform.Find("Pos").GetChild(0).gameObject.SetActive(true);

        yield return new WaitForSeconds(1f);

        GameManager.Instance.nowPlayer.nowTile.transform.Find("Pos").GetChild(0).gameObject.SetActive(false);
        GameManager.Instance.NextTurnFunc();
    }

    //통행료 지불 코루틴(내 움직임이 끝날때까지 기다렸다가 징수하기 위해 코루틴 사용)
    IEnumerator ExemptionCoroutine()
    {
        yield return new WaitUntil(() => GameManager.Instance.nowPlayer.finishMoving == true);

        if (!GameManager.Instance.nowPlayer.exemptionFlag)
        {
            GameManager.Instance.nowPlayer.playerMoney -= GameManager.Instance.nowPlayer.nowTile.price;
            GameManager.Instance.SetFloatingText(GameManager.Instance.nowPlayer, GameManager.Instance.nowPlayer.nowTile.price, false);
            GameManager.Instance.nowPlayer.againstPlayer.playerMoney += GameManager.Instance.nowPlayer.nowTile.price;
            GameManager.Instance.SetFloatingText(GameManager.Instance.nowPlayer.againstPlayer, GameManager.Instance.nowPlayer.nowTile.price, true);

            GameManager.Instance.NextTurnFunc();
        }
        else
        {
            StartCoroutine(GameManager.Instance.ParticleFunc());
        }
    }
}
