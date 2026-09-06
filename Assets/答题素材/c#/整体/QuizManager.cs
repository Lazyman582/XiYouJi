using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine.EventSystems;

public class QuizManager : MonoBehaviour
{
    [Header("界面引用")]
    public GameObject startUI;
    public GameObject quizUI;
    public GameObject selectPanel;
    public GameObject answerPanel;
    public GameObject resultPopup;

    [Header("阶段按钮")]
    public Button btnStage1;
    public Button btnStage2;

    [Header("答题 UI")]
    public Text txtQuestionCount;
    public Text txtQuestion;
    public Button[] optionBtns;
    public Button btnReturn;
    public Button btnNext;

    [Header("结果 UI")]
    public Text txtPopupResult;
    public Button btnPopupClose;

    [Header("动画")]
    public Animator quizAnimator;
    public string animQuizIdle = "答题";
    public string animSelectToAnswer = "进入";
    public string animAnswerToSelect = "退出";

    [Header("答题配置")]
    public int pickQuestionCount = 20;
    public float unlockThreshold = 0.7f;

    [Header("文字颜色配置")]
    public Color normalTextColor = Color.white;
    public Color hoverTextColor = Color.green;
    public Color rightTextColor = Color.green;
    public Color wrongTextColor = Color.red;

    // ��������
    private List<Question> currentQuizList;
    private int currentIndex;
    private int rightCount;
    private int totalAnswerCount;
    public bool isStage2Unlocked { get; private set; } = false;
    private bool isAnswered = false; // ��ǰ��Ŀ�Ƿ�������

    void Awake()
    {
        // This component starts on an inactive panel. Initializing in Awake makes
        // a same-frame SetActive(true) + OnClickEnterQuiz() sequence deterministic.
        // ��ʼ��״̬
        InitUIState();

        // �󶨰�ť�¼�
        btnStage1.onClick.AddListener(OnClickStage1);
        btnStage2.onClick.AddListener(OnClickStage2);
        btnReturn.onClick.AddListener(OnClickReturn);
        btnPopupClose.onClick.AddListener(OnPopupClose);
        btnNext.onClick.AddListener(OnClickNext);

        // ��ѡ������+����¼�
        for (int i = 0; i < optionBtns.Length; i++)
        {
            int idx = i;
            // �������
            optionBtns[i].onClick.AddListener(() => OnOptionClick(idx));
            // �����¼��󶨣�UGUIĬ���¼���
            EventTrigger trigger = optionBtns[i].GetComponent<EventTrigger>() ?? optionBtns[i].AddComponent<EventTrigger>();
            trigger.triggers.Clear();

            // ������
            EventTrigger.Entry enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((data) => { OnOptionHoverEnter(idx); });
            trigger.triggers.Add(enterEntry);

            // ����뿪
            EventTrigger.Entry exitEntry = new EventTrigger.Entry();
            exitEntry.eventID = EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((data) => { OnOptionHoverExit(idx); });
            trigger.triggers.Add(exitEntry);
        }
    }

    // ��ʼUI״̬����
    void InitUIState()
    {
        if (startUI != null)
            startUI.SetActive(true);
        quizUI.SetActive(false);
        selectPanel.SetActive(true);
        answerPanel.SetActive(false);
        resultPopup.SetActive(false);
        btnNext.gameObject.SetActive(false);
        btnStage2.interactable = false;
        ResetAllOptionColor();
    }

    // ����������
    public void OnClickEnterQuiz()
    {
        StopAllCoroutines();
        ResetQuizState();

        if (startUI != null)
            startUI.SetActive(false);

        if (quizUI != null)
            quizUI.SetActive(true);

        if (quizAnimator != null && !string.IsNullOrEmpty(animQuizIdle))
        {
            quizAnimator.Play(animQuizIdle, 0, 0f);
            quizAnimator.Update(0f);
        }

        if (selectPanel != null)
            selectPanel.SetActive(true);

        if (answerPanel != null)
            answerPanel.SetActive(false);

        if (resultPopup != null)
            resultPopup.SetActive(false);
    }

    #region �׶δ��⿪��
    void OnClickStage1() => StartQuiz(QuizLibrary.GetStage1FixedAndRandomList(pickQuestionCount));
    void OnClickStage2() => StartQuiz(QuizLibrary.Stage2QuizList(pickQuestionCount));

    void StartQuiz(List<Question> bankList)
    {
        if (bankList == null || bankList.Count == 0)
        {
            Debug.LogError("QuizManager: question list is empty.", this);
            return;
        }

        quizAnimator.Play(animSelectToAnswer);
        rightCount = 0;
        totalAnswerCount = 0;
        currentIndex = 0;
        isAnswered = false;

        currentQuizList = bankList;
        StartCoroutine(ShowAnswerPanelAfterAnim());
    }

    System.Collections.IEnumerator ShowAnswerPanelAfterAnim()
    {
        // Animator.Play takes effect during the next Animator update.
        yield return null;
        yield return new WaitForSeconds(quizAnimator.GetCurrentAnimatorStateInfo(0).length);
        //selectPanel.SetActive(false);
        answerPanel.SetActive(true);
        ShowCurrentQuestion();
    }
    #endregion

    #region ��Ŀ��ʾ & ��������
    void ShowCurrentQuestion()
    {
        // ����״̬
        isAnswered = false;
        btnNext.gameObject.SetActive(false);
        ResetAllOptionColor();
        SetOptionInteractable(true);

        if (currentIndex >= currentQuizList.Count)
        {
            QuizFinish();
            return;
        }

        // ������Ŀ����
        txtQuestionCount.text = $"{currentIndex + 1}/{currentQuizList.Count}";

        // ������Ŀ����
        Question q = currentQuizList[currentIndex];
        txtQuestion.text = q.questionText;
        optionBtns[0].GetComponentInChildren<Text>().text = q.optionA;
        optionBtns[1].GetComponentInChildren<Text>().text = q.optionB;
        optionBtns[2].GetComponentInChildren<Text>().text = q.optionC;
    }
    #endregion

    #region ѡ������Ч��
    void OnOptionHoverEnter(int index)
    {
        if (isAnswered) return; // �Ѵ����ֹ������ɫ
        optionBtns[index].GetComponentInChildren<Text>().color = hoverTextColor;
    }

    void OnOptionHoverExit(int index)
    {
        if (isAnswered) return;
        optionBtns[index].GetComponentInChildren<Text>().color = normalTextColor;
    }

    // ��������ѡ������Ϊ��ɫ
    void ResetAllOptionColor()
    {
        foreach (var btn in optionBtns)
        {
            btn.GetComponentInChildren<Text>().color = normalTextColor;
        }
    }
    #endregion

    #region �����߼� & �Դ���ɫ����
    void OnOptionClick(int selectIndex)
    {
        if (isAnswered) return;
        isAnswered = true;
        SetOptionInteractable(false);

        Question q = currentQuizList[currentIndex];
        totalAnswerCount++;

        // �Դ���ɫ��Ⱦ
        if (selectIndex == q.correctIndex)
        {
            // ��ԣ�ѡ�����ֱ���
            optionBtns[selectIndex].GetComponentInChildren<Text>().color = rightTextColor;
            rightCount++;
        }
        else
        {
            // �����ѡ�б�죬��ȷ����
            optionBtns[selectIndex].GetComponentInChildren<Text>().color = wrongTextColor;
            optionBtns[q.correctIndex].GetComponentInChildren<Text>().color = rightTextColor;
        }

        // �ж��Ƿ����һ�⣬�л���ť����
        if (currentIndex == currentQuizList.Count - 1)
        {
            btnNext.GetComponentInChildren<Text>().text = "结算";
        }
        else
        {
            btnNext.GetComponentInChildren<Text>().text = "下一题";
        }
        btnNext.gameObject.SetActive(true);
    }

    // ����ѡ���Ƿ�ɵ��
    void SetOptionInteractable(bool state)
    {
        foreach (var btn in optionBtns) btn.interactable = state;
    }
    #endregion

    #region ��һ�� / ���㰴ť�����Ż������������ʧ��
    void OnClickNext()
    {
        // �����ť������ʧ
        btnNext.gameObject.SetActive(false);

        currentIndex++;
        if (currentIndex >= currentQuizList.Count)
        {
            // ���㣺��ť��ʧ�󵯳��������
            QuizFinish();
        }
        else
        {
            // ��һ�⣺ֱ��ˢ����Ŀ
            ShowCurrentQuestion();
        }
    }
    #endregion

    #region ������� & ����
    void QuizFinish()
    {
        answerPanel.SetActive(false);
        float rate = totalAnswerCount > 0 ? (float)rightCount / totalAnswerCount : 0f;
        string rateStr = $"{rate * 100:F1}%";

        if (rate >= unlockThreshold)
        {
            txtPopupResult.text = $"本轮答题正确率为 {rateStr}，已解锁第二阶段测试。";
            isStage2Unlocked = true;
            btnStage2.interactable = true;
        }
        else
        {
            txtPopupResult.text = $"本轮答题正确率为 {rateStr}，达到要求后即可解锁第二阶段测试。";
        }
        resultPopup.SetActive(true);
    }
    #endregion

    #region �����߼�
    void OnClickReturn() => PlayBackToSelectAnim();
    void OnPopupClose()
    {
        resultPopup.SetActive(false);
        PlayBackToSelectAnim();
    }

    void PlayBackToSelectAnim()
    {
        quizAnimator.Play(animAnswerToSelect);
        StartCoroutine(ShowSelectPanelAfterAnim());
    }

    System.Collections.IEnumerator ShowSelectPanelAfterAnim()
    {
        yield return null;
        yield return new WaitForSeconds(quizAnimator.GetCurrentAnimatorStateInfo(0).length);
        answerPanel.SetActive(false);
        selectPanel.SetActive(true);
        ResetAllOptionColor();
    }
    #endregion
    /// <summary>�ⲿ���ã���ȫ���ô�������״̬�����غ�ָ���ʼ�ɾ�״̬</summary>
    public void ResetQuizState()
    {
        StopAllCoroutines();
        currentQuizList = null;
        currentIndex = 0;
        rightCount = 0;
        totalAnswerCount = 0;
        isAnswered = false;
        isStage2Unlocked = false;

        //UI�ص���ʼ״̬
        if (startUI != null)
            startUI.SetActive(true);
        quizUI.SetActive(false);
        selectPanel.SetActive(true);
        answerPanel.SetActive(false);
        resultPopup.SetActive(false);
        btnNext.gameObject.SetActive(false);
        btnStage2.interactable = false;

        ResetAllOptionColor();
    }
}
