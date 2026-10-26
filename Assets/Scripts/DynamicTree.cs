using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using TreeEditor;
using UnityEngine;

public class DynamicTree : MonoBehaviour
{

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

    Bounds AddBounds(Bounds bound1, Bounds bound2)
    {
        bound1.Encapsulate(bound2);
        return bound1;
    }

    private float Area(Bounds bounds)
    {
        Vector3 size = bounds.size;
        return (size.x * size.y + size.y * size.z + size.z * size.x);
    }

    [SerializeField]
    float margin = .2f;

    List<Collider> colliders;

    List<Node> nodes = new();
    Stack<int> freeNodes = new();
    int root = -1;

    // Collect all colliders and store them in the tree
    void Start()
    {
        colliders = new(FindObjectsByType<Collider>(FindObjectsSortMode.None));

        for (int i = 0; i < colliders.Count; i++)
        {
            InsertNode(colliders[i], i);
        }
    }

    Bounds Fatten(Bounds bounds)
    {
        bounds.Expand(margin);
        return bounds;
    }

    void InsertNode(Collider newCollider, int colliderId)
    {
        Bounds fattened = Fatten(newCollider.bounds);
        if (root == -1)
        {
            root = nodes.Count;
            nodes.Add(new(fattened, colliderId));
            return;
        }

        int parent = root;
        while (!nodes[parent].IsLeaf())
        {
            Node n = nodes[parent];
            float costL = Area(AddBounds(nodes[n.childL].bounds, fattened)) - Area(nodes[n.childL].bounds);
            float costR = Area(AddBounds(nodes[n.childR].bounds, fattened)) - Area(nodes[n.childR].bounds);

            parent = costL > costR ? n.childR : n.childL;
        }

        Bounds newBounds = AddBounds(nodes[parent].bounds, fattened);

        Node sibling = nodes[parent];
        int grandparent = sibling.parent;

        sibling.parent = nodes.Count;
        nodes[parent] = sibling;
        
        if (grandparent == -1) root = nodes.Count;
        else
        {
            Node g = nodes[grandparent];
            if (g.childL == parent) g.childL = nodes.Count;
            else g.childR = nodes.Count;
            nodes[grandparent] = g;
        }
        Node newNode = new(fattened, colliderId, nodes.Count);

        nodes.Add(new(newBounds, -1, grandparent, parent, nodes.Count + 1));  // new parent

        nodes.Add(newNode); // new sibling

        UpdateParentBounds(nodes[parent].parent);
    }

    private void RemoveLeaf(int leaf)
    {
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

    private void OnDrawGizmos()
    {
        foreach (Node node in nodes)
        {
            Gizmos.DrawWireCube(node.bounds.center, node.bounds.size);
        }
    }
}
