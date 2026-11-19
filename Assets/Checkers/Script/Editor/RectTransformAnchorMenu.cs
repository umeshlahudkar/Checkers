using UnityEditor;
using UnityEngine;

public static class RectTransformAnchorMenu
{
    [MenuItem("Checkers/RectTransform/Set Anchors To Rect Corners %#`")]
    private static void SetAnchorsToRectCorners()
    {
        foreach (Transform transform in Selection.transforms)
        {
            RectTransform rectTransform = transform as RectTransform;
            RectTransform parent = rectTransform != null ? rectTransform.parent as RectTransform : null;

            if (rectTransform == null || parent == null)
            {
                continue;
            }

            Undo.RecordObject(rectTransform, "Set Anchors To Rect Corners");

            Rect parentRect = parent.rect;

            Vector2 anchorMin = new(
                rectTransform.anchorMin.x + rectTransform.offsetMin.x / parentRect.width,
                rectTransform.anchorMin.y + rectTransform.offsetMin.y / parentRect.height);

            Vector2 anchorMax = new(
                rectTransform.anchorMax.x + rectTransform.offsetMax.x / parentRect.width,
                rectTransform.anchorMax.y + rectTransform.offsetMax.y / parentRect.height);

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }

    [MenuItem("Checkers/RectTransform/Set Anchors To Rect Corners %#`", true)]
    private static bool ValidateSetAnchorsToRectCorners()
    {
        Transform activeTransform = Selection.activeTransform;
        return activeTransform != null && activeTransform as RectTransform != null && activeTransform.parent as RectTransform != null;
    }
}
