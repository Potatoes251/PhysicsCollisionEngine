using TMPro;
using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
public struct AABB
{
    public Vector3 center;
    public Vector3 extents;

    public readonly Vector3 size => extents * 2;

    public readonly Vector3 min => center - extents;
    public readonly Vector3 max => center + extents;

    public readonly bool Contains(Vector3 point)
    {
        Vector3 min = this.min;
        Vector3 max = this.max;

        return  min.x <= point.x && max.x >= point.x &&
                min.y <= point.y && max.y >= point.y &&
                min.z <= point.z && max.z >= point.z;
    }

    public readonly bool Contains(AABB bounds)
    {
        return Contains(bounds.min) && Contains(bounds.max);
    }

    public readonly bool Collide(AABB bounds)
    {
        Vector3 min1 = min;
        Vector3 max1 = max;
        Vector3 min2 = bounds.min;
        Vector3 max2 = bounds.max;

        return !(min1.x > max2.x || min1.y > max2.y || min1.z > max2.z ||
                min2.x > max1.x || min2.y > max1.y || min2.z > max1.z);
    }

    public void Expand(float margin)
    {
        extents += Vector3.one * (margin * .5f);
    }

    public void SetMinMax(Vector3 min, Vector3 max)
    {
        extents = (max - min) * 0.5f;
        center = min + extents;
    }

    public void Encapsulate(Vector3 point)
    {
        Vector3 lhs = min;
        Vector3 vector = Vector3.Min(lhs, point);
        lhs = max;
        Vector3 vector2 = Vector3.Max(lhs, point);
        SetMinMax(vector, vector2);
    }

    public void Encapsulate(AABB bounds)
    {
        Encapsulate(bounds.min);
        Encapsulate(bounds.max);
    }

    public readonly float Area()
    {
        Vector3 size = this.size;
        return (size.x * size.y + size.y * size.z + size.z * size.x);
    }
}
