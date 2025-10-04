using Microsoft.AspNetCore.Mvc;
using OpenAI;
using OpenAI.Chat;
using System.Text.Json;

namespace ShelfLifeApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImageProcessorController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly OpenAIClient _client;
        private readonly string systemPrompt = $"Analyze the image carefully and extract all printed text related to the product’s manufacturing and expiry details." + 
        "Return the result strictly in JSON format with the following keys if present:" + 
        "{" + 
            "\"ProductName\": \"\"," +
            "\"NetWeight\": \"\", " +
            "\"PackedDate\": \"\", " +
            "\"UseBy\": \"\", " +
            "\"BatchNo\": \"\", " +
            "\"MRP\": \"\", " +
            "\"OtherDetails\": \"\" " +
        "}" +
        "ProductName: The exact printed name of the product (e.g., “Everest Chaat Masala”)." +
        "NetWeight: The printed weight (e.g., “50g”)." +
"--PackedDate: The date of packaging or manufacturing as printed." +
"--UseBy: The expiry or “best before/use by” date." +
"--BatchNo: The batch number exactly as printed." +
"--MRP: The price as printed (e.g., “₹85.00”)." +
"--OtherDetails: Capture any other relevant text (like “Mixed Masala Powder”, “Made in India”, “USP ₹”, etc.)." +
"Be careful to correctly identify each field even if they are printed in different sections of the image or in short forms." +
"Ensure the dates are clearly labeled and consistent with the field meaning (e.g., “MAY24”, “JUL25” → PackedDate: \"May 2024\", UseBy: \"July 2025\")." +
"If any field is missing, return it with an empty string (\"\") but do not omit it." +
"Output only clean JSON with no extra commentary or text.";

        public ImageProcessorController(IConfiguration config)
        {
            _config = config;
            _client = new OpenAIClient(config["OpenAI:ApiKey"]);
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessImage(IFormFile image)
        {
            if (image == null || image.Length == 0)
                return BadRequest("No image uploaded.");

           byte[] imageByte;
           using(var ms = new MemoryStream())
           {
                await image.CopyToAsync(ms);
                imageByte = ms.ToArray();
            }
            var chatClient = _client.GetChatClient("gpt-4o-mini");
            var binaryData = BinaryData.FromBytes(imageByte);

            var imagePrompt = ChatMessage.CreateUserMessage(ChatMessageContentPart.CreateImagePart(
                    binaryData,
                    "image/jpeg",
                    ChatImageDetailLevel.High
            ));
            var messages = new List<ChatMessage>{systemPrompt, imagePrompt};
            ChatCompletion response = await chatClient.CompleteChatAsync(messages);

            var text = response.Content[0].Text;

            return Ok(new { raw = text});
        }
    }
}
