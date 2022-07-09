using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class Utilities
{
    static public IEnumerable<IEnumerable<T>>
    GetPermutations<T>(IEnumerable<T> list, int length)
    {
        if (length == 1) return list.Select(t => new T[] { t });
        return GetPermutations(list, length - 1)
            .SelectMany(t => list.Where(o => !t.Contains(o)),
                (t1, t2) => t1.Concat(new T[] { t2 }));
    }

    static public IEnumerable<IEnumerable<T>>
    GetKCombs<T>(IEnumerable<T> list, int length) where T : IComparable
    {
        if (length == 1) return list.Select(t => new T[] { t });
        return GetKCombs(list, length - 1)
            .SelectMany(t => list.Where(o => o.CompareTo(t.Last()) > 0),
                (t1, t2) => t1.Concat(new T[] { t2 }));
    }

    static private bool isOverLapping1D(float xmin, float xmax, float ymin, float ymax)
    {
        return !(xmax < ymin || ymax < xmin);
    }

    static public bool isOverLapping3D(Vector3 c1min, Vector3 c1max, Vector3 c2min, Vector3 c2max)
    {
        return isOverLapping1D(c1min.x, c1max.x, c2min.x, c2max.x) &&
            isOverLapping1D(c1min.y, c1max.y, c2min.y, c2max.y) &&
            isOverLapping1D(c1min.z, c1max.z, c2min.z, c2max.z);
    }


    static public float RayCastAABB(Vector3 origin, Vector3 direction, Vector3 aabbMin, Vector3 aabbMax)
    {
        direction = direction.normalized;
        float t1 = (aabbMin.x - origin.x) / direction.x;
        float t2 = (aabbMax.x - origin.x) / direction.x;
        float t3 = (aabbMin.y - origin.y) / direction.y;
        float t4 = (aabbMax.y - origin.y) / direction.y;

        float tmin = Mathf.Max(Mathf.Min(t1, t2), Mathf.Min(t3, t4));
        float tmax = Mathf.Min(Mathf.Max(t1, t2), Mathf.Max(t3, t4));

        if (tmax < 0)
            return -1;

        if (tmin > tmax)
            return -1;

        if (tmin < 0f)
            return tmax;

        return tmin;
    }


}
