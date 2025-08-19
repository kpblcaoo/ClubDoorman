namespace ClubDoorman.Baseline.Golden;

public interface IGoldenBaselineSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
