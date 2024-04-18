using UnityEngine;

public class Indicator : MonoBehaviour
{



    public void Show(bool value)
    {
        gameObject.SetActive(value);
    }

    public void ChangeAppearance(int colorNumber)
    {
        switch (colorNumber)
        {
            case 1:
                SetColour(new(0.2f, 0.2f, 0.2f, 0.0f));
                break;
            case 2:
                SetColour(new(0.0f, 1.0f, 0.0f, 0.5f));
                break;
            case 3:
                SetColour(new(1.0f, 1.0f, 0.0f, 0.5f));
                break;

                
            default:
                break;
        }


    }

    private void SetColour(Color color)
    {
        gameObject.GetComponent<Renderer>().material.color = color;
    }
}