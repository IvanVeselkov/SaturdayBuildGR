using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;

public class Counter : MonoBehaviour
{

    public int Count = 6;
    private int ehh = 0;

    public UnityEvent Event;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (ehh == Count)
        {
            Event.Invoke();
            gameObject.SetActive(false);
        }    
    }

    public void Plus()
    {
        ehh++;
    }
}
