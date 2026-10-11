using UnityEngine;

public class PushPlanet : MonoBehaviour
{
    public Transform Place;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.CompareTag("Planet")) other.transform.position = Place.transform.position;


    }
}
