using System.Collections;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.tag != "World" && other.transform.parent.parent.tag != "Player")
        {
            print(other.name);
            gameObject.GetComponent<Renderer>().material.color = new Color(255.0f, 0.0f, 0.0f);
            StartCoroutine(Delete());
        }
    }

    private IEnumerator Delete()
    {
        yield return new WaitForSeconds(1.0f);
        Destroy(gameObject);
    }
}
