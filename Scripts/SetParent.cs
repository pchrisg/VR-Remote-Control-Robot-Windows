using UnityEngine;

public class SetParent : MonoBehaviour
{
    [SerializeField] private Transform parent;

    private void Awake()
    {
        gameObject.transform.SetParent(parent);
    }
}