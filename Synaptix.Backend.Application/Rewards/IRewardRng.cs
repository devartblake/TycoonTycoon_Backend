namespace Synaptix.Backend.Application.Rewards;

public interface IRewardRng
{
    // Returns a value in [0.0, 1.0)
    double NextDouble();
}
