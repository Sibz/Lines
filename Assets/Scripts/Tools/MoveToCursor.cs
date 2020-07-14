using UnityEngine;

public class MoveToCursor : MonoBehaviour
{
    private Camera   mainCamera;
    private Collider terrainCollider;

    // Start is called before the first frame update
    private void Start()
    {
        mainCamera      = Camera.main;
        terrainCollider = GameObject.FindGameObjectWithTag("Terrain").GetComponent<Collider>();
    }

    // Update is called once per frame
    private void Update()
    {
        var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (terrainCollider.Raycast(ray, out var hit, Mathf.Infinity))
            transform.position = hit.point + Vector3.up * 0.01f;
    }
}