using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace ClimbUpAPI.Services
{
    public interface ICloudinaryService
    {
        Task<string> UploadImageAsync(IFormFile imageFile, string folderName);
    }
}