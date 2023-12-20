using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour
{
    public int playerId; //몇번째 플레이어인지 정보
    public int playerMoney; //플레이어의 자금
    public Text playerMoneyText; //플레이어 자금 플로팅
    public float speed; //이동속도
    [SerializeField] bool movingFlag; //이동 완료인지 이동중인지 체크하는 플래그
    [SerializeField] List<GameObject> tileToGo = new List<GameObject>(); //플레이어가 가야될 타일//최대 12칸.
    public Tile nowTile; //현재 서 있는 타일의 정보
    public int diceNum; //주사위의 눈금
    public bool diceFlag; // 주사위 굴렸는지 플래그
    public bool movingCoroutineFlag; //코루틴 반복을 방지하는 플래그
    public List<Card> cards = new List<Card>(); //플레이어가 가진 카드
    PlayerCard thePCard;
    [SerializeField] int tileNum; //플레이어가 서있는 칸의 번호
    TileManager theTM;//플레이어가 가야될 타일 정보 받아오기 위해 추가
    public bool myTurn;
    public Text downInformationText;
    GameManager theGM;

    public GameObject VirtualCamera;
    public GameObject cardPrefab;
    public Transform cardParent;
    public PlayerManager againstPlayer;
    public List<GameObject> againstPlayer_Tile = new List<GameObject>();

    [Header("Building")]
    public int buildingCount = 0;
    public int groundCount = 0;

    [Header("Buy")]
    [SerializeField] GameObject groundBuyUi;
    [SerializeField] GameObject purchaseUi;

    TurnSignScript theTSI;
    [Header("Effect")]
    public bool tpFlag;
    bool tpSelectFlag;
    public GameObject tpBack; //tp활성화 시 맵 이외의 주변이 어둡게 변함.

    public bool tpMovingFlag; //tp무빙 플래그 활성화 시 애니메이션 재생
    public GameObject tpTile; //다음 이동할곳 저장
    public bool highSpeedFlag;
    public bool invisibleFlag; //투명화
    public bool toosiFlag; //투시
    public bool biggerFlag; //거대화
    public bool higherDiceFlag;
    public bool lowerDiceFlag;
    public bool exemptionFlag;
    public bool laserFlag;

    public CardManager theCM;
    public bool showCardFlag;

    // Start is called before the first frame update
    void Start()
    {
        theTM = FindObjectOfType<TileManager>();
        theGM = FindObjectOfType<GameManager>();
        theTSI = FindObjectOfType<TurnSignScript>();
        thePCard = FindObjectOfType<PlayerCard>();
    }

    // Update is called once per frame
    void Update()
    {
        playerMoneyText.text = playerMoney.ToString();
        if (myTurn)
        {
            downInformationText.gameObject.SetActive(true);
        }
        else
        {
            downInformationText.gameObject.SetActive(false);
        }
        if (!tpFlag && movingCoroutineFlag)
        {

            StartCoroutine(DiceCoroutine());
        }

        if (toosiFlag && myTurn)
        {
            toosiFlag = false;
            theCM.Penetrate();
        }

        if (laserFlag && myTurn)
        {
            laserFlag = false;
            theCM.LaserBeam();
        }

        if (tpFlag && myTurn)
        {//말끔하게 수정 필요
            tpMovingFlag = true;
            StartCoroutine(TeleportCoroutine());
            //waitcoroutine 추가로 자신의 턴이 끝나기 전에 상대방이 움직이는 버그 해결
            StartCoroutine(NextTrunWait());
        }
    }

    IEnumerator DiceCoroutine()
    {
        movingCoroutineFlag = false;

        // 카드효과 사전작업
        if (theGM.nowPlayer.highSpeedFlag)
        {
            theCM.HighSpeedMove();
        }
        if (theGM.nowPlayer.invisibleFlag)
        {
            // 캐릭터가 점점 투명해지는 듯한 연출
            Color alpha = new(1, 1, 1, 1);
            while (true)
            {
                theGM.nowPlayer.GetComponent<SpriteRenderer>().color = alpha;
                alpha.a -= 0.1f;
                yield return new WaitForSeconds(0.1f);

                if (alpha.a <= 0.5f)
                    break;
            }
        }
        if (theGM.nowPlayer.biggerFlag)
        {
            // 크기가 서서히 커지는 듯한 연출
            Vector3 scale = new Vector3(1.5f, 1.5f, 0);
            while (true)
            {
                scale += new Vector3(0.1f, 0.1f, 0);
                this.gameObject.transform.localScale = scale;
                yield return new WaitForSeconds(0.1f);

                if (scale.x >= 2f)
                {
                    break;
                }
            }
        }


        if (diceFlag)
        {
            for (int i = 0; i < diceNum; i++)
            { //주사위 눈금만큼 리스트에 넣어야됨.
                if (tileNum + i >= theTM.tiles.Length)
                {
                    //넣어야하는 오브젝트의 길이가 전체 리스트의 길이를 넘어간다면 제대로 더해지지 않는거임.
                    tileToGo.Add(theTM.tiles[tileNum + i - theTM.tiles.Length].gameObject);
                }
                else
                {
                    //아니라면 그냥 추가시켜주면 됨.
                    tileToGo.Add(theTM.tiles[tileNum + i].gameObject);
                }
            }

            //

            VirtualCamera.SetActive(true);
            //주사위 굴리는거 기다려야됨
            yield return new WaitForSeconds(1f);
            print("주사위 완료");

            //플레이어 이동
            if (theGM.nowPlayer.highSpeedFlag)
            {
                theCM.highMoveParticle.gameObject.SetActive(true);
                theCM.highMoveParticle.Play();
            }

            theTSI.cursorPos = 3;
            for (; tileToGo.Count != 0;)
            {
                if (tileToGo[0].transform.name == "0")
                {
                    // this.transform.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 0f, 1f);
                    playerMoney += 100; //지나다닐때마다 100알씩 지급
                }
                else
                {
                    // this.transform.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 1f);
                }

                // 투명도둑을 사용하고 나와 상대방이 겹쳐질때, 상대방의 카드가 있을 때 투명도둑 효과 발동
                if (invisibleFlag)
                {
                    if (againstPlayer.nowTile == nowTile && againstPlayer.cards.Count != 0)
                    {
                        theCM.InvisibleThief();
                    }
                }

                //player를 이동시킴 (애니메이션 필요) 메인 이동 코드
                movingFlag = true;
                Vector3 targetPos = tileToGo[0].transform.Find("Pos").transform.position;

                StartCoroutine(MovingCoroutine(targetPos));
                yield return new WaitUntil(() => movingFlag == false); //코루틴이 끝난지 체크
                nowTile = tileToGo[0].GetComponent<Tile>(); //현재 타일
                //AudioManager.instance.Play("moveSound");

                //리스트에서 첫번째 요소 삭제
                tileToGo.RemoveAt(0);
            }

            this.gameObject.GetComponent<Animator>().SetBool("FlyFlag", false);
            this.gameObject.GetComponent<Animator>().SetBool("WalkFlag", false);

            // 투명도둑을 사용했었다면 알파값 원상복구
            if (theGM.nowPlayer.invisibleFlag)
            {
                // invisibleFlag를 False로 변환
                theGM.nowPlayer.invisibleFlag = false;

                Color alpha = new(1, 1, 1, 0.5f);
                while (true)
                {
                    theGM.nowPlayer.GetComponent<SpriteRenderer>().color = alpha;
                    alpha.a += 0.1f;
                    yield return new WaitForSeconds(0.1f);

                    if (alpha.a >= 1f)
                        break;
                }

                theCM.InvisibleParticle.Stop();
                theCM.InvisibleParticle.gameObject.SetActive(false);
            }

            // 고속이동이 끝났다면 스피드를 원상복구 시키고 플래그를 비활성화시킴
            if (theGM.nowPlayer.highSpeedFlag)
            {
                theGM.nowPlayer.speed = 4f;
                theGM.nowPlayer.highSpeedFlag = false;

                theCM.highMoveParticle.Stop();
                theCM.highMoveParticle.gameObject.SetActive(false);
            }

            // 플레이어가 거대화 스킬을 사용하고 이동이 끝났다면 효과 발동
            if (theGM.nowPlayer.biggerFlag)
            {
                theCM.BiggerChicken();
                yield return new WaitUntil(() => theCM.completeFlag);
                theCM.completeFlag = false;
            }

            if (tileNum + diceNum > theTM.tiles.Length)
            {
                //만약 현재 위치를 업데이트 했을때, 총 타일의 길이를 넘어간다면 길이만큼 빼 줘야 정확한 위치에 있는것임.
                tileNum += diceNum;
                tileNum -= theTM.tiles.Length;
            }
            else
            {//아니라면 그대로 더하기 진행
                tileNum += diceNum;
            }

            diceFlag = false;//작업 완료 후 다이스 false
            this.gameObject.GetComponent<Animator>().SetBool("WalkFlag", false);
            movingCoroutineFlag = false; //무빙 플래그도 false


            // 이동이 끝난 후, 일반 타일에 도착했다면
            if (!nowTile.specialTile)
            {
                // 일반 타일 중 자신이 구매한 타일이라면
                if (nowTile.ownPlayer == playerId)
                {
                    // 건물이 없으면 건물 구매 UI 활성화
                    if (nowTile.building == null)
                    {
                        purchaseUi.SetActive(true);
                        //카드 선택 방지를 위한 UI활성화 플래그 활성화
                        theGM.UIFlag = true;
                    }
                    // 건물이 있으면 건물 방문 효과 활성화
                    else
                    {

                    }
                }
                // 일반 타일 중 아무도 구매하지 않은 타일이라면 땅 구매 UI 활성화
                else if (nowTile.ownPlayer == -1)
                {
                    groundBuyUi.SetActive(true);
                    //카드 선택 방지를 위한 UI활성화 플래그 활성화
                    theGM.UIFlag = true;
                }
                // 일반 타일 중 상대방이 구매한 타일이라면
                else
                {
                    // 통행료 카드가 없는 경우 통행료 징수
                    if (!exemptionFlag)
                    {
                        // 건물이 있는 경우 건물에 따른 통행료 징수
                        if (nowTile.building != null)
                        {
                            switch (nowTile.building.type)
                            {
                                // 농장
                                case 0:
                                    break;

                                // 제단
                                case 1:
                                    break;

                                // 특별상점
                                case 2:
                                    break;

                                // 랜드마크
                                case 3:
                                    if (nowTile.building.visitCount < 5)
                                        nowTile.building.visitCount += 1;
                                    playerMoney -= nowTile.building.visitCount * 100;
                                    againstPlayer.playerMoney += nowTile.building.visitCount * 100;
                                    break;

                                default:
                                    playerMoney -= 100;
                                    againstPlayer.playerMoney += 100;
                                    break;
                            }

                        }
                        // 건물이 없다면 기본 토지 통행료만 징수
                        else
                        {
                            playerMoney -= 50;
                        }
                    }

                    // 통행료 면제 카드가 있다면 통행료 징수를 하지 않음
                    else
                    {
                        theCM.TollExemption();
                    }
                    theGM.NextTurnFunc();
                }
            }
            // 일반 타일이 아니라, 특수 타일일 경우
            else
            {
                switch (nowTile.specialTileType)
                {
                    // 양계장(출발점)
                    case 0: //양계장 
                        break;

                    // 카드지급
                    case 1:
                        if (cardParent.childCount < 8)
                        {
                            // 랜덤하게 카드번호를 추출
                            // Card newCard = theGM.cards[UnityEngine.Random.Range(0, theGM.cards.Length)];
                            Card newCard = theGM.cards[UnityEngine.Random.Range(3, 4)];

                            // 팻말 아래 카드리스트에 복제하고 플레이어의 카드 목록에 추가함
                            var _card = Instantiate(cardPrefab, Vector3.zero, Quaternion.identity, cardParent);
                            _card.transform.localPosition = new Vector3(0f, 0f, 0f);
                            cards.Add(newCard);

                            StartCoroutine(GetCardShow());
                            yield return new WaitUntil(() => showCardFlag);
                            showCardFlag = false;

                            // 만약 통행료면제 카드라면 카드효과를 즉시 활성화.
                            if (newCard == theGM.cards[6])
                            {
                                exemptionFlag = true;
                                theGM.textManager.ShowText("플레이어" + theGM.nowPlayer.playerId + " 통행료 면제 효과 발동");
                                yield return new WaitForSeconds(3f);
                                theGM.textManager.HideText();
                            }
                        }
                        break;

                    // 텔레포트
                    case 2:
                        tpSelectFlag = true;
                        StartCoroutine(TeleportSetCoroutine());
                        break;

                    // 세금징수
                    case 3: //세금
                            //상대방 땅 갯수 *5 + 건물 갯수 * 10
                            //세금 징수하는 애니메이션이나, 영수증 띄워주면 좋을듯
                        playerMoney -= (groundCount * 5) + (buildingCount * 10);
                        break;

                    // 룰렛
                    case 4:
                        GameObject dCard = againstPlayer.cardParent.GetChild(UnityEngine.Random.Range(0, againstPlayer.cardParent.childCount)).gameObject;
                        dCard.transform.parent = cardParent;
                        break;

                }
                if (!tpSelectFlag)
                { //tp중일땐 일단 타일이 선택되기 전까지는 기다려야하기 때문에 탈출할 수 없음...
                  //특수 행동 후 턴을 넘김
                    theGM.NextTurnFunc();
                    //invisibleFlag = false;
                    this.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);
                }
            }
            VirtualCamera.SetActive(false);
        }
        else
        { //tpFlag일 경우 텔레포트함.

        }
    }

    IEnumerator TeleportSetCoroutine()
    {
        tpBack.SetActive(true);
        theGM.tpImg.SetActive(true);
        for (int i = 0; i < theTM.tiles.Length; i++)
        {
            theTM.tiles[i].cardActive = true; //모든 카드 클릭 가능하도록 미리 클릭하고 다음턴에 해당 위치로 이동.
        }

        yield return new WaitUntil(() => theGM.tpTile != null);

        for (int i = 0; i < theTM.tiles.Length; i++)
        {
            theTM.tiles[i].cardActive = false; //다시 클릭 못하도록 변경
        }

        tpTile = theGM.tpTile;
        theGM.tpTile = null;
        tpBack.SetActive(false);
        theGM.tpImg.SetActive(false);
        tpFlag = true;
        myTurn = false;
        theGM.NextTurnFunc();
    }

    IEnumerator TeleportCoroutine()
    {
        this.tileNum = int.Parse(tpTile.gameObject.name);
        this.tileToGo.Add(tpTile);
        //주석 처리된 코드는 그 자리로 순간이동하는 코드
        //만약, 순간이동처럼 보이고 싶다면 애니매이션 추가한 후 아래의 코드를 써야될듯.
        //this.transform.position = tileToGo[0].transform.TransformDirection(tileToGo[0].transform.Find("Pos").transform.position);
        //새로 작성한 코드는 그 자리로 moving하는것처럼 이동하는 코드
        Vector3 target = tileToGo[0].transform.Find("Pos").transform.position;
        while (tpMovingFlag)
        {
            this.transform.position = Vector3.MoveTowards(this.transform.position, target, Time.deltaTime * speed);
            // 여기서 뭔가 텔레포트하는 애니메이션이 필요할듯!
            if (this.transform.position == target)
            {
                tpMovingFlag = false;
            }
        }
        nowTile = tileToGo[0].GetComponent<Tile>();
        this.GetComponent<Animator>().SetInteger("Dir", nowTile.dir);
        this.tileToGo.RemoveAt(0);
        theGM.NextTurnFunc();
        tpFlag = false;
        yield return null;
    }

    IEnumerator NextTrunWait()
    { //현재 플레이어의 텔레포트가 완료되기 전에 상대 플레이어가 이동하는걸 방지하는 코드.
        yield return new WaitUntil(() => tpMovingFlag == false);
    }

    IEnumerator MovingCoroutine(Vector3 target)
    {
        this.gameObject.GetComponent<Animator>().SetInteger("Dir", nowTile.dir);
        this.gameObject.GetComponent<Animator>().SetBool("WalkFlag", true);

        if (theGM.nowPlayer.highSpeedFlag)
        {
            this.gameObject.GetComponent<Animator>().SetBool("FlyFlag", true);

            if (nowTile.dir == 5)
            {
                theCM.highMoveParticle.gameObject.transform.SetParent(theGM.nowPlayer.transform.GetChild(1));
            }
            else if (nowTile.dir == 2)
            {
                theCM.highMoveParticle.gameObject.transform.SetParent(theGM.nowPlayer.transform.GetChild(2));
            }
            else if (nowTile.dir == 3)
            {
                theCM.highMoveParticle.gameObject.transform.SetParent(theGM.nowPlayer.transform.GetChild(3));
            }
            else if (nowTile.dir == 4)
            {
                theCM.highMoveParticle.gameObject.transform.SetParent(theGM.nowPlayer.transform.GetChild(4));
            }
            theCM.highMoveParticle.gameObject.transform.localPosition = new Vector3(0f, 0f, 1f);
        }

        while (movingFlag)
        {
            this.transform.position = Vector3.MoveTowards(this.transform.position, target, Time.deltaTime * speed);
            yield return new WaitForEndOfFrame();
            if (this.transform.position == target)
            {
                movingFlag = false;
            }
        }
        yield return null;
    }

    // 획득한 카드를 게임화면에 띄워서 보여주는 코루틴
    public IEnumerator GetCardShow()
    {
        // GameManager에 만들어놓은 카드이미지프리팹을 카드를 띄워줄 위치에 있는 오브젝트에 복제
        // 이후 위치값, 스케일값, 스프라이트 이미지를 변경함
        var _card = Instantiate(theGM.onlyCardImg, Vector3.zero, Quaternion.identity, theGM.showCardObject.transform);
        _card.transform.localPosition = new Vector3(0f, 0f, 0f);
        _card.transform.localScale = new Vector3(20f, 20f, 20f);
        _card.GetComponent<SpriteRenderer>().sprite = theGM.nowPlayer.cards[cards.Count - 1].cardImg;

        // 3초 대기 이후 보여줬던 카드를 파괴하고 코루틴 탈출
        yield return new WaitForSeconds(3f);

        Destroy(_card);
        showCardFlag = true;
    }
}

