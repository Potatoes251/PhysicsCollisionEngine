using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using UnityEditor;
using UnityEngine;

public class DynamicTree : MonoBehaviour
{
    [SerializeField]
    float margin = .2f;

    [SerializeField]
    bool debug = true;

    List<BaseCollider> colliders;

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

    private void LateUpdate()
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

        foreach ((int x, int y) in pairs)
        {
            if (GJK(colliders[x], colliders[y]))
            {
                Gizmos.color = Color.red;
            }
            else
            {
                Gizmos.color = Color.green;
            }
            Gizmos.DrawLine(colliders[x].bounds.center, colliders[y].bounds.center);
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

    List<Vector3> simplex = new List<Vector3>();

    Vector3 Support(BaseCollider c1, BaseCollider c2, Vector3 dir)
    {
        return c1.Support(dir) - c2.Support(-dir);
    }

    static bool SameDirection(Vector3 a, Vector3 b) => Vector3.Dot(a, b) > 0f;

    bool NextSimplex(ref List<Vector3> simplex, ref Vector3 dir)
    {
        return simplex.Count switch
        {
            2 => Line(ref dir),
            3 => Triangle(ref dir),
            4 => Tetrahedron(ref dir),
            _ => false,
        };
    }

    public bool GJK(BaseCollider c1, BaseCollider c2)
    {
        simplex.Clear();

        Vector3 dir = Vector3.forward;

        Vector3 p = Support(c1, c2, dir);
        simplex.Add(p);

        dir = -p;

        while (true)
        {
            if (dir.sqrMagnitude < 1e-12f)
                return true;

            p = Support(c1, c2, dir);

            if (Vector3.Dot(p, dir) <= 0f) 
                return false;

            simplex.Insert(0, p);

            if (NextSimplex(ref simplex, ref dir))
            {
                return true;
            }
        }
    }

    bool Line(ref Vector3 dir)
    {
        Vector3 a = simplex[0];
        Vector3 b = simplex[1];

        Vector3 ab = b - a;
        Vector3 ao = -a;

        if (SameDirection(ab, ao))
        {
            dir = Vector3.Cross(Vector3.Cross(ab, ao), ab);
        }
        else
        {
            simplex = new List<Vector3> { a };
            dir = ao;
        }

        return false;
    }

    bool Triangle(ref Vector3 dir)
    {
        Vector3 a = simplex[0];
        Vector3 b = simplex[1];
        Vector3 c = simplex[2];
        
        Vector3 ab = b - a;
        Vector3 ac = c - a;
        Vector3 ao = -a;
        
        Vector3 abc = Vector3.Cross(ab, ac);

        if (SameDirection(Vector3.Cross(abc, ac), ao))
        {
            if (SameDirection(ac, ao))
            {
                simplex = new List<Vector3> { a, c };
                dir = Vector3.Cross(Vector3.Cross(ac, ao), ac);
            }
            else
            {
                simplex = new List<Vector3> { a, b };
                return Line(ref dir);
            }
        }
        else
        {
            if (SameDirection(Vector3.Cross(ab, abc), ao))
            {
                simplex = new List<Vector3> { a, b };
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
                    simplex = new List<Vector3> { a, c, b };
                    dir = -abc;
                }
            }
        }

        return false;
    }

    bool Tetrahedron(ref Vector3 dir)
    {
        Vector3 a = simplex[0];
        Vector3 b = simplex[1];
        Vector3 c = simplex[2];
        Vector3 d = simplex[3];
        
        Vector3 ab = b - a;
        Vector3 ac = c - a;
        Vector3 ad = d - a;
        Vector3 ao = -a;
        
        Vector3 abc = Vector3.Cross(ab, ac);
        Vector3 acd = Vector3.Cross(ac, ad);
        Vector3 adb = Vector3.Cross(ad, ab);

        if (SameDirection(abc, ao))
        {
            simplex = new List<Vector3> { a, b, c };
            return Triangle(ref dir);
        }

        if (SameDirection(acd, ao))
        {
            simplex = new List<Vector3> { a, c, d };
            return Triangle(ref dir);
        }

        if (SameDirection(adb, ao))
        {
            simplex = new List<Vector3> { a, d, b };
            return Triangle(ref dir);
        }

        return true;
    }

#endregion
}