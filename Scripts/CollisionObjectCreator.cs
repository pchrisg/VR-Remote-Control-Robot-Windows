using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionObjectCreator : MonoBehaviour
{
    [SerializeField] private ManipulatorPublisher m_ManipulationPublisher;

    // Start is called before the first frame update
    void Start()
    {
        m_ManipulationPublisher.PublishCollisionObject(this.gameObject);
    }
}
