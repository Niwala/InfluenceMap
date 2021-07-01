using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbitCamera : MonoBehaviour
{

    public Transform target;
    public Vector2 viewportPosition;
    public float sensibility;
    public float distance = 5.0f;

    private float pitch;
    private float yaw;

    private Vector2 lastMousePosition;

    void Update()
    {
        //Inputs
        if (Input.GetMouseButton(0))
        {
            Vector2 mouseDelta = (Vector2)Input.mousePosition - lastMousePosition;
            lastMousePosition = Input.mousePosition;
            pitch += mouseDelta.y * sensibility * 0.01f;
            pitch = Mathf.Clamp(pitch, -Mathf.PI, Mathf.PI);

            yaw -= mouseDelta.x * sensibility * 0.01f;
        }



        Vector3 focusPoint = target.position;

        float cosPitch = Mathf.Cos(pitch);
        Vector3 offset = new Vector3(Mathf.Cos(yaw) * cosPitch, Mathf.Sin(pitch), Mathf.Sin(yaw) * cosPitch);

        transform.position = focusPoint + offset * distance;
        transform.LookAt(target.position);


    }
}
