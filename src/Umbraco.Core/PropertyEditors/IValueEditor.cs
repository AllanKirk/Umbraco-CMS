﻿namespace Umbraco.Core.PropertyEditors
{
    /// <summary>
    /// Represents an editor for editing values.
    /// </summary>
    public interface IValueEditor
    {
        /// <summary>
        /// Gets the editor view.
        /// </summary>
        string View { get; }
    }
}
