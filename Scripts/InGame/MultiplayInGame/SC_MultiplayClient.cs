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

public class SC_MultiplayClient : MonoBehaviour
{
    public SC_GameManager m_gameManager;

    private void OnDestroy()
    {
        //Receive 쓰레드는 항상 종료다.
        m_isConnected = false;

        if (m_client != null)
        {
            m_client.Close();
            Debug.Log("※ 클라이언트 소켓 연결 종료!");
        }

        //인게임이 끝나면 방들어갈때 넣은 유저들의 차 정보 지운다.
        if (m_InfoManager.m_roomCarList.Count != 0)
        {
            m_InfoManager.m_roomCarList.Clear();
        }        
    }
    //싱글톤 정보매니져
    SC_InfoManager m_InfoManager;

    //메세지 큐를 관리하는 우편함   
    MessageQueue m_messageList;


    TcpClient m_client;

    NetworkStream m_clientStream;

    Thread m_Thread_Receive;

    bool m_isConnected; 

    // Start is called before the first frame update
    void Start()
    {
        m_InfoManager = GameObject.Find("InfoManager").GetComponent<SC_InfoManager>();

        m_messageList = MessageQueue.GetInstance;

        if (m_InfoManager.m_client == null || m_InfoManager.m_clientStream == null)
        {
            Debug.LogError("소켓 연결 실패!");
            return;
        }

        m_client = m_InfoManager.m_client;
        m_clientStream = m_InfoManager.m_clientStream;
        m_isConnected = true;

        m_Thread_Receive = new Thread(new ThreadStart(ReceiveThreading));

        m_Thread_Receive.Start();

        
    }

    // Update is called once per frame
    void Update()
    {                       

        while (m_messageList.IsEmpty() == false)
        {            
            //우편함에서 데이타 꺼내기
            string _S2CMSG = m_messageList.GetData();

            //Debug.Log(_S2CMSG);

            if (_S2CMSG != null)
            {
                

                //우편함에 데이타가 있는 경우
                if (_S2CMSG.Equals(string.Empty) == false)
                {
                    //메세지 타입 구분자로 떼어내기
                    //0번 [메세지타입] 
                    //1번 [해당 타입의 데이터]
                    string[] _S2CMSG_TYPE = _S2CMSG.Split(':');

                    switch (_S2CMSG_TYPE[0])
                    {
                        //채팅방 유저공간 갱신
                        case "GAMESTATE":

                            //GAMESTATE:START       
                            //GAMESTATE:OVER
                            if (_S2CMSG_TYPE[1] == "START")
                            {
                                //클라이언트 들 게임 시작!
                                m_gameManager.StartCoroutine(m_gameManager.StartReadyGoCountdown());
                            }
                           
                            break;

                        case "GAMEFALL":

                            m_gameManager.SetCarForTriggerAnother(_S2CMSG_TYPE[1]);

                            break;

                        case "GAMERANK":

                            //GAMERANK:유저닉네임/m_spawnIndex
                            string[] _S2C_GAMERANK = _S2CMSG_TYPE[1].Split('/');

                            string _userNickname = _S2C_GAMERANK[0];
                            string _userIndex = _S2C_GAMERANK[1];

                            m_gameManager.m_carRankingList[_userNickname] = _userIndex;

                            break;


                        case "GAMEOVER":

                            //GAMEOVER:유저닉네임/GAMEOVER      -> 아직 5초카운트 재생중인 유저가 있다.
                            //GAMEOVER:GAMEOVER -> 모든게임종료
                           
                            string[] _S2C_GAMEOVER = _S2CMSG_TYPE[1].Split('/');

                            //만약 길이가 1이라면 GAMEOVER가 온것이다.
                            if (_S2C_GAMEOVER.Length == 1)
                            {

                                //잠시만 기다려주세요 or 5,3,2,1을 표시할 UI는 끈다.
                                m_gameManager.m_UISecondsToGameOver.SetActive(false);

                                //결과 정산한다.

                                //내가 골인 지점에 도착했다면
                                if (m_gameManager.m_carRankingList[m_InfoManager.m_nickname] == "100")
                                {
                                    //만약 자신의 랭킹이 1등이라면
                                    if (m_gameManager.m_playerRank == 1)
                                    {
                                        m_gameManager.m_resultMultiUI.transform.Find("Text_Money").GetChild(0).GetComponent<Text>().text = 1000.ToString();
                                    }
                                    //1등이 아니라면
                                    else
                                    {
                                        m_gameManager.m_resultMultiUI.transform.Find("Text_Money").GetChild(0).GetComponent<Text>().text = 100.ToString();
                                    }

                                    //결과창 보여준다.
                                    m_gameManager.ShowResult();
                                }
                                //골인지점에 도착하지 못했다면
                                else
                                {
                                    m_gameManager.m_resultMultiUI.transform.Find("Text_Money").GetChild(0).GetComponent<Text>().text = 0.ToString();

                                    //결과창 보여준다. 완주실패라고 뜬다.
                                    m_gameManager.ShowResult(true);
                                }
                                
                                
                            }
                            //2개의 문자열이 왔다면 유저닉네임/GAMEOVER가 온것이다.
                            else
                            {
                                //현재 플레이 중이라면
                                if (m_gameManager.m_isPlay == true)
                                {
                                    string _overUserNickname = _S2C_GAMEOVER[0];

                                    m_gameManager.m_carRankingList[_overUserNickname] = 100.ToString();

                                    //5초카운트 코루틴은 한번만 진행한다.
                                    if (m_gameManager.m_isSecondsToGameOverPlaying == false)
                                    {
                                        //5초 카운트 실행
                                        m_gameManager.StartCoroutine(m_gameManager.m_SecondsToGameOver);
                                    }
                                    
                                }
                                //골인 지점에 도착해 있는 유저라면
                                else
                                {
                                    //아무것도 하지 않는다.
                                }
                                
                            }

                            break;


                        //게임 동기화
                        case "GAMESYNCHRONIZE":

                            
                            //GAMESYNCHRONIZE: 유저닉네임 / X / Y / Z / X / Y / Z
                            string[] _S2C_GAMESYNCHRONIZE = _S2CMSG_TYPE[1].Split('/');

                            if (_S2CMSG_TYPE.Length == 2)
                            {
                                if (_S2C_GAMESYNCHRONIZE.Length == 7)
                                {
                                    string _userNickName = _S2C_GAMESYNCHRONIZE[0];
                                    Vector3 _position = new Vector3(float.Parse(_S2C_GAMESYNCHRONIZE[1]), float.Parse(_S2C_GAMESYNCHRONIZE[2]), float.Parse(_S2C_GAMESYNCHRONIZE[3]));
                                    Vector3 _forward = new Vector3(float.Parse(_S2C_GAMESYNCHRONIZE[4]), float.Parse(_S2C_GAMESYNCHRONIZE[5]), float.Parse(_S2C_GAMESYNCHRONIZE[6]));
                                    
                                    GameObject _anotherUser = m_gameManager.m_userList.transform.Find(_userNickName).gameObject;
                                    
                                    float _positionDelta = 0.0f;
                                    while (_positionDelta <= 1.0f)
                                    {
                                        Vector3 _newPosition = Vector3.Lerp(_anotherUser.transform.position, _position, _positionDelta);
                                        _positionDelta += 0.05f;
                                        _anotherUser.transform.position = _newPosition;
                                    }

                                    float _forwardDelta = 0.0f;
                                    while (_forwardDelta <= 1.0f)
                                    {
                                        Vector3 _newForward = Vector3.Lerp(_anotherUser.transform.forward, _forward, _forwardDelta);
                                        _forwardDelta += 0.05f;
                                        _anotherUser.transform.forward = _newForward;
                                    }

                                    m_gameManager.m_Count++;
                                    Debug.Log(_S2CMSG_TYPE[1] + "[" + m_gameManager.m_Count + "]");
                                    //_anotherUser.transform.position = _position;
                                    //_anotherUser.transform.forward = _forward;                                    

                                }
                            }

                            break;



                        default:

                            //서버에서 보낸 메세지를 판별 할수 없을경우 에러이다.
                            //Debug.Log("ErrorMSG[메세지타입 판별불가]:" + _S2CMSG);

                            break;
                    }

                }
            }
            

        }
    }


    private void ReceiveThreading() // 서버로 부터 값 받아오기
    {
        Debug.Log("================================================= SC_MultiplayClient 수신 쓰레드 시작=================================================");

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
                            //Debug.Log("수신" + msgList[i]);
                            m_messageList.PushData(msgList[i]);
                        }
                    }
                }
            }
        }

        //쓰레드 종료
        Debug.Log("================================================= SC_MultiplayClient 수신 쓰레드 종료=================================================");
    }

    public void C2S_SendMessage(String _msg)
    {

        _msg += "\n";

        byte[] _buffer = Encoding.UTF8.GetBytes(_msg);

        m_clientStream.Write(_buffer, 0, _buffer.Length);
        m_clientStream.Flush();

    }

}
