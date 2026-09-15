using UnityEngine;

public class HunterDebugDisplay : MonoBehaviour
{
    [SerializeField] private HunterAgent hunter;

    private void OnGUI()
    {
        if (hunter == null) return;

        GUILayout.BeginArea(
            new Rect(10f, 10f, 350f, 200f)
        );

        GUILayout.Label(
            $"Hunter State: {hunter.CurrentStateName}"
        );

        GUILayout.Label(
            $"Target: {hunter.CurrentTargetName}"
        );

        if (hunter.Perception != null)
        {
            GUILayout.Label(
                $"Alive detected: " +
                $"{hunter.Perception.PerceivedAliveBoids.Count}"
            );

            GUILayout.Label(
                $"Eliminated detected: " +
                $"{hunter.Perception.PerceivedEliminatedBoids.Count}"
            );
        }

        GUILayout.Label(
            $"Attack Ready: {hunter.IsAttackReady}"
        );

        GUILayout.EndArea();
    }
}