﻿using Collectively.Common.Domain;
using Collectively.Common.Files;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Repositories;
using Collectively.Services.Remarks.Services;
using Machine.Specifications;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Collectively.Services.Remarks.Settings;
using It = Machine.Specifications.It;

namespace Collectively.Services.Remarks.Tests.Specs
{
    public abstract class RemarkService_specs
    {
        protected static IRemarkService RemarkService;
        protected static Mock<IFileHandler> FileHandlerMock;
        protected static Mock<ICategoryRepository> RemarkCategoryRepositoryMock;
        protected static Mock<IRemarkRepository> RemarkRepositoryMock;
        protected static Mock<ITagRepository> TagRepositoryMock;
        protected static Mock<IUserRepository> UserRepositoryMock;
        protected static Mock<ICategoryRepository> CategoryRepositoryMock;
        protected static Mock<IImageService> ImageServiceMock;
        protected static Mock<IRemarkPhotoService> RemarkPhotoServiceMock;
        protected static Mock<IUniqueNumberGenerator> UniqueNumberGeneratorMock;
        protected static GeneralSettings GeneralSettings;
        protected static string UserId = "userId";
        protected static User User = new User(UserId, "TestUser", "user");
        protected static File File = File.Create("image.png", "image/png", new byte[] { 1, 2, 3, 4 });
        protected static Guid RemarkId = Guid.NewGuid();
        protected static Location Location = Location.Zero;
        protected static Remark Remark;
        protected static Exception Exception;

        protected static void Initialize()
        {
            FileHandlerMock = new Mock<IFileHandler>();
            RemarkRepositoryMock = new Mock<IRemarkRepository>();
            RemarkCategoryRepositoryMock = new Mock<ICategoryRepository>();
            TagRepositoryMock = new Mock<ITagRepository>();
            UserRepositoryMock = new Mock<IUserRepository>();
            CategoryRepositoryMock = new Mock<ICategoryRepository>();
            ImageServiceMock = new Mock<IImageService>();
            RemarkPhotoServiceMock = new Mock<IRemarkPhotoService>();
            UniqueNumberGeneratorMock = new Mock<IUniqueNumberGenerator>();
            GeneralSettings = new GeneralSettings
            {
                AllowedDistance = 15.0
            };

            RemarkService = new RemarkService(RemarkRepositoryMock.Object, 
                UserRepositoryMock.Object,
                CategoryRepositoryMock.Object,
                TagRepositoryMock.Object,
                RemarkPhotoServiceMock.Object,
                GeneralSettings);

            var user = new User(UserId, "name", "user");
            var category = new Category("category");
            Remark = new Remark(RemarkId, user, category, Location);
            Remark.AddPhoto(RemarkPhoto.Small(Guid.NewGuid(), "test.jpg", "http://my-test-image.com"));

            RemarkRepositoryMock.Setup(x => x.GetByIdAsync(Moq.It.IsAny<Guid>()))
                .ReturnsAsync(Remark);
            UserRepositoryMock.Setup(x => x.GetByUserIdAsync(Moq.It.IsAny<string>()))
                .ReturnsAsync(User);
            ImageServiceMock.Setup(x => x.ProcessImage(Moq.It.IsAny<File>()))
                .Returns(new Dictionary<string, File>
                {
                    {"small", File},
                    {"medium", File},
                    {"big", File}
                });
        }
    }

    [Subject("RemarkService DeleteAsync")]
    public class when_delete_async_is_invoked : RemarkService_specs
    {
        Establish context = () => Initialize();

        Because of = () => RemarkService.DeleteAsync(RemarkId).Await();

        It should_call_remove_photos_async_on_remark_photo_service = () =>
        {
            RemarkPhotoServiceMock.Verify(x => x.RemovePhotosAsync(RemarkId, Moq.It.IsAny<string[]>()), Times.Once);
        };

        It should_call_delete_async_on_remark_repository = () =>
        {
            RemarkRepositoryMock.Verify(x => x.DeleteAsync(Moq.It.IsAny<Remark>()), Times.Once);
        };
    }

    [Subject("RemarkService DeleteAsync")]
    public class when_delete_async_is_invoked_but_remark_doesnt_exist : RemarkService_specs
    {
        Establish context = () =>
        {
            Initialize();
            RemarkRepositoryMock.Setup(x => x.GetByIdAsync(Moq.It.IsAny<Guid>()))
                .ReturnsAsync(() => null);
        };

        Because of = () => Exception = Catch.Exception(() => RemarkService.DeleteAsync(RemarkId).Await());

        It should_throw_service_exception = () =>
        {
            Exception.ShouldBeOfExactType<ServiceException>();
        };

        It should_not_call_delete_async_on_file_handler = () =>
        {
            FileHandlerMock.Verify(x => x.DeleteAsync(Moq.It.IsAny<string>()), Times.Never);
        };

        It should_not_call_delete_async_on_remark_repository = () =>
        {
            RemarkRepositoryMock.Verify(x => x.DeleteAsync(Moq.It.IsAny<Remark>()), Times.Never);
        };
    }
}