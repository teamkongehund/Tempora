using System;
using System.Collections.Generic;
using Godot;
using GD = Tempora.Classes.DataTools.GD;
using Tempora.Classes.TimingClasses;
using System.Reflection;

namespace Tempora.Classes.Utility;

public partial class MementoHandler : Node
{
    private static MementoHandler instance = null!;
    public static MementoHandler Instance { get => instance; set => instance = value; }

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
        }
    }

    private TimingPoint? timingPointBeingChanged = null;
    private int[]? SelectionBeingChanged = null;

    /// <summary>
    /// When doing many changes to a single <see cref="TimingPoint"/>, this overload will save them all in one <see cref="IMemento"/>.
    /// </summary>
    /// <param name="timingPoint"></param>
    public void AddTimingMemento(TimingPoint? timingPoint)
    {
        if (timingPointBeingChanged == timingPoint)
            DeleteMementosAfterIndex(mementoIndex - 1);

        IMemento memento = Timing.Instance.GetMemento();
        AddMemento(memento);

        timingPointBeingChanged = timingPoint;
    }

    /// <summary>
    /// When doing many changes to a single selection, this overload will save them all in one <see cref="IMemento"/>.
    /// </summary>
    /// <param name="timingPoint"></param>
    public void AddTimingMemento(int[]? selection)
    {
        if (SelectionBeingChanged == selection)
            DeleteMementosAfterIndex(mementoIndex - 1);

        IMemento memento = Timing.Instance.GetMemento();
        AddMemento(memento);

        SelectionBeingChanged = selection;
    }

    public void AddTimingMemento()
    {
        IMemento memento = Timing.Instance.GetMemento();
        AddMemento(memento);
        timingPointBeingChanged = null;
    }

    public void AddSelectionMemento()
    {
        IMemento memento = Timing.Instance.GetMemento(); // TODO: Change this
        return;
        AddMemento(memento);
        timingPointBeingChanged = null;
    }

    private void AddMemento(IMemento memento)
    {
        if (mementoIndex < (mementoList.Count - 1))
            DeleteMementosAfterIndex(mementoIndex);

        mementoList.Add(memento);
        mementoIndex = (mementoList.Count - 1);
    }

    private void DeleteMementosAfterIndex(int index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, mementoList.Count - 1);
        mementoList.RemoveRange(index + 1, mementoList.Count - (index + 1));
    }

    private void Undo()
    {
        switch (mementoIndex)
        {
            case > 0:
                mementoIndex--;
                break;
            case 0:
                return;
            default:
                throw new Exception($"Memento index could not be handled");
        }

        IMemento memento = mementoList[(int)mementoIndex];
        if (memento == null)
            throw new NullReferenceException($"{nameof(memento)}");

        Timing.Instance.RestoreMemento(memento);
    }

    private void Redo()
    {
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

        IMemento memento = mementoList[(int)mementoIndex];

        Timing.Instance.RestoreMemento(memento);
    }
}