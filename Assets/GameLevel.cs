using UnityEngine;

public class GameLevel : MonoBehaviour
{
    [SerializeField] private SpawnZone spawnZone;

    private void Start()
    {
        Game.Instance.spawnZoneOfLevel = spawnZone;
    }
}
