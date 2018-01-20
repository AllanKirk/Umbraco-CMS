﻿using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.PropertyEditors;

namespace Umbraco.Web.PropertyEditors
{
    /// <summary>
    /// Represents a property editor for label properties.
    /// </summary>
    [PropertyEditor(Constants.PropertyEditors.Aliases.NoEdit, "Label", "readonlyvalue", Icon = "icon-readonly")]
    public class LabelPropertyEditor : PropertyEditor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LabelPropertyEditor"/> class.
        /// </summary>
        /// <param name="logger"></param>
        public LabelPropertyEditor(ILogger logger)
            : base(logger)
        { }

        /// <inheritdoc />
        protected override PropertyValueEditor CreateValueEditor()
        {
            return new LabelPropertyValueEditor(base.CreateValueEditor());
        }

        /// <inheritdoc />
        protected override PreValueEditor CreateConfigurationEditor()
        {
            return new LabelPreValueEditor();
        }

        // provides the property value editor
        internal class LabelPropertyValueEditor : PropertyValueEditorWrapper
        {
            public LabelPropertyValueEditor(PropertyValueEditor wrapped)
                : base(wrapped)
            { }

            /// <inheritdoc />
            public override bool IsReadOnly => true;
        }

        // provides the datatype configuration editor
        internal class LabelPreValueEditor : PreValueEditor
        {
            private const string LegacyPropertyEditorValuesKey = "values"; // fixme wtf is this?!

            public LabelPreValueEditor()
            {
                //Fields.Add(new DataTypeConfigurationField
                //{
                //    Key = Constants.PropertyEditors.PreValueKeys.DataValueType,
                //    Name = "Value type",
                //    View = "valuetype"
                //});
                Fields.Add(new DataTypeConfigurationField
                {
                    HideLabel = true,
                    View = "readonlykeyvalues",
                    Key = LegacyPropertyEditorValuesKey
                });

                // fixme what 
                ValueType = PropertyEditorValueTypes.String;
            }

            // fixme - mixing declarative and code fields, what a mess?!
            [DataTypeConfigurationField(Constants.PropertyEditors.ConfigurationKeys.DataValueType, "Value type", "valuetype")]
            public string ValueType { get; set; }

            /// <summary>
            /// Other than for the pre-value fields defined on this property editor, chuck all the values into one field so devs can see what is stored there.
            /// We want this in case we've converted a legacy property editor over to a label as we should still show the pre-values stored for the data type.
            /// </summary>
            /// <param name="defaultPreVals"></param>
            /// <param name="persistedPreVals"></param>
            /// <returns></returns>
            public override IDictionary<string, object> ConvertDbToEditor(IDictionary<string, object> defaultPreVals, PreValueCollection persistedPreVals)
            {
                var existing = base.ConvertDbToEditor(defaultPreVals, persistedPreVals);

                // Check for a saved value type.  If not found set to default string type.
                var valueType = PropertyEditorValueTypes.String;
                if (existing.ContainsKey(Constants.PropertyEditors.ConfigurationKeys.DataValueType))
                {
                    valueType = (string)existing[Constants.PropertyEditors.ConfigurationKeys.DataValueType];
                }

                // Convert any other values from a legacy property editor to a list, easier to enumerate on the editor.
                // Make sure to exclude values defined on the label property editor itself.
                var asList = existing
                    .Select(e => new KeyValuePair<string, object>(e.Key, e.Value))
                    .Where(e => e.Key != Constants.PropertyEditors.ConfigurationKeys.DataValueType)
                    .ToList();

                var result = new Dictionary<string, object> { { Constants.PropertyEditors.ConfigurationKeys.DataValueType, valueType } };
                if (asList.Any())
                {
                    result.Add(LegacyPropertyEditorValuesKey, asList);
                }

                return result;
            }

            /// <summary>
            /// When saving we want to avoid saving an empty "legacy property editor values" field if there are none.
            /// </summary>
            /// <param name="editorValue"></param>
            /// <param name="currentValue"></param>
            /// <returns></returns>
            public override IDictionary<string, PreValue> ConvertEditorToDb(IDictionary<string, object> editorValue, PreValueCollection currentValue)
            {
                // notes (from the PR):
                //
                // "All stemmed from the fact that even though the label property editor could have pre-values (from a legacy type),
                // they couldn't up to now be edited and saved through the UI. Now that a "true" pre-value has been added, it can,
                // which led to some odd behaviour.
                //
                // Firstly there would be a pre-value record saved for legacy values even if there aren't any (the key would exist
                // but with no value). In that case I remove that pre-value so it's not saved(likely does no harm, but it's not
                // necessary - we only need this legacy values pre-value record if there are any).
                //
                // Secondly if there are legacy values, I found on each save the JSON structure containing them would get repeatedly
                // nested (an outer JSON wrapper would be added each time). So what I'm doing is if there are legacy pre-values,
                // I'm converting what comes in "wrapped" like (below) into the legacy property editor values."

                if (editorValue.ContainsKey(LegacyPropertyEditorValuesKey))
                {
                    // If provided value contains an empty legacy property editor values, don't save it
                    if (editorValue[LegacyPropertyEditorValuesKey] == null)
                    {
                        editorValue.Remove(LegacyPropertyEditorValuesKey);
                    }
                    else
                    {
                        // If provided value contains legacy property editor values, unwrap the value to save so it doesn't get repeatedly nested on saves.
                        // This is a bit funky - but basically needing to parse out the original value from a JSON structure that is passed in
                        // looking like:
                        //   Value = {[
                        //   {
                        //      "Key": "values",
                        //      "Value": {
                        //          <legacy property editor values>
                        //      }}
                        //   ]}
                        var values = editorValue[LegacyPropertyEditorValuesKey] as JArray;
                        if (values != null && values.Count == 1 && values.First.Values().Count() == 2)
                        {
                            editorValue[LegacyPropertyEditorValuesKey] = values.First.Values().Last();
                        }
                    }
                }

                return base.ConvertEditorToDb(editorValue, currentValue);
            }
        }
    }
}
