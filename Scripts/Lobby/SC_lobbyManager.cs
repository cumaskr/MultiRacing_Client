using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//=============================================추가적으로 필요한 라이브러리
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
//==========================================================================================

public class SC_lobbyManager : MonoBehaviour
{
    //싱글톤 정보매니져
    SC_InfoManager m_InfoManager;

    //=================================================멤버 변수=================================================

    //마우스 왼쪽 클릭후 충돌된 물체에 대한 정보를 가져올때 사용한다.
    RaycastHit m_hit;
    //누르고 있는 중이냐?를 담는 변수
    bool m_isClicked = false;
    //자동차 회전속도
    float m_rotSpeed = 10.0f;

    //================================================오브젝트 연결===============================================    
    //회전할 공간의 오브젝트
    public GameObject m_rotateObjectParent;

    //000님(닉네임) 안녕하세요 라는 문구를 표시할 Text
    public Text m_hello;

    //멀티플레이 버튼을 누르면 방목록 팝업창이 생성된다.
    public GameObject m_Popup_RoomList;
    

    ////방목록->방만들기 버튼을 누르면 방만들기 팝업창이 생성된다.
    //public GameObject m_Popup_RoomMake;
    //public GameObject m_Popup_RoomMakeParent;

    ////방목록->방선택 버튼을 누르면 채팅방 팝업창이 생성된다.
    //public GameObject m_Popup_RoomChatting;
    //public GameObject m_Popup_RoomChattingParent;

    //============================================================================================================
    // Start is called before the first frame update
    void Start()
    {
        m_InfoManager = GameObject.Find("InfoManager").GetComponent<SC_InfoManager>();

        StartCoroutine(S2C_UserData());        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            if (m_isClicked == true)
            {
                float deltaX = Input.GetAxis("Mouse X");
                m_rotateObjectParent.transform.Rotate(new Vector3(0, 1, 0), deltaX * -1 * m_rotSpeed);
            }

            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out m_hit, Mathf.Infinity))
            {
                if (m_hit.transform.gameObject.name.Equals("RotateObject"))
                {
                    m_isClicked = true;
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            m_isClicked = false;
        }
    }


    //로비화면에서 필요한 데이터만 로그인 유저 테이블을 참조하여 가져온다.
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

            GameObject memberCar;
            
            memberCar = Instantiate(Resources.Load("Car/" + _carname), new Vector3(0, 0, 0), Quaternion.identity) as GameObject;

            memberCar.transform.SetParent(m_rotateObjectParent.transform, false);

            memberCar.transform.localScale = new Vector3(1.6f, 1.6f, 1.6f);

            //==================================================================================바퀴



            MeshRenderer[] _wheelList = memberCar.transform.Find("Wheel_Mesh").GetComponentsInChildren<MeshRenderer>();

            foreach (MeshRenderer _wheel in _wheelList)
            {
                _wheel.material = Resources.Load<Material>("Wheel/" + _carwheel);
            }

            //==================================================================================날개

            memberCar.transform.Find(_carwing).gameObject.SetActive(true);


            //로비화면에서 로그인한 회원의 닉네임과 함께 인사말을 날린다.
            m_hello.text = _nickname + "님 안녕하세요!";
        }
    }

    void ChangeToSelectCarWing(GameObject _prev, GameObject _new)
    {
        if (_prev != null)
        {
            _prev.gameObject.SetActive(false);
        }

        _new.gameObject.SetActive(true);
    }



    public void SceneChangeToGarage()
    {
        SceneManager.LoadScene("scene_garageMain");
    }

    public void SceneChangeSinglePlay()
    {
        SceneManager.LoadScene("scene_singleplay");
    }

    public void MakePopup_RoomList()
    {
        m_Popup_RoomList.SetActive(true);

        //GameObject _roomList = Instantiate(m_Popup_RoomList, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;

        //_roomList.transform.SetParent(m_Popup_RoomListParent.transform, false);
    }

}//SC_lobbyManager
