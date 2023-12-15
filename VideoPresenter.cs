using UnityEngine;
using System.Collections;
using UnityEngine.Video;

public class VideoPresenter : MonoBehaviour
{
    public static VideoPresenter instance { get; private set; }

    [Tooltip("Fade duration")] public float fadeTime = 2.0f;
    [Tooltip("Screen color at maximum fade")] public Color fadeColor = new Color(0.01f, 0.01f, 0.01f, 1.0f);
    public RenderTexture renderTexture;

    public bool fadeOnStart = true;

    public int renderQueue = 5000;

    public float currentAlpha { get { return Mathf.Max(explicitFadeAlpha, animatedFadeAlpha, uiFadeAlpha); } }

    private float explicitFadeAlpha = 0.0f;
    public float animatedFadeAlpha = 0.0f;
    private float uiFadeAlpha = 0.0f;

    [SerializeField] private Vector3[] vertices = new Vector3[4];

    [SerializeField] private Shader testShader;

    private MeshRenderer fadeRenderer;
    private MeshFilter fadeMesh;
    private Material fadeMaterial = null;
    private bool isFading = false;

    private Vector3 screenSpacePos;
    Mesh mesh;

    private IEnumerator WaitForTexture()
    {
        var assetPathHeader = Application.installMode == ApplicationInstallMode.Editor ? $"D:\\Users\\Admin\\Desktop\\Clean 4H\\Assets\\asset_video\\" : $"{Application.persistentDataPath}/asset_video/";
        //fadeMaterial = new Material(Shader.Find("Shader Graph/Transparent Shader"));
        fadeMaterial = new Material(testShader);
        
        yield return new WaitUntil(() => renderTexture != null);
        fadeMaterial.mainTexture = renderTexture;
        fadeMesh = gameObject.AddComponent<MeshFilter>();
        fadeRenderer = gameObject.AddComponent<MeshRenderer>();

        mesh = new Mesh();
        fadeMesh.mesh = mesh;

        mesh.vertices = vertices;

        float width = 2f;
        float height = 2f;
        float depth = 1f;

        int[] tri = new int[6];

        tri[0] = 0;
        tri[1] = 2;
        tri[2] = 1;

        tri[3] = 2;
        tri[4] = 3;
        tri[5] = 1;

        mesh.triangles = tri;

        Vector3[] normals = new Vector3[4];

        normals[0] = -Vector3.forward;
        normals[1] = -Vector3.forward;
        normals[2] = -Vector3.forward;
        normals[3] = -Vector3.forward;
        Debug.Log(vertices.Length);

        mesh.normals = normals;

        Vector2[] uv = new Vector2[4];

        uv[0] = new Vector2(0, 0);
        uv[1] = new Vector2(1, 0);
        uv[2] = new Vector2(0, 1);
        uv[3] = new Vector2(1, 1);

        mesh.uv = uv;

        explicitFadeAlpha = 0.0f;
        animatedFadeAlpha = 0.0f;
        uiFadeAlpha = 0.0f;

        if (fadeOnStart)
        {
            FadeIn();
        }

        instance = this;
    }


    void Start()
    {
        StartCoroutine(WaitForTexture());
        
    }

    public void FadeIn()
    {
        StartCoroutine(Fade(1.0f, 0.0f));
    }

    public void FadeOut()
    {
        StartCoroutine(Fade(0, 1));
    }

    void OnEnable()
    {
        if (!fadeOnStart)
        {
            explicitFadeAlpha = 0.0f;
            animatedFadeAlpha = 0.0f;
            uiFadeAlpha = 0.0f;
        }
    }

    void OnDestroy()
    {
        instance = null;

        if (fadeRenderer != null)
            Destroy(fadeRenderer);

        if (fadeMaterial != null)
            Destroy(fadeMaterial);

        if (fadeMesh != null)
            Destroy(fadeMesh);
    }

    IEnumerator Fade(float startAlpha, float endAlpha)
    {
        float elapsedTime = 0.0f;
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            animatedFadeAlpha = Mathf.Lerp(startAlpha, endAlpha, Mathf.Clamp01(elapsedTime / fadeTime));
            SetMaterialAlpha();
            yield return new WaitForEndOfFrame();
        }
        animatedFadeAlpha = endAlpha;
        SetMaterialAlpha();
    }

    private void SetMaterialAlpha()
    {
        Color color = fadeColor;
        color.a = currentAlpha;
        isFading = color.a > 0;
        if (fadeMaterial != null)
        {
            fadeMaterial.color = color;
            fadeMaterial.renderQueue = renderQueue;
            fadeRenderer.material = fadeMaterial;
            fadeRenderer.enabled = isFading;
        }
    }
}
