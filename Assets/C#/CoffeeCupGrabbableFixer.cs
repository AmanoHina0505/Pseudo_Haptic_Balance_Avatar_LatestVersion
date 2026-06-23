using Oculus.Interaction;
using Oculus.Interaction.Grab;
using Oculus.Interaction.GrabAPI;
using Oculus.Interaction.HandGrab;
using UnityEngine;

public class CoffeeCupGrabbableFixer : MonoBehaviour
{
    private const string CoffeeCupName = "CoffeeCup";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void FixCoffeeCupAfterSceneLoad()
    {
        GameObject cup = GameObject.Find(CoffeeCupName);
        if (cup == null)
        {
            return;
        }

        MakeGrabbable(cup);
    }

    private static void MakeGrabbable(GameObject cup)
    {
        Rigidbody rigidbody = cup.GetComponent<Rigidbody>();
        if (rigidbody == null)
        {
            rigidbody = cup.AddComponent<Rigidbody>();
        }

        rigidbody.mass = 0.25f;
        rigidbody.useGravity = false;
        rigidbody.isKinematic = true;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        Collider collider = GetOrCreateSolidCollider(cup);
        NormalizeColliders(cup);

        Grabbable grabbable = cup.GetComponent<Grabbable>();
        if (grabbable == null)
        {
            grabbable = cup.AddComponent<Grabbable>();
        }

        grabbable.InjectOptionalRigidbody(rigidbody);
        grabbable.InjectOptionalTargetTransform(cup.transform);
        grabbable.InjectOptionalKinematicWhileSelected(true);
        grabbable.InjectOptionalThrowWhenUnselected(true);

        GrabInteractable grabInteractable = cup.GetComponent<GrabInteractable>();
        if (grabInteractable == null)
        {
            grabInteractable = cup.AddComponent<GrabInteractable>();
        }

        grabInteractable.InjectRigidbody(rigidbody);
        grabInteractable.InjectOptionalPointableElement(grabbable);
        grabInteractable.UseClosestPointAsGrabSource = true;
        grabInteractable.ReleaseDistance = 0.35f;

        HandGrabInteractable handGrabInteractable = cup.GetComponent<HandGrabInteractable>();
        if (handGrabInteractable == null)
        {
            handGrabInteractable = cup.AddComponent<HandGrabInteractable>();
        }

        handGrabInteractable.InjectAllHandGrabInteractable(
            GrabTypeFlags.All,
            rigidbody,
            GrabbingRule.DefaultPinchRule,
            GrabbingRule.DefaultPalmRule);
        handGrabInteractable.InjectOptionalPointableElement(grabbable);
        grabInteractable.enabled = true;
        handGrabInteractable.enabled = true;

        if (cup.GetComponent<AvatarHandCoffeeGrab>() == null)
        {
            cup.AddComponent<AvatarHandCoffeeGrab>();
        }

        Debug.Log($"CoffeeCup grab setup ready with {collider.GetType().Name}, GrabInteractable, HandGrabInteractable, and avatar fallback.");
    }

    private static Collider GetOrCreateSolidCollider(GameObject cup)
    {
        MeshCollider meshCollider = cup.GetComponent<MeshCollider>();
        if (meshCollider != null)
        {
            meshCollider.enabled = true;
            meshCollider.isTrigger = false;
            meshCollider.convex = true;
            if (meshCollider.sharedMesh != null)
            {
                return meshCollider;
            }
        }

        BoxCollider boxCollider = cup.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = cup.AddComponent<BoxCollider>();
        }

        Bounds bounds = CalculateRendererBounds(cup);
        boxCollider.center = cup.transform.InverseTransformPoint(bounds.center);
        boxCollider.size = new Vector3(
            Mathf.Max(0.05f, bounds.size.x / Mathf.Max(0.0001f, cup.transform.lossyScale.x)),
            Mathf.Max(0.05f, bounds.size.y / Mathf.Max(0.0001f, cup.transform.lossyScale.y)),
            Mathf.Max(0.05f, bounds.size.z / Mathf.Max(0.0001f, cup.transform.lossyScale.z)));
        boxCollider.isTrigger = false;
        return boxCollider;
    }

    private static void NormalizeColliders(GameObject cup)
    {
        Collider[] colliders = cup.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = true;
            collider.isTrigger = false;

            if (collider is MeshCollider meshCollider)
            {
                meshCollider.convex = true;
            }
        }
    }

    private static Bounds CalculateRendererBounds(GameObject cup)
    {
        Renderer[] renderers = cup.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Bounds(cup.transform.position, Vector3.one * 0.1f);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }
}
