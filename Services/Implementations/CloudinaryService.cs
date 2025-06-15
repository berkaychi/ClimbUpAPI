using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using ClimbUpAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace ClimbUpAPI.Services.Implementations
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IOptions<CloudinarySettings> cloudinaryConfig)
        {
            var account = new Account(
                cloudinaryConfig.Value.CloudName,
                cloudinaryConfig.Value.ApiKey,
                cloudinaryConfig.Value.ApiSecret
            );
            _cloudinary = new Cloudinary(account);
        }

        public async Task<string> UploadImageAsync(IFormFile imageFile, string folderName)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                return string.Empty;
            }

            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(imageFile.FileName, imageFile.OpenReadStream()),
                Folder = folderName,
                Transformation = new Transformation().Height(500).Width(500).Crop("fill").Gravity("face")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                throw new System.Exception($"Cloudinary upload failed: {uploadResult.Error.Message}");
            }

            return uploadResult.SecureUrl.ToString();
        }
    }
}