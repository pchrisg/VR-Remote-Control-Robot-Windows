using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rails : MonoBehaviour
{
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

    public void Show(bool value)
    {
        gameObject.SetActive(value);
    }

    public Transform GetLastChild()
    {
        int count = gameObject.transform.childCount;
        print("Count " + count);
        if (count == 1)
            return gameObject.transform;
        else
            return gameObject.transform.GetChild(count - 1);
    }
}
