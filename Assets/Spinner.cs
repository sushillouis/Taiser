using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spinner : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public Vector3 EulerAngle = Vector3.zero;
    // Update is called once per frame
    void Update()
    {
        transform.localEulerAngles = EulerAngle;
        EulerAngle.z -= 50 * Time.deltaTime;
    }
}
