using Application.DTOs.Audit;
using Application.DTOs.Auth;
using Application.DTOs.Mfa;
using Application.DTOs.Organization;
using AutoMapper;
using Domain.Entities.Audit;
using Domain.Entities.Identity;
using Domain.Entities.Security;

namespace Application.Mapper.Base;

/// <summary>
/// Represents a base AutoMapper profile configuration for mapping domain entities to Data Transfer Objects (DTOs).
/// </summary>
/// <remarks>
/// This class is used as a foundation for mapping configurations involving domain entities and DTOs in the application.
/// It inherits from the <see cref="Profile"/> class provided by the AutoMapper library.
/// </remarks>
public class BaseProfile : Profile
{
    public BaseProfile()
    {
        CreateMap<Role, RoleDto>().ReverseMap();
        CreateMap<Privilege, PrivilegeDto>();
        CreateMap<Organization, BasicOrganizationDto>();

        // Audit mappings
        CreateMap<AuditArchiveManifest, ArchiveManifestDto>();
        CreateMap<AuditLedgerEntry, AuditEntryDto>();

        // MFA mappings
        CreateMap<MfaPushDevice, MfaPushDeviceDto>();
    }
}
