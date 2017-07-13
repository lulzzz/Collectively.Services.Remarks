﻿using System.Threading.Tasks;
using Collectively.Messages.Commands;
using Collectively.Common.Domain;
using Collectively.Common.Files;
using Collectively.Common.Services;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Services;
using Collectively.Messages.Commands.Remarks;
using Collectively.Messages.Events.Remarks;
using NLog;
using RawRabbit;
using RemarkState = Collectively.Services.Remarks.Domain.RemarkState;

namespace Collectively.Services.Remarks.Handlers
{
    public class ResolveRemarkHandler : ICommandHandler<ResolveRemark>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IHandler _handler;
        private readonly IBusClient _bus;
        private readonly IRemarkService _remarkService;
        private readonly IRemarkStateService _remarkStateService;
        private readonly IFileResolver _fileResolver;
        private readonly IFileValidator _fileValidator;
        private readonly IResourceFactory _resourceFactory;

        public ResolveRemarkHandler(IHandler handler,
            IBusClient bus,
            IRemarkService remarkService,
            IRemarkStateService remarkStateService,
            IFileResolver fileResolver,
            IFileValidator fileValidator,
            IResourceFactory resourceFactory)
        {
            _handler = handler;
            _bus = bus;
            _remarkService = remarkService;
            _remarkStateService = remarkStateService;
            _fileResolver = fileResolver;
            _fileValidator = fileValidator;
            _resourceFactory = resourceFactory;
        }

        public async Task HandleAsync(ResolveRemark command)
        {
            File file = null;
            
            await _handler.Validate(() => 
                {
                    if (command.ValidatePhoto)
                    {
                        var resolvedFile = _fileResolver.FromBase64(command.Photo.Base64, command.Photo.Name, command.Photo.ContentType);
                        if (resolvedFile.HasNoValue)
                        {
                            Logger.Error($"File cannot be resolved from base64, photoName:{command.Photo.Name}, " +
                                $"contentType:{command.Photo.ContentType}, userId:{command.UserId}");
                            throw new ServiceException(OperationCodes.CannotConvertFile);
                        }
                        file = resolvedFile.Value;
                        var isImage = _fileValidator.IsImage(file);
                        if (isImage == false)
                        {
                            Logger.Warn($"File is not an image! name:{file.Name}, contentType:{file.ContentType}, " +
                                $"userId:{command.UserId}");
                            throw new ServiceException(OperationCodes.InvalidFile);
                        }
                    }
                })
                .Run(async () =>
                {
                    Location location = null;
                    if (command.Latitude != 0 && command.Longitude != 0)
                    {
                        location = Location.Create(command.Latitude, command.Longitude, command.Address);
                    }
                    await _remarkStateService.ResolveAsync(command.RemarkId, command.UserId, command.Description, 
                            location, file, command.ValidateLocation);
                })
                .OnSuccess(async () =>
                {
                    var remark = await _remarkService.GetAsync(command.RemarkId);
                    var state = remark.Value.GetLatestStateOf(RemarkState.Names.Resolved).Value;
                    var resource = _resourceFactory.Resolve<RemarkResolved>(command.RemarkId);
                    await _bus.PublishAsync(new RemarkResolved(command.Request.Id, resource, 
                        command.UserId, command.RemarkId));
                })
                .OnCustomError(async ex => await _bus.PublishAsync(new ResolveRemarkRejected(command.Request.Id,
                    command.UserId, command.RemarkId, ex.Code, ex.Message)))
                .OnError(async (ex, logger) =>
                {
                    logger.Error(ex, "Error occured while resolving a remark.");
                    await _bus.PublishAsync(new ResolveRemarkRejected(command.Request.Id,
                        command.UserId, command.RemarkId, OperationCodes.Error, ex.Message));
                })
                .ExecuteAsync();
        }
    }
}