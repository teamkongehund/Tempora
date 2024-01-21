using Godot;
using System;
using System.Collections.Generic;

namespace OsuTimer.Classes.Utility;

public partial class ActionsHandler : Node
{
    public static ActionsHandler Instance = null!;

    private List<IMemento> mementoList = [];

    private int? mementoIndex = null;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        Instance = this;
	}

    public override void _Input(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            case InputEventKey { Keycode: Key.Z, Pressed: true} keyEvent:
                {
                    if (Input.IsKeyPressed(Key.Ctrl))
                    {
                        Undo();
                    }
                    break;
                }
            case InputEventKey { Keycode: Key.Y, Pressed: true } keyEvent:
                {
                    if (Input.IsKeyPressed(Key.Ctrl))
                    {
                        Redo();
                    }
                    break;
                }
            case InputEventKey { Keycode: Key.L, Pressed: true } keyEvent:
                {
                    AddTimingMemento();
                    Project.Instance.NotificationMessage = $"State saved with mementoIndex = {mementoIndex} / {mementoList.Count - 1}.";
                    break;
                }
        }
    }

    private void AddAudioFileMemento()
    {

    }

    public void AddTimingMemento()
    {
        IMemento memento = Timing.Instance.GetMemento();
        AddMemento(memento);
    }

    private void AddMemento(IMemento memento)
    {
        if (mementoIndex != null)
        {
            DeleteMementosAfterIndex((int)mementoIndex);
            mementoIndex = null;
        }
        mementoList.Add(memento);
    }

    private void DeleteMementosAfterIndex(int index)
    {
        //Project.Instance.NotificationMessage = $"Deleting all mementos after index {index}";
        mementoList.RemoveRange(index, mementoList.Count - index);
    }

    private void Undo()
    {
        switch (mementoIndex)
        {
            case null:
                mementoIndex = mementoList.Count - 2 >= 0 ? mementoList.Count - 2 : 0;
                break;
            case > 0:
                mementoIndex--;
                break;
            case 0:
                return;
            default:
                throw new Exception($"Memento index could not be handled");
        }
        //Project.Instance.NotificationMessage = $"Undo: mementoIndex = {mementoIndex ?? (mementoList.Count - 1)} / {mementoList.Count - 1}";

        IMemento memento = mementoList[(int)mementoIndex];
        if (memento == null)
            throw new NullReferenceException($"{nameof(memento)}");

        Timing.Instance.RestoreMemento(memento);
    }

    private void Redo()
    {
        switch (mementoIndex)
        {
            case null:
                return;
            case var expression when (mementoIndex < (mementoList.Count - 1)):
                mementoIndex++;
                break;
            case var expression when (mementoIndex == mementoList.Count - 1):
                mementoIndex = null;
                return;
            default:
                throw new Exception($"Memento index could not be handled");
        }
        //Project.Instance.NotificationMessage = $"Redo: mementoIndex = {mementoIndex} / {mementoList.Count-1}";

        IMemento memento = mementoList[(int)mementoIndex];

        Timing.Instance.RestoreMemento(memento);
    }
}
