using UnityEngine;
using UnityEngine.Events;

public class EventsWater : MonoBehaviour
{
    public string Tag;
    public UnityEvent Event;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(Tag))
        {
            Event.Invoke();
        }

    }
}
