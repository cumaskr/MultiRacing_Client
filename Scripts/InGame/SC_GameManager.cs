using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using UnityEngine.SceneManagement;
//==========================================================================================

public class SC_GameManager : MonoBehaviour
{
    //싱글톤 정보매니져
    public SC_InfoManager m_InfoManager;

    //========================================================오브젝트 연결

    //통과지점 스폰 리스트
    public List<GameObject> m_spawnList;

    //한바퀴 돌았을때 시간
    public Text m_laptime;

    //게임 전체시간
    public Text m_wholetime;

    //현재바퀴
    public Text m_around;

    //총 돌아야할 바퀴
    public Text m_maxaround;

    //레디! 고! UI
    public GameObject m_readygoCountdown;
    //결과창 UI
    public GameObject m_resultui;
    //인게임 UI
    public GameObject m_ingameUI;

    //시네마틱 장면 UI -> ESC : SKIP    
    public GameObject m_canvasCinematic;

    //미니맵을 찍는 카메라
    public GameObject m_minimapCamera;

    //시네마틱 카메라가 이동할 Transform 배열
    public Transform[] m_cinematicList;
    //시네마틱 카메라가 쳐다봐야할 오브젝트 위치(맵 가운데 Position)
    public Transform m_cinematiclookAt;

    //========================================================멤버 변수
    //차 뒤를 따라 다니는 메인카메라
    public Camera m_mainCamera;

    //플레이어
    public GameObject m_player;

    //====================================분/초/밀리세컨드
    int m_minuate;
    int m_second;
    float m_millisecond;

    //====================================1바퀴 돌았을때 분/초/밀리세컨드
    int m_lapminuate;
    int m_lapsecond;
    float m_lapmillisecond;

    //현재 바퀴수
    int m_nowAroundNumber;
    //총 바퀴수
    int m_maxAroundNumber;

    //============================================ 1/3(현재등수/총인원)
    public int m_playerRank;
    int m_rankAllCount;


    //현재 맵 이름
    string m_mapassetName;

    //유저가 다음통과 해야할 벽 인덱스
    public int m_spawnIndex;

    //시네마틱 카메라가 끝나고 조정하여 게임 플레이 가능한지
    public bool m_isPlay;

    //시네마틱 카메라 종료 여부
    bool m_isCinematicSkip = false;

    //멀티플레이/싱글플레이 판별
    public bool m_isMultiPlay = false;

    //===================================================================================================멀티플레이 전용
    //멀티플레이시 스폰될 위치
    //(-8,0,0) / (-3,0,0) / (3,0,0) / (8,0,0)

    public SC_MultiplayClient m_client;

    public GameObject m_userList;

    //결과창 UI
    public GameObject m_resultMultiUI;


    //유저들의 랭킹을 산출하기위해 필요한 변수
    //닉네임, 해당 유저가 다음 가야할 트랙 이름(0,1,2,3,4,5,6,7,8.... m_spawnIndex)
    public Dictionary<string, string> m_carRankingList;

    //===================================================================================================FPS30
    IEnumerator m_FPS30;
    bool m_isFPS30Play;

    //=================================================================FPS체크===========================
    private float TimeLeft = 1.0f;
    private float nextTime = 0.0f;
    public int m_Count = 0;
    //===================================================================================================

    //===================================================================================================게임종료 5초 카운트
    public IEnumerator m_SecondsToGameOver;
    public bool m_isSecondsToGameOverPlaying;
    //잠시만기다려주세요 / 혹은 5초 표시 UI
    public GameObject m_UISecondsToGameOver;

    private void OnDestroy()
    {
        m_isPlay = false;
        StopCoroutine(m_FPS30);
    }

    IEnumerator FPS30()
    {
        Debug.Log("FPS30 코루틴 시작");
        while (m_isPlay)
        {            
            //Debug.Log("GAMESYNCHRONIZE:" + m_InfoManager.m_nickname + "/" + (m_player.transform.position.x).ToString("F7") + "/" + (m_player.transform.position.y).ToString("F7") + "/" + (m_player.transform.position.z).ToString("F7") + "/" + (m_player.transform.forward.x).ToString("F7") + "/" + (m_player.transform.forward.y).ToString("F7") + "/" + (m_player.transform.forward.z).ToString("F7") + "[" + m_Count + "]");
            m_client.C2S_SendMessage("GAMESYNCHRONIZE:" + m_InfoManager.m_nickname + "/" + (m_player.transform.position.x).ToString("F7") + "/" + (m_player.transform.position.y).ToString("F7") + "/" + (m_player.transform.position.z).ToString("F7") + "/" + (m_player.transform.forward.x).ToString("F7") + "/" + (m_player.transform.forward.y).ToString("F7") + "/" + (m_player.transform.forward.z).ToString("F7"));
            yield return new WaitForSeconds(0.03f);
        }
        Debug.Log("FPS30 코루틴 종료");
    }

    IEnumerator SecondsToGameOver()
    {
        Debug.Log("m_5SecondsToGameOver 코루틴 시작");

        m_isSecondsToGameOverPlaying = true;

        //5,4,3,2,1 을 표시할 UI
        m_UISecondsToGameOver.SetActive(true);

        int i = 6;

        while (i > 1)
        {
            i -= 1;

            m_UISecondsToGameOver.transform.Find("Text").GetComponent<Text>().text = i.ToString();

            yield return new WaitForSeconds(1.0f);

        }

        m_laptime.text = m_lapminuate.ToString() + " : " + m_lapsecond.ToString() + " : " + m_lapmillisecond.ToString("F0");

        StartCoroutine(ChangeCamera(m_mainCamera.gameObject, m_player.transform.Find("FinishCamera").gameObject));

        //충돌 무시 하게한다.
        m_player.GetComponent<SC_CarController>().SetCarForTriggerMine(true);

        m_isPlay = false;

        m_ingameUI.gameObject.SetActive(false);

        m_client.C2S_SendMessage("GAMEOVER:" + m_InfoManager.m_nickname + "/" + "GAMEOVER");

        //게임종료 루틴
        Debug.Log("m_5SecondsToGameOver 코루틴 종료");
    }


    void Init()
    {
        //InfoManager 가져온다.
        m_InfoManager = GameObject.Find("InfoManager").GetComponent<SC_InfoManager>();

        //싱글플레이
        if (m_InfoManager.m_client == null)
        {
            m_isMultiPlay = false;
        }
        //멀티플레이
        else
        {
            m_isMultiPlay = true;
        }


        //맵 몇번 맵인지 받아온다.
        m_mapassetName = m_InfoManager.m_mapName;

        //맵 못받아오면 에러 처리한다.
        if (m_mapassetName == "null")
        {
            Debug.LogError("Error : map is not setting!");
            return;
        }

        //====================================================================================시분초 초기화
        m_second = 0;
        m_minuate = 0;
        m_millisecond = 0.0f;

        m_lapsecond = 0;
        m_lapminuate = 0;
        m_lapmillisecond = 0.0f;
        //====================================================================================

        //====================================================================================바퀴수 셋팅
        m_nowAroundNumber = 0;
        m_maxAroundNumber = 1;
        m_around.text = m_nowAroundNumber.ToString();
        m_maxaround.text = m_maxAroundNumber.ToString();
        //====================================================================================


        m_spawnIndex = 0;

        //시네마틱 카메라가 나오기 때문에, 아직 플레이 하면 안된다.
        m_isPlay = false;

        m_FPS30 = FPS30();
        m_isFPS30Play = false;

        m_SecondsToGameOver = SecondsToGameOver();
        m_isSecondsToGameOverPlaying = false;

    }

    private void Start()
    {
        //UI 및 기본 셋팅
        Init();


        //싱글플레이 / 멀티플레이 나뉘어서 로직이 들어간다.=====================================================================

        //싱글플레이
        if (m_isMultiPlay == false)
        {
            //====================================================================================현재 랭킹/총 인원
            m_playerRank = 1;
            m_rankAllCount = 1;
            m_ingameUI.transform.Find("Text_Rank").GetChild(0).GetComponent<Text>().text = m_playerRank.ToString();
            m_ingameUI.transform.Find("Text_Rank").GetChild(2).GetComponent<Text>().text = m_rankAllCount.ToString();
            //====================================================================================


            //m_player에 유저 데이터를 받아와서 생성해준다.
            StartCoroutine(S2C_UserData());

            //시네마틱 카메라 발동
            StartCoroutine(CinematicCamera(m_mainCamera.gameObject, m_cinematicList));
        }
        //멀티플레이
        else
        {
            m_carRankingList = new Dictionary<string, string>();


            int _index = 0;

            SortedDictionary<string, SC_InfoManager.SC_UserCar> sorted_List = new SortedDictionary<string, SC_InfoManager.SC_UserCar>(m_InfoManager.m_roomCarList);

            //유저들 생성해준다.
            foreach (KeyValuePair<string, SC_InfoManager.SC_UserCar> _user in sorted_List)
            {
                SC_InfoManager.SC_UserCar _userCar = _user.Value;
                string _nickname = _user.Key;
                string _carAssetName = _userCar.m_carAssetName;
                string _wheelAssetName = _userCar.m_wheelAssetName;
                string _wingAssetName = _userCar.m_wingAssetName;

                //랭킹 관련해서 필요하다
                m_carRankingList.Add(_nickname, m_spawnIndex.ToString());


                GameObject _gameCar = Instantiate(Resources.Load("Car/" + _carAssetName + "_InGame"), new Vector3(-8 + 5 * _index, 0, 0), Quaternion.identity) as GameObject;
                _gameCar.name = _nickname;
                _gameCar.transform.SetParent(m_userList.transform);

                //==================================================================================바퀴

                MeshRenderer[] _wheelList = _gameCar.transform.Find("Wheel_Mesh").GetComponentsInChildren<MeshRenderer>();

                foreach (MeshRenderer _wheel in _wheelList)
                {
                    _wheel.material = Resources.Load<Material>("Wheel/" + _wheelAssetName);
                }

                //==================================================================================날개

                _gameCar.transform.Find(_wingAssetName).gameObject.SetActive(true);

                //자기 차인경우
                if (_nickname == m_InfoManager.m_nickname)
                {
                    m_player = _gameCar;
                }
                else
                {
                    Destroy(_gameCar.GetComponent<SC_CarController>());
                    Destroy(_gameCar.GetComponent<Rigidbody>());
                }

                _index++;
            }


            //====================================================================================현재 랭킹/총 인원
            m_playerRank = 1;
            m_rankAllCount = m_carRankingList.Count;            
            m_ingameUI.transform.Find("Text_Rank").GetChild(0).GetComponent<Text>().text = m_playerRank.ToString();
            m_ingameUI.transform.Find("Text_Rank").GetChild(2).GetComponent<Text>().text = m_rankAllCount.ToString();
            //====================================================================================



            //시네마틱 카메라 발동
            StartCoroutine(CinematicCamera(m_mainCamera.gameObject, m_cinematicList));

        }

        //=======================================================================================================================
    }

    private void Update()
    {
        //싱글플레이 / 멀티플레이 나뉘어서 로직이 들어간다.=====================================================================

        //싱글플레이
        if (m_isMultiPlay == false)
        {
            //시네마틱 카메라 스킵 할수있는 로직
            if (m_isCinematicSkip == false && Input.GetKeyDown(KeyCode.Escape))
            {
                m_isCinematicSkip = true;
            }


            //플레이 할수있다면 미니맵 카메라 작동
            if (m_isPlay)
            {
                Vector3 _backup = m_player.transform.position + m_player.transform.forward * -1 * 50;
                _backup.y = 50.0f;
                m_minimapCamera.transform.position = _backup;
                m_minimapCamera.transform.rotation = Quaternion.Euler(35.0f, m_player.transform.rotation.eulerAngles.y, 0.0f);

                //분/초/밀리초 계산하여 InGameUI에 표시해준다.
                CalculateWholeTime();

                var dot = Vector3.Dot(m_player.transform.forward, new Vector3(0, 0, 0));



            }
        }
        //멀티플레이
        else
        {
            //시네마틱 카메라 스킵 할수있는 로직
            if (m_isCinematicSkip == false && Input.GetKeyDown(KeyCode.Escape))
            {
                m_isCinematicSkip = true;
            }

            //플레이 할수있다면 미니맵 카메라 작동
            if (m_isPlay)
            {

                //랭킹 산출 알고리즘===================================================================================
                int _myRank = 1;
                foreach (KeyValuePair<string, string> _user in m_carRankingList)
                {
                    int _userIndex = System.Convert.ToInt32(_user.Value);

                    //나자신은 제외하고 랭킹 산출을한다.
                    if (_user.Key != m_InfoManager.m_nickname)
                    {
                        //만약 같은 트랙이라면 거리로 랭킹 산출
                        if (m_spawnIndex == _userIndex)
                        {
                            int _index = m_spawnIndex % m_spawnList.Count;
                            float myDistance = (m_player.transform.position.x - m_spawnList[_index].transform.position.x) * (m_player.transform.position.x - m_spawnList[_index].transform.position.x) + (m_player.transform.position.z - m_spawnList[_index].transform.position.z) * (m_player.transform.position.z - m_spawnList[_index].transform.position.z);
                            float userDistance = (m_userList.transform.Find(_user.Key).position.x - m_spawnList[_index].transform.position.x) * (m_userList.transform.Find(_user.Key).position.x - m_spawnList[_index].transform.position.x) + (m_userList.transform.Find(_user.Key).position.z - m_spawnList[_index].transform.position.z) * (m_userList.transform.Find(_user.Key).position.z - m_spawnList[_index].transform.position.z);

                            //내가 목표지점 보다 더멀다면 랭킹이 낮아진다.
                            if (myDistance > userDistance)
                            {
                                _myRank++;
                            }
                        }
                        //다른유저가 더 앞에있다면 내랭킹 ++을한다. ex)1등에서 2등이된다.
                        else if (m_spawnIndex < _userIndex)
                        {
                            _myRank++;
                        }
                    }
                }


                //====================================================================================현재 랭킹 갱신
                m_playerRank = _myRank;
                m_ingameUI.transform.Find("Text_Rank").GetChild(0).GetComponent<Text>().text = m_playerRank.ToString();
                //====================================================================================


                //=====================================================================================================

                Vector3 _backup = m_player.transform.position + m_player.transform.forward * -1 * 50;
                _backup.y = 50.0f;
                m_minimapCamera.transform.position = _backup;
                m_minimapCamera.transform.rotation = Quaternion.Euler(35.0f, m_player.transform.rotation.eulerAngles.y, 0.0f);

                //분/초/밀리초 계산하여 InGameUI에 표시해준다.
                CalculateWholeTime();

                //GAMESYNCHRONIZE:유저닉네임/X/Y/Z/X/Y/Z
                Vector3 _position = m_player.transform.position;
                Vector3 _forward = m_player.transform.forward;

                if (m_isFPS30Play == false)
                {
                    StartCoroutine(m_FPS30);
                    m_isFPS30Play = true;
                }



                ////1초마다 실행
                if (Time.time > nextTime)
                {
                    nextTime = Time.time + TimeLeft;

                    m_Count = 0;
                    Debug.Log("=====================================================================================================================================================");
                }
            }
        }

        //=======================================================================================================================

    }



    //m_isPlay가 참이면 실행
    void CalculateWholeTime()
    {
        //Time.deltaTime 단위 1초

        //밀리초 부터 계산해서 하나씩 값을 올린다.
        //원래 초로 변환하려면 1000을 곱해야하지만, 필요한건 초 뒤에 소수점에서 0~9까지 빠르게 올라가는 숫자가 필요하다.
        m_millisecond += Time.deltaTime * 10;
        m_lapmillisecond += Time.deltaTime * 10;

        // 밀리세컨드를 표현하는데에 0 ~ 9 만 필요하다. 그리고 초기화시 초를 올려준다.
        if (m_millisecond > 9)
        {
            m_millisecond = 0.0f;
            m_second += 1;
        }

        if (m_lapmillisecond > 9)
        {
            m_lapmillisecond = 0.0f;
            m_lapsecond += 1;
        }

        // 60초가 지나면 분을 올려주고 초기화
        if (m_second >= 60)
        {
            m_minuate += 1;
            m_second = 0;
        }

        // 60초가 지나면 분을 올려주고 초기화
        if (m_lapsecond >= 60)
        {
            m_lapminuate += 1;
            m_lapsecond = 0;
        }

        m_wholetime.text = m_minuate.ToString() + " : " + m_second.ToString() + " : " + m_millisecond.ToString("F0");

    }

    //결과화면에서 사용할 카메라로 변경
    public IEnumerator ChangeCamera(GameObject _cameraFrom, GameObject _cameraTo)
    {
        float _moverate = 0.0f;

        //카메라 모드를 바꾸어준다.
        _cameraFrom.GetComponent<SC_CameraController>().m_mode = SC_CameraController.E_MODE.MOVE;


        while (_moverate <= 0.1f)
        {
            Vector3 _movePos = Vector3.Lerp(_cameraFrom.transform.position, _cameraTo.transform.position, _moverate);

            _moverate += 0.001f;

            _cameraFrom.transform.position = _movePos;
            _cameraFrom.transform.LookAt(m_player.transform);

            yield return new WaitForSeconds(0.001f);
        }

        //카메라 모드를 바꾸어준다.
        _cameraFrom.GetComponent<SC_CameraController>().m_mode = SC_CameraController.E_MODE.FINISH;

        yield break;

    }


    //결과창에서 로비로 가기 누를때 ================================================================================
    public void SceneChangeToRobby()
    {
        StartCoroutine(C2S_GameOver());
    }


    public void SceneChangeToRobbyMulti()
    {
        StartCoroutine(C2S_GameOverMultiplay());
    }

    IEnumerator C2S_GameOverMultiplay()
    {
        //로그인한 Email 정보를 가져온다.
        string _loginID = m_InfoManager.m_email;

        //HTTP통신을 할 대상 주소
        string url = "49.247.131.35/C2S_GameOver.php";

        WWWForm myForm = new WWWForm();
        //아이디
        myForm.AddField("email", _loginID);
        //획득금액                
        myForm.AddField("m_money", m_resultMultiUI.transform.Find("Text_Money").GetChild(0).GetComponent<Text>().text);

        UnityWebRequest uwr = UnityWebRequest.Post(url, myForm);

        //서버에 문서를 요청한다. 그리고 반환되면 yield return 뒤에 구문이 실행된다.
        yield return uwr.SendWebRequest();

        //반환에서 네트워크 에러가 발생했다면
        if (uwr.isNetworkError)
        {
            Debug.Log("Error While Sending: " + uwr.error);

            yield break;
        }
        //랭킹시스템 적용 했고 유저 돈 변화 시켰다면
        //정상적으로 반환값이 왔다면 화면 전환 한다.
        else
        {
            //여기서 화면 전환한다.
            SceneManager.LoadScene("scene_robby");
        }



    }


    IEnumerator C2S_GameOver()
    {
        //로그인한 Email 정보를 가져온다.
        string _loginID = m_InfoManager.m_email;

        //HTTP통신을 할 대상 주소
        string url = "49.247.131.35/C2S_GameOver.php";

        WWWForm myForm = new WWWForm();
        //아이디
        myForm.AddField("email", _loginID);

        myForm.AddField("m_mapassetname", m_mapassetName);

        //아이디의 경기 기록
        myForm.AddField("m_minute", m_minuate);
        myForm.AddField("m_second", m_second);
        myForm.AddField("m_millisecond", m_millisecond.ToString("F0"));
        myForm.AddField("m_money", m_resultui.transform.Find("Text_Money").GetChild(0).GetComponent<Text>().text);


        UnityWebRequest uwr = UnityWebRequest.Post(url, myForm);

        //서버에 문서를 요청한다. 그리고 반환되면 yield return 뒤에 구문이 실행된다.
        yield return uwr.SendWebRequest();

        //반환에서 네트워크 에러가 발생했다면
        if (uwr.isNetworkError)
        {
            Debug.Log("Error While Sending: " + uwr.error);

            yield break;
        }
        //랭킹시스템 적용 했고 유저 돈 변화 시켰다면
        //정상적으로 반환값이 왔다면 화면 전환 한다.
        else
        {
            //여기서 화면 전환한다.
            SceneManager.LoadScene("scene_singleplay");
        }
    }
    //=============================================================================================================

    //결과창에서 다시 하기 누를때 ================================================================================
    public void SceneChangeToReplay()
    {
        StartCoroutine(C2S_GameOverReplay());
    }

    //다시하기
    IEnumerator C2S_GameOverReplay()
    {
        //로그인한 Email 정보를 가져온다.
        string _loginID = m_InfoManager.m_email;

        //HTTP통신을 할 대상 주소
        string url = "49.247.131.35/C2S_GameOver.php";

        WWWForm myForm = new WWWForm();
        //아이디
        myForm.AddField("email", _loginID);

        myForm.AddField("m_mapassetname", m_mapassetName);

        //아이디의 경기 기록
        myForm.AddField("m_minute", m_minuate);
        myForm.AddField("m_second", m_second);

        myForm.AddField("m_millisecond", m_millisecond.ToString("F0"));

        myForm.AddField("m_money", m_resultui.transform.Find("Text_Money").GetChild(0).GetComponent<Text>().text);


        UnityWebRequest uwr = UnityWebRequest.Post(url, myForm);

        //서버에 문서를 요청한다. 그리고 반환되면 yield return 뒤에 구문이 실행된다.
        yield return uwr.SendWebRequest();

        //반환에서 네트워크 에러가 발생했다면
        if (uwr.isNetworkError)
        {
            Debug.Log("Error While Sending: " + uwr.error);

            yield break;
        }
        //랭킹시스템 적용 했고 유저 돈 변화 시켰다면
        //정상적으로 반환값이 왔다면 화면 전환 한다.
        else
        {
            //여기서 화면 전환한다.
            SceneManager.LoadScene("scene_map" + m_mapassetName);
        }
    }

    //=============================================================================================================

    //게임시작시====================================================================================================================================

    IEnumerator S2C_UserData()
    {
        //로그인한 Email 정보를 가져온다.
        string _loginID = m_InfoManager.m_email;

        //HTTP통신을 할 대상 주소
        string url = "49.247.131.35/S2C_UserData.php";

        WWWForm myForm = new WWWForm();
        myForm.AddField("email", _loginID);
        UnityWebRequest uwr = UnityWebRequest.Post(url, myForm);

        //서버에 문서를 요청한다. 그리고 반환되면 yield return 뒤에 구문이 실행된다.
        yield return uwr.SendWebRequest();

        //반환에서 네트워크 에러가 발생했다면
        if (uwr.isNetworkError)
        {
            Debug.Log("Error While Sending: " + uwr.error);

            yield break;
        }
        //정상적으로 반환값이 왔다면
        else
        {
            //서버로부터 JSON형태의 메세지를 받는다.
            string jsonMessage = uwr.downloadHandler.text;

            //파싱할수있는 JOBJect 형태로 변환한다.
            JObject jobject = JObject.Parse(jsonMessage);

            //==================================================================================닉네임
            string _nickname = (string)jobject["m_nickname"];

            //==================================================================================차
            string _carname = (string)jobject["m_car"];
            string _carwheel = (string)jobject["m_wheel"];
            string _carwing = (string)jobject["m_wing"];

            m_player = Instantiate(Resources.Load("Car/" + _carname + "_InGame"), new Vector3(0, 0, 0), Quaternion.identity) as GameObject;

            //==================================================================================바퀴

            MeshRenderer[] _wheelList = m_player.transform.Find("Wheel_Mesh").GetComponentsInChildren<MeshRenderer>();

            foreach (MeshRenderer _wheel in _wheelList)
            {
                _wheel.material = Resources.Load<Material>("Wheel/" + _carwheel);
            }

            //==================================================================================날개

            m_player.transform.Find(_carwing).gameObject.SetActive(true);

        }
    }

    //시네마틱 카메라 코드(카메라가 다 돌고나 ESC를 누르면 스킵이 플레이가 된다.)
    IEnumerator CinematicCamera(GameObject _cameraFrom, Transform[] _positionArray)
    {

        float _moverate = 0.0f;

        //카메라 모드를 바꾸어준다.
        _cameraFrom.GetComponent<SC_CameraController>().m_mode = SC_CameraController.E_MODE.MOVE;

        //시네마틱 카메라가 이동할 경로들을 가져온다.
        for (int i = 0; i < _positionArray.Length; i++)
        {
            //만약 스킵한다면 탈출
            if (m_isCinematicSkip == true)
            {
                break;
            }

            //============================================================================================================해당 경로만큼 순서대로 이동한다.
            while (_moverate <= 1.0f && (i + 1) < _positionArray.Length)
            {
                if (m_isCinematicSkip == true)
                {
                    break;
                }

                Vector3 _movePos = Vector3.Lerp(_positionArray[i].transform.position, _positionArray[i + 1].transform.position, _moverate);

                _moverate += 0.001f;

                _cameraFrom.transform.position = _movePos;
                _cameraFrom.transform.LookAt(m_cinematiclookAt);

                yield return new WaitForSeconds(0.001f);
            }

            _moverate = 0.0f;
        }

        //================================================================================================================
        //여기에 온다면 1)ESC를누르고 스킵 2)시네마틱 카메라 경로 다 돌았다. 이 2가지의 경우이다.



        //시네마틱 카메라 UI는 꺼준다.
        m_canvasCinematic.SetActive(false);

        //인게임 UI 킨다.
        m_ingameUI.gameObject.SetActive(true);

        //카메라에 필요한 정보를 넘겨준다.
        m_player.GetComponent<SC_CarController>().CameraControllerToUserObject();

        //카메라 모드를 바꾸어준다.
        _cameraFrom.GetComponent<SC_CameraController>().m_mode = SC_CameraController.E_MODE.PLAY;

        //싱글플레이라면
        if (m_isMultiPlay == false)
        {
            //래디/고 를 발동시킨다.
            StartCoroutine(StartReadyGoCountdown());
        }
        //멀티플레이라면
        else
        {
            m_client.C2S_SendMessage("GAMESTATE:" + m_InfoManager.m_nickname + "/" + "START");
        }




        yield break;
    }

    public IEnumerator StartReadyGoCountdown()
    {
        //래디고 UI를 킨다.
        m_readygoCountdown.SetActive(true);

        //============================================================================================================래디/고! 알고리즘

        float _rotateRate = 0.0f;

        m_readygoCountdown.transform.Find("Front").GetComponent<Text>().text = "READY";

        while (_rotateRate <= 1.0f)
        {
            float _x = Mathf.Lerp(90.0f, 0.0f, _rotateRate);
            m_readygoCountdown.transform.localRotation = Quaternion.Euler(_x, 0, 0);
            _rotateRate += 0.05f;
            yield return new WaitForSeconds(0.01f);
        }

        yield return new WaitForSeconds(1.5f);

        _rotateRate = 0.0f;
        m_readygoCountdown.transform.Find("Front").GetComponent<Text>().text = "GO!";

        while (_rotateRate <= 1.0f)
        {
            float _x = Mathf.Lerp(90.0f, 0.0f, _rotateRate);
            m_readygoCountdown.transform.localRotation = Quaternion.Euler(_x, 0, 0);
            _rotateRate += 0.05f;
            yield return new WaitForSeconds(0.01f);
        }
        yield return new WaitForSeconds(0.5f);

        //============================================================================================================래디/고! 알고리즘
        //여기에 온다면 래디/고 알고리즘이 끝난경우다.

        //래디고 UI는 꺼준다.
        m_readygoCountdown.SetActive(false);

        //플레이할수있다.
        m_isPlay = true;

        yield break;
    }

    //==============================================================================================================================================

    //다른 스크립트에서 호출하는 콜백함수===========================================================================================================

    //스폰지점을 통과할때마다 이벤트로 호출된다.
    public void CheckTrigger(string _triggerIndex)
    {
        //현재 충돌된 오브젝트의 이름정보를 가져온다. 0,1,2,3,4
        int _index = System.Convert.ToInt32(_triggerIndex);

        //해당 오브젝트를 꺼준다.
        m_spawnList[_index].gameObject.SetActive(false);

        //그오브젝트의 번호를 담는다.
        m_spawnIndex = _index;

        //다음 오브젝트
        m_spawnIndex++;

        //나머지 계산으로 반복해서 키고 끄고를한다.
        //0으로 초기화하지않고 m_spawnIndex를 5,6,7,8로 증가시키는 이유는 몇바퀴 돌았는지 알아야 하기때문에 or 랭킹 산출에 쓰인다.
        m_spawnList[m_spawnIndex % m_spawnList.Count].gameObject.SetActive(true);


        //싱글플레이라면
        if (m_isMultiPlay == false)
        {
            //골인지점에 도착했을때 1바퀴라면 게임종료 2바퀴라면 1바퀴 더진행
            if (m_spawnList[_index].gameObject.name == "0")
            {
                //게임종료바퀴가 아니라면
                if (m_nowAroundNumber != m_maxAroundNumber)
                {
                    m_nowAroundNumber++;
                    m_around.text = m_nowAroundNumber.ToString();
                    m_laptime.text = m_lapminuate.ToString() + " : " + m_lapsecond.ToString() + " : " + m_lapmillisecond.ToString("F0");
                    m_lapminuate = 0;
                    m_lapsecond = 0;
                    m_lapmillisecond = 0.0f;
                }
                //게임종료
                else
                {
                    //Debug.Log("게임종료");
                    
                    m_laptime.text = m_lapminuate.ToString() + " : " + m_lapsecond.ToString() + " : " + m_lapmillisecond.ToString("F0");

                    StartCoroutine(ChangeCamera(m_mainCamera.gameObject, m_player.transform.Find("FinishCamera").gameObject));

                    //충돌 무시 하게한다.
                    m_player.GetComponent<SC_CarController>().SetCarForTriggerMine(true);

                    m_isPlay = false;

                    m_ingameUI.gameObject.SetActive(false);

                    ShowResult();
                }
            }
        }
        //멀티플레이라면
        else
        {

            //서버에 자신이 가야할 벽 숫자를 보낸다.
            m_client.C2S_SendMessage("GAMERANK:" + m_InfoManager.m_nickname + "/" + m_spawnIndex.ToString());


            //골인지점에 도착했을때 1바퀴라면 게임종료 2바퀴라면 1바퀴 더진행
            if (m_spawnList[_index].gameObject.name == "0")
            {
                //게임종료바퀴가 아니라면
                if (m_nowAroundNumber != m_maxAroundNumber)
                {
                    m_nowAroundNumber++;
                    m_around.text = m_nowAroundNumber.ToString();
                    m_laptime.text = m_lapminuate.ToString() + " : " + m_lapsecond.ToString() + " : " + m_lapmillisecond.ToString("F0");
                    m_lapminuate = 0;
                    m_lapsecond = 0;
                    m_lapmillisecond = 0.0f;
                }
                //게임종료
                else
                {
                    if (m_isSecondsToGameOverPlaying)
                    {
                        StopCoroutine(m_SecondsToGameOver);
                    }

                    //나는 골인했다고 입력한다.
                    m_carRankingList[m_InfoManager.m_nickname] = "100";

                    //잠시만 기다려주세요를 표시할 UI
                    m_UISecondsToGameOver.SetActive(true);

                    m_UISecondsToGameOver.transform.Find("Text").GetComponent<Text>().text = "잠시 후 게임이 종료 됩니다.";

                    m_laptime.text = m_lapminuate.ToString() + " : " + m_lapsecond.ToString() + " : " + m_lapmillisecond.ToString("F0");

                    StartCoroutine(ChangeCamera(m_mainCamera.gameObject, m_player.transform.Find("FinishCamera").gameObject));

                    //충돌 무시 하게한다.
                    m_player.GetComponent<SC_CarController>().SetCarForTriggerMine(true);

                    m_isPlay = false;

                    m_ingameUI.gameObject.SetActive(false);

                    m_client.C2S_SendMessage("GAMEOVER:" + m_InfoManager.m_nickname + "/" + "GAMEOVER");
                }
            }
        }
    }


    public void ShowResult(bool _timeOut = false)
    {
        //싱글플레이라면
        if (m_isMultiPlay == false)
        {
            m_resultui.SetActive(true);

            m_resultui.transform.Find("Text_WholeTime").GetChild(0).GetComponent<Text>().text = m_minuate.ToString() + " : " + m_second.ToString() + " : " + m_millisecond.ToString("F0");
        }
        //멀티플레이라면
        else
        {
            if (_timeOut == true)
            {
                m_resultMultiUI.SetActive(true);
                m_resultMultiUI.transform.Find("Text_WholeTime").GetChild(0).GetComponent<Text>().text = "완주실패";
                m_resultMultiUI.transform.Find("Text_Rank").GetChild(0).GetComponent<Text>().text = m_playerRank.ToString();
                m_resultMultiUI.transform.Find("Text_Rank").GetChild(2).GetComponent<Text>().text = m_rankAllCount.ToString();
            }
            else
            {
                m_resultMultiUI.SetActive(true);
                m_resultMultiUI.transform.Find("Text_WholeTime").GetChild(0).GetComponent<Text>().text = m_minuate.ToString() + " : " + m_second.ToString() + " : " + m_millisecond.ToString("F0");
                m_resultMultiUI.transform.Find("Text_Rank").GetChild(0).GetComponent<Text>().text = m_playerRank.ToString();
                m_resultMultiUI.transform.Find("Text_Rank").GetChild(2).GetComponent<Text>().text = m_rankAllCount.ToString();
            }
            
        }

    }


    //스폰지점으로 이동시킨다. 바다에 떨어졌을경우
    public void SetTransformSpawn(GameObject _player)
    {
        int _spawnIndex = m_spawnIndex % m_spawnList.Count;
        _spawnIndex -= 1;

        if (_spawnIndex < 0)
        {
            _spawnIndex = m_spawnList.Count - 1;
        }

        _player.transform.position = m_spawnList[_spawnIndex].transform.position;
        _player.transform.rotation = m_spawnList[_spawnIndex].transform.rotation;
    }

    //차함수에서 호출
    public IEnumerator StartFallGo()
    {
        //충돌 무시 설정
        m_player.GetComponent<SC_CarController>().SetCarForTriggerMine(true);

        m_readygoCountdown.SetActive(true);

        float _rotateRate = 0.0f;

        m_readygoCountdown.transform.Find("Front").GetComponent<Text>().text = "FALL";

        while (_rotateRate <= 1.0f)
        {
            float _x = Mathf.Lerp(90.0f, 0.0f, _rotateRate);
            m_readygoCountdown.transform.localRotation = Quaternion.Euler(_x, 0, 0);
            _rotateRate += 0.05f;
            yield return new WaitForSeconds(0.01f);
        }

        yield return new WaitForSeconds(0.5f);

        _rotateRate = 0.0f;
        m_readygoCountdown.transform.Find("Front").GetComponent<Text>().text = "GO!";

        while (_rotateRate <= 1.0f)
        {
            float _x = Mathf.Lerp(90.0f, 0.0f, _rotateRate);
            m_readygoCountdown.transform.localRotation = Quaternion.Euler(_x, 0, 0);
            _rotateRate += 0.05f;
            yield return new WaitForSeconds(0.01f);
        }

        yield return new WaitForSeconds(0.5f);

        m_readygoCountdown.SetActive(false);

        //충돌 무시 해제
        m_player.GetComponent<SC_CarController>().SetCarForTriggerMine(false);

        yield break;
    }


    public void SetCarForTriggerAnother(string _nickName)
    {
        Transform _anotherFallingUser = m_userList.transform.Find(_nickName);

        if (_anotherFallingUser.Find("Body").GetComponent<BoxCollider>().enabled == true)
        {
            _anotherFallingUser.Find("Body").GetComponent<BoxCollider>().enabled = false;
            _anotherFallingUser.Find("Wheel_Collider").gameObject.SetActive(false);
        }
        else
        {
            _anotherFallingUser.Find("Body").GetComponent<BoxCollider>().enabled = true;
            _anotherFallingUser.Find("Wheel_Collider").gameObject.SetActive(true);
        }


    }

    //============================================================================================================================




}
