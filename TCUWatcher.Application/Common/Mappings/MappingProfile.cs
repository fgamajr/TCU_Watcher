using AutoMapper;
using TCUWatcher.Application.SessionEvents.DTOs;
using TCUWatcher.Domain.Entities;

namespace TCUWatcher.Application.Common.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Mapeia da Entidade para o DTO de leitura
            CreateMap<SessionEvent, SessionEventDto>();

            // Mapeia do DTO de criação para a Entidade
            CreateMap<CreateSessionEventDto, SessionEvent>();
        }
    }
}