﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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