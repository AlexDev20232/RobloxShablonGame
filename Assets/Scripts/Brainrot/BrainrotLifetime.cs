using System;
using UnityEngine;

public class BrainrotLifetime : MonoBehaviour
{
    public BrainrotDefinition definition;
    public BrainrotUI ui;

    private float _remaining;
    private float _nextUiUpdate;
    private bool _running;
    private bool _expired;
    private bool _paused;
    private Action<BrainrotLifetime> _onExpired;

    public void Initialize(BrainrotDefinition def, BrainrotUI uiInstance, float lifetimeSeconds, Action<BrainrotLifetime> onExpired)
    {
        definition = def;
        ui = uiInstance;
        _remaining = Mathf.Max(0.01f, lifetimeSeconds);
        _onExpired = onExpired;
        _running = true;
        _expired = false;
        _nextUiUpdate = 0f;

        if (ui != null)
        {
            ui.SetDefinition(definition);
            ui.SetTimeRemaining(_remaining);
        }
    }

    private void Update()
    {
        if (!_running || _paused)
        {
            return;
        }

        _remaining -= Time.deltaTime;
        if (_remaining <= 0f)
        {
            Expire();
            return;
        }

        if (ui != null && Time.time >= _nextUiUpdate)
        {
            _nextUiUpdate = Time.time + 0.2f;
            ui.SetTimeRemaining(_remaining);
        }
    }

    private void Expire()
    {
        if (_expired)
        {
            return;
        }

        _expired = true;
        _running = false;
        InvokeExpired();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying || _expired || !_running)
        {
            return;
        }

        InvokeExpired();
    }

    private void InvokeExpired()
    {
        if (_onExpired == null)
        {
            return;
        }

        var target = _onExpired.Target as UnityEngine.Object;
        if (target == null && _onExpired.Target != null)
        {
            return;
        }

        _onExpired(this);
    }

    public void SetPaused(bool paused)
    {
        _paused = paused;
    }
}
