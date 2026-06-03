using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 8, 6);
    public float followSpeed = 5f;
    public float lookAtHeight = 1f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);

        Vector3 lookPos = target.position + Vector3.up * lookAtHeight;
        transform.LookAt(lookPos);
    }
}
