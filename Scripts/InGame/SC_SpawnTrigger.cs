using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SC_SpawnTrigger : MonoBehaviour
{
    public SC_GameManager m_gameManager; 
    private void OnTriggerEnter(Collider other)
    {
        m_gameManager.CheckTrigger(this.gameObject.name);
    }

}
