using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tempora.Classes.Utility;

/// <summary>
/// The IMemento class should also implement a private method to get the state, which the originator must access.
/// </summary>
public interface IMemento
{
    /// <summary>
    /// Return the reference to the originator object whose state should be modified with the memento. Should be a public method to be used by the caretaker to send mementos to the originator.
    /// </summary>
    /// <returns></returns>
    IMementoOriginator GetOriginator();
}
