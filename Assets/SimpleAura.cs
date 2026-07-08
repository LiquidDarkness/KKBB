using UnityEngine;

public class SimpleAura : MonoBehaviour
{
    public int pointCount = 20;
    public float radius = 1f;
    public float rotationSpeed = 50f;

    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = pointCount;
        lineRenderer.loop = true;
    }

    void Update()
    {
        float rotation = Time.time * rotationSpeed;

        for (int i = 0; i < pointCount; i++)
        {
            float angle = (i / (float)pointCount) * Mathf.PI * 2f;

            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * (radius + Mathf.Sin(angle * 5 + Time.time) * 0.2f);

            Vector3 pos = new Vector3(x, y, 0);

            // obrót wokó³ œrodka
            pos = Quaternion.Euler(0, 0, rotation) * pos;

            lineRenderer.SetPosition(i, pos);
        }
    }
}
