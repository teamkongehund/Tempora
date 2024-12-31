// Copyright 2024 https://github.com/kongehund
// 
// This file is licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International (CC BY-NC-ND 4.0).
// You are free to:
// - Share — copy and redistribute the material in any medium or format
//
// Under the following terms:
// - Attribution — You must give appropriate credit, provide a link to the license, and indicate if changes were made.
// - NonCommercial — You may not use the material for commercial purposes.
// - NoDerivatives — If you remix, transform, or build upon the material, you may not distribute the modified material.
//
// Full license text is available at: https://creativecommons.org/licenses/by-nc-nd/4.0/legalcode

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