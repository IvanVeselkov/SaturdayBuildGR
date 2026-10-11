using UnityEngine;

public class chzh : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Zhopa()
    {
        transform.rotation=new Quaternion(0, 0, 0, transform.rotation.w); 
    }
}
