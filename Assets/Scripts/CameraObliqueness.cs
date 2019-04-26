using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraObliqueness : MonoBehaviour {

    public Camera theCamera;

    public Transform clipTransform;


    private void Update()
    {
        if (clipTransform)
        {
            SetObliqueClip(clipTransform);
        }
    }

    private void SetObliqueClip(Transform clipTrans)
    {
        if (!theCamera)
        {
            return;
        }
        var normal = clipTrans.forward;
        Vector4 planeWorldSpace;
        planeWorldSpace.x = normal.x;
        planeWorldSpace.y = normal.y;
        planeWorldSpace.z = normal.z;
        planeWorldSpace.w = -Vector3.Dot(normal, clipTrans.position);
        Vector4 clipPlaneCameraSpace = Matrix4x4.Transpose(theCamera.cameraToWorldMatrix) * planeWorldSpace;
        theCamera.projectionMatrix = theCamera.CalculateObliqueMatrix(clipPlaneCameraSpace);
    }
}
