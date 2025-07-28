using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApi
{
    [Route("api/[controller]")]
    [ApiController]
    public class PhotoController : ControllerBase
    {
        [HttpGet("image")]
        public IActionResult GetImage([FromBody] CoreApp.Photo photo)
        {
            if (photo == null || photo.ImageData == null)
            {
                return NotFound();
            }
            return File(photo.ImageData, "image/jpeg", photo.FileName + ".jpg");
        }
    }
}
