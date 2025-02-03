using System.Threading.Tasks;
using UnityEngine;

public class LineRendererPoolManager : MonoBehaviour
{
    public static LineRendererPoolManager Instance;

    public GameObject lineRendererPrefab;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void AddLineRendererToPool()
    {
        GameObject lineRenderer = Instantiate(lineRendererPrefab, transform);
        lineRenderer.SetActive(false);
    }

    private LineRenderer GetLineRenderer()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            if (!transform.GetChild(i).gameObject.activeInHierarchy)
            {
                return transform.GetChild(i).GetComponent<LineRenderer>();
            }
        }

        AddLineRendererToPool();
        return transform.GetChild(transform.childCount - 1).GetComponent<LineRenderer>();
    }

    public void RenderLine(Vector3 start, Vector3 end, Color color, float width, float duration)
    {
        LineRenderer lineRenderer = GetLineRenderer();
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        lineRenderer.gameObject.SetActive(true);
        _ = DisableLineRenderer(lineRenderer, duration);
    }

    private async Task DisableLineRenderer(LineRenderer lineRenderer, float duration)
    {
        await Task.Delay((int)(duration * 1000));
        lineRenderer.gameObject.SetActive(false);
    }
	
}
