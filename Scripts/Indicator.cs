using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Indicator : MonoBehaviour
{
    public void Show(bool value)
    {
        gameObject.SetActive(value);
    }

    public void SetColour(Color color)
    {
        gameObject.GetComponent<Renderer>().material.color = color;
    }
}
