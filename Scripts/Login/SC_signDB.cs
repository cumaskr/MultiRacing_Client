using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Text.RegularExpressions;


public class SC_signDB : MonoBehaviour
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

    //닉네임 입력 받는 위젯
    public InputField m_nickname_inputfield;
    //닉네임 인증 완료후 띄울 메세지
    public Text m_nickname_text;

    //비밀번호 입력 받는 위젯
    public InputField m_pw_inputfield;

    //비밀번호 재입력 받는 위젯
    public InputField m_rpw_inputfield;

    public const string MatchEmailPattern = @"[a-zA-Z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-zA-Z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?\.)+[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?";



    public bool ValidateEmail(string email)
    {
        if (email != null)
            return Regex.IsMatch(email, MatchEmailPattern);
        else
            return false;
    }



    //=================================================멤버 변수=================================================
    bool m_isEmailCheck;
    bool m_isNicknameCheck;

    //=================================================라이프 사이클==============================================
    // Start is called before the first frame update
    void Start()
    {
        m_isEmailCheck = false;
        m_isNicknameCheck = false;
        m_email_text.gameObject.SetActive(false);
        m_nickname_text.gameObject.SetActive(false);
        
        m_popup_message = GameObject.Find("Canvas").transform.GetChild(1).gameObject;
        m_popup_message_text = m_popup_message.transform.GetChild(0).transform.GetChild(2).GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {

    }


    public void OnClickConfirmEmail()
    {

        //빈 공란이 있다면 중복확인이 안된다.
        if (m_email_inputfield.text.Length == 0)
        {
            m_popup_message.SetActive(true);
            m_popup_message_text.text = "입력란을 채워주세요.";
        }        
        else if (ValidateEmail(m_email_inputfield.text) == false)
        {
            m_popup_message.SetActive(true);
            m_popup_message_text.text = "이메일 형식이 아닙니다.";
        }
        else
        {
            StartCoroutine(this.SendWebConfirmEmail());
        }
       
    }

    public void OnClickConfirmNickname()
    {
        //빈 공란이 있다면 중복확인이 안된다.
        if (m_nickname_inputfield.text.Length == 0)
        {
            m_popup_message.SetActive(true);
            m_popup_message_text.text = "입력란을 채워주세요.";
        }
        else
        {
            StartCoroutine(this.SendWebConfirmNickname());
        }        
    }

    public void OnClickSign()
    {
        StartCoroutine(this.SendWebSign());
    }

    public IEnumerator SendWebConfirmEmail()
    {
        string url = "49.247.131.35/sign.php";
        WWWForm myForm = new WWWForm();
        myForm.AddField("what", "confirm_email");
        myForm.AddField("email", m_email_inputfield.text);
        UnityWebRequest uwr = UnityWebRequest.Post(url, myForm);
        yield return uwr.SendWebRequest();

        if (uwr.isNetworkError)
        {
            Debug.Log("Error While Sending: " + uwr.error);

            yield break;
        }
        else
        {
            //중복확인을 누른후 서버로부터 중복이 true라고 넘어온다면 사용 불가능한 아이디이다.
            if (string.Compare(uwr.downloadHandler.text, "true") == 1)
            {
                m_popup_message.SetActive(true);
                m_popup_message_text.text = "이미 사용중인 이메일 입니다.\n\n 다시입력해주세요.";
            }
            //중복확인을 누른후 서버로부터 중복이 false라고 넘어온다면 사용 가능한 아이디이다.
            else if (string.Compare(uwr.downloadHandler.text, "false") == 1)
            {
                m_popup_message.SetActive(true);
                m_popup_message_text.text = "사용 가능한 이메일 입니다.";
                m_isEmailCheck = true;
                InputFieldColorChange(m_email_text, m_isEmailCheck);
            }
            //그 외에 값이 넘어온다면 에러로 간주하고 에러메세지를 띄운다.
            else
            {
                m_popup_message.SetActive(true);
                m_popup_message_text.text = uwr.downloadHandler.text;
            }
        }
    }

    public IEnumerator SendWebConfirmNickname()
    {
        string url = "49.247.131.35/sign.php";
        WWWForm myForm = new WWWForm();                    
        myForm.AddField("what", "confirm_nickname");        
        myForm.AddField("nickname", m_nickname_inputfield.text);


        UnityWebRequest uwr = UnityWebRequest.Post(url, myForm);
        yield return uwr.SendWebRequest();

        if (uwr.isNetworkError)
        {
            Debug.Log("Error While Sending: " + uwr.error);

            yield break;
        }
        else
        {            
            //중복확인을 누른후 서버로부터 중복이 true라고 넘어온다면 사용 불가능한 닉네임이다.
            if (string.Compare(uwr.downloadHandler.text, "true") == 1)
            {
                m_popup_message.SetActive(true);
                m_popup_message_text.text = "이미 사용중인 닉네임 입니다.\n\n 다시입력해주세요.";
            }
            //중복확인을 누른후 서버로부터 중복이 false라고 넘어온다면 사용 가능한 아이디이다.
            else if (string.Compare(uwr.downloadHandler.text, "false") == 1)
            {
                m_popup_message.SetActive(true);
                m_popup_message_text.text = "사용 가능한 닉네임 입니다.";
                m_isNicknameCheck = true;
                InputFieldColorChange(m_nickname_text, m_isNicknameCheck);
            }
            //그 외에 값이 넘어온다면 에러로 간주하고 에러메세지를 띄운다.
            else
            {
                m_popup_message.SetActive(true);
                m_popup_message_text.text = uwr.downloadHandler.text;
            }
        }
    }

    public IEnumerator SendWebSign()
    {
        //빈 공란이 있다면 회원가입이 안된다.
        if (m_email_inputfield.text.Length == 0 || m_pw_inputfield.text.Length == 0 || m_rpw_inputfield.text.Length == 0 || m_nickname_inputfield.text.Length == 0)
        {
            m_popup_message.SetActive(true);
            m_popup_message_text.text = "입력란을 모두 채워주세요.";
            yield break;
        }

        //비밀번호 입력, 비밀번호 재입력 두 곳이 틀렸다면
        if (m_pw_inputfield.text != m_rpw_inputfield.text)
        {
            m_popup_message.SetActive(true);
            m_popup_message_text.text = "비밀번호/재입력 란을 다시 입력 해주세요.";
            yield break;
        }
        //이메일 중복 체크 했는지 예외처리
        if (m_isEmailCheck == false)
        {
            m_popup_message.SetActive(true);
            m_popup_message_text.text = "이메일 중복확인 체크를 해주세요.";
            yield break;
        }
        //닉네임 중복 체크 했는지 예외처리
        if (m_isNicknameCheck == false)
        {
            m_popup_message.SetActive(true);
            m_popup_message_text.text = "닉네임 중복확인 체크를 해주세요.";
            yield break;
        }

        string url = "49.247.131.35/sign.php";
        WWWForm myForm = new WWWForm();
        myForm.AddField("what", "sign");
        myForm.AddField("email", m_email_inputfield.text);
        myForm.AddField("pw", m_pw_inputfield.text);
        myForm.AddField("nickname", m_nickname_inputfield.text);

        UnityWebRequest uwr = UnityWebRequest.Post(url, myForm);
        yield return uwr.SendWebRequest();

        if (uwr.isNetworkError)
        {
            Debug.Log("Error While Sending: " + uwr.error);

            yield break;
        }
        else
        {
            //중복확인을 누른후 서버로부터 회원가입이 true라고 온다면 회원가입이 완료된것이다.
            if (string.Compare(uwr.downloadHandler.text, "true") == 1)
            {
                m_popup_message.SetActive(true);
                m_popup_message_text.text = "회원가입이 완료되었습니다.";
            }
            //그 외에 값이 넘어온다면 에러로 간주하고 에러메세지를 띄운다.
            else
            {
                m_popup_message.SetActive(true);
                m_popup_message_text.text = uwr.downloadHandler.text;
            }
        }
        this.gameObject.SetActive(false);
    }

    public void OnValueChangeEmail()
    {
        if (m_isEmailCheck == true)
        {            

            m_isEmailCheck = false;
            InputFieldColorChange(m_email_text, m_isEmailCheck);
        }
    }

    public void OnValueChangeNickname()
    {
        if (m_isNicknameCheck == true)
        {            
            m_isNicknameCheck = false;
            InputFieldColorChange(m_nickname_text, m_isNicknameCheck);
        }
    }

    void InputFieldColorChange(Text _signtext, bool _isChecked)
    {
        if (_isChecked)
        {
            _signtext.gameObject.SetActive(true);
        }
        else
        {
            _signtext.gameObject.SetActive(false);
        }
    }
}
