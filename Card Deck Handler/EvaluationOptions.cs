namespace Card_Deck_Handler.Evaluation;

public sealed class EvaluationOptions
{
    public int FlushMinCards { get; init; } = 5;
    public int StraightMinLength { get; init; } = 5;
    public bool AceLowStraight { get; init; } = true;
    public bool AllowFiveOfAKind { get; init; } = true;
    public int HandSize { get; init; } = 5;
    public bool EvaluateBestSubset { get; init; } = false;

    public static readonly EvaluationOptions StandardPoker = new()
    {
        FlushMinCards = 5,
        StraightMinLength = 5,
        AceLowStraight = true,
        AllowFiveOfAKind = false,
        HandSize = 5,
        EvaluateBestSubset = false
    };

    public static readonly EvaluationOptions WildCardPoker = new()
    {
        FlushMinCards = 5,
        StraightMinLength = 5,
        AceLowStraight = true,
        AllowFiveOfAKind = true,
        HandSize = 5,
        EvaluateBestSubset = false
    };

    public static readonly EvaluationOptions TexasHoldem = new()
    {
        FlushMinCards = 5,
        StraightMinLength = 5,
        AceLowStraight = true,
        AllowFiveOfAKind = false,
        HandSize = 5,
        EvaluateBestSubset = true
    };
    
    public static readonly EvaluationOptions Rummy = new()
    {
        FlushMinCards = 3,
        StraightMinLength = 3,
        AceLowStraight = true,
        AllowFiveOfAKind = false,
        HandSize = 7,
        EvaluateBestSubset = false
    };
}