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

public class SC_garageWheelManager : MonoBehaviour
{
    //싱글톤 정보매니져
    SC_InfoManager m_InfoManager;

    //=================================================멤버 변수=================================================

    //마우스 왼쪽 클릭후 충돌된 물체에 대한 정보를 가져올때 사용한다.
    RaycastHit m_hit;
    //누르고 있는 중이냐?를 담는 변수
    bool m_isClicked = false;
    //자동차 모델 회전속도
    float m_rotSpeed = 10.0f;


    //=================================================멤버 변수=======================================================

    //로그인한 유저가 보유하고 있는 돈을 표시할 TextUI
    public Text m_memberMoney;

    //로그인한 유저가 보유하고 있는 차량이 셋팅되어야 할 부모 오브젝트
    public GameObject m_pref_carbutton_ScrollView;

    //로그인한 유저가 보유하고 있는 차량을 나타내는 버튼프리팹
    public GameObject m_pref_carbutton;


    //실제 회전하는 빈 오브젝트
    public GameObject m_rotateObject;

    //차 교체시 이전에 있던 차 삭제 해줘야한다. m_now_carbutton과 비슷한개념
    GameObject m_rotateCarModel;


    //선택된 차에 선택됬다고 표시를 해주어야하기때문에, 
    //선택한 차버튼을 가지고있을 변수(처음 로드될때 장착하고있는 차 버튼 들어오고, 이후에 누를때 마다 바뀜)
    GameObject m_now_carbutton;

    //선택된 차량의 로고와 이름을 나타낸다.
    public GameObject m_carname_Object;


    //안내메세지를 띄울 오브젝트 -> 공통 Popup창이기때문에 확인을 누르면 안내메세지가 SetActive(false)가 된다.
    public GameObject m_popup_message;
    //안내메세지의 text 부분
    public Text m_popup_message_text;

    //안내메세지를 띄울 오브젝트-> 구매하기를 눌렀을때 다시한번 확인 용 질문!
    public GameObject m_popup_buyconfirmMessage;

    //구매하기를 눌렀을때 착용중이라는 이미지 표시를 해주어야하는데 이전에 눌렀던 버튼이 무엇인지 모르기 때문에 담아둔다.    
    //ChangeCar()에서 m_prev_carbutton에 담는다.
    //BuyCar()에서 사용한다.
    GameObject m_buy_carbutton;

    //차는 장착한것이 그대로 유지해야한다. 처음에 로드할때 가져오는 차 이름을 알아야해서 저장해 둔다.
    string _carcarAssetname;
    //차모델 말고 날개는 장착한것이 그대로 유지해야한다. 처음에 로드할때 가져오는 날개 이름을 알아야해서 저장해 둔다.
    string _carwingAssetname;

    public Text m_topspeed;
    public Text m_acceleration;
    public Text m_handling;

    // Start is called before the first frame update
    void Start()
    {
        m_InfoManager = GameObject.Find("InfoManager").GetComponent<SC_InfoManager>();

        StartCoroutine(this.S2C_WheelList_All());

    }

    // Update is called once per frame
    void Update()
    {
        RotateCarObject();



    }


    //주의
    //이 함수는 차고 화면의 스크롤뷰에 생성되는 차버튼(유저가 가지고있는 차 목록이 버튼 형식으로 나온다.)
    //차버튼을 클릭했을때 해당버튼에 붙어있는 스크립트에서 Onclick이벤트에 ChangeCar함수를 호출하도록 연결되어있다.
    public void ChangeCar(GameObject _select_carbutton)
    {
        //경로를 가져온다.
        string _carbuttonname = _select_carbutton.gameObject.name;

        //스탯 받아와서 출력해주어야한다.
        StartCoroutine(S2C_Stat(_carcarAssetname, _select_carbutton.name, _carwingAssetname));

        //선택한 버튼이 구매를 한 차라면?
        if (_select_carbutton.transform.GetChild(3).GetComponent<Text>().text == "1")
        {
            //장착중 이미지표시를 바꾸어준다.
            ChangeToSelectCarButtonImage(m_now_carbutton, _select_carbutton);

            //SQL에도 교체한 아이템들에 대해 장착상황을 갱신해준다.
            StartCoroutine(C2S_ChangeEquip(m_now_carbutton, _select_carbutton));

            //장착중인 버튼 으로 변수 갱신해준다.
            m_now_carbutton = _select_carbutton;

            //구매한 차량이기떄문에 차량 금액에 보유차량이라고 표시한다.
            m_carname_Object.transform.GetChild(2).gameObject.SetActive(false);
            m_carname_Object.transform.GetChild(3).gameObject.SetActive(true);

        }
        //구입하지 않은 차량이라면
        else
        {
            //현재 실제 쓰이는곳은 BuyCar()에서다.
            m_buy_carbutton = _select_carbutton;

            //구매하지 않은 차량이기떄문에 차량 금액에 금액을 표시한다.
            m_carname_Object.transform.GetChild(2).gameObject.SetActive(true);
            m_carname_Object.transform.GetChild(3).gameObject.SetActive(false);

        }

        //==================================================================================바퀴

        MeshRenderer[] _wheelList = m_rotateCarModel.transform.Find("Wheel_Mesh").GetComponentsInChildren<MeshRenderer>();

        foreach (MeshRenderer _wheel in _wheelList)
        {
            _wheel.material = Resources.Load<Material>("Wheel/" + _carbuttonname);
        }

        //==================================================================================날개

        //상단에 보일 차량 로고
        m_carname_Object.transform.GetChild(0).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/Wheel/" + _carbuttonname);
        //상단에 보일 차량 이름
        m_carname_Object.transform.GetChild(1).GetComponent<Text>().text = _select_carbutton.transform.GetChild(2).GetComponent<Text>().text;
        //상단에 보일 차량 가격
        m_carname_Object.transform.GetChild(2).GetComponent<Text>().text = _select_carbutton.transform.GetChild(4).GetComponent<Text>().text;

        //유저가 아직 구입하지 않은 차량이라면 구매하기 버튼을 띄어준다.
        if (_select_carbutton.transform.GetChild(3).GetComponent<Text>().text == "0")
        {
            //구매하기 버튼 보이게 한다.
            m_carname_Object.transform.GetChild(4).gameObject.SetActive(true);

        }
        else
        {
            //구매하기 버튼 보이지 않게 한다.
            m_carname_Object.transform.GetChild(4).gameObject.SetActive(false);

        }

    }

    //차고 화면에 나오는 차들 마우스로 왼쪽 클릭후 360도로 돌려볼수있는 기능
    void RotateCarObject()
    {
        if (Input.GetMouseButton(0))
        {
            if (m_isClicked == true)
            {
                float deltaX = Input.GetAxis("Mouse X");
                m_rotateObject.transform.Rotate(new Vector3(0, 1, 0), deltaX * -1 * m_rotSpeed);
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

    //차고화면에서 필요한 로그인 유저가 보유한 모든 차량을 가져온다. 그리고 스크롤뷰에 차버튼을 만든다.
    IEnumerator S2C_WheelList_All()
    {
        //HTTP통신을 할 대상 주소
        string url = "49.247.131.35/S2C_WheelList_All.php";

        //====================================================================================클라이언트->서버 보낼 데이터
        //로그인한 Email 정보를 가져온다.
        string _loginID = m_InfoManager.m_email;

        WWWForm myForm = new WWWForm();
        myForm.AddField("email", _loginID);

        //=============================================================================================================

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

            //==================================================================================회원정보
            //회원 닉네임 정보 불러온다.
            string _nickname = (string)jobject["m_nickname"];

            m_memberMoney.text = (string)jobject["m_money"];

            //==================================================================================차
            JArray _wheels = (JArray)jobject["m_wheel"];

            _carcarAssetname = (string)jobject["m_car"];
            _carwingAssetname = (string)jobject["m_wing"];


            //널값 예외처리는 꼮 해준다.
            if (_wheels != null)
            {
                //유저가 소지 하고있는 차는 1개이상이다.
                for (int i = 0; i < _wheels.Count; i++)
                {
                    //해당 차 버튼(차고화면에서 밑에 메뉴바의 스크롤뷰 안에 횡으로 생성된다.)을 생성한다.
                    GameObject _CarButton = Instantiate(m_pref_carbutton, new Vector3(0, 0, 0), Quaternion.identity);

                    //=======================================================차버튼에 필요한 정보들을 셋팅한다.==============================================================

                    //버튼 최상 부모의 이름을 차이름으로 설정하여 어떤차를 클릭했는지 알고, 바로 경로에서 3D 모델을 불러올수있도록 구상
                    _CarButton.gameObject.name = (string)_wheels[i]["m_assetname"];

                    //차 버튼 1번째는 선택됬을때 표시할 이미지 이다. 
                    //ChangeToSelectCarButton()에서 사용중

                    //차버튼 2번재 텍스트창에 차이름을 표시해준다.
                    _CarButton.transform.GetChild(2).GetComponent<Text>().text = (string)_wheels[i]["m_name"];

                    //차버튼 3번재 텍스트창에 차구매여부를 표시해준다. 이것을 기반으로 차 구매하기 버튼이 뜬다.
                    _CarButton.transform.GetChild(3).GetComponent<Text>().text = (string)_wheels[i]["m_buy"];

                    //구매한 차라면
                    if (_CarButton.transform.GetChild(3).GetComponent<Text>().text == "1")
                    {
                        //차버튼 밑 0번째는 로고를 넣어준다.
                        _CarButton.transform.GetChild(0).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/Wheel/" + (string)_wheels[i]["m_assetname"]);
                    }
                    //구매한 차가 아니면
                    else
                    {
                        //차버튼 밑 0번째는 자물쇠를 넣어준다.
                        _CarButton.transform.GetChild(0).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/lock");
                    }

                    //차버튼 밑의 네번재 텍스트창에 차 가격을 표시해준다.
                    _CarButton.transform.GetChild(4).GetComponent<Text>().text = (string)_wheels[i]["m_price"];

                    //차고 하단 메뉴 스크롤뷰 자식으로 넣어준다.(그래야지 스크롤뷰안에 아이템으로 움직인다.)
                    _CarButton.transform.SetParent(m_pref_carbutton_ScrollView.transform, false);

                    //=======================================================회전 오브젝트에 필요한 정보를 셋팅한다.==============================================================
                    //현재 착용중이라면 차고 회전오브젝트에 생성해준다.
                    if ((int)_wheels[i]["m_equip"] == 1)
                    {
                        m_now_carbutton = _CarButton;

                        //장착중인차는 장착중이라고 차버튼에 표시를 해준다.
                        ChangeToSelectCarButtonImage(null, m_now_carbutton);

                        //==================================================================================차
                      
                        m_rotateCarModel = Instantiate(Resources.Load("Car/" + _carcarAssetname), new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
                        m_rotateCarModel.transform.SetParent(m_rotateObject.transform, false);
                        m_rotateCarModel.transform.localScale = new Vector3(1.6f, 1.6f, 1.6f);

                        MeshRenderer[] _wheelList = m_rotateCarModel.transform.Find("Wheel_Mesh").GetComponentsInChildren<MeshRenderer>();

                        foreach (MeshRenderer _wheel in _wheelList)
                        {
                            _wheel.material = Resources.Load<Material>("Wheel/" + (string)_wheels[i]["m_assetname"]);
                        }

                        //==================================================================================날개

                        m_rotateCarModel.transform.Find(_carwingAssetname).gameObject.SetActive(true);

                        //====================================================================================================================================================


                        //화면 상단 이름 셋팅
                        m_carname_Object.transform.GetChild(0).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/Wheel/" + (string)_wheels[i]["m_assetname"]);
                        m_carname_Object.transform.GetChild(1).GetComponent<Text>().text = (string)_wheels[i]["m_name"];
                        m_carname_Object.transform.GetChild(2).GetComponent<Text>().text = (string)_wheels[i]["m_price"];
                        m_carname_Object.transform.GetChild(2).gameObject.SetActive(false);
                        m_carname_Object.transform.GetChild(3).gameObject.SetActive(true);

                    }
                }
            }

        }

        //스탯 받아와서 출력해주어야한다.
        StartCoroutine(S2C_Stat(_carcarAssetname, m_now_carbutton.name, _carwingAssetname));

    }//Receive_Login_AllCar

    IEnumerator C2S_ChangeEquip(GameObject _nowCarbutton, GameObject _selectCarbutton)
    {
        //HTTP통신을 할 대상 주소
        string url = "49.247.131.35/C2S_ChangeEquip.php";

        WWWForm myForm = new WWWForm();

        myForm.AddField("email", m_InfoManager.m_email);

        myForm.AddField("m_nowCarbutton_name", _nowCarbutton.gameObject.name);

        myForm.AddField("m_selectCarbutton_name", _selectCarbutton.gameObject.name);

        myForm.AddField("m_type", "wheel");

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
            //저장이 목적이므로 아무것도 안한다.

        }
    }

    //차 선택을 할때 장착중이라는 표시가 바뀌어야한다. 구매한 차들끼리에서만 장착중이 바뀌어야한다.
    void ChangeToSelectCarButtonImage(GameObject _prev, GameObject _new)
    {
        if (_prev != null)
        {
            _prev.transform.GetChild(1).gameObject.SetActive(false);
        }

        _new.transform.GetChild(1).gameObject.SetActive(true);

        //여기다가 InfoManager에 바퀴 정보에 관한 데이터를 갱신한다.(멀티플레이에서 이값을 참조하여 채팅방에 차를 생성한다.)
        m_InfoManager.m_wheelAssetName = _new.gameObject.name;
    }

    //구매전 돈이 부족하거나, 혹은 한번더 질문하기 위한 함수
    public void BuyCarConfirm()
    {
        //여기서 돈 모자라면 예외처리 한다.
        //확인 취소 창 띄울것
        int _memberMoney = int.Parse(m_memberMoney.text);
        int _carprice = int.Parse(m_carname_Object.transform.GetChild(2).GetComponent<Text>().text);

        //돈이 부족하다면
        if (_memberMoney - _carprice < 0)
        {
            m_popup_message.SetActive(true);
            m_popup_message_text.text = "금액이 부족합니다.";
        }
        else
        {
            m_popup_buyconfirmMessage.SetActive(true);
        }
    }

    //사시겠습니까? 에서 확인 눌렀을때 실제로 구매 되는함수
    public void BuyCar()
    {
        m_popup_buyconfirmMessage.SetActive(false);

        //m_popup_buyconfirmMessage 팝업 창에서 확인을 눌렀을때 호출된다.
        StartCoroutine(C2S_BuyItem());

        if (m_buy_carbutton != null)
        {
            //구매하기 버튼을 눌렀을때 장착중 표시를 바꾸어준다.
            ChangeToSelectCarButtonImage(m_now_carbutton, m_buy_carbutton);

            //차버튼 자물쇠 이미지를 로고로 바꾸어준다.
            m_buy_carbutton.transform.GetChild(0).GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/Wheel/" + m_buy_carbutton.gameObject.name);

            m_now_carbutton = m_buy_carbutton;
        }

    }

    IEnumerator C2S_BuyItem()
    {
        //HTTP통신을 할 대상 주소
        string url = "49.247.131.35/C2S_BuyItem.php";

        //====================================================================================클라이언트->서버 보낼 데이터
        //로그인한 Email 정보를 가져온다.
        string _loginID = m_InfoManager.m_email;
        //차 이름
        string _itemAssetname = m_buy_carbutton.gameObject.name;
        //차 가격
        string _price = m_carname_Object.transform.GetChild(2).GetComponent<Text>().text;

        WWWForm myForm = new WWWForm();

        myForm.AddField("email", _loginID);

        myForm.AddField("m_itemAssetname", _itemAssetname);

        myForm.AddField("m_price", _price);

        myForm.AddField("m_type", "wheel");

        //=============================================================================================================

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
            //MySQL에서 작업이 끝났으면 Unity에서 해주어야할 처리를 한다.

            //구매하기버튼은 없어지게한다.
            m_carname_Object.transform.GetChild(4).gameObject.SetActive(false);

            //차량 바꿀때 장착중을 MySQL에 연동하려면 m_buy항목을 true로 바꾸어주어야한다. 
            //구매할때만 값을 바뀌고 나머진 값이 바뀔 일이 없다.
            m_buy_carbutton.transform.GetChild(3).GetComponent<Text>().text = "1";


            //서버로부터 JSON형태의 메세지를 받는다.
            string jsonMessage = uwr.downloadHandler.text;

            //파싱할수있는 JOBJect 형태로 변환한다.
            JObject jobject = JObject.Parse(jsonMessage);

            //==================================================================================회원정보            
            //구매로 인한 바뀐 금액표시를 갱신해준다.
            m_memberMoney.text = (string)jobject["m_money"];

            //==================================================================================차 금액 보유 차량이라고 변경
            m_carname_Object.transform.GetChild(2).gameObject.SetActive(false);
            m_carname_Object.transform.GetChild(3).gameObject.SetActive(true);
        }
    }

    //차고로 화면전환 함수
    public void SceneChangeToGarage()
    {
        SceneManager.LoadScene("scene_garageMain");
    }

    // wheel
    //_select_carbutton.gameObject.name(ChangeCar())
    //m_now_carbutton.gameObject.name(ReceiveLoginCar())
    //_carcarAssetname / _carwingAssetname
    IEnumerator S2C_Stat(string _carAssetname, string _wheelAssetname, string _wingAssetname)
    {
        //HTTP통신을 할 대상 주소
        string url = "49.247.131.35/S2C_Stat.php";

        //====================================================================================클라이언트->서버 보낼 데이터

        //로그인한 Email 정보를 가져온다.
        string _loginID = m_InfoManager.m_email;

        WWWForm myForm = new WWWForm();
        myForm.AddField("email", _loginID);

        myForm.AddField("m_carAssetname", _carAssetname);
        myForm.AddField("m_wheelAssetname", _wheelAssetname);
        myForm.AddField("m_wingAssetname", _wingAssetname);

        //=============================================================================================================

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


            JObject m_carstat = (JObject)jobject["m_carstat"];
            JObject m_wheelstat = (JObject)jobject["m_wheelstat"];
            JObject m_wingstat = (JObject)jobject["m_wingstat"];



            string _cartopspeed = (string)m_carstat["m_topspeed"];
            string _caracceleration = (string)m_carstat["m_acceleration"];
            string _carhandling = (string)m_carstat["m_handling"];

            string _wheeltopspeed = (string)m_wheelstat["m_topspeed"];
            string _wheelacceleration = (string)m_wheelstat["m_acceleration"];
            string _wheelhandling = (string)m_wheelstat["m_handling"];

            string _wingtopspeed = (string)m_wingstat["m_topspeed"];
            string _wingacceleration = (string)m_wingstat["m_acceleration"];
            string _winghandling = (string)m_wingstat["m_handling"];


            m_topspeed.text = (int.Parse(_cartopspeed) + int.Parse(_wheeltopspeed) + int.Parse(_wingtopspeed)).ToString();
            m_acceleration.text = (int.Parse(_caracceleration) + int.Parse(_wheelacceleration) + int.Parse(_wingacceleration)).ToString();
            m_handling.text = (int.Parse(_carhandling) + int.Parse(_wheelhandling) + int.Parse(_winghandling)).ToString();
        }
    }


}
