using UnityEngine;

[System.Serializable]
public class Question
{
    [Header("题目内容")]
    public string questionText;
    [Header("三个选项")]
    public string optionA;
    public string optionB;
    public string optionC;
    [Header("正确答案 0=A,1=B,2=C")]
    public int correctIndex;
}
