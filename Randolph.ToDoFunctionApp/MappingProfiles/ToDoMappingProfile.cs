using AutoMapper;
using Randolph.ToDoFunctionApp.Entities;
using Randolph.ToDoFunctionApp.Models;

namespace Randolph.ToDoFunctionApp.MappingProfiles;

public class ToDoMappingProfile : Profile
{
    public ToDoMappingProfile()
    {
        this.CreateMap<ToDoModel, TodoTableEntity>()
            .ForMember(e => e.PartitionKey, opts => opts.MapFrom(_ => "TODO"))
            .ReverseMap()
            .ForMember(m => m.Id, opts => opts.MapFrom(e => e.RowKey));
    }
}