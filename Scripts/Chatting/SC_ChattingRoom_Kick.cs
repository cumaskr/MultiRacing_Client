using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SC_ChattingRoom_Kick : MonoBehaviour
{
    SC_ClientRoomChatting m_chattingRoom;
    Button m_kickButton;
    string m_myNickName = null;

    // Start is called before the first frame update
    void Start()
    {
        m_chattingRoom = GameObject.Find("Popup_RoomChatting").gameObject.GetComponent<SC_ClientRoomChatting>();
        m_kickButton = this.gameObject.GetComponent<Button>();
        m_myNickName = this.gameObject.transform.parent.gameObject.name;


        //클릭하면 실행할 이벤트 함수를 등록한다.
        m_kickButton.onClick.AddListener(delegate { SendToChattingRoom(m_myNickName); });
    }

    void SendToChattingRoom(string _myNickname)
    {
        m_chattingRoom.OnClickKick(_myNickname);
    }

    

}
