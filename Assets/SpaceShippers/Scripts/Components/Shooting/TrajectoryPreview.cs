using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
public class TrajectoryPreview : MonoBehaviour
{
    // Channels allow multiple IAimSource scripts to register with unique lines
    private class Channel
    {
        public IAimSource Source;
        public LineRenderer[] Lines;
        public float LineLength;
        public bool Visible;
    }
    private readonly Dictionary<IAimSource, Channel> _channels = new();
    private int _visibleCount; // Allows update can skip when nothing visible

    public void Register(IAimSource source, TrajectoryStyle style)
    {
        if (source == null || style == null) return;

        Transform[] muzzles = source.Muzzles;
        var channel = new Channel
        {
            Source = source,
            Lines = new LineRenderer[muzzles.Length],
            LineLength = style.Length,
        };

        // Fill the channel's Lines[] using style specifications
        for (int i = 0; i < muzzles.Length; i++)
        {
            if (muzzles[i] == null) continue;

            var lineObject = new GameObject($"Trajectory Preview {i + 1}");
            lineObject.transform.SetParent(source.Ship, false);

            var line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.startWidth = line.endWidth = style.Width;
            line.sharedMaterial = style.Material;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            channel.Lines[i] = line;
        }

        _channels[source] = channel;
    }

    public void Unregister(IAimSource source)
    {
        if (source == null || !_channels.TryGetValue(source, out var channel)) return;

        if (channel.Visible) 
            _visibleCount--;

        foreach (var line in channel.Lines)
            if (line != null) Destroy(line.gameObject);

        _channels.Remove(source);
    }

    public void SetVisible(IAimSource source, bool visible)
    {
        if (source == null || !_channels.TryGetValue(source, out var channel)) return;
        if (channel.Visible == visible) return;

        channel.Visible = visible;
        _visibleCount += visible ? 1 : -1;

        foreach (var line in channel.Lines)
            if (line != null) line.enabled = visible;
    }

    public void Update()
    {
        if (_visibleCount == 0) return;

        foreach (var channel in _channels.Values)
        {
            if (!channel.Visible) continue;

            Transform ship = channel.Source.Ship;
            Transform[] muzzles = channel.Source.Muzzles;
            LineRenderer[] lines = channel.Lines;

            for (int i = 0; i < lines.Length; i++)
            {
                if (muzzles[i] == null || lines[i] == null) continue;

                Quaternion rotation = channel.Source.GetAimRotation(muzzles[i]);
                Vector3 start = ship.InverseTransformPoint(muzzles[i].position);
                Vector3 dir = ship.InverseTransformDirection(rotation * Vector3.forward);

                lines[i].SetPosition(0, start);                            // Start
                lines[i].SetPosition(1, start + dir * channel.LineLength); // End
            }
        }
    }
}
