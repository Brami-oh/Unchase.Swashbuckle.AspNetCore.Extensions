﻿using System;
using System.Linq;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Unchase.Swashbuckle.AspNetCore.Extensions.Extensions;
using Unchase.Swashbuckle.AspNetCore.Extensions.Options;

namespace Unchase.Swashbuckle.AspNetCore.Extensions.Filters
{
    internal class DisplayEnumsWithValuesDocumentFilter : IDocumentFilter
    {
        #region Fields

        private readonly bool _applyFiler;
        private readonly bool _includeDescriptionFromAttribute;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="options"><see cref="FixEnumsOptions"/>.</param>
        /// <param name="configureOptions">An <see cref="Action{FixEnumsOptions}"/> to configure options for filter.</param>
        public DisplayEnumsWithValuesDocumentFilter(IOptions<FixEnumsOptions> options, Action<FixEnumsOptions> configureOptions = null)
        {
            if (options.Value != null)
            {
                configureOptions?.Invoke(options.Value);
                this._includeDescriptionFromAttribute = options.Value.IncludeDescriptions;
                this._applyFiler = options.Value.ApplyDocumentFilter;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Apply the filter.
        /// </summary>
        /// <param name="openApiDoc"><see cref="OpenApiDocument"/>.</param>
        /// <param name="context"><see cref="DocumentFilterContext"/>.</param>
        public void Apply(OpenApiDocument openApiDoc, DocumentFilterContext context)
        {
            if (!this._applyFiler)
                return;

            foreach (var schemaDictionaryItem in openApiDoc.Components.Schemas)
            {
                var schema = schemaDictionaryItem.Value;
                var description = schema.AddEnumValuesDescription(this._includeDescriptionFromAttribute);
                if (description != null && schema.Description != null && !schema.Description.Contains(description))
                    schema.Description += description;
            }

            if (openApiDoc.Paths.Count <= 0)
                return;

            // add enum descriptions to input parameters of every operation
            foreach (var parameter in openApiDoc.Paths.Values.SelectMany(v => v.Operations).SelectMany(op => op.Value.Parameters))
            {
                if (parameter.Schema.Reference == null)
                    continue;

                var componentReference = parameter.Schema.Reference.Id;
                var schema = openApiDoc.Components.Schemas[componentReference];

                var description = schema.AddEnumValuesDescription(this._includeDescriptionFromAttribute);
                if (description != null && parameter.Description != null && !parameter.Description.Contains(description))
                    parameter.Description += description;
            }

            // add enum descriptions to request body
            foreach (var operation in openApiDoc.Paths.Values.SelectMany(v => v.Operations))
            {
                var requestBodyContents = operation.Value.RequestBody?.Content;
                if (requestBodyContents != null)
                {
                    foreach (var requestBodyContent in requestBodyContents)
                    {
                        if (requestBodyContent.Value.Schema?.Reference?.Id != null)
                        {
                            var schema = context.SchemaRepository.Schemas[requestBodyContent.Value.Schema?.Reference?.Id];
                            if (schema != null)
                            {
                                requestBodyContent.Value.Schema.Description = schema.Description;
                                requestBodyContent.Value.Schema.Extensions = schema.Extensions;
                            }
                        }
                    }
                }
            }
        }

        #endregion
    }
}
