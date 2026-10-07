using UnityEditor;
using UnityEditor.Search;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class TrackPiecePicker : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;
    private IntegerField selectedTrack;
    private bool trackHasBeenOpened = false;
    private bool updateScreen = false;
    private FollowPath followPath;
    private Slider startSlider, lengthSlider;

    private FloatField startFloat, lengthFloat;

    public static Gradient gradient;

    [MenuItem("Window/UI Toolkit/TrackPiecePicker")]
    public static void ShowExample()
    {
        TrackPiecePicker wnd = GetWindow<TrackPiecePicker>();
        wnd.titleContent = new GUIContent("TrackPiecePicker");
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        var targetField = new UnityEditor.UIElements.ObjectField("Target")
        {
            objectType = typeof(FollowPath)
        };

        targetField.value = FindFirstObjectByType<FollowPath>();
        followPath = targetField.value as FollowPath;

        targetField.RegisterValueChangedCallback(target => { followPath = target.newValue as FollowPath; });

        // VisualElements objects can contain other VisualElement following a tree hierarchy.

        // Instantiate UXML
        VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
        root.Add(labelFromUXML);


        var a = new GradientField("SpeedGradient");
        a.RegisterValueChangedCallback(target => {  gradient = target.newValue as Gradient; SceneView.RepaintAll(); });
        rootVisualElement.Add(a);
        rootVisualElement.Add(targetField);

        SetupHandler();
    }

    private void SetupHandler()
    {
        VisualElement root = rootVisualElement;
        selectedTrack = root.Query<IntegerField>(name = "SelectedTrack");
        lengthSlider = root.Query<Slider>(name = "LengthSlider");
        startSlider = root.Query<Slider>(name = "StartPoint");
        startFloat = root.Query<FloatField>(name = "StartFloat");
        lengthFloat = root.Query<FloatField>(name = "LengthFloat");
        Button button = root.Query<Button>(name = "OpenTrack");
        Button clip = root.Query<Button>(name = "ClipPath");
        clip.RegisterCallback<ClickEvent>(onClick =>
        {
            if (!trackHasBeenOpened)
                return;

            followPath.pathPieces.Add(new PathPiece(startSlider.value, startSlider.value + lengthSlider.value, selectedTrack.value));
        });

        button.RegisterCallback<ClickEvent>(openTrack);
        trackHasBeenOpened = false;

        startSlider.RegisterValueChangedCallback(startSliderHandler);
        lengthSlider.RegisterValueChangedCallback(lengthSliderHandler);
        startFloat.RegisterValueChangedCallback(startSliderHandler);
        lengthFloat.RegisterValueChangedCallback(lengthSliderHandler);
        
    }

    private void openTrack(ClickEvent evt)
    {
        int selected = selectedTrack.value;
        followPath.openPath(selected);
        SceneView.RepaintAll();
        trackHasBeenOpened = true;
    }

    private void startSliderHandler(ChangeEvent<float> evt)
    {
        startSlider.value = evt.newValue;
        startFloat.value = evt.newValue;
        if (trackHasBeenOpened) {

            robotPose start = followPath.getPose(evt.newValue);
            robotPose end = followPath.getPose(evt.newValue + lengthSlider.value);

            MoveCamera((start.pos + end.pos) / 2, Quaternion.LookRotation(Vector3.down));
        }
        SceneView.RepaintAll();
    }

    private void lengthSliderHandler(ChangeEvent<float> evt)
    {
        lengthFloat.value = evt.newValue;
        lengthSlider.value = evt.newValue;
        startSlider.highValue = 900 - evt.newValue;
        SceneView.RepaintAll();
    }

    

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;

    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }



    void OnSceneGUI(SceneView sceneView)
    {

        if (!trackHasBeenOpened)
            return;
        float start = startSlider.value;
        float length = lengthSlider.value;
        robotPose prev = followPath.getPose(start);
        for (float f = start+0.25f; f<start+length; f+=0.25f)
        {
            robotPose next = followPath.getPose(f);
            float speed = ((prev.linearVelocity + next.linearVelocity)/4);
            if (gradient == null)
                gradient = new Gradient();
            Color color = gradient.Evaluate(speed);
;           Handles.color = color;
            Handles.DrawLine(prev.pos, next.pos);
            prev = next;
        }

    }

    public void MoveCamera(Vector3 position, Quaternion rotation, float size = 10)
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null) return;

        sceneView.pivot = position;
        sceneView.rotation = rotation;
        sceneView.size = size;
        sceneView.Repaint();
    }

}
