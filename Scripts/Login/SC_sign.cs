using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class SC_sign : MonoBehaviour
{
    public Toggle m_online;
    public Toggle m_information;    
    public GameObject m_popup_signMember;
    public GameObject m_popupsignMember_parent;

    GameObject m_popup_message;

    // Start is called before the first frame update
    void Start()
    {
        m_popup_message = GameObject.Find("Canvas").transform.GetChild(1).gameObject;
        m_popupsignMember_parent = GameObject.Find("Panel");
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnClickToSignMember()
    {
        //온라인약관 하고 개인정보약관 둘다 동의를 눌렀다면
        if (m_online.isOn == true && m_information.isOn == true)
        {
            //회원가입 양식 창을 킨다.
            m_popup_signMember.SetActive(true);

            GameObject _prefabSign = Instantiate(m_popup_signMember, new Vector3(0, 0, 0), Quaternion.identity);
            _prefabSign.transform.SetParent(m_popupsignMember_parent.transform, false);


            //이용약관 창은 끈다.
            Destroy(this.gameObject);
        }
        else
        {
            m_popup_message.SetActive(true);
        }
    }
}
