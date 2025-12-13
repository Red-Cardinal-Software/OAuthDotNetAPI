using FluentValidation;
using FluentValidation.Results;

namespace Application.Validators;

/// <summary>
/// Provides extension methods for enhancing FluentValidation functionalities,
/// such as ruleset-specific validation and rule configuration with custom messages.
/// </summary>
public static class FluentExtensions
{
    /// <summary>
    /// Validates the specified model using the given ruleset name.
    /// </summary>
    /// <typeparam name="T">The type of the model to validate.</typeparam>
    /// <param name="validator">The validator instance that performs validation.</param>
    /// <param name="model">The object to be validated.</param>
    /// <param name="rulesetName">The name of the ruleset to use during validation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the validation result.</returns>
    public static Task<ValidationResult> ValidateWithRuleset<T>(this IValidator<T> validator, T model,
        string rulesetName) =>
        validator.ValidateAsync(model, options => options.IncludeRuleSets(rulesetName).IncludeRulesNotInRuleSet());

    /// <summary>
    /// Configures all rules within a rule builder to use the specified custom message.
    /// </summary>
    /// <typeparam name="T">The type of the object being validated.</typeparam>
    /// <typeparam name="TProperty">The type of the property being validated.</typeparam>
    /// <param name="ruleBuilder">The rule builder to configure.</param>
    /// <param name="message">The custom message to apply to all rules.</param>
    /// <returns>The configured rule builder options.</returns>
    public static IRuleBuilderOptions<T, TProperty> WithMessageForAllRules<T, TProperty>(
        this IRuleBuilderOptions<T, TProperty> ruleBuilder, string message) =>
        ruleBuilder.Configure(x => x.MessageBuilder = _ => message);
}
