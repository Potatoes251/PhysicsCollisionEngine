using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DynamicTree : MonoBehaviour
{
    [SerializeField]
    float margin = .2f;

    [SerializeField]
    bool debug = true;

    [HideInInspector]
    public List<BaseCollider> colliders;

    List<(int x, int y)> pairs = new();

    List<Node> nodes = new();
    List<int> leafOf = new();
    Stack<int> freeNodes = new();
    int root = -1;

#region MonoBehaviour

    // Collect all colliders and store them in the tree
    void Start()
    {
        colliders = new(FindObjectsByType<BaseCollider>(FindObjectsSortMode.None));

        for (int i = 0; i < colliders.Count; i++)
        {
            int leafIdx = InsertNode(colliders[i], i);
            leafOf.Add(leafIdx);
        }
    }

    public void UpdateTree()
    {
        for (int i = 0; i < colliders.Count; i++)
        {
            int leaf = leafOf[i];

            if (leaf < 0 || leaf >= nodes.Count) continue;

            Node n = nodes[leaf];

            if (!n.bounds.Contains(colliders[i].bounds))
            {
                RemoveLeaf(leaf);
                leafOf[i] = InsertNode(colliders[i], i);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!EditorApplication.isPlaying || !debug) return;

        foreach (Node node in nodes)
        {
            Gizmos.DrawWireCube(node.bounds.center, node.bounds.size);
        }

        GetPairs();

        foreach ((int, int) pair in pairs)
        {
            if (GJK(pair))
            {
                Gizmos.color = Color.red;
                HitInfo info = EPA(pair);
                Gizmos.DrawSphere(info.hitPoint, .1f);
                Gizmos.DrawLine(info.hitPoint, info.hitPoint + info.normal * info.depth);
            }
            else
            {
                Gizmos.color = Color.green;
                (int x, int y) = pair;
                Gizmos.DrawLine(colliders[x].bounds.center, colliders[y].bounds.center);
            }
        }
    }

#endregion

#region Bounds

    AABB AddBounds(AABB bound1, AABB bound2)
    {
        bound1.Encapsulate(bound2);
        return bound1;
    }

    AABB Fatten(AABB bounds)
    {
        bounds.Expand(margin * 2);
        return bounds;
    }

    #endregion

#region Node
    struct Node
    {
        public AABB bounds;
        public int parent;
        public int childL;
        public int childR;
        public int colliderId;

        public Node(AABB bounds, int colliderId = -1, int parent = -1, int childL = -1, int childR = -1)
        {
            this.bounds = bounds;
            this.parent = parent;
            this.childL = childL;
            this.childR = childR;
            this.colliderId = colliderId;
        }

        public readonly bool IsLeaf() { return childL == -1 && childR == -1; }
    }

    int Allocate(Node n)
    {
        if (freeNodes.Count > 0)
        {
            int i = freeNodes.Pop();
            nodes[i] = n;
            return i;
        }
        nodes.Add(n);
        return nodes.Count - 1;
    }

    int InsertNode(BaseCollider newCollider, int colliderId)
    {
        AABB fattened = Fatten(newCollider.bounds);
        if (root == -1)
        {
            root = Allocate(new Node(fattened, colliderId));
            return root;
        }

        int sibling = root;
        while (!nodes[sibling].IsLeaf())
        {
            Node n = nodes[sibling];
            float costL = AddBounds(nodes[n.childL].bounds, fattened).Area() - nodes[n.childL].bounds.Area();
            float costR = AddBounds(nodes[n.childR].bounds, fattened).Area() - nodes[n.childR].bounds.Area();

            sibling = costL > costR ? n.childR : n.childL;
        }

        int oldParent = nodes[sibling].parent;

        int leaf = Allocate(new(fattened, colliderId));
        int newParent = Allocate(new(AddBounds(nodes[sibling].bounds, fattened), -1, oldParent, sibling, leaf));

        // Wire up
        Node leafNode = nodes[leaf];
        leafNode.parent = newParent;
        nodes[leaf] = leafNode;

        Node siblingNode = nodes[sibling];
        siblingNode.parent = newParent;
        nodes[sibling] = siblingNode;

        if (oldParent == -1)
        {
            root = newParent;
        }
        else
        {
            Node oldParentNode = nodes[oldParent];
            if (oldParentNode.childL == sibling) oldParentNode.childL = newParent;
            else oldParentNode.childR = newParent;
            nodes[oldParent] = oldParentNode;
        }

        UpdateParentBounds(nodes[sibling].parent);

        return leaf;
    }

    private void RemoveLeaf(int leaf)
    {
        freeNodes.Push(leaf);
        if (leaf == root)
        {
            root = -1;
            return;
        }

        int parent = nodes[leaf].parent;

        Node p = nodes[parent];
        int sibling = p.childL == leaf ? p.childR : p.childL;
        int grandParent = p.parent;

        Node sib = nodes[sibling];
        sib.parent = grandParent;
        nodes[sibling] = sib;

        if (grandParent == -1)
        {
            root = sibling;
        }
        else
        {
            Node grandPa = nodes[grandParent];

            if (grandPa.childL == parent)
                grandPa.childL = sibling;
            else
                grandPa.childR = sibling;

            nodes[grandParent] = grandPa;
        }

        freeNodes.Push(parent);
        UpdateParentBounds(grandParent);
    }

    private void UpdateParentBounds(int parent)
    {
        while (parent != -1)
        {
            Node n = nodes[parent];
            n.bounds = AddBounds(nodes[n.childL].bounds, nodes[n.childR].bounds);
            nodes[parent] = n;
            parent = nodes[parent].parent;
        }
    }

    public List<(int x, int y)> GetPairs()
    {
        pairs.Clear();

        PairsHelper(root);

        return pairs;
    }

    private void PairsHelper(int n)
    {
        if (n == -1) return;

        Node node = nodes[n];
        if (node.IsLeaf()) return;

        PairsHelper(node.childL);
        PairsHelper(node.childR);
        CrossChildren(node.childL, node.childR);
    }

    private void CrossChildren(int childL, int childR)
    {
        if (childL == -1 || childR == -1) return;

        Node nodeL = nodes[childL];
        Node nodeR = nodes[childR];

        if (!nodeL.bounds.Collide(nodeR.bounds)) return;

        if (nodeL.IsLeaf() && nodeR.IsLeaf())
        {
            pairs.Add((nodeL.colliderId, nodeR.colliderId));
        }
        else if (nodeL.IsLeaf())
        {
            CrossChildren(childL, nodeR.childL);
            CrossChildren(childL, nodeR.childR);
        }
        else if (nodeR.IsLeaf())
        {
            CrossChildren(nodeL.childL, childR);
            CrossChildren(nodeL.childR, childR);
        }
        else
        {
            CrossChildren(nodeL.childL, nodeR.childL);
            CrossChildren(nodeL.childL, nodeR.childR);
            CrossChildren(nodeL.childR, nodeR.childL);
            CrossChildren(nodeL.childR, nodeR.childR);
        }
    }

    #endregion

#region GJK

    List<SupportPoint> simplex = new List<SupportPoint>();

    public struct HitInfo
    {
        public Vector3 normal;
        public Vector3 hitPoint;
        public float depth;
    }

    public struct SupportPoint
    {
        public Vector3 point; // a - b (Minkowski difference)
        public Vector3 a;     // point on collider 1
        public Vector3 b;     // point on collider 2
    }

    static SupportPoint Support(BaseCollider c1, BaseCollider c2, Vector3 dir)
    {
        Vector3 a = c1.Support(dir);
        Vector3 b = c2.Support(-dir);
        return new SupportPoint { a = a, b = b, point = a - b };
    }

    static bool SameDirection(Vector3 a, Vector3 b) => Vector3.Dot(a, b) > 0f;

    bool NextSimplex(ref List<SupportPoint> simplex, ref Vector3 dir)
    {
        return simplex.Count switch
        {
            2 => Line(ref dir),
            3 => Triangle(ref dir),
            4 => Tetrahedron(ref dir),
            _ => false,
        };
    }

    public bool GJK((int, int) pair)
    {
        (int x, int y) = pair;
        BaseCollider c1 = colliders[x];
        BaseCollider c2 = colliders[y];
        simplex.Clear();

        Vector3 dir = Vector3.forward;

        SupportPoint supportPoint = Support(c1, c2, dir);
        Vector3 p = supportPoint.point;
        simplex.Add(supportPoint);

        dir = -p;

        for (int i = 0; i < 64; i++)
        {
            if (dir.sqrMagnitude < 1e-12f)
                return true;

            supportPoint = Support(c1, c2, dir);
            p = supportPoint.point;

            if (Vector3.Dot(p, dir) <= 0f) 
                return false;

            simplex.Insert(0, supportPoint);

            if (NextSimplex(ref simplex, ref dir))
            {
                return true;
            }
        }
        return false;
    }

    (List<(Vector3, float)>, int) GetFaceNormals(List<SupportPoint> polytope, List<int> faces)
    {
        List<(Vector3, float)> normals = new();
        int minTriangle = 0;
        float minDist = float.MaxValue;

        for (int i = 0; i < faces.Count; i += 3)
        {
            SupportPoint a = polytope[faces[i]];
            SupportPoint b = polytope[faces[i + 1]];
            SupportPoint c = polytope[faces[i + 2]];

            Vector3 normal = Vector3.Cross(b.point - a.point, c.point - a.point).normalized;
            float distance = Vector3.Dot(normal, a.point);

            if (distance < 0)
            {
                normal *= -1;
                distance *= -1;
            }
            
            normals.Add((normal, distance));

            if (distance < minDist)
            {
                minDist = distance;
                minTriangle = i / 3;
            }
        }

        return (normals, minTriangle);
    }

    // needs to be called right after GJK
    public HitInfo EPA((int, int) pair)
    {
        (int x, int y) = pair;
        BaseCollider c1 = colliders[x];
        BaseCollider c2 = colliders[y];

        List<SupportPoint> polytope = simplex;
        List<int> faces = new (){0, 1, 2,
                                0, 3, 1,
                                0, 2, 3,
                                1, 3, 2};

        (List<(Vector3, float)> normals, int minTriangle) = GetFaceNormals(polytope, faces);

        Vector3 minNormal = new();
        float minDist = float.MaxValue;
        SupportPoint support = new();

        for (int i = 0; i < 64 && minDist == float.MaxValue; i++)
        {
            minNormal = normals[minTriangle].Item1;
            minDist = normals[minTriangle].Item2;

            support = Support(c1, c2, minNormal);

            float distance = Vector3.Dot(support.point, minNormal);

            if (Mathf.Abs(distance - minDist) > 0.001f)
            {
                minDist = float.MaxValue;

                List<(int, int)> edges = new();

                for (int j = 0; j < normals.Count; j++)
                {
                    int f = j * 3;

                    Vector3 onFace = polytope[faces[f]].point;
                    if (Vector3.Dot(normals[j].Item1, support.point - onFace) > 1e-6f)
                    {
                        AddIfUniqueEdge(ref edges, faces, f, f + 1);
                        AddIfUniqueEdge(ref edges, faces, f + 1, f + 2);
                        AddIfUniqueEdge(ref edges, faces, f + 2, f);

                        faces[f + 2] = faces[^1]; 
                        faces.RemoveAt(faces.Count - 1);
                        faces[f + 1] = faces[^1]; 
                        faces.RemoveAt(faces.Count - 1);
                        faces[f] = faces[^1]; 
                        faces.RemoveAt(faces.Count - 1);

                        normals[j] = normals[^1]; // pop-erase
                        normals.RemoveAt(normals.Count - 1);

                        j--;
                    }
                }

                List<int> newFaces = new();

                foreach ((int idx1, int idx2) in edges)
                {
                    newFaces.Add(idx1);
                    newFaces.Add(idx2);
                    newFaces.Add(polytope.Count);
                }
                polytope.Add(support);

                var (newNormals, newMinFace) = GetFaceNormals(polytope, newFaces);

                float oldMinDistance = float.MaxValue;

                for (int j = 0; j < normals.Count; j++)
                {
                    if (normals[j].Item2 < oldMinDistance)
                    {
                        oldMinDistance = normals[j].Item2;
                        minTriangle = j;
                    }
                }

                if (newNormals.Count != 0 && newNormals[newMinFace].Item2 < oldMinDistance)
                {
                    minTriangle = newMinFace + normals.Count;
                }

                faces.AddRange(newFaces);
                normals.AddRange(newNormals);
            }
        }

        int ff = minTriangle * 3;
        SupportPoint A = polytope[faces[ff]];
        SupportPoint B = polytope[faces[ff + 1]];
        SupportPoint C = polytope[faces[ff + 2]];

        Vector3 p = minNormal * minDist;
        Vector3 bary = Barycentric(p, A.point, B.point, C.point);

        Vector3 contactA = bary.x * A.a + bary.y * B.a + bary.z * C.a;
        Vector3 contactB = bary.x * A.b + bary.y * B.b + bary.z * C.b;

        return new() { normal = minNormal, depth = minDist, hitPoint = (contactA + contactB) * .5f };
    }

    static Vector3 Barycentric(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 v0 = b - a, v1 = c - a, v2 = p - a;
        float d00 = Vector3.Dot(v0, v0);
        float d01 = Vector3.Dot(v0, v1);
        float d11 = Vector3.Dot(v1, v1);
        float d20 = Vector3.Dot(v2, v0);
        float d21 = Vector3.Dot(v2, v1);
        float denom = d00 * d11 - d01 * d01;

        float v = (d11 * d20 - d01 * d21) / denom;
        float w = (d00 * d21 - d01 * d20) / denom;
        return new Vector3(1f - v - w, v, w);
    }

    void AddIfUniqueEdge(ref List<(int, int)> edges, List<int> faces, int a, int b)
    {
        (int, int) reverse = (faces[b], faces[a]);
        int index = edges.IndexOf(reverse);

        if (index >= 0)
            edges.RemoveAt(index);
        else
            edges.Add((faces[a], faces[b]));
    }

    bool Line(ref Vector3 dir)
    {
        SupportPoint a = simplex[0];
        SupportPoint b = simplex[1];

        Vector3 ab = b.point - a.point;
        Vector3 ao = -a.point;

        if (SameDirection(ab, ao))
        {
            dir = Vector3.Cross(Vector3.Cross(ab, ao), ab);
        }
        else
        {
            simplex = new List<SupportPoint> { a };
            dir = ao;
        }

        return false;
    }

    bool Triangle(ref Vector3 dir)
    {
        SupportPoint a = simplex[0];
        SupportPoint b = simplex[1];
        SupportPoint c = simplex[2];
        
        Vector3 ab = b.point - a.point;
        Vector3 ac = c.point - a.point;
        Vector3 ao = -a.point;
        
        Vector3 abc = Vector3.Cross(ab, ac);

        if (SameDirection(Vector3.Cross(abc, ac), ao))
        {
            if (SameDirection(ac, ao))
            {
                simplex = new List<SupportPoint> { a, c };
                dir = Vector3.Cross(Vector3.Cross(ac, ao), ac);
            }
            else
            {
                simplex = new List<SupportPoint> { a, b };
                return Line(ref dir);
            }
        }
        else
        {
            if (SameDirection(Vector3.Cross(ab, abc), ao))
            {
                simplex = new List<SupportPoint> { a, b };
                return Line(ref dir);
            }
            else
            {
                if (SameDirection(abc, ao))
                {
                    dir = abc;
                }
                else
                {
                    simplex = new List<SupportPoint> { a, c, b };
                    dir = -abc;
                }
            }
        }

        return false;
    }

    bool Tetrahedron(ref Vector3 dir)
    {
        SupportPoint a = simplex[0];
        SupportPoint b = simplex[1];
        SupportPoint c = simplex[2];
        SupportPoint d = simplex[3];
        
        Vector3 ab = b.point - a.point;
        Vector3 ac = c.point - a.point;
        Vector3 ad = d.point - a.point;
        Vector3 ao = -a.point;
        
        Vector3 abc = Vector3.Cross(ab, ac);
        Vector3 acd = Vector3.Cross(ac, ad);
        Vector3 adb = Vector3.Cross(ad, ab);

        if (SameDirection(abc, ao))
        {
            simplex = new List<SupportPoint> { a, b, c };
            return Triangle(ref dir);
        }

        if (SameDirection(acd, ao))
        {
            simplex = new List<SupportPoint> { a, c, d };
            return Triangle(ref dir);
        }

        if (SameDirection(adb, ao))
        {
            simplex = new List<SupportPoint> { a, d, b };
            return Triangle(ref dir);
        }

        return true;
    }

#endregion
}