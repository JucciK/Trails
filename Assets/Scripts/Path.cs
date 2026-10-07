using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Path : MonoBehaviour
{
    // Start is called before the first frame update
    public List<float> segmentLengths = new List<float>();

    public List<Vector3> points = new List<Vector3>();
    public float controlDistance = 0.1f;
    public float mControlDistance = 0.1f;
    public float extraLength;

    public float[] Yaws;
    public AnimationCurve yawCurve;
    public float maxYaw = 25;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

}
