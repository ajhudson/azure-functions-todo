using System;
using AutoMapper;
using Randolph.ToDoFunctionApp.Entities;
using Randolph.ToDoFunctionApp.Models;

namespace Randolph.ToDoFunctionApp.MappingProfiles;

public class ToDoMappingProfile : Profile
{
    public ToDoMappingProfile()
    {
        this.CreateMap<ToDoModel, TodoTableEntity>()
            .ForMember(e => e.PartitionKey, opts => opts.MapFrom(_ => Constants.PartitionKey))
            .ForMember(e => e.RowKey, opts => opts.MapFrom(m => m.ToDoId.ToString("n")))
            .ForMember(e => e.CreatedDt, opts => opts.MapFrom(_ => DateTime.UtcNow))
            .ReverseMap()
            .ForMember(m => m.ToDoId, opts => opts.MapFrom(e => e.RowKey));

        this.CreateMap<UpdateToDoModel, TodoTableEntity>();
    }
}