// <copyright file="AliasValidator.cs" company="Pexip">
// Copyright (c) Pexip. All rights reserved.
// </copyright>

namespace PulseDemoApp.Utilities;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

// Refer to https://docs.pexip.com/admin/about_aliases.htm
public sealed class AliasValidator
{
    // FQDN provided
    public bool ValidateFullyQualifiedAlias(string alias)
    {
        var context = new ValidationContext(alias);
        var attributes = new List<ValidationAttribute>
        {
            new RequiredAttribute(),
            new EmailAddressAttribute(),
        };

        var results = new List<ValidationResult>();
        bool qualified = Validator.TryValidateValue(alias, context, results, attributes);

        return qualified;
    }

    // FQDN inferred
    public bool ValidateRegisteredAlias(string address)
    {
        var context = new ValidationContext(address);
        var attributes = new List<ValidationAttribute>
        {
            new RequiredAttribute(),
            new RegularExpressionAttribute(@"^[\p{L}\p{N}_@.-]*$"), // Match Unicode letters, numbers, dashes, underscore at signs and periods
        };

        var results = new List<ValidationResult>();
        bool qualified = Validator.TryValidateValue(address, context, results, attributes);

        return qualified;
    }
}
