using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SC_ClientRoomMake : MonoBehaviour
{

    public SC_Client p_client;
    public GameObject m_Popup_RoomChatting;    //방채팅 오브젝트는 에디터에서 연결한다.


    public InputField m_roomName;
    public Dropdown m_Dropdown;

    public GameObject m_ErrorPopUp;
    public Text m_ErrorPupUp_Text;

    private void OnEnable()
    {
        //방제목 초기화
        m_roomName.text = null;
        
        //인원 2명
        //m_Dropdown = GetComponent<Dropdown>();

        m_Dropdown.options.Clear();
        for (int i = 2; i < 5; i++)//1부터 10까지
        {
            Dropdown.OptionData option = new Dropdown.OptionData();
            option.text = i.ToString() + "명";
            m_Dropdown.options.Add(option);
        }

        m_Dropdown.value = 0;
        m_Dropdown.RefreshShownValue();
    }


    public void MakePopup_RoomChatting()
    {
        //만약 방 이름을 입력을 하지 않았다면
        if (m_roomName.text.Length == 0)
        {
            m_ErrorPupUp_Text.text = "방 제목을 입력해주세요.";
            m_ErrorPopUp.SetActive(true);
            return;
        }


        //서버에 맵이름/방제목/총인원 를 넘겨준다.
        p_client.C2S_SendMessage("ROOMMAKE:0/"+ m_roomName.text+"/"+ (m_Dropdown.value + 2).ToString());
        
        //테스트 코드
        //m_client.C2S_SendMessage("ROOMMAKE:0/"+ m_roomName.text+"/"+ 0);

        ////채팅방 창을 켠다.(서버에서 조인 메세지 보낸다. 일관성을 위해 주석 걸음)
        //m_Popup_RoomChatting.SetActive(true);

        //방만들기창은 끈다.
        OnClickCancel();
    }

    public void OnClickCancel()
    {
        p_client.RoomListShow();

        //채팅창 Off시킨다.
        this.gameObject.SetActive(false);

    }

}
