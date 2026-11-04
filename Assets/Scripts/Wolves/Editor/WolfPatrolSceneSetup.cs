
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WolfPatrolSceneSetup
{
    private static readonly Vector2[,] Routes =
    {
        // Wolf_01 - Oeste
        {
            new Vector2(-19f, -4f),
            new Vector2(-16f, -4f),
            new Vector2(-16f, -9f),
            new Vector2(-19f, -9f)
        },

        // Wolf_02 - Norte
        {
            new Vector2(-2f, 16f),
            new Vector2(2f, 16f),
            new Vector2(2f, 19f),
            new Vector2(-2f, 19f)
        },

        // Wolf_03 - Este
        {
            new Vector2(17f, 8f),
            new Vector2(21f, 8f),
            new Vector2(21f, 3f),
            new Vector2(17f, 3f)
        },

        // Wolf_04 - Sur
        {
            new Vector2(-7f, -16f),
            new Vector2(7f, -16f),
            new Vector2(7f, -20f),
            new Vector2(-7f, -20f)
        }
    };

    [MenuItem("Tools/IA/Configurar patrullas de lobos")]
    public static void Configure()
    {
        // Run only while editing the scene.
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError(
                "Salí de Play Mode antes de configurar patrullas."
            );
            return;
        }

        GameObject wolvesRoot = GameObject.Find("Wolves");
        GameObject obstacles =
            GameObject.Find("Environment/Obstacles");

        if (wolvesRoot == null || obstacles == null)
        {
            Debug.LogError(
                "No se encontró Wolves o Environment/Obstacles " +
                "en la escena actual."
            );
            return;
        }

        Transform[] wolves = new Transform[4];
        WolfAgent[] agents = new WolfAgent[4];

        // Validate references before changing anything.
        for (int i = 0; i < 4; i++)
        {
            string wolfName = $"Wolf_{i + 1:00}";

            wolves[i] =
                wolvesRoot.transform.Find(wolfName);

            if (wolves[i] == null)
            {
                Debug.LogError(
                    $"No se encontró {wolfName} en Wolves."
                );
                return;
            }

            agents[i] =
                wolves[i].GetComponent<WolfAgent>();

            if (agents[i] == null)
            {
                Debug.LogError(
                    $"{wolfName} no tiene WolfAgent."
                );
                return;
            }

            SerializedObject check =
                new SerializedObject(agents[i]);

            if (check.FindProperty("patrolWaypoints") == null ||
                check.FindProperty("obstaclesRoot") == null)
            {
                Debug.LogError(
                    "Reemplazá WolfAgent.cs antes de ejecutar " +
                    "la configuración."
                );
                return;
            }
        }

        Transform routesRoot = GetOrCreate(
            wolvesRoot.transform,
            "PatrolRoutes"
        );

        for (int i = 0; i < 4; i++)
        {
            string wolfName = $"Wolf_{i + 1:00}";

            Transform routeRoot = GetOrCreate(
                routesRoot,
                $"{wolfName}_Route"
            );

            Transform[] waypoints = new Transform[4];

            for (int j = 0; j < 4; j++)
            {
                Transform waypoint = GetOrCreate(
                    routeRoot,
                    $"WP_{j:00}"
                );

                Vector2 point = Routes[i, j];

                Undo.RecordObject(
                    waypoint,
                    "Configure patrol waypoint"
                );

                waypoint.position =
                    new Vector3(point.x, 0f, point.y);

                waypoints[j] = waypoint;
            }

            // Place the wolf at the start of its route.
            Vector2 start = Routes[i, 0];

            Undo.RecordObject(
                wolves[i],
                "Position patrol wolf"
            );

            wolves[i].position =
                new Vector3(start.x, 1f, start.y);

            // Configure WolfAgent in the Inspector.
            SerializedObject serializedWolf =
                new SerializedObject(agents[i]);

            serializedWolf.Update();

            SerializedProperty patrolProperty =
                serializedWolf.FindProperty("patrolWaypoints");

            patrolProperty.arraySize = 4;

            for (int j = 0; j < 4; j++)
            {
                patrolProperty
                    .GetArrayElementAtIndex(j)
                    .objectReferenceValue = waypoints[j];
            }

            serializedWolf
                .FindProperty("obstaclesRoot")
                .objectReferenceValue =
                    obstacles.transform;

            serializedWolf.ApplyModifiedProperties();

            EditorUtility.SetDirty(agents[i]);

            Debug.Log(
                $"[Patrol Setup] {wolfName}: " +
                "4 waypoints y referencias asignados."
            );
        }

        EditorSceneManager.MarkSceneDirty(
            EditorSceneManager.GetActiveScene()
        );

        Debug.Log(
            "[Patrol Setup] COMPLETED: " +
            "4 lobos y 16 waypoints configurados."
        );

        EditorUtility.DisplayDialog(
            "Configuración terminada",
            "Se crearon o actualizaron 16 waypoints " +
            "y se configuraron los cuatro lobos.\n" +
            "Guardá MainScene y probá en Play.",
            "Aceptar"
        );
    }

    private static Transform GetOrCreate(
        Transform parent,
        string objectName)
    {
        Transform existing = parent.Find(objectName);

        if (existing != null)
        {
            return existing;
        }

        GameObject created =
            new GameObject(objectName);

        Undo.RegisterCreatedObjectUndo(
            created,
            "Create patrol route"
        );

        created.transform.SetParent(parent, false);

        return created.transform;
    }
}
#endif
