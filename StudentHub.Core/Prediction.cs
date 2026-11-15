namespace StudentHub.Core;

public record Prediction(decimal Required, bool Possible);
public static class GradePrediction
{
    public static Result<Prediction> RequiredFinal(decimal current, decimal finalWeight, decimal target)
    {
        if (current is < 0 or > 100 || target is < 0 or > 100)
            return new Fail<Prediction>("Grades must be between 0 and 100.");
        if (finalWeight is <= 0 or > 100)
            return new Fail<Prediction>("Final weight must be between 1 and 100 percent.");
        var weight = finalWeight / 100m;
        var needed = Math.Max(0, (target - current * (1 - weight)) / weight);
        return new Ok<Prediction>(new(decimal.Ceiling(needed * 100m) / 100m, needed <= 100m));
    }
}
