using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ThirdPersonCam : MonoBehaviour
{
    #region Constant

    public const string INPUT_MOUSE_SCROLLWHEEL = "Mouse ScrollWheel";
    public const string ERROR_UN_BINDCAM = "ThirdPersonCam脚本没有绑定摄像机!"; //chinese text says "The script does not bind the camera"
    public const string ERROR_UN_PLAYER = "ThirdPersonCam脚本没有指定玩家"; //chinese text says "The script does not specify a player"

    /// The basic direction of the camera
    private Vector3 CamBaseAxis = Vector3.back;

    /// The distance from the intersection of the camera and the collision body to the observation point of the camera
    private float CollisionReturnDis = 0.5f;

    #endregion

    #region Variable

    /// Video camera
    private Transform mCamera;

    /// Player transform
    public Transform mPlayer;

    /// Character center point offset
    public Vector3 mPivotOffset = new Vector3(0.0f, 1.0f, 0.0f);

    /// Horizontal aiming speed
    public float mHorizontalAimingSpeed = 400.0f;

    /// Vertical aiming speed
    public float mVerticalAimingSpeed = 400.0f;

    /// Maximum vertical angle
    public float mMaxVerticalAngle = 30.0f;

    /// Minimum vertical angle
    public float mMinVerticalAngle = -60.0f;

    /// The maximum value of the magnification of the base camera shift
    public float mMaxDistance = 2.0f;

    /// The minimum value of the magnification of the base camera shift
    public float mMinDistance = 1.0f;

    /// The speed of lens advance
    public float mZoomSpeed = 5.0f;

    /// The angle of horizontal rotation
    private float mAngleH = 0.0f;

    /// Vertical rotation angle
    private float mAngleV = -30.0f;

    /// Base camera shift magnification
    private float mDistance = 0.0f;

    #endregion

    #region Built-in function

    void Awake()
    {
        mCamera = GetComponent<Camera>().transform;
        mDistance = (mMinDistance + mMaxDistance) * 0.5f;
    }

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (mCamera == null)
        {
            Debug.LogError(ERROR_UN_BINDCAM);
            return;
        }

        if (mPlayer == null)
        {
            Debug.LogError(ERROR_UN_PLAYER);
            return;
        }

        float mousex = Input.GetAxis("Mouse X");
        float mousey = Input.GetAxis("Mouse Y");
        float zoom = Input.GetAxis("Mouse ScrollWheel");
        mAngleH += (Mathf.Clamp(mousex, -1.0f, 1.0f) * mHorizontalAimingSpeed);
        mAngleV += (Mathf.Clamp(mousey, -1.0f, 1.0f) * mVerticalAimingSpeed);
        mDistance -= zoom * mZoomSpeed;

        mAngleV = Mathf.Clamp(mAngleV, mMinVerticalAngle, mMaxVerticalAngle);
        mDistance = Mathf.Clamp(mDistance, mMinDistance, mMaxDistance);

        Quaternion animRotation = Quaternion.Euler(-mAngleV, mAngleH, 0.0f);
        Quaternion camYRotation = Quaternion.Euler(0.0f, mAngleH, 0.0f);
        mCamera.rotation = animRotation;

        Vector3 lookatpos = mPlayer.position + camYRotation * mPivotOffset;
        Vector3 camdir = animRotation * CamBaseAxis;
        camdir.Normalize();
        mCamera.position = lookatpos + camdir * mDistance;

        // Calculate camera points after collision
        RaycastHit rayhit;
        bool hit = Physics.Raycast(lookatpos, camdir, out rayhit, mDistance);
        if (hit)
        {
            // Block character collision
            bool charcol = rayhit.collider as CharacterController;
            if (!charcol)
            {
                Vector3 modifypos = rayhit.normal * CollisionReturnDis * 2.0f;
                mCamera.position = rayhit.point + modifypos;

                // The distance correction is within the range (1, to avoid the camera interspersed into the character)
                float distance = Vector3.Distance(mCamera.position, lookatpos);
                distance = Mathf.Clamp(distance, mMinDistance, mMaxDistance);
                mCamera.position = lookatpos + camdir * distance + modifypos;
            }
        }
    }

    void OnDestroy()
    {

    }

    void LateUpdate()
    {

    }

    #endregion
}