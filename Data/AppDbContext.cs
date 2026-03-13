// ApiSGA/Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using ApiSGA.Models;  // ajustá el namespace de tus modelos

namespace ApiSGA.Data
{
    // OJO: hereda de Microsoft.EntityFrameworkCore.DbContext
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<Oficina> Oficinas { get; set; } = null!;
        public DbSet<Sede> Sedes { get; set; } = null!;
        public DbSet<CategoriaOficina> Categorias { get; set; } = null!;
        public DbSet<Prestamo> Prestamos { get; set; } = null!;
        public DbSet<Reporte> Reportes { get; set; }
        public DbSet<Activo> Activos { get; set; } = null!;
        public DbSet<Bitacora> Bitacoras { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("usuario");

                entity.HasKey(e => e.Id_Usuario);

                entity.Property(e => e.Id_Usuario).HasColumnName("id_usuario");
                entity.Property(e => e.Nombre).HasColumnName("nombre");
                entity.Property(e => e.Correo).HasColumnName("correo");
                entity.Property(e => e.Contrasena).HasColumnName("contrasena");
                entity.Property(e => e.Rol).HasColumnName("rol");
            });

            modelBuilder.Entity<Oficina>(entity =>
            {
                entity.ToTable("oficina");

                entity.HasKey(o => o.Id_Oficina);

                entity.Property(o => o.Id_Oficina).HasColumnName("id_oficina");
                entity.Property(o => o.Nombre_Oficina).HasColumnName("nombre_oficina");
                entity.Property(o => o.Encargado).HasColumnName("encargado");
                entity.Property(o => o.Id_Sede).HasColumnName("id_sede");
                entity.Property(o => o.Id_Categoria).HasColumnName("id_categoria");

                entity.HasOne(o => o.Sede)
                      .WithMany(s => s.Oficinas)
                      .HasForeignKey(o => o.Id_Sede);

                entity.HasOne(o => o.Categoria)
                      .WithMany(c => c.Oficinas)
                      .HasForeignKey(o => o.Id_Categoria);
            });

            modelBuilder.Entity<Sede>(entity =>
            {
                entity.ToTable("sede");
                entity.HasKey(s => s.Id_Sede);
                entity.Property(s => s.Id_Sede).HasColumnName("id_sede");
                entity.Property(s => s.Nombre_Sede).HasColumnName("nombre_sede");
            });

            modelBuilder.Entity<CategoriaOficina>(entity =>
            {
                entity.ToTable("categoria_oficina");
                entity.HasKey(c => c.Id_Categoria);
                entity.Property(c => c.Id_Categoria).HasColumnName("id_categoria");
                entity.Property(c => c.Nombre_Categoria).HasColumnName("nombre_categoria");
            });

            modelBuilder.Entity<Activo>(entity =>
            {
                entity.ToTable("activo");

                entity.HasKey(a => a.Id_Activo);

                entity.Property(a => a.Id_Activo).HasColumnName("id_activo");
                entity.Property(a => a.Placa).HasColumnName("placa");
                entity.Property(a => a.Descripcion).HasColumnName("descripcion");
                entity.Property(a => a.Modelo).HasColumnName("modelo");
                entity.Property(a => a.Serie).HasColumnName("serie");
                entity.Property(a => a.Estado).HasColumnName("estado");
                entity.Property(a => a.Id_Sede).HasColumnName("id_sede");
                entity.Property(a => a.Id_Oficina).HasColumnName("id_oficina");
                entity.Property(a => a.Observaciones).HasColumnName("observaciones");
                entity.Property(a => a.Estado_Oaf).HasColumnName("estado_oaf");

                // Relación con Sede (sin colección en Sede)
                entity.HasOne(a => a.Sede)
                      .WithMany() // ⬅ sin s => s.Activos
                      .HasForeignKey(a => a.Id_Sede);

                // Relación con Oficina (sin colección en Oficina)
                entity.HasOne(a => a.Oficina)
                      .WithMany() // ⬅ sin o => o.Activos
                      .HasForeignKey(a => a.Id_Oficina);
            });
            modelBuilder.Entity<Prestamo>(entity =>
            {
                entity.ToTable("prestamo");

                entity.HasKey(e => e.Id_Prestamo);

                entity.Property(e => e.Id_Prestamo).HasColumnName("id_prestamo");
                entity.Property(e => e.Fecha).HasColumnName("fecha");
                entity.Property(e => e.Estado).HasColumnName("estado");
                entity.Property(e => e.Observacion).HasColumnName("observacion");
                entity.Property(e => e.Id_Activo).HasColumnName("id_activo");
                entity.Property(e => e.Id_Solicitante).HasColumnName("id_solicitante");
                entity.Property(e => e.Id_Encargado).HasColumnName("id_encargado");

                // Relación Prestamo -> Activo
                entity.HasOne(e => e.Activo)
                      .WithMany()   // si luego querés: .WithMany(a => a.Prestamos)
                      .HasForeignKey(e => e.Id_Activo)
                      .OnDelete(DeleteBehavior.Restrict);

                // Relación Prestamo -> Usuario (Encargado)
                entity.HasOne<Usuario>()
                      .WithMany()
                      .HasForeignKey(e => e.Id_Encargado)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Reporte>(entity =>
            {
                entity.ToTable("reporte");

                entity.HasKey(e => e.Id_Reporte);

                entity.Property(e => e.Id_Reporte).HasColumnName("id_reporte");
                entity.Property(e => e.Fecha).HasColumnName("fecha");
                entity.Property(e => e.Tipo_Reporte).HasColumnName("tipo_reporte");
                entity.Property(e => e.Id_Usuario).HasColumnName("id_usuario");

                // Relación opcional con usuario (id_usuario → usuario.id_usuario)
                entity.HasOne(e => e.Usuario)
                    .WithMany() // si después querés, podés crear ICollection<Reporte> en Usuario
                    .HasForeignKey(e => e.Id_Usuario)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Bitacora>(entity =>
            {
                entity.ToTable("bitacora");

                entity.HasKey(e => e.Id_Bitacora);

                entity.Property(e => e.Id_Bitacora).HasColumnName("id_bitacora");
                entity.Property(e => e.Fecha).HasColumnName("fecha");
                entity.Property(e => e.Accion).HasColumnName("accion");
                entity.Property(e => e.Detalle).HasColumnName("detalle");
                entity.Property(e => e.Id_Usuario).HasColumnName("id_usuario");

                entity.HasOne(e => e.Usuario)
                    .WithMany() // si querés, después agregás ICollection<Bitacora> en Usuario
                    .HasForeignKey(e => e.Id_Usuario)
                    .OnDelete(DeleteBehavior.Restrict);
            });

        }
    }
}
