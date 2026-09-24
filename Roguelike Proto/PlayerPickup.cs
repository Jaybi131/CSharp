using Mirror;
using UnityEngine;

public class PlayerPickup : NetworkBehaviour
{
    [Header("Aim assist")]
    public float aimRadius = 0.18f;
    public float searchConeAngle = 16f;
    public float nearbySearchRadius = 0.9f;

    [Header("Refs")]
    public Transform cam;
    public Transform holdPoint;

    [Header("Params")]
    public LayerMask pickupMask;
    public LayerMask playerMask;
    public float maxPickupDistance = 3.0f;
    public float throwForce = 8f;
    public float nameDetectDistance = 12f;

    [Header("Local UI")]
    public LocalLookHUD lookHud;

    PickupItem heldItem;

    void Update()
    {
        if (!isLocalPlayer) return;

        RefreshHeldReferenceLocal();
        UpdateLocalLookHud();

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (heldItem == null)
                TryPickupClient();
            else
                TryDropOrThrowClient();
        }
    }

    void RefreshHeldReferenceLocal()
    {
        if (heldItem != null)
        {
            if (heldItem.HolderNetId == netId)
                return;
            heldItem = null;
        }

        var all = GameObject.FindObjectsByType<PickupItem>(FindObjectsSortMode.None);
        foreach (var pi in all)
        {
            if (pi != null && pi.HolderNetId == netId)
            {
                heldItem = pi;
                return;
            }
        }
    }

    void UpdateLocalLookHud()
    {
        if (lookHud == null)
            lookHud = GetComponentInChildren<LocalLookHUD>(true);
        if (lookHud == null)
            return;

        if (cam == null) cam = Camera.main ? Camera.main.transform : null;
        if (cam == null)
        {
            lookHud.HideAll();
            return;
        }

        if (TryFindPickupPrompt(out _))
        {
            lookHud.ShowPickupPrompt();
        }
        else
        {
            lookHud.HidePickupPrompt();
        }

        if (TryFindPlayerLookTarget(out PlayerNameTag targetTag))
        {
            lookHud.ShowPlayerName(targetTag.GetDisplayName(), targetTag.nameAnchor != null ? targetTag.nameAnchor.position : targetTag.transform.position + Vector3.up * 2f);
        }
        else
        {
            lookHud.HidePlayerName();
        }
    }

    bool TryFindPickupPrompt(out PickupItem item)
    {
        item = null;
        if (cam == null) return false;

        if (Physics.SphereCast(cam.position, aimRadius, cam.forward, out RaycastHit hit, maxPickupDistance, pickupMask, QueryTriggerInteraction.Ignore))
        {
            var pi = hit.collider.GetComponentInParent<PickupItem>();
            if (pi != null && !pi.IsHeld)
            {
                item = pi;
                return true;
            }
        }

        return FindCandidate(out item);
    }

    bool TryFindPlayerLookTarget(out PlayerNameTag tag)
    {
        tag = null;
        if (cam == null) return false;

        if (!Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, nameDetectDistance, playerMask, QueryTriggerInteraction.Ignore))
            return false;

        tag = hit.collider.GetComponentInParent<PlayerNameTag>();
        if (tag == null) return false;
        if (tag.netIdentity != null && tag.netIdentity.netId == netId) return false;
        return true;
    }

    void TryPickupClient()
    {
        if (cam == null) cam = Camera.main ? Camera.main.transform : null;
        if (cam == null) return;

        if (FindCandidate(out var item))
            CmdTryPickup(item.netIdentity);
    }

    bool FindCandidate(out PickupItem item)
    {
        if (Physics.SphereCast(cam.position, aimRadius, cam.forward, out RaycastHit hit, maxPickupDistance, pickupMask, QueryTriggerInteraction.Ignore))
        {
            item = hit.collider.GetComponentInParent<PickupItem>();
            if (item != null && !item.IsHeld) return true;
        }

        Vector3 probeCenter = cam.position + cam.forward * Mathf.Min(maxPickupDistance, 1.2f);
        var overlaps = Physics.OverlapSphere(probeCenter, nearbySearchRadius, pickupMask, QueryTriggerInteraction.Ignore);

        float bestScore = float.MaxValue;
        PickupItem best = null;

        foreach (var col in overlaps)
        {
            var pi = col.GetComponentInParent<PickupItem>();
            if (pi == null || pi.IsHeld) continue;

            Vector3 toItem = pi.transform.position - cam.position;
            float dist = toItem.magnitude;
            if (dist > maxPickupDistance) continue;

            float ang = Vector3.Angle(cam.forward, toItem);
            if (ang > searchConeAngle) continue;

            float score = ang * 0.6f + dist * 0.4f;
            if (score < bestScore)
            {
                bestScore = score;
                best = pi;
            }
        }

        item = best;
        return item != null;
    }

    void TryDropOrThrowClient()
    {
        Vector3 dir = cam ? cam.forward : transform.forward;
        var id = heldItem ? heldItem.netIdentity : null;
        if (id != null)
            CmdDropOrThrow(id, dir, throwForce);
    }

    [Command]
    void CmdTryPickup(NetworkIdentity itemIdentity)
    {
        if (itemIdentity == null) return;
        var item = itemIdentity.GetComponent<PickupItem>();
        if (item == null) return;

        float d = Vector3.Distance(transform.position, item.transform.position);
        if (d > maxPickupDistance + 0.5f) return;

        if (!item.IsHeld)
        {
            var hp = holdPoint != null ? holdPoint : transform;
            bool ok = item.ServerPickup(netIdentity, hp);
            if (ok)
                TargetOnPickup(connectionToClient);
        }
    }

    [Command]
    void CmdDropOrThrow(NetworkIdentity itemIdentity, Vector3 dir, float force)
    {
        if (itemIdentity == null) return;
        var item = itemIdentity.GetComponent<PickupItem>();
        if (item == null) return;
        if (item.HolderNetId != netId) return;

        item.ServerDrop(dir, force);
        TargetOnDrop(connectionToClient);
    }

    [TargetRpc]
    void TargetOnPickup(NetworkConnectionToClient conn)
    {
        RefreshHeldReferenceLocal();
    }

    [TargetRpc]
    void TargetOnDrop(NetworkConnectionToClient conn)
    {
        heldItem = null;
    }
}
