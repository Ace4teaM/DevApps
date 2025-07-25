using System.Windows.Input;

namespace DevApps.GUI
{
    /// <summary>
    /// Interface permettant de recevoir les commandes claviers reçues par l'application aux vues enfants de DesignerWindow
    /// </summary>
    public interface IKeyCommand
    {
        void OnKeyCommand(KeyCommand command);
        void OnKeyState(ModifierKeys modifier);
    }

    /// <summary>
    /// Commandes possibles
    /// </summary>
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
