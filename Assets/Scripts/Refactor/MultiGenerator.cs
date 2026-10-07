using UnityEngine;

public class MultiGenerator : MonoBehaviour
{

    public Rect menuRect = new Rect(300, 0, 500, 300);
    public rMap map;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnGUI()
    {
            GUI.Window(0, menuRect, menuFunction, "Menu");
        
    }

    public void menuFunction(int id)
    {
        if(GUI.Button(new Rect(10,10,480,30), "Start generating"))
        {
            map.startMany();
        }
        GUI.Label(new Rect(10, 80, 480, 30), map.currentMessage);
    }
}
