using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeleteWheelImage : MonoBehaviour
{

    IEnumerator DeleteOnsecond()
    {
        yield return new WaitForSeconds(1.0f);
        Destroy(this.gameObject);

    }


    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(DeleteOnsecond());
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
