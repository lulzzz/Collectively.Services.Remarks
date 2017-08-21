﻿using Collectively.Common.Files;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Handlers;
using Collectively.Services.Remarks.Services;
using Machine.Specifications;
using Moq;
using RawRabbit;
using System;
using Collectively.Messages.Commands;
using Collectively.Common.Services;
using Collectively.Services.Remarks.Settings;
using Collectively.Messages.Commands.Remarks;
using Collectively.Messages.Events.Remarks;
using It = Machine.Specifications.It;
using System.Collections.Generic;
using Collectively.Common.Types;
using RawRabbit.Pipe;
using System.Threading;

namespace Collectively.Services.Remarks.Tests.Specs.Handlers
{
    public abstract class AddPhotosToRemarkHandler_specs : SpecsBase
    {
        protected static AddPhotosToRemarkHandler AddPhotosToRemarkHandler;
        protected static IHandler Handler;
        protected static Mock<IFileResolver> FileResolverMock;
        protected static Mock<IFileValidator> FileValidatorMock;
        protected static Mock<IRemarkService> RemarkServiceMock;
        protected static Mock<IRemarkPhotoService> RemarkPhotoServiceMock;
        protected static Mock<IExceptionHandler> ExceptionHandlerMock;
        protected static Mock<IResourceFactory> ResourceFactoryMock;
        protected static AddPhotosToRemark Command;
        protected static GeneralSettings GeneralSettings;
        protected static File File;
        protected static Exception Exception;

        protected static void Initialize()
        {
            InitializeBus();
            ExceptionHandlerMock = new Mock<IExceptionHandler>();
            Handler = new Handler(ExceptionHandlerMock.Object);
            FileResolverMock = new Mock<IFileResolver>();
            FileValidatorMock = new Mock<IFileValidator>();
            RemarkServiceMock = new Mock<IRemarkService>();
            RemarkPhotoServiceMock = new Mock<IRemarkPhotoService>();
            ResourceFactoryMock = new Mock<IResourceFactory>();
            GeneralSettings = new GeneralSettings
            {
                PhotosLimit = 2
            };
            Command = new AddPhotosToRemark
            {
                RemarkId = Guid.NewGuid(),
                Request = new Request
                {
                    Name = "add_photos_to_remark",
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.Now,
                    Origin = "test",
                    Resource = ""
                },
                UserId = "userId",
                Photos = new List<Collectively.Messages.Commands.Models.File>()
            };
            AddPhotosToRemarkHandler = new AddPhotosToRemarkHandler(Handler, BusClientMock.Object, 
                RemarkServiceMock.Object, RemarkPhotoServiceMock.Object, FileResolverMock.Object,  
                FileValidatorMock.Object, GeneralSettings, ResourceFactoryMock.Object);
        }
    }

    [Subject("AddPhotosToRemarkHandler HandleAsync")]
    public class when_invoking_add_photos_to_remark_handle_async_without_file : AddPhotosToRemarkHandler_specs
    {
        Establish context = () =>
        {
            Initialize();
            FileResolverMock.Setup(x => x.FromBase64(Moq.It.IsAny<string>(),
            Moq.It.IsAny<string>(), Moq.It.IsAny<string>())).Returns(Maybe<File>.Empty);
        };

        Because of = () => AddPhotosToRemarkHandler.HandleAsync(Command).Await();

        It should_publish_add_photos_to_remark_rejected_message = () =>
        {
            VerifyPublishAsync(Moq.It.Is<AddPhotosToRemarkRejected>(m =>
                    m.RequestId == Command.Request.Id
                    && m.RemarkId == Command.RemarkId
                    && m.UserId == Command.UserId
                    && m.Code == OperationCodes.NoFiles), Times.Once());
        };
    }

    [Subject("AddPhotosToRemarkHandler HandleAsync")]
    public class when_invoking_add_photos_to_remark_handle_async_with_invalid_file : AddPhotosToRemarkHandler_specs
    {
        Establish context = () =>
        {
            Initialize();
            Command.Photos.Add(new Collectively.Messages.Commands.Models.File
                {
                    Base64 = "base64",
                    Name = "remark.jpg",
                    ContentType = "image/jpeg"
                });
            File = File.Create(Command.Photos[0].Name, Command.Photos[0].ContentType, new byte[] { 0x1 });
            FileResolverMock.Setup(x => x.FromBase64(Moq.It.IsAny<string>(),
                Moq.It.IsAny<string>(), Moq.It.IsAny<string>())).Returns(File);
            FileValidatorMock.Setup(x => x.IsImage(File)).Returns(false);
        };

        Because of = () => AddPhotosToRemarkHandler.HandleAsync(Command).Await();

        It should_not_call_add_photos_async_on_remark_photo_service = () =>
        {
            RemarkPhotoServiceMock.Verify(x => x.AddPhotosAsync(Moq.It.IsAny<Guid>(), Command.UserId, File), Times.Never());
        };

        It should_publish_add_photos_to_remark_rejected_message = () =>
        {
            VerifyPublishAsync(Moq.It.Is<AddPhotosToRemarkRejected>(m =>
                    m.RequestId == Command.Request.Id
                    && m.RemarkId == Command.RemarkId
                    && m.UserId == Command.UserId
                    && m.Code == OperationCodes.InvalidFile), Times.Once());
        };
    }

    [Subject("AddPhotosToRemarkHandler HandleAsync")]
    public class when_invoking_add_photos_to_remark_handle_async_with_too_many_files : AddPhotosToRemarkHandler_specs
    {
        Establish context = () =>
        {
            Initialize();
            Command.Photos.Add(new Collectively.Messages.Commands.Models.File
                {
                    Base64 = "base64",
                    Name = "remark1.jpg",
                    ContentType = "image/jpeg"
                });
            Command.Photos.Add(new Collectively.Messages.Commands.Models.File
                {
                    Base64 = "base64",
                    Name = "remark2.jpg",
                    ContentType = "image/jpeg"
                });
            Command.Photos.Add(new Collectively.Messages.Commands.Models.File
                {
                    Base64 = "base64",
                    Name = "remark3.jpg",
                    ContentType = "image/jpeg"
                });
            FileValidatorMock.Setup(x => x.IsImage(Moq.It.IsAny<File>())).Returns(true);
        };

        Because of = () => AddPhotosToRemarkHandler.HandleAsync(Command).Await();

        It should_not_call_add_photos_async_on_remark_photo_service = () =>
        {
            RemarkPhotoServiceMock.Verify(x => x.AddPhotosAsync(Moq.It.IsAny<Guid>(), Command.UserId, File), Times.Never());
        };

        It should_publish_add_photos_to_remark_rejected_message = () =>
        {
            VerifyPublishAsync(Moq.It.Is<AddPhotosToRemarkRejected>(m =>
                    m.RequestId == Command.Request.Id
                    && m.RemarkId == Command.RemarkId
                    && m.UserId == Command.UserId
                    && m.Code == OperationCodes.TooManyFiles), Times.Once());
        };
    }


    [Subject("AddPhotosToRemarkHandler HandleAsync")]
    public class when_invoking_add_photos_to_remark_handle_async_with_valid_file : AddPhotosToRemarkHandler_specs
    {
        static Remark Remark;
        
        Establish context = () =>
        {
            Initialize();
            Remark = new Remark(Guid.NewGuid(), new User(Command.UserId, "user", "user"),
                new Category("test"), Location.Create(1, 1, "Address"), "Test");
            Command.Photos.Add(new Collectively.Messages.Commands.Models.File
                {
                    Base64 = "base64",
                    Name = "remark.jpg",
                    ContentType = "image/jpeg"
                });
            File = File.Create(Command.Photos[0].Name, Command.Photos[0].ContentType, new byte[] { 0x1 });
            FileResolverMock.Setup(x => x.FromBase64(Moq.It.IsAny<string>(),
                Moq.It.IsAny<string>(), Moq.It.IsAny<string>())).Returns(File);
            FileValidatorMock.Setup(x => x.IsImage(File)).Returns(true);
            RemarkServiceMock.Setup(x => x.GetAsync(Moq.It.IsAny<Guid>())).ReturnsAsync(Remark);
        };

        Because of = () => AddPhotosToRemarkHandler.HandleAsync(Command).Await();

        It should_call_add_photos_async_on_remark_photo_service = () =>
        {
            RemarkPhotoServiceMock.Verify(x => x.AddPhotosAsync(Moq.It.IsAny<Guid>(), Command.UserId,  File), Times.Once());
        };

        It should_publish_photos_to_remark_added_message = () =>
        {
            VerifyPublishAsync(Moq.It.Is<PhotosToRemarkAdded>(m =>
                    m.RequestId == Command.Request.Id
                    && m.RemarkId == Command.RemarkId
                    && m.UserId == Command.UserId), Times.Once());
        };
    }
}