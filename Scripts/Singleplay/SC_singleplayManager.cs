using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[SerializeField]
public class C_map
{
    public string m_assetname;
    public string m_name;

    public C_map(string _assetname, string _name)
    {
        m_assetname = _assetname;
        m_name = _name;
    }
}

public class SC_singleplayManager : MonoBehaviour
{
    //싱글톤 정보매니져
    SC_InfoManager m_InfoManager;


    //========================================================멤버 변수
    GameObject m_mapPrefab;

    int m_mapIndex;

    public List<C_map> m_mapList;
    List<GameObject> m_rankList;
    //========================================================오브젝트 연결
    public GameObject m_camera;
    public GameObject m_rankPrefabParent;
    public GameObject m_rankPrefab;

    public Text m_mapName;
    
    private void Start()
    {
        m_InfoManager = GameObject.Find("InfoManager").GetComponent<SC_InfoManager>();

        m_mapList = new List<C_map>();
        m_rankList = new List<GameObject>();

        StartCoroutine(S2C_MapData());

        m_mapIndex = 0;
        m_mapPrefab = Instantiate(Resources.Load("Map/" + m_mapIndex.ToString()), new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
        
        StartCoroutine(S2C_RankList(m_mapIndex));
        
    }

    public void MapChangeLeft()
    {
        m_mapIndex -= 1;
        //맵 제일 왼쪽으로 갔을때(맵 시작 번호 1 )
        if (m_mapIndex < 0)
        {
            m_mapIndex = 0;
        }
        else
        {
            //3D 맵 삭제
            Destroy(m_mapPrefab);
            //랭크목록삭제
            for (int i = 0; i < m_rankList.Count; i++)
            {
                Destroy(m_rankList[i]);
            }
            //새로운 맵 생성
            m_mapPrefab = Instantiate(Resources.Load("Map/" + m_mapIndex.ToString()), new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
            //랭크 목록 불러온다.
            StartCoroutine(S2C_RankList(m_mapIndex));
            //맵 이름 변경
            m_mapName.text = m_mapList[m_mapIndex].m_name;
        }
    }

    public void MapChangeRight()
    {
        //3D 맵 삭제
        m_mapIndex += 1;
        //맵 제일 오른쪽으로 갔을때(맵 마지막 번호 2)
        if (m_mapIndex >= m_mapList.Count)
        {
            m_mapIndex = m_mapList.Count - 1;
        }
        else
        {
            Destroy(m_mapPrefab);
            //랭크목록삭제
            for (int i = 0; i< m_rankList.Count; i++)
            {
                Destroy(m_rankList[i]);
            }

            //새로운 맵 생성
            m_mapPrefab = Instantiate(Resources.Load("Map/" + m_mapIndex.ToString()), new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
            //랭크 목록 불러온다.
            StartCoroutine(S2C_RankList(m_mapIndex));
            //맵 이름 변경
            m_mapName.text = m_mapList[m_mapIndex].m_name;
        }
    }


    private void Update()
    {
        
    }


    IEnumerator S2C_MapData()
    {
        //로그인한 Email 정보를 가져온다.
        string _loginID = m_InfoManager.m_email;

        //HTTP통신을 할 대상 주소
        string url = "49.247.131.35/S2C_MapData.php";

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

            JArray _mapList = (JArray)jobject["m_mapList"];

            for (int i = 0; i < _mapList.Count; i++)
            {                
                string _assetname = (string)_mapList[i]["m_assetname"];
                string _name = (string)_mapList[i]["m_name"];

                C_map _map = new C_map(_assetname,_name);

                m_mapList.Add(_map);                
            }

            m_mapName.text = m_mapList[m_mapIndex].m_name;
        }
    }

    IEnumerator S2C_RankList(int _mapIndex)
    {
        //로그인한 Email 정보를 가져온다.
        string _loginID = m_InfoManager.m_email;

        //HTTP통신을 할 대상 주소
        string url = "49.247.131.35/S2C_RankList.php";

        WWWForm myForm = new WWWForm();
        myForm.AddField("email", _loginID);

        myForm.AddField("m_mapassetname", _mapIndex.ToString());

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

            //불러올 랭킹 목록이 있다면
            if ((string)jobject["m_empty"] == "false")
            {
                JArray _rankList = (JArray)jobject["m_rankList"];

                for (int i = 0; i < _rankList.Count; i++)
                {
                    GameObject _rankMember = Instantiate(m_rankPrefab, new Vector3(0, 0, 0), Quaternion.identity);

                    string _nickname = (string)_rankList[i]["m_nickname"];
                    string _minute = (string)_rankList[i]["m_minute"];
                    string _second = (string)_rankList[i]["m_second"];
                    string _millisecond = (string)_rankList[i]["m_millisecond"];

                    _rankMember.GetComponent<Text>().text = (i + 1).ToString() + " . " + _nickname + " - " + _minute + ":" + _second + ":" + _millisecond;

                    _rankMember.transform.SetParent(m_rankPrefabParent.transform);

                    _rankMember.transform.localPosition = new Vector3(0, 150 + -i * 100, 0);

                    m_rankList.Add(_rankMember);
                }
            }                                        
        }
    }

    public void SceneChangeRobby()
    {
        SceneManager.LoadScene("scene_robby");
    }

    public void SceneChangeInGame()
    {
        string _sceneName = "scene_map" + m_mapIndex.ToString();

        m_InfoManager.m_mapName = m_mapIndex.ToString();
        
        SceneManager.LoadScene(_sceneName);
    }

}
