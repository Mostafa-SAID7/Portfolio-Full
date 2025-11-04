// ---------------------------------------
// Email: quickapp@ebenmonney.com
// Templates: www.ebenmonney.com/templates
// (c) 2024 www.ebenmonney.com/mit-license
// ---------------------------------------

using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Quick_Application1.Core.Services.Portfolio;
using Quick_Application1.Server.Services.Email;
using Quick_Application1.Server.ViewModels.Portfolio;

namespace Quick_Application1.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PortfolioController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IEmailSender _emailSender;
        private readonly IPortfolioService _portfolioService;

        public PortfolioController(IMapper mapper, ILogger<PortfolioController> logger, IEmailSender emailSender,
            IPortfolioService portfolioService)
        {
            _mapper = mapper;
            _logger = logger;
            _emailSender = emailSender;
            _portfolioService = portfolioService;
        }

        [HttpGet("sections")]
        public IActionResult GetSections()
        {
            var sections = _portfolioService.GetAllSections();
            return Ok(_mapper.Map<IEnumerable<PortfolioSectionVM>>(sections));
        }

        [HttpGet("items")]
        public IActionResult GetItems()
        {
            var items = _portfolioService.GetAllItems();
            return Ok(_mapper.Map<IEnumerable<PortfolioItemVM>>(items));
        }

        [HttpGet("sections/{sectionName}")]
        public IActionResult GetSection(string sectionName)
        {
            var section = _portfolioService.GetSectionByName(sectionName);
            if (section == null)
                return NotFound();

            return Ok(_mapper.Map<PortfolioSectionVM>(section));
        }

        [HttpGet("items/{type}")]
        public IActionResult GetItemsByType(string type)
        {
            var items = _portfolioService.GetItemsByType(type);
            return Ok(_mapper.Map<IEnumerable<PortfolioItemVM>>(items));
        }

        [HttpPost("contact")]
        public async Task<IActionResult> SendContactMessage([FromBody] ContactMessageVM message)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Send email notification
            var emailBody = $@"
                New Contact Message from Portfolio:

                Name: {message.Name}
                Email: {message.Email}
                Subject: {message.Subject}

                Message:
                {message.Message}

                Sent at: {DateTime.UtcNow}
            ";

            var (success, errorMsg) = await _emailSender.SendEmailAsync(
                "Portfolio Contact", // recipient name
                "your-email@example.com", // replace with your email
                $"Portfolio Contact: {message.Subject}",
                emailBody);

            if (success)
                return Ok(new { message = "Message sent successfully" });
            else
                return BadRequest(new { message = $"Failed to send message: {errorMsg}" });
        }
    }
}
