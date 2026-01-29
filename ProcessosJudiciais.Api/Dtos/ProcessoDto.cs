using System.ComponentModel.DataAnnotations;

namespace ProcessosJudiciais.Api.Dtos;

public record ProcessoDto(
    [Required(ErrorMessage = "O número do processo é obrigatório.")]
    [RegularExpression(@"^\d{7}-\d{2}\.\d{4}\.\d\.\d{2}\.\d{4}$",
     ErrorMessage = "O número deve seguir o padrão CNJ: 0000000-00.0000.0.00.0000")]
    string Numero,

    [Required]
    [StringLength(100, MinimumLength = 3)]
    string NomeAutor,

    [Required]
    [StringLength(100, MinimumLength = 3)]
    string NomeReu
);