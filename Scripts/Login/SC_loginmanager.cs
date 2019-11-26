using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;

public class SC_loginmanager : MonoBehaviour
{
    //싱글톤 정보매니져
    SC_InfoManager m_InfoManager;

    //================================================PlayerPrefs===============================================   
    //1.FINDPW 비밀번호 찾기를 진행했을때 생성 로비화면 넘어가면 삭제        
    //================================================오브젝트 연결===============================================    
    public GameObject m_popupsign;
    public GameObject m_popupFindPW;
    public GameObject m_popupsign_parent;
    public InputField m_inputfield_email;
    public InputField m_inputfield_pw;


    //안내메세지를 띄울 오브젝트 -> 공통 Popup창이기때문에 확인을 누르면 안내메세지가 SetActive(false)가 된다.
    public GameObject m_popup_message;
    //안내메세지의 text 부분
    public Text m_popup_message_text;

    //안내메세지를 띄울 오브젝트-> 로그인 Popup창이기 때문에 확인을 누르면 화면 전한이 된다.
    public GameObject m_popup_login_message;
    //안내메세지의 text 부분
    public Text m_popup_login_message_text;

    //================================================멤버변수===============================================    
    int m_limitTime = 3;
    public string m_findpw = null;

    // Start is called before the first frame update
    void Start()
    {
        //화면 셋팅 구문(꼭 필요한건 지는 모르겠다. 추후 조사 필요)
        //Screen.SetResolution(Screen.width, Screen.width, true);

        m_InfoManager = GameObject.Find("InfoManager").GetComponent<SC_InfoManager>();

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnClickSign()
    {
        GameObject _prefabSign = Instantiate(m_popupsign, new Vector3(0, 0, 0), Quaternion.identity);
        _prefabSign.transform.SetParent(m_popupsign_parent.transform, false);
    }

    public void OnClickFindPW()
    {
        GameObject _prefabFindPW = Instantiate(m_popupFindPW, new Vector3(0, 0, 0), Quaternion.identity);
        _prefabFindPW.transform.SetParent(m_popupsign_parent.transform, false);
    }

    public void OnClickLogin()
    {
        //비밀번호 찾기를 누르고 로그인 할경우 인지 확인해야한다. 이메일을 보낼떄 FINDPW라는 키값에 날짜가 저장될것이다.
        string findpw = PlayerPrefs.GetString("FINDPW", "null");
        //만약 FINDPW 키랑 값이 없다면 그대로 로그인 한다. -> 비밀번호 찾기 안누른경우
        if (findpw == "null")
        {
            StartCoroutine(this.SendWebLogin());
        }
        //만약 FINDPW 키랑 값이 있다면 시간차이를 보고 00분 이내라면 바꾸고 로그인 / 아니라면 그냥 로그인 이때 비밀번호가 다를거기 떄문에 아라서 접속안됨
        else
        {
            //비밀번호 찾기를 눌렀을대 시간을 불러온다.
            System.DateTime FINDPWDate = System.Convert.ToDateTime(findpw);
            //현재 시간을 담는다.
            System.DateTime loginDate = System.DateTime.Now;
            //두시간의 차이를 담는다.
            System.TimeSpan timeInterver = loginDate - FINDPWDate;
            // 몇분 차이나는지를 담는 변수
            int timeInterverMinute = timeInterver.Minutes;

            //제한시간안에 새로운 비밀번호 확인하고 맞게 로그인 하는거라면
            if (timeInterverMinute <= m_limitTime && m_findpw == m_inputfield_pw.text)
            {
                //비밀번호 업데이트 후 로그인
                StartCoroutine(this.SendChangePW());
            }
            //제한시간이 초과했거나, 틀린 비밀번호를 쳤다면
            else
            {
                StartCoroutine(this.SendWebLogin());
            }

        }

    }

    //로그인이 정상적으로 된다면
    public void SceneChangeToRobby()
    {
        //PlayerPrefs.SetString("LOGINID", m_inputfield_email.text);
        SceneManager.LoadScene("scene_robby");
    }


    public IEnumerator SendChangePW()
    {
        string url = "49.247.131.35/changePW.php";
        WWWForm myForm = new WWWForm();
        myForm.AddField("email", m_inputfield_email.text);
        myForm.AddField("newpw", m_inputfield_pw.text);

        UnityWebRequest uwr = UnityWebRequest.Post(url, myForm);
        yield return uwr.SendWebRequest();

        if (uwr.isNetworkError)
        {
            Debug.Log("Error While Sending: " + uwr.error);

            yield break;
        }
        else
        {
            //빈 공란이 있다면 로그인이 안된다.
            if (m_inputfield_email.text.Length == 0 || m_inputfield_pw.text.Length == 0)
            {
                m_popup_message.SetActive(true);
                m_popup_message_text.text = "입력란을 모두 채워주세요.";
                yield break;
            }

            //이메일/비밀번호를 치고 로그인을 하고 서버로부터 넘어온값이 true라면 데이터가 맞는것이다. 로그인을 한다.
            if (string.Compare(uwr.downloadHandler.text, "true") == 1)
            {
                StartCoroutine(S2C_InfoManager(m_inputfield_email.text));
                m_popup_login_message.SetActive(true);
                m_popup_login_message_text.text = "비밀번호 수정 되었습니다.\n로그인이 완료되었습니다.";
            }
            //중복확인을 누른후 서버로부터 중복이 false라고 넘어온다면 사용 가능한 아이디이다.
            else if (string.Compare(uwr.downloadHandler.text, "false") == 1)
            {
                m_popup_message.SetActive(true);
                m_popup_message_text.text = "다시 로그인을 해주세요.";
            }
            //그 외에 값이 넘어온다면 에러로 간주하고 에러메세지를 띄운다.
            else
            {
                m_popup_message.SetActive(true);
                m_popup_message_text.text = uwr.downloadHandler.text;
            }
        }

    }


    public IEnumerator SendWebLogin()
    {
        string url = "49.247.131.35/login.php";
        WWWForm myForm = new WWWForm();
        myForm.AddField("email", m_inputfield_email.text);
        myForm.AddField("pw", m_inputfield_pw.text);

        UnityWebRequest uwr = UnityWebRequest.Post(url, myForm);
        yield return uwr.SendWebRequest();

        if (uwr.isNetworkError)
        {
            Debug.Log("Error While Sending: " + uwr.error);

            yield break;
        }
        else
        {
            //빈 공란이 있다면 로그인이 안된다.
            if (m_inputfield_email.text.Length == 0 || m_inputfield_pw.text.Length == 0)
            {
                m_popup_message.SetActive(true);
                m_popup_message_text.text = "입력란을 모두 채워주세요.";
                yield break;
            }


            //이메일/비밀번호를 치고 로그인을 하고 서버로부터 넘어온값이 true라면 데이터가 맞는것이다. 로그인을 한다.
            if (string.Compare(uwr.downloadHandler.text, "true") == 1)
            {
                StartCoroutine(S2C_InfoManager(m_inputfield_email.text));
                m_popup_login_message.SetActive(true);
                m_popup_login_message_text.text = "로그인이 완료되었습니다.";
            }
            //중복확인을 누른후 서버로부터 중복이 false라고 넘어온다면 사용 가능한 아이디이다.
            else if (string.Compare(uwr.downloadHandler.text, "false") == 1)
            {
                m_popup_message.SetActive(true);
                m_popup_message_text.text = "다시 로그인을 해주세요.";
            }
            //그 외에 값이 넘어온다면 에러로 간주하고 에러메세지를 띄운다.
            else
            {
                m_popup_message.SetActive(true);
                m_popup_message_text.text = uwr.downloadHandler.text;
            }
        }
    }

    private void OnDestroy()
    {
        string findpw = PlayerPrefs.GetString("FINDPW", "null");

        if (findpw != "null")
        {
            PlayerPrefs.DeleteKey("FINDPW");
        }

    }

    //로그인이 정상적으로 되면 호출한다.    
    public IEnumerator S2C_InfoManager(string _email)
    {
        //HTTP통신을 할 대상 주소
        string url = "49.247.131.35/S2C_InfoManager.php";

        //====================================================================================클라이언트->서버 보낼 데이터

        WWWForm myForm = new WWWForm();
        myForm.AddField("email", _email);

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
            string _nickname = (string)jobject["m_nickname"];

            //==================================================================================회원이 장착중인 아이템 정보
            string _carAssetName = (string)jobject["m_car"];
            string _wheelAssetName = (string)jobject["m_wheel"];
            string _wingAssetName = (string)jobject["m_wing"];

            //==================================================================================맵정보
            JArray _maps = (JArray)jobject["m_maps"];


            for (int i = 0; i < _maps.Count; i++)
            {
                //버튼 최상 부모의 이름을 차이름으로 설정하여 어떤차를 클릭했는지 알고, 바로 경로에서 3D 모델을 불러올수있도록 구상
                string _assetname = (string)_maps[i]["m_assetname"];
                string _name = (string)_maps[i]["m_name"];

                m_InfoManager.m_map.Add(_assetname, _name);
            }

            m_InfoManager.m_email = _email;
            m_InfoManager.m_nickname = _nickname;
            m_InfoManager.m_carAssetName = _carAssetName;
            m_InfoManager.m_wheelAssetName = _wheelAssetName;
            m_InfoManager.m_wingAssetName = _wingAssetName;
           
        }
    }

}
