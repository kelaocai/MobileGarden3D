using UnityEngine;

public static class KnightEquipment
{
    public static void AttachSceneSword(Animator knightAnimator)
    {
        if (knightAnimator == null)
        {
            return;
        }

        Transform handSlot = null;
        foreach (Transform child in knightAnimator.GetComponentsInChildren<Transform>(true))
        {
            if (child.name.Equals("handslot.r", System.StringComparison.OrdinalIgnoreCase))
            {
                handSlot = child;
                break;
            }
        }

        if (handSlot == null || handSlot.Find("sword_2handed") != null)
        {
            return;
        }

        Transform closestSword = null;
        float closestDistance = float.PositiveInfinity;
        Transform[] sceneTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude);
        foreach (Transform sceneTransform in sceneTransforms)
        {
            if (!sceneTransform.name.Equals("sword_2handed", System.StringComparison.OrdinalIgnoreCase) ||
                sceneTransform.IsChildOf(knightAnimator.transform))
            {
                continue;
            }

            float distance = (sceneTransform.position - knightAnimator.transform.position).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestSword = sceneTransform;
            }
        }

        if (closestSword == null)
        {
            return;
        }

        closestSword.SetParent(handSlot, false);
        closestSword.localPosition = Vector3.zero;
        closestSword.localRotation = Quaternion.identity;
        closestSword.localScale = Vector3.one;

        foreach (Collider swordCollider in closestSword.GetComponentsInChildren<Collider>())
        {
            swordCollider.enabled = false;
        }
    }
}
