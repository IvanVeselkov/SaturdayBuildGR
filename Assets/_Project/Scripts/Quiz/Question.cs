using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class Question : MonoBehaviour
{
    public GameObject truee;
    public GameObject falsee;

    [Header("Варианты ответов")]
    public List<Toggle> Variants = new List<Toggle>();

    [Header("Правильный ответлол")]
    public List<bool> Correct = new List<bool>();
    void Awake()
    {

    }

    public void CheckForAnswers()
    {
        bool check = true;

        for (int i = 0; i < Variants.Count; i++) {
            if (Variants[i].isOn != Correct[i])
            {
                check = false;
                break;
            }
        }

        if (check == true) {
            truee.SetActive(true);
        }
        else falsee.SetActive(true);
    }
}
