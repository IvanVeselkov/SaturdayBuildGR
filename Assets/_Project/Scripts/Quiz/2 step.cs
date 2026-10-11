using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using static UnityEngine.GraphicsBuffer;
using UnityEngine.Events;

public class step : MonoBehaviour
{
    public UnityEvent Event;
    public Transform place;
    public GameObject water;
    public GameObject glasswater;


    private GameObject cup;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Cup"))
        {
            this.GetComponent<BoxCollider>().enabled = false;
            cup = other.gameObject;
            cup.GetComponent<Rigidbody>().isKinematic = true;
            cup.transform.position=place.transform.position;
            cup.transform.rotation=place.transform.rotation;
            SecondStep();
        }
    }

    private void SecondStep()
    {
        water.SetActive(true);
        XRGrabInteractable few = cup.GetComponent<XRGrabInteractable>();
        few.enabled = false;
        Animator animator = glasswater.GetComponent<Animator>();
        glasswater.SetActive(true);
        animator.Play("wata");
        Invoke(nameof(SecondStepTimerEnd), 15f);
    }
    private void SecondStepTimerEnd()
    {
        Event.Invoke();
        water.SetActive(false);
        XRGrabInteractable few = cup.GetComponent<XRGrabInteractable>();
        few.enabled = true;
        cup.GetComponent <Rigidbody>().isKinematic = false;
    }
}
