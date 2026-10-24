using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

//Builds the particle effect prefabs used by the game and assigns them to the GameManager in the open scene.
//Run it from the menu: Tools > Create Particle Effects
public static class ParticleEffectCreator
{
    private const string k_Folder = "Assets/Prefabs/Effects";

    [MenuItem("Tools/Create Particle Effects")]
    public static void CreateEffects()
    {
        if (!AssetDatabase.IsValidFolder(k_Folder))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Effects");

        Material material = CreateMaterial();

        //wall: brown/grey debris that falls down
        var wall = CreateEffect("WallDestroyEffect", material,
            new Color(0.55f, 0.4f, 0.25f), new Color(0.35f, 0.35f, 0.35f),
            count: 25, speed: new Vector2(1.5f, 3.5f), size: new Vector2(0.08f, 0.2f),
            lifetime: new Vector2(0.4f, 0.8f), gravity: 1.5f);

        //food: green/yellow sparkles floating up
        var food = CreateEffect("FoodPickupEffect", material,
            new Color(0.4f, 1f, 0.3f), new Color(1f, 0.9f, 0.2f),
            count: 15, speed: new Vector2(0.8f, 2f), size: new Vector2(0.05f, 0.12f),
            lifetime: new Vector2(0.4f, 0.7f), gravity: -0.5f);

        //enemy: red burst
        var enemy = CreateEffect("EnemyDeathEffect", material,
            new Color(0.8f, 0.1f, 0.1f), new Color(0.3f, 0f, 0f),
            count: 35, speed: new Vector2(2f, 4.5f), size: new Vector2(0.1f, 0.25f),
            lifetime: new Vector2(0.5f, 0.9f), gravity: 1f);

        var gameManager = Object.FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            //the fields are private, so assign them through the serialized properties
            var serializedManager = new SerializedObject(gameManager);
            serializedManager.FindProperty("m_WallDestroyEffect").objectReferenceValue = wall;
            serializedManager.FindProperty("m_FoodPickupEffect").objectReferenceValue = food;
            serializedManager.FindProperty("m_EnemyDeathEffect").objectReferenceValue = enemy;
            serializedManager.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(gameManager.gameObject.scene);
            Debug.Log("Particle effects created in " + k_Folder + " and assigned to the GameManager. Save the scene to keep the assignment.");
        }
        else
        {
            Debug.LogWarning("Particle effects created in " + k_Folder + ", but no GameManager was found in the open scene to assign them to.");
        }
    }

    static Material CreateMaterial()
    {
        string path = k_Folder + "/ParticleMaterial.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
            return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        var material = new Material(shader);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    static ParticleSystem CreateEffect(string name, Material material, Color colorA, Color colorB,
        int count, Vector2 speed, Vector2 size, Vector2 lifetime, float gravity)
    {
        var go = new GameObject(name);
        var ps = go.AddComponent<ParticleSystem>();
        //AddComponent starts playing in edit mode, stop so we can change the duration
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.x, lifetime.y);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
        main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
        main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
        main.gravityModifier = gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = count;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.2f;

        //fade out and shrink over the particle lifetime
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        //draw on top of the board and the player
        renderer.sortingOrder = 20;

        string path = k_Folder + "/" + name + ".prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);

        return prefab.GetComponent<ParticleSystem>();
    }
}
