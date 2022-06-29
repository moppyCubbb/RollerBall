using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PathCreator))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class TrackCreator : MonoBehaviour
{
    public float trackWidth = 1;
    public float trackScale = 1;
    [Range(0.05f, 1.5f)]
    public float spacing = 1;

    // auto update in editor
    public bool autoUpdate;

    public float tiling = 1;

    public void UpdateTrack()
    {
        Path path = GetComponent<PathCreator>().path;
        Vector2[] points = path.CalculateEvenlySpacedPoints(spacing);
        GetComponent<MeshFilter>().mesh = CreateTrackMesh(points, path.IsClosed);

        int textureRepeat = Mathf.RoundToInt(tiling * points.Length * spacing * 0.05f);
        GetComponent<MeshRenderer>().sharedMaterial.mainTextureScale = new Vector2(1, textureRepeat);

        transform.localScale = new Vector3(trackScale, trackScale, 1);
    }
    Mesh CreateTrackMesh(Vector2[] points, bool isClosed)
    {
        Vector3[] vertices = new Vector3[points.Length * 2];
        Vector2[] uvs = new Vector2[vertices.Length];

        int numberOfTriangles = 2 * (points.Length - 1) + (isClosed ? 2 : 0);
        int[] triangles = new int[numberOfTriangles * 3];

        int verticeIndex = 0;
        int triangleIndex = 0;

        for (int i = 0; i < points.Length; i++)
        {
            Vector2 forward = Vector2.zero;
            if (i < points.Length - 1 || isClosed)
            {
                forward += points[(i + 1) % points.Length] - points[i];
            }
            if (i > 0 || isClosed)
            {
                forward += points[i] - points[(i - 1 + points.Length) % points.Length];
            }
            forward.Normalize();

            Vector2 left = new Vector2(-forward.y, forward.x);

            vertices[verticeIndex] = points[i] + left * trackWidth * 0.5f;
            vertices[verticeIndex + 1] = points[i] - left * trackWidth * 0.5f;

            float completionPercent = i / (float)(points.Length - 1); // but if going comppletly from 0 - 1 would cause some art problem when it is closed
            float v = 1 - Mathf.Abs(2 * completionPercent - 1);
            uvs[verticeIndex] = new Vector2(0, v);
            uvs[verticeIndex + 1] = new Vector2(1, v);


            if (i < points.Length - 1 || isClosed) //if isClosed path, need two more triangles
            {
                triangles[triangleIndex] = verticeIndex;
                triangles[triangleIndex + 1] = (verticeIndex + 2) % vertices.Length;
                triangles[triangleIndex + 2] = verticeIndex + 1;

                triangles[triangleIndex + 3] = verticeIndex + 1;
                triangles[triangleIndex + 4] = (verticeIndex + 2) % vertices.Length;
                triangles[triangleIndex + 5] = (verticeIndex + 3) % vertices.Length;
            }

            verticeIndex += 2;
            triangleIndex += 6;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        EdgeCollider2D[] edgeColliders = GetComponents<EdgeCollider2D>();
        if (edgeColliders != null && edgeColliders.Length > 0)
        {
            for (int path = 0; path < edgeColliders.Length; path++)
            {
                List<Vector2> edgePath = new List<Vector2>();
                for (int i = 0; i < vertices.Length && i + path < vertices.Length; i += 2)
                {
                    edgePath.Add(new Vector2(vertices[i + path].x, vertices[i + path].y));
                }
                edgeColliders[path].points = edgePath.ToArray();
            }
        }

        return mesh;
    }
}
