﻿using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.PropertyEditors;
using Umbraco.Core.PropertyEditors.Validators;

namespace Umbraco.Web.PropertyEditors
{
    [PropertyEditor(Constants.PropertyEditors.Aliases.Integer, "Numeric", "integer", IsParameterEditor = true, ValueType = PropertyEditorValueTypes.IntegerAlternative)]
    public class IntegerPropertyEditor : PropertyEditor
    {
        /// <summary>
        /// The constructor will setup the property editor based on the attribute if one is found
        /// </summary>
        public IntegerPropertyEditor(ILogger logger) : base(logger)
        {
        }

        /// <summary>
        /// Overridden to ensure that the value is validated
        /// </summary>
        /// <returns></returns>
        protected override PropertyValueEditor CreateValueEditor()
        {
            var editor = base.CreateValueEditor();
            editor.Validators.Add(new IntegerValidator());
            return editor;
        }

        protected override PreValueEditor CreateConfigurationEditor()
        {
            return new IntegerPreValueEditor();
        }

        /// <summary>
        /// A custom pre-value editor class to deal with the legacy way that the pre-value data is stored.
        /// </summary>
        internal class IntegerPreValueEditor : PreValueEditor
        {
            public IntegerPreValueEditor()
            {
                //create the fields
                Fields.Add(new DataTypeConfigurationField(new IntegerValidator())
                {
                    Description = "Enter the minimum amount of number to be entered",
                    Key = "min",
                    View = "number",
                    Name = "Minimum"
                });
                Fields.Add(new DataTypeConfigurationField(new IntegerValidator())
                {
                    Description = "Enter the intervals amount between each step of number to be entered",
                    Key = "step",
                    View = "number",
                    Name = "Step Size"
                });
                Fields.Add(new DataTypeConfigurationField(new IntegerValidator())
                {
                    Description = "Enter the maximum amount of number to be entered",
                    Key = "max",
                    View = "number",
                    Name = "Maximum"
                });
            }
        }
    }
}
