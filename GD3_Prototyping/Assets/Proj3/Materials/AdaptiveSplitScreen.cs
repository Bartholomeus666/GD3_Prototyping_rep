using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// URP-compatible adaptive split screen.
/// Replaces EasySplitScreen — drop this on any GameObject in your scene.
///
/// Setup:
///   1. Assign player1 and player2 transforms.
///   2. Disable (or delete) your original Main Camera — this script creates its own cameras.
///   3. Tune camHeight / camBackDistance / camOffset to match your game's camera angle.
/// </summary>
public class AdaptiveSplitScreen : MonoBehaviour
{
    [Header("Players")]
    public Transform player1;
    public Transform player2;

    [Header("Camera Follow")]
    [SerializeField] private float camHeight       = 6f;
    [SerializeField] private float camBackDistance = 5f;
    [SerializeField] private Vector3 camOffset     = Vector3.zero;
    [SerializeField] private float lerpSpeed       = 5f;
    [SerializeField] private float fov             = 60f;

    [Header("Split Line")]
    [SerializeField] private Color lineColor                  = Color.white;
    [SerializeField, Range(0.001f, 0.05f)] private float lineWidth = 0.008f;

    // ---- private state ----
    private Camera       _cam1;     // renders player 1's view  → _rt1
    private Camera       _cam2;     // renders player 2's view  → _rt2
    private Camera       _refCam;   // sits at midpoint; never renders, used only for WorldToViewportPoint
    private RenderTexture _rt1;
    private RenderTexture _rt2;
    private Material     _splitMat;

    // -----------------------------------------------------------------------

    private void Start()
    {
        // Disable the existing main camera so it doesn't overdraw our canvas.
        Camera existingMain = Camera.main;
        if (existingMain != null)
            existingMain.enabled = false;

        CreateRenderTextures();
        CreateCameras(existingMain);
        CreateFullscreenCanvas();
    }

    // -----------------------------------------------------------------------
    //  Setup
    // -----------------------------------------------------------------------

    private void CreateRenderTextures()
    {
        _rt1 = new RenderTexture(Screen.width, Screen.height, 24);
        _rt2 = new RenderTexture(Screen.width, Screen.height, 24);
    }

    private void CreateCameras(Camera source)
    {
        _cam1   = BuildCamera("SplitCam_P1",  _rt1,  source);
        _cam2   = BuildCamera("SplitCam_P2",  _rt2,  source);

        // Reference camera — disabled so it never renders, only used for projection math.
        GameObject refGo = new GameObject("SplitCam_Ref");
        _refCam = refGo.AddComponent<Camera>();
        CopyCameraSettings(source, _refCam);
        _refCam.targetTexture = null;
        _refCam.enabled       = false;
    }

    private Camera BuildCamera(string goName, RenderTexture rt, Camera source)
    {
        GameObject go = new GameObject(goName);
        Camera cam    = go.AddComponent<Camera>();
        CopyCameraSettings(source, cam);
        cam.targetTexture = rt;
        return cam;
    }

    private void CopyCameraSettings(Camera src, Camera dst)
    {
        dst.fieldOfView    = src != null ? src.fieldOfView    : fov;
        dst.nearClipPlane  = src != null ? src.nearClipPlane  : 0.3f;
        dst.farClipPlane   = src != null ? src.farClipPlane   : 1000f;
        dst.cullingMask    = src != null ? src.cullingMask    : ~0;
        dst.clearFlags     = src != null ? src.clearFlags     : CameraClearFlags.Skybox;
        dst.backgroundColor = src != null ? src.backgroundColor : Color.black;
    }

    private void CreateFullscreenCanvas()
    {
        Shader shader = Shader.Find("Custom/AdaptiveSplitScreen");
        if (shader == null)
        {
            Debug.LogError("[AdaptiveSplitScreen] Shader 'Custom/AdaptiveSplitScreen' not found. " +
                           "Make sure AdaptiveSplitScreen.shader is in your project.");
            return;
        }

        _splitMat = new Material(shader);
        _splitMat.SetTexture("_CamTex1",   _rt1);
        _splitMat.SetTexture("_CamTex2",   _rt2);
        _splitMat.SetColor  ("_LineColor", lineColor);
        _splitMat.SetFloat  ("_LineWidth", lineWidth);

        // Full-screen ScreenSpaceOverlay canvas
        GameObject canvasGo = new GameObject("SplitScreenCanvas");
        Canvas canvas       = canvasGo.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        // RawImage that fills the canvas — driven by our custom material
        GameObject imgGo = new GameObject("SplitImage");
        imgGo.transform.SetParent(canvasGo.transform, false);

        RawImage img  = imgGo.AddComponent<RawImage>();
        img.material  = _splitMat;
        img.color     = Color.white;

        RectTransform rect = img.rectTransform;
        rect.anchorMin  = Vector2.zero;
        rect.anchorMax  = Vector2.one;
        rect.offsetMin  = Vector2.zero;
        rect.offsetMax  = Vector2.zero;
    }

    // -----------------------------------------------------------------------
    //  Per-frame update
    // -----------------------------------------------------------------------

    private void LateUpdate()
    {
        if (player1 == null || player2 == null) return;

        MoveCamera(_cam1,   player1.position);
        MoveCamera(_cam2,   player2.position);

        Vector3 midPoint = (player1.position + player2.position) * 0.5f;
        MoveCamera(_refCam, midPoint);

        UpdateSplitLine();
    }

    /// <summary>Smoothly moves a camera to an overhead position above <paramref name="target"/>.</summary>
    private void MoveCamera(Camera cam, Vector3 target)
    {
        Vector3 desiredPos = target + new Vector3(camOffset.x,
                                                   camHeight + camOffset.y,
                                                  -camBackDistance + camOffset.z);

        cam.transform.position = Vector3.Lerp(cam.transform.position,
                                               desiredPos,
                                               Time.deltaTime * lerpSpeed);

        Quaternion desiredRot = Quaternion.LookRotation(target - cam.transform.position);
        cam.transform.rotation = Quaternion.Lerp(cam.transform.rotation,
                                                  desiredRot,
                                                  Time.deltaTime * lerpSpeed);
    }

    /// <summary>
    /// Projects both players through the reference camera into UV space,
    /// then passes the split line normal + centre to the shader.
    /// </summary>
    private void UpdateSplitLine()
    {
        // WorldToViewportPoint works even on a disabled camera — it just uses the
        // camera's current transform + projection matrix, no rendering needed.
        Vector3 vp1 = _refCam.WorldToViewportPoint(player1.position);
        Vector3 vp2 = _refCam.WorldToViewportPoint(player2.position);

        Vector2 uv1 = new Vector2(vp1.x, vp1.y);
        Vector2 uv2 = new Vector2(vp2.x, vp2.y);

        // Normal of the split line = direction from p1 → p2 in UV space.
        // Pixels where dot(pixel - center, normal) >= 0 show cam1, otherwise cam2.
        Vector2 normal = uv2 - uv1;
        if (normal.sqrMagnitude < 0.0001f)
            normal = Vector2.right;   // fallback when players are exactly on top of each other
        normal.Normalize();

        Vector2 center = (uv1 + uv2) * 0.5f;

        _splitMat.SetVector("_SplitNormal", new Vector4(normal.x, normal.y, 0f, 0f));
        _splitMat.SetVector("_SplitCenter", new Vector4(center.x, center.y, 0f, 0f));
    }

    // -----------------------------------------------------------------------
    //  Cleanup
    // -----------------------------------------------------------------------

    private void OnDestroy()
    {
        if (_rt1 != null) { _rt1.Release(); Destroy(_rt1); }
        if (_rt2 != null) { _rt2.Release(); Destroy(_rt2); }
        if (_splitMat != null) Destroy(_splitMat);
    }
}
