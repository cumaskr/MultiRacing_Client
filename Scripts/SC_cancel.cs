using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SC_cancel : MonoBehaviour
{
    public void OnClickCancel()
    {
        this.gameObject.SetActive(false);
    }

    public void OnClickPopupCancel()
    {
        Destroy(this.gameObject);
    }






}
