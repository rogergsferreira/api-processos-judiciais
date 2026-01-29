using System.ComponentModel.DataAnnotations;

namespace ProcessosJudiciais.Api.Dtos;

public record MovimentacaoDto(
    [Required(ErrorMessage = "O texto da movimentação é obrigatório.")]
    [StringLength(500, MinimumLength = 5)]
    string TextoMovimentacao
);