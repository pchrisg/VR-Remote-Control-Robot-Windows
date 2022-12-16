using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionObjectCreator : MonoBehaviour
{
    [SerializeField] private ROSPublisher m_ROSPublisher;

    // Start is called before the first frame update
    void Start()
    {
        m_ROSPublisher.PublishCollisionObject(this.gameObject);
    }
}
