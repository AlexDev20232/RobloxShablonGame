using UnityEngine;

/// <summary>
/// Постоянно крутит объект вокруг выбранной оси.
/// По умолчанию — полный оборот (360°) в секунду вокруг оси Z.
/// </summary>
public class Rotater : MonoBehaviour
{
    [Header("Скорость вращения (градусов в секунду)")]
    [SerializeField] private float speed = 360f;

    [Header("Ось вращения (1 — вращаем, 0 — нет)")]
    [SerializeField] private Vector3 axis = new Vector3(0f, 0f, 1f); // Z

    void Update()
    {
        // Вращаем каждый кадр: угол = скорость * время кадра
        transform.Rotate(axis.normalized * speed * Time.deltaTime, Space.Self);
        // Space.Self — вокруг собственной оси объекта. Можно поменять на Space.World.
    }
}
