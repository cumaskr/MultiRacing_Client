using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SC_garageWheelButton : MonoBehaviour
{
    //바퀴 버튼 자기자신의 버튼속성
    Button m_carbutton;
    //바퀴매니져
    SC_garageWheelManager m_garageWheelmanager;

    // Start is called before the first frame update
    void Start()
    {
        m_carbutton = this.gameObject.GetComponent<Button>();

        m_garageWheelmanager = GameObject.Find("Garage_WheelManager").GetComponent<SC_garageWheelManager>();

        //클릭하면 실행할 이벤트 함수를 등록한다.
        m_carbutton.onClick.AddListener(delegate { SendToGarageManager(this.gameObject); });

    }

    // Update is called once per frame
    void Update()
    {

    }

    //garagemanager에서 이함수로 넘어온 _buttonname을가지고 회전하는 차모델을 바꾼다.
    void SendToGarageManager(GameObject _selectcarButton)
    {
        if (m_garageWheelmanager)
        {
            //차고매니져에게 나 자신을 넘겨준다.(여기서 나자신은 바뀌게 될 차량의 버튼)
            m_garageWheelmanager.ChangeCar(_selectcarButton);
        }

    }
}
