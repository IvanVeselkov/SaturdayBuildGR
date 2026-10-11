using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;
using static UnityEngine.GraphicsBuffer;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class Planet : MonoBehaviour
{
    [Header("Планеты")]
    public List<GameObject> Planets = new List<GameObject>();
    public List<GameObject> PlanetsChecker = new List<GameObject>();

    [Header("Проверка")]
    public GameObject Checky;
    public float first = -5f;
    public float last = 5f;
    public GameObject truee;
    public GameObject falsee;

    public UnityEvent Checking;
    public UnityEvent Return;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Check()
    {
        Vector3 target = new Vector3(first, Checky.transform.position.y, Checky.transform.position.z);

        Checking.Invoke();
        StartCoroutine(MoveOverTime(target, 1f));
    }

    private IEnumerator MoveOverTime(Vector3 target, float duration)
    {
        Vector3 start = Checky.transform.position;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            Checky.transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }

        Checky.transform.position = target;

        yield return new WaitForSeconds(1f);
        Plc();
        Return.Invoke();
    }

        public void Plc()
    {
        if (PlanetsChecker.Count != Planets.Count)
        {
            falsee.SetActive(true);
        }
        else
        {
            bool x = true;
            for (int i = 0; i < Planets.Count; i++)
            {
                if (PlanetsChecker[i] != Planets[i])
                {
                    x = false;
                    break;
                }
            }
            if (x == false)
            {
                falsee.SetActive(true);
            }
            else truee.SetActive(true);
        }
        
        PlanetsChecker.Clear();
        Checky.transform.position=new Vector3(last, Checky.transform.position.y, Checky.transform.position.z);
        Invoke(nameof(Close), 5f);
    }

    private void Close()
    {
        falsee.SetActive(false);
    }
}
