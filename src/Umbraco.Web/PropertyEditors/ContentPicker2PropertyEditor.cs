﻿using System.Collections.Generic;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.PropertyEditors;

namespace Umbraco.Web.PropertyEditors
{
    /// <summary>
    /// Content property editor that stores UDI
    /// </summary>
    [PropertyEditor(Constants.PropertyEditors.Aliases.ContentPicker2Alias, "Content Picker", PropertyEditorValueTypes.String, "contentpicker", IsParameterEditor = true, Group = "Pickers")]
    public class ContentPicker2PropertyEditor : PropertyEditor
    {
        public ContentPicker2PropertyEditor(ILogger logger)
            : base(logger)
        {
            InternalPreValues = new Dictionary<string, object>
            {
                {"startNodeId", "-1"},
                {"showOpenButton", "0"},
                {"showEditButton", "0"},
                {"showPathOnHover", "0"},
                {"idType", "udi"}
            };
        }

        internal IDictionary<string, object> InternalPreValues;

        public override IDictionary<string, object> DefaultPreValues
        {
            get => InternalPreValues;
            set => InternalPreValues = value;
        }

        protected override PreValueEditor CreateConfigurationEditor()
        {
            return new ContentPickerPreValueEditor();
        }

        internal class ContentPickerPreValueEditor : PreValueEditor
        {
            public ContentPickerPreValueEditor()
            {
                //create the fields
                Fields.Add(new DataTypeConfigurationField()
                {
                    Key = "showOpenButton",
                    View = "boolean",
                    Name = "Show open button (this feature is in preview!)",
                    Description = "Opens the node in a dialog"
                });
                Fields.Add(new DataTypeConfigurationField()
                {
                    Key = "startNodeId",
                    View = "treepicker",
                    Name = "Start node",
                    Config = new Dictionary<string, object>
                    {
                        {"idType", "udi"}
                    }
                });
            }
        }
    }
}
