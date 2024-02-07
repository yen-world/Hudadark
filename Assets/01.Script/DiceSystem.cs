using System.Collections;
using System.Collections.Generic;
using BackEnd;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DiceSystem : MonoBehaviour, IDragHandler, IEndDragHandler
{

    TurnSignScript theTSI;
    CardManager theCM;
    GameManager theGM;

    [SerializeField] PlayerManager thePlayer; //?? ?΄?΄? ? λ³?
    [SerializeField] Vector3 nowPos;//??¬ ?»λ§μ ?μΉ? ????₯
    [SerializeField] GameObject EggObj; //μ£Όμ¬? ?€λΈμ ?Έ
    [SerializeField] Text diceNumText; //μ£Όμ¬? ?κΈ? ??€?Έ
    [SerializeField] Animator EggAnimator; //? ?λ©μ΄?° ?? μ²΄ν¬ ?? λ³??
    [SerializeField] bool animatorFlag; //? ?λ©μ΄?°κ°? ???κ³ λ???° Update? λ‘μ§? ?€???€κΈ°μ?΄ μΆκ??.

    public bool diceFlag; //μ£Όμ¬?κ°? κ΅΄λ €μ‘λμ§? ??Έ?? ??κ·?

    #region Instance
    private static DiceSystem _instance;
    public static DiceSystem Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindObjectOfType(typeof(DiceSystem)) as DiceSystem;

            return _instance;
        }
    }
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        nowPos = this.transform.localPosition;
        theTSI = FindObjectOfType<TurnSignScript>();
        theCM = FindObjectOfType<CardManager>();
        theGM = FindObjectOfType<GameManager>();
    }

    void Update()
    {
        if (animatorFlag)
        {
            // EggAnimatorκ°? Finish? ?€?΄κ°?? ? ?λ©μ΄??΄ μ’λ£??€λ©?
            if (!EggAnimator.GetCurrentAnimatorStateInfo(0).IsName("Egg"))
            {
                EggObj.SetActive(false);
                // thePlayer.canMove = true;
                animatorFlag = false;
            }
        }

    }

    // ?»λ§μ ??κ·Έν? ? ?ΈμΆ?
    public void OnDrag(PointerEventData eventData)
    {
        // ?¬???? ? ?΄???λΉμ ?¬?©?΄ λͺ¨λ ??¬? ? μ£Όμ¬?λ₯? κ΅΄λ¦΄ ? ?κ²?(=> ?¬?©μ€μ΄?Όλ©? κ΅΄λ¦΄ ? ?κ²?)
        if (thePlayer.myTurn && theGM.penetrateComplete && theGM.laserComplete && theTSI.cursorPos == 1)
        {
            Vector3 yPos = new Vector3(0f, eventData.position.y, 0f);

            if (yPos.y < 200f) yPos = new Vector3(0f, 200f, 0f);
            else if (yPos.y > nowPos.y) yPos = new Vector3(0f, nowPos.y, 0f);

            this.transform.localPosition = new Vector3(nowPos.x, yPos.y, nowPos.z);
        }
    }

    // ?»λ§μ ?λ‘??? ? ?ΈμΆ?
    public void OnEndDrag(PointerEventData eventData1)
    {
        // ?¬???? ? ?΄???λΉμ ?¬?©?΄ λͺ¨λ ??¬? ? μ£Όμ¬?λ₯? κ΅΄λ¦΄ ? ?κ²?(=> ?¬?©μ€μ΄?Όλ©? κ΅΄λ¦΄ ? ?κ²?)
        if (thePlayer.myTurn && theGM.penetrateComplete && theGM.laserComplete && theTSI.cursorPos == 1)
        {
            // ?»λ§μ΄ ?Όμ²? ?μΉ? μ’νλ₯? ??΄?λ©? μ£Όμ¬?λ₯? κ΅΄λ¦Ό
            if (this.transform.localPosition.y < 470)
            {
                theTSI.cursorPos = 2;


                RollDice();
            }

            // ?»λ§μ ?μΉλ?? ?€? μ΄κΈ° ?μΉλ‘ ?? €??
            this.transform.localPosition = nowPos;
        }



    }

    public void RollDice()
    {
        int dNum = Random.Range(1, 9);
        DiceData dData = new(dNum, GameManager.Instance.turnIndex); //?λ²λ‘ ? ?‘?κΈ? ??΄ ?°?΄?° ?΄??€?
        byte[] data = ParsingManager.Instance.ParsingSendData(ParsingType.Dice, JsonUtility.ToJson(dData));
        Backend.Match.SendDataToInGameRoom(data);
    }

    public IEnumerator RollDiceCoroutine()
    {
        print("diceCoroutine Start");
        yield return new WaitUntil(() => diceFlag == true); //?λ²μ? μ£Όμ¬?κ°μ ????₯? ?κΉμ?? κΈ°λ€λ¦?.


        //??? ?€?΄?€λ₯? κ΅΄λ¦¬λ©? ?? ?΄?΄?κ²? ????₯?μ§?λ§?, ?΄? ? κ²μλ§€λ???? ????₯??΄? ??¬ ?΄? ?? ?΄?΄?κ²? ? ?Ή? κ²?.
        // GameManager.Instance.diceNum = Random.Range(1,9);
        // κ²μλ§€λ???? ????₯??¬ λ³??? EventManagerλ‘? ?΄?.
        //if() //?΄ ???Όλ©? ?? ?΄?΄ ?€?΄?€ ?? ????₯.
        thePlayer.diceNum = GameManager.Instance.diceNum;
        AudioManager.instance.Play("RollDice_Sound");

        thePlayer.diceNum = Random.Range(1, 9);

        // μ£Όμ¬?μ»¨νΈλ‘? μΉ΄λ ?¬?© ?, ?΄?Ή ?¨? ?ΈμΆ?
        if (thePlayer.lowerDiceFlag)
        {
            theCM.LowerDiceControl();
        }

        if (thePlayer.higherDiceFlag)
        {
            theCM.HigherDiceControll();
        }

        // μ£Όμ¬?λ₯? ??€?κ²? κ΅΄λ¦° ?€? text? ? ?©
        diceNumText.text = thePlayer.diceNum.ToString();

        // ??λ‘? ?ΉκΈ°μ?€ ??€?Έλ₯? ?¨κΈ°κ³ , μ£Όμ¬?λ₯? ??±??κ³?, animatorFlagλ₯? trueλ‘? μΌμ ??°?΄?Έλ¬Έμ ?€?΄κ°?κ²ν¨
        thePlayer.downInformationText.gameObject.SetActive(false);
        EggObj.SetActive(true);
        animatorFlag = true;
        diceFlag = false;
    }
}
