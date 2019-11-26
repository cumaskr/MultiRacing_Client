using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Text;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SC_Client : MonoBehaviour
{
    //싱글톤 정보매니져
    public SC_InfoManager m_InfoManager;

    //방생성후 다시 로비로 갔는지 혹은 인게임으로 갔는지에 따라 client소켓을 끄고 키냐가 정해지기 때문에 구별할 변수가 필요하다.
    public bool m_isDisableByGame = false;
   
    //========================================================멤버 변수

    TcpClient m_client;

    NetworkStream m_clientStream;

    Thread ReceiveThread;

    bool m_isConnected;

    MessageQueue m_messageList;    //메세지 큐를 관리하는 우편함    

    public string m_nickName = null;

    //========================================================오브젝트 연결

    //방이없을경우 메세지를 띄어준다.
    public GameObject m_RoomListEmpty;

    //안내메세지를 띄울 필요가 있을경우 띄어준다.
    public GameObject m_ErrorPopUp;
    public Text m_ErrorPupUp_Text;



    //LobbyManager에서 멀티플레이를 누르면 이 스크립트가 실행된다
    //=======================방목록===========================

    public GameObject m_prefabRoomListContent;    //방 오브젝트는 에디터에서 연결한다.
    public GameObject m_prefabRoomListContent_Parent;

    //=======================방만들기=========================
    public GameObject m_Popup_RoomMake;    //방 오브젝트는 에디터에서 연결한다.


    //=======================방채팅=========================
    public GameObject m_Popup_RoomChatting;    //방에 JOIN할경우(ROOMJOIN) or 방에서 강퇴당할때(ROOMKICK) 사용 / ROOMREFRESH에서 채팅방에 맵정보 갱신한다.(UNITYJOIN / UNITYOUT)
    public ScrollRect m_prefabRoomChattingScrollView;
    public GameObject m_prefabRoomChattingContent;
    public GameObject m_prefabRoomChattingContent_Parent;

    public GameObject m_Popup_RoomChattingUser;
    public GameObject m_Popup_RoomChattingUser_parent;

    public Image m_prefabRoomChattingMapImage;
    public Text m_prefabRoomChattingMapName;
    public Text m_prefabRoomChattingRoomName;
    public Text m_prefabRoomChattingUserCount;


    //=======================클라이언트 소켓 연결 설계에 대한 주석=========================
    //1)m_isDisableByGame라는 Bool 변수로 소켓연결후 로비로 가는지/인게임으로 가는지 판별한다.
    //2)로비로 갈경우 [Receive 쓰레드][클라이언트 소켓]을 연결 해제한다.

    //3)게임으로 갈경우 [Receive 쓰레드]는 종료 [클라이언트 소켓]과 네트워크 자원은 해제하지않는다.
    //4)InfoManager 싱글톤 클래스에서 네트워크 자원을 넘겨받는다.
    //5)멀티플레이 씬에서 InfoManager의 네트워크 자원을 넘겨받는다.(씬 전환으로 인해 네트워크 자원변수를 잃기때문에 잠시 가지고 있을 다리가 필요했다.)
    //6)멀티플레이 씬에서는 새로운 [Receive 쓰레드]를 생성해서 메세지를 받고 [네트워크 자원]도 그대로 연결 종료 없이 사용한다. -> 유저들이 방에 들어 가있기때문에 연결 해재 하면 모든 정보가 날아간다.


    private void OnDestroy()
    {        
        //Receive 쓰레드는 항상 종료다.
        m_isConnected = false;

        //방에서 나갔을때만 실행할 로직 인게임시에는 로직이 안돈다.
        if (m_isDisableByGame == false && m_client != null)
        {            
            m_client.Close();
            m_client = null;
            m_InfoManager.m_client = null;
            Debug.Log("※ 클라이언트 소켓 연결 종료!");

            //방에서 나갔을때는 초기화
            m_InfoManager.m_roomCarList.Clear();
        }        
    }
    
    private void OnDisable()
    {        
        //방목록 생성되어있는 오브젝트들은 지워준다.
        //왜냐하면 다시 뜰대 로드 한다.
        Transform[] _contents = m_prefabRoomListContent_Parent.GetComponentsInChildren<Transform>();
        int _size = _contents.Length;

        for (int i = 1; i < _size; i++)
        {
            //Debug.Log(_contents[i].gameObject.name);
            Destroy(_contents[i].gameObject);
        }

        //Receive 쓰레드는 항상 종료다.
        m_isConnected = false;

        //방에서 나갔을때만 실행할 로직 인게임시에는 로직이 안돈다.
        if (m_isDisableByGame == false && m_client != null)
        {
            Debug.Log("클라이언트 소켓 연결 종료!");
            m_client.Close();
            m_client = null;
            m_InfoManager.m_client = null;
            //방에서 나갔을때는 초기화
            m_InfoManager.m_roomCarList.Clear();
        }
    }

    private void OnEnable()
    {
        m_isDisableByGame = false;

        m_InfoManager = GameObject.Find("InfoManager").GetComponent<SC_InfoManager>();

        m_messageList = MessageQueue.GetInstance;
        
        //방목록을 담을 스크롤뷰를 가져온다. 방이 추가되면 맨밑으로 셋팅하기위해서 가져온다.
        //m_scroll_RoomList = transform.Find("Scroll View_RoomList").gameObject.GetComponent<ScrollRect>();

        //방이 생성될 부모 오브젝트
        //m_prefabRoomListContent_Parent = transform.Find("Scroll View_RoomList").Find("Viewport").Find("Content").gameObject;

        //============================================================================레이싱게임 서버에 접속한다. 대기방에 접속한다.

        String IP = "49.247.131.35"; // 접속 할 서버 아이피를 입력

        int port = 8888; // 포트

        m_client = new TcpClient();

        m_client.Connect(IP, port);

        m_clientStream = m_client.GetStream();

        m_isConnected = true;

        ReceiveThread = new Thread(new ThreadStart(Receive));

        ReceiveThread.Start();

        Debug.Log("[ 49.247.131.35 서버 접속 ]");

        //============================================================================처음 접속했을때 필요한 메세지를 보낸다.

        //NICKNAME:유저닉네임/차AssetName/바퀴AssetName/날개AssetName
        C2S_SendMessage("NICKNAME:"+ m_InfoManager.m_nickname+ "/" +m_InfoManager.m_carAssetName+ "/" +m_InfoManager.m_wheelAssetName+ "/" +m_InfoManager.m_wingAssetName);
        
        //방목록을 받는다.
        C2S_SendMessage("ROOMLISTSHOW:");

        //============================================================================        
        m_nickName = m_InfoManager.m_nickname;



        //============================================================================인게임에 넘어갈때 가지고 갈 네트워크 자원 넘겨준다.     
        m_InfoManager.m_client = m_client;
        m_InfoManager.m_clientStream = m_clientStream;

    }
    private void Update()
    {                

        while (m_messageList.IsEmpty() == false)
        {
            //동기화 구간(여러 쓰레드가 동시에 접근할때 줄 세운다.)
            //lock (lockObject)
            //{
            //}

            //우편함에서 데이타 꺼내기
            string _S2CMSG = m_messageList.GetData();
            
            //우편함에 데이타가 있는 경우
            if (!_S2CMSG.Equals(string.Empty))
            {
                //메세지 타입 구분자로 떼어내기
                //0번 [메세지타입] 
                //1번 [해당 타입의 데이터]
                string[] _S2CMSG_TYPE = _S2CMSG.Split(':');
                
                switch (_S2CMSG_TYPE[0])
                {

                    //대기실에서 방 목록 갱신
                    case "ROOMLISTSHOW":

                        //생성 되어있는 방이 없는 경우
                        if (_S2CMSG_TYPE[1].Length == 0)
                        {
                            //생성되어 있는 방이 없습니다. 문구 출력
                            m_RoomListEmpty.SetActive(true);
                        }
                        //생성 되어있는 방이 있는경우
                        else
                        {
                            //생성되어 있는 방이 없습니다. 문구는 끈다.
                            m_RoomListEmpty.SetActive(false);

                            //ROOMLISTSHOW:방ID/맵이름/방제목/현재인원/총인원 메세지
                            string[] _S2CMSG_DATA = _S2CMSG_TYPE[1].Split('/');

                            string _roomId = _S2CMSG_DATA[0];
                            string _roomMapName = _S2CMSG_DATA[1];
                            string _roomName = _S2CMSG_DATA[2];
                            string _roomUserCount = _S2CMSG_DATA[3];
                            string _roomAllCount = _S2CMSG_DATA[4];

                            //방목록에 표시할 방 오브젝트 생성
                            GameObject _room = Instantiate(m_prefabRoomListContent, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
                            _room.transform.SetParent(m_prefabRoomListContent_Parent.transform, false);
                            _room.name = _roomId;
                            _room.transform.Find("Text_No").GetComponent<Text>().text = _roomId;
                            _room.transform.Find("Text_RoomMap").GetComponent<Text>().text = m_InfoManager.m_map[_roomMapName];
                            _room.transform.Find("Image_RoomMap").GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/Map/" + _roomMapName);
                            _room.transform.Find("Text_RoomName").GetComponent<Text>().text = _roomName;
                            _room.transform.Find("Text_UserCount").GetComponent<Text>().text = _roomUserCount + "/" + _roomAllCount;
                        }
                        
                        break;

                    //채팅방안의 메세지 표시
                    case "MSG":

                        //채팅메세지 생성 코드
                        GameObject _msg = Instantiate(m_prefabRoomChattingContent, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
                        _msg.transform.SetParent(m_prefabRoomChattingContent_Parent.transform, false);
                        _msg.GetComponent<Text>().text = _S2CMSG_TYPE[1];

                        //채팅 스크롤뷰 맨 밑으로 셋팅 코드
                        Canvas.ForceUpdateCanvases();
                        m_prefabRoomChattingScrollView.verticalNormalizedPosition = 0.0f;                
                        Canvas.ForceUpdateCanvases();

                        break;

                    //채팅방에서 강퇴 당할 유저 
                    //1. X자 버튼에 붙어있는 SC_ChattingRoom_Kick에서 ROOMKICK:유저이름 을 호출
                    //2. 서버에서 ROOMKICK을 전체 방인원에 전송
                    //3. 여기서 그  ROOMKICK메세지를 받고 나 자신라면 내가 나를 내보낸다.
                    case "ROOMKICK":

                        string _kickUserNickName = _S2CMSG_TYPE[1];

                        //만약 나자신이 강퇴당해야 할 유저라면
                        if (m_InfoManager.m_nickname == _kickUserNickName)
                        {
                            //나를 방에서 뺀다.
                            C2S_SendMessage("ROOMOUT:");

                            RoomListShow();

                            //채팅창 Off시킨다.
                            m_Popup_RoomChatting.gameObject.SetActive(false);
                        }


                        break;


                    //채팅방 열기
                    case "ROOMJOIN":

                        //정상적으로 조인했다면 => ROOMJOIN:
                        if (_S2CMSG_TYPE[1].Length == 0)
                        {
                            //채팅방을 연다.
                            m_Popup_RoomChatting.SetActive(true);                            
                        }
                        //문제가 생겨 안내 메세지가 들어온다.(방 인원초과 / 방존재 x) => ROOMJOIN:000문제가 발생하였습니다.                       
                        else
                        {
                            //안내 메세지를 켠다.
                            m_ErrorPopUp.SetActive(true);
                            m_ErrorPupUp_Text.text = _S2CMSG_TYPE[1];
                        }

                        break;

                   //채팅방 유저공간 갱신
                    case "UNITYJOIN":

                        //서버에서 JoinToRoom()에서 모든 유저정보 보낸다.
                        //UNITYJOIN:방안의 유저들(닉네임,차,날개,바퀴)/유저인덱스/방장인덱스/총인원
                        //전우형,5,3,4/0/0/2
                        //ex)
                        //Length 6 
                        //UserAllCount = 5
                        //MasterIndex = 4
                        //UserIndex = 3
                        //UserLength = 3

                        string[] _S2C_UNITYJOIN = _S2CMSG_TYPE[1].Split('/');

                        //총인원(이 변수가 있어야 하는 이유는 방안에 모든 유저가 접속 해 있지 않은경우도 있기때문임
                        //예를들면 5명 방에 3명의 유저만 있다면 유저수 갖고 빈 유저공간을 생성 할 수 없다.
                        int _userAllCount = Convert.ToInt32(_S2C_UNITYJOIN[_S2C_UNITYJOIN.Length - 1]);

                        //방장 닉네임 인덱스
                        int _masterNickNameIndex = Convert.ToInt32(_S2C_UNITYJOIN[_S2C_UNITYJOIN.Length - 2]);

                        //유저 닉네임 인덱스
                        int _userNicknameIndex = Convert.ToInt32(_S2C_UNITYJOIN[_S2C_UNITYJOIN.Length - 3]);

                        //방안에 접속 해있는 유저는 몇명인지 -> 이 숫자 미만으로 반복문을 돈다. -> 다음부터 이렇게 짜지말자 헷갈리는 코드다.
                        int _userCount = _S2C_UNITYJOIN.Length - 3;





                        //방장닉네임                        
                        String _masterNickName = _S2C_UNITYJOIN[_masterNickNameIndex].Split(',')[0];

                        //유저닉네임
                        String _userNickName = _S2C_UNITYJOIN[_userNicknameIndex].Split(',')[0];
                        
                        //서버에서 보낸 [ UNITYJOIN ]가 클라이언트 자신이라면 방에 모든 유저 유니티오브젝트 생성
                        //방안에 처음 들어갈때(방생성 or 방조인)
                        if (m_InfoManager.m_nickname == _userNickName)
                        {
                            //방 총 인원수 만큼 돈다.
                            for (int i = 0; i < _userAllCount; i++)
                            {                              
                                //총 인원 수 만큼  유저 공간을 만든다.
                                GameObject _unityUser = Instantiate(m_Popup_RoomChattingUser, new Vector3(i * 300, 0, 0), Quaternion.identity) as GameObject;
                                _unityUser.transform.SetParent(m_Popup_RoomChattingUser_parent.transform, false);

                                //유저공간의 이름은 공백으로 한다.
                                _unityUser.gameObject.name = "";

                                //현재 방안에 유저들 정보 갱신
                                if (i < _userCount)
                                {                                    
                                    string[] _userinfo = _S2C_UNITYJOIN[i].Split(',');

                                    string _Nickname = _userinfo[0];
                                    string _CarAssetname = _userinfo[1];
                                    string _WheelAssetname = _userinfo[2];
                                    string _WingAssetname = _userinfo[3];
                                    
                                    //해당 유저공간의 이름은 닉네임으로 한다.
                                    _unityUser.gameObject.name = _Nickname;
                                    //해당 유저의 닉네임을 표시한다.
                                    _unityUser.transform.Find("Text_NickName").GetComponent<Text>().text = _Nickname;
                                    _unityUser.transform.Find("Text_NickName").gameObject.SetActive(true);

                                    if (_unityUser.gameObject.name == m_InfoManager.m_nickname)
                                    {
                                        _unityUser.GetComponent<Image>().color = new Color32(180, 100, 255, 255);
                                    }


                                    //해당 유저가 방장이라면
                                    if (_unityUser.gameObject.name == _masterNickName)
                                    {
                                        _unityUser.transform.Find("Image_Master").gameObject.SetActive(true);
                                    }
                                    //해당 유저가 방장이 아니라면
                                    else
                                    {
                                        _unityUser.transform.Find("Image_Master").gameObject.SetActive(false);
                                    }

                                    //해당 유저의 상태 표시는 끈다.
                                    _unityUser.transform.Find("Image_Ready").gameObject.SetActive(false);

                                    //======================================================유저 아이템 표시======================================================

                                    //InfoManager에 데이터를 넘겨준다.(인게임에서 필요)
                                    m_InfoManager.m_roomCarList.Add(_Nickname, new SC_InfoManager.SC_UserCar(_CarAssetname, _WheelAssetname, _WingAssetname));

                                    //----------------------------------------------------------------------------------차량  

                                    GameObject _userSpaceCar = Instantiate(Resources.Load("Car/" + _CarAssetname), new Vector3(0, -30, 0), Quaternion.identity) as GameObject;
                                    _userSpaceCar.gameObject.name = "GameObject_Rotate";
                                    _userSpaceCar.transform.localRotation = Quaternion.Euler(0, 90.0f, 0);
                                    _userSpaceCar.transform.SetParent(_unityUser.transform, false);
                                    _userSpaceCar.transform.localScale = new Vector3(35.0f, 35.0f, 35.0f);

                                    //----------------------------------------------------------------------------------바퀴 

                                    MeshRenderer[] _wheelList = _userSpaceCar.transform.Find("Wheel_Mesh").GetComponentsInChildren<MeshRenderer>();

                                    foreach (MeshRenderer _wheel in _wheelList)
                                    {
                                        _wheel.material = Resources.Load<Material>("Wheel/" + _WheelAssetname);
                                    }

                                    //----------------------------------------------------------------------------------날개  

                                    _userSpaceCar.transform.Find(_WingAssetname).gameObject.SetActive(true);

                                    //===========================================================================================================================

                                }
                                //유저가 아닌 빈공간
                                else
                                {
                                    //아무것도 하지 않는다.(애초에 세팅 되어있슴)
                                }                                                               
                            }                            
                        }
                        //서버에서 보낸 [ UNITYJOIN ]가 클라이언트 자신이 아니라면 해당 유저 정보만 갱신 / 방안에 다른사람이 들어오는 경우
                        else
                        {
                            //생성 되어있는 유저 공간만큼 돈다.
                            for (int i = 0; i < m_Popup_RoomChattingUser_parent.transform.childCount; i++)
                            {
                                //유저 공간을 하나씩 체크한다.
                                GameObject _unityUser = m_Popup_RoomChattingUser_parent.transform.GetChild(i).gameObject;

                                //비어있는 유저공간에 유저를 넣는다.
                                if (_unityUser.gameObject.name.Length == 0)
                                {                                  
                                    //유저공간의 이름을 들어오는 유저의 닉네임으로 한다.
                                    _unityUser.gameObject.name = _userNickName;
                                    _unityUser.transform.Find("Text_NickName").GetComponent<Text>().text = _userNickName;
                                    _unityUser.transform.Find("Text_NickName").gameObject.SetActive(true);

                                    //들어오는 유저가 방장이라면
                                    if (_unityUser.gameObject.name == _masterNickName)
                                    {
                                        _unityUser.transform.Find("Image_Master").gameObject.SetActive(true);
                                        Debug.Log("이런경우는 생길수가 없다.");
                                    }
                                    //들어오는 유저가 방장이 아니라면
                                    else
                                    {
                                        _unityUser.transform.Find("Image_Master").gameObject.SetActive(false);
                                    }

                                    //해당 유저의 상태 표시는 끈다.
                                    _unityUser.transform.Find("Image_Ready").gameObject.SetActive(false);

                                    //======================================================유저 아이템 표시======================================================

                                    string[] _userinfo = _S2C_UNITYJOIN[_userNicknameIndex].Split(',');                                    
                                    string _userCarAssetname = _userinfo[1];
                                    string _userWheelAssetname = _userinfo[2];
                                    string _userWingAssetname = _userinfo[3];


                                    //InfoManager에 데이터를 넘겨준다.(인게임에서 필요)
                                    m_InfoManager.m_roomCarList.Add(_userNickName, new SC_InfoManager.SC_UserCar(_userCarAssetname, _userWheelAssetname, _userWingAssetname));

                                    //----------------------------------------------------------------------------------차량  

                                    GameObject _userSpaceCar = Instantiate(Resources.Load("Car/" + _userCarAssetname), new Vector3(0, -30, 0), Quaternion.identity) as GameObject;
                                    _userSpaceCar.gameObject.name = "GameObject_Rotate";
                                    _userSpaceCar.transform.localRotation = Quaternion.Euler(0, 90.0f, 0);
                                    _userSpaceCar.transform.SetParent(_unityUser.transform, false);
                                    _userSpaceCar.transform.localScale = new Vector3(35.0f, 35.0f, 35.0f);

                                    //----------------------------------------------------------------------------------바퀴 

                                    MeshRenderer[] _wheelList = _userSpaceCar.transform.Find("Wheel_Mesh").GetComponentsInChildren<MeshRenderer>();

                                    foreach (MeshRenderer _wheel in _wheelList)
                                    {
                                        _wheel.material = Resources.Load<Material>("Wheel/" + _userWheelAssetname);
                                    }

                                    //----------------------------------------------------------------------------------날개  

                                    _userSpaceCar.transform.Find(_userWingAssetname).gameObject.SetActive(true);

                                    //===========================================================================================================================




                                    //다른사람입장에서는 지금 들어오려는 유저만 유니티 유저표시에 갱신하면 끝이다.
                                    break;
                                }                                
                            }
                        }

                        //방장의 경우에만 예외처리 방장 클라이언트에만 표시해주어야하기때문에 따로 조건문을 뺀다.
                        if (m_InfoManager.m_nickname == _masterNickName)
                        {
                            //생성 되어있는 유저 공간만큼 돈다.
                            for (int i = 0; i < m_Popup_RoomChattingUser_parent.transform.childCount; i++)
                            {
                                //유저 공간을 하나씩 체크한다.
                                GameObject _unityUser = m_Popup_RoomChattingUser_parent.transform.GetChild(i).gameObject;

                                //방장이 아닌 유저들한테는 강퇴버튼을 활성화한다.
                                if (_unityUser.gameObject.name.Length != 0 && _unityUser.gameObject.name != _masterNickName)
                                {
                                    _unityUser.transform.Find("Image_Kick").gameObject.SetActive(true);
                                }
                            }

                            //맵변경 버튼 활성화
                            m_Popup_RoomChatting.GetComponent<SC_ClientRoomChatting>().m_MapLeftButton.gameObject.SetActive(true);
                            m_Popup_RoomChatting.GetComponent<SC_ClientRoomChatting>().m_MapRightButton.gameObject.SetActive(true);

                            //게임준비 버튼 비활성화
                            m_Popup_RoomChatting.GetComponent<SC_ClientRoomChatting>().m_Button_Ready.gameObject.SetActive(false);
                            //게임시작 버튼 활성화
                            m_Popup_RoomChatting.GetComponent<SC_ClientRoomChatting>().m_Button_Start.gameObject.SetActive(true);
                        }


                        break;

                        //채팅방 유저공간 갱신
                        //여기는 다른 사람이 나갔을때 호출된다. 내가 나갔을때는 SC_ClientRoomChatting의 OnDisable에서 지워준다.
                    case "UNITYOUT":

                        //UNITYOUT:유저닉네임/방장닉네임
                        string[] _S2CUNITYOUT_DATA = _S2CMSG_TYPE[1].Split('/');

                        //나가려는 유저 닉네임
                        String _userNickName_unityOut = _S2CUNITYOUT_DATA[0];
                        //방장 닉네임
                        String _masterNickName_unityOut = _S2CUNITYOUT_DATA[1];
                        
                        //유저공간에서 나가려는 유적닉네임의 공간을 가져온다.
                        GameObject _unityOutUser = m_Popup_RoomChattingUser_parent.transform.Find(_userNickName_unityOut).gameObject;
                        //유저공간의 이름을 공백으로 바꾼다.
                        _unityOutUser.gameObject.name = "";


                        if (_unityOutUser.transform.Find("GameObject_Rotate"))
                        {
                            //생성되어있는 3D차량 삭제
                            Destroy(_unityOutUser.transform.Find("GameObject_Rotate").gameObject);
                        }
                        
                        //그외에 UI 초기화
                        _unityOutUser.transform.Find("Text_NickName").GetComponent<Text>().text = "";
                        _unityOutUser.transform.Find("Text_NickName").gameObject.SetActive(false);
                        _unityOutUser.transform.Find("Image_Master").gameObject.SetActive(false);
                        _unityOutUser.transform.Find("Image_Kick").gameObject.SetActive(false);
                        _unityOutUser.transform.Find("Image_Ready").gameObject.SetActive(false);

                        //방장 위임으로 인한 유저공간 이미지 변경
                        //유저가 나감으로서 방장이 바뀌는데 이때 들어온 방장 이름의 오브젝트의 방장 이미지를 킨다.
                        GameObject _unityNewMaster = m_Popup_RoomChattingUser_parent.transform.Find(_masterNickName_unityOut).gameObject;
                        _unityNewMaster.transform.Find("Image_Master").gameObject.SetActive(true);

                        //유저가 래디를 하고있다가 방장이 되는경우도 있으니 방지
                        _unityNewMaster.transform.Find("Image_Ready").gameObject.SetActive(false);

                        //방장의 경우에만 예외처리 방장 클라이언트에만 표시해주어야하기때문에 따로 조건문을 뺀다.
                        if (m_InfoManager.m_nickname == _masterNickName_unityOut)
                        {
                            //생성 되어있는 유저 공간만큼 돈다.
                            for (int i = 0; i < m_Popup_RoomChattingUser_parent.transform.childCount; i++)
                            {
                                //유저 공간을 하나씩 체크한다.
                                GameObject _unityUser = m_Popup_RoomChattingUser_parent.transform.GetChild(i).gameObject;

                                //방장이 아닌 유저들한테는 강퇴버튼을 활성화한다.
                                if (_unityUser.gameObject.name.Length != 0 && _unityUser.gameObject.name != _masterNickName_unityOut)
                                {
                                    _unityUser.transform.Find("Image_Kick").gameObject.SetActive(true);
                                }
                            }

                            //맵변경 버튼 활성화
                            m_Popup_RoomChatting.GetComponent<SC_ClientRoomChatting>().m_MapLeftButton.gameObject.SetActive(true);
                            m_Popup_RoomChatting.GetComponent<SC_ClientRoomChatting>().m_MapRightButton.gameObject.SetActive(true);
                            //게임준비 버튼 비활성화
                            m_Popup_RoomChatting.GetComponent<SC_ClientRoomChatting>().m_Button_Ready.gameObject.SetActive(false);
                            //게임시작 버튼 활성화
                            m_Popup_RoomChatting.GetComponent<SC_ClientRoomChatting>().m_Button_Start.gameObject.SetActive(true);                            


                        }

                        //InfoManager에 데이터를 넘겨준다.(인게임에서 필요)
                        m_InfoManager.m_roomCarList.Remove(_userNickName_unityOut);

                        break;


                    //채팅방 유저들 READY or START판별하여 표시 혹은 게임시작
                    case "UNITYSTATE":

                        //UNITYSTATE:유저닉네임/READY, UNITYSTATE:유저닉네임/START
                        string[] _S2CMSG_DATA_UNITYSTATE = _S2CMSG_TYPE[1].Split('/');

                        //구분자로 2개로 안나누어진다면 방장한테 보내는 안내 메세지이다.
                        if (_S2CMSG_DATA_UNITYSTATE.Length == 1)
                        {

                            GameObject _unityStateUser = m_Popup_RoomChattingUser_parent.transform.Find(m_InfoManager.m_nickname).gameObject;

                            //방장만
                            if (_unityStateUser.transform.Find("Image_Master").gameObject.activeSelf == true)
                            {
                                //안내 메세지를 켠다.
                                m_ErrorPopUp.SetActive(true);
                                m_ErrorPupUp_Text.text = _S2CMSG_TYPE[1];
                            }
                        }
                        //2개 이상으로 나누어진다면 READY를 하던 START를 하던 처리가 필요한 메세지이다.
                        else
                        {
                            if (_S2CMSG_DATA_UNITYSTATE[1] == "READY")
                            {
                                GameObject _unityReadyUser = m_Popup_RoomChattingUser_parent.transform.Find(_S2CMSG_DATA_UNITYSTATE[0]).gameObject;
                                GameObject _unityReady = _unityReadyUser.transform.Find("Image_Ready").gameObject;
                                //켜져있다면 래디표시를 끈다.
                                if (_unityReady.activeSelf)
                                {
                                    _unityReadyUser.transform.Find("Image_Ready").gameObject.SetActive(false);
                                }
                                //꺼져있다면 ㄹㄷ표시를 킨다.
                                else
                                {
                                    _unityReadyUser.transform.Find("Image_Ready").gameObject.SetActive(true);
                                }                                
                            }
                            //게임 시작이라면 모든 유저가 인게임 씬으로 화면 전환을 한다.
                            else if (_S2CMSG_DATA_UNITYSTATE[1] == "START")
                            {
                                //채팅방에서 사용한 Receive쓰레드를 지우기위해 true로 바꾼다.
                                m_isDisableByGame = true;

                                //해당 방에서 선택한 맵으로 넘어갈 씬 이름을 만든다.(인게임)
                                string _sceneName = "scene_multiplaymap" + m_InfoManager.m_mapName;

                                
                                //=============================================유저정보


                                //씬을 넘어간다.
                                SceneManager.LoadScene(_sceneName);
                            }
                        }                       
                        break;


                    //채팅방 정보 갱신
                    case "ROOMREFRESH":

                        //ROOMREFRESH:맵이름/방제목/현재인원/총인원
                        string[] _S2CMSG_DATA_ROOMREFERESH = _S2CMSG_TYPE[1].Split('/');
                        
                        string _roomMapName_ROOMREFERESH = _S2CMSG_DATA_ROOMREFERESH[0];
                        string _roomName_ROOMREFERESH = _S2CMSG_DATA_ROOMREFERESH[1];
                        string _roomUserCount_ROOMREFERESH = _S2CMSG_DATA_ROOMREFERESH[2];
                        string _roomAllCount_ROOMREFERESH = _S2CMSG_DATA_ROOMREFERESH[3];

                        m_InfoManager.m_mapName = _roomMapName_ROOMREFERESH;

                        m_prefabRoomChattingMapImage.sprite = Resources.Load<Sprite>("Images/Map/" + _roomMapName_ROOMREFERESH);
                        m_prefabRoomChattingMapName.text = m_InfoManager.m_map[_roomMapName_ROOMREFERESH];
                        m_prefabRoomChattingRoomName.text = _roomName_ROOMREFERESH;
                        m_prefabRoomChattingUserCount.text = _roomUserCount_ROOMREFERESH + "/" + _roomAllCount_ROOMREFERESH;
                        
                        //방장의 경우 맵을 바꾸어야 하기때문에 맵정보 갱신한다.
                        m_Popup_RoomChatting.GetComponent<SC_ClientRoomChatting>().m_mapIndex = Convert.ToInt32(_roomMapName_ROOMREFERESH);
                        m_Popup_RoomChatting.GetComponent<SC_ClientRoomChatting>().m_mapLength = m_InfoManager.m_map.Count;

                        break;
                        
                    default:

                        //서버에서 보낸 메세지를 판별 할수 없을경우 에러이다.
                        Debug.Log("ErrorMSG[메세지타입 판별불가]:" + _S2CMSG_TYPE[0] + _S2CMSG_TYPE[1]);

                        break;
                }
                               
            }

        }
    }


    private void Receive() // 서버로 부터 값 받아오기
    {
        Debug.Log("================================================= SC_Client 수신 쓰레드 시작=================================================");

        //쓰레드를 종료 시키기 위한 태그값
        while (m_isConnected)
        {
            if (m_clientStream.DataAvailable && m_client.Connected)
            {
                int BUFFERSIZE = m_client.ReceiveBufferSize;
                byte[] _receiveBuffer = new byte[BUFFERSIZE];
                m_clientStream.Read(_receiveBuffer, 0, BUFFERSIZE);
                string tempStr = Encoding.UTF8.GetString(_receiveBuffer);

                tempStr = tempStr.Replace("\0", "");

                string[] msgList = tempStr.Split('\n');

                if (msgList.Length > 0)
                {
                    for (int i = 0; i < msgList.Length; i++)
                    {
                        //구분자로 나누었을때 ""이 공간이 하나 남기때문에 예외처리함
                        if (msgList[i] != "")
                        {
                            Debug.Log("수신" + msgList[i]);
                            m_messageList.PushData(msgList[i]);                            
                        }
                    }
                }
            }
        }

        //쓰레드 종료
        Debug.Log("================================================= SC_Client 수신 쓰레드 종료=================================================");
    }

    public void C2S_SendMessage(String _msg)
    {   
        
        _msg += "\n";

        byte[] _buffer = Encoding.UTF8.GetBytes(_msg);

        m_clientStream.Write(_buffer, 0, _buffer.Length);
        m_clientStream.Flush();

    }


    public void MakePopup_RoomMake()
    {        
        m_Popup_RoomMake.SetActive(true);
    }


    public void MakePopup_RoomChatting(String _roomID)
    {
        //서버에 들어갈 방번호 를 넘겨준다.
        C2S_SendMessage("ROOMJOIN:"+ _roomID);
        
    }

    public void RoomListShow()
    {
        //방목록 생성되어있는 오브젝트들은 지워준다.
        //왜냐하면 다시 뜰대 로드 한다.
        Transform[] _contents = m_prefabRoomListContent_Parent.GetComponentsInChildren<Transform>();
        int _size = _contents.Length;

        for (int i = 1; i < _size; i++)
        {            
            Destroy(_contents[i].gameObject);
            //Debug.Log(_contents[i].gameObject.name);
        }

        //방목록을 받는다.
        C2S_SendMessage("ROOMLISTSHOW:");
    }

    

}
