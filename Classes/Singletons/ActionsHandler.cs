using System;
using System.Collections.Generic;
using Godot;
using GD = Tempora.Classes.DataTools.GD;

namespace Tempora.Classes.Utility;

public partial class ActionsHandler : Node
{
    private static ActionsHandler instance = null!;
    public static ActionsHandler Instance { get => instance; set => instance = value; }

    private List<IMemento> mementoList = [];

    private int mementoIndex = 0;


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Instance = this;
    }

    public override void _Input(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            case InputEventKey { Keycode: Key.Z, Pressed: true } keyEvent:
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
                //case InputEventKey { Keycode: Key.L, Pressed: true } keyEvent:
                //    {
                //        AddTimingMemento();
                //        Project.Instance.NotificationMessage = $"State saved with mementoIndex = {mementoIndex} / {mementoList.Count - 1}.";
                //        break;
                //    }
        }
    }

    //private void AddAudioFileMemento()
    //{

    //}

    public void AddTimingMemento()
    {
        IMemento memento = Timing.Instance.GetMemento();
        AddMemento(memento);
    }

    private void AddMemento(IMemento memento)
    {
        //if (mementoList.Count > 0)
        //    GD.Print($"Adding memento... Current memento count: {mementoList.Count}. Current Memento index: {mementoIndex}");

        if (mementoIndex < (mementoList.Count - 1))
        {
            GD.Print($"AddMemento: mementoIndex is not the last in the list : Deleting mementos after {mementoIndex}");
            DeleteMementosAfterIndex((int)mementoIndex);
        }
        mementoList.Add(memento);
        mementoIndex = (mementoList.Count - 1);
        //GD.Print($"Added memento. New memento count: {mementoList.Count}. Current Memento Index: {mementoIndex}");
    }

    private void DeleteMementosAfterIndex(int index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, mementoList.Count - 1);
        //GD.Print($"Deleting all mementos after index {index}...");
        mementoList.RemoveRange(index + 1, mementoList.Count - (index + 1));
        //GD.Print($"Mementos Deleted. Memento count: {mementoList.Count}. Current Memento index: {mementoIndex}");
    }

    private void Undo()
    {
        GD.Print($"Undoing... Memento count: {mementoList.Count}. Current Memento index: {mementoIndex}");
        switch (mementoIndex)
        {
            //case var expression when (mementoIndex == (mementoList.Count - 1)):
            //    mementoIndex = mementoList.Count - 2 >= 0 ? mementoList.Count - 2 : 0;
            //    break;
            case > 0:
                mementoIndex--;
                break;
            case 0:
                //GD.Print($"Undo failed. Already in beginning of list. Memento count: {mementoList.Count}. Current Memento index: {mementoIndex}");
                return;
            default:
                throw new Exception($"Memento index could not be handled");
        }
        //Project.Instance.NotificationMessage = $"Undo: mementoIndex = {mementoIndex ?? (mementoList.Count - 1)} / {mementoList.Count - 1}";

        IMemento memento = mementoList[(int)mementoIndex];
        if (memento == null)
            throw new NullReferenceException($"{nameof(memento)}");

        Timing.Instance.RestoreMemento(memento);

        //GD.Print($"Undo complete. Memento count: {mementoList.Count}. Current Memento index: {mementoIndex}");
    }

    private void Redo()
    {
        //GD.Print($"Redoing... Memento count: {mementoList.Count}. Current Memento index: {mementoIndex}");
        switch (mementoIndex)
        {
            case var expression when (mementoIndex < (mementoList.Count - 1)):
                mementoIndex++;
                break;
            case var expression when (mementoIndex == mementoList.Count - 1):
                //mementoIndex = null;
                return;
            default:
                throw new Exception($"Memento index could not be handled");
        }
        //Project.Instance.NotificationMessage = $"Redo: mementoIndex = {mementoIndex} / {mementoList.Count-1}";

        IMemento memento = mementoList[(int)mementoIndex];

        Timing.Instance.RestoreMemento(memento);

        //GD.Print($"Redo complete. Memento count: {mementoList.Count}. Current Memento index: {mementoIndex}");
    }
}