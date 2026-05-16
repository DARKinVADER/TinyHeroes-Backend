using TinyHeroes.Domain.Enums;

namespace TinyHeroes.Application.DTOs.Child;

public record CreateChildRequest(string Name, int Age, Gender Gender, string AvatarEmoji);
public record UpdateChildRequest(string Name, int Age, Gender Gender, string AvatarEmoji);
public record ChildResponse(Guid Id, string Name, int Age, Gender Gender, string AvatarEmoji);
