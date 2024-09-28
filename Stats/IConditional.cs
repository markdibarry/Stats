namespace GameCore.Statistics;

public interface IConditional
{
    StatsBase? Stats { get; }
    void OnConditionChanged(Condition condition);
}