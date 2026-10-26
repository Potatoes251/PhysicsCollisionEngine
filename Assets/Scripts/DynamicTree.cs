using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using UnityEditor;
using UnityEngine;

public class DynamicTree : MonoBehaviour
{

    [SerializeField]
    float margin = .2f;

    List<Collider> colliders;

    List<(int x, int y)> pairs = new();

    List<Node> nodes = new();
    List<int> leafOf = new();
    Stack<int> freeNodes = new();
    int root = -1;

#region MonoBehaviour

    // Collect all colliders and store them in the tree
    void Start()
    {
        colliders = new(FindObjectsByType<Collider>(FindObjectsSortMode.None));

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

            if (!Contains(n.bounds, colliders[i].bounds))
            {
                RemoveLeaf(leaf);
                leafOf[i] = InsertNode(colliders[i], i);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!EditorApplication.isPlaying) return;
        foreach (Node node in nodes)
        {
            Gizmos.DrawWireCube(node.bounds.center, node.bounds.size);
        }

        GetPairs();

        foreach ((int x, int y) in pairs)
        {
            Gizmos.DrawLine(colliders[x].bounds.center, colliders[y].bounds.center);
        }
    }

#endregion

#region Bounds

    Bounds AddBounds(Bounds bound1, Bounds bound2)
    {
        bound1.Encapsulate(bound2);
        return bound1;
    }

    Bounds Fatten(Bounds bounds)
    {
        bounds.Expand(margin * 2);
        return bounds;
    }

    private float Area(Bounds bounds)
    {
        Vector3 size = bounds.size;
        return (size.x * size.y + size.y * size.z + size.z * size.x);
    }

    bool Contains(Bounds large, Bounds tight)
    {
        return large.Contains(tight.max) && large.Contains(tight.min);
    }

    bool Collide(Bounds b1, Bounds b2)
    {
        Vector3 min1 = b1.min;
        Vector3 max1 = b1.max;
        Vector3 min2 = b2.min;
        Vector3 max2 = b2.max;

        return !(min1.x > max2.x || min1.y > max2.y || min1.z > max2.z ||
                min2.x > max1.x || min2.y > max1.y || min2.z > max1.z);
    }

#endregion

#region Node
    struct Node
    {
        public Bounds bounds;
        public int parent;
        public int childL;
        public int childR;
        public int colliderId;

        public Node(Bounds bounds, int colliderId = -1, int parent = -1, int childL = -1, int childR = -1)
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

    int InsertNode(Collider newCollider, int colliderId)
    {
        Bounds fattened = Fatten(newCollider.bounds);
        if (root == -1)
        {
            root = Allocate(new Node(fattened, colliderId));
            return root;
        }

        int sibling = root;
        while (!nodes[sibling].IsLeaf())
        {
            Node n = nodes[sibling];
            float costL = Area(AddBounds(nodes[n.childL].bounds, fattened)) - Area(nodes[n.childL].bounds);
            float costR = Area(AddBounds(nodes[n.childR].bounds, fattened)) - Area(nodes[n.childR].bounds);

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

        if (!Collide(nodeL.bounds, nodeR.bounds)) return;

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

}
