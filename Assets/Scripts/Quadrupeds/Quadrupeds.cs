using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Quadrupeds : MonoBehaviour
{
    public float maxMoveSpeed;
    public float inRotationMaxMoveSpeed;
    public float acceleration;
    public float rotationSpeed;

    private new Camera camera;
    private Quaternion smoothRotation;
    private float smoothSpeed;

    void Awake()
    {
        camera = Camera.main;
        smoothRotation = transform.rotation;
    }

    void Update()
    {
        Move();
    }

    private void Move()
    {
        //Get the motion vector entered by the player's inputs.
        Vector3 inputVector = new Vector3(Input.GetAxis("Horizontal"), 0.0f, Input.GetAxis("Vertical"));

        //Normalize the vector if it is too long
        float inputMgn = inputVector.magnitude;
        if (inputMgn > 1.0f)
            inputVector /= inputMgn;



        //Takes the directional vector of the camera
        Vector3 camFwd = camera.transform.forward;      

        //We are only interested in the horizontal direction of the vector.
        camFwd.y = 0.0f;
        camFwd.Normalize();


        //If there is a valid input there is a rotation.
        if (inputMgn > 0.01f)
        {
            //We turn the character slowly towards the direction of the camera.
            Quaternion targetRotation = Quaternion.LookRotation(camFwd, Vector3.up);
            smoothRotation = Quaternion.Slerp(smoothRotation, targetRotation, Time.deltaTime * rotationSpeed);

            //Apply the rotation
            transform.rotation = smoothRotation;
        }

        //Apply this rotation to the input vector. If the player goes forward, it will be the forward of his character.
        Vector3 moveVector = smoothRotation * inputVector;


        //Look at how well the character is aligned with the camera. If it is not well aligned, we use the inRotationMaxMoveSpeed instead of maxMoveSpeed.
        float alignement = Vector3.Dot(camFwd, smoothRotation * Vector3.forward) * 0.5f + 0.5f;
        float maxSpeed = Mathf.Lerp(inRotationMaxMoveSpeed, maxMoveSpeed, alignement);

        //The smooth speed tries to reach the maxmoveSpeed 
        smoothSpeed = Mathf.Lerp(smoothSpeed, maxSpeed, Time.deltaTime * acceleration);

        //Apply the translation
        transform.Translate(moveVector * Time.deltaTime * smoothSpeed, Space.World);
    }

}
