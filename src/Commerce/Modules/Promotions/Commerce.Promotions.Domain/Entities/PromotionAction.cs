using Commerce.Framework.Core.Entities;
using Commerce.Promotions.Domain.Enums;

namespace Commerce.Promotions.Domain.Entities;

public sealed class PromotionAction : Entity
{
    public const int ParametersMaxLength = 4000;

    public int PromotionId { get; private set; }

    public PromotionActionType ActionType { get; private set; }

    public PromotionTargetScope TargetScope { get; private set; }

    public string ParametersJson { get; private set; } = "{}";

    private PromotionAction()
    {
    }

    public static PromotionAction Create(
        int promotionId,
        PromotionActionType actionType,
        PromotionTargetScope targetScope,
        string parametersJson)
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

        return new PromotionAction
        {
            PromotionId = promotionId,
            ActionType = actionType,
            TargetScope = targetScope,
            ParametersJson = parametersJson
        };
    }
}
