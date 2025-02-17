﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Persistence;

#nullable disable

namespace Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Core.Entities.Memoryland", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<long>("MemorylandTypeId")
                        .HasColumnType("bigint");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("MemorylandTypeId");

                    b.HasIndex("UserId");

                    b.HasIndex("Name", "UserId")
                        .IsUnique();

                    b.ToTable("Memorylands");
                });

            modelBuilder.Entity("Core.Entities.MemorylandConfiguration", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<long>("MemorylandId")
                        .HasColumnType("bigint");

                    b.Property<long>("PhotoId")
                        .HasColumnType("bigint");

                    b.Property<int>("Position")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("MemorylandId");

                    b.HasIndex("PhotoId");

                    b.HasIndex("Position", "MemorylandId")
                        .IsUnique();

                    b.ToTable("MemorylandConfigurations");
                });

            modelBuilder.Entity("Core.Entities.MemorylandToken", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<bool>("IsInternal")
                        .HasColumnType("boolean");

                    b.Property<long>("MemorylandId")
                        .HasColumnType("bigint");

                    b.Property<Guid>("Token")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasDefaultValueSql("gen_random_uuid()");

                    b.HasKey("Id");

                    b.HasIndex("MemorylandId");

                    b.HasIndex("Token")
                        .IsUnique();

                    b.HasIndex("IsInternal", "MemorylandId")
                        .IsUnique();

                    b.ToTable("MemorylandTokens");
                });

            modelBuilder.Entity("Core.Entities.MemorylandType", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<int>("PhotoAmount")
                        .HasMaxLength(50)
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("MemorylandTypes");
                });

            modelBuilder.Entity("Core.Entities.Photo", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<long>("PhotoAlbumId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("PhotoAlbumId");

                    b.HasIndex("Name", "PhotoAlbumId")
                        .IsUnique();

                    b.ToTable("Photos");
                });

            modelBuilder.Entity("Core.Entities.PhotoAlbum", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.HasIndex("Name", "UserId")
                        .IsUnique();

                    b.ToTable("PhotoAlbums");
                });

            modelBuilder.Entity("Core.Entities.Transaction", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<long>("PhotoAlbumId")
                        .HasColumnType("bigint");

                    b.Property<string>("PhotoAlbumPath")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("PhotoAlbumId");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.ToTable("Transactions");
                });

            modelBuilder.Entity("Core.Entities.User", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.HasKey("Id");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Core.Entities.Memoryland", b =>
                {
                    b.HasOne("Core.Entities.MemorylandType", "MemorylandType")
                        .WithMany()
                        .HasForeignKey("MemorylandTypeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Core.Entities.User", "User")
                        .WithMany("Memorylands")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("MemorylandType");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Core.Entities.MemorylandConfiguration", b =>
                {
                    b.HasOne("Core.Entities.Memoryland", "Memoryland")
                        .WithMany("MemorylandConfigurations")
                        .HasForeignKey("MemorylandId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Core.Entities.Photo", "Photo")
                        .WithMany()
                        .HasForeignKey("PhotoId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Memoryland");

                    b.Navigation("Photo");
                });

            modelBuilder.Entity("Core.Entities.MemorylandToken", b =>
                {
                    b.HasOne("Core.Entities.Memoryland", "Memoryland")
                        .WithMany("MemorylandTokens")
                        .HasForeignKey("MemorylandId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Memoryland");
                });

            modelBuilder.Entity("Core.Entities.Photo", b =>
                {
                    b.HasOne("Core.Entities.PhotoAlbum", "PhotoAlbum")
                        .WithMany("Photos")
                        .HasForeignKey("PhotoAlbumId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("PhotoAlbum");
                });

            modelBuilder.Entity("Core.Entities.PhotoAlbum", b =>
                {
                    b.HasOne("Core.Entities.User", "User")
                        .WithMany("PhotoAlbums")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("Core.Entities.Transaction", b =>
                {
                    b.HasOne("Core.Entities.PhotoAlbum", "PhotoAlbum")
                        .WithMany()
                        .HasForeignKey("PhotoAlbumId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Core.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("PhotoAlbum");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Core.Entities.Memoryland", b =>
                {
                    b.Navigation("MemorylandConfigurations");

                    b.Navigation("MemorylandTokens");
                });

            modelBuilder.Entity("Core.Entities.PhotoAlbum", b =>
                {
                    b.Navigation("Photos");
                });

            modelBuilder.Entity("Core.Entities.User", b =>
                {
                    b.Navigation("Memorylands");

                    b.Navigation("PhotoAlbums");
                });
#pragma warning restore 612, 618
        }
    }
}
