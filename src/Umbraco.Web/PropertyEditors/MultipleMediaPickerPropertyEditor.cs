﻿using System;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.PropertyEditors;

namespace Umbraco.Web.PropertyEditors
{
    [Obsolete("This editor is obsolete, use MultipleMediaPickerPropertyEditor2 instead which stores UDI")]
    [PropertyEditor(Constants.PropertyEditors.MultipleMediaPickerAlias, "(Obsolete) Media Picker", "mediapicker", Group = "media", Icon = "icon-pictures-alt-2", IsDeprecated = true)]
    public class MultipleMediaPickerPropertyEditor : MediaPicker2PropertyEditor
    {
        public MultipleMediaPickerPropertyEditor(ILogger logger): base(logger)
        {
            //default it to multi picker
            InternalPreValues["multiPicker"] = "1";
            InternalPreValues["idType"] = "int";
        }

        protected override PreValueEditor CreateConfigurationEditor()
        {
            var preValEditor = base.CreateConfigurationEditor();
            preValEditor.Fields.Single(x => x.Key == "startNodeId").Config["idType"] = "int";
            return preValEditor;
        }
    }
}
