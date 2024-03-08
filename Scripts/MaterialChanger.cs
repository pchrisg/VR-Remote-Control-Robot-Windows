/**
 * Added to every interactable object in CollisionHandling Script
 */

using UnityEngine;

public class MaterialChanger : MonoBehaviour
{
    Material m_OriginalMat = null;

    private void Awake()
    {
        m_OriginalMat = gameObject.GetComponent<Renderer>().material;
    }

    public void ChangeMat(Material mat = null)
    {
        if (mat == null)
            gameObject.GetComponent<Renderer>().material = m_OriginalMat;

        else
            gameObject.GetComponent<Renderer>().material = mat;
    }
}