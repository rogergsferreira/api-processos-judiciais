using Microsoft.EntityFrameworkCore;
using ProcessosJudiciais.Api.Models;

namespace ProcessosJudiciais.Api.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Processo> Processos => Set<Processo>();
    public DbSet<Movimentacao> Movimentacoes => Set<Movimentacao>();
    public DbSet<Alerta> Alertas => Set<Alerta>();

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

        modelBuilder.Entity<Alerta>(entity =>
        {
            entity.ToTable("Alertas");
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => new { a.ProcessoId, a.ResolvidoEm });

            entity.HasOne(a => a.Processo)
                  .WithMany()
                  .HasForeignKey(a => a.ProcessoId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}