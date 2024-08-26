﻿// <auto-generated />
using System;
using DevHome.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace DevHome.Database.Migrations
{
    [DbContext(typeof(DevHomeDatabaseContext))]
    [Migration("20240823230859_InitialMigration")]
    partial class InitialMigration
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.8");

            modelBuilder.Entity("DevHome.Database.DatabaseModels.RepositoryManagement.Repository", b =>
                {
                    b.Property<int>("RepositoryId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("CreatedUTCDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT")
                        .HasDefaultValueSql("datetime()");

                    b.Property<string>("RepositoryClonePath")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT")
                        .HasDefaultValue("");

                    b.Property<string>("RepositoryName")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT")
                        .HasDefaultValue("");

                    b.Property<DateTime?>("UpdatedUTCDate")
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("TEXT");

                    b.HasKey("RepositoryId");

                    b.HasIndex("RepositoryName", "RepositoryClonePath")
                        .IsUnique();

                    b.ToTable("Repositories");
                });

            modelBuilder.Entity("DevHome.Database.DatabaseModels.RepositoryManagement.RepositoryMetadata", b =>
                {
                    b.Property<int>("RepositoryMetadataId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("CreatedUTCDate")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT")
                        .HasDefaultValue(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

                    b.Property<bool>("IsHiddenFromPage")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasDefaultValue(false);

                    b.Property<int>("RepositoryId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("UpdatedUTCDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT")
                        .HasDefaultValue(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

                    b.Property<DateTime>("UtcDateHidden")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT")
                        .HasDefaultValue(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

                    b.HasKey("RepositoryMetadataId");

                    b.HasIndex("RepositoryId")
                        .IsUnique();

                    b.ToTable("RepositoryMetadatas");
                });

            modelBuilder.Entity("DevHome.Database.DatabaseModels.RepositoryManagement.RepositoryMetadata", b =>
                {
                    b.HasOne("DevHome.Database.DatabaseModels.RepositoryManagement.Repository", "Repository")
                        .WithOne("RepositoryMetadata")
                        .HasForeignKey("DevHome.Database.DatabaseModels.RepositoryManagement.RepositoryMetadata", "RepositoryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Repository");
                });

            modelBuilder.Entity("DevHome.Database.DatabaseModels.RepositoryManagement.Repository", b =>
                {
                    b.Navigation("RepositoryMetadata");
                });
#pragma warning restore 612, 618
        }
    }
}