using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SC_ClientRoomChatting : MonoBehaviour
{

    public GameObject m_prefabRoomChattingContent_Parent;
    public GameObject m_prefabRoomChattingUser_Parent;

    public Button m_MapLeftButton;
    public Button m_MapRightButton;

    public Button m_Button_Ready;
    public Button m_Button_Start;

    public SC_Client p_client;



    public int m_mapLength = -1;
    public int m_mapIndex = -1;

    //본인이 나갔을때 호출된다.
    private void OnDisable()
    {
        //채팅글 다 지워준다.
        //왜냐하면 다시 뜰대 로드 한다.        
        int _ChatCount = m_prefabRoomChattingContent_Parent.transform.childCount;        

        for (int i = 0; i < _ChatCount; i++)
        {
            //Debug.Log(_contents[i].gameObject.name);
            Destroy(m_prefabRoomChattingContent_Parent.transform.GetChild(i).gameObject);
        }
        
        int _UserCount = m_prefabRoomChattingUser_Parent.transform.childCount;
        
        for (int i = 0; i < _UserCount; i++)
        {
            //Debug.Log(_contents[i].gameObject.name);

            //유저가 있던 자리라면
            if (m_prefabRoomChattingUser_Parent.transform.GetChild(i).gameObject.name.Length != 0)
            {
                //구지 필요없는 조건이지만 혹시 못찾았을경우가 생기면 지울때 에러가 뜨니 조건을 건다.
                if (m_prefabRoomChattingUser_Parent.transform.GetChild(i).gameObject.transform.Find("GameObject_Rotate"))
                {
                    //생성되어있는 3D차량
                    GameObject _userCar = m_prefabRoomChattingUser_Parent.transform.GetChild(i).gameObject.transform.Find("GameObject_Rotate").gameObject;
                    //생성되어있는 3D차량 삭제
                    Destroy(_userCar);
                }                
            }
            
            //유저공간도 삭제
            Destroy(m_prefabRoomChattingUser_Parent.transform.GetChild(i).gameObject);

            //인게임이아닌 대기실로 나갈때
            if (p_client.m_isDisableByGame == false)
            {
                //방에서 나갔을때는 초기화
                p_client.m_InfoManager.m_roomCarList.Clear();
            }
            
        }

        System.GC.Collect();

        //맵 왼쪽 오른쪽 수정 버튼 끈다.
        m_MapLeftButton.gameObject.SetActive(false);
        m_MapRightButton.gameObject.SetActive(false);

        //게임준비 버튼 활성화
        m_Button_Ready.gameObject.SetActive(true);
        //게임시작 버튼 비활성화
        m_Button_Start.gameObject.SetActive(false);
    }

    public void OnClickCancel()
    {
        p_client.C2S_SendMessage("ROOMOUT:");
                        
        p_client.RoomListShow();

        //채팅창 Off시킨다.
        this.gameObject.SetActive(false);

    }

    public void OnClickKick(string _kickUserNickname)
    {
        p_client.C2S_SendMessage("ROOMKICK:"+ _kickUserNickname);        
    }

    public void OnMapChangeToRightClick()
    {
        m_mapIndex += 1;

        if (m_mapIndex >= m_mapLength)
        {
            m_mapIndex = m_mapLength - 1;
        }

        p_client.C2S_SendMessage("ROOMREFRESH:" + m_mapIndex.ToString());
    }

    public void OnMapChangeToLeftClick()
    {
        m_mapIndex -= 1;

        if (m_mapIndex < 0)
        {
            m_mapIndex = 0;
        }

        p_client.C2S_SendMessage("ROOMREFRESH:" + m_mapIndex.ToString());
    }

    public void OnClickReady()
    {        
        p_client.C2S_SendMessage("UNITYSTATE:" + p_client.m_nickName + "/" + "READY");
    }

    public void OnClickStart()
    {
        p_client.C2S_SendMessage("UNITYSTATE:" + p_client.m_nickName + "/" + "START");        
    }

}
