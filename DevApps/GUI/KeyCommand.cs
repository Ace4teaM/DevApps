namespace DevApps.GUI
{
    public interface IKeyCommand
    {
        void OnKeyCommand(KeyCommand command);
    }

    public enum KeyCommand
    {
        Cancel,
        MoveLeft,
        MoveRight,
        MoveTop,
        MoveBottom,
        ShowDetails,
    }
}
