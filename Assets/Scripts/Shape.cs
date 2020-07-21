using System.Collections.Generic;
using UnityEngine;

public class Shape : PersistableObject
{
    public int ShapeId
    {
        get { return shapeId; }
        set
        {
            if (shapeId == int.MinValue && value != int.MinValue)
            {
                shapeId = value;
            }
            else
            {
                Debug.LogError("Not allowed to change shapeId");
            }
        }
    }
    public ShapeFactory OriginFactory
    {
        get { return originFactory; }
        set
        {
            if (originFactory == null)
            {
                originFactory = value;
            }
            else
            {
                Debug.LogError("Not allowed to change origin factory");
            }
        }
    }
    public int ColorCount { get { return colors.Length; } }
    public int MaterialId { get; private set; }
    public float Age { get; private set; }
    public int InstanceId { get; private set; }
    public int SaveIndex { get; set; }
    private ShapeFactory originFactory;
    private static int colorPropertyId = Shader.PropertyToID("_Color");
    private static MaterialPropertyBlock sharedPropertyBlock;
    private int shapeId = int.MinValue;
    private Color color;
    private Color[] colors;
    private List<ShapeBehavior> behaviorList = new List<ShapeBehavior>();
    [SerializeField] private MeshRenderer[] meshRenderers = null;

    private void Awake()
    {
        colors = new Color[meshRenderers.Length];
    }

    public void SetMaterial(Material material, int materialId)
    {
        for (int i = 0; i < meshRenderers.Length; ++i)
        {
            meshRenderers[i].material = material;
        }

        MaterialId = materialId;
    }

    public void SetColor(Color color)
    {
        if (sharedPropertyBlock == null)
        {
            sharedPropertyBlock = new MaterialPropertyBlock();
        }

        sharedPropertyBlock.SetColor(colorPropertyId, color);
        
        for (int i = 0; i < meshRenderers.Length; ++i)
        {
            colors[i] = color;
            meshRenderers[i].SetPropertyBlock(sharedPropertyBlock);
        }
    }

    public void SetColor(Color color, int index)
    {
        if (sharedPropertyBlock == null)
        {
            sharedPropertyBlock = new MaterialPropertyBlock();
        }

        sharedPropertyBlock.SetColor(colorPropertyId, color);
        colors[index] = color;
        meshRenderers[index].SetPropertyBlock(sharedPropertyBlock);
    }

   

    public override void Save(GameDataWriter writer)
    {
        base.Save(writer);
        writer.Write(colors.Length);
        
        for (int i = 0; i < colors.Length; ++i)
        {
            writer.Write(colors[i]);
        }

        writer.Write(Age);
        writer.Write(behaviorList.Count);
        
        for (int i = 0; i < behaviorList.Count; ++i)
        {
            writer.Write((int)behaviorList[i].BehaviorType);
            behaviorList[i].Save(writer);
        }
    }

    public override void Load(GameDataReader reader)
    {
        base.Load(reader);

        if (reader.Version >= 5)
        {
            LoadColors(reader);
        }
        else
        {
            SetColor(reader.Version > 0 ? reader.ReadColor() : Color.white);
        }
        
        if (reader.Version >= 6)
        {
            Age = reader.ReadFloat();
            int behaviorCount = reader.ReadInt();

            for (int i = 0; i < behaviorCount; ++i)
            {
                ShapeBehavior behavior = ((ShapeBehaviorType)reader.ReadInt()).GetInstance();
                behaviorList.Add(behavior);
                behavior.Load(reader);
            }
        }
        else if (reader.Version >= 4)
        {
            AddBehavior<RotationShapeBehavior>().AngularVelocity = reader.ReadVector3();
            AddBehavior<MovementShapeBehavior>().Velocity = reader.ReadVector3();
        }
    }

    private void LoadColors(GameDataReader reader)
    {
        int count = reader.ReadInt();
        int max = count <= colors.Length ? count : colors.Length;
        
        //When loading the colors, we must first read the amount that were saved, which might not match the amount of colors that we are currently expecting.
        //The maximum amount of colors that we can safely read and set is equal to either the loaded or current count, whichever is lower.
        //But there may be work left to do after that, so define the iterator variable outside the loop, for later use.
        //That's all that we have to do when both counts end up equal, which should nearly always be the case. But if they're different, then there are two possibilities.
        //The first case is that we have stored more colors than we currently need.
        //This means that there are more colors saved, which we must read, even though we don't use them.
        //The other case is that we have stored less colors than we currently need.
        //We've read all data that was available, but still have colors to set.
        //We cannot ignore them, because then we end up with arbitrary colors.
        //We have to be consistent, so just set the remaining colors to white.
        int i = 0;
        
        for (; i < max; ++i)
        {
            SetColor(reader.ReadColor(), i);
        }

        if (count > colors.Length)
        {
            for (; i < count; ++i)
            {
                reader.ReadColor();
            }
        }
        else if (count < colors.Length)
        {
            for (; i < colors.Length; ++i)
            {
                SetColor(Color.white, i);
            }
        }
    }

    public void GameUpdate()
    {
        Age += Time.deltaTime;

        for (int i = 0; i < behaviorList.Count; ++i)
        {
            if (!behaviorList[i].GameUpdate(this))
            {
                behaviorList[i].Recycle();
                behaviorList.RemoveAt(i--);
            }
        }
    }
    
    public T AddBehavior<T>() where T : ShapeBehavior, new()
    {
        T behavior = ShapeBehaviorPool<T>.Get();
        behaviorList.Add(behavior);

        return behavior;
    }

    public void Recycle()
    {
        Age = 0f;
        InstanceId += 1;

        for (int i = 0; i < behaviorList.Count; ++i)
        {
            behaviorList[i].Recycle();
        }
    
        behaviorList.Clear();
        OriginFactory.Reclaim(this);
    }

    public void ResolveShapeInstances()
    {
        for (int i = 0; i < behaviorList.Count; ++i)
        {
            behaviorList[i].ResolveShapeInstances();
        }
    }
}
