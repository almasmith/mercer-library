using AutoMapper;
using Library.Api.Domain;
using Library.Api.Dtos;

namespace Library.Api.Mappings;

public sealed class BookProfile : Profile
{
    public BookProfile()
    {
        CreateMap<Book, BookDto>();
        CreateMap<CreateBookRequest, Book>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.OwnerUserId, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.MapFrom(_ => DateTimeOffset.UtcNow))
            .ForMember(d => d.UpdatedAt, o => o.MapFrom(_ => DateTimeOffset.UtcNow))
            .ForMember(d => d.RowVersion, o => o.Ignore());
        CreateMap<UpdateBookRequest, Book>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.OwnerUserId, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.MapFrom(_ => DateTimeOffset.UtcNow))
            .ForMember(d => d.RowVersion, o => o.Ignore());
    }
}


