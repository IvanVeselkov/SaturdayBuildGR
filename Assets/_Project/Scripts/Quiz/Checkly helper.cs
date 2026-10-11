using System.Collections.Generic;
using UnityEngine;

public class Checklyhelper : MonoBehaviour
{
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
        if(other.gameObject.name == "Checkly")
        {
            Planet planet = other.GetComponent<Planet>();
            List<GameObject> list = planet.PlanetsChecker;
            list.Add(this.gameObject);
        }
    }
}
