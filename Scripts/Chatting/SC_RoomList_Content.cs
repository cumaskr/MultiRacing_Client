using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SC_RoomList_Content : MonoBehaviour
{

    Button m_roomButton;
    SC_Client p_clinet;

    // Start is called before the first frame update
    void Start()
    {

        m_roomButton = this.gameObject.GetComponent<Button>();

        p_clinet = GameObject.Find("Popup_RoomList").GetComponent<SC_Client>();

        //클릭하면 실행할 이벤트 함수를 등록한다.
        m_roomButton.onClick.AddListener(delegate { SentToClientRoomID(this.gameObject.name); });
    }


    void SentToClientRoomID(string _roomID)
    {
        p_clinet.MakePopup_RoomChatting(_roomID);
    }


}
