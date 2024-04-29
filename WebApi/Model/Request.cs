namespace WebApi.Model
{
    public record Request
    {
        public DateTime StartingTime { get; init; }
        public UserSettings UserSettings { get; init; } = new UserSettings();
        public CarData CarData { get; init; } = new CarData();
    }
}
