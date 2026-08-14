using Commerce.Framework.Core.Entities;
using Commerce.Promotions.Domain.Enums;

namespace Commerce.Promotions.Domain.Entities;

public sealed class PromotionCondition : Entity
{
    public const int ParametersMaxLength = 4000;

    public int PromotionId { get; private set; }

    public PromotionConditionType ConditionType { get; private set; }

    public string ParametersJson { get; private set; } = "{}";

    private PromotionCondition()
    {
    }

    public static PromotionCondition Create(int promotionId, PromotionConditionType conditionType, string parametersJson)
    {
        if (promotionId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(promotionId));
        }

        if (string.IsNullOrWhiteSpace(parametersJson))
        {
            parametersJson = "{}";
        }

        if (parametersJson.Length > ParametersMaxLength)
        {
            throw new ArgumentException($"Parameters cannot exceed {ParametersMaxLength} characters.");
        }

        return new PromotionCondition
        {
            PromotionId = promotionId,
            ConditionType = conditionType,
            ParametersJson = parametersJson
        };
    }
}
