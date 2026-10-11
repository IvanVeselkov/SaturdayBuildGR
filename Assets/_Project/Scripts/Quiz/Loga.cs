using UnityEngine;
using UnityEngine.Events;

public class Loga : MonoBehaviour
{
    public string tag;
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
        if (other.gameObject.CompareTag(tag))
        {
            other.gameObject.SetActive(false);
            this.gameObject.SetActive(false);
            Event.Invoke();
        }
    }
}
