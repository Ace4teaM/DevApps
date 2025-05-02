using System.Windows.Input;

namespace DevApps.GUI
{
    public interface IKeyCommand
    {
        void OnKeyCommand(KeyCommand command);
        void OnKeyState(ModifierKeys modifier);
    }

    public enum KeyCommand
    {
        Cancel,
        MoveLeft,
        MoveRight,
        MoveTop,
        MoveBottom,
        Create,
        Delete,
    }
}
