using UnityEngine;

public class GameLevel : MonoBehaviour
{
    [SerializeField] private SpawnZone spawnZone = null;

    private void Start()
    {
        Game.Instance.spawnZoneOfLevel = spawnZone;
    }
}
