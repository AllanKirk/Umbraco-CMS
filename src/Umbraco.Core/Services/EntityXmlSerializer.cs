﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using Newtonsoft.Json;
using Umbraco.Core.Composing;
using Umbraco.Core.Models;
using Umbraco.Core.Strings;

namespace Umbraco.Core.Services
{
    //TODO: Move the rest of the logic for the PackageService.Export methods to here!

    /// <summary>
    /// A helper class to serialize entities to XML
    /// </summary>
    internal class EntityXmlSerializer
    {
        /// <summary>
        /// Exports an IContent item as an XElement.
        /// </summary>
        public static XElement Serialize(
            IContentService contentService,
            IDataTypeService dataTypeService,
            IUserService userService,
            ILocalizationService localizationService,
            IEnumerable<IUrlSegmentProvider> urlSegmentProviders,
            IContent content,
            bool published,
            bool withDescendants = false) // fixme take care of usage!
        {
            if (contentService == null) throw new ArgumentNullException(nameof(contentService));
            if (dataTypeService == null) throw new ArgumentNullException(nameof(dataTypeService));
            if (userService == null) throw new ArgumentNullException(nameof(userService));
            if (localizationService == null) throw new ArgumentNullException(nameof(localizationService));
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (urlSegmentProviders == null) throw new ArgumentNullException(nameof(urlSegmentProviders));

            // nodeName should match Casing.SafeAliasWithForcingCheck(content.ContentType.Alias);
            var nodeName = content.ContentType.Alias.ToSafeAliasWithForcingCheck();

            var xml = SerializeContentBase(dataTypeService, localizationService, content, content.GetUrlSegment(urlSegmentProviders), nodeName, published);

            xml.Add(new XAttribute("nodeType", content.ContentType.Id));
            xml.Add(new XAttribute("nodeTypeAlias", content.ContentType.Alias));

            xml.Add(new XAttribute("creatorName", content.GetCreatorProfile(userService).Name));
            //xml.Add(new XAttribute("creatorID", content.CreatorId));
            xml.Add(new XAttribute("writerName", content.GetWriterProfile(userService).Name));
            xml.Add(new XAttribute("writerID", content.WriterId));

            xml.Add(new XAttribute("template", content.Template?.Id.ToString(CultureInfo.InvariantCulture) ?? "0"));

            if (withDescendants)
            {
                var descendants = contentService.GetDescendants(content).ToArray();
                var currentChildren = descendants.Where(x => x.ParentId == content.Id);
                SerializeDescendants(contentService, dataTypeService, userService, localizationService, urlSegmentProviders, descendants, currentChildren, xml, published);
            }

            return xml;
        }

        /// <summary>
        /// Exports an IMedia item as an XElement.
        /// </summary>
        public static XElement Serialize(
            IMediaService mediaService,
            IDataTypeService dataTypeService,
            IUserService userService,
            ILocalizationService localizationService,
            IEnumerable<IUrlSegmentProvider> urlSegmentProviders,
            IMedia media,
            bool withDescendants = false)
        {
            if (mediaService == null) throw new ArgumentNullException(nameof(mediaService));
            if (dataTypeService == null) throw new ArgumentNullException(nameof(dataTypeService));
            if (userService == null) throw new ArgumentNullException(nameof(userService));
            if (localizationService == null) throw new ArgumentNullException(nameof(localizationService));
            if (media == null) throw new ArgumentNullException(nameof(media));
            if (urlSegmentProviders == null) throw new ArgumentNullException(nameof(urlSegmentProviders));

            // nodeName should match Casing.SafeAliasWithForcingCheck(content.ContentType.Alias);
            var nodeName = media.ContentType.Alias.ToSafeAliasWithForcingCheck();

            const bool published = false; // always false for media
            var xml = SerializeContentBase(dataTypeService, localizationService, media, media.GetUrlSegment(urlSegmentProviders), nodeName, published);

            xml.Add(new XAttribute("nodeType", media.ContentType.Id));
            xml.Add(new XAttribute("nodeTypeAlias", media.ContentType.Alias));

            //xml.Add(new XAttribute("creatorName", media.GetCreatorProfile(userService).Name));
            //xml.Add(new XAttribute("creatorID", media.CreatorId));
            xml.Add(new XAttribute("writerName", media.GetWriterProfile(userService).Name));
            xml.Add(new XAttribute("writerID", media.WriterId));

            //xml.Add(new XAttribute("template", 0)); // no template for media

            if (withDescendants)
            {
                var descendants = mediaService.GetDescendants(media).ToArray();
                var currentChildren = descendants.Where(x => x.ParentId == media.Id);
                SerializeDescendants(mediaService, dataTypeService, userService, localizationService, urlSegmentProviders, descendants, currentChildren, xml);
            }

            return xml;
        }

        /// <summary>
        /// Exports an IMember item as an XElement.
        /// </summary>
        public static XElement Serialize(
            IDataTypeService dataTypeService,
            ILocalizationService localizationService,
            IMember member)
        {
            // nodeName should match Casing.SafeAliasWithForcingCheck(content.ContentType.Alias);
            var nodeName = member.ContentType.Alias.ToSafeAliasWithForcingCheck();

            const bool published = false; // always false for member
            var xml = SerializeContentBase(dataTypeService, localizationService, member, "", nodeName, published);

            xml.Add(new XAttribute("nodeType", member.ContentType.Id));
            xml.Add(new XAttribute("nodeTypeAlias", member.ContentType.Alias));

            // what about writer/creator/version?

            xml.Add(new XAttribute("loginName", member.Username));
            xml.Add(new XAttribute("email", member.Email));
            xml.Add(new XAttribute("icon", member.ContentType.Icon));

            return xml;
        }

        public XElement Serialize(IDataTypeService dataTypeService, IDataType dataType)
        {
            var xml = new XElement("DataType");
            xml.Add(new XAttribute("Name", dataType.Name));
            //The 'ID' when exporting is actually the property editor alias (in pre v7 it was the IDataType GUID id)
            xml.Add(new XAttribute("Id", dataType.EditorAlias));
            xml.Add(new XAttribute("Definition", dataType.Key));
            xml.Add(new XAttribute("DatabaseType", dataType.DatabaseType.ToString()));
            xml.Add(new XAttribute("Configuration", JsonConvert.SerializeObject(dataType.Configuration)));

            var folderNames = string.Empty;
            if (dataType.Level != 1)
            {
                //get url encoded folder names
                var folders = dataTypeService.GetContainers(dataType)
                    .OrderBy(x => x.Level)
                    .Select(x => HttpUtility.UrlEncode(x.Name));

                folderNames = string.Join("/", folders.ToArray());
            }

            if (string.IsNullOrWhiteSpace(folderNames) == false)
                xml.Add(new XAttribute("Folders", folderNames));

            return xml;
        }

        public XElement Serialize(IDictionaryItem dictionaryItem)
        {
            var xml = new XElement("DictionaryItem", new XAttribute("Key", dictionaryItem.ItemKey));
            foreach (var translation in dictionaryItem.Translations)
            {
                xml.Add(new XElement("Value",
                    new XAttribute("LanguageId", translation.Language.Id),
                    new XAttribute("LanguageCultureAlias", translation.Language.IsoCode),
                    new XCData(translation.Value)));
            }

            return xml;
        }

        public XElement Serialize(Stylesheet stylesheet)
        {
            var xml = new XElement("Stylesheet",
                new XElement("Name", stylesheet.Alias),
                new XElement("FileName", stylesheet.Path),
                new XElement("Content", new XCData(stylesheet.Content)));

            var props = new XElement("Properties");
            xml.Add(props);

            foreach (var prop in stylesheet.Properties)
            {
                props.Add(new XElement("Property",
                    new XElement("Name", prop.Name),
                    new XElement("Alias", prop.Alias),
                    new XElement("Value", prop.Value)));
            }

            return xml;
        }

        public XElement Serialize(ILanguage language)
        {
            var xml = new XElement("Language",
                new XAttribute("Id", language.Id),
                new XAttribute("CultureAlias", language.IsoCode),
                new XAttribute("FriendlyName", language.CultureName));

            return xml;
        }

        public XElement Serialize(ITemplate template)
        {
            var xml = new XElement("Template");
            xml.Add(new XElement("Name", template.Name));
            xml.Add(new XElement("Alias", template.Alias));
            xml.Add(new XElement("Design", new XCData(template.Content)));

            if (template is Template concreteTemplate && concreteTemplate.MasterTemplateId != null)
            {
                if (concreteTemplate.MasterTemplateId.IsValueCreated &&
                    concreteTemplate.MasterTemplateId.Value != default)
                {
                    xml.Add(new XElement("Master", concreteTemplate.MasterTemplateId.ToString()));
                    xml.Add(new XElement("MasterAlias", concreteTemplate.MasterTemplateAlias));
                }
            }

            return xml;
        }

        public XElement Serialize(IDataTypeService dataTypeService, IMediaType mediaType)
        {
            var info = new XElement("Info",
                                    new XElement("Name", mediaType.Name),
                                    new XElement("Alias", mediaType.Alias),
                                    new XElement("Icon", mediaType.Icon),
                                    new XElement("Thumbnail", mediaType.Thumbnail),
                                    new XElement("Description", mediaType.Description),
                                    new XElement("AllowAtRoot", mediaType.AllowedAsRoot.ToString()));

            var masterContentType = mediaType.CompositionAliases().FirstOrDefault();
            if (masterContentType != null)
                info.Add(new XElement("Master", masterContentType));

            var structure = new XElement("Structure");
            foreach (var allowedType in mediaType.AllowedContentTypes)
            {
                structure.Add(new XElement("MediaType", allowedType.Alias));
            }

            var genericProperties = new XElement("GenericProperties"); // actually, all of them
            foreach (var propertyType in mediaType.PropertyTypes)
            {
                var definition = dataTypeService.GetDataType(propertyType.DataTypeDefinitionId);

                var propertyGroup = propertyType.PropertyGroupId == null // true generic property
                    ? null
                    : mediaType.PropertyGroups.FirstOrDefault(x => x.Id == propertyType.PropertyGroupId.Value);

                var genericProperty = new XElement("GenericProperty",
                                                   new XElement("Name", propertyType.Name),
                                                   new XElement("Alias", propertyType.Alias),
                                                   new XElement("Type", propertyType.PropertyEditorAlias),
                                                   new XElement("Definition", definition.Key),
                                                   new XElement("Tab", propertyGroup == null ? "" : propertyGroup.Name),
                                                   new XElement("Mandatory", propertyType.Mandatory.ToString()),
                                                   new XElement("Validation", propertyType.ValidationRegExp),
                                                   new XElement("Description", new XCData(propertyType.Description)));
                genericProperties.Add(genericProperty);
            }

            var tabs = new XElement("Tabs");
            foreach (var propertyGroup in mediaType.PropertyGroups)
            {
                var tab = new XElement("Tab",
                                       new XElement("Id", propertyGroup.Id.ToString(CultureInfo.InvariantCulture)),
                                       new XElement("Caption", propertyGroup.Name),
                                       new XElement("SortOrder", propertyGroup.SortOrder));

                tabs.Add(tab);
            }

            var xml = new XElement("MediaType",
                                   info,
                                   structure,
                                   genericProperties,
                                   tabs);

            return xml;
        }

        public XElement Serialize(IMacro macro)
        {
            var xml = new XElement("macro");
            xml.Add(new XElement("name", macro.Name));
            xml.Add(new XElement("alias", macro.Alias));
            xml.Add(new XElement("scriptType", macro.ControlType));
            xml.Add(new XElement("scriptAssembly", macro.ControlAssembly));
            xml.Add(new XElement("scriptingFile", macro.ScriptPath));
            xml.Add(new XElement("xslt", macro.XsltPath));
            xml.Add(new XElement("useInEditor", macro.UseInEditor.ToString()));
            xml.Add(new XElement("dontRender", macro.DontRender.ToString()));
            xml.Add(new XElement("refreshRate", macro.CacheDuration.ToString(CultureInfo.InvariantCulture)));
            xml.Add(new XElement("cacheByMember", macro.CacheByMember.ToString()));
            xml.Add(new XElement("cacheByPage", macro.CacheByPage.ToString()));

            var properties = new XElement("properties");
            foreach (var property in macro.Properties)
            {
                properties.Add(new XElement("property",
                    new XAttribute("name", property.Name),
                    new XAttribute("alias", property.Alias),
                    new XAttribute("sortOrder", property.SortOrder),
                    new XAttribute("propertyType", property.EditorAlias)));
            }
            xml.Add(properties);

            return xml;
        }

        public XElement Serialize(IDataTypeService dataTypeService, IContentTypeService contentTypeService, IContentType contentType)
        {
            var info = new XElement("Info",
                                    new XElement("Name", contentType.Name),
                                    new XElement("Alias", contentType.Alias),
                                    new XElement("Icon", contentType.Icon),
                                    new XElement("Thumbnail", contentType.Thumbnail),
                                    new XElement("Description", contentType.Description),
                                    new XElement("AllowAtRoot", contentType.AllowedAsRoot.ToString()),
                                    new XElement("IsListView", contentType.IsContainer.ToString()));

            var masterContentType = contentType.ContentTypeComposition.FirstOrDefault(x => x.Id == contentType.ParentId);
            if(masterContentType != null)
                info.Add(new XElement("Master", masterContentType.Alias));

            var compositionsElement = new XElement("Compositions");
            var compositions = contentType.ContentTypeComposition;
            foreach (var composition in compositions)
            {
                compositionsElement.Add(new XElement("Composition", composition.Alias));
            }
            info.Add(compositionsElement);

            var allowedTemplates = new XElement("AllowedTemplates");
            foreach (var template in contentType.AllowedTemplates)
            {
                allowedTemplates.Add(new XElement("Template", template.Alias));
            }
            info.Add(allowedTemplates);

            if (contentType.DefaultTemplate != null && contentType.DefaultTemplate.Id != 0)
                info.Add(new XElement("DefaultTemplate", contentType.DefaultTemplate.Alias));
            else
                info.Add(new XElement("DefaultTemplate", ""));

            var structure = new XElement("Structure");
            foreach (var allowedType in contentType.AllowedContentTypes)
            {
                structure.Add(new XElement("DocumentType", allowedType.Alias));
            }

            var genericProperties = new XElement("GenericProperties"); // actually, all of them
            foreach (var propertyType in contentType.PropertyTypes)
            {
                var definition = dataTypeService.GetDataType(propertyType.DataTypeDefinitionId);

                var propertyGroup = propertyType.PropertyGroupId == null // true generic property
                    ? null
                    : contentType.PropertyGroups.FirstOrDefault(x => x.Id == propertyType.PropertyGroupId.Value);

                var genericProperty = new XElement("GenericProperty",
                                                   new XElement("Name", propertyType.Name),
                                                   new XElement("Alias", propertyType.Alias),
                                                   new XElement("Type", propertyType.PropertyEditorAlias),
                                                   new XElement("Definition", definition.Key),
                                                   new XElement("Tab", propertyGroup == null ? "" : propertyGroup.Name),
                                                   new XElement("SortOrder", propertyType.SortOrder),
                                                   new XElement("Mandatory", propertyType.Mandatory.ToString()),
                                                   propertyType.ValidationRegExp != null ? new XElement("Validation", propertyType.ValidationRegExp) : null,
                                                   propertyType.Description != null ? new XElement("Description", new XCData(propertyType.Description)) : null);

                genericProperties.Add(genericProperty);
            }

            var tabs = new XElement("Tabs");
            foreach (var propertyGroup in contentType.PropertyGroups)
            {
                var tab = new XElement("Tab",
                                       new XElement("Id", propertyGroup.Id.ToString(CultureInfo.InvariantCulture)),
                                       new XElement("Caption", propertyGroup.Name),
                                       new XElement("SortOrder", propertyGroup.SortOrder));
                tabs.Add(tab);
            }

            var xml = new XElement("DocumentType",
                info,
                structure,
                genericProperties,
                tabs);

            var folderNames = string.Empty;
            //don't add folders if this is a child doc type
            if (contentType.Level != 1 && masterContentType == null)
            {
                //get url encoded folder names
                var folders = contentTypeService.GetContainers(contentType)
                    .OrderBy(x => x.Level)
                    .Select(x => HttpUtility.UrlEncode(x.Name));

                folderNames = string.Join("/", folders.ToArray());
            }

            if (string.IsNullOrWhiteSpace(folderNames) == false)
                xml.Add(new XAttribute("Folders", folderNames));

            return xml;
        }

        // exports an IContentBase (IContent, IMedia or IMember) as an XElement.
        private static XElement SerializeContentBase(IDataTypeService dataTypeService, ILocalizationService localizationService, IContentBase contentBase, string urlValue, string nodeName, bool published)
        {
            var xml = new XElement(nodeName,
                new XAttribute("id", contentBase.Id),
                new XAttribute("key", contentBase.Key),
                new XAttribute("parentID", contentBase.Level > 1 ? contentBase.ParentId : -1),
                new XAttribute("level", contentBase.Level),
                new XAttribute("creatorID", contentBase.CreatorId),
                new XAttribute("sortOrder", contentBase.SortOrder),
                new XAttribute("createDate", contentBase.CreateDate.ToString("s")),
                new XAttribute("updateDate", contentBase.UpdateDate.ToString("s")),
                new XAttribute("nodeName", contentBase.Name),
                new XAttribute("urlName", urlValue),
                new XAttribute("path", contentBase.Path),
                new XAttribute("isDoc", ""));

            foreach (var property in contentBase.Properties)
                xml.Add(SerializeProperty(dataTypeService, localizationService, property, published));

            return xml;
        }

        // exports a property as XElements.
        private static IEnumerable<XElement> SerializeProperty(IDataTypeService dataTypeService, ILocalizationService localizationService, Property property, bool published)
        {
            var propertyType = property.PropertyType;

            // get the property editor for this property and let it convert it to the xml structure
            var propertyEditor = Current.PropertyEditors[propertyType.PropertyEditorAlias];
            return propertyEditor == null
                ? Array.Empty<XElement>()
                : propertyEditor.ValueEditor.ConvertDbToXml(property, dataTypeService, localizationService, published);
        }

        // exports an IContent item descendants.
        private static void SerializeDescendants(IContentService contentService, IDataTypeService dataTypeService, IUserService userService, ILocalizationService localizationService, IEnumerable<IUrlSegmentProvider> urlSegmentProviders, IContent[] originalDescendants, IEnumerable<IContent> children, XElement xml, bool published)
        {
            foreach (var child in children)
            {
                // add the child xml
                var childXml = Serialize(contentService, dataTypeService, userService, localizationService, urlSegmentProviders, child, published);
                xml.Add(childXml);

                // capture id (out of closure) and get the grandChildren (children of the child)
                var parentId = child.Id;
                var grandChildren = originalDescendants.Where(x => x.ParentId == parentId);

                // recurse
                SerializeDescendants(contentService, dataTypeService, userService, localizationService, urlSegmentProviders, originalDescendants, grandChildren, childXml, published);
            }
        }

        // exports an IMedia item descendants.
        private static void SerializeDescendants(IMediaService mediaService, IDataTypeService dataTypeService, IUserService userService, ILocalizationService localizationService, IEnumerable<IUrlSegmentProvider> urlSegmentProviders, IMedia[] originalDescendants, IEnumerable<IMedia> children, XElement xml)
        {
            foreach (var child in children)
            {
                // add the child xml
                var childXml = Serialize(mediaService, dataTypeService, userService, localizationService, urlSegmentProviders, child);
                xml.Add(childXml);

                // capture id (out of closure) and get the grandChildren (children of the child)
                var parentId = child.Id;
                var grandChildren = originalDescendants.Where(x => x.ParentId == parentId);

                // recurse
                SerializeDescendants(mediaService, dataTypeService, userService, localizationService, urlSegmentProviders, originalDescendants, grandChildren, childXml);
            }
        }
    }
}
