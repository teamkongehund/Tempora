namespace Tempora.Classes.Utility;
public interface IMementoOriginator
{
    public void RestoreMemento(IMemento memento);

    /// <summary>
    /// Create a snapshot of the originator, the state of which should only be visible to the originator.
    /// </summary>
    /// <returns></returns>
    public IMemento GetMemento();
}