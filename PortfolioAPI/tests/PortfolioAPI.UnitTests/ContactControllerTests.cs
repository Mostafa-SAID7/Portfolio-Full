using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PortfolioAPI.Application.DTOs;
using PortfolioAPI.Application.Interfaces;
using PortfolioAPI.Controllers;
using PortfolioAPI.Core.Common;
using PortfolioAPI.Core.Entities;
using Xunit;

namespace PortfolioAPI.UnitTests
{
    public class ContactControllerTests
    {
        private static (ContactController Controller, Mock<IContactService> Service) Create()
        {
            var service = new Mock<IContactService>();
            var controller = new ContactController(service.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };
            return (controller, service);
        }

        [Fact]
        public async Task GetAllContacts_ShouldReturnPagedPage_AndExposePaginationHeaders()
        {
            // Arrange — page 1 of a 42-message inbox, newest first.
            var (controller, service) = Create();
            var contacts = new List<Contact>
            {
                new Contact { Id = 2, Name = "Jane", CreatedAt = DateTime.UtcNow },
                new Contact { Id = 1, Name = "John", CreatedAt = DateTime.UtcNow.AddDays(-1) }
            };
            service
                .Setup(s => s.GetPagedContactsAsync(1, 25, null))
                .ReturnsAsync(new PagedResult<Contact>(contacts, 42, 1, 25));

            // Act
            var result = await controller.GetAllContacts();

            // Assert
            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeAssignableTo<IEnumerable<ContactDto>>()
                .Which.Should().HaveCount(2);
            controller.Response.Headers["X-Total-Count"].ToString().Should().Be("42");
            controller.Response.Headers["X-Page"].ToString().Should().Be("1");
            controller.Response.Headers["X-Page-Size"].ToString().Should().Be("25");
            service.Verify(s => s.GetPagedContactsAsync(1, 25, null), Times.Once);
        }

        [Fact]
        public async Task GetAllContacts_ShouldClampAbusivePageAndPageSize()
        {
            // Arrange — page=0 becomes 1; pageSize is capped at 100.
            var (controller, service) = Create();
            service
                .Setup(s => s.GetPagedContactsAsync(1, 100, null))
                .ReturnsAsync(new PagedResult<Contact>(new List<Contact>(), 0, 1, 100));

            // Act
            await controller.GetAllContacts(page: 0, pageSize: 1000);

            // Assert
            service.Verify(s => s.GetPagedContactsAsync(1, 100, null), Times.Once);
        }

        [Fact]
        public async Task GetAllContacts_WithUnreadFilter_ShouldForwardFilterAndPage()
        {
            // Arrange
            var (controller, service) = Create();
            service
                .Setup(s => s.GetPagedContactsAsync(2, 8, true))
                .ReturnsAsync(new PagedResult<Contact>(new List<Contact>(), 3, 2, 8));

            // Act
            await controller.GetAllContacts(page: 2, pageSize: 8, unread: true);

            // Assert
            service.Verify(s => s.GetPagedContactsAsync(2, 8, true), Times.Once);
        }
    }
}
