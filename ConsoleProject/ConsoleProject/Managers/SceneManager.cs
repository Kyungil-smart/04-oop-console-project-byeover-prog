using System;
using System.Collections.Generic;

public static class SceneManager
{
    private static readonly Dictionary<string, Scene> _scenes = new(StringComparer.Ordinal);

    public static Scene? Current { get; private set; }

    public static event Action? OnChangeScene;

    private static Scene? _prevScene;
    private static bool _hasPrevScene;

    public static void AddScene(string key, Scene scene)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        if (scene == null) return;

        _scenes[key] = scene;
    }

    public static void AddScene(Scene scene)
    {
        if (scene == null) return;

        _scenes[scene.GetType().Name] = scene;
    }

    public static void Register(string key, Scene scene) => AddScene(key, scene);

    public static void ChangePrevScene()
    {
        if (!_hasPrevScene) return;
        if (_prevScene == null) return;

        Change(_prevScene);
    }

    public static void Change(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        if (!_scenes.TryGetValue(key, out var next)) return;

        Change(next);
    }

    public static void Change(Scene next)
    {
        if (next == null) return;
        if (ReferenceEquals(Current, next)) return;

        if (Current != null)
        {
            _prevScene = Current;
            _hasPrevScene = true;
        }

        Current?.Exit();

        Console.Clear();

        next.Enter();
        Current = next;

        OnChangeScene?.Invoke();
    }

    public static void Update()
    {
        Current?.Update();
    }

    public static void Render()
    {
        Current?.Render();
    }
}
