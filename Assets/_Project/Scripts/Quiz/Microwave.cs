using UnityEngine;
using UnityEngine.Events;

public class Microwave : MonoBehaviour
{

    public UnityEvent Event;


    private void Awake()
    {
        Invoke("Lolol", 5f);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void Lolol()
    {
        Event.Invoke();
    }
}
