using AggilleDFe.Application.DTOs;

namespace AggilleDFe.Application.Interfaces;

public interface IAutenticacaoService
{
    /// <returns>Resultado com o token, ou null se login/senha inválidos ou usuário inativo.</returns>
    Task<LoginResponseDto?> AutenticarAsync(LoginRequestDto dto, CancellationToken cancellationToken = default);
}
