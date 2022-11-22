using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class LogPosition : MonoBehaviour
{
    [SerializeField]
    private bool logPosition;

    private void Update()
    {
        if (logPosition)
        {
            logPosition = false;
            Debug.Log(transform.position.y);
        }
    }
}
