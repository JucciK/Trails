using UnityEngine;

public class RaycastVisibility : MonoBehaviour
{
    public static RaycastVisibility Instance;
    private void Awake()
    {
        Instance = this;
    }
    public float startAngle, endAngle;

    public int measurements = 100;
    public bool rotateWithCamera = true;
    float[] distances;
    public bool debug = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        /*distances = new float[measurements];
        float ang = rotateWithCamera ? (endAngle - startAngle) / (measurements - 1) : 360/measurements;
        float sin = Mathf.Sin(ang*Mathf.Deg2Rad);
        float area = 0;
        for (int i=0; i<measurements; i++) 
        {
            Vector3 direction = Vector3.forward;
            if (!rotateWithCamera)
            {
                float angle = i * 360 / measurements;
                direction = Quaternion.Euler(0, angle, 0) * direction;
            }
            else
            {
                float angle = startAngle + i*(endAngle-startAngle)/(measurements-1);
                direction = Quaternion.AngleAxis(angle, transform.up) * transform.forward;

            }

            Vector3 position = transform.position;

            Ray ray = new Ray(position, direction);

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100f))
            {
                distances[i] = hit.distance;
                if (debug)
                    Debug.DrawRay(position, direction*hit.distance);
            }
            else
                distances[i] = 100f;

            if(i>0)
            {
                area += 0.5f * distances[i - 1] * distances[i] * sin;
            }

        }

        if (!rotateWithCamera)
            area += 0.5f * distances[0] * distances[measurements - 1] * sin;

        Debug.Log("Area: " + area);
        */
    }

    public float getVisibility(Vector3 position)
    {
        distances = new float[measurements];
        float ang = rotateWithCamera ? (endAngle - startAngle) / (measurements - 1) : 360 / measurements;
        float sin = Mathf.Sin(ang * Mathf.Deg2Rad);
        float area = 0;
        for (int i = 0; i < measurements; i++)
        {
            Vector3 direction = Vector3.forward;
            if (!rotateWithCamera)
            {
                float angle = i * 360 / measurements;
                direction = Quaternion.Euler(0, angle, 0) * direction;
            }
            else
            {
                float angle = startAngle + i * (endAngle - startAngle) / (measurements - 1);
                direction = Quaternion.AngleAxis(angle, transform.up) * transform.forward;

            }

            

            Ray ray = new Ray(position, direction);

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100f))
            {
                distances[i] = hit.distance;
                if (debug)
                    Debug.DrawRay(position, direction * hit.distance);
            }
            else
                distances[i] = 100f;

            if (i > 0)
            {
                area += 0.5f * distances[i - 1] * distances[i] * sin;
            }

        }

        if (!rotateWithCamera)
            area += 0.5f * distances[0] * distances[measurements - 1] * sin;

        //Debug.Log("Area: " + area);
        return area;
    }
}
