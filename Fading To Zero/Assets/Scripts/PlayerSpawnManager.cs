using UnityEngine;

public class PlayerSpawnManager : MonoBehaviour
{
    void Start()
    {
        // Saat scene baru ke-load, cek apakah ada data spawn point dari pintu sebelumnya
        if (!string.IsNullOrEmpty(DoorInteraction.nextSpawnPoint))
        {
            // Cari lokasi titik spawn berdasarkan nama
            GameObject spawnPoint = GameObject.Find(DoorInteraction.nextSpawnPoint);

            if (spawnPoint != null)
            {
                // Pindahkan Player ke lokasi tersebut
                transform.position = spawnPoint.transform.position;
            }
        }
    }
}