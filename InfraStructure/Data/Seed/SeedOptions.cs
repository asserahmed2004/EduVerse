namespace InfraStructure.Data.Seed
{
    public class SeedOptions
    {
        public const string SectionName = "DataSeeding";

        public bool Enabled { get; set; } = true;
        public bool RunOnStartup { get; set; } = true;
        public string SeedPassword { get; set; } = "Seed@12345";
    }
}
