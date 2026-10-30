using UnityEngine;

public class Spawner : MonoBehaviour
{
    [SerializeField]
    private GameObject SpherePrefab;
    [SerializeField]
    private GameObject CapsulePrefab;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            Instantiate(CapsulePrefab, transform);
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            Instantiate(SpherePrefab, transform);
        }
    }
}
