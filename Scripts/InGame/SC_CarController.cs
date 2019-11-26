using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//=============================================추가적으로 필요한 라이브러리
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
//==========================================================================================

//===============================================================옵션에 따른 세부조정

//1. 최고속도 (몇 Km/h 까지 속도낼수 있는지? 100 / 200)
//m_maxSpeed

//2. 가    속 (얼마나 속도가 빨리올라가냐?)
//motorForce(3천 넘어가면 앞뒤로 왔다갔다 컨트롤이 안됨 마찰력이 적어서 (1000 ~ 2000사이가 적당하다.)


//3. 핸들링(드리프트시 얼마나 잘 미끌어지고 속도감소없이 회전하냐)
//모든 바퀴 WheelCollider의 ForwardFriction의 ExtremumValue 0.5~2.0 작을수록 덜미끌어지고 속도 감소 심함
////모든 바퀴 WheelCollider의 SideFriction의 ExtremumValue 2.0~2.3 클수록 확꺽인다
//===============================================================


public class SC_CarController : MonoBehaviour
{

    SC_CameraController m_camera;

    //좌우 버튼 키 입력을 받는 변수
    float m_horizontalInput;
    //상하 버튼 키 입력을 받는 변수
    float m_verticalInput;

    //실제 움직이는 바퀴
    public WheelCollider frontDriverW, frontPassengerW;
    public WheelCollider rearDriverW, rearPassengerW;

    //실제 움직이는 바퀴에 띄울 3D메쉬
    public Transform frontDriverT, frontPassengerT;
    public Transform rearDriverT, rearPassengerT;

    //회전각도 Input.좌우 값에 따라서 방향이 바뀐다.
    private float m_steeringAngle;

    //최소 회전 각도(30도)
    public float m_minSteerAngle;

    //최대 회전 각도(30도)
    public float m_maxSteerAngle;


    //앞뒤로 가는 모터 힘
    float motorForce;

    //브레이크 힘 : motorForce * 10;
    float breakForce;

    //브레이크 키를 눌럿냐?
    bool m_isbreak;
    //드리프트 중이냐?
    bool m_isDrift;
    
    //현재 속도(km/h)
    float m_currentSpeed;
    //최고 속도(km/h)
    public float m_maxSpeed;

    float _steerAngle;

    Vector3 prevPos;
    public GameObject skidPrefab;
    float skidTime;


    //RPM -> Km/h로 변환해서 띄울곳
    public Text m_kmPh;

    public GameObject m_speedometer;

    public GameObject m_boost_particle;

    public GameObject m_boost_particle2;
    


    //게이지 관련 ===============================================================================

    float m_gage;    
    bool m_isboost;

    public GameObject m_img_gage;

    //===========================================================================================
    SC_GameManager m_gameManager;

    //아이템에 따른 차량 옵션
    public int m_itemMaxSpeed = 0;
    public int m_itemMotorForce = 0;
    public int m_itemHandling = 0;


    private void Awake()
    {        
        m_gameManager = GameObject.Find("GameManager").GetComponent<SC_GameManager>();        
    }

    // Start is called before the first frame update
    void Start()
    {
        
        if (m_gameManager == null) { Debug.LogError("No GameManagerObject"); }

        //무게중심을 내려서 쉽게 전복되는 현상을 막는다.
        GetComponent<Rigidbody>().centerOfMass = new Vector3(0, -1, 0);

        Init();        
    }

    // Update is called once per frame
    void Update()
    {
        if (m_isbreak == false)
        {
            Drift();
        }
    }

    private void FixedUpdate()
    {

        //플레이가 가능하면
        if (m_gameManager.m_isPlay)
        {            
            GetInput();

            Steer();

            if (m_isbreak)
            {
                Breaking();
            }
            else
            {
                Accelerate();
            }

            UpdatesWheelMesh();

            //  km/h = 원의둘레 * rpm -> m/m => km/h로변환하려면 60/1000을 해준다. 1m = 1/1000 km 이다. 
            //m_currentSpeed = Mathf.Round((2 * frontDriverW.radius * Mathf.PI) * frontDriverW.rpm * (60.0f / 1000.0f));



            //1초당 1M -> 1시간당 1KM
            m_currentSpeed = Mathf.Round(GetComponent<Rigidbody>().velocity.magnitude * 3.6f);

            m_speedometer.GetComponent<Image>().fillAmount = m_currentSpeed / m_maxSpeed;

            m_kmPh.text = m_currentSpeed.ToString();

            WheelHit hit;

            rearDriverW.GetComponent<WheelCollider>().GetGroundHit(out hit);

            float currentFrictionValue = hit.sidewaysSlip;

            currentFrictionValue = Mathf.Abs(currentFrictionValue);
            //Debug.Log(currentFrictionValue);

            if (currentFrictionValue >= 0.1f)
            {
                setSkidMark();

                m_img_gage.GetComponent<Image>().fillAmount = m_gage / 100.0f;
                m_gage += 0.5f;

                if (m_isboost == false && m_gage >= 100.0f)
                {
                    m_gage = 100.0f;

                    m_img_gage.GetComponent<Image>().color = new Color(1, 0, 0, 1);

                    m_isboost = true;
                }
            }

            if (m_isboost)
            {
                if (Input.GetKeyDown(KeyCode.E) && frontPassengerW.brakeTorque == 0.0f)
                {
                    m_gage = 0.0f;
                    m_img_gage.GetComponent<Image>().fillAmount = m_gage / 100.0f;
                    m_img_gage.GetComponent<Image>().color = new Color(0, 0, 1, 1);
                    m_isboost = false;
                    StartCoroutine(StartBoost());
                }
            }
        }
    }



    //SC_GameManager-> CinematicCamera()에서 시네마틱 카메라가 다 호출 되고 나면 InitUI를 호출한다.
    public void CameraControllerToUserObject()
    {       
        //카메라가 따라올 오브젝트에 나를 넘겨준다.==================================================================
        m_camera = GameObject.Find("Main Camera").GetComponent<SC_CameraController>();

        m_camera.objectToFollow = this.gameObject.transform;
        //===========================================================================================================
    }

    public void Init()
    {
        StartCoroutine(S2C_Stat());

        m_maxSteerAngle = 30.0f;

        _steerAngle = m_maxSteerAngle;

        m_isDrift = false;

        m_gage = 0.0f;

        m_isboost = false;
        
        m_boost_particle2.SetActive(false);
        
        //속도계 표시셋팅=====================================================
        m_speedometer = m_gameManager.m_ingameUI.transform.Find("Speed_metor").gameObject;

        m_kmPh = m_speedometer.transform.GetChild(1).GetComponent<Text>();
        //====================================================================

        //게이지 표시 셋팅
        m_img_gage = m_gameManager.m_ingameUI.transform.Find("DriftGage").transform.GetChild(0).gameObject;

        m_img_gage.GetComponent<Image>().fillAmount = m_gage;
        //====================================================================
        
    }

    public void GetInput()
    {
        m_horizontalInput = Input.GetAxis("Horizontal");
        m_verticalInput = Input.GetAxis("Vertical");

        m_isbreak = Input.GetKey(KeyCode.Q);

        if (Input.GetKeyUp(KeyCode.Q))
        {
            m_isbreak = false;
        }

    }

    public void Steer()
    {
        //      //최대 속도에서 현재 속도의 비율
        float _speedRate = m_currentSpeed / m_maxSpeed;

        //_speedRate는 0 ~ 1 사이에 값을 갖는다. 
        //0일때는 m_minSteerAngle각도까지 돌릴 수 있고, 1일때는 m_maxSteerAngle까지 밖에 못돌린다.
        //즉 속도가 풀일때는 m_maxSteerAngle의 절반 각도로만 돌릴 수 있다.

        if (m_isDrift == false)
        {
            //_steerAngle = Mathf.Lerp(m_maxSteerAngle, m_minSteerAngle, _speedRate);
            _steerAngle = m_maxSteerAngle;
        }

        //Debug.Log(_steerAngle);

        m_steeringAngle = _steerAngle * m_horizontalInput;

        frontDriverW.steerAngle = m_steeringAngle;
        frontPassengerW.steerAngle = m_steeringAngle;
    }

    public void Drift()
    {
        if (Input.GetKey(KeyCode.Space) && m_isDrift == false)
        {
            WheelFrictionCurve wfc = new WheelFrictionCurve();
            wfc.asymptoteSlip = rearDriverW.sidewaysFriction.asymptoteSlip;
            wfc.asymptoteValue = rearDriverW.sidewaysFriction.asymptoteValue;
            wfc.extremumSlip = rearDriverW.sidewaysFriction.extremumSlip;
            wfc.extremumValue = rearDriverW.sidewaysFriction.extremumValue;
            wfc.stiffness = 0.1f;
            //frontDriverW.sidewaysFriction = wfc;
            //frontPassengerW.sidewaysFriction = wfc;
            rearDriverW.sidewaysFriction = wfc;
            rearPassengerW.sidewaysFriction = wfc;

            _steerAngle = 30.0f;
            m_isDrift = true;
        }

        if (Input.GetKeyUp(KeyCode.Space) && m_isDrift == true)
        {

            WheelFrictionCurve wfc = new WheelFrictionCurve();
            wfc.asymptoteSlip = rearDriverW.sidewaysFriction.asymptoteSlip;
            wfc.asymptoteValue = rearDriverW.sidewaysFriction.asymptoteValue;
            wfc.extremumSlip = rearDriverW.sidewaysFriction.extremumSlip;
            wfc.extremumValue = rearDriverW.sidewaysFriction.extremumValue;
            wfc.stiffness = 1.0f;
            //frontDriverW.sidewaysFriction = wfc;
            //frontPassengerW.sidewaysFriction = wfc;
            rearDriverW.sidewaysFriction = wfc;
            rearPassengerW.sidewaysFriction = wfc;

            _steerAngle = 20.0f;
            m_isDrift = false;


        }



    }

    public void Accelerate()
    {
        if (m_verticalInput != 0.0f)
        {
            if (m_currentSpeed >= m_maxSpeed)
            {
                frontDriverW.motorTorque = 0.0f;
                frontPassengerW.motorTorque = 0.0f;
                //frontDriverW.brakeTorque = breakForce / 3;
                //frontPassengerW.brakeTorque = breakForce / 3;
            }
            else if (m_currentSpeed < -m_maxSpeed)
            {
                frontDriverW.motorTorque = 0.0f;
                frontPassengerW.motorTorque = 0.0f;
                //frontDriverW.brakeTorque = breakForce / 3;
                //frontPassengerW.brakeTorque = breakForce / 3;
            }
            else
            {
                frontDriverW.motorTorque = m_verticalInput * motorForce;
                frontPassengerW.motorTorque = m_verticalInput * motorForce;

                frontDriverW.brakeTorque = 0.0f;
                frontPassengerW.brakeTorque = 0.0f;
            }


        }
        //아무것도 안누르면 서서히 서게 한다.
        else
        {
            frontDriverW.brakeTorque = breakForce;
            frontPassengerW.brakeTorque = breakForce;
            frontDriverW.motorTorque = 0.0f;
            frontPassengerW.motorTorque = 0.0f;
        }




    }

    public void Breaking()
    {
        frontDriverW.motorTorque = 0.0f;
        frontPassengerW.motorTorque = 0.0f;
        frontDriverW.brakeTorque = breakForce;
        frontPassengerW.brakeTorque = breakForce;

    }

    public void UpdatesWheelMesh()
    {
        UpdatesWheelMesh(frontDriverW, frontDriverT);
        UpdatesWheelMesh(frontPassengerW, frontPassengerT);
        UpdatesWheelMesh(rearDriverW, rearDriverT);
        UpdatesWheelMesh(rearPassengerW, rearPassengerT);
    }

    public void UpdatesWheelMesh(WheelCollider _collider, Transform _transform)
    {
        //실제 움직이는 WheelCollider의 위치와 회전값을 가져와서 그대로 메쉬 transform에 적용한다.(즉 mesh도 WheelCollider와 같이 돌아간다.)
        Vector3 _pos = _transform.position;
        Quaternion _quat = _transform.rotation;

        _collider.GetWorldPose(out _pos, out _quat);
        
        _transform.position = _pos;
        _transform.rotation = _quat;
        
    }


    void setSkidMark()
    {
        Quaternion _quat = rearDriverW.transform.rotation;
        _quat.x = 0.0f;
        _quat.z = 0.0f;

        WheelHit _driver_hit;
        rearDriverW.GetComponent<WheelCollider>().GetGroundHit(out _driver_hit);

        WheelHit _passenger_hit;
        rearPassengerW.GetComponent<WheelCollider>().GetGroundHit(out _passenger_hit);


        GameObject skidInstance3 = (GameObject)Instantiate(skidPrefab, new Vector3(rearDriverW.transform.position.x, _driver_hit.point.y + 0.01f, rearDriverW.transform.position.z), _quat);

        _quat = rearPassengerW.transform.rotation;
        _quat.x = 0.0f;
        _quat.z = 0.0f;

        GameObject skidInstance4 = (GameObject)Instantiate(skidPrefab, new Vector3(rearPassengerW.transform.position.x, _passenger_hit.point.y + 0.01f, rearPassengerW.transform.position.z), _quat);

    }
    
    IEnumerator StartBoost()
    {
        float _timer = 0.0f;

        m_boost_particle.GetComponent<ParticleSystem>().Play();
        
        m_boost_particle2.SetActive(true);


        while (_timer < 1.0f)
        {
            transform.GetComponent<Rigidbody>().AddForce(transform.forward * (motorForce * 5));
            _timer += Time.deltaTime;

            yield return new WaitForSeconds(0.001f);
        }

        m_boost_particle.GetComponent<ParticleSystem>().Stop();
        
        m_boost_particle2.SetActive(false);

    }

    //true 이면 충돌 무시 false 이면 충돌
    public void SetCarForTriggerMine(bool _isOn)
    {
        if (m_gameManager.m_isMultiPlay == false)
        {
            if (_isOn)
            {
                GetComponent<Rigidbody>().useGravity = false;
                GetComponent<Rigidbody>().isKinematic = true;
                transform.Find("Body").GetComponent<BoxCollider>().enabled = false;
                transform.Find("Wheel_Collider").gameObject.SetActive(false);
            }
            else
            {
                GetComponent<Rigidbody>().useGravity = true;
                GetComponent<Rigidbody>().isKinematic = false;
                transform.Find("Body").GetComponent<BoxCollider>().enabled = true;
                transform.Find("Wheel_Collider").gameObject.SetActive(true);
            }
        }
        else
        {
            if (_isOn)
            {
                GetComponent<Rigidbody>().useGravity = false;
                GetComponent<Rigidbody>().isKinematic = true;
                transform.Find("Body").GetComponent<BoxCollider>().enabled = false;
                transform.Find("Wheel_Collider").gameObject.SetActive(false);
                m_gameManager.m_client.C2S_SendMessage("GAMEFALL:" + m_gameManager.m_InfoManager.m_nickname);
            }
            else
            {
                GetComponent<Rigidbody>().useGravity = true;
                GetComponent<Rigidbody>().isKinematic = false;
                transform.Find("Body").GetComponent<BoxCollider>().enabled = true;
                transform.Find("Wheel_Collider").gameObject.SetActive(true);
                m_gameManager.m_client.C2S_SendMessage("GAMEFALL:" + m_gameManager.m_InfoManager.m_nickname);
            }
        }
       
    }

    private void OnTriggerEnter(Collider _col)
    {
        Debug.Log("OnTriggerEnter");

        if (_col.gameObject.tag == "TAG_FALL")
        {
            m_gameManager.SetTransformSpawn(m_gameManager.m_player);

            StartCoroutine(m_gameManager.StartFallGo());
        }
    }

    private void OnCollisionEnter(Collision _col)
    {
        Debug.Log("OnCollisionEnter");

        //바다나 절벽에 떨어졌다면
        if (_col.gameObject.tag == "TAG_FALL")
        {
            m_gameManager.SetTransformSpawn(m_gameManager.m_player);

            StartCoroutine(m_gameManager.StartFallGo());

        }

        //장애물이랑 부디치면 게이지 초기화한다.
        if (_col.gameObject.tag == "TAG_OBSTACLE")
        {
            frontDriverW.motorTorque = 0.0f;
            frontPassengerW.motorTorque = 0.0f;

            if (m_isDrift == true)
            {
                m_gage = 0.0f;
                m_isDrift = false;
            }
        }   
    }



    IEnumerator S2C_Stat()
    {
        //HTTP통신을 할 대상 주소
        string url = "49.247.131.35/S2C_Stat.php";

        //====================================================================================클라이언트->서버 보낼 데이터

        //로그인한 Email 정보를 가져온다.
        string _loginID = m_gameManager.m_InfoManager.m_email;

        WWWForm myForm = new WWWForm();
        myForm.AddField("email", _loginID);

        myForm.AddField("m_carAssetname", m_gameManager.m_InfoManager.m_carAssetName);
        myForm.AddField("m_wheelAssetname", m_gameManager.m_InfoManager.m_wheelAssetName);
        myForm.AddField("m_wingAssetname", m_gameManager.m_InfoManager.m_wingAssetName);

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


            m_itemMaxSpeed = (int.Parse(_cartopspeed) + int.Parse(_wheeltopspeed) + int.Parse(_wingtopspeed));
            m_itemMotorForce = (int.Parse(_caracceleration) + int.Parse(_wheelacceleration) + int.Parse(_wingacceleration));
            m_itemHandling = (int.Parse(_carhandling) + int.Parse(_wheelhandling) + int.Parse(_winghandling));

            SetStat();
            
        }
    }

    public void SetStat()
    {

        //최고속도 적용
        //m_itemMaxSpeed : 1,3
        m_maxSpeed = 70.0f + m_itemMaxSpeed * 10.0f;

        //가속 옵션값 적용
        //m_itemMortorForce 0,1,2
        motorForce = 1000.0f + 500 * m_itemMotorForce;

        breakForce = motorForce * 10.0f;

        //핸들링 옵션값 적용
        WheelFrictionCurve wfc = new WheelFrictionCurve();
        wfc.asymptoteSlip = rearDriverW.sidewaysFriction.asymptoteSlip;
        wfc.asymptoteValue = rearDriverW.sidewaysFriction.asymptoteValue;
        wfc.extremumSlip = rearDriverW.sidewaysFriction.extremumSlip;

        //핸들링 옵션값을 적용해준다.
        wfc.extremumValue = 2.0f + m_itemHandling * 0.1f;
        wfc.stiffness = 1.0f;

        frontDriverW.sidewaysFriction = wfc;
        frontPassengerW.sidewaysFriction = wfc;
        rearDriverW.sidewaysFriction = wfc;
        rearPassengerW.sidewaysFriction = wfc;
        
    }

}
