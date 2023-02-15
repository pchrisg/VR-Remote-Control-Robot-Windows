using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Rails;


public class Rails : MonoBehaviour
{
    public struct Rail
    {
        public Vector3 start;
        public Vector3 end;
    };
    public Rail []rails;

    private void Awake()
    {
        Array.Resize(ref rails, 0);
    }

    private void OnDisable()
    {
        foreach (Transform child in gameObject.GetComponentInChildren<Transform>())
        {
            if (child.GetComponent<CapsuleCollider>() != null)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void OnDestroy()
    {
        RemoveAllRails();
    }

    public Transform GetLastChild()
    {
        int count = gameObject.transform.childCount;
        if (count == 1)
            return gameObject.transform;
        else
            return gameObject.transform.GetChild(count - 1);
    }

    public void AddRail(GameObject rail)
    {
        Array.Resize(ref rails, rails.Length + 1);

        Vector3 offset = rail.transform.up * rail.transform.localScale.y;
        rails[^1].start = rail.transform.position - offset;
        rails[^1].end = rail.transform.position + offset;
    }

    public void RemoveLastRail()
    {
        if(rails.Length > 0)
            Array.Resize(ref rails, rails.Length - 1);
    }

    public void RemoveAllRails()
    {
        while (rails.Length > 0)
            RemoveLastRail();
    }
}
