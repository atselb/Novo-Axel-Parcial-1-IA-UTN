
using System.Collections.Generic;
using UnityEngine;

public class WolfPackCoordinator : MonoBehaviour
{
    [SerializeField] private bool showDebugPanel = true;

    private readonly List<WolfAgent> wolves = new();

    public int WolfCount => wolves.Count;
    public int AlertCount { get; private set; }

    public void Register(WolfAgent wolf)
    {
        if (wolf != null && !wolves.Contains(wolf))
        {
            wolves.Add(wolf);
        }
    }

    public void Unregister(WolfAgent wolf)
    {
        wolves.Remove(wolf);
    }

    public void BroadcastAlert(
        WolfAgent sender,
        Vector3 hunterPosition)
    {
        AlertCount++;

        int informedWolves = 0;

        foreach (WolfAgent wolf in wolves)
        {
            if (wolf == null || wolf == sender)
            {
                continue;
            }

            wolf.ReceiveAlert(hunterPosition);
            informedWolves++;
        }

        Debug.Log(
            $"[WolfPack] {sender.name} alerted " +
            $"{informedWolves} wolves."
        );
    }

    private void OnGUI()
    {
        if (!showDebugPanel) return;

        float height = 55f + wolves.Count * 23f;

        Rect panel = new Rect(
            10f,
            Screen.height - height - 10f,
            300f,
            height
        );

        GUI.Box(panel, "Wolf Pack");

        GUI.Label(
            new Rect(
                panel.x + 10f,
                panel.y + 23f,
                280f,
                22f
            ),
            $"Wolves: {wolves.Count} | Alerts: {AlertCount}"
        );

        for (int i = 0; i < wolves.Count; i++)
        {
            WolfAgent wolf = wolves[i];
            if (wolf == null) continue;

            GUI.Label(
                new Rect(
                    panel.x + 10f,
                    panel.y + 46f + i * 23f,
                    280f,
                    22f
                ),
                $"{wolf.name}: {wolf.CurrentStateName}"
            );
        }
    }
}
