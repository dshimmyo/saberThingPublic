using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HudLocator : MonoBehaviour {

    private Transform _CameraTransform;
    [SerializeField] private Vector3 followOffset;
    [SerializeField] private float speed = 15f;
    private float realDeltaTime = 0;
    private float previousFrameTime = 0;
    private Transform _transform;//don't think this is necessary but probably not redundant either

    void Start () {
        if (_CameraTransform == null) _CameraTransform = Camera.main.gameObject.transform;
        if (UnityEngine.XR.XRDevice.isPresent)
        {
            if (_CameraTransform == null) _CameraTransform = GameObject.Find("Camera (eye)").transform;
            if (_CameraTransform == null) _CameraTransform = GameObject.Find("CenterEyeAnchor").transform;//quest
            if (_CameraTransform == null) _CameraTransform = Camera.main.gameObject.transform;
        }
        previousFrameTime = Time.realtimeSinceStartup;
        Debug.Log("Camera Transform: " + _CameraTransform.name);
        _transform = gameObject.transform;
    }

    void Update () {
        //float deltaTime = Time.deltaTime;
        float _realtimeSinceStartup = Time.realtimeSinceStartup;
        realDeltaTime = _realtimeSinceStartup - previousFrameTime;
        previousFrameTime = _realtimeSinceStartup;

        Vector3 camPos = _CameraTransform.position;
        Vector3 newPos = camPos + _CameraTransform.forward * followOffset.z + _CameraTransform.right * followOffset.x + _CameraTransform.up * followOffset.y;
        _transform.position = Vector3.Lerp(_transform.position,newPos, realDeltaTime * speed);
        _transform.LookAt(camPos + _CameraTransform.right * followOffset.x,_CameraTransform.up);
    }
}
