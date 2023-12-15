using UnityEngine;
using System.Collections;

public class FadeController : MonoBehaviour
{
    public static FadeController instance { get; private set; }

    [Tooltip("Fade duration")] public float fadeTime = 2.0f;
    [Tooltip("Screen color at maximum fade")] public Color fadeColor = new Color(0.01f, 0.01f, 0.01f, 1.0f);

    public bool fadeOnStart = true;

    public int renderQueue = 5000;

    public float currentAlpha;

    private MeshRenderer fadeRenderer;
    private MeshFilter fadeMesh;
    private Material fadeMaterial = null;
    private bool isFading = false;

    void Start()
    {
        fadeMaterial = new Material(Shader.Find("Shader Graphs/Fade Shader"));
        fadeMaterial.color = fadeColor;

        fadeMesh = gameObject.AddComponent<MeshFilter>();
        fadeRenderer = gameObject.AddComponent<MeshRenderer>();
        fadeRenderer.material = fadeMaterial;

        var mesh = new Mesh();
        fadeMesh.mesh = mesh;

        Vector3[] vertices = new Vector3[4];

        float width = 2f;
        float height = 2f;
        float depth = 1f;

        vertices[0] = new Vector3(-width, -height, depth);
        vertices[1] = new Vector3(width, -height, depth);
        vertices[2] = new Vector3(-width, height, depth);
        vertices[3] = new Vector3(width, height, depth);

        mesh.vertices = vertices;

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

        mesh.normals = normals;

        Vector2[] uv = new Vector2[4];

        uv[0] = new Vector2(0, 0);
        uv[1] = new Vector2(1, 0);
        uv[2] = new Vector2(0, 1);
        uv[3] = new Vector2(1, 1);

        mesh.uv = uv;

        if (fadeOnStart)
        {
            FadeIn();
        }

        instance = this;
    }

    public void FadeIn()
    {
        StartCoroutine(Fade(1, 0));
    }

    public void FadeOut()
    {
        StartCoroutine(Fade(0, 1));
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
            currentAlpha = Mathf.Lerp(startAlpha, endAlpha, Mathf.Clamp01(elapsedTime / fadeTime));
            SetMaterialAlpha();
            yield return new WaitForEndOfFrame();
        }
        currentAlpha = endAlpha;
        SetMaterialAlpha();
    }

    private void SetMaterialAlpha()
    {
        isFading = fadeMaterial.GetFloat("_Alpha") > 0;
        Material tempMaterial = new Material(Shader.Find("Shader Graphs/Fade Shader"));
        tempMaterial.SetFloat("_Alpha", currentAlpha);
        if (fadeMaterial != null)
        {
            fadeMaterial = tempMaterial;
        }
    }
}
