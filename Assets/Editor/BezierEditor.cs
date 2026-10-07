using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class BezierEditor : EditorWindow
{
    public enum ShortType { STRAIGHT_CORRIDOR, TURN_CORRIDOR,STRAIGHT_OPEN, TURN_OPEN, CORRIDOR_TO_OPEN};
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;


    public Vector3 center = Vector3.zero;

    [MenuItem("Window/UI Toolkit/BezierEditor")]
    public static void ShowExample()
    {
        BezierEditor wnd = GetWindow<BezierEditor>();
        wnd.titleContent = new GUIContent("BezierEditor");
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        

        // Instantiate UXML
        VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
        root.Add(labelFromUXML);
    }
}
