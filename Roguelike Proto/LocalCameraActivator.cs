using UnityEngine;
using Mirror;

public class LocalCameraActivator : NetworkBehaviour
{
    [Header("Ссылка на камеру игрока")]
    public Camera playerCamera;
    public AudioListener audioListener;

    void Awake()
    {
        // если не указали руками — ищем в детях (включая неактивных)
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>(true);

        if (audioListener == null && playerCamera != null)
            audioListener = playerCamera.GetComponent<AudioListener>();

        // ПО УМОЛЧАНИЮ выключаем камеру и звук у всех
        if (playerCamera != null) playerCamera.enabled = false;
        if (audioListener != null) audioListener.enabled = false;
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        // ВКЛЮЧАЕМ камеру и звук только у локального игрока
        if (playerCamera != null) playerCamera.enabled = true;
        if (audioListener != null) audioListener.enabled = true;
    }
}
