using UnityEngine;

public class Rotater : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private float speed = 360f;
    [SerializeField] private Vector3 axis = new Vector3(0f, 0f, 1f);

    private void Update()
    {
        transform.Rotate(axis.normalized * speed * Time.deltaTime, Space.Self);
    }
}
