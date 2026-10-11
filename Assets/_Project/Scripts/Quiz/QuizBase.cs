using System.Collections.Generic;
using System.Net.NetworkInformation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;

public class QuizBase : MonoBehaviour
{
   public List<GameObject> Questions = new List<GameObject>(6);
    int counter = 0;


    private void Start()
    {
        
    }

    public void Next()
    {
        Questions[counter].SetActive(false);
        counter++;
        Questions[counter].SetActive(true);
    }
}
