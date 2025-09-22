using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow2D : MonoBehaviour
{
  
    [SerializeField] private Transform target;   // сюда перетащи игрока

    
    [SerializeField] private float smoothTime = 0.15f; // плавность
    private Vector3 _velocity;

   
    [SerializeField] private Collider2D levelBounds;

    private Camera _cam;
    private float _halfW, _halfH;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        if (!_cam.orthographic)
            _cam.orthographic = true; // для 2D
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // целевая позиция (оставляем Z камеры)
        Vector3 targetPos = new Vector3(target.position.x, target.position.y, transform.position.z);

        // если заданы границы — зажимаем внутри них с учётом размера экрана
        if (levelBounds != null)
        {
            UpdateHalfExtents();

            Bounds b = levelBounds.bounds;
            float minX = b.min.x + _halfW;
            float maxX = b.max.x - _halfW;
            float minY = b.min.y + _halfH;
            float maxY = b.max.y - _halfH;

            // если уровень меньше экрана — центрируем
            float clampedX = (minX <= maxX) ? Mathf.Clamp(targetPos.x, minX, maxX) : (b.center.x);
            float clampedY = (minY <= maxY) ? Mathf.Clamp(targetPos.y, minY, maxY) : (b.center.y);

            targetPos.x = clampedX;
            targetPos.y = clampedY;
        }

        // плавное следование
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _velocity, smoothTime);
    }

    private void UpdateHalfExtents()
    {
        _halfH = _cam.orthographicSize;
        _halfW = _halfH * _cam.aspect;
    }
}
