using Microsoft.EntityFrameworkCore;
using ProcessosJudiciais.Api.Models;

namespace ProcessosJudiciais.Api.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Processo> Processos => Set<Processo>();
    public DbSet<Movimentacao> Movimentacoes => Set<Movimentacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Processo>(entity =>
        {
            entity.ToTable("Processos");
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.Numero).IsUnique();
        });

        // Relacionamento 1:N
        modelBuilder.Entity<Movimentacao>(entity =>
        {
            entity.ToTable("Movimentacoes");
            entity.HasKey(m => m.Id);

            entity.Property(m => m.TextoMovimentacao).HasColumnName("TextoMovimentacao").IsRequired();

            entity.HasOne(m => m.Processo)
                  .WithMany(p => p.Movimentacoes)
                  .HasForeignKey(m => m.ProcessoId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}