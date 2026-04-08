using UnityEngine;

public class UiFollowController
{
    private Vector3 followVelocity;
    private Vector3 worldOffset;
    private Quaternion fixedRotation;

    public Vector3 GetTargetPosition(Transform cameraTransform, float uiDistance, Vector3 uiOffset)
    {
        return cameraTransform.position
               + cameraTransform.forward * uiDistance
               + cameraTransform.right * uiOffset.x
               + cameraTransform.up * uiOffset.y
               + cameraTransform.forward * uiOffset.z;
    }

    public Quaternion GetTargetRotation(Transform cameraTransform, Vector3 canvasPosition)
    {
        Vector3 toCamera = cameraTransform.position - canvasPosition;
        toCamera.y = 0f;
        if (toCamera.sqrMagnitude < 0.001f)
        {
            toCamera = -cameraTransform.forward;
        }

        return Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
    }

    public void InitializeOffsets(Transform cameraTransform, Transform canvasTransform)
    {
        if (cameraTransform == null || canvasTransform == null)
            return;

        worldOffset = canvasTransform.position - cameraTransform.position;
        fixedRotation = canvasTransform.rotation;
    }

    public void UpdateFollow(
        Transform cameraTransform,
        Transform canvasTransform,
        bool followPositionOnly,
        float uiDistance,
        Vector3 uiOffset,
        float followAngleThreshold,
        float followDistanceThreshold,
        float followSpeed,
        float rotateSpeed)
    {
        if (cameraTransform == null || canvasTransform == null)
            return;

        Vector3 targetPos = followPositionOnly
            ? cameraTransform.position + worldOffset
            : GetTargetPosition(cameraTransform, uiDistance, uiOffset);

        Quaternion targetRot = followPositionOnly
            ? fixedRotation
            : GetTargetRotation(cameraTransform, canvasTransform.position);

        float angleToTarget = Vector3.Angle(cameraTransform.forward, targetPos - cameraTransform.position);
        float distanceToTarget = Vector3.Distance(canvasTransform.position, targetPos);

        if (angleToTarget > followAngleThreshold || distanceToTarget > followDistanceThreshold)
        {
            canvasTransform.position = Vector3.SmoothDamp(
                canvasTransform.position,
                targetPos,
                ref followVelocity,
                1f / Mathf.Max(0.01f, followSpeed));

            if (!followPositionOnly)
            {
                canvasTransform.rotation = Quaternion.Slerp(
                    canvasTransform.rotation,
                    targetRot,
                    Time.deltaTime * rotateSpeed);
            }
        }
    }
}
