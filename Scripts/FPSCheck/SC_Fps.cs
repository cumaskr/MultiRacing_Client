using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SC_Fps : MonoBehaviour
{
    float deltaTime = 0.0f;

    private float TimeLeft = 1.0f;
    private float nextTime = 0.0f;

    int m_Count = 0;

    void MoveMoles()
    {
        //m_Count = 0;
        //Debug.Log("1초");
    }
    

    private void Start()
    {
        StartCoroutine(FPS30());
    }

    void Update()
    {
        //deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

        //1초마다 실행
        if (Time.time > nextTime)
        {
            nextTime = Time.time + TimeLeft;

            Debug.Log(m_Count);
        }

       


        //Debug.Log(Time.deltaTime);
    }

    //void OnGUI()
    //{
    //    int w = Screen.width, h = Screen.height;

    //    GUIStyle style = new GUIStyle();

    //    Rect rect = new Rect(0, 0, w, h * 2 / 100);
    //    style.alignment = TextAnchor.UpperLeft;
    //    style.fontSize = 50;
    //    style.normal.textColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    //    float msec = deltaTime * 1000.0f;
    //    float fps = 1.0f / deltaTime;
    //    string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
    //    GUI.Label(rect, text, style);
    //}

    IEnumerator FPS30()
    {
        
        while (true)
        {
            m_Count++;
            yield return new WaitForSeconds(0.0005f);
        }
    }

}
