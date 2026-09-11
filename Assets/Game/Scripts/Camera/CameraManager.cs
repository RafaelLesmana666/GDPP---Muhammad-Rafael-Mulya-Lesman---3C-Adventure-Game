using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField]
    public CameraState CameraState;

    [SerializeField]
    private CinemachineCamera _fpsCamera;

    [SerializeField]
    private CinemachineCamera _tpsCamera;

    [SerializeField]
    private InputManager _inputManager;

    public Action OnChangePerspective;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _inputManager.OnChangePOV += SwitchCamera;
    }

    private void OnDestroy()
    {
        _inputManager.OnChangePOV -= SwitchCamera;
    }

    // Update is called once per frame
    void Update() { }

    public void SetFPSClampedCamera(bool isClamped, Vector3 playerRotation)
    {
        CinemachinePanTilt panTilt = _fpsCamera.GetComponent<CinemachinePanTilt>();
        if (isClamped)
        {
            panTilt.PanAxis.Wrap = false;
            panTilt.PanAxis.Range = new Vector2(playerRotation.y - 45, playerRotation.y + 45);
        }
        else
        {
            panTilt.PanAxis.Wrap = true;
            panTilt.PanAxis.Range = new Vector2(-180, 180);
        }
    }

    public void SetTPSFieldOfView(float fieldOfView)
    {
        _tpsCamera.Lens.FieldOfView = fieldOfView;
    }

    private void SwitchCamera()
    {
        OnChangePerspective();

        if (CameraState == CameraState.ThirdPerson)
        {
            CameraState = CameraState.FirstPerson;
            _tpsCamera.gameObject.SetActive(false);
            _fpsCamera.gameObject.SetActive(true);
        }
        else
        {
            CameraState = CameraState.ThirdPerson;
            _fpsCamera.gameObject.SetActive(false);
            _tpsCamera.gameObject.SetActive(true);
        }
    }
}
