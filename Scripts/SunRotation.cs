using UnityEngine;

public class SunRotation : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(new Vector3(0, -Time.deltaTime * 360f / 86400f, 0));
    }
}
