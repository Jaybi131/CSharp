using UnityEngine;
using Mirror;

[RequireComponent(typeof(CharacterController))]
public class SimpleFPPlayer : NetworkBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3.5f;
    public float sprintSpeed = 6.2f;
    public float crouchSpeed = 2.0f;

    public float groundAcceleration = 28f;
    public float groundDeceleration = 32f;

    public float jumpHeight = 1.15f;
    public float gravity = 24f;
    public float groundStickForce = 3f;

    [Header("Mouse Look")]
    public float mouseSensitivityX = 2.2f;
    public float mouseSensitivityY = 2.0f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    [Header("Crouch")]
    public bool useToggleCrouch = false;
    public KeyCode crouchHoldKey = KeyCode.LeftControl;
    public KeyCode crouchToggleKey = KeyCode.C;
    public float standingHeight = 1.8f;
    public float crouchingHeight = 1.15f;
    public float crouchTransitionSpeed = 10f;

    [Header("References")]
    public Transform camPivot;
    public Animator anim;
    public Transform modelRoot;
    public Transform headBone;
    public Camera playerCamera;
    public AudioListener audioListener;
    public bool hideLocalBody = true;

    [Header("Animation Parameters")]
    public string moveXParam = "MoveX";
    public string moveYParam = "MoveY";
    public string speedParam = "Speed";
    public string isMovingParam = "isMoving";
    public string isRunningParam = "isRunning";
    public string isCrouchingParam = "isCrouching";
    public string jumpParam = "Jump";
    public string verticalSpeedParam = "VerticalSpeed";
    public string groundedParam = "Grounded";

    private CharacterController cc;

    private Quaternion headBaseLocalRotation;

    private float pitch;
    private float verticalVelocity;

    private bool isGrounded;
    private bool wasGrounded;
    private bool isCrouching;
    private bool jumpedThisFrame;

    private Vector3 planarVelocity;
    private Vector3 airbornePlanarVelocity;

    private float standCamLocalY;
    private float crouchCamLocalY;

    void Awake()
    {
        cc = GetComponent<CharacterController>();

        if (anim == null)
            anim = GetComponentInChildren<Animator>(true);

        if (modelRoot == null && anim != null)
            modelRoot = anim.transform;

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>(true);

        if (audioListener == null && playerCamera != null)
            audioListener = playerCamera.GetComponent<AudioListener>();

        if (headBone != null)
            headBaseLocalRotation = headBone.localRotation;

        if (cc != null)
            standingHeight = cc.height;

        if (camPivot != null)
        {
            standCamLocalY = camPivot.localPosition.y;
            crouchCamLocalY = standCamLocalY - (standingHeight - crouchingHeight);
        }

        if (playerCamera != null) playerCamera.enabled = false;
        if (audioListener != null) audioListener.enabled = false;

        isGrounded = cc.isGrounded;
        wasGrounded = isGrounded;
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        SetCursorCaptured(true);

        if (playerCamera != null) playerCamera.enabled = true;
        if (audioListener != null) audioListener.enabled = true;

        if (hideLocalBody)
        {
            HideRenderers(modelRoot);
            HideRenderers(headBone);
        }
    }

    void OnDisable()
    {
        if (playerCamera != null) playerCamera.enabled = false;
        if (audioListener != null) audioListener.enabled = false;

        if (isLocalPlayer)
            SetCursorCaptured(false);
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        if (Input.GetKeyDown(KeyCode.Escape))
            SetCursorCaptured(Cursor.lockState != CursorLockMode.Locked);

        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        HandleLook();
        HandleCrouch();
        HandleMovement();
        UpdateAnimation();

        if (modelRoot != null)
            modelRoot.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
    }

    void LateUpdate()
    {
        if (!isLocalPlayer) return;
        if (headBone == null) return;

        float neckPitch = Mathf.Clamp(pitch, -45f, 45f);
        headBone.localRotation = headBaseLocalRotation * Quaternion.Euler(neckPitch, 0f, 0f);
    }

    void SetCursorCaptured(bool captured)
    {
        Cursor.visible = !captured;
        Cursor.lockState = captured ? CursorLockMode.Locked : CursorLockMode.None;
    }

    void HideRenderers(Transform root)
    {
        if (root == null) return;

        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            r.enabled = false;
    }

    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivityX;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivityY;

        transform.Rotate(0f, mouseX, 0f);

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        if (camPivot != null)
            camPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void HandleCrouch()
    {
        if (useToggleCrouch)
        {
            if (Input.GetKeyDown(crouchToggleKey))
            {
                if (isCrouching)
                {
                    if (CanStandUp())
                        isCrouching = false;
                }
                else
                {
                    isCrouching = true;
                }
            }
        }
        else
        {
            if (Input.GetKey(crouchHoldKey))
            {
                isCrouching = true;
            }
            else
            {
                if (CanStandUp())
                    isCrouching = false;
            }
        }

        float targetHeight = isCrouching ? crouchingHeight : standingHeight;
        cc.height = Mathf.Lerp(cc.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);

        Vector3 center = cc.center;
        center.y = cc.height * 0.5f;
        cc.center = center;

        if (camPivot != null)
        {
            Vector3 localPos = camPivot.localPosition;
            float targetCamY = isCrouching ? crouchCamLocalY : standCamLocalY;
            localPos.y = Mathf.Lerp(localPos.y, targetCamY, crouchTransitionSpeed * Time.deltaTime);
            camPivot.localPosition = localPos;
        }
    }

    bool CanStandUp()
    {
        if (cc == null) return true;
        if (crouchingHeight >= standingHeight) return true;

        float radius = Mathf.Max(0.05f, cc.radius - 0.02f);

        Vector3 bottom = transform.position + Vector3.up * radius;
        Vector3 top = transform.position + Vector3.up * (standingHeight - radius);

        bool oldEnabled = cc.enabled;
        cc.enabled = false;
        bool blocked = Physics.CheckCapsule(bottom, top, radius, ~0, QueryTriggerInteraction.Ignore);
        cc.enabled = oldEnabled;

        return !blocked;
    }

    void HandleMovement()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        input = Vector2.ClampMagnitude(input, 1f);

        bool sprintRequested =
            Input.GetKey(KeyCode.LeftShift) &&
            !isCrouching &&
            input.y > 0.55f &&
            Mathf.Abs(input.x) < 0.35f;

        float targetSpeed = isCrouching ? crouchSpeed : (sprintRequested ? sprintSpeed : walkSpeed);

        Vector3 desiredDirection = transform.right * input.x + transform.forward * input.y;
        if (desiredDirection.sqrMagnitude > 1f)
            desiredDirection.Normalize();

        jumpedThisFrame = false;

        if (isGrounded)
        {
            Vector3 targetPlanarVelocity = desiredDirection * targetSpeed;

            float accel = input.sqrMagnitude > 0.001f ? groundAcceleration : groundDeceleration;
            planarVelocity = Vector3.MoveTowards(planarVelocity, targetPlanarVelocity, accel * Time.deltaTime);

            if (verticalVelocity < -groundStickForce)
                verticalVelocity = -groundStickForce;

            if (Input.GetButtonDown("Jump") && !isCrouching)
            {
                verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);

                // Фиксируем горизонтальный импульс на момент прыжка
                airbornePlanarVelocity = planarVelocity;

                jumpedThisFrame = true;
                isGrounded = false;

                if (anim != null)
                    anim.SetTrigger(jumpParam);
            }
        }
        else
        {
            // В ВОЗДУХЕ НЕТ УПРАВЛЕНИЯ
            planarVelocity = airbornePlanarVelocity;
        }

        verticalVelocity -= gravity * Time.deltaTime;

        Vector3 motion = planarVelocity;
        motion.y = verticalVelocity;

        CollisionFlags flags = cc.Move(motion * Time.deltaTime);

        wasGrounded = isGrounded;
        isGrounded = (flags & CollisionFlags.Below) != 0;

        if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
            verticalVelocity = 0f;

        if (isGrounded)
        {
            verticalVelocity = -groundStickForce;

            if (!jumpedThisFrame)
                airbornePlanarVelocity = planarVelocity;
        }
        else
        {
            if (wasGrounded && !jumpedThisFrame)
                airbornePlanarVelocity = planarVelocity;
        }
    }

    void UpdateAnimation()
    {
        if (anim == null) return;

        Vector3 localPlanar = transform.InverseTransformDirection(planarVelocity);

        float normX = Mathf.Clamp(localPlanar.x / walkSpeed, -1f, 1f);

        float forwardDenominator = walkSpeed;
        if (localPlanar.z > walkSpeed + 0.05f)
            forwardDenominator = sprintSpeed;
        else if (isCrouching)
            forwardDenominator = crouchSpeed;

        float normY = Mathf.Clamp(localPlanar.z / Mathf.Max(0.01f, forwardDenominator), -1f, 1f);

        Vector2 flatSpeed = new Vector2(localPlanar.x, localPlanar.z);
        bool moving = flatSpeed.sqrMagnitude > 0.04f;
        bool running = !isCrouching && localPlanar.z > walkSpeed + 0.05f;

        anim.SetFloat(moveXParam, normX, 0.08f, Time.deltaTime);
        anim.SetFloat(moveYParam, normY, 0.08f, Time.deltaTime);
        anim.SetFloat(speedParam, Mathf.Clamp01(flatSpeed.magnitude / sprintSpeed), 0.08f, Time.deltaTime);

        anim.SetBool(isMovingParam, moving);
        anim.SetBool(isRunningParam, running);
        anim.SetBool(isCrouchingParam, isCrouching);
        anim.SetBool(groundedParam, isGrounded);
        anim.SetFloat(verticalSpeedParam, verticalVelocity);
    }
}