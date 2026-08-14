namespace Commerce.Reviews.Domain;

public static class RatingScale
{
    public const int MinRating = 1;
    public const int MaxRating = 5;

    public static bool IsValid(int rating) => rating >= MinRating && rating <= MaxRating;
}
