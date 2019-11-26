using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;


public class SC_InfoManager : MonoBehaviour
{
    public class SC_UserCar
    {


        //유저 차량 에셋 이름
        public string m_carAssetName = null;
        //유저 바퀴 에셋 이름
        public string m_wheelAssetName = null;
        //유저 날개 에셋 이름
        public string m_wingAssetName = null;
        
        public SC_UserCar(string _carAssetName, string _wheelAssetName, string _wingAssetName)
            {
                m_carAssetName = _carAssetName;
                m_wheelAssetName = _wheelAssetName;
                m_wingAssetName = _wingAssetName;
            }
    }

    //=======================================================================해당 유저 정보
    //유저 ID
    public string m_email = null;
    //유저 닉네임
    public string m_nickname = null;
    //유저 차량 에셋 이름
    public string m_carAssetName = null;
    //유저 바퀴 에셋 이름
    public string m_wheelAssetName = null;
    //유저 날개 에셋 이름
    public string m_wingAssetName = null;
    //유저가 들어간 방 이름(인게임에서 사용)
    public string m_mapName = null;
    //=======================================================================


    //=======================================================================멀티플레이에서 사용할 유저정보
    //<닉네임,차정보>        
    //채팅방에서의 관리
    //SC_Client -> UNITYJOIN에서 넣어준다.
    //SC_Client -> UNITYOUT에서 뺀다.
    //SC_ClientRoomChatting -> OnDestroy() / OnDisable() 방에서 나간 유저(대기실로)는 유저 차정보 초기화

    //인게임에서의 관리
    //SC_MultiplayClient -> OnDestroy()에서 데이터 지운다.

    //인게임에서 유저들의 차정보로 차를 생성해준다.    
    public Dictionary<string, SC_UserCar> m_roomCarList;

    //=======================================================================
    
    //===================================================================네트워크자원 관리

    //채팅방에서 멀티플레이 인게임으로 넘어가면서 넘겨준다.
    //그리고 이 변수를 SC_MultiplayClient.CS에서 받아간다.

    //방생성하면서 접속한 클라이언트 소켓
    public TcpClient m_client = null;
    //스트림도 가지고 있는다.
    public NetworkStream m_clientStream = null;

    //===================================================================

    //<맵AssetName,맵닉네임>
    public Dictionary<string, string> m_map;

    private void Awake()
    {        
        DontDestroyOnLoad(this.gameObject);

        m_map = new Dictionary<string, string>();
        m_roomCarList = new Dictionary<string, SC_UserCar>();

        //=====================================테스트 데이터==============================================
        //1)디버깅 할때 말고는 주석 걸것! 
        //2)해당 씬에 InfoManager오브젝트도 지울것
        //m_email = "cumaskr@naver.com";
        //m_nickname = "전우형";
        //m_map.Add("0", "BUSAN-SUNNY");
        //m_map.Add("1", "BUSAN-RAIN");
        //m_carAssetName = "spoartscar";
        //m_wheelAssetName = "michelin3";
        //m_wingAssetName = "default";
        //=========================================================================================================

    }


}
