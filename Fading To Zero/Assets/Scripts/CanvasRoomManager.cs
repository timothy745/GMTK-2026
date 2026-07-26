using UnityEngine;

public class CanvasRoomManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject momNPC;
    [SerializeField] private GameObject exitDoor;

    [Header("Dialog Settings")]
    [SerializeField] private string momSpeakerName = "Ibu";
    [SerializeField] private string[] momDialogLines = new string[]
    {
        "Anak ibu sudah pulang.",
        "Foto-foto itu sudah lengkap.",
        "Sekarang kita bisa keluar."
    };

    private bool momSpawned = false;
    private bool momDialogDone = false;

    void Start()
    {
        if (momNPC != null) momNPC.SetActive(false);
        if (exitDoor != null) exitDoor.SetActive(false);
    }

    void Update()
    {
        if (!momSpawned && InventoryUI.GetPhotoPieceCount() >= 3)
        {
            SpawnMomNPC();
        }

        if (momSpawned && !momDialogDone && momNPC == null)
        {
            momDialogDone = true;
            if (exitDoor != null) exitDoor.SetActive(true);
        }
    }

    void SpawnMomNPC()
    {
        momSpawned = true;

        if (momNPC == null) return;

        momNPC.SetActive(true);

        TriggerZoneDialog tzd = momNPC.GetComponent<TriggerZoneDialog>();
        if (tzd != null)
        {
            tzd.speakerName = momSpeakerName;
            tzd.dialogLines = momDialogLines;
            tzd.triggerOnce = true;
            tzd.autoStartDialog = true;
        }
        else
        {
            momDialogDone = true;
            if (exitDoor != null) exitDoor.SetActive(true);
        }
    }
}
