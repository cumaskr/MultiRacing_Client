using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class SC_findPW : MonoBehaviour
{
    //================================================오브젝트 연결===============================================    
    //안내메세지를 띄울 오브젝트
    public GameObject m_popup_message;

    //안내메세지의 text 부분
    public Text m_popup_message_text;

    //이메일 입력 받는 위젯
    public InputField m_email_inputfield;
    //이메일 인증 완료후 띄울 메세지
    public Text m_email_text;


    // Start is called before the first frame update
    void Start()
    {
        m_popup_message = GameObject.Find("Canvas").transform.GetChild(1).gameObject;
        m_popup_message_text = m_popup_message.transform.GetChild(0).transform.GetChild(2).GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnClickFindPW()
    {

        //빈 공란이 있다면 중복확인이 안된다.
        if (m_email_inputfield.text.Length == 0)
        {
            m_popup_message.SetActive(true);
            m_popup_message_text.text = "입력란을 채워주세요.";
        }
        else
        {
            StartCoroutine(this.SendWebFindPW());
        }

    }

    public IEnumerator SendWebFindPW()
    {
        string url = "49.247.131.35/findPW.php";
        WWWForm myForm = new WWWForm();        
        myForm.AddField("email", m_email_inputfield.text);

        //=================================================================새로운 비밀번호 생성
        //Random.Range(1, 10) -> 1~9 까지 랜덤한수 반환
        string pw1 = (Random.Range(1, 10)).ToString();
        string pw2 = (Random.Range(1, 10)).ToString();
        string pw3 = (Random.Range(1, 10)).ToString();
        string pw4 = (Random.Range(1, 10)).ToString();
        string newpw = pw1 + pw2 + pw3 + pw4;

        GameObject loginmanager = GameObject.Find("Login_Manager");
        loginmanager.GetComponent<SC_loginmanager>().m_findpw = newpw;

        string StartDate = System.DateTime.Now.ToString();

        PlayerPrefs.SetString("FINDPW", StartDate);


        myForm.AddField("newpw", newpw);
        //=================================================================


        UnityWebRequest uwr = UnityWebRequest.Post(url, myForm);
        yield return uwr.SendWebRequest();

        if (uwr.isNetworkError)
        {
            Debug.Log("Error While Sending: " + uwr.error);

            yield break;
        }
        else
        {
            //비밀번호찾기 이메일 입력 후 서버로부터 해당이메일이 있어서 이메일로 비밀번호 전송후 true라고 넘어온닫면 이메일로 임시 비밀번호가 전송 되었습니다. 라고 메세지를 띄운다.
            if (string.Compare(uwr.downloadHandler.text, "true") == 1)
            {
                m_popup_message.SetActive(true);
                m_popup_message_text.text = "해당 이메일로 [임시비밀번호]가 전송되었습니다.";
                Destroy(this.gameObject);
            }
            //비밀번호찾기 이메일 입력 후 서버로부터 해당이메일이 없어서 false라고 넘어온다면 다시 입력해주세요 라고 메세지를 띄운다.
            else if (string.Compare(uwr.downloadHandler.text, "false") == 1)
            {
                m_popup_message.SetActive(true);
                m_popup_message_text.text = "이메일을 다시 입력해주세요.";                
            }
            //그 외에 값이 넘어온다면 에러로 간주하고 에러메세지를 띄운다.
            else
            {
                m_popup_message.SetActive(true);
                m_popup_message_text.text = uwr.downloadHandler.text;
            }
        }
    }


}
