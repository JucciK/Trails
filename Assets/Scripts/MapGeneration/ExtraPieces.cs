using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExtraPieces : MonoBehaviour
{

    public ExtraObject[] extras;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void applyExtras()
    {
        ExtraObject e = getRandomObject(extras);
        e.obj.SetActive(true);
        foreach(ExtraObject obj in extras)
        {
            if(e!=obj)
                obj.obj.SetActive(false);
        }
    }
    [System.Serializable]
    public class ExtraObject
    {
        public string Name;
        public int weight;
        public GameObject obj;
    }

    public ExtraObject getRandomObject(ExtraObject[] extra)
    {
        int total = 0;
        foreach(ExtraObject e in extra)
        {
            total += e.weight;
        }

        float r = Random.Range(0.0f, total);
        foreach(ExtraObject e in extra)
        {
            if (r < e.weight)
                return e;
            r -= e.weight;
        }
        return extra[extra.Length - 1];
    }
}
