using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SC_CameraController : MonoBehaviour
{
    public enum E_MODE
    {
        PLAY,MOVE,FINISH
    }


    //따라갈 오브젝트 -> 플레이어(자동차)
    public Transform objectToFollow;
    //어느 위치 만큼 떨어질것이냐? (x,y,z)
    public Vector3 offset;
    //어느 속도로 따라 갈것이냐? (위치)
    public float followSpeed = 10.0f;
    //어느 속도로 볼 것이냐? (회전)
    public float lookSpeed = 10.0f;


    public E_MODE m_mode;

    private void Start()
    {
        m_mode = E_MODE.PLAY;
    }


    private void FixedUpdate()
    {        
        if (m_mode == E_MODE.PLAY && objectToFollow != null)
        {
            LookAtTarget();
            MoveToTarget();
        }
        else if (m_mode == E_MODE.FINISH)
        {
            RotateAround(objectToFollow.gameObject);
        }
        
    }

    public void LookAtTarget()
    {
        Vector3 _lookDirection = objectToFollow.position - this.transform.position;
        Quaternion _rot = Quaternion.LookRotation(_lookDirection, Vector3.up);
        transform.rotation = Quaternion.Lerp(transform.rotation, _rot, lookSpeed * Time.deltaTime);
    }

    public void MoveToTarget()
    {
        Vector3 _targetPos = objectToFollow.position +
                             objectToFollow.forward * offset.z +
                             objectToFollow.right * offset.x +
                             objectToFollow.up * offset.y;

        transform.position = Vector3.Lerp(transform.position, _targetPos, followSpeed * Time.deltaTime);

    }

    public void RotateAround(GameObject _target)
    {        
        transform.RotateAround(_target.transform.position, Vector3.up, Time.deltaTime * 10.0f);
    }


}
