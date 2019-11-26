using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SC_Inputfield : MonoBehaviour
{


    InputField m_inputField;

    public SC_Client m_client;

    void Start()
    {
        m_inputField = gameObject.GetComponent<InputField>();

        m_inputField.onEndEdit.AddListener(delegate { ChatEnter(); });                        
    }


    public void ChatEnter()
    {
        //엔터키 조건을 건 이유는 채팅을 치고 바깥쪽을 클릭할때도 보내지기 떄문이다.
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (m_inputField.text.Length > 0)
            {
                m_client.C2S_SendMessage("MSG:"+m_inputField.text);
            }
        }

        m_inputField.text = null;
    }

    
}
