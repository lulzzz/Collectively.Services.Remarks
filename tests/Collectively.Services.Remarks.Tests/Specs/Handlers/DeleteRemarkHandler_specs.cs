﻿using System;
using Collectively.Messages.Commands;
using Collectively.Common.Domain;
using Collectively.Common.Services;
using It = Machine.Specifications.It;
using RawRabbit;
using Moq;
using Collectively.Services.Remarks.Services;
using Collectively.Services.Remarks.Handlers;
using Collectively.Messages.Commands.Remarks;
using Collectively.Messages.Events.Remarks;
using Machine.Specifications;
using RawRabbit.Pipe;
using System.Threading;

namespace Collectively.Services.Remarks.Tests.Specs.Handlers
{
    public abstract class DeleteRemarkHandler_specs
    {
        protected static DeleteRemarkHandler DeleteRemarkHandler;
        protected static IHandler Handler;
        protected static Mock<IBusClient> BusClientMock;
        protected static Mock<IRemarkService> RemarkServiceMock;
        protected static Mock<IExceptionHandler> ExceptionHandlerMock;
        protected static Mock<IResourceFactory> ResourceFactoryMock;
        protected static DeleteRemark Command;
        protected static Exception Exception;

        protected static void Initialize()
        {
            ExceptionHandlerMock = new Mock<IExceptionHandler>();
            Handler = new Handler(ExceptionHandlerMock.Object);
            BusClientMock = new Mock<IBusClient>();
            RemarkServiceMock = new Mock<IRemarkService>();
            ResourceFactoryMock = new Mock<IResourceFactory>();
            Command = new DeleteRemark
            {
                Request = new Request
                {
                    Name = "delete_remark",
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.Now,
                    Origin = "test",
                    Resource = ""
                },
                UserId = "userId",
                RemarkId = Guid.NewGuid()
            };
            DeleteRemarkHandler = new DeleteRemarkHandler(Handler, BusClientMock.Object, 
                RemarkServiceMock.Object, ResourceFactoryMock.Object);
        }
    }

    [Subject("DeleteRemarkHandler HandleAsync")]
    public class when_invoking_delete_remark_handle_async : DeleteRemarkHandler_specs
    {
        Establish context = () =>
        {
            Initialize();
        };

        Because of = () => DeleteRemarkHandler.HandleAsync(Command).Await();

        It should_call_delete_async_on_remark_service = () =>
        {
            RemarkServiceMock.Verify(x => x.DeleteAsync(Command.RemarkId), Times.Once);
        };

        It should_publish_remark_deleted_event = () =>
        {
            BusClientMock.Verify(x => x.PublishAsync(Moq.It.IsAny<RemarkDeleted>(), 
                Moq.It.IsAny<Action<IPipeContext>>(),
                Moq.It.IsAny<CancellationToken>()), Times.Once);
        };
    }


    [Subject("DeleteRemarkHandler HandleAsync")]
    public class when_invoking_delete_remark_handle_async_and_service_throws_exception : DeleteRemarkHandler_specs
    {
        protected static string ErrorCode = "Error"; 

        Establish context = () =>
        {
            Initialize();
            RemarkServiceMock.Setup(x => x.DeleteAsync(Command.RemarkId))
                .Throws(new ServiceException(ErrorCode));
        };

        Because of = () => Exception = Catch.Exception(() => DeleteRemarkHandler.HandleAsync(Command).Await());

        It should_call_delete_async_on_remark_service = () =>
        {
            RemarkServiceMock.Verify(x => x.DeleteAsync(Command.RemarkId), Times.Once);
        };

        It should_not_publish_remark_deleted_event = () =>
        {
            BusClientMock.Verify(x => x.PublishAsync(Moq.It.IsAny<RemarkDeleted>(),
                Moq.It.IsAny<Action<IPipeContext>>(),
                Moq.It.IsAny<CancellationToken>()), Times.Never);
        };

        It should_publish_delete_remark_rejected_message = () =>
        {
            BusClientMock.Verify(x => x.PublishAsync(Moq.It.Is<DeleteRemarkRejected>(m =>
                    m.RequestId == Command.Request.Id
                    && m.RemarkId == Command.RemarkId
                    && m.UserId == Command.UserId
                    && m.Code == ErrorCode),
                Moq.It.IsAny<Action<IPipeContext>>(),
                Moq.It.IsAny<CancellationToken>()), Times.Once);
        };
    }
}