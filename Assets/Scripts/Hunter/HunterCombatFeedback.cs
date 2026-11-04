
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class HunterCombatFeedback : MonoBehaviour
{
    [Header("Pursuit Prediction")]
    [SerializeField, Min(0.1f)]
    private float maxPredictionTime = 1.5f;
    [SerializeField] private bool drawPredictionGizmo = true;

    [Header("Attack Visuals")]
    [SerializeField] private Color rangedColor =
        new Color(1f, 0.92f, 0.1f);
    [SerializeField] private Color meleeColor =
        new Color(1f, 0.3f, 0.1f);
    [SerializeField, Min(0.02f)]
    private float traceDuration = 0.2f;
    [SerializeField, Min(0.01f)]
    private float rangedWidth = 0.16f;
    [SerializeField, Min(0.01f)]
    private float meleeWidth = 0.3f;

    private LineRenderer trace;
    private Material traceMaterial;
    private Coroutine hideRoutine;
    private bool hasPrediction;
    private Vector3 predictedPosition;

    public float MaxPredictionTime => maxPredictionTime;

    private void Awake()
    {
        GameObject traceObject = new GameObject("AttackTrace (Runtime)");
        traceObject.transform.SetParent(transform, false);

        trace = traceObject.AddComponent<LineRenderer>();
        trace.useWorldSpace = true;
        trace.positionCount = 2;
        trace.alignment = LineAlignment.View;
        trace.numCapVertices = 4;
        trace.shadowCastingMode = ShadowCastingMode.Off;
        trace.receiveShadows = false;
        trace.enabled = false;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        if (shader != null)
        {
            traceMaterial = new Material(shader);
            trace.sharedMaterial = traceMaterial;
        }
        else
        {
            Debug.LogWarning(
                "Hunter Combat Feedback: no Unlit shader found.", this
            );
        }
    }

    public void SetPredictedPosition(Vector3 position)
    {
        predictedPosition = position;
        hasPrediction = true;
    }

    public void ClearPrediction()
    {
        hasPrediction = false;
    }

    public void PlayAttack(Vector3 origin, Vector3 target, bool melee)
    {
        if (trace == null) return;

        Color color = melee ? meleeColor : rangedColor;
        float width = melee ? meleeWidth : rangedWidth;

        if (traceMaterial != null)
        {
            if (traceMaterial.HasProperty("_BaseColor"))
                traceMaterial.SetColor("_BaseColor", color);
            if (traceMaterial.HasProperty("_Color"))
                traceMaterial.SetColor("_Color", color);
        }

        trace.startColor = color;
        trace.endColor = color;
        trace.startWidth = width;
        trace.endWidth = width * 0.4f;
        trace.SetPosition(0, origin + Vector3.up * 0.9f);
        trace.SetPosition(1, target + Vector3.up * 0.9f);
        trace.enabled = true;

        if (hideRoutine != null) StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(traceDuration);
        if (trace != null) trace.enabled = false;
        hideRoutine = null;
    }

    private void OnDestroy()
    {
        if (traceMaterial != null) Destroy(traceMaterial);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawPredictionGizmo || !hasPrediction) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(predictedPosition, 0.5f);
        Gizmos.DrawLine(
            transform.position + Vector3.up * 0.5f,
            predictedPosition + Vector3.up * 0.5f
        );
    }

    private void OnValidate()
    {
        maxPredictionTime = Mathf.Max(0.1f, maxPredictionTime);
        traceDuration = Mathf.Max(0.02f, traceDuration);
        rangedWidth = Mathf.Max(0.01f, rangedWidth);
        meleeWidth = Mathf.Max(0.01f, meleeWidth);
    }
}
