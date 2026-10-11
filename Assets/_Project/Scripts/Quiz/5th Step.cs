using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using static Unity.Burst.Intrinsics.X86.Avx;
using Unity.VisualScripting;

public class thStep : MonoBehaviour
{

    public UnityEvent Event;

    public GameObject spoon;
    public GameObject coolspoon;
    public GameObject sugar;
    public GameObject Cup;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        StartCoroutine(SixSeve());
    }
    IEnumerator SixSeve()
    {
           yield return new WaitForSeconds(4f);
        gameObject.GetComponent<BoxCollider>().enabled = true;

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Spoon"))
        {
            spoon.SetActive(false);
            coolspoon.SetActive(true);
            XRGrabInteractable few = Cup.GetComponent<XRGrabInteractable>();
            few.enabled = false;
            sugar.GetComponent<Animator>().Play("sugar");
            Invoke(nameof(SecondStepTimerEnd), 7f);
        }
    }
    private void SecondStepTimerEnd()
    {
        spoon.SetActive(true);
        coolspoon.GetComponent<Animator>().enabled = false;
        coolspoon.SetActive(false);
        sugar.SetActive(false);
        Event.Invoke();
    }
}
