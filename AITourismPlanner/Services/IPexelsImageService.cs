namespace AITourismPlanner.Services
{
    public interface IPexelsImageService
    {
        Task<string> GetImageForDestinationAsync(string destinationName);
        Task<List<string>> GetMultipleImagesAsync(string destinationName, int count = 5);
    }
}