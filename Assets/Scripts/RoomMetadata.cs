using UnityEngine;

[DisallowMultipleComponent]
public class RoomMetadata : MonoBehaviour
{
    [Tooltip("Optional room name. If empty, GameObject name will be used.")]
    public string roomName;
}
