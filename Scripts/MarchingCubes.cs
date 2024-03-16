using Godot;
using System.Collections.Generic;
using System;
public class MarchingCubes : Node
{
    private static readonly int[] edgeTable = {
        0x0  , 0x109, 0x203, 0x30a, 0x406, 0x50f, 0x605, 0x70c,
        0x80c, 0x905, 0xa0f, 0xb06, 0xc0a, 0xd03, 0xe09, 0xf00,
        0x190, 0x99 , 0x393, 0x29a, 0x596, 0x49f, 0x795, 0x69c,
        0x99c, 0x895, 0xb9f, 0xa96, 0xd9a, 0xc93, 0xf99, 0xe90,
        0x230, 0x339, 0x33 , 0x13a, 0x636, 0x73f, 0x435, 0x53c,
        0xa3c, 0xb35, 0x83f, 0x936, 0xe3a, 0xf33, 0xc39, 0xd30,
        0x3a0, 0x2a9, 0x1a3, 0xaa , 0x7a6, 0x6af, 0x5a5, 0x4ac,
        0xbac, 0xaa5, 0x9af, 0x8a6, 0xfaa, 0xea3, 0xda9, 0xca0,
        0x460, 0x569, 0x663, 0x76a, 0x66 , 0x16f, 0x265, 0x36c,
        0xc6c, 0xd65, 0xe6f, 0xf66, 0x86a, 0x963, 0xa69, 0xb60,
        0x5f0, 0x4f9, 0x7f3, 0x6fa, 0x1f6, 0xff , 0x3f5, 0x2fc,
        0xdfc, 0xcf5, 0xfff, 0xef6, 0x9fa, 0x8f3, 0xbf9, 0xaf0,
        0x650, 0x759, 0x453, 0x55a, 0x256, 0x35f, 0x55 , 0x15c,
        0xe5c, 0xf55, 0xc5f, 0xd56, 0xa5a, 0xb53, 0x859, 0x950,
        0x7c0, 0x6c9, 0x5c3, 0x4ca, 0x3c6, 0x2cf, 0x1c5, 0xcc ,
        0xfcc, 0xec5, 0xdcf, 0xcc6, 0xbca, 0xac3, 0x9c9, 0x8c0,
        0x8c0, 0x9c9, 0xac3, 0xbca, 0xcc6, 0xdcf, 0xec5, 0xfcc,
        0xcc , 0x1c5, 0x2cf, 0x3c6, 0x4ca, 0x5c3, 0x6c9, 0x7c0,
        0x950, 0x859, 0xb53, 0xa5a, 0xd56, 0xc5f, 0xf55, 0xe5c,
        0x15c, 0x55 , 0x35f, 0x256, 0x55a, 0x453, 0x759, 0x650,
        0xaf0, 0xbf9, 0x8f3, 0x9fa, 0xef6, 0xfff, 0xcf5, 0xdfc,
        0x2fc, 0x3f5, 0xff , 0x1f6, 0x6fa, 0x7f3, 0x4f9, 0x5f0,
        0xb60, 0xa69, 0x963, 0x86a, 0xf66, 0xe6f, 0xd65, 0xc6c,
        0x36c, 0x265, 0x16f, 0x66 , 0x76a, 0x663, 0x569, 0x460,
        0xca0, 0xda9, 0xea3, 0xfaa, 0x8a6, 0x9af, 0xaa5, 0xbac,
        0x4ac, 0x5a5, 0x6af, 0x7a6, 0xaa , 0x1a3, 0x2a9, 0x3a0,
        0xd30, 0xc39, 0xf33, 0xe3a, 0x936, 0x83f, 0xb35, 0xa3c,
        0x53c, 0x435, 0x73f, 0x636, 0x13a, 0x33 , 0x339, 0x230,
        0xe90, 0xf99, 0xc93, 0xd9a, 0xa96, 0xb9f, 0x895, 0x99c,
        0x69c, 0x795, 0x49f, 0x596, 0x29a, 0x393, 0x99 , 0x190,
        0xf00, 0xe09, 0xd03, 0xc0a, 0xb06, 0xa0f, 0x905, 0x80c,
        0x70c, 0x605, 0x50f, 0x406, 0x30a, 0x203, 0x109, 0x0
    };

    private static readonly Vector3[] vertexList = {
        new Vector3(-1, -1, -1), new Vector3( 1, -1, -1), new Vector3(-1, -1, 1), new Vector3( 1, -1, 1),
        new Vector3(-1,  1, -1), new Vector3( 1,  1, -1), new Vector3(-1,  1, 1), new Vector3( 1,  1, 1)
    };

    private static readonly int[,] edgeIndexArray = {
        {0, 1}, {1, 3}, {3, 2}, {2, 0}, {4, 5}, {5, 7}, {7, 6}, {6, 4},
        {0, 4}, {1, 5}, {3, 7}, {2, 6}
    };

    private float[,,] _scalarField;
    private int _size;
    private float _threshold;

    public override void _Ready()
    {
        // Example usage: Generate a scalar field and call MarchingCubes
        _size = 32;
        _scalarField = GenerateScalarField(_size);
        _threshold = 0.5f;

        var mesh = GenerateMarchingCubesMesh(_scalarField, _threshold);
        // Create a MeshInstance and assign the generated mesh
        var meshInstance = new MeshInstance();
        meshInstance.Mesh = mesh;
        AddChild(meshInstance);
    }

    private float[,,] GenerateScalarField(int size)
    {
        float[,,] field = new float[size, size, size];
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int z = 0; z < size; z++)
                {
                    float value = (float)Math.Sin(x * 0.1) * (float)Math.Cos(y * 0.2) * (float)Math.Sin(z * 0.3);
                    field[x, y, z] = value;
                }
            }
        }
        return field;
    }

    private Mesh GenerateMarchingCubesMesh(float[,,] scalarField, float threshold)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<int> indices = new List<int>();

        for (int x = 0; x < _size - 1; x++)
        {
            for (int y = 0; y < _size - 1; y++)
            {
                for (int z = 0; z < _size - 1; z++)
                {
                    int cubeIndex = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        if (scalarField[(int)(x + MarchingCubes.vertexList[i].x),
                            (int)(y + MarchingCubes.vertexList[i].y),
                            (int)(z + MarchingCubes.vertexList[i].z)] > threshold)
                        {
                            cubeIndex |= 1 << i;
                        }
                    }

                    if (edgeTable[cubeIndex] == 0)
                        continue;

                    Vector3[] vertexList = GetVertexList(scalarField, x, y, z, threshold);
                    if (vertexList != null)
                    {
                        int offset = vertices.Count;
                        foreach (Vector3 vertex in vertexList)
                        {
                            vertices.Add(vertex);
                            normals.Add(-vertex.Normalized());
                        }

                        for (int i = 0; edgeTable[cubeIndex] != 0; i += 3)
                        {
                            if ((edgeTable[cubeIndex] & (1 << ((i + 2) % 12))) != 0)
                            {
                                int a = offset + edgeIndexArray[i, 0];
                                int b = offset + edgeIndexArray[i, 1];
                                indices.Add(a);
                                indices.Add(b);
                            }
                        }
                    }
                }
            }
        }

        //https://docs.godotengine.org/en/stable/classes/class_meshdatatool.html
        var arrayMesh = new ArrayMesh();
        var verticesArray = new Godot.Collections.Array(vertices.ToArray());
        arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, verticesArray);//normals.ToArray(), null, null, indices.ToArray()
        var mdt = new MeshDataTool();
        mdt.CreateFromSurface(arrayMesh, 0);
        for (var i = 0; i < mdt.GetVertexCount(); i++)
        {
            Vector3 vertex = mdt.GetVertex(i);
            // In this example we extend the mesh by one unit, which results in separated faces as it is flat shaded.
            vertex += mdt.GetVertexNormal(i);
            // Save your change.
            mdt.SetVertex(i, vertex);
        }
        arrayMesh.ClearSurfaces();
        mdt.CommitToSurface(arrayMesh);
        var mi = new MeshInstance();
        mi.Mesh = arrayMesh;
        AddChild(mi);
        return arrayMesh;
    }

    private Vector3[] GetVertexList(float[,,] scalarField, int x, int y, int z, float threshold)
    {
        Vector3[] vertices = new Vector3[12];
        int index = 0;

        for (int i = 0; i < 12; i++)
        {
            int x1 = x + edgeIndexArray[i, 0] / 4;
            int y1 = y + edgeIndexArray[i, 0] / 2 % 2;
            int z1 = z + edgeIndexArray[i, 0] % 2;

            int x2 = x + edgeIndexArray[i, 1] / 4;
            int y2 = y + edgeIndexArray[i, 1] / 2 % 2;
            int z2 = z + edgeIndexArray[i, 1] % 2;

            float value1 = scalarField[x1, y1, z1];
            float value2 = scalarField[x2, y2, z2];

            if ((value1 > threshold && value2 < threshold) || (value1 < threshold && value2 > threshold))
            {
                float mu = (threshold - value1) / (value2 - value1);
                Vector3 vertex = new Vector3(
                    (float)(x1 + mu * (x2 - x1)),
                    (float)(y1 + mu * (y2 - y1)),
                    (float)(z1 + mu * (z2 - z1))
                );
                vertices[index++] = vertex;
            }
        }

        return index > 0 ? vertices : null;
    }
}