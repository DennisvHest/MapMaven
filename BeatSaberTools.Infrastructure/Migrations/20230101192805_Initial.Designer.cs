﻿// <auto-generated />
using System;
using BeatSaberTools.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace BeatSaberTools.Infrastructure.Migrations
{
    [DbContext(typeof(BSToolsContext))]
    [Migration("20230101192805_Initial")]
    partial class Initial
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.1");

            modelBuilder.Entity("BeatSaberTools.Models.Data.MapInfo", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("AddedDateTime")
                        .HasColumnType("TEXT");

                    b.Property<string>("CoverImageFilename")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasAnnotation("Relational:JsonPropertyName", "_coverImageFilename");

                    b.Property<string>("DirectoryPath")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Hash")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("LevelAuthorName")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasAnnotation("Relational:JsonPropertyName", "_levelAuthorName");

                    b.Property<float>("PreviewDurationInSeconds")
                        .HasColumnType("REAL")
                        .HasAnnotation("Relational:JsonPropertyName", "_previewDuration");

                    b.Property<float>("PreviewStartTimeInSeconds")
                        .HasColumnType("REAL")
                        .HasAnnotation("Relational:JsonPropertyName", "_previewStartTime");

                    b.Property<string>("SongAuthorName")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasAnnotation("Relational:JsonPropertyName", "_songAuthorName");

                    b.Property<TimeSpan>("SongDuration")
                        .HasColumnType("TEXT");

                    b.Property<string>("SongFileName")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasAnnotation("Relational:JsonPropertyName", "_songFilename");

                    b.Property<string>("SongName")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasAnnotation("Relational:JsonPropertyName", "_songName");

                    b.HasKey("Id");

                    b.ToTable("MapInfos");
                });
#pragma warning restore 612, 618
        }
    }
}
