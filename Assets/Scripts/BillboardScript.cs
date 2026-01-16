using UnityEngine;

public class WorldLockedBillboard : MonoBehaviour
{
    private Transform cam;

    void Start()
    {
        cam = Camera.main.transform;
    }

    void LateUpdate()
    {
        // Otoèí se èelem k hráèi, jen horizontálnì (pro VR menu ideální)
        Vector3 direction = transform.position - cam.position;
        direction.y = 0; // ignoruj výšku, aby se nenaklánìlo
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}