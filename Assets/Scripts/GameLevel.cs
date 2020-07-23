using UnityEngine;

public partial class GameLevel : PersistableObject
{
    [SerializeField] private SpawnZone spawnZone = null;
    [UnityEngine.Serialization.FormerlySerializedAs("persistentObjects")]
    [SerializeField]
    private GameLevelObject[] levelObjects;
    [SerializeField] private int populationLimit = 100;

    public static GameLevel Current { get; private set; }
    public int PopulationLimit { get { return populationLimit; } }

    public void GameUpdate()
    {
        for (int i = 0; i < levelObjects.Length; ++i)
        {
            levelObjects[i].GameUpdate();
        }
    }

    public void SpawnShapes()
    {
        spawnZone.SpawnShapes();
    }

    public override void Save(GameDataWriter writer)
    {
        writer.Write(levelObjects.Length);

        for (int i = 0; i < levelObjects.Length; ++i)
        {
            levelObjects[i].Save(writer);
        }
    }

    public override void Load(GameDataReader reader)
    {
        int savedCount = reader.ReadInt();

        for (int i = 0; i < savedCount; ++i)
        {
            levelObjects[i].Load(reader);
        }
    }

    private void OnEnable()
    {
        Current = this;

        if (levelObjects == null)
        {
            levelObjects = new GameLevelObject[0];
        }
    }

    
}
