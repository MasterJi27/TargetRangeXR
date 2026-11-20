using UnityEngine.InputSystem;

namespace TargetRange.Tests.PlayMode
{
    /// <summary>Batch-mode Unity has no OS keyboard/mouse; give the real action maps deterministic test devices.</summary>
    internal static class TestInputDevices
    {
        internal static void EnsurePresent()
        {
            if (Keyboard.current == null) InputSystem.AddDevice<Keyboard>();
            if (Mouse.current == null) InputSystem.AddDevice<Mouse>();
            #if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        }
    }
}


