using System.Collections.Generic;
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


    List<Collider> colliders;

    List<Node> nodes = new();

    // Collect all colliders and store them in the tree
    void Start()
    {
        colliders = new(FindObjectsByType<Collider>(FindObjectsSortMode.None));

        for (int i = 0; i < colliders.Count; i++)
        {
            InsertNode(colliders[i], i);
        }
    }

    void InsertNode(Collider newCollider, int colliderId)
    {
        if (nodes.Count == 0)
        {
            nodes.Add(new(newCollider.bounds, colliderId));
            return;
        }

        int parent = 0;
        while (!nodes[parent].IsLeaf())
        {
            Node parentNode = nodes[parent];
            float combinedAreaL = Area(AddBounds(nodes[parentNode.childL].bounds, newCollider.bounds));
            float combinedAreaR = Area(AddBounds(nodes[parentNode.childR].bounds, newCollider.bounds));

            parent = combinedAreaL > combinedAreaR ? parentNode.childR : parentNode.childL;
        }

        Bounds newBounds = AddBounds(nodes[parent].bounds, newCollider.bounds);

        Node movedLeaf = nodes[parent];
        movedLeaf.parent = parent;
        
        Node newNode = new(newCollider.bounds, colliderId, parent);

        int grandParent = nodes[parent].parent;
        nodes[parent] = new(newBounds, -1, grandParent, nodes.Count, nodes.Count + 1);
        nodes.Add(movedLeaf);
        nodes.Add(newNode);

        UpdateParentBounds(nodes[parent].parent);
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
