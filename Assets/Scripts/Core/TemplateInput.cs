using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public static class TemplateInput
{
    public static bool IsKeyPressed(KeyCode key)
    {
#if ENABLE_INPUT_SYSTEM
        if (TryReadInputSystemKey(key, out bool isPressed))
        {
            return isPressed;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(key);
#else
        return false;
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private static bool TryReadInputSystemKey(KeyCode key, out bool isPressed)
    {
        isPressed = false;
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        switch (key)
        {
            case KeyCode.E:
                isPressed = keyboard.eKey.isPressed;
                return true;
            case KeyCode.Space:
                isPressed = keyboard.spaceKey.isPressed;
                return true;
            case KeyCode.Return:
            case KeyCode.KeypadEnter:
                isPressed = keyboard.enterKey.isPressed;
                return true;
            case KeyCode.Escape:
                isPressed = keyboard.escapeKey.isPressed;
                return true;
            case KeyCode.Tab:
                isPressed = keyboard.tabKey.isPressed;
                return true;
            default:
                return false;
        }
    }
#endif
}
