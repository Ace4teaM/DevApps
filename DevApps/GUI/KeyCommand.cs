using System.Windows.Input;

namespace DevApps.GUI
{
    /// <summary>
    /// Interface permettant de recevoir les commandes de l'application dans les vues enfants du DesignerWindow
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
