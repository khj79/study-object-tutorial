using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Game : PersistableObject
{
    [SerializeField] private  PersistentStorage storage = null;
    [SerializeField] private  int levelCount = 0;
    [SerializeField] private  KeyCode createKey = KeyCode.C;
    [SerializeField] private  KeyCode newGameKey = KeyCode.N;
    [SerializeField] private  KeyCode saveKey = KeyCode.S;
    [SerializeField] private  KeyCode loadKey = KeyCode.L;
    [SerializeField] private  KeyCode destroyKey = KeyCode.X;
    [SerializeField] private ShapeFactory shapeFactory = null;
    [SerializeField] private bool reseedOnLoad = false;
    [SerializeField] private Slider creationSpeedSlider = null;
    [SerializeField] private Slider destructionSpeedSlider = null;
    
    private const int saveVersion = 3;
    private List<Shape> shapes;
    private float creationProgress;
    private float destructionProgress;
    private int loadedLevelBuildIndex;
    private Random.State mainRandomState;

    public float CreationSpeed { get; set; }
    public float DestructionSpeed { get; set; }


    private IEnumerator LoadLevel(int levelBuildIndex)
    {
        enabled = false;

        if (loadedLevelBuildIndex > 0)
        {
            yield return SceneManager.UnloadSceneAsync(loadedLevelBuildIndex);
        }

        yield return SceneManager.LoadSceneAsync(levelBuildIndex, LoadSceneMode.Additive);

        SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(levelBuildIndex));
        loadedLevelBuildIndex = levelBuildIndex;
        enabled = true;
    }
    
    private void BeginNewGame()
    {
        Random.state = mainRandomState;
        //To make the seeds a little more unpredictable,
        //we'll mix them with the current play time, accessible via Time.unscaledTime.
        //The bitwise exclusive-OR operator ^ is good for this.
        int seed = Random.Range(0, int.MaxValue) ^ (int)Time.unscaledTime;
        mainRandomState = Random.state;
        Random.InitState(seed);

        CreationSpeed = 0;
        creationSpeedSlider.value = 0;
        DestructionSpeed = 0;
        destructionSpeedSlider.value = 0;
        
        for (int i = 0; i < shapes.Count; ++i)
        {
            shapeFactory.Reclaim(shapes[i]);
        }

        shapes.Clear();
    }

    private void CreateShape()
    {
        Shape instance = shapeFactory.GetRandom();

        Transform t = instance.transform;
        t.localPosition = GameLevel.Current.SpawnPoint;
        t.localRotation = Random.rotation;
        t.localScale = Vector3.one * Random.Range(0.1f, 1f);
        instance.SetColor(Random.ColorHSV
        (
            hueMin: 0f, hueMax: 1f,
            saturationMin: 0.5f, saturationMax: 1f,
            valueMin: 0.25f, valueMax: 1f,
            alphaMin: 1f, alphaMax: 1f
        ));
        
        shapes.Add(instance);
    }

    private void DestroyShape()
    {
        if (shapes.Count > 0)
        {
            int index = Random.Range(0, shapes.Count);
            shapeFactory.Reclaim(shapes[index]);
            int lastIndex = shapes.Count -1;
            shapes[index] = shapes[lastIndex];
            shapes.RemoveAt(lastIndex);
        }
    }

    private void Start()
    {
        mainRandomState = Random.state;
        shapes = new List<Shape>();
        
        if (Application.isEditor)
        {
            for (int i = 0; i < SceneManager.sceneCount; ++i)
            {
                Scene loadedScene = SceneManager.GetSceneAt(i);
                if (loadedScene.name.Contains("Level "))
                {
                    SceneManager.SetActiveScene(loadedScene);
                    loadedLevelBuildIndex = loadedScene.buildIndex;

                    return;
                }
            }
        }        

        BeginNewGame();
        StartCoroutine(LoadLevel(1));
    }

    private void Update()
    {
        if (Input.GetKeyDown(createKey))
        {
            CreateShape();
        }
        else if (Input.GetKeyDown(destroyKey))
        {
            DestroyShape();
        }
        else if (Input.GetKeyDown(newGameKey))
        {
            BeginNewGame();
            StartCoroutine(LoadLevel(loadedLevelBuildIndex));
        }
        else if (Input.GetKeyDown(saveKey))
        {
            storage.Save(this, saveVersion);
        }
        else if (Input.GetKeyDown(loadKey))
        {
            BeginNewGame();
            storage.Load(this);
        }
        else
        {
            for (int i = 1; i <= levelCount; ++i)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i))
                {
                    BeginNewGame();
                    StartCoroutine(LoadLevel(i));
                    
                    return;
                }
            }
        }   
    }

    private void FixedUpdate()
    {
        creationProgress += Time.deltaTime * CreationSpeed;
        
        while (creationProgress >= 1f)
        {
            creationProgress -= 1f;
            CreateShape();
        }

        destructionProgress += Time.deltaTime * DestructionSpeed;

        while (destructionProgress >= 1f)
        {
            destructionProgress -= 1f;
            DestroyShape();
        }
    }

    public override void Save(GameDataWriter writer)
    {
        writer.Write(shapes.Count);
        writer.Write(Random.state);
        writer.Write(CreationSpeed);
        writer.Write(creationProgress);
        writer.Write(DestructionSpeed);
        writer.Write(destructionProgress);
        writer.Write(loadedLevelBuildIndex);
        GameLevel.Current.Save(writer);

        for (int i = 0; i < shapes.Count; ++i)
        {
            writer.Write(shapes[i].ShapeId);
            writer.Write(shapes[i].MaterialId);
            shapes[i].Save(writer);
        }
    }

    public override void Load(GameDataReader reader)
    {
        int version = reader.Version;

        if (version > saveVersion)
        {
            Debug.LogError("Unsuppoorted future save version " + version);
            return;
        }

        StartCoroutine(LoadGame(reader));
    }

    IEnumerator LoadGame(GameDataReader reader)
    {
        int version = reader.Version;
        int count = version <= 0 ? -version : reader.ReadInt();

        if (version >= 3)
        {
            //Note : save 파일에서 가져온 랜덤 시드를 재활용 
            Random.State state = reader.ReadRandomState();

            if (!reseedOnLoad)
            {
                Random.state = state;
            }

            CreationSpeed = reader.ReadFloat();
            creationProgress = reader.ReadFloat();
            DestructionSpeed = reader.ReadFloat();
            destructionProgress = reader.ReadFloat();

            creationSpeedSlider.value = CreationSpeed;
            destructionSpeedSlider.value = DestructionSpeed;
        }

        yield return LoadLevel(version < 2 ? 1 : reader.ReadInt());

        if (version >= 3)
        {
            GameLevel.Current.Load(reader);
        }

        for (int i = 0; i < count; ++i)
        {
            int shapeId = version > 0 ? reader.ReadInt() : 0;
            int materialId = version > 0 ? reader.ReadInt() : 0;
            Shape instance = shapeFactory.Get(shapeId, materialId);
            instance.Load(reader);
            shapes.Add(instance);
        }
    }
}
