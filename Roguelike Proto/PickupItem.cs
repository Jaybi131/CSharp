using System.Collections;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkIdentity))]
public class PickupItem : NetworkBehaviour
{
    [Header("Hold tuning")]
    public Vector3 heldLocalPosition = new Vector3(0f, -0.1f, 0.4f);
    public Vector3 heldLocalEuler = Vector3.zero;
    public float followSpeed = 20f;
    public float rotateSpeed = 20f;

    [SyncVar(hook = nameof(OnHolderNetIdChanged))]
    public uint HolderNetId; // 0 = свободен

    public bool IsHeld => HolderNetId != 0;

    private Rigidbody rb;
    private Collider[] cols;

    // серверная точка удержания
    private Transform serverHoldPoint;

    // кэш владельца для коллизий
    private Transform lastHolderTransform;

    // клиентская/общая кэш-ссылка на hold point владельца
    private Transform resolvedHoldPoint;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cols = GetComponentsInChildren<Collider>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        ResolveHoldPoint();
        ApplyStateClientLike();
    }

    void OnEnable()
    {
        ResolveHoldPoint();
        ApplyStateClientLike();
    }

    void OnHolderNetIdChanged(uint oldValue, uint newValue)
    {
        ResolveHoldPoint();
        ApplyStateClientLike();
    }

    void ResolveHoldPoint()
    {
        resolvedHoldPoint = null;

        if (HolderNetId == 0)
            return;

        if (NetworkClient.spawned.TryGetValue(HolderNetId, out NetworkIdentity holderIdentity))
        {
            var pickup = holderIdentity.GetComponent<PlayerPickup>();
            if (pickup != null && pickup.holdPoint != null)
            {
                resolvedHoldPoint = pickup.holdPoint;
                return;
            }

            resolvedHoldPoint = holderIdentity.transform;
        }
    }

    void ApplyStateClientLike()
    {
        bool held = IsHeld;

        if (rb != null)
        {
            rb.isKinematic = held;
            if (held)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        if (cols != null)
        {
            foreach (var c in cols)
            {
                if (c != null)
                    c.enabled = !held;
            }
        }
    }

    void LateUpdate()
    {
        Transform target = serverHoldPoint != null ? serverHoldPoint : resolvedHoldPoint;
        if (!IsHeld || target == null)
            return;

        Vector3 targetPos = target.TransformPoint(heldLocalPosition);
        Quaternion targetRot = target.rotation * Quaternion.Euler(heldLocalEuler);

        transform.SetPositionAndRotation(targetPos, targetRot);
    }

    [Server]
    public bool ServerPickup(NetworkIdentity newHolder, Transform holdPoint)
    {
        if (IsHeld || newHolder == null || holdPoint == null)
            return false;

        HolderNetId = newHolder.netId;
        lastHolderTransform = newHolder.transform;
        serverHoldPoint = holdPoint;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (cols != null)
        {
            foreach (var c in cols)
            {
                if (c != null)
                    c.enabled = false;
            }
        }

        transform.SetPositionAndRotation(
            holdPoint.TransformPoint(heldLocalPosition),
            holdPoint.rotation * Quaternion.Euler(heldLocalEuler)
        );

        return true;
    }

    [Server]
    public void ServerDrop(Vector3 dir, float force)
    {
        Transform previousHolder = lastHolderTransform;

        HolderNetId = 0;
        serverHoldPoint = null;
        resolvedHoldPoint = null;
        lastHolderTransform = null;

        if (cols != null)
        {
            foreach (var c in cols)
            {
                if (c != null)
                    c.enabled = true;
            }
        }

        if (rb != null)
        {
            transform.position += dir.normalized * 0.1f;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;
            rb.AddForce(dir.normalized * Mathf.Max(0f, force), ForceMode.VelocityChange);
            rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.VelocityChange);
        }

        if (previousHolder != null)
            StartCoroutine(IgnoreOwnerCollisions(previousHolder, 0.5f));
    }

    IEnumerator IgnoreOwnerCollisions(Transform holder, float time)
    {
        if (holder == null || cols == null)
            yield break;

        var holderCols = holder.GetComponentsInChildren<Collider>();

        foreach (var hc in holderCols)
        {
            foreach (var ic in cols)
            {
                if (hc != null && ic != null)
                    Physics.IgnoreCollision(hc, ic, true);
            }
        }

        yield return new WaitForSeconds(time);

        foreach (var hc in holderCols)
        {
            foreach (var ic in cols)
            {
                if (hc != null && ic != null)
                    Physics.IgnoreCollision(hc, ic, false);
            }
        }
    }
}