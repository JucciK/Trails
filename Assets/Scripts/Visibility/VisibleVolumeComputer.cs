using UnityEngine;

[RequireComponent(typeof(Camera))]
public class VisibleVolumeComputer : MonoBehaviour
{
    [Header("Compute Shader Settings")]
    public ComputeShader volumeComputeShader;
    public Shader replacementShader;
    public int threadGroupSize = 8;

    [Header("Depth Settings")]
    public int depthResolution = 256;

    public RenderTexture depthTexture;
    private ComputeBuffer volumeBuffer;
    private Camera cam;

    private const float SCALE = 1000000f;
    private uint[] volumeData = new uint[1];
    public Transform debug;

    public float volume;
    void Start()
    {
        cam = GetComponent<Camera>();
        cam.depthTextureMode = DepthTextureMode.None;

        // Create the RFloat RenderTexture for depth
        depthTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.RFloat);
        depthTexture.enableRandomWrite = true;
        depthTexture.Create();
        if (debug != null)
        {
            debug.GetComponent<Renderer>().material.mainTexture = depthTexture;
        }

        // Create the compute buffer for volume result
        volumeBuffer = new ComputeBuffer(1, sizeof(uint));
    }

    void OnDestroy()
    {
        if (depthTexture != null) depthTexture.Release();
        if (volumeBuffer != null) volumeBuffer.Release();
    }

    void LateUpdate()
    {
        // Clear volume buffer
        volumeData[0] = 0;
        volumeBuffer.SetData(volumeData);

        // Render linear depth using replacement shader
        RenderCameraDepth();

        // Dispatch compute shader
        int kernel = volumeComputeShader.FindKernel("CSMain");

        volumeComputeShader.SetTexture(kernel, "_DepthTex", depthTexture);
        volumeComputeShader.SetBuffer(kernel, "_VolumeResult", volumeBuffer);
        volumeComputeShader.SetMatrix("_InvProjMatrix", cam.projectionMatrix.inverse);
        volumeComputeShader.SetMatrix("_InvViewMatrix", cam.cameraToWorldMatrix);
        volumeComputeShader.SetVector("_CameraWorldPos", cam.transform.position);
        volumeComputeShader.SetVector("_TexSize", new Vector2(depthTexture.width, depthTexture.height));
        volumeComputeShader.SetFloat("_CameraFar", cam.farClipPlane);
        volumeComputeShader.SetFloat("_CameraFOV", cam.fieldOfView);

        int tgx = Mathf.CeilToInt((float)depthTexture.width / threadGroupSize);
        int tgy = Mathf.CeilToInt((float)depthTexture.height / threadGroupSize);
        volumeComputeShader.Dispatch(kernel, tgx, tgy, 1);

        // Retrieve result
        volumeBuffer.GetData(volumeData);
        volume = volumeData[0] / SCALE;

        //Debug.Log($"Visible Free Space Volume: {volume:F3} (approx cubic units)");
    }

    void RenderCameraDepth()
    {
        Shader.SetGlobalMatrix("_CameraMatrixV", cam.worldToCameraMatrix);
        Shader.SetGlobalFloat("_CameraFar", cam.farClipPlane);
        cam.targetTexture = depthTexture;
        cam.RenderWithShader(replacementShader, "RenderType");
        cam.targetTexture = null;
    }
}


/*using UnityEngine;

public class VisibleVolumeComputer : MonoBehaviour
{
    public ComputeShader volumeComputeShader;
    public Camera targetCamera;

    private ComputeBuffer volumeBuffer;
    private float[] volumeResult = new float[1];
    public RenderTexture depthTexture, debugOut;
    void Start()
    {
        if (!targetCamera) targetCamera = Camera.main;
        targetCamera.depthTextureMode |= DepthTextureMode.Depth;
        depthTexture = new RenderTexture(targetCamera.pixelWidth, targetCamera.pixelHeight, 16, RenderTextureFormat.Depth);
        //depthTexture.enableRandomWrite = true;  // Allow random writes to the texture
        depthTexture.Create();

        targetCamera.targetTexture = depthTexture;

        // Set the camera's clear flags to Depth (if you need depth clearing)
        targetCamera.clearFlags = CameraClearFlags.Depth;

        debugOut = new RenderTexture(targetCamera.pixelWidth, targetCamera.pixelHeight, 16, RenderTextureFormat.RFloat);
        debugOut.enableRandomWrite = true;
        debugOut.Create();
    }

    void LateUpdate()
    {
        int width = targetCamera.pixelWidth;
        int height = targetCamera.pixelHeight;

        if (volumeBuffer == null)
            volumeBuffer = new ComputeBuffer(1, sizeof(float));

        volumeResult[0] = 0;
        volumeBuffer.SetData(volumeResult);

        int kernel = volumeComputeShader.FindKernel("CSMain");
        Graphics.SetRenderTarget(depthTexture);

        //GL.Clear(true, true, Color.clear);
        volumeComputeShader.SetTexture(kernel, "_CameraDepthTexture", depthTexture);
        volumeComputeShader.SetBuffer(kernel, "_VolumeBuffer", volumeBuffer);
        volumeComputeShader.SetMatrix("_CameraInverseProjection", targetCamera.projectionMatrix.inverse);
        volumeComputeShader.SetMatrix("_CameraToWorld", targetCamera.cameraToWorldMatrix);
        volumeComputeShader.SetFloat("_Fov", targetCamera.fieldOfView);
        volumeComputeShader.SetFloat("_Aspect", targetCamera.aspect);
        //volumeComputeShader.SetTexture(kernel, "_DebugOut", debugOut);

        volumeComputeShader.SetFloats("_TextureSize", width, height);

        int threadX = Mathf.CeilToInt(width / 8f);
        int threadY = Mathf.CeilToInt(height / 8f);
        volumeComputeShader.Dispatch(kernel, threadX, threadY, 1);

        volumeBuffer.GetData(volumeResult);
        Debug.Log($"Visible Volume: {volumeResult[0] / 1000.0f} m³");
    }

    void OnGUI()
    {
        GUI.DrawTexture(new Rect(0, 0, 256, 256), debugOut, ScaleMode.ScaleToFit, false, 1);
    }

    void OnDestroy()
    {
        volumeBuffer?.Release();
        if (depthTexture != null)
            depthTexture.Release();
    }
}*/